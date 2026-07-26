#!/usr/bin/env python3
"""Unified game detection tool — fast scan + deep enrichment.

Single entry point for game folder detection.  Combines the fast-then-deep
architecture with all store signals, engine detection, and optional metadata
enrichment (PE metadata, PCGamingWiki).

Architecture
------------
Phase 1 — Root scan (fast, all folders):
  Scan root entries for store signals + exe.  Classify immediately.
  No deep walks, no stat calls.  ~3s on 120+ directories.

Phase 2 — Deep signal scan (unknowns only):
  Walk .exe/.dll/.ini files up to WALK_MAX_DEPTH.  Find store signals
  in subdirectories.  Extension-filtered to skip asset files.

Phase 3 — Container check (remaining unknowns):
  Check if child directories have launcher markers.  If yes, recurse
  into children as individual games.

Phase 4 — Enrichment (optional, --metadata / --pcgw flags):
  PE metadata extraction + PCGamingWiki lookup.  Only runs on folders
  that are still unknown after Phases 1-3.

Store detection priority (first match wins):
  1.  GOG              — goggame* files at root
  2.  EA               — __Installer/ directory
  3.  Ubisoft Emulator — uplay_loader* + .ini with Username=/AccountId=
  4.  Ubisoft          — uplay_install.manifest / uplay_r*_loader*.dll
  5.  Epic             — .egstore/ or .egsstore/ directory
  6.  Blizzard         — .battle.net/ directory
  7.  Xbox             — default-metadata.json
  8.  Rockstar         — title.rgl
  9.  Steam Emulator   — steam_api64.dll / steam_api.dll
 10.  (deep) Steam Emu — steam_emu.ini in child dirs / UE ThirdParty
 11.  (deep) Ubisoft   — UbiStats.dll in child dirs

Note: UE bundles Steamworks SDK in Engine/Binaries/ThirdParty/Steamworks/
by default.  That is NOT a valid Steam Emulator signal — only steam_emu.ini
and root-level steam_api64.dll (outside Steam library) indicate emulation.

Usage
-----
  python tools/detect.py /path/to/games
  python tools/detect.py /path/to/games --json
  python tools/detect.py /path/to/games --metadata     # PE metadata for unknowns
  python tools/detect.py /path/to/games --pcgw         # PCGamingWiki for unknowns
  python tools/detect.py /path/to/games --steam-libraries /path/to/steamapps
"""

from __future__ import annotations

import json
import os
import re
import sys
import time
import urllib.parse
import urllib.request
from pathlib import Path

# ── Optional dependencies ──────────────────────────────────────

try:
    import pefile  # type: ignore[import-untyped]
except ImportError:
    pefile = None

# ── Constants ──────────────────────────────────────────────────

WALK_MAX_DEPTH = 4

# Signal extensions — only these are processed during deep scans.
SIGNAL_EXTS = {".exe", ".dll", ".ini"}


# ── Detection Logger ───────────────────────────────────────────

class DetectionLogger:
    """Accumulates detailed detection log entries, writes to file on request."""

    def __init__(self) -> None:
        self._entries: list[str] = []
        self._current_folder: str = ""

    def folder(self, name: str, path: str) -> None:
        """Start a new folder section."""
        self._current_folder = name
        self._entries.append(f"\n{'='*70}")
        self._entries.append(f"FOLDER: {name}")
        self._entries.append(f"  PATH: {path}")

    def root_scan(self, *, root_exes: list[str], has_lnk: bool, store: str | None,
                  signal: str | None, root_exe: str | None) -> None:
        """Log Phase 1 root scan results."""
        self._entries.append(f"  [Phase 1] Root scan:")
        if root_exes:
            self._entries.append(f"    Root exes: {root_exes}")
            if len(root_exes) > 1:
                self._entries.append(f"    Best exe (scored): {root_exe}")
        else:
            self._entries.append(f"    Root exes: (none)")
        self._entries.append(f"    .lnk present: {has_lnk}")
        if store:
            self._entries.append(f"    Store signal: {store} ({signal})")
        else:
            self._entries.append(f"    Store signal: (none)")

    def lnk_parse(self, lnk_name: str, exe_name: str | None) -> None:
        """Log .lnk parsing result."""
        if exe_name:
            self._entries.append(f"    .lnk '{lnk_name}' → exe: {exe_name}")
        else:
            self._entries.append(f"    .lnk '{lnk_name}' → (no exe found)")

    def subdir_scan(self, exe_path: str) -> None:
        """Log that exe was found in subdirectory."""
        self._entries.append(f"    Subdir scan found: {exe_path}")

    def tier1_store(self, store: str, exe: str | None, engine: str) -> None:
        """Log Tier 1 classification."""
        self._entries.append(f"  → Tier 1 (Store): {store} | exe={exe or '(none)'} | engine={engine}")

    def tier2_standalone(self, exe: str | None, signal: str, engine: str) -> None:
        """Log Tier 2 classification."""
        self._entries.append(f"  → Tier 2 (Standalone): exe={exe or '(none)'} | signal={signal} | engine={engine}")

    def tier3_container(self, children: list[str]) -> None:
        """Log Tier 3 container detection."""
        self._entries.append(f"  → Tier 3 (Container): {len(children)} children")
        for c in children:
            self._entries.append(f"      - {c}")

    def tier4_unknown(self, exe: str | None, engine: str) -> None:
        """Log Tier 4 unknown."""
        self._entries.append(f"  → Tier 4 (Unknown): exe={exe or '(none)'} | engine={engine}")

    def deep_scan(self, signals: list[str], exes: list[str]) -> None:
        """Log Phase 2 deep scan results."""
        self._entries.append(f"  [Phase 2] Deep scan:")
        if signals:
            self._entries.append(f"    Signals found: {signals}")
        if exes:
            self._entries.append(f"    Exes found: {exes}")
        if not signals and not exes:
            self._entries.append(f"    (nothing found)")

    def skipped(self, reason: str) -> None:
        """Log why a folder was skipped."""
        self._entries.append(f"  → SKIPPED: {reason}")

    def note(self, text: str) -> None:
        """Add a general note."""
        self._entries.append(f"    NOTE: {text}")

    def write(self, path: str) -> None:
        """Write accumulated log to file."""
        with open(path, "w", encoding="utf-8") as f:
            f.write(f"Detection Log — {time.strftime('%Y-%m-%d %H:%M:%S')}\n")
            f.write(f"{'='*70}\n")
            f.write("\n".join(self._entries))
            f.write("\n")


# Global logger instance (used when --log is active)
_detlog = DetectionLogger()

# ── Skip lists ─────────────────────────────────────────────────

# Top-level directory names to skip entirely.
SKIP_NAMES: set[str] = {
    "_commonredist", "commonredist", "easyanticheat", "devtools",
    "support", "docs", "licenses", "vcredist", "directx",
    "steam controller configs", "steamworks shared",
    # Known launcher directories (not game containers)
    "epiclauncher", "launcher", "battle.net",
    "ubisoft game launcher", "origin", "ea desktop", "gog galaxy",
    # Known non-game tools
    "wiiu", "reshade", "sweetfx", "enbseries", "enb",
    "nexus mod manager", "vortex", "mod organizer",
    # Uninstall folders — never games
    "uninstall",
}

