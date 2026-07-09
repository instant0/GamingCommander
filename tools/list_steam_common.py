#!/usr/bin/env python3
"""
list_steam_common.py — Cross-reference ACF installdir with common/ folder names.

Objectives:
  - Validate that every ACF's `installdir` maps to an actual folder under
    `steamapps/common/`.
  - Identify orphaned folders (in `common/` but no matching ACF) and missing
    folders (ACF has `installdir` but no folder exists).
  - Provide the foundation for locating game files on disk for migration.

Usage:
    python tools/list_steam_common.py /mnt/p/Program Files (x86)/Steam/steamapps
    python tools/list_steam_common.py --test

Output:
    JSON cross-reference report to stdout. Human summary to stderr.

Privacy: This script reads local machine data for validation only. Output
should not be committed to documentation; use only for development verification.

Pathing: The input path is a Linux/WSL mount point. ACF `installdir` values
are folder names only (no path separators), so cross-platform issues do not
arise at this level.
"""

import json
import os
import sys
import glob

# Reuse the ACF parser from task 1
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
from tools.parse_steam_acf import parse_acf, extract_required


def scan_steamapps(steamapps_path):
    """Scan a steamapps/ directory and cross-reference ACFs with common/ folders.

    Args:
        steamapps_path: Path to the steamapps/ directory (e.g. on WSL:
                        /mnt/p/Program Files (x86)/Steam/steamapps)

    Returns:
        dict with:
          - acf_files: list of found ACF filenames
          - acf_entries: list of extracted ACF data (appid, name, installdir)
          - common_folders: list of folder names in common/
          - matched: list of installdir values that have a matching folder
          - missing_folders: list of installdir values with NO matching folder
          - orphaned_folders: list of folder names with NO matching ACF
    """
    result = {
        "library_path": steamapps_path,
        "acf_files": [],
        "common_folders": [],
        "acf_entries": [],
        "matched": [],
        "missing_folders": [],
        "orphaned_folders": [],
    }

    # --- List ACF files ---
    acf_pattern = os.path.join(steamapps_path, "appmanifest_*.acf")
    acf_paths = sorted(glob.glob(acf_pattern))
    result["acf_files"] = [os.path.basename(p) for p in acf_paths]

    # --- List common/ folders ---
    common_path = os.path.join(steamapps_path, "common")
    if os.path.isdir(common_path):
        result["common_folders"] = sorted([
            d for d in os.listdir(common_path)
            if os.path.isdir(os.path.join(common_path, d))
        ])

    # --- Extract from each ACF ---
    for acf_path in acf_paths:
        try:
            with open(acf_path, "r", encoding="utf-8") as f:
                text = f.read()
            parsed = parse_acf(text)
            extracted = extract_required(parsed)
            if extracted:
                result["acf_entries"].append({
                    "appid": extracted["appid"],
                    "name": extracted["name"],
                    "installdir": extracted["installdir"],
                })
        except Exception:
            pass  # Skip unparseable ACFs

    # --- Cross-reference ---
    common_set = set(result["common_folders"])
    installdirs = {e["installdir"] for e in result["acf_entries"]}

    result["matched"] = sorted(installdirs & common_set)
    result["missing_folders"] = sorted(installdirs - common_set)
    result["orphaned_folders"] = sorted(common_set - installdirs)

    return result


def print_report(report):
    """Print human-readable cross-reference report to stderr."""
    total = len(report["acf_entries"])
    matched = len(report["matched"])
    missing = len(report["missing_folders"])
    orphaned = len(report["orphaned_folders"])

    print("=== Steam ACF ↔ common/ Cross-Reference Report ===", file=sys.stderr)
    print(f"  Library: {report['library_path']}", file=sys.stderr)
    print(f"  ACF files found: {len(report['acf_files'])}", file=sys.stderr)
    print(f"  ACFs parsed OK:  {total}", file=sys.stderr)
    print(f"  common/ folders:  {len(report['common_folders'])}", file=sys.stderr)
    print(file=sys.stderr)

    print(f"  Matched:          {matched} — installdir has a folder", file=sys.stderr)
    print(f"  Missing folders:  {missing} — installdir has NO folder", file=sys.stderr)
    print(f"  Orphaned folders: {orphaned} — folder has no ACF", file=sys.stderr)
    print(file=sys.stderr)

    if report["missing_folders"]:
        print("  -- Missing folders (ACF expects these under common/) --", file=sys.stderr)
        for name in report["missing_folders"]:
            print(f"    {name}", file=sys.stderr)
        print(file=sys.stderr)

    if report["orphaned_folders"]:
        print("  -- Orphaned folders (in common/ but no matching ACF) --", file=sys.stderr)
        for name in report["orphaned_folders"]:
            print(f"    {name}", file=sys.stderr)
        print(file=sys.stderr)

    # Identification summary
    print("-- IDENTIFICATION (ACF → installdir → common/ folder) --", file=sys.stderr)
    for entry in report["acf_entries"]:
        status = "✓" if entry["installdir"] in report["matched"] else "✗"
        print(f"  {status} appid={entry['appid']:>10}  installdir={entry['installdir']}", file=sys.stderr)


