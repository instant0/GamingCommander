#!/usr/bin/env python3
"""
discover_steam_libraries.py — Enumerate Steam library paths from libraryfolders.vdf.

Objectives:
  - Parse libraryfolders.vdf to discover all Steam library root paths.
  - Provide the list of valid target locations for game migration.
  - Understand that migration works by placing ACFs in a library's steamapps/
    folder — Steam rescans and updates libraryfolders.vdf on restart.

Usage:
    python tools/discover_steam_libraries.py <path_to_libraryfolders.vdf>
    python tools/discover_steam_libraries.py <path> --wsl-prefix /mnt
    python tools/discover_steam_libraries.py --test

Output:
    JSON list of discovered libraries to stdout. Human summary to stderr.

Pathing: libraryfolders.vdf contains Windows-style paths (backslash separators).
The script stores them as-is. For WSL validation, use --wsl-prefix to
specify a mount point mapping (e.g. --wsl-prefix /mnt).
"""

import json
import os
import re
import sys

# ---------------------------------------------------------------------------
# Minimal VDF parser (subset: flat key-value inside indexed blocks)
# ---------------------------------------------------------------------------

def parse_vdf_libraryfolders(text):
    """Parse libraryfolders.vdf and extract library entries.

    Handles the format where keys and braces can be on separate lines:
      "libraryfolders"
      {
          "0"
          {
              "path"   "C:\\..."
              "apps"
              {
                  "730"   "12345"
              }
          }
      }

    Returns a dict of {index: {path, label, contentid, totalsize, apps}}.
    """

    def consume_line(lines, idx):
        """Get the next non-empty line, return (stripped_line, new_idx)."""
        while idx < len(lines):
            line = lines[idx].strip()
            idx += 1
            if line:
                return line, idx
        return "", idx

    def consume_brace(lines, idx):
        """Skip whitespace then expect a '{'."""
        line, idx = consume_line(lines, idx)
        if line != '{':
            raise ValueError(f"Expected '{{' at line, got: {line}")
        return idx

    def expect(line, pattern):
        return re.match(pattern, line)

    lines = text.splitlines()
    idx = 0
    libraries = {}

    # Consume root key
    line, idx = consume_line(lines, idx)  # "libraryfolders"
    # Consume root opening brace
    idx = consume_brace(lines, idx)

    # Parse numbered library entries
    while idx < len(lines):
        line, idx = consume_line(lines, idx)
        if not line or line == '}':
            break

        # Match "index" optionally followed by {
        m = expect(line, r'^"(\d+)"\s*\{?$')
        if not m:
            continue

        lib_index = m.group(1)

        # If brace not on same line, consume it
        if not line.rstrip().endswith('{'):
            idx = consume_brace(lines, idx)

        lib_data = {
            "path": "", "label": "", "contentid": "",
            "totalsize": "", "apps": {}
        }

        # Parse inside this library block
        while idx < len(lines):
            inner, idx = consume_line(lines, idx)
            if inner == '}':
                break

            # "key" "value" pair
            kv = expect(inner, r'^"([^"]+)"\s+"([^"]*)"$')
            if kv:
                key, val = kv.group(1), kv.group(2)
                lib_data[key] = val
                continue

            # "apps" { ... } or "apps" then { on next line
            if expect(inner, r'^"apps"\s*\{?$'):
                if not inner.rstrip().endswith('{'):
                    idx = consume_brace(lines, idx)

                while idx < len(lines):
                    app_line, idx = consume_line(lines, idx)
                    if app_line == '}':
                        break
                    akv = expect(app_line, r'^"(\d+)"\s+"(\d+)"$')
                    if akv:
                        lib_data["apps"][akv.group(1)] = akv.group(2)
                continue

        libraries[lib_index] = lib_data

    return libraries


# ---------------------------------------------------------------------------
# Path conversion helpers
# ---------------------------------------------------------------------------

