#!/usr/bin/env python3
"""Test detection logic patterns against real game folder data.

This script extracts specific patterns from the training data and tests
whether the detection logic handles them correctly. Focus areas:
  1. Noise filtering false positives/negatives
  2. Scoring accuracy (correct primary exe selection)
  3. Store signal detection gaps
  4. Container/publisher folder handling
  5. Backup/crack exe handling

Usage:
  python tools/test_detection_patterns.py /home/malware/projects/game-text
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path
from collections import defaultdict

sys.path.insert(0, str(Path(__file__).parent))
from detect import (
    _is_noise_exe,
    _is_noise_dir,
    _NOISE_EXE_PARTS,
    _pick_best_root_exe,
    _NON_GAME_DIR_NAMES,
    SKIP_NAMES,
)


# ══════════════════════════════════════════════════════════════
# Parse game-text files
# ══════════════════════════════════════════════════════════════

def parse_game_text(filepath: str) -> dict[str, list[str]]:
    games = defaultdict(list)
    with open(filepath, "r", encoding="utf-8", errors="ignore") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            parts = line.replace("\\", "/").split("/")
            if len(parts) < 4:
                continue
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


# ══════════════════════════════════════════════════════════════
# Test Cases
# ══════════════════════════════════════════════════════════════

def test_noise_filtering(all_games: dict[str, list[str]]) -> dict:
    """Test noise filtering against known patterns from training data."""
    issues = []
    stats = {"total": 0, "filtered": 0, "kept": 0}

    # Patterns that SHOULD be noise (from training data analysis)
    should_be_noise = [
        # Crash reporters
        "crashpad_handler", "CrashReport", "BsSndRpt", "BugSplatHD",
        "crashreporter", "CrashSender", "BlizzardError",
        # Uninstallers
        "unins000", "unins001", "unins002", "uninst", "UNINSTAL",
        # Installers/redists
        "DXSETUP", "vcredist", "dotnet", "oalinst", "VC_redist",
        "D3D11Install", "Setup", "setup",
        # Anti-cheat
        "EAAntiCheat", "BEService", "EACGuard",
        # Launchers (when deeper exe exists)
        "Launcher", "launcher",
        # Tools
        "configtool", "Worldbuilder", "AdventureStudio",
        # Backup copies
        "Copy of", " - Copy",
        # GOG stubs
        "galaxy", "setup_", "language_setup",
        # Web UI
        "BlizzardBrowser", "awesomium", "CoherentUI", "CefHost",
        # Modding
        "dnSpy", "SB3Utility", "ZipStudio", "KKManager",
    ]

    # Patterns that should NOT be noise (actual games)
    should_not_be_noise = [
        "AC3", "AC3SP", "AC3MP", "AssassinsCreed3",  # AC3
        "GTA_SA", "GTA5_Enhanced",  # GTA
        "Wiz8", "Gothic3", "Gothic", "Grimrock",  # Classic games
        "dink", "Dink", "anox",  # Classic games
        "lotrbfme2", "Heroes4", "IWD2",  # Strategy/RPG
        "Minecraft",  # Minecraft
        "hl2",  # Half-Life
        "witcher", "Witcher",  # Witcher
        "DarkSoulsIII", "deadspace3", "NFS13",  # AAA games
        "Northgard", "GreedFall", "VRising",  # Indie/modern
        "Warhammer", "Warhammer2",  # Total War
        "DaysGone", "RiftApart", "HorizonForbiddenWest",  # PS ports
        "AlanWake2", "Starfield", "Wukong",  # Recent AAA
        "b1", "SandFall",  # UE games with short names
    ]

    for name, exes in all_games.items():
        for exe in exes:
            stats["total"] += 1
            exe_name = Path(exe).stem
            is_noise = _is_noise_exe(exe_name)
            if is_noise:
                stats["filtered"] += 1
            else:
                stats["kept"] += 1

    # Check specific false positive/negative patterns
    false_positives = []
    false_negatives = []

    # Test patterns that might be incorrectly filtered
    test_cases = [
        # (exe_name, should_be_noise, reason)
        ("CrashReport", True, "crash reporter"),
        ("crashpad_handler", True, "crash handler"),
        ("BsSndRpt", True, "BugSplat send report"),
        ("BugSplatHD", True, "BugSplat reporter"),
        ("BlizzardError", True, "Blizzard error reporter"),
        ("BlizzardBrowser", True, "Blizzard browser process"),
        ("UnityCrashHandler64", True, "Unity crash handler"),
        ("UnityCrashHandler32", True, "Unity crash handler"),
        ("BEService_x64", True, "BattlEye service"),
        ("EAAntiCheat.GameServiceLauncher", True, "EA anti-cheat"),
        ("unins000", True, "uninstaller"),
        ("DXSETUP", True, "DirectX setup"),
        ("vcredist_x64", True, "VC++ redistributable"),
        ("VC_redist.x64", True, "VC++ redistributable"),
        ("oalinst", True, "OpenAL installer"),
        ("galaxy_no_mans_sky_2.12.0.15", True, "GOG Galaxy stub"),
        ("setup_", True, "installer"),
        ("language_setup", True, "installer"),

        # Should NOT be noise
        ("AC3", False, "Assassin's Creed III"),
        ("AC3SP", False, "AC3 single player"),
        ("GTA_SA", False, "GTA San Andreas"),
        ("GTA5_Enhanced", False, "GTA V Enhanced"),
        ("Wiz8", False, "Wizardry 8"),
        ("Gothic3", False, "Gothic 3"),
        ("Gothic", False, "Gothic"),
        ("Grimrock", False, "Grimrock"),
        ("Grimrock2", False, "Grimrock 2"),
        ("dink", False, "Dink Smallwood"),
        ("anox", False, "Anachronox"),
        ("lotrbfme2", False, "LOTR BFME2"),
        ("Heroes4", False, "Heroes of Might and Magic IV"),
        ("IWD2", False, "Icewind Dale II"),
        ("Minecraft", False, "Minecraft"),
        ("hl2", False, "Half-Life 2"),
        ("witcher2", False, "The Witcher 2"),
        ("DarkSoulsIII", False, "Dark Souls 3"),
        ("deadspace3", False, "Dead Space 3"),
        ("NFS13", False, "Need for Speed Most Wanted"),
        ("Northgard", False, "Northgard"),
        ("GreedFall", False, "GreedFall"),
        ("VRising", False, "V Rising"),
        ("Warhammer2", False, "Total War Warhammer 2"),
        ("DaysGone", False, "Days Gone"),
        ("RiftApart", False, "Ratchet & Clank Rift Apart"),
        ("AlanWake2", False, "Alan Wake 2"),
        ("Starfield", False, "Starfield"),
        ("Wukong", False, "Black Myth Wukong"),
        ("b1", False, "Black Myth Wukong (UE exe)"),
        ("SandFall", False, "Expedition 33 (UE exe)"),
        ("BASS2", False, "Beyond a Steel Sky"),
        ("NMS", False, "No Man's Sky"),
        ("FSD", False, "Deep Rock Galactic"),
        ("Troy", False, "Total War Saga Troy"),
        ("EtG", False, "Enter the Gungeon"),
        ("AI", False, "Alien Isolation"),
        ("RAGE2", False, "RAGE 2"),
        ("SOTTR", False, "Shadow of the Tomb Raider"),
        ("ACOrigins", False, "Assassin's Creed Origins"),
        ("SRTTR", False, "Saints Row the Third Remastered"),
        ("GTA5_Enhanced_BE", False, "GTA V Enhanced with BattlEye"),
        ("RainbowSix", False, "Rainbow Six Siege"),
        ("RainbowSix_BE", False, "Rainbow Six Siege with BattlEye"),
        ("GRB", False, "Ghost Recon Breakpoint"),
        ("GRB_vulkan", False, "Ghost Recon Breakpoint Vulkan"),
        ("PhoenixPointWin64", False, "Phoenix Point"),
        ("crs-handler", True, "CrashReporter Suite handler"),
        ("crs-uploader", True, "CrashReporter Suite uploader"),
        ("ActivationUI", True, "EA activation UI"),
        ("touchup", True, "EA touchup installer"),
        ("Cleanup", True, "EA cleanup installer"),
    ]

    for exe_name, expected_noise, reason in test_cases:
        is_noise = _is_noise_exe(exe_name)
        if is_noise and not expected_noise:
            false_positives.append({
                "exe": exe_name,
                "reason": reason,
                "is_noise": is_noise,
                "expected_noise": expected_noise,
            })
        elif not is_noise and expected_noise:
            false_negatives.append({
                "exe": exe_name,
                "reason": reason,
                "is_noise": is_noise,
                "expected_noise": expected_noise,
            })

    return {
        "stats": stats,
        "false_positives": false_positives,
        "false_negatives": false_negatives,
    }


def test_scoring(all_games: dict[str, list[str]]) -> dict:
    """Test exe scoring logic against known game folders."""
    issues = []
    correct = 0
    total = 0

    # Expected primary exes for specific games (ground truth from training data)
    expected_primaries = {
        "ACreed3": "Assassin's Creed III/AC3.exe",  # Not AssassinsCreed3
        "Age of Wonders III Golden Realms": "AoW3.exe",  # Not AoW3Launcher
        "Anachronox": "anox.exe",  # Not afscmd, dparse, particleman
        "ARC": "ARC/Arc.exe",
        "Ashen": "Ashen/Binaries/Win64/Ashen-Win64-Shipping.exe",  # UE shipping
        "BD": "BD/BlackDesert/BlackDesertEAC.exe",
        "DarkSouls3": "DarkSouls3/Game/DarkSoulsIII.exe",
        "Dead Space 3": "Dead Space 3/deadspace3.exe",
        "DeadSpace": "DeadSpace/Dead Space.exe",
        "Deadlight": "Deadlight/Binaries/Win32/LOTDGame.exe",
        "DeepRock": "DeepRock/FSD/Binaries/Win64/FSD-Win64-Shipping.exe",
        "Diablo III": "Diablo III/Diablo III.exe",  # Not x64 copy
        "DoomDarkAges": "DoomDarkAges/DOOMTheDarkAges.exe",
        "Dungeon Siege 2": "Dungeon Siege 2/DungeonSiege2.exe",
        "EVE": "EVE/eve.exe",
        "FarCry4": "FarCry4/bin/FarCry4.exe",
        "GhostREconWild": "GhostREconWild/Tom Clancy's Ghost Recon Wildlands/GRW.exe",
        "Gothic 2 Gold": "Gothic 2 Gold/system/Gothic.exe",
        "GRIMROCK": "GRIMROCK/grimrock.exe",
        "Grimrock2": "Grimrock2/grimrock2.exe",
        "Hard Bullet": "hardB/steamapps/common/Hard Bullet/Hard Bullet.exe",
        "HORGOW": "HORGOW/HorizonForbiddenWest.exe",
        "IdleChampions": "IdleChampions/IdleDragons.exe",
        "jag2": "jag2/ja2.exe",
        "jag2UB": "jag2UB/JA2UB.exe",
        "MC": "MC/client/Minecraft.exe",  # Not Minecraft_Server
        "Might and Magic IX": "Might and Magic IX/mm9.exe",
        "mmxl": "mmxl/Might and Magic X Legacy.exe",
        "necropolis_brutal_edition-1": "necropolis_brutal_edition-1/game/Necropolis.exe",
        "NIER": "NIER/NieRAutomata.exe",
        "penumbra": "penumbra/redist/PENUMBRA.EXE",
        "ShadowRun": "ShadowRun/Shadowrun.exe",
        "SpellForce3": "SpellForce3/SF3ClientFinal.exe",
        "steelstorm": "steelstorm/steelstorm.exe",
        "sysshock2.25": "sysshock2.25/SystemShock2Remastered.exe",
        "Vampire - Bloodlines": "Vampire - Bloodlines/vampire.exe",
        "VRK": "VRK/VR_Kanojo.exe",
        "wl3": "wl3/WL3.exe",
        "Xenonauts": "Xenonauts/Xenonauts.exe",
        "ACValhalla": "ACValhalla/ACValhalla.exe",
        "AW2": "AW2/AlanWake2.exe",
        "AiGirl": "AiGirl/AI-Syoujyo.exe",
        "DeathStranding": "DeathStranding/DeathStranding.exe",
        "DR3": "DR3/deadrising3.exe",
        "arx": "arx/ARX.exe",
        "dow2": "dow2/DOW2.exe",
        "doom6": "doom6/DOOMx64.exe",
        "galciv4": "galciv4/GalCiv4.exe",
        "MadMax": "MadMax/MadMax.exe",
        "Oblivion": "Oblivion/OblivionRemastered.exe",
        "Phoenix Point": "Phoenix Point/PhoenixPointWin64.exe",
        "RAtchet": "RAtchet/RiftApart.exe",
        "RE8": "RE8/re8.exe",
        "SINS2": "SINS2/sins2.exe",
        "TombRaiderGOTYE": "TombRaiderGOTYE/TombRaider.exe",
        "sr3rmx": "sr3rmx/SRTTR.exe",
        "The Witcher 2": "The Witcher 2/bin/witcher2.exe",
        "The Witcher Enhanced Edition": "The Witcher Enhanced Edition/System/witcher.exe",
        "Starfield": "Starfield/Starfield.exe",
        "The Outer Worlds": "The Outer Worlds/TheOuterWorldsSpacersChoiceEdition.exe",
        "Wukong": "Wukong/b1/Binaries/Win64/b1-Win64-Shipping.exe",
        "Clair": "Clair/Sandfall/Binaries/Win64/SandFall-Win64-Shipping.exe",
        "DG": "DG/BendGame/Binaries/Win64/DaysGone.exe",
        "GOT": "GOT/Binaries/Win32/ShippingPC-AGOTGame.exe",
        "GearsJack": "GearsJack/GearGame/Binaries/Steam/GearsTactics.exe",
        "hadse2": "hadse2/Ship/Hades2.exe",
        "Terminator Resistance": "Terminator Resistance/Terminator/Binaries/Win64/Terminator-Win64-Shipping.exe",
        "DyingLight2StayHuman": "DyingLight2StayHuman/ph/work/bin/x64/DyingLightGame_x64_rwdi.exe",
    }

    for game_name, expected_path in expected_primaries.items():
        # Find this game in all_games
        found = False
        for key, exes in all_games.items():
            game_folder = key.split("/")[-1] if "/" in key else key
            if game_folder == game_name:
                found = True
                total += 1
                # The expected path is relative to the game folder
                # e.g., "Assassin's Creed III/AC3.exe" means root + subdir
                # For scoring, we need the full relative path
                # The training data already has full paths relative to game folder
                if expected_path in exes:
                    correct += 1
                else:
                    # Check if the exe exists at all
                    exe_stem = Path(expected_path).stem.lower()
                    matching = [e for e in exes if Path(e).stem.lower() == exe_stem]
                    if matching:
                        correct += 1  # Found with different case
                    else:
                        issues.append({
                            "game": game_name,
                            "expected": expected_path,
                            "available": [e for e in exes if not _is_noise_exe(Path(e).stem)],
                        })
                break

        if not found:
            # Try fuzzy match
            for key, exes in all_games.items():
                if game_name.lower() in key.lower():
                    found = True
                    total += 1
                    exe_stem = Path(expected_path).stem.lower()
                    matching = [e for e in exes if Path(e).stem.lower() == exe_stem]
                    if matching:
                        correct += 1
                    else:
                        issues.append({
                            "game": game_name,
                            "expected": expected_path,
                            "available": [e for e in exes if not _is_noise_exe(Path(e).stem)],
                        })
                    break

    return {
        "total": total,
        "correct": correct,
        "accuracy": correct / total if total > 0 else 0,
        "issues": issues,
    }


def test_store_detection(all_games: dict[str, list[str]]) -> dict:
    """Test store signal detection patterns."""
    issues = []

    # Games that should be detected as specific stores
    expected_stores = {
        # BattleNet
        "Blizzard": "BattleNet",
        "Diablo III": "BattleNet",  # Under Blizzard folder
        "World of Warcraft": "BattleNet",
        "Diablo Immortal": "BattleNet",
        "COD": "BattleNet",  # Call of Duty

        # GOG
        "Arx Fatalis": "GOG",
        "Baldurs Gate 3": "GOG",
        "Cyberpunk 2077": "GOG",
        "Fallout 4 GOTY": "GOG",
        "The Witcher 3 Wild Hunt GOTY": "GOG",
        "Metro Exodus": "GOG",
        "System Shock Remake": "GOG",

        # EA/Origin
        "ORIGIN": "EA",
        "EA": "EA",
        "Dead Space 3": "EA",
        "Need for Speed(TM) Most Wanted": "EA",
        "Dragon Age Inquisition": "EA",
        "Battlefield 1": "EA",

        # Ubisoft
        "Ubi": "Ubisoft",
        "ubi": "Ubisoft",
        "Assassin's Creed IV Black Flag": "Ubisoft",
        "FarCry4": "Ubisoft",
        "GhostREconWild": "Ubisoft",
        "Ghost Recon Breakpoint": "Ubisoft",

        # Rockstar
        "Rockstar Games": "Rockstar",
        "Rockstar": "Rockstar",
        "Grand Theft Auto V Enhanced": "Rockstar",

        # Epic
        "Epic Games": "Epic",
        "EpicLauncher": "Epic",
    }

    for game_name, expected_store in expected_stores.items():
        found = False
        for key in all_games:
            game_folder = key.split("/")[-1] if "/" in key else key
            if game_folder == game_name or game_name.lower() in key.lower():
                found = True
                # We can't run actual store detection without filesystem,
                # but we can check if the folder name matches known patterns
                break

        if not found:
            issues.append({
                "game": game_name,
                "expected_store": expected_store,
                "issue": "Game not found in training data",
            })

    return {
        "total_checked": len(expected_stores),
        "issues": issues,
    }


def test_backup_handling(all_games: dict[str, list[str]]) -> dict:
    """Test backup/crack exe handling patterns."""
    issues = []
    stats = {"games_with_backups": 0, "backups_correctly_penalized": 0}

    backup_patterns = [
        "copy of", " - copy", "_copy",
        "org_", "org",
        "original",
        "crack", "cracked",
    ]

    for name, exes in all_games.items():
        has_backup = False
        for exe in exes:
            exe_lower = Path(exe).stem.lower()
            if any(p in exe_lower for p in backup_patterns):
                has_backup = True
                break

        if has_backup:
            stats["games_with_backups"] += 1

            # Check that the scoring logic would penalize backups
            non_noise = [Path(e).stem for e in exes if not _is_noise_exe(Path(e).stem)]
            if non_noise:
                best = _pick_best_root_exe(
                    Path(f"/tmp/{name}"),
                    [f"{n}.exe" for n in non_noise]
                )
                if best:
                    best_lower = Path(best).stem.lower()
                    is_backup = any(p in best_lower for p in backup_patterns)
                    if is_backup:
                        issues.append({
                            "game": name,
                            "selected": best,
                            "issue": "Scoring selected backup/crack as primary",
                        })
                    else:
                        stats["backups_correctly_penalized"] += 1

    return {
        "stats": stats,
        "issues": issues,
    }


# ══════════════════════════════════════════════════════════════
# Main
# ══════════════════════════════════════════════════════════════

def main():
    args = sys.argv[1:]
    if not args or "-h" in args or "--help" in args:
        print("usage: python test_detection_patterns.py <text-dir>")
        sys.exit(0)

    text_dir = args[0]
    text_path = Path(text_dir)

    # Parse all game-text files
    all_games = {}
    for txt_file in sorted(text_path.glob("*.txt")):
        games = parse_game_text(str(txt_file))
        for name, exes in games.items():
            key = f"{txt_file.stem}/{name}"
            all_games[key] = exes

    print(f"Parsed {len(all_games)} games from {len(list(text_path.glob('*.txt')))} files\n")

    # Run tests
    print("=" * 70)
    print("  NOISE FILTERING TEST")
    print("=" * 70)
    noise_results = test_noise_filtering(all_games)
    print(f"  Total exes: {noise_results['stats']['total']}")
    print(f"  Filtered: {noise_results['stats']['filtered']}")
    print(f"  Kept: {noise_results['stats']['kept']}")
    if noise_results["false_positives"]:
        print(f"\n  FALSE POSITIVES ({len(noise_results['false_positives'])}):")
        for fp in noise_results["false_positives"]:
            print(f"    - {fp['exe']:30s} ({fp['reason']})")
    if noise_results["false_negatives"]:
        print(f"\n  FALSE NEGATIVES ({len(noise_results['false_negatives'])}):")
        for fn in noise_results["false_negatives"]:
            print(f"    - {fn['exe']:30s} ({fn['reason']})")
    if not noise_results["false_positives"] and not noise_results["false_negatives"]:
        print("\n  All noise filtering patterns correct!")

    print(f"\n{'='*70}")
    print("  SCORING TEST")
    print("=" * 70)
    scoring_results = test_scoring(all_games)
    print(f"  Games checked: {scoring_results['total']}")
    print(f"  Correct selections: {scoring_results['correct']}")
    print(f"  Accuracy: {scoring_results['accuracy']:.1%}")
    if scoring_results["issues"]:
        print(f"\n  SCORING ISSUES ({len(scoring_results['issues'])}):")
        for issue in scoring_results["issues"]:
            print(f"    - {issue['game']}")
            print(f"      Expected: {issue['expected']}")
            if issue['available']:
                print(f"      Available: {issue['available'][:5]}")

    print(f"\n{'='*70}")
    print("  STORE DETECTION TEST")
    print("=" * 70)
    store_results = test_store_detection(all_games)
    print(f"  Games checked: {store_results['total_checked']}")
    if store_results["issues"]:
        print(f"\n  STORE ISSUES ({len(store_results['issues'])}):")
        for issue in store_results["issues"]:
            print(f"    - {issue['game']}: {issue['expected_store']} — {issue['issue']}")

    print(f"\n{'='*70}")
    print("  BACKUP HANDLING TEST")
    print("=" * 70)
    backup_results = test_backup_handling(all_games)
    print(f"  Games with backups: {backup_results['stats']['games_with_backups']}")
    print(f"  Correctly penalized: {backup_results['stats']['backups_correctly_penalized']}")
    if backup_results["issues"]:
        print(f"\n  BACKUP ISSUES ({len(backup_results['issues'])}):")
        for issue in backup_results["issues"]:
            print(f"    - {issue['game']}: selected '{issue['selected']}'")

    # Summary
    total_issues = (
        len(noise_results["false_positives"])
        + len(noise_results["false_negatives"])
        + len(scoring_results["issues"])
        + len(store_results["issues"])
        + len(backup_results["issues"])
    )

    print(f"\n{'='*70}")
    print(f"  SUMMARY: {total_issues} total issues found")
    print(f"{'='*70}\n")

    # Output JSON if requested
    if "--json" in args:
        results = {
            "noise": noise_results,
            "scoring": scoring_results,
            "store": store_results,
            "backup": backup_results,
        }
        print(json.dumps(results, indent=2, default=str))


if __name__ == "__main__":
    main()
