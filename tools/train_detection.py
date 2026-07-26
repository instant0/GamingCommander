#!/usr/bin/env python3
"""Train and test detection logic against real game folder data.

Parses exported .txt files listing every .exe in game folders,
reconstructs the folder hierarchy, and tests:
  1. Noise filtering — which exes are correctly/incorrectly filtered
  2. Scoring — does the scoring logic pick the right primary exe?
  3. Store signal detection — does the folder contain identifiable store markers?
  4. Container detection — which folders are publisher/container dirs?

Usage:
  python tools/train_detection.py /home/malware/projects/game-text
  python tools/train_detection.py /home/malware/projects/game-text --json
  python tools/train_detection.py /home/malware/projects/game-text --verbose
"""

from __future__ import annotations

import json
import os
import re
import sys
from collections import defaultdict
from pathlib import Path

# ── Import detection functions from detect.py ───────────────────
# We import the existing logic so we train against the REAL codebase
sys.path.insert(0, str(Path(__file__).parent))
from detect import (
    _is_noise_exe,
    _is_noise_dir,
    _NOISE_EXE_PARTS,
    NOISE_DIR_PARTS,
    SKIP_NAMES,
    _NON_GAME_DIR_NAMES,
    _pick_best_root_exe,
)


# ══════════════════════════════════════════════════════════════
# Data Parsing
# ══════════════════════════════════════════════════════════════