# Substring blacklist for executables (merged from both scripts).
# All matching is case-insensitive substring on the lowercase filename.
_NOISE_EXE_PARTS: tuple[str, ...] = (
    # Tier 1 — Universal noise
    "cleanup", "touchup", "installer", "unins", "uninstal", "unwise",
    "setup", "redist", "vcredist", "dxsetup", "oalinst", "dotnet",
    "directx", "physx", "msi", "msiexec", "xna", "ndp", "dotnetfx",
    # Tier 2 — Launcher stubs (penalized in scoring, NOT filtered as noise)
    # Note: "launcher" is intentionally NOT in this list — launchers are penalized
    # in scoring but not filtered, as some games use launchers as entry points.
    "updater", "patcher", "startup", "bootstrapper",
    # Tier 3 — Store bootstraps & integration stubs (exe names only, not game names)
    "galaxy", "epicgames", "uplay_loader", "ubisoft_game_launcher",
    # Tier 4 — Anti-cheat / DRM
    "easyanticheat", "battleye", "beclient", "beservice", "equ8",
    "punkbuster", "nprotect", "xigncode", "denuvo", "vmprotect",
    # Tier 5 — Crash reporting infrastructure (generic "crash" catches all variants)
    "crash", "bugsplat", "crs-",
    "unrealcefsubprocess", "symboldump", "ubiquitous",
    # Tier 6 — Error reporters (BlizzardError, CrypticError, etc.)
    "error",
    # Tier 7 — DRM wrappers & compatibility shims
    "xlive",
    # Tier 8 — Installer/patch utilities shipped alongside games
    "autorun", "7za", "xdelta", "delsaves", "asksavegames",
    # Tier 9 — Dedicated servers, loaders, stubs, updaters
    # Note: "server" is NOT a noise indicator — games ship server executables
    # (e.g. Minecraft_Server.exe). Only "dedicatedserver" is noise.
    "dedicatedserver", "stub", "update", "loader", "browser", "dowser",
    # Tier 10 — Media/movie players shipped alongside games
    "movie",
    # Tier 10 — Stardock distribution tools
    "sdcr", "tachyon",
    # Tier 11 — Dev/content editor tools
    "datacompiler", "editor", "modmanager", "packagemanager",
    "reminder", "contented", "leveled", "resourceed",
    # Tier 12 — Utilities & debug builds
    "install", "debug", "utils", "sndrpt", "exception", "explorer",
    "brwc", "activation", "ccmini", "acpc",
    # Tier 13 — Trial/stub/demo exes
    "trial", "_upp",
    # Tier 14 — Media/codec/streaming tools
    "ffmpeg", "ffplay", "ffprobe",
    # Tier 15 — Installer/update frameworks
    "squirrel", "wininst", "w9xpopen",
    # Tier 16 — Runtime interpreters
    "python", "blender",
    # Tier 17 — Web UI / overlay frameworks
    "coherentui", "cefhost", "awesomium", "webview", "overlay", "scummvm",
    # Tier 18 — Repair/service/helper processes
    "repair", "service", "helper",
    # Tier 19 — Unreal engine build tools
    "unrealpak",
    # Tier 20 — Patch/update executables
    "patch",
    # Tier 21 — Utility tools that ship alongside games
    "winscp", "activate",
    # Tier 22 — Driver/hardware utilities
    "kernelmodedriverloader", "driverloader",
    # Tier 23 — Intro video player (not the game itself)
    "intro",
)

# Substring blacklist for directory names (skip during exe scanning).
NOISE_DIR_PARTS: tuple[str, ...] = (
    "__redist", "_commonredist", "redist", "directx", "vcredist",
    "dotnet", "physx", "support", "_installer", "install", "installer",
)


# ── Noise helpers ──────────────────────────────────────────────

def _is_noise_exe(name: str) -> bool:
    """True for filenames that are clearly not game executables."""
    lower = name.lower()
    return any(part in lower for part in _NOISE_EXE_PARTS)


def _is_noise_dir(name: str) -> bool:
    """True for directories that contain only redist/installer payloads."""
    lower = name.lower()
    return any(part in lower for part in NOISE_DIR_PARTS)


def _parse_lnk_exe_name(lnk_path: Path) -> str | None:
    """Extract the .exe filename from a Windows .lnk shortcut file.
    The exe name appears as readable text in the .lnk binary data."""
    try:
        data = lnk_path.read_bytes()
    except OSError:
        return None
    # Find any .exe filename in the raw bytes
    # .lnk files store the exe name as a readable string
    text = data.decode("latin-1", errors="ignore")
    matches = re.findall(r'([A-Za-z0-9_\-\.]+\.exe)', text, re.IGNORECASE)
    if matches:
        # Return the most likely game exe (longest name, skip common DLLs)
        skip = {"steam_api.dll", "steam_api64.dll", "eos.dll", "upc.dll"}
        candidates = [m for m in matches if m.lower() not in skip]
        if candidates:
            return max(candidates, key=len)
    return None


def _find_exe_via_lnk(d: Path) -> str | None:
    """If root has a .lnk file, extract the exe name and search for it.
    Also handles backup renames (-Penumbra.exe, copy of Penumbra.exe, etc).
    Returns relative path from game dir, or None."""
    for entry in os.scandir(d):
        if entry.name.lower().endswith(".lnk"):
            exe_name = _parse_lnk_exe_name(Path(entry.path))
            if exe_name:
                exe_lower = exe_name.lower()
                exe_stem = exe_lower.rsplit(".", 1)[0]  # e.g. "penumbra"

                # Search ALL subdirectories for the specific exe (don't skip noise dirs
                # when we know exactly what we're looking for from the .lnk)
                # Use os.walk for up to 3 levels, collecting exact and fuzzy matches
                fuzzy_match = None
                for root_dir, dirs, files in os.walk(d):
                    # Limit depth
                    depth = len(Path(root_dir).relative_to(d).parts)
                    if depth > 3:
                        dirs.clear()
                        continue
                    for f in files:
                        if not f.lower().endswith(".exe"):
                            continue
                        fn_lower = f.lower()
                        if fn_lower == exe_lower:
                            # Exact match — return immediately
                            rel = os.path.relpath(os.path.join(root_dir, f), d)
                            return rel.replace("\\", "/")
                        if fuzzy_match is None:
                            # Check backup patterns: -Name.exe, copy of Name.exe
                            if fn_lower.startswith("-") and fn_lower[1:] == exe_lower:
                                fuzzy_match = os.path.relpath(os.path.join(root_dir, f), d)
                            elif fn_lower.startswith("copy of ") and fn_lower[8:] == exe_lower:
                                fuzzy_match = os.path.relpath(os.path.join(root_dir, f), d)
                            elif exe_stem in fn_lower and fn_lower.endswith(".exe"):
                                fuzzy_match = os.path.relpath(os.path.join(root_dir, f), d)
                if fuzzy_match:
                    return fuzzy_match.replace("\\", "/")
    return None


def _pick_best_root_exe(d: Path, exe_names: list[str]) -> str | None:
    """Score root-level exe names and return the best candidate.
    Lightweight scoring — no stat calls, just name analysis."""
    if not exe_names:
        return None
    if len(exe_names) == 1:
        return exe_names[0]

    folder_tokens = {
        part.lower()
        for part in d.name.replace("_", " ").replace("-", " ").split()
        if part
    }

    # Group heuristic: if most exes have "org" and one doesn't,
    # the clean one is almost certainly the game.
    org_count = sum(1 for n in exe_names if "org" in n.lower())
    has_clean_exe = org_count > 0 and org_count < len(exe_names)

    scored: list[tuple[int, str]] = []
    for name in exe_names:
        score = 0
        lower = name.lower()

        # Backup/copy penalties
        if "copy of" in lower or lower.startswith("copy of "):
            score -= 30
        if "_copy" in lower or lower.endswith(" copy") or " - copy" in lower:
            score -= 25
        # "org" as backup indicator — penalize heavily if there's a clean alternative
        if "org" in lower:
            penalty = -40 if has_clean_exe else -20
            score += penalty
        if "original" in lower:
            score -= 15
        # Crack/piracy indicators
        if "crack" in lower:
            score -= 25

        # Tool/utility penalties
        if "launcher" in lower:
            score -= 20
        _TOOL_NAMES = {
            "faces viewer", "ini editor", "luaedit", "map editor",
            "profile editor", "xml editor", "configtool", "config tool",
            "autorun", "setupanox", "dparse", "particleman",
            "videoconfig", "video config", "settings editor",
            "afscmd", "delsaves", "asksavegames",
            "config.exe", "drv_", "dxsetup",
        }
        if any(tool in lower for tool in _TOOL_NAMES):
            score -= 25
        if "unins" in lower or "uninstal" in lower:
            score -= 30

        # Folder name matching
        if any(token in lower for token in folder_tokens):
            score += 10
        # Exact match: exe stem equals a folder token (e.g. "heroes4" matches "heroes4")
        exe_stem = lower.replace(".exe", "")
        if exe_stem in folder_tokens:
            score += 15
        # Abbreviation: short exe stem that starts with a folder token's first letter
        # e.g. "ra3" ≈ "ra3", "g3" ≈ "g3"
        if len(exe_stem) <= 4:
            for token in folder_tokens:
                if exe_stem[0] == token[0] and len(exe_stem) <= len(token):
                    score += 8
                    break
        # Roman numeral matching: "u9" matches "ix" (9=IX), "g3" matches "iii" (3=III)
        # Also bidirectional: "heroes4" matches "iv" (4=IV)
        _ROMAN = {"i": "1", "ii": "2", "iii": "3", "iv": "4", "v": "5",
                  "vi": "6", "vii": "7", "viii": "8", "ix": "9", "x": "10"}
        _ROMAN_REV = {v: k for k, v in _ROMAN.items()}
        digits_in_stem = re.findall(r'\d+', exe_stem)
        roman_in_stem = re.findall(r'(x|ix|viii|vii|vi|iv|v|iii|ii|i)', exe_stem)
        for token in folder_tokens:
            # exe has digit, folder has roman: "u9" ≈ "ix"
            if token in _ROMAN and digits_in_stem and _ROMAN[token] in digits_in_stem:
                score += 12
                break
            # exe has roman, folder has digit: "heroes4" ≈ "iv"
            if token in _ROMAN_REV and roman_in_stem and token in roman_in_stem:
                score += 12
                break

        scored.append((score, name))

    scored.sort(key=lambda x: x[0], reverse=True)
    return scored[0][1]