def win_to_wsl_path(win_path, wsl_prefix="/mnt"):
    """Convert a Windows path to a WSL path for runtime validation.

    Handles both P:\\path and P:\\\path (single or double backslash from VDF).
    Strips leading slash from the remainder so joining doesn't double up.
    Returns None if the path doesn't look like a Windows path.
    """
    m = re.match(r'^([A-Za-z]):\\(.*)$', win_path)
    if not m:
        return None
    drive = m.group(1).lower()
    rest = m.group(2)
    # Normalize backslashes to forward slashes
    rest = rest.replace("\\", "/")
    # Strip leading slash in case VDF had double backslashes
    rest = rest.lstrip("/")
    # Collapse any consecutive slashes from double-backslash artifacts
    while "//" in rest:
        rest = rest.replace("//", "/")
    return f"{wsl_prefix}/{drive}/{rest}"


# ---------------------------------------------------------------------------
# Library Discovery
# ---------------------------------------------------------------------------

def discover_libraries(vdf_path, wsl_prefix=None):
    """Read libraryfolders.vdf and return discovered library info.

    Args:
        vdf_path: Path to libraryfolders.vdf
        wsl_prefix: If set (e.g. "/mnt"), also resolve WSL paths for validation

    Returns:
        dict with libraries list and summary counts
    """
    with open(vdf_path, "r", encoding="utf-8") as f:
        text = f.read()

    libraries = parse_vdf_libraryfolders(text)

    result = {
        "vdf_path": vdf_path,
        "library_count": len(libraries),
        "libraries": [],
        "wsl_valid": [],
        "wsl_invalid": [],
    }

    for index in sorted(libraries.keys(), key=int):
        lib = libraries[index]
        entry = {
            "index": int(index),
            "path": lib.get("path", ""),
            "label": lib.get("label", ""),
            "totalsize": int(lib.get("totalsize", 0)),
            "app_count": len(lib.get("apps", {})),
        }
        result["libraries"].append(entry)

        # WSL path resolution for validation
        if wsl_prefix:
            wsl_path = win_to_wsl_path(entry["path"], wsl_prefix)
            if wsl_path and os.path.isdir(wsl_path):
                entry["wsl_path"] = wsl_path
                entry["wsl_valid"] = True
                result["wsl_valid"].append(wsl_path)
            else:
                entry["wsl_path"] = wsl_path or "N/A"
                entry["wsl_valid"] = False
                result["wsl_invalid"].append(entry["path"])

    return result


# ---------------------------------------------------------------------------
# Self-Test
# ---------------------------------------------------------------------------

def run_self_test():
    """Validate the VDF parser against synthetic libraryfolders data."""
    print("=== Steam Library Discovery — Self-Test ===", file=sys.stderr)

    sample = '''"libraryfolders"
{
    "0"
    {
        "path"      "C:\\Program Files (x86)\\Steam"
        "label"     ""
        "contentid" "12345"
        "totalsize" "0"
        "apps"
        {
            "730"   "43686442816"
            "240"   "123456789"
        }
    }
    "1"
    {
        "path"      "D:\\SteamLibrary"
        "label"     "Games"
        "contentid" "67890"
        "totalsize" "1000000000000"
        "apps"
        {
            "440"   "5000000000"
        }
    }
}'''

    libraries = parse_vdf_libraryfolders(sample)
    checks = 0

    if len(libraries) == 2:
        print("  PASS: Found 2 libraries", file=sys.stderr)
        checks += 1
    else:
        print(f"  FAIL: Expected 2 libraries, got {len(libraries)}", file=sys.stderr)

    path0 = libraries.get("0", {}).get("path")
    if path0 == "C:\\Program Files (x86)\\Steam":
        print("  PASS: Library 0 path correct", file=sys.stderr)
        checks += 1
    else:
        print(f"  FAIL: Library 0 path wrong: {repr(path0)}", file=sys.stderr)

    path1 = libraries.get("1", {}).get("path")
    if path1 == "D:\\SteamLibrary":
        print("  PASS: Library 1 path correct", file=sys.stderr)
        checks += 1
    else:
        print(f"  FAIL: Library 1 path wrong: {repr(path1)}", file=sys.stderr)

    if len(libraries.get("0", {}).get("apps", {})) == 2:
        print("  PASS: Library 0 has 2 apps", file=sys.stderr)
        checks += 1
    else:
        print(f"  FAIL: Library 0 apps count wrong", file=sys.stderr)

    if len(libraries.get("1", {}).get("apps", {})) == 1:
        print("  PASS: Library 1 has 1 app", file=sys.stderr)
        checks += 1
    else:
        print(f"  FAIL: Library 1 apps count wrong", file=sys.stderr)

    # Test path conversion
    wsl = win_to_wsl_path("P:\\Program Files (x86)\\Steam")
    if wsl == "/mnt/p/Program Files (x86)/Steam":
        print("  PASS: win_to_wsl_path single backslash", file=sys.stderr)
        checks += 1
    else:
        print(f"  FAIL: win_to_wsl_path got {repr(wsl)}", file=sys.stderr)

    wsl2 = win_to_wsl_path("P:\\\\Program Files (x86)\\\\Steam")
    if wsl2 == "/mnt/p/Program Files (x86)/Steam":
        print("  PASS: win_to_wsl_path double backslash", file=sys.stderr)
        checks += 1
    else:
        print(f"  FAIL: win_to_wsl_path double got {repr(wsl2)}", file=sys.stderr)

    all_pass = checks == 7
    print(f"\nResult: {'ALL PASS' if all_pass else 'SOME FAILED'} ({checks}/7)", file=sys.stderr)
    return all_pass