def parse_game_text(filepath: str) -> dict[str, list[str]]:
    """Parse a game-text file into {game_folder: [exe_paths]} structure.
    
    Each line is a full path like: D:\Games\GameName\subdir\game.exe
    We extract the game folder name (first level under root) and collect
    all exes belonging to each game.
    """
    games: dict[str, list[str]] = defaultdict(list)
    with open(filepath, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            # Extract game folder: first dir after root (e.g., D:\Games\<GameName>\...)
            # Normalize path separators
            parts = line.replace("\\", "/").split("/")
            # Find the root level: drive letter + Games
            # e.g., D:/Games/GameName/... → GameName is at index 2 (0=D:, 1=Games)
            if len(parts) < 4:
                continue
            # Root is drive:/Games or drive:/Program Files (x86)/GOG Galaxy/Games
            # Find the "Games" level or "GOG Galaxy/Games" level
            game_folder_idx = None
            for i, p in enumerate(parts):
                if p.lower() == "games" and i >= 1:
                    game_folder_idx = i + 1
                    break
            if game_folder_idx is None or game_folder_idx >= len(parts):
                continue
            game_name = parts[game_folder_idx]
            exe_relative = "/".join(parts[game_folder_idx + 1:])
            games[game_name].append(exe_relative)
    return dict(games)


def analyze_game(name: str, exes: list[str]) -> dict:
    """Analyze a single game's exe list for noise patterns, scoring, etc."""
    result = {
        "name": name,
        "exe_count": len(exes),
        "exes": exes,
        "noise_filtered": [],
        "non_noise": [],
        "root_exes": [],
        "subdir_exes": [],
        "backup_copies": [],
        "launcher_candidates": [],
        "tools": [],
        "deepest_exes": [],  # exes found via UE layout or deep scan
    }

    for exe in exes:
        exe_name = Path(exe).stem.lower()
        exe_filename = Path(exe).name
        is_noise = _is_noise_exe(exe_name)
        is_in_subdir = "/" in exe

        entry = {
            "path": exe,
            "name": exe_filename,
            "stem": exe_name,
            "is_noise": is_noise,
            "is_subdir": is_in_subdir,
        }

        if is_noise:
            result["noise_filtered"].append(entry)
        else:
            result["non_noise"].append(entry)

        if not is_in_subdir:
            result["root_exes"].append(entry)
        else:
            result["subdir_exes"].append(entry)

        # Backup copies
        if "copy of" in exe_name or " - copy" in exe_name or "_copy" in exe_name:
            result["backup_copies"].append(entry)
        elif "org_" in exe_name or exe_name.startswith("org"):
            result["backup_copies"].append(entry)
        elif "original" in exe_name:
            result["backup_copies"].append(entry)

        # Launcher candidates
        if any(p in exe_name for p in ["launcher", "launch", "updater", "bootstrap"]):
            result["launcher_candidates"].append(entry)

        # Tools
        if any(p in exe_name for p in ["editor", "builder", "tool", "config", "settings",
                                         "viewer", "manager", "compiler"]):
            result["tools"].append(entry)

        # UE deep exes (Binaries/Win64, Binaries/Win32, etc.)
        if "binaries/" in exe.lower() or "bin/" in exe.lower():
            result["deepest_exes"].append(entry)

    return result


# ══════════════════════════════════════════════════════════════
# Store Signal Detection (from folder name + exe patterns)
# ══════════════════════════════════════════════════════════════

# Known store folder structures from the training data
STORE_FOLDER_PATTERNS = {
    "BattleNet": {
        "parent_folders": ["blizzard"],
        "game_folders": ["diablo iii", "diablo iv", "world of warcraft",
                        "call of duty", "diablo immortal"],
        "signal_dirs": [".battle.net"],
        "known_exes": ["Diablo III.exe", "Wow.exe", "cod.exe"],
    },
    "GOG": {
        "parent_folders": ["gog galaxy"],
        "game_folders": [],  # GOG games are direct children
        "signal_files": ["goggame.dll", "gog.ico"],
        "signal_prefixes": ["goggame-", "gog_"],
    },
    "EA": {
        "parent_folders": ["ea", "origin"],
        "game_folders": ["battlefield", "dragon age", "mass effect", "dead space",
                        "titanfall", "need for speed", "star wars"],
        "signal_dirs": ["__installer"],
        "signal_exes": ["touchup.exe", "activationui.exe"],
    },
    "Epic": {
        "parent_folders": ["epic games", "epic"],
        "game_folders": [],
        "signal_dirs": [".egstore", ".egsstore"],
    },
    "Ubisoft": {
        "parent_folders": ["ubi", "ubisoft"],
        "game_folders": ["assassin", "far cry", "ghost recon", "splinter",
                        "rainbow six", "watch dogs"],
        "signal_files": ["uplay_install.manifest"],
        "signal_dlls": ["uplay_r1_loader64.dll", "uplay_r2_loader64.dll"],
    },
    "Rockstar": {
        "parent_folders": ["rockstar games", "rockstar"],
        "game_folders": ["gta", "grand theft auto", "red dead"],
        "signal_files": ["title.rgl"],
    },
}


def detect_store_from_context(name: str, exes: list[str], root_path: str) -> str | None:
    """Heuristic store detection from folder name + exe patterns."""
    name_lower = name.lower()

    # Direct folder name matches
    if name_lower in ("blizzard", "battle.net"):
        return "BattleNet"
    if name_lower in ("epic games", "epiclauncher"):
        return "Epic"
    if name_lower in ("origin", "ea"):
        return "EA/Origin"
    if name_lower in ("rockstar games", "rockstar"):
        return "Rockstar"
    if name_lower in ("ubi", "ubisoft"):
        return "Ubisoft"
    if "gog galaxy" in name_lower:
        return "GOG"

    # Parent context
    if "blizzard" in root_path.lower():
        return "BattleNet"
    if "epic games" in root_path.lower() or "epic\\" in root_path.lower():
        return "Epic"
    if "origin" in root_path.lower() or "\\ea\\" in root_path.lower():
        return "EA/Origin"
    if "rockstar" in root_path.lower():
        return "Rockstar"
    if "ubi" in root_path.lower():
        return "Ubisoft"
    if "gog galaxy" in root_path.lower() or "gog" in root_path.lower():
        return "GOG"

    # Exe-based detection
    exe_names = {Path(e).name.lower() for e in exes}
    if any("goggame" in e for e in exe_names):
        return "GOG"
    if "__installer" in name_lower:
        return "EA/Origin"
    if "battle.net" in name_lower:
        return "BattleNet"

    return None


# ══════════════════════════════════════════════════════════════
# Expected Primary Exe Selection
# ══════════════════════════════════════════════════════════════

# Rules for determining the expected primary exe from the training data.
# This uses heuristics based on what we know about real game installations.

def expected_primary_exe(name: str, non_noise_exes: list[dict]) -> str | None:
    """Determine the expected primary exe based on naming heuristics.
    
    This is the GROUND TRUTH for training — what the detection SHOULD pick.
    """
    if not non_noise_exes:
        return None

    name_lower = name.lower().replace(" ", "").replace("-", "").replace("_", "")
    folder_tokens = set(name.lower().split())

    # Score each non-noise exe
    scored = []
    for exe in non_noise_exes:
        exe_lower = exe["stem"].lower()
        score = 0

        # Exact folder name match (strongest)
        if exe_lower == name_lower:
            score += 100

        # Token match
        for token in folder_tokens:
            if len(token) > 1 and token in exe_lower:
                score += 10

        # UE Shipping binary (most likely the game)
        if "shipping" in exe_lower:
            score += 20
        if "win64" in exe_lower:
            score += 5

        # Penalize noise
        if exe["is_noise"]:
            score -= 100

        # Penalize backups
        if "copy of" in exe_lower or " - copy" in exe_lower:
            score -= 50
        if "org_" in exe_lower or exe_lower.startswith("org"):
            score -= 40
        if "original" in exe_lower:
            score -= 30

        # Penalize tools
        if any(p in exe_lower for p in ["editor", "builder", "tool", "config", "settings",
                                          "viewer", "manager", "compiler", "setup"]):
            score -= 25

        # Penalize launchers (when deeper exe exists)
        if "launcher" in exe_lower:
            score -= 20

        # Penalize tiny exes
        if "unins" in exe_lower or "uninstal" in exe_lower:
            score -= 50

        # Penalize installer/redist
        if any(p in exe_lower for p in ["install", "redist", "vcredist", "dxsetup", "oalinst"]):
            score -= 50

        # Penalize crash reporters
        if any(p in exe_lower for p in ["crash", "bugsplat", "crs-"]):
            score -= 40

        # Penalize anti-cheat
        if any(p in exe_lower for p in ["easyanticheat", "battleye", "beservice", "beclient"]):
            score -= 30

        # Prefer shorter names (more likely the game exe)
        if len(exe_lower) < 20:
            score += 5

        # Prefer root exes
        if not exe["is_subdir"]:
            score += 10

        # Prefer deeper exes (UE layout)
        if "binaries/" in exe["path"].lower() or "bin/" in exe["path"].lower():
            score += 15

        scored.append((score, exe))

    scored.sort(key=lambda x: x[0], reverse=True)
    return scored[0][1]["path"] if scored else None


# ══════════════════════════════════════════════════════════════
# Container / Publisher Detection
# ══════════════════════════════════════════════════════════════

def is_publisher_folder(name: str, game_count: int) -> bool:
    """Heuristic: is this a publisher/container folder?"""
    name_lower = name.lower()
    known_publishers = {
        "blizzard", "epic games", "origin", "ea", "rockstar games",
        "rockstar", "ubi", "ubisoft", "squareenix", "square enix",
        "bethesda", "2k", "paradox", "stardock",
    }
    if name_lower in known_publishers:
        return True
    # Publisher folder: has many game subdirectories
    if game_count >= 3:
        return True
    return False


# ══════════════════════════════════════════════════════════════
# Main Analysis
# ══════════════════════════════════════════════════════════════

def run_analysis(text_dir: str, verbose: bool = False) -> dict:
    """Run full analysis on all game-text files."""
    text_path = Path(text_dir)
    if not text_path.is_dir():
        print(f"Error: {text_dir} is not a directory", file=sys.stderr)
        sys.exit(1)

    all_games = {}
    for txt_file in sorted(text_path.glob("*.txt")):
        games = parse_game_text(str(txt_file))
        for name, exes in games.items():
            key = f"{txt_file.stem}/{name}"
            all_games[key] = {
                "source_file": txt_file.name,
                "name": name,
                "exes": exes,
            }

    results = {
        "total_games": len(all_games),
        "total_exes": sum(len(g["exes"]) for g in all_games.values()),
        "games": [],
        "noise_stats": {
            "total_filtered": 0,
            "total_non_noise": 0,
            "false_positives": [],  # non-noise exes that look like they SHOULD be filtered
            "false_negatives": [],  # noise exes that look like they should NOT be filtered
        },
        "scoring_stats": {
            "games_with_multiple_candidates": 0,
            "games_with_backup_exes": 0,
            "games_with_launcher_at_root": 0,
        },
        "store_detection": {
            "battle_net_games": [],
            "gog_games": [],
            "ea_games": [],
            "epic_games": [],
            "ubisoft_games": [],
            "rockstar_games": [],
            "standalone_games": [],
        },
        "issues": [],
    }

    for key, game_data in sorted(all_games.items()):
        name = game_data["name"]
        exes = game_data["exes"]
        source = game_data["source_file"]

        analysis = analyze_game(name, exes)

        # Detect store
        store = detect_store_from_context(name, exes, source)

        # Find expected primary exe
        primary = expected_primary_exe(name, analysis["non_noise"])

        game_result = {
            "key": key,
            "name": name,
            "source": source,
            "store": store,
            "exe_count": len(exes),
            "non_noise_count": len(analysis["non_noise"]),
            "noise_count": len(analysis["noise_filtered"]),
            "expected_primary": primary,
            "root_exe_count": len(analysis["root_exes"]),
            "backup_count": len(analysis["backup_copies"]),
            "launcher_count": len(analysis["launcher_candidates"]),
            "tool_count": len(analysis["tools"]),
            "deepest_exe_count": len(analysis["deepest_exes"]),
        }

        # Categorize by store
        if store:
            store_key = store.lower().replace("/", "_").replace(" ", "_")
            if store_key in results["store_detection"]:
                results["store_detection"][store_key].append(name)
        else:
            results["store_detection"]["standalone_games"].append(name)

        # Stats
        if len(analysis["non_noise"]) > 1:
            results["scoring_stats"]["games_with_multiple_candidates"] += 1
        if analysis["backup_copies"]:
            results["scoring_stats"]["games_with_backup_exes"] += 1
        if analysis["launcher_candidates"] and any(not e["is_subdir"] for e in analysis["launcher_candidates"]):
            results["scoring_stats"]["games_with_launcher_at_root"] += 1

        results["noise_stats"]["total_filtered"] += len(analysis["noise_filtered"])
        results["noise_stats"]["total_non_noise"] += len(analysis["non_noise"])

        # Check for potential issues
        if not analysis["non_noise"] and not analysis["deepest_exes"]:
            results["issues"].append({
                "type": "NO_NON_NOISE_EXE",
                "game": name,
                "source": source,
                "detail": f"All {len(exes)} exes filtered as noise",
                "exes": exes[:10],
            })

        if analysis["backup_copies"] and not any(
            e for e in analysis["non_noise"]
            if "copy of" not in e["stem"] and "org_" not in e["stem"]
            and "original" not in e["stem"]
        ):
            results["issues"].append({
                "type": "ONLY_BACKUP_EXES",
                "game": name,
                "source": source,
                "detail": f"All non-noise exes are backup copies",
                "backup_exes": [e["path"] for e in analysis["backup_copies"]],
            })

        if verbose:
            game_result["non_noise_exes"] = [e["path"] for e in analysis["non_noise"]]
            game_result["noise_exes"] = [e["path"] for e in analysis["noise_filtered"][:5]]
            game_result["backup_exes"] = [e["path"] for e in analysis["backup_copies"]]

        results["games"].append(game_result)

    return results


def print_report(results: dict, verbose: bool = False) -> None:
    """Print human-readable analysis report."""
    print(f"\n{'='*70}")
    print(f"  Detection Training Report")
    print(f"{'='*70}")
    print(f"\n  Total games analyzed: {results['total_games']}")
    print(f"  Total exes parsed: {results['total_exes']}")
    print(f"\n  Noise filtering:")
    print(f"    Exes filtered as noise: {results['noise_stats']['total_filtered']}")
    print(f"    Non-noise exes kept:    {results['noise_stats']['total_non_noise']}")

    print(f"\n  Scoring:")
    print(f"    Games with multiple non-noise candidates: {results['scoring_stats']['games_with_multiple_candidates']}")
    print(f"    Games with backup/copy exes:             {results['scoring_stats']['games_with_backup_exes']}")
    print(f"    Games with launcher at root:             {results['scoring_stats']['games_with_launcher_at_root']}")

    print(f"\n  Store detection:")
    for store, games in sorted(results["store_detection"].items()):
        if games:
            print(f"    {store:25s} {len(games):3d} games")
            for g in sorted(games)[:5]:
                print(f"      - {g}")
            if len(games) > 5:
                print(f"      ... and {len(games) - 5} more")

    if results["issues"]:
        print(f"\n  Issues found: {len(results['issues'])}")
        for issue in results["issues"][:20]:
            print(f"\n    [{issue['type']}] {issue['game']} ({issue['source']})")
            print(f"      {issue['detail']}")
            if "exes" in issue:
                for exe in issue["exes"][:5]:
                    print(f"        - {exe}")

    if verbose:
        print(f"\n{'='*70}")
        print(f"  Detailed Game Results")
        print(f"{'='*70}")
        for game in results["games"]:
            print(f"\n  {game['name']} ({game['source']})")
            print(f"    Store: {game['store'] or 'Standalone'}")
            print(f"    Exes: {game['exe_count']} total, {game['non_noise_count']} non-noise")
            print(f"    Expected primary: {game.get('expected_primary', 'N/A')}")
            if game.get("non_noise_exes"):
                print(f"    Non-noise exes:")
                for exe in game["non_noise_exes"]:
                    print(f"      - {exe}")
            if game.get("backup_exes"):
                print(f"    Backup exes:")
                for exe in game["backup_exes"]:
                    print(f"      - {exe}")

    print(f"\n{'='*70}\n")


def main():
    args = sys.argv[1:]
    if not args or "-h" in args or "--help" in args:
        print(
            "usage: python train_detection.py <text-dir> [options]\n\n"
            "Train and test detection logic against real game folder data.\n\n"
            "Options:\n"
            "  --json      Output full JSON results\n"
            "  --verbose   Show detailed per-game results\n"
            "  -h, --help  Show this help\n"
        )
        sys.exit(0)

    text_dir = args[0]
    output_json = "--json" in args
    verbose = "--verbose" in args

    results = run_analysis(text_dir, verbose=verbose)

    if output_json:
        print(json.dumps(results, indent=2, default=str))
    else:
        print_report(results, verbose=verbose)


if __name__ == "__main__":
    main()
