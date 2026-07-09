#!/usr/bin/env python3
"""
setup_mock_data.py — Generate mock Windows game folder structure for offline testing.

This script creates a simulated Windows game library structure on Linux for:
  - Steam ACF parsing validation
  - Epic .item manifest parsing validation
  - Standalone game detection testing
  - Folder scanner heuristics testing (exe selection, non-game filtering)

The mock data lives under data/mock/ and is used by both Python validation
scripts and C# unit tests. It must be regenerated if the test fixtures need
updating.

Usage:
    python tools/setup_mock_data.py          # generate mock data tree
    python tools/setup_mock_data.py --clean   # remove mock data tree

Output:
    data/mock/ with subdirectories and sample files
"""

import argparse
import os
import shutil
import stat
import sys
from pathlib import Path

MOCK_ROOT = Path(__file__).resolve().parent.parent / "data" / "mock"


# ---------------------------------------------------------------------------
# ACF content helpers
# ---------------------------------------------------------------------------

def make_acf(appid: str, name: str, installdir: str, buildid: str = "12345678",
             stateflags: str = "4", sizeondisk: str = "5000000000",
             lastupdated: str = "1700000000") -> str:
    """Generate a valid Steam ACF file."""
    return (
        f'"AppState"\n'
        f'{{\n'
        f'    "appid"         "{appid}"\n'
        f'    "universe"      "1"\n'
        f'    "LauncherPath"  "C:\\\\Program Files (x86)\\\\Steam\\\\steam.exe"\n'
        f'    "name"          "{name}"\n'
        f'    "StateFlags"    "{stateflags}"\n'
        f'    "installdir"    "{installdir}"\n'
        f'    "LastUpdated"   "{lastupdated}"\n'
        f'    "LastPlayed"    "{lastupdated}"\n'
        f'    "SizeOnDisk"    "{sizeondisk}"\n'
        f'    "buildid"       "{buildid}"\n'
        f'    "InstalledDepots"\n'
        f'    {{\n'
        f'        "{appid}1"\n'
        f'        {{\n'
        f'            "manifest"  "12345678901234567890"\n'
        f'            "size"      "{sizeondisk}"\n'
        f'        }}\n'
        f'    }}\n'
        f'}}\n'
    )


def make_libraryfolders_vdf(libraries: list[tuple[str, str, list[str]]]) -> str:
    """Generate a libraryfolders.vdf.
    
    Args:
        libraries: list of (path, label, [appid, ...]) tuples
    """
    lines = ['"libraryfolders"\n{']
    for i, (path, label, appids) in enumerate(libraries):
        lines.append(f'\t"{i}"\n\t{{')
        lines.append(f'\t\t"path"          "{path}"')
        lines.append(f'\t\t"label"         "{label}"')
        for appid, size in appids:
            lines.append(f'\t\t"apps"\n\t\t{{\n\t\t\t"{appid}"\t"{size}"\n\t\t}}')
        lines.append('\t}')
    lines.append('}')
    return '\n'.join(lines)


# ---------------------------------------------------------------------------
# Epic .item content helpers
# ---------------------------------------------------------------------------