# ══════════════════════════════════════════════════════════════
# Phase 1 — Root signal checks (fast, single directory)
# ══════════════════════════════════════════════════════════════

def _check_gog(d: Path) -> bool:
    """Any goggame* or gog_* file at root.  Uses targeted existence checks (fast)
    instead of glob (slow on large directories)."""
    if (d / "goggame.dll").exists():
        return True
    # Check for goggame-* or gog_* prefix — scan directory for matching names
    try:
        for entry in os.scandir(d):
            name_lower = entry.name.lower()
            if name_lower.startswith("goggame-") or name_lower.startswith("gog_"):
                return True
    except PermissionError:
        pass
    return False


def _check_ea(d: Path) -> bool:
    """__Installer/ directory at root."""
    return (d / "__Installer").is_dir()


def _check_ubisoft_emu(d: Path) -> bool:
    """Ubisoft emulator: uplay_loader* + .ini with Username= and AccountId=."""
    has_loader = False
    has_ini = False
    try:
        for entry in os.scandir(d):
            name_lower = entry.name.lower()
            if name_lower.startswith("uplay_loader") or (
                name_lower.startswith("uplay_r") and "loader" in name_lower
            ):
                has_loader = True
            if name_lower.endswith(".ini"):
                has_ini = True
    except PermissionError:
        return False

    if not has_loader or not has_ini:
        return False

    # Check .ini files for Ubisoft emulator config
    try:
        for entry in os.scandir(d):
            if entry.name.lower().endswith(".ini"):
                try:
                    text = Path(entry.path).read_text(encoding="utf-8", errors="ignore")
                except OSError:
                    continue
                if "Username=" in text and "AccountId=" in text:
                    return True
    except PermissionError:
        pass
    return False


def _check_ubisoft(d: Path) -> bool:
    """uplay_install.manifest or uplay_r*_loader*.dll at root."""
    if (d / "uplay_install.manifest").exists():
        return True
    try:
        for entry in os.scandir(d):
            name_lower = entry.name.lower()
            if name_lower.startswith("uplay_r") and "loader" in name_lower and name_lower.endswith(".dll"):
                return True
    except PermissionError:
        pass
    return False


def _check_epic(d: Path) -> bool:
    """.egstore/ or .egsstore/ directory at root."""
    return (d / ".egstore").is_dir() or (d / ".egsstore").is_dir()


def _check_blizzard(d: Path) -> bool:
    """.battle.net/ directory at root."""
    return (d / ".battle.net").is_dir()


def _check_xbox(d: Path) -> bool:
    """default-metadata.json at root (Xbox Game Pass / MS Store)."""
    return (d / "default-metadata.json").exists()


def _check_rockstar(d: Path) -> bool:
    """title.rgl at root (Rockstar Games Launcher)."""
    return (d / "title.rgl").exists()


def _check_steam_emu(d: Path) -> bool:
    """steam_api64.dll or steam_api.dll at root."""
    return (d / "steam_api64.dll").exists() or (d / "steam_api.dll").exists()


# Ordered list: (store_name, signal_label, check_fn)
_ROOT_SIGNAL_CHECKS = [
    ("GOG",              "goggame",          _check_gog),
    ("EA",               "ea_installer",     _check_ea),
    ("Ubisoft Emulator", "uplay_emu",        _check_ubisoft_emu),
    ("Ubisoft",          "uplay",            _check_ubisoft),
    ("Epic",             "egstore",          _check_epic),
    ("Blizzard",         "battle_net",       _check_blizzard),
    ("Xbox",             "default_metadata", _check_xbox),
    ("Rockstar",         "rgl",              _check_rockstar),
    ("Steam Emulator",   "steam_api",        _check_steam_emu),
]


def _scan_root(path: Path):
    """
    One-level scan of *path*.  Returns:
      (store, signal, has_root_exe, root_exe_name, has_root_lnk, child_dirs)
    Single os.scandir() call.  All signal checks run against collected entries.
    """
    try:
        entries = list(os.scandir(path))
    except PermissionError:
        return None, None, False, None, False, []

    names: set[str] = set()
    names_lower: set[str] = set()
    child_dirs = []
    root_exes: list[str] = []  # ALL non-noise exes (for scoring)
    has_root_lnk = False

    for e in entries:
        name_lower = e.name.lower()
        names_lower.add(name_lower)
        if e.is_dir(follow_symlinks=False):
            child_dirs.append(e)
        elif name_lower.endswith(".exe"):
            # Track ALL non-noise exes for scoring
            if not _is_noise_exe(e.name):
                root_exes.append(e.name)
        elif name_lower.endswith(".lnk"):
            has_root_lnk = True

    has_root_exe = len(root_exes) > 0
    # Pick best root exe via scoring (if multiple candidates)
    root_exe = _pick_best_root_exe(path, root_exes) if root_exes else None

    # ── Check all store signals from collected names (single pass) ──

    # GOG: goggame.dll, goggame-*/gog_* prefix, or gog.ico
    if ("goggame.dll" in names_lower
            or any(n.startswith("goggame-") or n.startswith("gog_") for n in names_lower)
            or "gog.ico" in names_lower):
        return "GOG", "goggame", has_root_exe, root_exe, has_root_lnk, child_dirs

    # EA: __installer dir, touchup.exe, or ActivationUI.exe
    if ("__installer" in names_lower
            or "touchup.exe" in names_lower
            or "activationui.exe" in names_lower):
        return "EA", "ea_installer", has_root_exe, root_exe, has_root_lnk, child_dirs

    # Ubisoft Emulator: uplay_loader* or uplay_r*_loader* + .ini with config
    ubi_loader = any(
        n.startswith("uplay_loader") or (n.startswith("uplay_r") and "loader" in n)
        for n in names_lower
    )
    if ubi_loader:
        # Check .ini files for emulator config (only if loader found)
        has_ini = any(n.endswith(".ini") for n in names_lower)
        if has_ini:
            for e in entries:
                if e.name.lower().endswith(".ini"):
                    try:
                        text = Path(e.path).read_text(encoding="utf-8", errors="ignore")
                    except OSError:
                        continue
                    if "Username=" in text and "AccountId=" in text:
                        return "Ubisoft Emulator", "uplay_emu", has_root_exe, root_exe, has_root_lnk, child_dirs

    # Ubisoft: uplay_install.manifest or uplay_r*_loader*.dll
    if "uplay_install.manifest" in names_lower:
        return "Ubisoft", "uplay", has_root_exe, root_exe, has_root_lnk, child_dirs
    if any(n.startswith("uplay_r") and "loader" in n and n.endswith(".dll") for n in names_lower):
        return "Ubisoft", "uplay", has_root_exe, root_exe, has_root_lnk, child_dirs

    # Epic: .egstore / .egsstore dir
    if ".egstore" in names_lower or ".egsstore" in names_lower:
        return "Epic", "egstore", has_root_exe, root_exe, has_root_lnk, child_dirs

    # Blizzard: .battle.net dir
    if ".battle.net" in names_lower:
        return "Blizzard", "battle_net", has_root_exe, root_exe, has_root_lnk, child_dirs

    # Xbox: default-metadata.json
    if "default-metadata.json" in names_lower:
        return "Xbox", "default_metadata", has_root_exe, root_exe, has_root_lnk, child_dirs

    # Rockstar: title.rgl
    if "title.rgl" in names_lower:
        return "Rockstar", "rgl", has_root_exe, root_exe, has_root_lnk, child_dirs

    # Steam Emulator: steam_api64.dll / steam_api.dll
    if "steam_api64.dll" in names_lower or "steam_api.dll" in names_lower:
        return "Steam Emulator", "steam_api", has_root_exe, root_exe, has_root_lnk, child_dirs

    # No store signal found
    return None, None, has_root_exe, root_exe, has_root_lnk, child_dirs