# ---------------------------------------------------------------------------
# Self-Test
# ---------------------------------------------------------------------------

SAMPLE_STEAMAPPS = None  # Not applicable; uses real path or explicit args

def run_self_test():
    """Run validation against synthetic data to verify cross-reference logic."""
    import tempfile
    import io

    print("=== Steam common/ Cross-Reference — Self-Test ===", file=sys.stderr)
    print(file=sys.stderr)

    # Create temp directory structure
    with tempfile.TemporaryDirectory() as tmpdir:
        steamapps = os.path.join(tmpdir, "steamapps")
        common = os.path.join(steamapps, "common")
        os.makedirs(common)

        # Create a few fake game folders
        for folder in ["Game A", "Game B", "Game C"]:
            os.makedirs(os.path.join(common, folder))

        # Create matching ACF files
        acf_a = '''"AppState"\n{\n\t"appid"\t"12345"\n\t"name"\t"Game A"\n\t"installdir"\t"Game A"\n\t"StateFlags"\t"4"\n\t"LastUpdated"\t"1000000"\n\t"SizeOnDisk"\t"1000"\n\t"buildid"\t"1"\n}'''
        acf_b = '''"AppState"\n{\n\t"appid"\t"67890"\n\t"name"\t"Game B"\n\t"installdir"\t"Game B"\n\t"StateFlags"\t"4"\n\t"LastUpdated"\t"2000000"\n\t"SizeOnDisk"\t"2000"\n\t"buildid"\t"2"\n}'''
        # ACF for a game with no matching folder
        acf_d = '''"AppState"\n{\n\t"appid"\t"99999"\n\t"name"\t"Game D (missing)"\n\t"installdir"\t"Game D"\n\t"StateFlags"\t"4"\n\t"LastUpdated"\t"3000000"\n\t"SizeOnDisk"\t"3000"\n\t"buildid"\t"3"\n}'''

        for name, content in [("appmanifest_12345.acf", acf_a), ("appmanifest_67890.acf", acf_b), ("appmanifest_99999.acf", acf_d)]:
            with open(os.path.join(steamapps, name), "w") as f:
                f.write(content)

        report = scan_steamapps(steamapps)

    # Validate
    checks = 0
    if report["matched"] == ["Game A", "Game B"]:
        print("  PASS: Matched A → B have folders", file=sys.stderr)
        checks += 1
    else:
        print(f"  FAIL: expected [Game A, Game B], got {report['matched']}", file=sys.stderr)

    if report["missing_folders"] == ["Game D"]:
        print("  PASS: Missing folder detected: Game D", file=sys.stderr)
        checks += 1
    else:
        print(f"  FAIL: expected [Game D], got {report['missing_folders']}", file=sys.stderr)

    if report["orphaned_folders"] == ["Game C"]:
        print("  PASS: Orphaned folder detected: Game C", file=sys.stderr)
        checks += 1
    else:
        print(f"  FAIL: expected [Game C], got {report['orphaned_folders']}", file=sys.stderr)

    print(file=sys.stderr)
    all_pass = checks == 3
    print(f"Result: {'ALL PASS' if all_pass else 'SOME FAILED'} ({checks}/3)", file=sys.stderr)
    return all_pass


# ---------------------------------------------------------------------------
# CLI Entry Point
# ---------------------------------------------------------------------------

def main():
    if "--test" in sys.argv:
        success = run_self_test()
        sys.exit(0 if success else 1)

    if "--help" in sys.argv or "-h" in sys.argv or len(sys.argv) != 2:
        print(__doc__)
        sys.exit(0 if "--help" in sys.argv else 1)

    steamapps_path = sys.argv[1]

    if not os.path.isdir(steamapps_path):
        print(f"Error: Not a directory: {steamapps_path}", file=sys.stderr)
        sys.exit(1)

    report = scan_steamapps(steamapps_path)

    # JSON to stdout
    print(json.dumps(report, indent=2))

    # Human summary to stderr
    print_report(report)


if __name__ == "__main__":
    main()
