#!/usr/bin/env python3
"""
DEPRECATED — Use tools/detect.py instead.

Standalone directory scanner — three-tier classification.

Flow:
  1. Quick root scan — check for signals (marker files/folders) and .exe.
  2. Classify immediately if clear signal found.  DONE.  No deep walk.
  3. Only for unknowns: deeper scan for signals + exe (only .exe/.dll/.ini files).
  4. If deep scan still finds nothing, flag for review.

Tiers (priority order):
  Tier 1 (HIGH)  — root has launcher markers (GOG, EA, Ubisoft, Epic)
  Tier 2 (LOW)   — root has .exe file(s) but no markers
  Tier 3 (? )    — no markers, no root exe → check children, else Unknown

Container detection:
  A folder is a container ONLY if at least one child has launcher markers.
  Children with only executables (no markers) do NOT make the parent a
  container — those exes belong to the parent's own game tree.
"""

import json
import os
import sys
from pathlib import Path


# ---------------------------------------------------------------------------
MARKER_SPEC: list[tuple[str, str, set[str]]] = [
    ("GOG",     "prefix", {"goggame-"}),
    ("GOG",     "exact",  {"goggame.dll"}),
    ("EA",      "dir",    {"__installer"}),
    ("Ubisoft", "exact",  {"uplay_r1_loader.dll", "uplay_r2_loader.dll",
                           "uplay_r1_loader64.dll", "uplay_r2_loader64.dll",
                           "uplay_r1_loader64.cdx",
                           "uplay_install.manifest", "uplay_install.state"}),
    ("Ubisoft", "dir",    {"uplay_download"}),
    ("Epic",    "dir",    {".egsstore", ".egstore"}),
    ("Steam Emulator", "exact", {"steam_api64.dll", "steam_api.dll"}),
]

SKIP_NAMES = {
    "_commonredist", "commonredist", "easyanticheat", "devtools",
    "support", "docs", "licenses", "vcredist", "directx",
    "steam controller configs", "steamworks shared",
}

SKIP_EXE_SUBSTR = {
    "installer", "crash_reporter", "reg_spy_launcher", "unins",
    "crashhandler", "vc_redist", "dxsetup", "oalinst", "setup",
    "uninstall", "dxwebsetup",
}

WALK_MAX_DEPTH = 4


# ---------------------------------------------------------------------------
def _exe_is_noise(name: str) -> bool:
    """True for filenames that are clearly not game executables."""
    return any(s in name.lower() for s in SKIP_EXE_SUBSTR)


def _match_markers(names_lower: set[str], dirs_lower: set[str]):
    """Return (store_name, matched_marker_list) or ('Standalone', [])."""
    for store, match_type, values in MARKER_SPEC:
        found: list[str] = []
        for v in values:
            if match_type == "exact":
                if v in names_lower:
                    found.append(v)
            elif match_type == "prefix":
                for n in sorted(names_lower):
                    if n.startswith(v):
                        found.append(n)
            elif match_type == "dir":
                if v in dirs_lower:
                    found.append(v)
        if found:
            return store, found
    return "Standalone", []


# ---------------------------------------------------------------------------
# Quick root scan — single directory, no recursion, no stat.
# ---------------------------------------------------------------------------

def _scan_root(path: Path):
    """
    One-level scan of *path*.  Returns:
      (store, markers, has_root_exe, root_exe_name, child_dirs)
    No file stats.  Only checks names.
    """
    try:
        entries = list(os.scandir(path))
    except PermissionError:
        return "Standalone", [], False, None, []

    names = set()
    dirs_lower = set()
    child_dirs = []
    root_exe = None

    for e in entries:
        name_lower = e.name.lower()
        names.add(name_lower)
        if e.is_dir(follow_symlinks=False):
            dirs_lower.add(name_lower)
            child_dirs.append(e)
        elif name_lower.endswith(".exe") and not _exe_is_noise(e.name):
            if root_exe is None:
                root_exe = e.name  # first non-noise exe

    store, markers = _match_markers(names, dirs_lower)
    has_root_exe = root_exe is not None
    return store, markers, has_root_exe, root_exe, child_dirs


# ---------------------------------------------------------------------------
# Deep signal scan — walk depth-limited, ONLY collect signals + exe names.
# No stat().  No size collection.  Fast.
# ---------------------------------------------------------------------------

def _deep_signal_scan(game_dir: Path):
    """
    Walk to WALK_MAX_DEPTH.  Only processes files with relevant extensions
    (.exe, .dll, .ini) — everything else is skipped.  This keeps the walk
    fast on large directories (e.g. MMO data folders with thousands of assets).
    Returns (store, markers, exe_names).
    """
    SIGNAL_EXTS = {".exe", ".dll", ".ini"}

    all_names: set[str] = set()
    all_dirs:  set[str] = set()
    exe_names: list[str] = []

    stack: list[tuple[Path, int]] = [(game_dir, 0)]
    while stack:
        current, depth = stack.pop()
        try:
            entries = list(os.scandir(current))
        except PermissionError:
            continue

        subdirs: list[Path] = []
        for e in entries:
            if e.is_dir(follow_symlinks=False):
                subdirs.append(Path(e.path))
                continue
            # Skip files without signal extensions entirely
            name_lower = e.name.lower()
            if not any(name_lower.endswith(ext) for ext in SIGNAL_EXTS):
                continue
            all_names.add(e.name.lower())
            if name_lower.endswith(".exe") and not _exe_is_noise(e.name):
                exe_names.append(e.name)

        if depth < WALK_MAX_DEPTH:
            for d in subdirs:
                all_dirs.add(d.name.lower())
                stack.append((d, depth + 1))

    store, markers = _match_markers(all_names, all_dirs)
    return store, markers, exe_names


