#!/usr/bin/env python3
"""Detect game store type for subfolders using signal files only.

Priority order (first match wins):
  1. GOG       — goggame-* files at root
  2. EA        — __Installer/ directory at root
  3. Ubisoft   — uplay_install.manifest / uplay_r*_loader*.dll at root
  4. Epic      — .egstore/ or .egsstore/ directory at root
  5. Blizzard  — .battle.net/ directory at root
  6. Xbox      — default-metadata.json at root
  7. Rockstar  — title.rgl at root
  8. Steam Emu — steam_api64.dll / steam_api.dll at root (also deep UE ThirdParty path)

Steam is detected via library structure (steamapps/ + .acf), not signal files.

If root-level signals miss, a second pass checks deeper patterns:
  - steam_emu.ini at root or in child folders → Steam Emulator
  - child folders with launcher signals → scan child as actual game folder
  - Unreal-style Binaries/Win64 executable layout → Standalone

Note: UE bundles Steamworks SDK in Engine/Binaries/ThirdParty/Steamworks/
by default. That is NOT a valid Steam Emulator signal — only steam_emu.ini
and root-level steam_api64.dll (outside Steam library) indicate emulation.

Unrecognized folders are listed at the end so we can investigate what
signals to add for them.
"""

import json, sys, urllib.parse, urllib.request
from pathlib import Path

try:
    import pefile
except Exception:  # pragma: no cover - optional helper dependency
    pefile = None

EXTRACT_METADATA = False
VERIFY_PCGW = False


# ── Signal definitions ────────────────────────────────────────
# Priority order (first match wins).


def _check_gog(d: Path):
    """Any goggame* file at root (catches goggame-*.info and goggame.dll)."""
    return bool(list(d.glob("goggame*")))


def _check_ea(d: Path):
    """__Installer/ directory at root."""
    return (d / "__Installer").is_dir()


def _check_ubisoft(d: Path):
    """uplay_install.manifest or uplay_r*_loader*.dll at root."""
    return (d / "uplay_install.manifest").exists() \
        or bool(list(d.glob("uplay_r*_loader*.dll")))


def _check_ubisoft_emu(d: Path):
    """Ubisoft emulator loader/config pattern at root."""
    has_loader = bool(list(d.glob("uplay_loader*"))) or bool(list(d.glob("uplay_r*_loader*")))
    if not has_loader:
        return False
    for ini in d.glob("*.ini"):
        try:
            text = ini.read_text(encoding="utf-8", errors="ignore")
        except OSError:
            continue
        if "Username=" in text and "AccountId=" in text:
            return True
    return False


def _check_epic(d: Path):
    """.egstore/ or .egsstore/ directory at root."""
    return (d / ".egstore").is_dir() or (d / ".egsstore").is_dir()


def _check_blizzard(d: Path):
    """.battle.net/ directory at root (Battle.net / Blizzard games)."""
    return (d / ".battle.net").is_dir()


def _check_xbox(d: Path):
    """default-metadata.json at root (Xbox Game Pass / Microsoft Store)."""
    return (d / "default-metadata.json").exists()


def _check_steam_emu(d: Path):
    """steam_api64.dll or steam_api.dll at root."""
    return (d / "steam_api64.dll").exists() or (d / "steam_api.dll").exists()


def _check_rockstar(d: Path):
    """title.rgl at root (Rockstar Games Launcher)."""
    return (d / "title.rgl").exists()


def _check_root_exe(d: Path):
    """Any non-noise executable at root marks a standalone game folder."""
    for child in d.iterdir():
        if child.is_file() and child.suffix.lower() == ".exe" and not _is_noise_exe(child.name):
            return True
    return False


def _check_root_lnk(d: Path):
    """Windows shortcut at root can mark older standalone installs."""
    return any(child.is_file() and child.suffix.lower() == ".lnk" for child in d.iterdir())


SIGNAL_CHECKS = [
    ("GOG",             "goggame",           _check_gog),
    ("EA",              "ea_installer",      _check_ea),
    ("Ubisoft Emulator", "uplay_emu",         _check_ubisoft_emu),
    ("Ubisoft",         "uplay",             _check_ubisoft),
    ("Epic",            "egstore",           _check_epic),
    ("Blizzard",        "battle_net",        _check_blizzard),
    ("Xbox",            "default_metadata",  _check_xbox),
    ("Rockstar",        "rgl",               _check_rockstar),
    ("Steam Emulator",  "steam_api",         _check_steam_emu),
]