# ══════════════════════════════════════════════════════════════
# Phase 2 — Deep signal scan (unknowns only, extension-filtered)
# ══════════════════════════════════════════════════════════════

def _has_steam_emu_ini(d: Path) -> bool:
    """steam_emu.ini at root, in child dirs, or in UE ThirdParty/Steamworks."""
    if (d / "steam_emu.ini").exists():
        return True
    for child in d.iterdir():
        if child.is_dir() and (child / "steam_emu.ini").exists():
            return True
    # UE pattern: Engine/Binaries/ThirdParty/Steamworks/Steamv*/Win64/
    sw = d / "Engine" / "Binaries" / "ThirdParty" / "Steamworks"
    if sw.is_dir():
        for sv in sw.iterdir():
            if sv.is_dir() and (sv / "Win64" / "steam_emu.ini").exists():
                return True
    return False


def _has_steam_app_manifest(d: Path) -> bool:
    """steamapps/ or .acf files at root indicate a Steam Emulated game.

    Real Steam libraries have fixed, known paths (set via --steam-libraries).
    Finding steamapps/ or .acf manifests OUTSIDE those paths means someone
    copied the Steam structure — this is a Steam Emulated (pirated/cracked) game."""
    if (d / "steamapps").is_dir():
        return True
    try:
        for entry in os.scandir(d):
            if entry.name.lower().endswith(".acf"):
                return True
    except PermissionError:
        pass
    return False


def _has_ubisoft_legacy(d: Path) -> bool:
    """UbiStats.dll at root or in immediate child dirs."""
    if (d / "UbiStats.dll").exists():
        return True
    for child in d.iterdir():
        if child.is_dir() and (child / "UbiStats.dll").exists():
            return True
    return False


# Deep signal checks — run only on Phase 2 unknowns.
_DEEP_SIGNAL_CHECKS = [
    ("Steam Emulator", "emu_ini",        _has_steam_emu_ini),
    ("Steam Emulator", "steam_app_manifest", _has_steam_app_manifest),
    ("Ubisoft",        "ubistats",       _has_ubisoft_legacy),
]


def _deep_signal_scan(game_dir: Path):
    """
    Walk to WALK_MAX_DEPTH.  Only processes .exe, .dll, .ini files.
    Collects store signals + exe names.  No stat calls.
    Returns (store, signal, exe_names).
    """
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
            name_lower = e.name.lower()
            if not any(name_lower.endswith(ext) for ext in SIGNAL_EXTS):
                continue
            all_names.add(name_lower)
            if name_lower.endswith(".exe") and not _is_noise_exe(e.name):
                exe_names.append(e.name)

        if depth < WALK_MAX_DEPTH:
            for d in subdirs:
                all_dirs.add(d.name.lower())
                stack.append((d, depth + 1))

    # Check deep signal patterns
    for store, signal, check_fn in _DEEP_SIGNAL_CHECKS:
        if check_fn(game_dir):
            return store, signal, exe_names

    # Check collected names against marker patterns
    store, markers = _match_markers(all_names, all_dirs)
    if markers:
        return store, markers[0], exe_names

    return None, None, exe_names


def _match_markers(names_lower: set[str], dirs_lower: set[str]):
    """Match collected names against known marker patterns.
    Returns (store_name, matched_markers) or (None, [])."""
    # GOG: goggame- prefix, gog_* prefix, or goggame.dll exact
    gog_files = [n for n in names_lower if n.startswith("goggame-") or n.startswith("gog_")]
    if gog_files:
        return "GOG", gog_files
    if "goggame.dll" in names_lower:
        return "GOG", ["goggame.dll"]

    # EA: __installer dir
    if "__installer" in dirs_lower:
        return "EA", ["__installer"]

    # Ubisoft: uplay files
    ubi_files = [n for n in names_lower if "uplay" in n and ("loader" in n or "manifest" in n or "state" in n)]
    if ubi_files:
        return "Ubisoft", ubi_files

    # Epic: .egstore / .egsstore
    epic_dirs = [d for d in dirs_lower if d in (".egstore", ".egsstore")]
    if epic_dirs:
        return "Epic", epic_dirs

    # Steam Emulator: steam_api*.dll
    steam_files = [n for n in names_lower if n in ("steam_api64.dll", "steam_api.dll")]
    if steam_files:
        return "Steam Emulator", steam_files

    return None, []


# ══════════════════════════════════════════════════════════════
# Engine detection (fast, root-level probes)
# ══════════════════════════════════════════════════════════════

def _detect_engine(d: Path) -> str:
    """Detect game engine from local file signals.  Returns engine name or 'Unknown'."""
    if _has_unreal_engine(d):
        return "Unreal Engine"
    if _has_unity(d):
        return "Unity"
    if _has_rage(d):
        return "RAGE"
    if _has_frostbite(d):
        return "Frostbite"
    return "Unknown"


def _has_unreal_engine(d: Path) -> bool:
    if not (d / "Engine").is_dir():
        return False
    if (d / "Engine" / "Binaries").is_dir():
        return True
    for child in d.iterdir():
        if child.is_dir() and (child / "Binaries" / "Win64").is_dir():
            return True
    return False


def _has_unity(d: Path) -> bool:
    if not (d / "UnityPlayer.dll").exists():
        return False
    return any(child.is_dir() and child.name.endswith("_Data") for child in d.iterdir())


def _has_rage(d: Path) -> bool:
    return (d / "title.rgl").exists() and (d / "common.rpf").exists()


def _has_frostbite(d: Path) -> bool:
    return (d / "Engine.BuildInfo_Win64_retail.dll").exists()


# ══════════════════════════════════════════════════════════════
# GOG metadata extraction
# ══════════════════════════════════════════════════════════════

def _extract_gog_metadata(d: Path) -> dict | None:
    """Parse goggame-*.info files.  Prefer main game (gameId == rootGameId).
    Searches root and one level of subdirectories."""
    best_name = None
    best_game_id = None
    best_exe = None
    best_args = None

    # Search root and one level of subdirs for .info files
    search_dirs = [d]
    try:
        for entry in os.scandir(d):
            if entry.is_dir() and not _is_noise_dir(entry.name):
                search_dirs.append(Path(entry.path))
    except PermissionError:
        pass

    for search_dir in search_dirs:
        for info_file in search_dir.glob("goggame-*.info"):
            try:
                data = json.loads(info_file.read_text(encoding="utf-8", errors="ignore"))
            except Exception:
                continue
            is_main = data.get("gameId") == data.get("rootGameId")
            game_name = data.get("name", "")
            game_id = data.get("gameId", "")
            if is_main:
                best_name = game_name
                best_game_id = game_id
            elif best_name is None:
                best_name = game_name
                best_game_id = game_id
            # Extract primary exe from playTasks
            for task in data.get("playTasks", []):
                if task.get("isPrimary") and task.get("path"):
                    best_exe = task["path"]
                    best_args = task.get("arguments")

    if best_name or best_game_id:
        result: dict = {
            "title": best_name or "",
            "game_id": best_game_id or "",
        }
        if best_exe:
            result["exe"] = best_exe
        if best_args:
            result["launch_args"] = best_args
        return result
    return None


# ══════════════════════════════════════════════════════════════
# Phase 4 — Enrichment (optional, only for unknowns)
# ══════════════════════════════════════════════════════════════

