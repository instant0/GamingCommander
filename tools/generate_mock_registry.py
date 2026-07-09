#!/usr/bin/env python3
"""
generate_mock_registry.py — Generate mock Windows registry (.reg) files for offline testing.

These .reg files simulate Windows registry keys that GamingCommander would read
at runtime to discover launcher installation paths. They are used by:
  - tools/parse_registry.py (Python validation)
  - C# unit tests (via mock data fixtures)

The .reg format is a standard Windows Registry Editor format:
  [KeyPath]
  "ValueName"=valuetype:ValueData

Usage:
    python tools/generate_mock_registry.py [--output-dir DIR]

Output:
    data/mock/registry/*.reg
"""

import argparse
import os
from pathlib import Path

# Default output directory is relative to this script's location
DEFAULT_OUTPUT = Path(__file__).resolve().parent.parent / "data" / "mock" / "registry"

# Base paths for mock data (relative to data/mock/)
MOCK_DATA_ROOT = Path(__file__).resolve().parent.parent / "data" / "mock"


def write_reg_file(path: Path, content: str):
    """Write a .reg file."""
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-16le")  # .reg files are UTF-16 LE
    # Also write a UTF-8 copy for easy reading on Linux
    utf8_path = path.with_suffix(".reg.txt")
    utf8_path.write_text(content, encoding="utf-8")
    print(f"  Created: {path}")
    print(f"  Created: {utf8_path} (UTF-8 copy for Linux)")


# ---------------------------------------------------------------------------
# Steam registry keys
# ---------------------------------------------------------------------------

def make_steam_reg(steam_path: str) -> str:
    """Generate HKCU Steam registry keys."""
    return (
        "Windows Registry Editor Version 5.00\n"
        "\n"
        "[HKEY_CURRENT_USER\\Software\\Valve\\Steam]\n"
        f'"SteamPath"=str(2):"{steam_path}"\n'
        f'"SteamExe"=str(2):"{steam_path}\\\\steam.exe"\n'
        '"LastConfigStore_HKLM_Saved_SteamPath"=str(2):"C:\\\\Program Files (x86)\\\\Steam"\n'
        "\n"
        "[HKEY_CURRENT_USER\\Software\\Valve\\Steam\\Apps\\12345]\n"
        '"Installed"=dword:00000001\n'
        '"AppType"=dword:00000000\n'
        "\n"
        "[HKEY_CURRENT_USER\\Software\\Valve\\Steam\\Apps\\67890]\n"
        '"Installed"=dword:00000001\n'
        '"AppType"=dword:00000000\n'
    )


# ---------------------------------------------------------------------------
# Epic Games Store registry keys
# ---------------------------------------------------------------------------

def make_epic_reg(epic_launcher_path: str) -> str:
    """Generate HKLM Epic Games Store registry keys."""
    return (
        "Windows Registry Editor Version 5.00\n"
        "\n"
        "[HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Epic Games\\EpicGamesLauncher]\n"
        f'"AppDataPath"=str(2):"{epic_launcher_path}\\\\EpicGamesLauncher\\\\Data"\n'
        f'"InstallationPath"=str(2):"{epic_launcher_path}\\\\Epic Games\\\\Launcher"\n'
        f'"LaunchCmd"=str(2):""{epic_launcher_path}\\\\Epic Games\\\\Launcher\\\\Portal\\\\Binaries\\\\Win32\\\\EpicGamesLauncher.exe" -OpenGL"\n'
        "\n"
        "[HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Epic Games\\EpicGamesLauncher\\Manifests]\n"
        f'"ManifestRoot"=str(2):"{epic_launcher_path}\\\\EpicGamesLauncher\\\\Data\\\\Manifests"\n'
    )


# ---------------------------------------------------------------------------
# GOG Galaxy registry keys
# ---------------------------------------------------------------------------