NOISE_EXE_PARTS = (
    "cleanup",
    "touchup",
    "crash",
    "installer",
    "unins",
    "uninstall",
    "setup",
    "redist",
    "vcredist",
    "dxsetup",
)


def _is_noise_exe(name: str) -> bool:
    lower = name.lower()
    return any(part in lower for part in NOISE_EXE_PARTS)


def _find_executables(d: Path) -> list[Path]:
    """Collect likely game executables from common shallow layouts."""
    candidates = []

    def add_exes(folder: Path):
        if not folder.is_dir():
            return
        for item in folder.iterdir():
            if item.is_file() and item.suffix.lower() == ".exe" and not _is_noise_exe(item.name):
                candidates.append(item)

    add_exes(d)
    for child in d.iterdir():
        if child.is_dir():
            add_exes(child)
            add_exes(child / "Binaries" / "Win64")
            add_exes(child / "Binaries" / "WinGDK")
    # Preserve order, remove duplicates.
    seen = set()
    unique = []
    for exe in candidates:
        key = str(exe)
        if key not in seen:
            seen.add(key)
            unique.append(exe)
    return unique


def _find_light_executable_names(d: Path) -> list[str]:
    """Cheap name-only executable candidates; avoids PE parsing and deep walks."""
    names = []

    def add(folder: Path):
        if not folder.is_dir():
            return
        for item in folder.iterdir():
            if item.is_file() and item.suffix.lower() == ".exe" and not _is_noise_exe(item.name):
                names.append(item.stem)

    add(d)
    for child in d.iterdir():
        if not child.is_dir():
            continue
        add(child / "Binaries" / "Win64")
        add(child / "Binaries" / "WinGDK")
    return names[:5]


def _name_candidates(folder_label: str, folder_path: Path, entry: dict) -> list[str]:
    candidates = []
    if entry.get("name"):
        candidates.append(entry["name"])
    candidates.append(Path(folder_label).name)
    candidates.extend(_find_light_executable_names(folder_path))
    cleaned = []
    seen = set()
    for candidate in candidates:
        value = candidate.replace("_", " ").replace("-", " ").strip()
        if value and value.lower() not in seen:
            seen.add(value.lower())
            cleaned.append(value)
    return cleaned


def _pcgw_lookup(name: str) -> dict | None:
    if not name:
        return None
    url = "https://www.pcgamingwiki.com/w/api.php?" + urllib.parse.urlencode({
        "action": "opensearch",
        "search": name,
        "limit": "1",
        "namespace": "0",
        "format": "json",
    })
    try:
        with urllib.request.urlopen(url, timeout=5) as response:
            data = json.loads(response.read().decode("utf-8"))
    except Exception:
        return None
    if len(data) >= 4 and data[1]:
        return {"query": name, "title": data[1][0], "url": data[3][0] if data[3] else ""}
    return None


def _verify_with_pcgw(entry: dict, folder_label: str, folder_path: Path) -> bool:
    candidates = _name_candidates(folder_label, folder_path, entry)
    entry["name_candidates"] = candidates
    if not VERIFY_PCGW:
        return False
    for candidate in candidates:
        match = _pcgw_lookup(candidate)
        if match:
            entry["pcgw"] = match
            if "name" not in entry:
                entry["name"] = match["title"]
            return True
    entry["needs_name_review"] = True
    return False


def _read_pe_metadata(exe: Path) -> dict:
    if pefile is None:
        return {}
    try:
        pe = pefile.PE(str(exe), fast_load=False)
    except Exception:
        return {}
    metadata = {}
    try:
        for file_info in getattr(pe, "FileInfo", []) or []:
            for table in getattr(file_info, "StringTable", []) or []:
                for raw_key, raw_value in table.entries.items():
                    key = raw_key.decode("utf-8", errors="ignore") if isinstance(raw_key, bytes) else str(raw_key)
                    value = raw_value.decode("utf-8", errors="ignore") if isinstance(raw_value, bytes) else str(raw_value)
                    if key in ("FileDescription", "ProductName", "OriginalFilename", "CompanyName") and value:
                        metadata[key] = value
    except Exception:
        return metadata
    return metadata