def make_epic_item(display_name: str, app_name: str, install_location: str,
                   launch_executable: str, catalog_namespace: str,
                   catalog_item_id: str, installation_guid: str,
                   app_version: str = "1.0.0.0") -> str:
    """Generate a valid Epic Games Store .item JSON file."""
    return (
        '{\n'
        f'  "FormatVersion": 0,\n'
        f'  "bIsIncompleteInstall": false,\n'
        f'  "LaunchCommand": "",\n'
        f'  "LaunchExecutable": "{launch_executable}",\n'
        f'  "ManifestLocation": "{install_location}/.egstore",\n'
        f'  "ManifestHash": "",\n'
        f'  "bIsApplication": true,\n'
        f'  "bIsExecutable": true,\n'
        f'  "bIsManaged": false,\n'
        f'  "bNeedsValidation": false,\n'
        f'  "bRequiresAuth": true,\n'
        f'  "bAllowMultipleInstances": false,\n'
        f'  "bCanRunOffline": true,\n'
        f'  "bAllowUriCmdArgs": false,\n'
        f'  "bLaunchElevated": false,\n'
        f'  "BaseURLs": [],\n'
        f'  "BuildLabel": "Live",\n'
        f'  "AppCategories": ["public", "games", "applications"],\n'
        f'  "ChunkDbs": [],\n'
        f'  "CompatibleApps": [],\n'
        f'  "DisplayName": "{display_name}",\n'
        f'  "InstallationGuid": "{installation_guid}",\n'
        f'  "InstallLocation": "{install_location}",\n'
        f'  "InstallSessionId": "",\n'
        f'  "InstallTags": [],\n'
        f'  "InstallComponents": [],\n'
        f'  "HostInstallationGuid": "00000000000000000000000000000000",\n'
        f'  "PrereqIds": [],\n'
        f'  "PrereqSHA1Hash": "",\n'
        f'  "LastPrereqSucceededSHA1Hash": "",\n'
        f'  "StagingLocation": "{install_location}/.egstore/bps",\n'
        f'  "TechnicalType": "public,games,applications",\n'
        f'  "VaultThumbnailUrl": "",\n'
        f'  "VaultTitleText": "",\n'
        f'  "InstallSize": 0,\n'
        f'  "MainWindowProcessName": "",\n'
        f'  "ProcessNames": [],\n'
        f'  "BackgroundProcessNames": [],\n'
        f'  "IgnoredProcessNames": [],\n'
        f'  "DlcProcessNames": [],\n'
        f'  "ExpectingDLCInstalled": {{}},\n'
        f'  "MandatoryAppFolderName": "{os.path.basename(install_location)}",\n'
        f'  "OwnershipToken": "true",\n'
        f'  "SidecarConfigRevision": 0,\n'
        f'  "PreloadState": 0,\n'
        f'  "CatalogNamespace": "{catalog_namespace}",\n'
        f'  "CatalogItemId": "{catalog_item_id}",\n'
        f'  "AppName": "{app_name}",\n'
        f'  "AppVersionString": "{app_version}",\n'
        f'  "MainGameCatalogNamespace": "{catalog_namespace}",\n'
        f'  "MainGameCatalogItemId": "{catalog_item_id}",\n'
        f'  "MainGameAppName": "{app_name}",\n'
        f'  "AllowedUriEnvVars": []\n'
        f'}}\n'
    )


# ---------------------------------------------------------------------------
# File creation helpers
# ---------------------------------------------------------------------------

def write_file(path: Path, content: str):
    """Write text content to a file, creating parent directories."""
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")
    print(f"  Created: {path.relative_to(MOCK_ROOT.parent.parent)}")


def write_binary_file(path: Path, size: int = 0):
    """Create a stub binary file (exe, dll). Writes a minimal valid PE-like marker."""
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(b"\x00" * max(size, 1))
    print(f"  Created: {path.relative_to(MOCK_ROOT.parent.parent)}")


# ---------------------------------------------------------------------------
# Mock data structure
# ---------------------------------------------------------------------------