# ---------------------------------------------------------------------------
# CLI Entry Point
# ---------------------------------------------------------------------------

def main():
    if "--test" in sys.argv:
        success = run_self_test()
        sys.exit(0 if success else 1)

    if "--help" in sys.argv or "-h" in sys.argv:
        print(__doc__)
        sys.exit(0)

    # Parse --wsl-prefix and the path argument
    wsl_prefix = None
    positional_args = []
    i = 1
    while i < len(sys.argv):
        arg = sys.argv[i]
        if arg == "--wsl-prefix":
            i += 1
            if i < len(sys.argv) and not sys.argv[i].startswith("--"):
                wsl_prefix = sys.argv[i]
            i += 1
        elif arg.startswith("--wsl-prefix="):
            wsl_prefix = arg.split("=", 1)[1]
            i += 1
        elif arg.startswith("-"):
            print(f"Unknown option: {arg}", file=sys.stderr)
            sys.exit(1)
        else:
            positional_args.append(arg)
            i += 1

    if len(positional_args) != 1:
        print("Usage: python discover_steam_libraries.py <path_to_libraryfolders.vdf> [--wsl-prefix /mnt]", file=sys.stderr)
        print("       python discover_steam_libraries.py --test", file=sys.stderr)
        sys.exit(1)

    vdf_path = positional_args[0]

    if not os.path.isfile(vdf_path):
        print(f"Error: File not found: {vdf_path}", file=sys.stderr)
        sys.exit(1)

    try:
        result = discover_libraries(vdf_path, wsl_prefix)
    except Exception as e:
        print(f"Error parsing VDF: {e}", file=sys.stderr)
        sys.exit(1)

    # JSON to stdout
    print(json.dumps(result, indent=2, default=str))

    # Human summary to stderr
    print(f"\n=== Steam Library Discovery ===", file=sys.stderr)
    print(f"  VDF file:    {vdf_path}", file=sys.stderr)
    print(f"  Libraries:   {result['library_count']}", file=sys.stderr)
    print(file=sys.stderr)
    for lib in result["libraries"]:
        label = f" ({lib['label']})" if lib.get("label") else ""
        wsl_note = ""
        if "wsl_valid" in lib:
            wsl_note = " [WSL: " + ("OK" if lib.get("wsl_valid") else "MISSING") + "]"
        print(f"  [{lib['index']}]{wsl_note} {lib['path']}{label} — {lib['app_count']} apps", file=sys.stderr)


if __name__ == "__main__":
    main()