def _find_game_executables(d: Path) -> tuple[list[Path], list[Path]]:
    """Find likely game executables from common layouts.
    Checks: root, 1-level children, common subpaths (Binaries, bin),
    and 2-level deep if root has no exes.
    Returns (exe_candidates, bat_launchers)."""
    candidates: list[Path] = []
    bat_launchers: list[Path] = []
    seen: set[str] = set()

    def _add_exes(folder: Path):
        if not folder.is_dir():
            return
        for item in folder.iterdir():
            if not item.is_file():
                continue
            name_lower = item.name.lower()
            key = str(item)
            if key in seen:
                continue
            if name_lower.endswith(".exe") and not _is_noise_exe(item.name):
                seen.add(key)
                candidates.append(item)
            elif name_lower.endswith(".bat") and not _is_noise_exe(item.name):
                seen.add(key)
                bat_launchers.append(item)

    def _add_exes_recursive(folder: Path, max_depth: int, depth: int = 0):
        """Walk subdirectories up to max_depth, only processing exe files."""
        if depth > max_depth or not folder.is_dir():
            return
        try:
            for item in folder.iterdir():
                if item.is_file() and item.suffix.lower() == ".exe":
                    if not _is_noise_exe(item.name):
                        key = str(item)
                        if key not in seen:
                            seen.add(key)
                            candidates.append(item)
                elif item.is_dir() and not _is_noise_dir(item.name):
                    _add_exes_recursive(item, max_depth, depth + 1)
        except PermissionError:
            pass

    # Level 0: root
    _add_exes(d)

    # Level 1: children + common subpaths
    for child in d.iterdir():
        if not child.is_dir() or _is_noise_dir(child.name):
            continue
        _add_exes(child)
        # UE standard layout: Child/Binaries/Win64/
        for subdir_name in ("Binaries/Win64", "Binaries/WinGDK"):
            b64 = child / subdir_name
            if b64.is_dir() and not _is_noise_dir(b64.parent.name):
                _add_exes(b64)
        # Common layout: Child/bin/ (older games like Gothic, Jagged Alliance)
        bin_dir = child / "bin"
        if bin_dir.is_dir():
            _add_exes(bin_dir)

    # If root has no exes, walk 2 levels deep (BioShock case: root → Binaries/Win64/)
    root_has_exes = any(c.is_file() and c.suffix.lower() == ".exe" for c in d.iterdir() if c.is_file())
    if not root_has_exes and not candidates:
        _add_exes_recursive(d, max_depth=2)

    return candidates, bat_launchers


