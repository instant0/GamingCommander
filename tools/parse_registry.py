#!/usr/bin/env python3
"""
parse_registry.py — Parse Windows .reg files for launcher path discovery.

Windows registry (.reg) files store key-value pairs in a standard format:
  [HKEY_CURRENT_USER\\Software\\Valve\\Steam]
  "SteamPath"=str(2):"C:\\Program Files (x86)\\Steam"

This parser extracts launcher-specific paths from .reg files. It is used
for offline testing of the registry reading logic that GamingCommander
will use at runtime.

Usage:
    python tools/parse_registry.py data/mock/registry/steam.reg.txt    # UTF-8 copy
    python tools/parse_registry.py data/mock/registry/steam.reg        # UTF-16 LE
    python tools/parse_registry.py --all                               # parse all mock files
    python tools/parse_registry.py --test
    python tools/parse_registry.py --help
"""

import json
import os
import re
import sys
from pathlib import Path

# ---------------------------------------------------------------------------
# .reg file parser
# ---------------------------------------------------------------------------

# Pattern for key header: [HKEY_PATH]
KEY_PATTERN = re.compile(r'^\[(.+)]$')

# Pattern for string value: "ValueName"=str(2):"ValueData"
STRING_VALUE_PATTERN = re.compile(r'^"([^"]+)"\s*=\s*str\(2\):\s*"((?:[^"\\]|\\.)*)"\s*$')

# Pattern for dword value: "ValueName"=dword:00000001
DWORD_VALUE_PATTERN = re.compile(r'^"([^"]+)"\s*=\s*dword:\s*([0-9a-fA-F]+)\s*$')

# Pattern for hex value (multi-line): "ValueName"=hex:00,01,02,...
# We don't need hex values for launcher paths but we need to skip them
HEX_VALUE_START = re.compile(r'^"([^"]+)"\s*=\s*hex(?:\([0-9a-fA-F]+\))?:\s*(.*)$')


def parse_reg_file(text: str) -> dict[str, dict[str, str]]:
    """Parse a Windows .reg file into a dict of {key_path: {value_name: value_data}}.
    
    Handles:
      - UTF-16 LE and UTF-8 encoding (caller should open with correct encoding)
      - [KeyPath] headers
      - "ValueName"=str(2):"ValueData"  (expandable string)
      - "ValueName"=dword:hexvalue
      - Multi-line hex values (skipped, but not left hanging)
      - Comments (lines starting with ;)
      - Windows Registry Editor Version 5.00 header
    """
    result: dict[str, dict[str, str]] = {}
    current_key: str | None = None
    in_multi_line_hex = False
    
    for raw_line in text.splitlines():
        line = raw_line.strip()
        
        # Skip empty lines, comments, and version header
        if not line or line.startswith(';') or line.startswith('Windows Registry Editor'):
            continue
        
        # Handle multi-line hex value continuation
        if in_multi_line_hex:
            if line.endswith('\\'):
                continue  # continuation continues
            else:
                in_multi_line_hex = False  # this line ends the hex value
                continue
        
        # Key header
        m = KEY_PATTERN.match(line)
        if m:
            current_key = m.group(1)
            if current_key not in result:
                result[current_key] = {}
            continue
        
        if current_key is None:
            continue  # value before any key header, skip
        
        # String value
        m = STRING_VALUE_PATTERN.match(line)
        if m:
            name = m.group(1)
            data = m.group(2)
            # Unescape standard escape sequences
            data = data.replace('\\\\', '\\')
            data = data.replace('\\"', '"')
            result[current_key][name] = data
            continue
        
        # DWORD value
        m = DWORD_VALUE_PATTERN.match(line)
        if m:
            name = m.group(1)
            data = str(int(m.group(2), 16))
            result[current_key][name] = data
            continue
        
        # Hex value start (skip)
        m = HEX_VALUE_START.match(line)
        if m:
            name = m.group(1)
            remaining = m.group(2)
            # Check if it ends with backslash (multi-line continuation)
            if remaining.rstrip().endswith('\\'):
                in_multi_line_hex = True
            result[current_key][name] = "<hex>"
            continue
    
    return result


# ---------------------------------------------------------------------------
# Launcher-specific path extractors
# ---------------------------------------------------------------------------

