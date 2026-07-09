#!/usr/bin/env python3
"""
parse_steam_acf.py — Minimal Steam ACF parser for game identification & migration.

Objectives (from phase-1.2.md):
  1. IDENTIFICATION: Extract appid, name, installdir to uniquely identify a game
     and know where it lives on disk.
  2. MIGRATION SUPPORT: Extract StateFlags, LastUpdated, SizeOnDisk, buildid to
     validate game state and enable manifest-aware migration.

Format: Valve Data Format (VDF) / ACF — quoted "key" "value" pairs and nested
"key" { ... } blocks. Only top-level flat fields under "AppState" are needed.

Usage:
    python tools/parse_steam_acf.py data/appmanifest_228980.acf
    python tools/parse_steam_acf.py --test           # self-test with embedded data
    python tools/parse_steam_acf.py --help           # this message

Output:
    JSON with extracted fields to stdout. Non-zero exit on failure.
    Human-readable summary to stderr.

Privacy: This script runs on local sample data only. Output should never contain
concrete game names or paths when used for research documentation.

Pathing: ACF files contain Windows-style paths (backslash separators). This script
treats them as opaque strings — no path interpretation or manipulation.
"""

import json
import sys
import os

# ---- Required fields for identification and migration support ----
REQUIRED_FIELDS = [
    "appid",           # IDENTIFICATION: Unique Steam App ID
    "name",            # IDENTIFICATION: Display name
    "installdir",      # IDENTIFICATION: Folder name under steamapps/common/
    "StateFlags",      # MIGRATION: Bitmask (4=fully installed)
    "LastUpdated",     # MIGRATION: Unix timestamp of last update
    "SizeOnDisk",      # MIGRATION: Bytes on disk (for space validation)
    "buildid",         # MIGRATION: Current build (for version tracking)
]

# ---------------------------------------------------------------------------
# ACF Parser
# ---------------------------------------------------------------------------

def _parse_quoted(line, pos):
    """Extract a quoted string starting at position pos.
       Returns (value, new_pos) or raises ValueError."""
    if pos >= len(line) or line[pos] != '"':
        raise ValueError(f"Expected '\"' at position {pos}")
    pos += 1
    chars = []
    while pos < len(line):
        ch = line[pos]
        if ch == '"':
            pos += 1
            return "".join(chars), pos
        if ch == '\\' and pos + 1 < len(line):
            chars.append(line[pos + 1])
            pos += 2
        else:
            chars.append(ch)
            pos += 1
    raise ValueError("Unterminated string")


def _skip_whitespace(line, pos):
    """Advance past whitespace. Returns new_pos."""
    while pos < len(line) and line[pos] in (' ', '\t'):
        pos += 1
    return pos


def parse_acf(text):
    """Parse ACF text into a nested dict.

    This is a minimal recursive descent parser for the subset of VDF used
    by Steam ACF files. It handles:
      - "key" "value" pairs
      - "key" { ... } nested blocks
      - Mixed indentation (tabs/spaces)
      - Empty lines
    """

    def _parse_value(lines, idx):
        """Parse a value: either a quoted string or a '{' block."""
        line = lines[idx]
        pos = _skip_whitespace(line, 0)
        if pos < len(line) and line[pos] == '{':
            return _parse_block(lines, idx + 1)
        val, _ = _parse_quoted(line, pos)
        return val

    def _parse_block(lines, idx):
        """Parse content inside { ... } back to a dict. Returns (dict, next_line_idx)."""
        result = {}
        while idx < len(lines):
            line = lines[idx].strip()
            idx += 1
            if not line:
                continue
            if line == '}':
                return result, idx
            # Split on first quoted pair "key" separated from value
            try:
                pos = _skip_whitespace(line, 0)
                key, pos = _parse_quoted(line, pos)
                pos = _skip_whitespace(line, pos)
                if pos < len(line) and line[pos] == '{':
                    # Block value
                    _, inner_idx = _parse_value(lines, idx - 1)
                    # We don't need to recurse into blocks for required fields
                    # but we need to skip them. Reconstruct the line and parse properly.
                    result[key], idx = _parse_block(lines, idx)
                else:
                    val, _ = _parse_quoted(line, pos)
                    result[key] = val
            except (ValueError, IndexError):
                # Skip unparseable lines silently
                pass
        return result, idx

    lines = text.splitlines()
    result, _ = _parse_block(lines, 0)
    return result


# ---------------------------------------------------------------------------
# Field Extraction
# ---------------------------------------------------------------------------