def _pick_primary_executable(folder_path: Path) -> tuple[str | None, dict]:
    exes = _find_executables(folder_path)
    if not exes:
        return None, {}

    scored = []
    folder_tokens = {part.lower() for part in folder_path.name.replace("_", " ").replace("-", " ").split() if part}
    for exe in exes:
        score = 0
        lower = exe.name.lower()
        if "launcher" in lower:
            score -= 20
        if "shipping" in lower or "win64" in lower:
            score += 5
        if any(token in lower for token in folder_tokens):
            score += 10
        try:
            score += min(exe.stat().st_size // 10_000_000, 10)
        except OSError:
            pass
        scored.append((score, exe))

    scored.sort(key=lambda item: item[0], reverse=True)
    # PE metadata parsing is optional because parsing large game executables is
    # too slow for every-folder detection passes.
    best_score, best = scored[0]
    best_metadata = {}
    if not EXTRACT_METADATA:
        return str(best.relative_to(folder_path)), best_metadata

    # Parse PE metadata only for the strongest few candidates.
    for score, exe in scored[:3]:
        metadata = _read_pe_metadata(exe)
        if metadata.get("FileDescription") or metadata.get("ProductName"):
            score += 8
        if score > best_score:
            best_score = score
            best = exe
            best_metadata = metadata
        elif exe == best:
            best_metadata = metadata
    return str(best.relative_to(folder_path)), best_metadata


# ── Engine detection ──────────────────────────────────────────
# Local-signal only. If no reliable engine signal exists, return Unknown.

def _detect_engine(d: Path) -> str:
    if _has_unreal_engine_signal(d):
        return "Unreal Engine"
    if _has_unity_signal(d):
        return "Unity"
    if _has_rockstar_rage_signal(d):
        return "RAGE"
    if _has_frostbite_signal(d):
        return "Frostbite"
    return "Unknown"


def _has_unreal_engine_signal(d: Path) -> bool:
    if not (d / "Engine").is_dir():
        return False
    if (d / "Engine" / "Binaries").is_dir():
        return True
    for child in d.iterdir():
        if child.is_dir() and (child / "Binaries" / "Win64").is_dir():
            return True
    return False


def _has_unity_signal(d: Path) -> bool:
    if not (d / "UnityPlayer.dll").exists():
        return False
    return any(child.is_dir() and child.name.endswith("_Data") for child in d.iterdir())


def _has_rockstar_rage_signal(d: Path) -> bool:
    return (d / "title.rgl").exists() and (d / "common.rpf").exists()


def _has_frostbite_signal(d: Path) -> bool:
    return (d / "Engine.BuildInfo_Win64_retail.dll").exists()


def detect(d: Path):
    """Returns (store_name, signal_label) or (None, None) if unrecognized."""
    # Pass 1: root-level signals (fast)
    for store, signal, check_fn in SIGNAL_CHECKS:
        if check_fn(d):
            return store, signal
    # Pass 2: deep signal checks (common subfolder patterns)
    return _detect_deep(d)


# ── Deep signal checks (second pass, focused) ────────────────
# Note: UE bundles Steamworks SDK in Engine/Binaries/ThirdParty/Steamworks/
# by default. That is NOT a valid Steam Emulator signal.
# Only steam_emu.ini (emulator config) and root-level steam_api64.dll
# (outside Steam library paths) are reliable emulator indicators.

def _has_steam_emu_ini(d: Path) -> bool:
    """steam_emu.ini at root, in child dirs, or in UE ThirdParty/Steamworks path."""
    if (d / "steam_emu.ini").exists():
        return True
    for child in d.iterdir():
        if child.is_dir() and (child / "steam_emu.ini").exists():
            return True
    # UE pattern: Engine/Binaries/ThirdParty/Steamworks/Steamv*/Win64/
    sw = d / "Engine" / "Binaries" / "ThirdParty" / "Steamworks"
    if sw.is_dir():
        for sv in sw.iterdir():
            if (sv / "Win64" / "steam_emu.ini").exists():
                return True
    return False


def _has_blizzard_deep(d: Path) -> bool:
    """.battle.net/ in an immediate child (container pattern)."""
    for child in d.iterdir():
        if child.is_dir() and (child / ".battle.net").is_dir():
            return True
    return False


def _has_ubisoft_legacy(d: Path) -> bool:
    """Legacy Ubisoft title signal observed in older installs."""
    if (d / "UbiStats.dll").exists():
        return True
    for child in d.iterdir():
        if child.is_dir() and (child / "UbiStats.dll").exists():
            return True
    return False


def _has_unreal_game_layout(d: Path) -> bool:
    """Unreal game folder with Engine/ plus */Binaries/Win64/*.exe."""
    if not (d / "Engine").is_dir():
        return False
    for child in d.iterdir():
        if not child.is_dir() or child.name == "Engine":
            continue
        binaries = child / "Binaries" / "Win64"
        if not binaries.is_dir():
            continue
        for exe in binaries.iterdir():
            if exe.is_file() and exe.suffix.lower() == ".exe" and not _is_noise_exe(exe.name):
                return True
    return False


# Priority order for deep checks (after all root-level checks fail)
DEEP_CHECKS = [
    ("Steam Emulator", "emu_ini", _has_steam_emu_ini),
    ("Ubisoft",        "ubistats", _has_ubisoft_legacy),
    ("Standalone",     "unreal_binaries", _has_unreal_game_layout),
    ("Standalone",     "root_exe", _check_root_exe),
    ("Standalone",     "root_lnk", _check_root_lnk),
]


def _detect_deep(d: Path):
    """Second-pass detection using deeper file patterns."""
    for store, signal, check_fn in DEEP_CHECKS:
        if check_fn(d):
            return store, signal
    return None, None


def _make_entry(folder_label: str, folder_path: Path, store: str, signal: str | None):
    entry = {"folder": folder_label, "store": store, "engine": _detect_engine(folder_path)}
    if signal:
        entry["signal"] = signal

    # For GOG: grab game name + primary executable from .info files
    # Iterate all .info files; prefer the main game (gameId == rootGameId)
    if store == "GOG":
        best_name = None
        for info_file in folder_path.glob("goggame-*.info"):
            try:
                data = json.loads(info_file.read_text(encoding="utf-8", errors="ignore"))
                is_main = data.get("gameId") == data.get("rootGameId")
                game_name = data.get("name", "")
                if is_main:
                    best_name = game_name
                elif best_name is None:
                    best_name = game_name
                for task in data.get("playTasks", []):
                    if task.get("isPrimary") and task.get("path"):
                        entry["exe"] = task["path"]
            except Exception:
                pass
        if best_name:
            entry["name"] = best_name

    verified = _verify_with_pcgw(entry, folder_label, folder_path)
    if EXTRACT_METADATA and not verified:
        exe, metadata = _pick_primary_executable(folder_path)
        if exe and "exe" not in entry:
            entry["exe"] = exe
        if metadata:
            entry["exe_metadata"] = metadata
            if "name" not in entry:
                entry["name"] = metadata.get("FileDescription") or metadata.get("ProductName")
            if VERIFY_PCGW and entry.get("name"):
                match = _pcgw_lookup(entry["name"])
                if match:
                    entry["pcgw"] = match
                    entry.pop("needs_name_review", None)

    return entry


def _scan_container_children(container: Path):
    """Return detected child games for organizer folders, one level deep.

    Only promote launcher/store detections. Standalone child matches are too
    likely to be utility/support folders inside an actual game tree.
    """
    entries = []
    for child in sorted(container.iterdir()):
        if not child.is_dir():
            continue
        store, signal = detect(child)
        if store is not None and store != "Standalone":
            entries.append(_make_entry(f"{container.name}/{child.name}", child, store, signal))
    return entries


def scan_folder(folder_path: str) -> dict:
    path = Path(folder_path)
    result = {
        "input": folder_path,
        "store": None,
        "recognized": [],
        "unrecognized": [],
    }

    if not path.exists():
        return result

    # Steam library or Epic manifests directory — not game folders
    if (path / "steamapps").is_dir():
        result["store"] = "Steam Library"
        return result
    if list(path.glob("Manifests/*.item")):
        result["store"] = "Epic (Launcher)"
        return result

    if not path.is_dir():
        return result

    for child in sorted(path.iterdir()):
        if not child.is_dir():
            continue

        store, signal = detect(child)

        if store is None:
            nested = _scan_container_children(child)
            if nested:
                result["recognized"].extend(nested)
            else:
                result["unrecognized"].append(child.name)
        else:
            result["recognized"].append(_make_entry(child.name, child, store, signal))

    # Summary
    stores = {e["store"] for e in result["recognized"]}
    result["store"] = "Mixed" if len(stores) > 1 else (stores.pop() if stores else None)

    return result


def main():
    global EXTRACT_METADATA, VERIFY_PCGW
    if len(sys.argv) < 2:
        print("Usage: detect_folder.py <path> [--pcgw] [--metadata]", file=sys.stderr)
        sys.exit(1)
    VERIFY_PCGW = "--pcgw" in sys.argv
    EXTRACT_METADATA = "--metadata" in sys.argv
    result = scan_folder(sys.argv[1])
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