def _pick_primary_executable(d: Path) -> tuple[str | None, dict, list[str]]:
    """Score executables and pick the best candidate.
    Returns (relative_path, pe_metadata_dict, bat_launcher_paths)."""
    exes, bat_launchers = _find_game_executables(d)
    if not exes:
        return None, {}, [str(b.relative_to(d)) for b in bat_launchers]

    folder_tokens = {
        part.lower()
        for part in d.name.replace("_", " ").replace("-", " ").split()
        if part
    }
    folder_name_lower = d.name.lower()

    # Pre-extract PE metadata for top candidates (if pefile available)
    pe_cache: dict[Path, dict] = {}
    if pefile is not None:
        for exe in exes[:5]:
            pe_cache[exe] = _read_pe_metadata(exe)

    scored: list[tuple[int, Path]] = []
    for exe in exes:
        score = 0
        lower = exe.name.lower()

        # ── Backup/copy penalties (highest priority — never pick these) ──
        if "copy of" in lower or lower.startswith("copy of "):
            score -= 30
        if "_copy" in lower or lower.endswith(" copy") or " - copy" in lower:
            score -= 25
        # "org_" prefix = original/backup copy (e.g. "133_org_div.exe")
        if "_org_" in lower or lower.startswith("org_") or lower.endswith("_org"):
            score -= 20
        # Numbered backup prefix: "22_org_", "1_12_org_", "133_org_"
        if any(lower.startswith(f"{i}_org_") or lower.startswith(f"{i}_") and "_org_" in lower
               for i in range(100)):
            score -= 15
        # "original" in name
        if "original" in lower:
            score -= 15

        # ── Tool/utility penalties ──
        if "launcher" in lower:
            score -= 20
        _TOOL_NAMES = {
            "faces viewer", "ini editor", "luaedit", "map editor",
            "profile editor", "xml editor", "configtool", "config tool",
            "autorun", "setupanox", "dparse", "particleman",
        }
        if any(tool in lower for tool in _TOOL_NAMES):
            score -= 25
        # Uninstaller penalty
        if "unins" in lower or "uninstal" in lower:
            score -= 30

        # ── Small exe penalty (< 100KB is almost never the game) ──
        try:
            size = exe.stat().st_size
            if size < 100_000:  # < 100KB
                score -= 15
            elif size < 500_000:  # < 500KB
                score -= 5
            score += min(size // 10_000_000, 10)
        except OSError:
            pass

        # ── Folder name matching (strongest signal) ──
        # Exact match: folder "g3" → exe "Gothic3.exe" (if tokens overlap)
        # Partial match: folder "Divine Divinity" → exe "div.exe" (token prefix)
        if any(token in lower for token in folder_tokens):
            score += 10
        # Stronger: exe stem starts with folder token (e.g. "div" matches "divine")
        for token in folder_tokens:
            if lower.startswith(token) or token.startswith(lower.replace(".exe", "")):
                score += 5
                break

        # ── UE standard path bonus ──
        if "shipping" in lower or "win64" in lower:
            score += 5

        scored.append((score, exe))

    scored.sort(key=lambda x: x[0], reverse=True)
    best_score, best = scored[0]
    best_metadata: dict = pe_cache.get(best, {})

    # ── PE metadata matching (strongest tiebreaker) ──
    # If FileDescription or ProductName matches folder name, boost score
    for score, exe in scored[:3]:
        metadata = pe_cache.get(exe, {})
        desc = metadata.get("FileDescription", "").lower()
        product = metadata.get("ProductName", "").lower()

        # PE description matches folder name → very strong signal
        if desc and any(token in desc for token in folder_tokens):
            score += 15
        if product and any(token in product for token in folder_tokens):
            score += 10

        # Update best if this exe scores higher
        if score > best_score:
            best_score = score
            best = exe
            best_metadata = metadata
        elif exe == best and metadata:
            best_metadata = metadata

    return str(best.relative_to(d)), best_metadata, [str(b.relative_to(d)) for b in bat_launchers]


def _read_pe_metadata(exe: Path) -> dict:
    """Parse PE version info from an executable."""
    if pefile is None:
        return {}
    try:
        pe = pefile.PE(str(exe), fast_load=False)
    except Exception:
        return {}
    metadata: dict = {}
    try:
        for file_info in getattr(pe, "FileInfo", []) or []:
            for table in getattr(file_info, "StringTable", []) or []:
                for raw_key, raw_value in table.entries.items():
                    key = raw_key.decode("utf-8", errors="ignore") if isinstance(raw_key, bytes) else str(raw_key)
                    value = raw_value.decode("utf-8", errors="ignore") if isinstance(raw_value, bytes) else str(raw_value)
                    if key in ("FileDescription", "ProductName", "OriginalFilename", "CompanyName") and value:
                        metadata[key] = value
    except Exception:
        pass
    return metadata


def _build_name_candidates(folder_label: str, folder_path: Path, entry: dict) -> list[str]:
    """Build candidate game names from folder name, PE metadata, and exe names."""
    candidates: list[str] = []
    if entry.get("name"):
        candidates.append(entry["name"])
    candidates.append(Path(folder_label).name)
    # Light exe name scan (max 5 stems)
    for child in folder_path.iterdir():
        if child.is_file() and child.suffix.lower() == ".exe" and not _is_noise_exe(child.name):
            candidates.append(child.stem)
            if len(candidates) >= 7:
                break
    # Normalize and deduplicate
    cleaned: list[str] = []
    seen: set[str] = set()
    for c in candidates:
        value = c.replace("_", " ").replace("-", " ").strip()
        if value and value.lower() not in seen:
            seen.add(value.lower())
            cleaned.append(value)
    return cleaned


def _pcgw_lookup(name: str) -> dict | None:
    """Query PCGamingWiki OpenSearch for a game name."""
    if not name:
        return None
    url = "https://www.pcgamingwiki.com/w/api.php?" + urllib.parse.urlencode({
        "action": "opensearch",
        "search": name,
        "limit": "1",
        "namespace": "0",
        "format": "json",
    })
    req = urllib.request.Request(url, headers={
        "User-Agent": "GamingCommander/0.1 (research tool)",
    })
    try:
        with urllib.request.urlopen(req, timeout=5) as response:
            data = json.loads(response.read().decode("utf-8"))
    except Exception:
        return None
    if len(data) >= 4 and data[1]:
        return {"query": name, "title": data[1][0], "url": data[3][0] if data[3] else ""}
    return None


# ══════════════════════════════════════════════════════════════
# Result building
# ══════════════════════════════════════════════════════════════

def _build_result(
    game_dir: Path,
    store: str | None,
    signal: str | None,
    exe_names: list[str],
    *,
    folder: str | None = None,
    container: str | None = None,
    engine: str = "Unknown",
    needs_review: bool = False,
    gog_metadata: dict | None = None,
    pe_metadata: dict | None = None,
    pcgw: dict | None = None,
    name_candidates: list[str] | None = None,
) -> dict:
    """Build a standardized result dict."""
    result: dict = {
        "folder": folder or game_dir.name,
        "path": str(game_dir),
        "store": store or "Unknown",
        "signal": signal,
        "engine": engine,
        "confidence": "High" if store and store != "Unknown" else "Low",
        "exe_count": len(exe_names),
        "exes": [{"path": name, "name": Path(name).name} for name in exe_names],
    }
    if container:
        result["container"] = container
    if needs_review:
        result["needs_review"] = True
    if gog_metadata:
        result["gog_metadata"] = gog_metadata
    if pe_metadata:
        result["pe_metadata"] = pe_metadata
    if pcgw:
        result["pcgw"] = pcgw
    if name_candidates:
        result["name_candidates"] = name_candidates
    return result


# ══════════════════════════════════════════════════════════════
# Non-game folder detection
# ══════════════════════════════════════════════════════════════

# Directory names that are clearly not games
_NON_GAME_DIR_NAMES: set[str] = {
    "dlc", "program files", "windowsapps", "squirreltemp",
    "epiclauncher", "nexus mod manager",
    "soundtrack", "soundtracks", "original soundtrack",
    "manuals",
    # Known non-game applications
    "wiiu",  # Wii U USB Helper (emulator tool, not a game)
    "portable",  # Portable app bundles — driver loaders, tools, not games
    # Mod injection libraries
    "reshade", "sweetfx", "enbseries", "enb",
    # Mod managers
    "nexus mod manager", "vortex", "mod organizer",
    # Redistributables
    "dotnet35", "dotnetfx35", "msvc2012", "msvc2012_x64",
    "msvc2013", "msvc2013_x64", "vcredist", "dotnet",
    # Uninstall folders (contain uninstaller exes, not games)
    "uninstall",
}

# Subdirectory names that indicate non-game content
_NON_GAME_SUBDIR_NAMES: set[str] = {
    "original soundtrack", "soundtrack", "manuals", "soundtracks",
    "mods", "moddingandgui",
    # Mod injection libraries (when found inside game folders)
    "reshade", "sweetfx", "enbseries", "enb",
    # Data-only folders (save games, configs, mod data)
    "saved games", "savegames", "save",
    "item data", "misc", "vo_soundsets", "vo_en", "depot",
    "_gamedata",
    # Texture/model/asset folders
    "textures", "models", "assets", "resources",
    "data", "xml",
}

# File extensions that indicate non-game content (at root, no exes)
_NON_GAME_FILE_EXTS: set[str] = {".mp3", ".flac", ".ogg", ".wav", ".m4a"}

# File extensions that are support/data files (not game data, not meaningful)
_SUPPORT_FILE_EXTS: set[str] = {
    ".dll", ".ini", ".cfg", ".txt", ".pdf", ".md",
    ".doc", ".docx", ".log", ".json", ".yml", ".yaml",
    ".bat", ".sh", ".jar", ".dat", ".tmp", ".dmp",
    ".converted", ".xml", ".dat_old",
}


def _is_non_game_folder(d: Path, child_dirs) -> bool:
    """Quick check: is this folder clearly not a game?

    Returns True for:
      - Redistributable directories (dotNet, MSVC, vcredist, etc.)
      - Documentation/music folders
      - Mod managers
      - System directories
      - Folders with only non-game file types

    Does NOT return True for ambiguous cases — those stay as needs_review.
    """
    name_lower = d.name.lower()

    # Exact name matches
    if name_lower in _NON_GAME_DIR_NAMES:
        return True

    # Redistributable / installer directories
    redist_names = {
        "dotnet35", "dotnetfx35", "msvc2012", "msvc2012_x64",
        "msvc2013", "msvc2013_x64", "vcredist", "dotnet",
    }
    if name_lower in redist_names:
        return True

    # Check children — if ALL children are non-game subdirs, skip
    if child_dirs:
        all_non_game = all(
            c.name.lower() in _NON_GAME_SUBDIR_NAMES
            or c.name.lower() in _NON_GAME_DIR_NAMES
            or c.name.lower() in redist_names
            for c in child_dirs
        )
        if all_non_game and len(child_dirs) > 0:
            return True

    # Folder with only non-game file types (music, docs, DLLs, data) and no real exes
    try:
        has_non_noise_exe = False
        has_meaningful_file = False
        file_count = 0
        for fe in os.scandir(d):
            if fe.is_file():
                file_count += 1
                name_lower = fe.name.lower()
                ext = name_lower.rsplit(".", 1)[-1] if "." in name_lower else ""
                if ext == "exe":
                    if not _is_noise_exe(fe.name):
                        has_non_noise_exe = True
                elif f".{ext}" in _NON_GAME_FILE_EXTS:
                    pass  # music files — non-game
                elif f".{ext}" in _SUPPORT_FILE_EXTS:
                    pass  # support/data files — neutral
                else:
                    has_meaningful_file = True  # unknown ext — might be game data
        # No non-noise exes and no meaningful files → not a game
        if not has_non_noise_exe and not has_meaningful_file:
            return True
    except PermissionError:
        pass

    return False


# ══════════════════════════════════════════════════════════════
# Subdirectory exe scan (reusable)
# ══════════════════════════════════════════════════════════════

def _find_exe_in_subdirs(child: Path, child_dirs: list) -> list[str]:
    """Scan child directories for game exes. Used when root has no exe
    or only a launcher. Returns list of relative exe paths."""
    exe_list: list[str] = []
    dir_names = {c.name.lower() for c in child_dirs}

    # Fast path: UE4-5 structure — Engine/ + GameName/Binaries/Win64/
    if "engine" in dir_names:
        for c in child_dirs:
            if c.name.lower() == "engine":
                continue
            bin_dir = Path(c.path) / "Binaries"
            if bin_dir.is_dir():
                for platform in ("Win64", "Win32", "Steam", "Linux"):
                    plat_dir = bin_dir / platform
                    if plat_dir.is_dir():
                        try:
                            for se in os.scandir(plat_dir):
                                if (se.is_file()
                                        and se.name.lower().endswith(".exe")
                                        and not _is_noise_exe(se.name)):
                                    exe_list.append(f"{c.name}/Binaries/{platform}/{se.name}")
                        except PermissionError:
                            pass
                    if exe_list:
                        break
            if exe_list:
                break

    # Fast path: UE3 structure — root Binaries/Win32/
    if not exe_list and "binaries" in dir_names:
        bin_dir = child / "Binaries"
        for platform in ("Win64", "Win32", "Steam", "Linux"):
            plat_dir = bin_dir / platform
            if plat_dir.is_dir():
                try:
                    for se in os.scandir(plat_dir):
                        if (se.is_file()
                                and se.name.lower().endswith(".exe")
                                and not _is_noise_exe(se.name)):
                            exe_list.append(f"Binaries/{platform}/{se.name}")
                except PermissionError:
                    pass
            if exe_list:
                break

    # Generic fallback: scan child dirs up to 3 levels
    if not exe_list:
        for c in child_dirs:
            c_path = Path(c.path)
            if _is_noise_dir(c.name):
                continue
            try:
                for se in os.scandir(c_path):
                    if (se.is_file()
                            and se.name.lower().endswith(".exe")
                            and not _is_noise_exe(se.name)):
                        exe_list.append(f"{c.name}/{se.name}")
                        break
                    if se.is_dir() and not _is_noise_dir(se.name):
                        try:
                            for sse in os.scandir(se.path):
                                if (sse.is_file()
                                        and sse.name.lower().endswith(".exe")
                                        and not _is_noise_exe(sse.name)):
                                    exe_list.append(f"{c.name}/{se.name}/{sse.name}")
                                    break
                                if sse.is_dir() and not _is_noise_dir(sse.name):
                                    try:
                                        for ssse in os.scandir(sse.path):
                                            if (ssse.is_file()
                                                    and ssse.name.lower().endswith(".exe")
                                                    and not _is_noise_exe(ssse.name)):
                                                exe_list.append(f"{c.name}/{se.name}/{sse.name}/{ssse.name}")
                                                break
                                    except PermissionError:
                                        pass
                        except PermissionError:
                            pass
            except PermissionError:
                pass
            if exe_list:
                break

    return exe_list


# ══════════════════════════════════════════════════════════════
# Top-level scan
# ══════════════════════════════════════════════════════════════

def scan_directory(
    root: str | Path,
    *,
    steam_library_paths: set[str] | None = None,
    extract_metadata: bool = False,
    verify_pcgw: bool = False,
    log_path: str | None = None,
) -> list[dict]:
    """
    Scan *root* for games.  Unified detection pipeline.

    Phases 1-3 always run (fast).  Phase 4 enrichment runs only when
    flags are enabled and only on folders still unknown after Phase 3.
    """
    root_path = Path(root)
    if not root_path.is_dir():
        return []

    # Activate logger if log path given
    if log_path:
        _detlog._entries.clear()
        _detlog._entries.append(f"Scan root: {root}")
        _detlog._entries.append(f"Steam libraries: {steam_library_paths or '(none)'}")
    else:
        _detlog._entries.clear()

    # Build exclusion set from Steam library paths
    excluded: set[str] = set()
    if steam_library_paths:
        for p in steam_library_paths:
            excluded.add(Path(p).as_posix().lower())

    def _is_excluded(path: Path) -> bool:
        return any(path.as_posix().lower().startswith(e) for e in excluded)

    # Check if root is itself a Steam library or Epic launcher
    if (root_path / "steamapps").is_dir():
        return []
    if list(root_path.glob("Manifests/*.item")):
        return []

    games: list[dict] = []

    def _scan(parent: Path, prefix: str = "", container: bool = False):
        try:
            entries = sorted(os.scandir(parent), key=lambda e: e.name)
        except PermissionError:
            return

        for entry in entries:
            if not entry.is_dir(follow_symlinks=False):
                continue
            if entry.name.lower() in SKIP_NAMES:
                if log_path:
                    _detlog.folder(f"{prefix}{entry.name}", entry.path)
                    _detlog.skipped(f"In SKIP_NAMES ({entry.name.lower()})")
                continue

            child = Path(entry.path)
            if _is_excluded(child):
                if log_path:
                    _detlog.folder(f"{prefix}{entry.name}", entry.path)
                    _detlog.skipped("Excluded Steam library path")
                continue

            folder_label = f"{prefix}{entry.name}"

            if log_path:
                _detlog.folder(folder_label, entry.path)

            # ── Phase 1: Root scan ──
            store, signal, has_root_exe, root_exe, has_root_lnk, child_dirs = _scan_root(child)

            if log_path:
                _detlog.root_scan(
                    root_exes=[e.name for e in child_dirs if False],  # placeholder
                    has_lnk=has_root_lnk, store=store, signal=signal, root_exe=root_exe,
                )
                # Log actual root exes found
                root_exes_list = []
                try:
                    for se in os.scandir(child):
                        if se.is_file() and se.name.lower().endswith(".exe") and not _is_noise_exe(se.name):
                            root_exes_list.append(se.name)
                except PermissionError:
                    pass
                if root_exes_list:
                    _detlog.note(f"Actual root exes: {root_exes_list}")
                if has_root_lnk:
                    for se in os.scandir(child):
                        if se.is_file() and se.name.lower().endswith(".lnk"):
                            exe_name = _parse_lnk_exe_name(Path(se.path))
                            _detlog.lnk_parse(se.name, exe_name)

            container_label = prefix.rstrip("/") if prefix else None

            # When inside a detected container, only promote children with
            # exes or store signals.  Data-only subfolders (Item Data, Misc,
            # vo_soundsets, etc.) are not separate games.
            # Also skip folders whose name is in the non-game list.
            if container and store is None:
                name_lower_check = entry.name.lower()
                if name_lower_check in _NON_GAME_DIR_NAMES:
                    if log_path:
                        _detlog.skipped(f"Container non-game dir name ({name_lower_check})")
                    continue  # Known non-game folder name — skip
                if not has_root_exe and not has_root_lnk:
                    # Check if any child has a store signal or exe
                    has_store_child = False
                    for c in child_dirs:
                        c_path = Path(c.path)
                        c_store, _, c_has_exe, _, _, _ = _scan_root(c_path)
                        if c_store is not None or c_has_exe:
                            has_store_child = True
                            break
                    if not has_store_child:
                        if log_path:
                            _detlog.skipped("Container data-only subfolder (no exe, no store child)")
                        continue  # Data-only subfolder inside container — skip

            # Tier 1: Store signal found → classify immediately
            if store is not None:
                exe_list = [root_exe] if root_exe else []
                # No root exe? Try .lnk shortcut target
                if not exe_list and has_root_lnk:
                    lnk_target = _find_exe_via_lnk(child)
                    if lnk_target:
                        exe_list = [lnk_target]
                # Still no exe? Scan subdirectories — we KNOW this is a game
                if not exe_list:
                    exe_list = _find_exe_in_subdirs(child, child_dirs)
                    if exe_list and log_path:
                        _detlog.note(f"Subdir scan found: {exe_list[0]}")
                gog_meta = _extract_gog_metadata(child) if store == "GOG" else None
                # Use GOG .info exe if we still have no exe from root/lnk/subdir scan
                if not exe_list and gog_meta and gog_meta.get("exe"):
                    gog_exe = gog_meta["exe"].replace("\\", "/")
                    exe_list = [gog_exe]
                    if log_path:
                        _detlog.note(f"GOG .info exe: {gog_exe}")
                engine = _detect_engine(child)
                if log_path:
                    _detlog.tier1_store(store, exe_list[0] if exe_list else None, engine)
                games.append(_build_result(
                    child, store, signal, exe_list,
                    folder=folder_label, container=container_label,
                    engine=engine, gog_metadata=gog_meta,
                ))
                continue

            # Tier 2: Root exe or .lnk → standalone
            if has_root_exe or has_root_lnk:
                exe_list = [root_exe] if root_exe else []
                # If only .lnk, parse it for the target exe
                if not exe_list and has_root_lnk:
                    lnk_target = _find_exe_via_lnk(child)
                    if lnk_target:
                        exe_list = [lnk_target]
                # If root exe is a launcher, also find the actual game exe deeper
                if exe_list and "launcher" in exe_list[0].lower():
                    deeper_exes = _find_exe_in_subdirs(child, child_dirs)
                    if deeper_exes:
                        exe_list.extend(deeper_exes)
                        if log_path:
                            _detlog.note(f"Launcher at root, also found: {deeper_exes}")
                engine = _detect_engine(child)
                if log_path:
                    _detlog.tier2_standalone(
                        exe_list[0] if exe_list else None,
                        "root_exe" if has_root_exe else "root_lnk",
                        engine,
                    )
                games.append(_build_result(
                    child, "Standalone", "root_exe" if has_root_exe else "root_lnk",
                    exe_list, folder=folder_label, container=container_label,
                    engine=engine,
                ))
                continue

            # ── Phase 3: Container check ──
            # Check children for store markers OR game executables
            is_container = False
            has_game_child = False

            for c in child_dirs:
                c_path = Path(c.path)
                c_store, c_signal, c_has_exe, c_exe, _, _ = _scan_root(c_path)
                if c_store is not None:
                    is_container = True
                    break
                # Child has a game exe AND is not a data/utility folder
                if c_has_exe and not _is_non_game_folder(c_path, []):
                    has_game_child = True

            # Store/publisher container: root has ONLY dirs, no files at root
            # Examples: Blizzard/, UBI/, Epic Games/ at top level
            if not is_container and not has_game_child and len(child_dirs) > 0:
                has_files_at_root = False
                try:
                    for se in os.scandir(child):
                        if se.is_file() and not se.name.startswith("."):
                            has_files_at_root = True
                            break
                except PermissionError:
                    pass
                if not has_files_at_root:
                    # Check if any child has game-like structure (subdir with exe)
                    for c in child_dirs:
                        c_path = Path(c.path)
                        try:
                            for se in os.scandir(c_path):
                                if se.is_dir() and not _is_noise_dir(se.name):
                                    try:
                                        for gse in os.scandir(se.path):
                                            if (gse.is_file()
                                                    and gse.name.lower().endswith(".exe")
                                                    and not _is_noise_exe(gse.name)):
                                                is_container = True
                                                break
                                    except PermissionError:
                                        pass
                                if is_container:
                                    break
                        except PermissionError:
                            pass
                        if is_container:
                            break

            if is_container or has_game_child:
                if log_path:
                    _detlog.tier3_container([c.name for c in child_dirs])
                _scan(child, prefix=f"{prefix}{entry.name}/", container=True)
                continue

            # ── Non-game folder check ──
            # Quick check: is this folder clearly not a game?
            if _is_non_game_folder(child, child_dirs):
                if log_path:
                    _detlog.skipped("Non-game folder check (redistributable/music/mod/tool)")
                continue

            # ── Phase 2: Deep signal scan ──
            deep_store, deep_signal, deep_exes = _deep_signal_scan(child)

            if log_path:
                _detlog.deep_scan(
                    signals=[deep_signal] if deep_signal else [],
                    exes=deep_exes,
                )

            # Score deep exes to pick the best one
            best_deep_exe = _pick_best_root_exe(child, deep_exes) if deep_exes else None
            deep_exe_list = [best_deep_exe] if best_deep_exe else []

            if deep_store is not None:
                engine = _detect_engine(child)
                if log_path:
                    _detlog.tier1_store(deep_store, deep_exe_list[0] if deep_exe_list else None, engine)
                games.append(_build_result(
                    child, deep_store, deep_signal, deep_exe_list,
                    folder=folder_label, container=container_label,
                    engine=engine,
                ))
                continue

            # ── Still unknown ──
            engine = _detect_engine(child)
            if log_path:
                _detlog.tier4_unknown(deep_exe_list[0] if deep_exe_list else None, engine)
            games.append(_build_result(
                child, None, None, deep_exe_list,
                folder=folder_label, container=container_label,
                engine=engine, needs_review=True,
            ))

    # Run Phases 1-3
    _scan(root_path)
    games.sort(key=lambda g: g["folder"].lower())

    # Write log if requested
    if log_path:
        _detlog.write(log_path)
        print(f"Log written to {log_path}", file=sys.stderr)

    # ── Phase 4: Enrichment (only for needs_review folders) ──
    if extract_metadata or verify_pcgw:
        _enrich_unknowns(games, extract_metadata=extract_metadata, verify_pcgw=verify_pcgw)

    return games


def _enrich_unknowns(
    games: list[dict],
    *,
    extract_metadata: bool = False,
    verify_pcgw: bool = False,
) -> None:
    """Enrich needs_review games with PE metadata and/or PCGW lookup."""
    pcgw_delay = 0.6  # Rate limit: 0.6s between API calls

    for game in games:
        if not game.get("needs_review"):
            continue

        folder_path = Path(game["path"])
        if not folder_path.is_dir():
            continue

        name_candidates: list[str] = []
        pe_metadata: dict = {}

        # PE metadata extraction
        if extract_metadata:
            exe_path, pe_metadata, bat_launchers = _pick_primary_executable(folder_path)
            if pe_metadata:
                game["pe_metadata"] = pe_metadata
                # Use PE metadata as name candidates
                for key in ("FileDescription", "ProductName"):
                    if pe_metadata.get(key):
                        name_candidates.append(pe_metadata[key])
            if bat_launchers:
                game["bat_launchers"] = bat_launchers
        else:
            # Still check for .bat launchers (always useful)
            _, _, bat_launchers = _pick_primary_executable(folder_path)
            if bat_launchers:
                game["bat_launchers"] = bat_launchers

        # Build name candidates from folder + exe names
        folder_candidates = _build_name_candidates(
            game["folder"], folder_path, game,
        )
        name_candidates.extend(folder_candidates)

        # Deduplicate
        seen: set[str] = set()
        unique: list[str] = []
        for c in name_candidates:
            if c.lower() not in seen:
                seen.add(c.lower())
                unique.append(c)
        game["name_candidates"] = unique

        # PCGamingWiki lookup
        if verify_pcgw and unique:
            for candidate in unique[:3]:  # Try top 3 candidates
                match = _pcgw_lookup(candidate)
                if match:
                    game["pcgw"] = match
                    break
                time.sleep(pcgw_delay)
            if "pcgw" not in game:
                game["needs_name_review"] = True


# ══════════════════════════════════════════════════════════════
# Output formatting
# ══════════════════════════════════════════════════════════════

def _print_summary(games: list[dict]) -> None:
    """Print human-readable summary table."""
    stores: dict[str, int] = {}
    unknowns: list[dict] = []

    for g in games:
        store = g["store"]
        stores[store] = stores.get(store, 0) + 1
        if g.get("needs_review"):
            unknowns.append(g)

    print(f"\n{'='*60}")
    print(f"  Detected {len(games)} games")
    print(f"{'='*60}")
    print(f"\n  Store breakdown:")
    for store, count in sorted(stores.items()):
        print(f"    {store:25s} {count}")
    print(f"\n  Engine breakdown:")
    engines: dict[str, int] = {}
    for g in games:
        e = g.get("engine", "Unknown")
        engines[e] = engines.get(e, 0) + 1
    for eng, count in sorted(engines.items()):
        print(f"    {eng:25s} {count}")

    if unknowns:
        print(f"\n  Needs review ({len(unknowns)}):")
        for g in unknowns:
            candidates = g.get("name_candidates", [])
            exe_info = f" ({g['exe_count']} exes)" if g["exe_count"] else ""
            cand_str = f" → {candidates[0]}" if candidates else ""
            pcgw_str = f" [PCGW: {g['pcgw']['title']}]" if g.get("pcgw") else ""
            print(f"    {g['folder']:40s}{exe_info}{cand_str}{pcgw_str}")
    print()


# ══════════════════════════════════════════════════════════════
# CLI
# ══════════════════════════════════════════════════════════════

def main() -> None:
    args = sys.argv[1:]
    if not args or "-h" in args or "--help" in args:
        print(
            "usage: python detect.py <directory> [options]\n\n"
            "Unified game detection tool.\n\n"
            "Options:\n"
            "  --json                Output full JSON (default: summary table)\n"
            "  --log FILE            Write detailed detection log to FILE\n"
            "  --metadata            Enable PE metadata extraction for unknowns\n"
            "  --pcgw                Enable PCGamingWiki lookup for unknowns\n"
            "  --steam-libraries P   Exclude Steam library paths\n"
            "  -h, --help            Show this help\n\n"
            "Phases 1-3 (always run): fast signal detection + deep scan for unknowns.\n"
            "Phase 4 (--metadata / --pcgw): enrichment only for needs_review folders.\n"
        )
        sys.exit(1 if args and "-h" not in args else 0)

    root = args[0]
    output_json = "--json" in args
    extract_metadata = "--metadata" in args
    verify_pcgw = "--pcgw" in args

    # Parse --log
    log_path = None
    try:
        idx = args.index("--log")
        if idx + 1 < len(args):
            log_path = args[idx + 1]
    except ValueError:
        pass

    # Parse --steam-libraries
    steam_libs: set[str] = set()
    try:
        idx = args.index("--steam-libraries")
        steam_libs = {
            args[i]
            for i in range(idx + 1, len(args))
            if not args[i].startswith("-")
        }
    except ValueError:
        pass

    games = scan_directory(
        root,
        steam_library_paths=steam_libs or None,
        extract_metadata=extract_metadata,
        verify_pcgw=verify_pcgw,
        log_path=log_path,
    )

    if output_json:
        print(json.dumps(games, indent=2, default=str))
    else:
        _print_summary(games)


if __name__ == "__main__":
    main()