def extract_required(data):
    """Extract only the fields needed for identification and migration.

    Args:
        data: Parsed ACF structure (nested dict).

    Returns:
        dict with required fields, or None if critical fields missing.

    Note:
        Usually the outer key is "AppState". We handle both cases: if the
        root dict has exactly one key whose value is a dict, we descend into it.
    """
    # Navigate into root "AppState" if present
    app_state = data
    if len(data) == 1 and isinstance(next(iter(data.values())), dict):
        app_state = next(iter(data.values()))

    extracted = {field: app_state.get(field) for field in REQUIRED_FIELDS}
    # appid is critical — without it we cannot identify the game
    if not extracted.get("appid"):
        return None
    return extracted


# ---------------------------------------------------------------------------
# Self-Test
# ---------------------------------------------------------------------------

SAMPLE_ACF = '''"AppState"
{
    "appid"     "228980"
    "name"      "Steamworks Common Redistributables"
    "installdir"    "Steamworks Shared"
    "StateFlags"    "4"
    "LastUpdated"   "1767792399"
    "SizeOnDisk"    "1189331096"
    "buildid"   "19222509"
    "InstalledDepots"
    {
        "228981" { "manifest" "7613356809904826842" "size" "5884085" }
    }
}
'''

def run_self_test():
    """Verify the parser can extract required fields and meet the two objectives."""
    print("=== Steam ACF Parser — Self-Test ===", file=sys.stderr)
    print(file=sys.stderr)

    # Parse
    parsed = parse_acf(SAMPLE_ACF)
    extracted = extract_required(parsed)

    if extracted is None:
        print("FAIL: Could not extract required fields", file=sys.stderr)
        return False

    # --- Objective 1: Identification ---
    print("Objective 1 — IDENTIFICATION:", file=sys.stderr)
    id_ok = all(extracted.get(f) for f in ["appid", "name", "installdir"])
    if id_ok:
        print(f"  PASS: appid={extracted['appid']}, name={extracted['name']}, installdir={extracted['installdir']}", file=sys.stderr)
    else:
        print(f"  FAIL: missing one or more identification fields", file=sys.stderr)
        return False

    # --- Objective 2: Migration Support ---
    print("Objective 2 — MIGRATION SUPPORT:", file=sys.stderr)
    mig_fields = ["StateFlags", "LastUpdated", "SizeOnDisk", "buildid"]
    mig_ok = all(extracted.get(f) is not None for f in mig_fields)
    if mig_ok:
        print(f"  PASS: StateFlags={extracted['StateFlags']}, LastUpdated={extracted['LastUpdated']}, SizeOnDisk={extracted['SizeOnDisk']}, buildid={extracted['buildid']}", file=sys.stderr)
    else:
        print(f"  FAIL: missing one or more migration fields", file=sys.stderr)
        return False

    # --- Output ---
    print(file=sys.stderr)
    print("All objectives met. Extracted data:", file=sys.stderr)
    print(json.dumps(extracted, indent=2))
    return True


# ---------------------------------------------------------------------------
# CLI Entry Point
# ---------------------------------------------------------------------------

HELP_TEXT = __doc__


def main():
    if "--help" in sys.argv or "-h" in sys.argv:
        print(HELP_TEXT)
        sys.exit(0)

    if "--test" in sys.argv:
        success = run_self_test()
        sys.exit(0 if success else 1)

    if len(sys.argv) != 2:
        print("Usage: python parse_steam_acf.py <path_to_appmanifest.acf>", file=sys.stderr)
        print("       python parse_steam_acf.py --test", file=sys.stderr)
        sys.exit(1)

    filepath = sys.argv[1]

    if not os.path.isfile(filepath):
        print(f"Error: File not found: {filepath}", file=sys.stderr)
        sys.exit(1)

    try:
        with open(filepath, "r", encoding="utf-8") as f:
            text = f.read()
    except Exception as e:
        print(f"Error reading file: {e}", file=sys.stderr)
        sys.exit(1)

    parsed = parse_acf(text)
    extracted = extract_required(parsed)

    if extracted is None:
        print("Error: Could not extract required fields (check file format)", file=sys.stderr)
        sys.exit(1)

    # Machine-readable output to stdout
    print(json.dumps(extracted, indent=2))

    # Human-readable summary to stderr
    print("\n-- IDENTIFICATION --", file=sys.stderr)
    print(f"  App ID:       {extracted['appid']}", file=sys.stderr)
    print(f"  Name:         {extracted['name']}", file=sys.stderr)
    print(f"  Install Dir:  {extracted['installdir']}", file=sys.stderr)
    print("-- MIGRATION SUPPORT --", file=sys.stderr)
    print(f"  State Flags:  {extracted['StateFlags']}", file=sys.stderr)
    print(f"  Last Updated: {extracted['LastUpdated']}", file=sys.stderr)
    print(f"  Size On Disk: {extracted['SizeOnDisk']}", file=sys.stderr)
    print(f"  Build ID:     {extracted['buildid']}", file=sys.stderr)


if __name__ == "__main__":
    main()