def extract_steam_paths(parsed: dict[str, dict[str, str]]) -> dict:
    """Extract Steam paths from parsed registry data."""
    paths = {}
    
    steam_key = r"HKEY_CURRENT_USER\Software\Valve\Steam"
    if steam_key in parsed:
        key_data = parsed[steam_key]
        if "SteamPath" in key_data:
            paths["steam_install_path"] = key_data["SteamPath"]
        if "SteamExe" in key_data:
            paths["steam_exe_path"] = key_data["SteamExe"]
    
    return paths


def extract_epic_paths(parsed: dict[str, dict[str, str]]) -> dict:
    """Extract Epic Games Store paths from parsed registry data."""
    paths = {}
    
    epic_key = r"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Epic Games\EpicGamesLauncher"
    if epic_key in parsed:
        key_data = parsed[epic_key]
        if "AppDataPath" in key_data:
            paths["epic_manifest_dir"] = key_data["AppDataPath"] + "\\Manifests"
        if "InstallationPath" in key_data:
            paths["epic_install_path"] = key_data["InstallationPath"]
    
    return paths


def extract_gog_paths(parsed: dict[str, dict[str, str]]) -> dict:
    """Extract GOG Galaxy paths from parsed registry data."""
    paths = {}
    
    gog_key = r"HKEY_CURRENT_USER\Software\GOG.com\Galaxy"
    if gog_key in parsed:
        key_data = parsed[gog_key]
        if "InstallationPath" in key_data:
            paths["gog_install_path"] = key_data["InstallationPath"]
        if "LibraryPath" in key_data:
            paths["gog_library_path"] = key_data["LibraryPath"]
    
    return paths


def extract_ea_paths(parsed: dict[str, dict[str, str]]) -> dict:
    """Extract EA App paths from parsed registry data."""
    paths = {}
    
    ea_key = r"HKEY_CURRENT_USER\Software\Electronic Arts\EA Core"
    if ea_key in parsed:
        key_data = parsed[ea_key]
        if "InstallFolder" in key_data:
            paths["ea_install_path"] = key_data["InstallFolder"]
        if "GameInstallFolder" in key_data:
            paths["ea_games_path"] = key_data["GameInstallFolder"]
    
    return paths


def extract_ubi_paths(parsed: dict[str, dict[str, str]]) -> dict:
    """Extract Ubisoft Connect paths from parsed registry data."""
    paths = {}
    
    ubi_key = r"HKEY_LOCAL_MACHINE\SOFTWARE\Ubisoft\Launcher"
    if ubi_key in parsed:
        key_data = parsed[ubi_key]
        if "InstallPath" in key_data:
            paths["ubi_install_path"] = key_data["InstallPath"]
        if "GameInstallPath" in key_data:
            paths["ubi_games_path"] = key_data["GameInstallPath"]
    
    return paths


# ---------------------------------------------------------------------------
# Self-Test
# ---------------------------------------------------------------------------

SAMPLE_REG = """
Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\\Software\\Valve\\Steam]
"SteamPath"=str(2):"C:\\Program Files (x86)\\Steam"
"SteamExe"=str(2):"C:\\Program Files (x86)\\Steam\\steam.exe"
"LastConfigStore_HKLM_Saved_SteamPath"=str(2):"C:\\Program Files (x86)\\Steam"

[HKEY_CURRENT_USER\\Software\\Valve\\Steam\\Apps\\12345]
"Installed"=dword:00000001

[HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Epic Games\\EpicGamesLauncher]
"AppDataPath"=str(2):"C:\\ProgramData\\Epic\\EpicGamesLauncher\\Data"
"InstallationPath"=str(2):"C:\\Program Files (x86)\\Epic Games\\Launcher"
"""