# ---------------------------------------------------------------------------
# Build result dict
# ---------------------------------------------------------------------------

def _build_result(game_dir: Path, store: str, markers: list[str],
                  exe_names: list[str], **extra) -> dict:
    """Build a result dict from collected data."""
    result: dict = {
        "folder": game_dir.name,
        "path": str(game_dir),
        "store": store,
        "confidence": "High" if markers else "Low",
        "markers": markers,
        "exe_count": len(exe_names),
        "exes": [{"path": name, "name": Path(name).name} for name in exe_names],
        "gog_metadata": None,
    }
    result.update(extra)

    if store == "GOG":
        try:
            for f in game_dir.glob("goggame-*.info"):
                if f.is_file():
                    data = json.loads(f.read_text(encoding="utf-8", errors="ignore"))
                    result["gog_metadata"] = {
                        "title": data.get("title", ""),
                        "game_id": data.get("gameId", ""),
                    }
                break
        except Exception:
            pass
    return result


# ---------------------------------------------------------------------------
# Top-level scan
# ---------------------------------------------------------------------------

def scan_standalone_directory(
    root: str | Path,
    *,
    steam_library_paths: set[str] | None = None,
) -> list[dict]:
    """
    Scan *root* for games.

    Flow per folder:
      1. Quick root scan → if Tier 1 or Tier 2 → done, no deep walk.
      2. Only for unknowns → deep signal scan for signals + exe names.
      3. If nothing found → flag for review.
    """
    root_path = Path(root)
    if not root_path.is_dir():
        return []

    excluded = set()
    if steam_library_paths:
        for p in steam_library_paths:
            excluded.add(Path(p).as_posix().lower())

    def _excluded(path: Path) -> bool:
        return any(path.as_posix().lower().startswith(e) for e in excluded)

    games: list[dict] = []

    def _scan(parent: Path, prefix: str = ""):
        try:
            entries = sorted(os.scandir(parent), key=lambda e: e.name)
        except PermissionError:
            return

        for entry in entries:
            if not entry.is_dir(follow_symlinks=False):
                continue
            if entry.name.lower() in SKIP_NAMES:
                continue

            child = Path(entry.path)
            if _excluded(child):
                continue

            store, markers, has_root_exe, root_exe, child_dirs = _scan_root(child)

            # --- Tier 1: has markers → launcher game.  Done. ---
            if markers:
                exe_list = [root_exe] if root_exe else []
                games.append(_build_result(
                    child, store, markers, exe_list,
                    folder=f"{prefix}{child.name}",
                    **({"container": prefix.rstrip("/")} if prefix else {}),
                ))
                continue

            # --- Tier 2: root exe, no markers → standalone.  Done. ---
            if has_root_exe:
                games.append(_build_result(
                    child, store, [], [root_exe],
                    folder=f"{prefix}{child.name}",
                    **({"container": prefix.rstrip("/")} if prefix else {}),
                ))
                continue

            # --- Tier 3: no markers, no root exe ---
            # Quick container check: do child dirs have markers?
            is_container = False
            for c in child_dirs:
                c_store, c_markers, _, _, _ = _scan_root(Path(c.path))
                if c_markers:
                    is_container = True
                    break

            if is_container:
                _scan(child, prefix=f"{prefix}{entry.name}/")
                continue

            # Not a container.  Deep scan for signals + exe names.
            deep_store, deep_markers, deep_exes = _deep_signal_scan(child)

            games.append(_build_result(
                child, deep_store if deep_markers else "Unknown",
                deep_markers, deep_exes,
                folder=f"{prefix}{child.name}",
                **({"container": prefix.rstrip("/")} if prefix else {}),
                **({"needs_review": True} if not deep_markers else {}),
            ))

    _scan(root_path)
    games.sort(key=lambda g: g["folder"].lower())
    return games


# ---------------------------------------------------------------------------
def main() -> None:
    args = sys.argv[1:]
    if not args or "-h" in args or "--help" in args:
        print(
            "usage: python list_standalone_games.py <directory> [--steam-libraries"
            " PATH [...]]\n\n"
            "Three-tier classification:\n"
            "  1. Markers at root        → launcher game (HIGH confidence)\n"
            "  2. Root exe, no markers   → standalone (LOW confidence)\n"
            "  3. No markers, no root exe → container if child has markers,\n"
            "                              else Unknown / needs_review\n"
            "Only unknowns get a deep walk.  Case-insensitive matching.\n"
            "--steam-libraries excludes Steam roots.\n"
        )
        sys.exit(1 if args and "-h" not in args else 0)

    root = args[0]
    steam_libs: set[str] = set()
    try:
        idx = args.index("--steam-libraries")
        steam_libs = {args[i] for i in range(idx + 1, len(args)) if not args[i].startswith("-")}
    except ValueError:
        pass

    results = scan_standalone_directory(root, steam_library_paths=steam_libs)
    print(json.dumps(results, indent=2, default=str))


if __name__ == "__main__":
    main()