def make_gog_reg(gog_path: str) -> str:
    """Generate HKCU GOG Galaxy registry keys."""
    return (
        "Windows Registry Editor Version 5.00\n"
        "\n"
        "[HKEY_CURRENT_USER\\Software\\GOG.com]\n"
        f'"GameInstalls"=str(2):"{gog_path}\\\\Games"\n'
        "\n"
        "[HKEY_CURRENT_USER\\Software\\GOG.com\\Galaxy]\n"
        f'"InstallationPath"=str(2):"{gog_path}\\\\GOG Galaxy"\n'
        f'"LibraryPath"=str(2):"{gog_path}\\\\Games"\n'
        f'"ClientArgs"=str(2):""\n'
    )


# ---------------------------------------------------------------------------
# EA App registry keys
# ---------------------------------------------------------------------------

def make_ea_reg(ea_path: str) -> str:
    """Generate HKCU EA App registry keys."""
    return (
        "Windows Registry Editor Version 5.00\n"
        "\n"
        "[HKEY_CURRENT_USER\\Software\\Electronic Arts\\EA Core]\n"
        f'"InstallFolder"=str(2):"{ea_path}"\n'
        f'"GameInstallFolder"=str(2):"{ea_path}\\\\Games"\n'
        "\n"
        "[HKEY_CURRENT_USER\\Software\\Electronic Arts\\EA Desktop]\n"
        f'"AppManifestDirectory"=str(2):"{ea_path}\\\\EA Desktop\\\\Manifests"\n'
    )


# ---------------------------------------------------------------------------
# Ubisoft Connect registry keys
# ---------------------------------------------------------------------------

def make_ubi_reg(ubi_path: str) -> str:
    """Generate HKLM Ubisoft Connect registry keys."""
    return (
        "Windows Registry Editor Version 5.00\n"
        "\n"
        "[HKEY_LOCAL_MACHINE\\SOFTWARE\\Ubisoft\\Launcher]\n"
        f'"InstallPath"=str(2):"{ubi_path}\\\\Ubisoft Game Launcher"\n'
        f'"GameInstallPath"=str(2):"{ubi_path}\\\\Games"\n'
        f'"CachePath"=str(2):"{ubi_path}\\\\Ubisoft Game Launcher\\\\cache"\n'
        "\n"
        "[HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Ubisoft\\Launcher]\n"
        f'"InstallPath"=str(2):"{ubi_path}\\\\Ubisoft Game Launcher"\n'
        f'"GameInstallPath"=str(2):"{ubi_path}\\\\Games"\n'
    )


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(description="Generate mock Windows registry .reg files")
    parser.add_argument("--output-dir", type=str, default=str(DEFAULT_OUTPUT),
                        help=f"Output directory (default: {DEFAULT_OUTPUT})")
    parser.add_argument("--clean", action="store_true", help="Remove output directory")
    args = parser.parse_args()

    output_dir = Path(args.output_dir)
    
    if args.clean:
        import shutil
        if output_dir.exists():
            shutil.rmtree(output_dir)
            print(f"Removed: {output_dir}")
        else:
            print(f"Nothing to clean — {output_dir} does not exist.")
        return

    # Base paths — point to our mock game folders
    mock_steam = str(MOCK_DATA_ROOT / "steam")
    mock_epic = str(MOCK_DATA_ROOT / "epic")
    mock_gog = str(MOCK_DATA_ROOT / "gog")
    mock_ea = str(MOCK_DATA_ROOT / "ea")
    mock_ubi = str(MOCK_DATA_ROOT / "ubi")

    print(f"Generating mock registry files in {output_dir}...\n")

    write_reg_file(output_dir / "steam.reg", make_steam_reg(mock_steam))
    write_reg_file(output_dir / "epic.reg", make_epic_reg(mock_epic))
    write_reg_file(output_dir / "gog.reg", make_gog_reg(mock_gog))
    write_reg_file(output_dir / "ea.reg", make_ea_reg(mock_ea))
    write_reg_file(output_dir / "ubisoft.reg", make_ubi_reg(mock_ubi))

    print(f"\n{5} registry files created in {output_dir}")
    print("Note: .reg files are UTF-16 LE (Windows standard).")
    print("      .reg.txt copies are UTF-8 for easy reading on Linux.")


if __name__ == "__main__":
    main()
