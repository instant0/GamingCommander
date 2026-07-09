#!/usr/bin/env python3
"""
Standalone directory scanner — three-tier classification.

Tiers (in priority order):
  1. HIGH confidence — folder root has launcher markers (GOG, EA, Ubisoxt, Epic)
  2. LOW confidence — folder root has .exe file(s) but no markers
  3. Unknown / needs review — no markers, no root exe

Container detection (Tier 3 sub-check):
  A folder with no markers and no root exe is a container ONLY if at least
  one of its *children* has launcher markers.  Children with only executables
  (no markers) do NOT make the parent a container — those execs belong to the
  parent's own game tree.

Markers are checked case-insensitively.  Executable walk is depth-limited
(WALK_MAX_DEPTH).  Folders in SKIP_NAMES are ignored.
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
def _scandir_info(path: Path):
    """One-level scandir of *path*.  Return (has_markers, has_root_exe, child_dirs)."""
    try:
        entries = list(os.scandir(path))
    except PermissionError:
        return False, False, []
    names = {e.name.lower() for e in entries}
    dirs  = {e.name.lower() for e in entries if e.is_dir(follow_symlinks=False)}
    store, _ = _match_markers(names, dirs)
    has_markers = store != "Standalone"
    has_root_exe = any(
        e.name.lower().endswith(".exe") and not _exe_is_noise(e.name)
        for e in entries
    )
    child_dirs = [e for e in entries if e.is_dir(follow_symlinks=False)]
    return has_markers, has_root_exe, child_dirs


# ---------------------------------------------------------------------------
# Full depth-limited walk + collection
# ---------------------------------------------------------------------------

def _walk_and_collect(game_dir: Path):
    """Walk to WALK_MAX_DEPTH.  Return (store, markers, exe_list)."""
    all_names: set[str] = set()
    all_dirs:  set[str] = set()
    exe_candidates: list[dict] = []

    stack: list[tuple[Path, int]] = [(game_dir, 0)]
    while stack:
        current, depth = stack.pop()
        try:
            entries = list(os.scandir(current))
        except PermissionError:
            continue

        rel = current.relative_to(game_dir)
        for e in entries:
            key = str(rel / e.name) if depth > 0 else e.name
            all_names.add(key.lower())
            if e.is_dir(follow_symlinks=False):
                all_dirs.add(key.lower())
            if e.name.lower().endswith(".exe") and not _exe_is_noise(key):
                try:
                    sz = e.stat(follow_symlinks=False).st_size
                except OSError:
                    sz = 0
                exe_candidates.append({"path": key, "size": sz, "name": e.name})
        if depth < WALK_MAX_DEPTH:
            for e in entries:
                if e.is_dir(follow_symlinks=False):
                    stack.append((Path(e.path), depth + 1))

    store, markers = _match_markers(all_names, all_dirs)
    exe_candidates.sort(key=lambda x: x["size"], reverse=True)
    return store, markers, exe_candidates


def detect_game(game_dir: Path) -> dict:
    """Collect all info for a folder assumed to be a game."""
    store, markers, exes = _walk_and_collect(game_dir)
    result: dict = {
        "folder": game_dir.name,
        "path": str(game_dir),
        "store": store,
        "confidence": "High" if markers else "Low",
        "markers": markers,
        "exe_count": len(exes),
        "exes": exes,
        "gog_metadata": None,
    }
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

    Three-tier classification:
      - Markers found at root  → launcher game (HIGH confidence)
      - Root .exe, no markers   → standalone (LOW confidence)
      - No markers, no root exe → check for container, else Unknown/needs_review
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

            hm, hre, child_dirs = _scandir_info(child)

            # --- Tier 1: has markers → launcher game ---
            if hm:
                g = detect_game(child)
                g["folder"] = f"{prefix}{child.name}"
                if prefix:
                    g["container"] = prefix.rstrip("/")
                games.append(g)
                continue

            # --- Tier 2: root exe, no markers → standalone ---
            if hre:
                g = detect_game(child)
                g["folder"] = f"{prefix}{child.name}"
                if prefix:
                    g["container"] = prefix.rstrip("/")
                games.append(g)
                continue

            # --- Tier 3: no markers, no root exe ---
            # Sub-check: container?  Only if a child has launcher markers.
            is_container = any(
                _scandir_info(Path(c.path))[0] for c in child_dirs
            )

            if is_container:
                _scan(child, prefix=f"{prefix}{entry.name}/")
                continue

            # Not a container.  Could be a game with deep exes or junk.
            # Do the full walk and let the user decide.
            g = detect_game(child)
            g["folder"] = f"{prefix}{child.name}"
            if prefix:
                g["container"] = prefix.rstrip("/")
            # If the deep walk also found no markers, flag for review
            if not g["markers"]:
                g["needs_review"] = True
                g["store"] = "Unknown"
            games.append(g)

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
            "Depth-limited walk (4 levels).  Case-insensitive matching.\n"
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