def create_mock_tree():
    """Generate the complete mock directory tree."""
    if MOCK_ROOT.exists():
        print(f"Warning: {MOCK_ROOT} already exists. Will overwrite.")
    
    print(f"Creating mock game data at {MOCK_ROOT}...\n")

    # ---- Steam library root ----
    steam_root = MOCK_ROOT / "steam"
    steamapps = steam_root / "steamapps"
    common = steamapps / "common"

    # Game A: standard Steam game
    write_acf(steamapps / "appmanifest_12345.acf",
              appid="12345", name="Mock Game Alpha", installdir="MockGameAlpha")
    write_binary_file(common / "MockGameAlpha" / "GameAlpha.exe")
    write_file(common / "MockGameAlpha" / "steam_appid.txt", "12345\n")

    # Game B: Steam game with launcher exe (should be filtered)
    write_acf(steamapps / "appmanifest_67890.acf",
              appid="67890", name="Mock Game Beta", installdir="MockGameBeta")
    write_binary_file(common / "MockGameBeta" / "GameBeta.exe")
    write_binary_file(common / "MockGameBeta" / "GameBetaLauncher.exe")
    write_file(common / "MockGameBeta" / "steam_appid.txt", "67890\n")

    # Library folders VDF
    write_file(steamapps / "libraryfolders.vdf",
               make_libraryfolders_vdf([
                   (str(steam_root), "MockSteam", [("12345", "5000000000"), ("67890", "3000000000")]),
               ]))

    # ---- Epic game root ----
    epic_root = MOCK_ROOT / "epic"
    epic_game = epic_root / "EpicGameGamma"
    epic_manifest_dir = epic_game / ".egsstore" / "manifests"

    write_binary_file(epic_game / "GameGamma.exe")
    write_file(epic_manifest_dir / "abc123.item",
               make_epic_item(
                   display_name="Mock Epic Game Gamma",
                   app_name="MockEpicGamma",
                   install_location=str(epic_game),
                   launch_executable="GameGamma.exe",
                   catalog_namespace="ns_gamma_1234",
                   catalog_item_id="item_gamma_5678",
                   installation_guid="ABCDEF1234567890ABCDEF1234567890",
               ))

    # ---- Standalone games root ----
    standalone_root = MOCK_ROOT / "standalone"

    # Game C: normal standalone game
    write_binary_file(standalone_root / "StandaloneGameDelta" / "GameDelta.exe")
    write_binary_file(standalone_root / "StandaloneGameDelta" / "GameDeltaLauncher.exe")

    # Game D: Steam-emu tagged (steam_api64.dll marker)
    write_binary_file(standalone_root / "SteamEmuEpsilon" / "GameEpsilon.exe")
    write_binary_file(standalone_root / "SteamEmuEpsilon" / "steam_api64.dll")

    # Exe selection test: folder with larger anti-cheat exe
    ac_root = standalone_root / "AntiCheatZeta"
    write_binary_file(ac_root / "GameZeta.exe", size=50_000_000)
    write_binary_file(ac_root / "easyanticheat_setup.exe", size=100_000_000)
    write_binary_file(ac_root / "steam_api64.dll", size=1_000_000)

    # Non-game folder: no exe, no markers (should be filtered out)
    no_game = standalone_root / "_installer"
    write_binary_file(no_game / "setup.exe", size=200_000_000)
    write_binary_file(no_game / "vcredist_x64.exe", size=50_000_000)

    # Non-game folder: has exe but only non-game patterns (installer, setup)
    should_filter = standalone_root / "redist"
    write_binary_file(should_filter / "dxwebsetup.exe", size=5_000_000)
    write_binary_file(should_filter / "oalinst.exe", size=2_000_000)

    # Non-game folder: has no exe at all, no markers (should be excluded)
    docs_folder = standalone_root / "documentation"
    write_file(docs_folder / "readme.txt", "This is documentation.\n")

    # Container-like folder structure
    container_root = standalone_root / "PublisherCollection"
    sub_game = container_root / "SubGameEta"
    write_binary_file(sub_game / "GameEta.exe")
    write_file(sub_game / "steam_appid.txt", "99999\n")

    print("\nMock data tree created successfully.")
    print(f"Location: {MOCK_ROOT}")


def write_acf(path: Path, **kwargs):
    """Write an ACF file parsed with `make_acf()`."""
    content = make_acf(**kwargs)
    write_file(path, content)


# ---------------------------------------------------------------------------
# Cleanup
# ---------------------------------------------------------------------------

def clean_mock_tree():
    """Remove the entire mock data tree."""
    if MOCK_ROOT.exists():
        shutil.rmtree(MOCK_ROOT, onerror=_handle_remove_readonly)
        print(f"Removed: {MOCK_ROOT}")
    else:
        print(f"Nothing to clean — {MOCK_ROOT} does not exist.")


def _handle_remove_readonly(func, path, exc_info):
    """Handle permission errors on read-only files (e.g. .git files)."""
    os.chmod(path, stat.S_IWRITE)
    func(path)


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(description="Generate mock Windows game folder structure")
    parser.add_argument("--clean", action="store_true", help="Remove mock data tree")
    args = parser.parse_args()

    if args.clean:
        clean_mock_tree()
        sys.exit(0)

    create_mock_tree()


if __name__ == "__main__":
    main()