def run_self_test():
    """Verify the parser can extract launcher paths from a .reg file."""
    print("=== Registry Parser — Self-Test ===", file=sys.stderr)
    print(file=sys.stderr)
    
    parsed = parse_reg_file(SAMPLE_REG)
    
    if not parsed:
        print("FAIL: No keys parsed", file=sys.stderr)
        return False
    
    # Test Steam path extraction
    steam_paths = extract_steam_paths(parsed)
    if steam_paths.get("steam_install_path") == "C:\\Program Files (x86)\\Steam":
        print("PASS: SteamPath extracted correctly", file=sys.stderr)
    else:
        print(f"FAIL: SteamPath = {steam_paths.get('steam_install_path')}", file=sys.stderr)
        return False
    
    # Test Epic path extraction
    epic_paths = extract_epic_paths(parsed)
    expected_epic_manifest = "C:\\ProgramData\\Epic\\EpicGamesLauncher\\Data\\Manifests"
    if epic_paths.get("epic_manifest_dir") == expected_epic_manifest:
        print("PASS: Epic manifest path extracted correctly", file=sys.stderr)
    else:
        print(f"FAIL: Epic manifest dir = {epic_paths.get('epic_manifest_dir')}", file=sys.stderr)
        return False
    
    # Test dword parsing
    steam_key = r"HKEY_CURRENT_USER\Software\Valve\Steam\Apps\12345"
    if steam_key in parsed and parsed[steam_key].get("Installed") == "1":
        print("PASS: DWORD value parsed correctly", file=sys.stderr)
    else:
        print(f"FAIL: DWORD value not parsed correctly", file=sys.stderr)
        return False
    
    print(file=sys.stderr)
    print("All tests passed.", file=sys.stderr)
    return True


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

HELP_TEXT = __doc__
MOCK_REG_DIR = Path(__file__).resolve().parent.parent / "data" / "mock" / "registry"


def main():
    if "--help" in sys.argv or "-h" in sys.argv:
        print(HELP_TEXT)
        sys.exit(0)
    
    if "--test" in sys.argv:
        success = run_self_test()
        sys.exit(0 if success else 1)
    
    if "--all" in sys.argv:
        # Parse all .reg files in the mock registry directory
        if not MOCK_REG_DIR.exists():
            print(f"Error: Mock registry directory not found: {MOCK_REG_DIR}", file=sys.stderr)
            print("Run 'python tools/generate_mock_registry.py' first.", file=sys.stderr)
            sys.exit(1)
        
        for reg_file in sorted(MOCK_REG_DIR.glob("*.reg.txt")):
            extractors = {
                "steam": extract_steam_paths,
                "epic": extract_epic_paths,
                "gog": extract_gog_paths,
                "ea": extract_ea_paths,
                "ubisoft": extract_ubi_paths,
            }
            
            launcher_name = reg_file.stem.replace(".reg", "")
            extractor = extractors.get(launcher_name)
            
            print(f"\n=== {reg_file.name} ===", file=sys.stderr)
            
            try:
                with open(reg_file, "r", encoding="utf-8") as f:
                    text = f.read()
            except Exception as e:
                print(f"  Error: {e}", file=sys.stderr)
                continue
            
            parsed = parse_reg_file(text)
            
            if extractor:
                paths = extractor(parsed)
                print(f"  Extracted paths:", file=sys.stderr)
                for key, val in paths.items():
                    print(f"    {key}: {val}", file=sys.stderr)
            else:
                print(f"  No extractor for {launcher_name}", file=sys.stderr)
            
            print(json.dumps(parsed, indent=2))
        
        sys.exit(0)
    
    if len(sys.argv) != 2:
        print("Usage: python parse_registry.py <path_to_.reg>", file=sys.stderr)
        print("       python parse_registry.py --all", file=sys.stderr)
        print("       python parse_registry.py --test", file=sys.stderr)
        sys.exit(1)
    
    filepath = sys.argv[1]
    
    if not os.path.isfile(filepath):
        print(f"Error: File not found: {filepath}", file=sys.stderr)
        sys.exit(1)
    
    # Try UTF-8 first, then UTF-16 LE
    for encoding in ["utf-8", "utf-16-le"]:
        try:
            with open(filepath, "r", encoding=encoding) as f:
                text = f.read()
            break
        except (UnicodeDecodeError, UnicodeError):
            continue
    else:
        print(f"Error: Could not read file with UTF-8 or UTF-16 LE", file=sys.stderr)
        sys.exit(1)
    
    parsed = parse_reg_file(text)
    print(json.dumps(parsed, indent=2))
    
    # Try to identify the launcher and extract paths
    print("\n-- Extracted Paths --", file=sys.stderr)
    for name, extractor in [("Steam", extract_steam_paths), ("Epic", extract_epic_paths)]:
        paths = extractor(parsed)
        if paths:
            print(f"  {name}:", file=sys.stderr)
            for key, val in paths.items():
                print(f"    {key}: {val}", file=sys.stderr)


if __name__ == "__main__":
    main()
