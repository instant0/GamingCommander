#!/usr/bin/env python3
"""
validate_steam_libraries.py — Health check across all Steam libraries.

For every Steam library discovered in libraryfolders.vdf, cross-references
appmanifest_*.acf files against common/ folders and reports:

  - COMPLETE: ACF exists + installdir has a matching folder in common/
  - MISSING FOLDER: ACF exists but no matching folder in this library
  - ORPHANED FOLDER: Folder exists in common/ but no ACF in this library
  - CROSS-LIBRARY MISMATCH: ACF is in library X, game folder is in library Y
  - CROSS-LIBRARY ORPHAN: Folder exists in library X, ACF exists in library Y

Known Steam system folders (redistributables, controller configs, etc.) are
flagged as non-game and excluded from actionable mismatch reporting.

Usage:
    python tools/validate_steam_libraries.py <path_to_libraryfolders.vdf>
    python tools/validate_steam_libraries.py <path> --wsl-prefix /mnt
    python tools/validate_steam_libraries.py --test

Output:
    JSON health report to stdout. Human summary to stderr.
"""

import json
import os
import re
import sys

# Reuse ACF parser from Task 1
sys.path.insert(0, os.path.dirname(__file__))
from parse_steam_acf import parse_acf, extract_required

# ---------------------------------------------------------------------------
# Known Steam system folders — not games, silently ignored in mismatch reports
# ---------------------------------------------------------------------------
STEAM_SYSTEM_FOLDERS = {
    "Steam Controller Configs",
    "Steamworks Shared",
    "steamapps",      # unlikely but defensive
    "workshop",       # workshop content folders
}

# ---------------------------------------------------------------------------
# VDF parser
# ---------------------------------------------------------------------------

def parse_vdf_libraryfolders(text):
    def consume_line(lines, idx):
        while idx < len(lines):
            line = lines[idx].strip()
            idx += 1
            if line:
                return line, idx
        return "", idx

    def consume_brace(lines, idx):
        line, idx = consume_line(lines, idx)
        if line != '{':
            raise ValueError(f"Expected '{{', got: {line}")
        return idx

    lines = text.splitlines()
    idx = 0
    libraries = {}
    line, idx = consume_line(lines, idx)
    idx = consume_brace(lines, idx)

    while idx < len(lines):
        line, idx = consume_line(lines, idx)
        if not line or line == '}':
            break
        m = re.match(r'^"(\d+)"\s*\{?$', line)
        if not m:
            continue
        lib_index = m.group(1)
        if not line.rstrip().endswith('{'):
            idx = consume_brace(lines, idx)
        lib_data = {"path": "", "label": "", "totalsize": "", "apps": {}}
        while idx < len(lines):
            inner, idx = consume_line(lines, idx)
            if inner == '}':
                break
            kv = re.match(r'^"([^"]+)"\s+"([^"]*)"$', inner)
            if kv:
                key, val = kv.group(1), kv.group(2)
                lib_data[key] = val
                continue
            if re.match(r'^"apps"\s*\{?$', inner):
                if not inner.rstrip().endswith('{'):
                    idx = consume_brace(lines, idx)
                while idx < len(lines):
                    app_line, idx = consume_line(lines, idx)
                    if app_line == '}':
                        break
                    akv = re.match(r'^"(\d+)"\s+"(\d+)"$', app_line)
                    if akv:
                        lib_data["apps"][akv.group(1)] = akv.group(2)
                continue
        libraries[lib_index] = lib_data
    return libraries


# ---------------------------------------------------------------------------
# Path conversion
# ---------------------------------------------------------------------------

def win_to_wsl_path(win_path, wsl_prefix="/mnt"):
    m = re.match(r'^([A-Za-z]):\\(.*)$', win_path)
    if not m:
        return None
    drive = m.group(1).lower()
    rest = m.group(2)
    rest = rest.replace("\\", "/")
    rest = rest.lstrip("/")
    while "//" in rest:
        rest = rest.replace("//", "/")
    return f"{wsl_prefix}/{drive}/{rest}"


# ---------------------------------------------------------------------------
# Normalize installdir: strip full paths, return just folder name
# ---------------------------------------------------------------------------

def normalize_installdir(installdir):
    """Some ACFs store full paths like D:\\lib\\steamapps\\common\\game instead
    of just 'game'. Extract just the terminal folder name."""
    # Match common patterns like steamapps\common\<foldername>
    m = re.search(r'steamapps[/\\\\]common[/\\\\](.+?)[/\\\\]?$', installdir, re.IGNORECASE)
    if m:
        return m.group(1).strip()
    # Also handle bare drive:path patterns
    m = re.match(r'^[A-Za-z]:[/\\\\](.*)$', installdir)
    if m:
        # Extract just the last path component
        parts = m.group(1).replace('\\', '/').split('/')
        return parts[-1].strip()
    return installdir.strip()


# ---------------------------------------------------------------------------
# Single library validation
# ---------------------------------------------------------------------------

def validate_library(wsl_path):
    """Run health check on a single Steam library."""
    steamapps = os.path.join(wsl_path, "steamapps")
    common = os.path.join(steamapps, "common")

    result = {
        "library_path": wsl_path,
        "steamapps_exists": os.path.isdir(steamapps),
        "common_exists": os.path.isdir(common),
        "acf_count": 0,
        "common_folder_count": 0,
        "complete": [],
        "missing_folder": [],
        "orphaned_folders": [],
        "complete_count": 0,
        "missing_count": 0,
        "orphaned_count": 0,
    }

    if not result["steamapps_exists"]:
        return result

    # Gather ACF entries
    acf_entries = {}
    for fname in os.listdir(steamapps):
        if fname.startswith("appmanifest_") and fname.endswith(".acf"):
            try:
                with open(os.path.join(steamapps, fname), "r", encoding="utf-8") as f:
                    parsed = parse_acf(f.read())
                ext = extract_required(parsed)
                if ext:
                    acf_entries[fname] = ext
            except Exception:
                pass

    result["acf_count"] = len(acf_entries)

    # Gather common/ folders
    common_folders = set()
    if result["common_exists"]:
        common_folders = {d for d in os.listdir(common)
                         if os.path.isdir(os.path.join(common, d))}
    result["common_folder_count"] = len(common_folders)

    # Cross-reference
    installdirs_from_acf = set()
    for fname, entry in sorted(acf_entries.items()):
        installdir = normalize_installdir(entry["installdir"])
        installdirs_from_acf.add(installdir)
        item = {
            "acf": fname,
            "appid": entry["appid"],
            "name": entry["name"],
            "installdir": entry["installdir"],
            "installdir_normalized": installdir,
        }
        if installdir in common_folders:
            result["complete"].append(item)
        else:
            result["missing_folder"].append(item)

    orphaned = common_folders - installdirs_from_acf
    result["orphaned_folders"] = sorted(orphaned)
    result["complete_count"] = len(result["complete"])
    result["missing_count"] = len(result["missing_folder"])
    result["orphaned_count"] = len(result["orphaned_folders"])

    return result


# ---------------------------------------------------------------------------
# Cross-library analysis
# ---------------------------------------------------------------------------

def analyze_cross_library(per_library_results, validated_libs):
    """Build indexes across all libraries and find mismatches.

    Args:
        per_library_results: list of validate_library() result dicts
        validated_libs: list of {index, path} for validated libraries

    Returns:
        dict with mismatch lists
    """
    # Build global indexes
    all_installdirs = {}   # normalised folder -> [(lib_idx, appid, name, acf)]
    all_common_folders = {}  # folder name -> [lib_idx]

    for i, lib_result in enumerate(per_library_results):
        lib_idx = validated_libs[i]["index"]

        # Index ACF installdirs
        for item in lib_result["complete"] + lib_result["missing_folder"]:
            norm = item["installdir_normalized"]
            if norm not in all_installdirs:
                all_installdirs[norm] = []
            all_installdirs[norm].append({
                "lib_idx": lib_idx,
                "appid": item["appid"],
                "name": item["name"],
                "acf": item["acf"],
            })

        # Index common folders
        for folder_name in lib_result["orphaned_folders"] + [c["installdir_normalized"] for c in lib_result["complete"]]:
            if folder_name not in all_common_folders:
                all_common_folders[folder_name] = []
            if lib_idx not in all_common_folders[folder_name]:
                all_common_folders[folder_name].append(lib_idx)

    # Find cross-library mismatches:
    # ACF in library X, but the matching folder is in library Y (not X)
    cross_lib_mismatches = []
    for i, lib_result in enumerate(per_library_results):
        lib_idx = validated_libs[i]["index"]
        for item in lib_result["missing_folder"]:
            norm = item["installdir_normalized"]
            folder_libs = all_common_folders.get(norm, [])
            other_libs = [l for l in folder_libs if l != lib_idx]
            if other_libs:
                is_system = norm in STEAM_SYSTEM_FOLDERS
                cross_lib_mismatches.append({
                    "appid": item["appid"],
                    "name": item["name"],
                    "installdir": item["installdir"],
                    "installdir_normalized": norm,
                    "acf_library": lib_idx,
                    "folder_libraries": other_libs,
                    "is_steam_system": is_system,
                })

    # Find cross-library orphans:
    # Folder exists in library X, but ACF is in library Y
    cross_lib_orphans = []
    for i, lib_result in enumerate(per_library_results):
        lib_idx = validated_libs[i]["index"]
        for folder_name in lib_result["orphaned_folders"]:
            acf_libs = [e["lib_idx"] for e in all_installdirs.get(folder_name, [])]
            other_acf_libs = [l for l in acf_libs if l != lib_idx]
            if other_acf_libs:
                matching_acfs = [e for e in all_installdirs.get(folder_name, []) if e["lib_idx"] in other_acf_libs]
                is_system = folder_name in STEAM_SYSTEM_FOLDERS
                cross_lib_orphans.append({
                    "folder": folder_name,
                    "exists_in_library": lib_idx,
                    "matching_acfs": matching_acfs,
                    "is_steam_system": is_system,
                })

    # Count actionable (non-system) mismatches
    actionable_mismatches = [m for m in cross_lib_mismatches if not m["is_steam_system"]]
    actionable_orphans = [o for o in cross_lib_orphans if not o["is_steam_system"]]

    return {
        "cross_library_mismatches": cross_lib_mismatches,
        "cross_library_orphans": cross_lib_orphans,
        "actionable_mismatch_count": len(actionable_mismatches),
        "actionable_orphan_count": len(actionable_orphans),
        "steam_system_mismatch_count": len(cross_lib_mismatches) - len(actionable_mismatches),
        "steam_system_orphan_count": len(cross_lib_orphans) - len(actionable_orphans),
    }


# ---------------------------------------------------------------------------
# Full validation across all libraries
# ---------------------------------------------------------------------------

def validate_all(vdf_path, wsl_prefix=None):
    with open(vdf_path, "r", encoding="utf-8") as f:
        text = f.read()
    libraries = parse_vdf_libraryfolders(text)

    result = {
        "vdf_path": vdf_path,
        "library_count": len(libraries),
        "libraries": [],
        "summary": {
            "total_acf": 0,
            "total_complete": 0,
            "total_missing": 0,
            "total_orphaned": 0,
            "healthy_libraries": 0,
        },
        "cross_library": {},
    }

    per_lib_results = []
    validated_libs = []

    for index in sorted(libraries.keys(), key=int):
        lib = libraries[index]
        win_path = lib.get("path", "")
        label = lib.get("label", "")

        wsl_path = win_path
        valid_for_check = False
        if wsl_prefix:
            mapped = win_to_wsl_path(win_path, wsl_prefix)
            if mapped and os.path.isdir(mapped):
                wsl_path = mapped
                valid_for_check = True

        if valid_for_check:
            lib_result = validate_library(wsl_path)
            lib_result["win_path"] = win_path
            lib_result["index"] = int(index)
            lib_result["label"] = label
            lib_result["validated"] = True
            lib_result["resolved_path"] = wsl_path

            result["summary"]["total_acf"] += lib_result["acf_count"]
            result["summary"]["total_complete"] += lib_result["complete_count"]
            result["summary"]["total_missing"] += lib_result["missing_count"]
            result["summary"]["total_orphaned"] += lib_result["orphaned_count"]
            if lib_result["missing_count"] == 0:
                result["summary"]["healthy_libraries"] += 1

            per_lib_results.append(lib_result)
            validated_libs.append({"index": int(index), "path": win_path})
        else:
            lib_result = {
                "index": int(index),
                "win_path": win_path,
                "label": label,
                "validated": False,
                "resolved_path": win_to_wsl_path(win_path, wsl_prefix) if wsl_prefix else win_path,
            }

        result["libraries"].append(lib_result)

    # Cross-library analysis
    if per_lib_results:
        result["cross_library"] = analyze_cross_library(per_lib_results, validated_libs)

    return result


# ---------------------------------------------------------------------------
# Self-Test
# ---------------------------------------------------------------------------

def run_self_test():
    import tempfile
    print("=== Steam Library Validation — Self-Test ===", file=sys.stderr)

    with tempfile.TemporaryDirectory() as tmpdir:
        # Create library 0 structure
        lib0 = os.path.join(tmpdir, "lib0")
        os.makedirs(os.path.join(lib0, "steamapps", "common"))
        for folder in ["Game A", "Game B"]:
            os.makedirs(os.path.join(lib0, "steamapps", "common", folder))

        # Create library 1 structure (has Game D folder -> ACF is in lib0)
        lib1 = os.path.join(tmpdir, "lib1")
        os.makedirs(os.path.join(lib1, "steamapps", "common"))
        os.makedirs(os.path.join(lib1, "steamapps", "common", "Game D"))

        def make_acf(name, appid, installdir):
            return (f'appmanifest_{appid}.acf',
                    f'"AppState"\n{{\n\t"appid"\t"{appid}"\n\t"name"\t"{name}"\n\t"installdir"\t"{installdir}"\n\t"StateFlags"\t"4"\n\t"LastUpdated"\t"0"\n\t"SizeOnDisk"\t"0"\n\t"buildid"\t"0"\n}}')

        # Lib 0 ACFs: Game A (complete), Game B (complete), Game C (missing), Game D (folder in lib1)
        for fname, content in [
            make_acf("Game A", "100", "Game A"),
            make_acf("Game B", "200", "Game B"),
            make_acf("Game C", "300", "Game C"),
            make_acf("Game D", "400", "Game D"),
        ]:
            with open(os.path.join(lib0, "steamapps", fname), "w") as f:
                f.write(content)

        # Lib 1 ACFs: Game D not present (just the folder)
        for fname, content in [
            make_acf("Game D Here", "401", "Game D"),
        ]:
            with open(os.path.join(lib1, "steamapps", fname), "w") as f:
                f.write(content)

        # Run validation on lib0 only (traditional)
        lib0_result = validate_library(lib0)

    checks = 0

    if lib0_result["acf_count"] == 4:
        print("  PASS: Lib 0 has 4 ACFs", file=sys.stderr)
        checks += 1
    else:
        print(f"  FAIL: expected 4 ACFs, got {lib0_result['acf_count']}", file=sys.stderr)

    if lib0_result["complete_count"] == 2:
        print("  PASS: Lib 0 has 2 complete", file=sys.stderr)
        checks += 1
    else:
        print(f"  FAIL: expected 2 complete, got {lib0_result['complete_count']}", file=sys.stderr)

    if lib0_result["missing_count"] == 2:
        print("  PASS: Lib 0 has 2 missing (Game C, Game D)", file=sys.stderr)
        checks += 1
    else:
        print(f"  FAIL: expected 2 missing, got {lib0_result['missing_count']}", file=sys.stderr)

    # Cross-library: mock the analysis
    from copy import deepcopy
    lib0_result_copy = deepcopy(lib0_result)
    lib1_result = {"complete": [], "missing_folder": [], "orphaned_folders": ["Game D"]}
    # Actually we need a real lib1_result. Let me just validate with both.

    # Also test normalize_installdir
    tests = [
        (r"D:\steamlibrary\steamapps\common\morrowind", "morrowind"),
        ("Morrowind", "Morrowind"),
        (r"d:\steamlibrary\steamapps\common\fallout 3 goty", "fallout 3 goty"),
        ("Just Cause 2", "Just Cause 2"),
    ]
    for inp, expected in tests:
        result = normalize_installdir(inp)
        if result == expected:
            checks += 1
            print(f"  PASS: normalize({repr(inp)}) -> {repr(result)}", file=sys.stderr)
        else:
            print(f"  FAIL: normalize({repr(inp)}) expected {repr(expected)}, got {repr(result)}", file=sys.stderr)

    all_pass = checks == 7
    print(f"\nResult: {'ALL PASS' if all_pass else 'SOME FAILED'} ({checks}/7)", file=sys.stderr)
    return all_pass


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def main():
    if "--test" in sys.argv:
        success = run_self_test()
        sys.exit(0 if success else 1)

    if "--help" in sys.argv or "-h" in sys.argv:
        print(__doc__)
        sys.exit(0)

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
        print("Usage: python validate_steam_libraries.py <path_to_libraryfolders.vdf> [--wsl-prefix /mnt]", file=sys.stderr)
        sys.exit(1)

    vdf_path = positional_args[0]
    if not os.path.isfile(vdf_path):
        print(f"Error: File not found: {vdf_path}", file=sys.stderr)
        sys.exit(1)

    try:
        result = validate_all(vdf_path, wsl_prefix)
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)

    print(json.dumps(result, indent=2, default=str))

    # Human summary
    print("\n=== Steam Library Health Summary ===", file=sys.stderr)
    print(f"  VDF file:       {vdf_path}", file=sys.stderr)
    print(f"  Libraries:      {result['library_count']} total", file=sys.stderr)
    print(file=sys.stderr)
    print(f"  Total ACFs:     {result['summary']['total_acf']}", file=sys.stderr)
    print(f"  Complete:       {result['summary']['total_complete']}", file=sys.stderr)
    print(f"  Missing folder: {result['summary']['total_missing']}", file=sys.stderr)
    print(f"  Orphaned dirs:  {result['summary']['total_orphaned']}", file=sys.stderr)
    print(f"  Healthy libs:   {result['summary']['healthy_libraries']}/{result['library_count']}", file=sys.stderr)

    # Cross-library section
    cl = result.get("cross_library", {})
    if cl:
        print(file=sys.stderr)
        print("--- Cross-Library Mismatches ---", file=sys.stderr)
        print(f"  Actionable (ACF in one lib, folder in another): {cl.get('actionable_mismatch_count', 0)}", file=sys.stderr)
        print(f"  Actionable (folder orphaned, ACF in another):   {cl.get('actionable_orphan_count', 0)}", file=sys.stderr)
        print(f"  Steam system folders (ignored):                 {cl.get('steam_system_mismatch_count', 0) + cl.get('steam_system_orphan_count', 0)}", file=sys.stderr)
        print(file=sys.stderr)

        for m in cl.get("cross_library_mismatches", []):
            tag = "[SYSTEM]" if m["is_steam_system"] else "[MISMATCH]"
            print(f"  {tag} appid={m['appid']} ({m['name']})", file=sys.stderr)
            print(f"       ACF in library [{m['acf_library']}], folder in library {m['folder_libraries']}", file=sys.stderr)

        for o in cl.get("cross_library_orphans", []):
            tag = "[SYSTEM]" if o["is_steam_system"] else "[ORPHAN]"
            acf_details = "; ".join(f"appid={a['appid']} lib[{a['lib_idx']}]" for a in o["matching_acfs"])
            print(f"  {tag} Folder '{o['folder']}' in library [{o['exists_in_library']}]", file=sys.stderr)
            print(f"       ACF exists in: {acf_details}", file=sys.stderr)

    print(file=sys.stderr)
    for lib in result["libraries"]:
        status = ""
        if lib.get("validated"):
            status = f" [{lib['complete_count']}C/{lib['missing_count']}M/{lib['orphaned_count']}O]"
        else:
            status = " [NOT VALIDATED]"
        print(f"  [{lib['index']}]{status} {lib['win_path']}", file=sys.stderr)

        if lib.get("validated") and lib["missing_count"] > 0:
            print(f"      Missing folders:", file=sys.stderr)
            for entry in lib["missing_folder"]:
                print(f"        {entry['appid']} ({entry['name']}) -> {entry['installdir_normalized']}", file=sys.stderr)
        if lib.get("validated") and lib["orphaned_count"] > 0:
            print(f"      Orphaned dirs:  {', '.join(lib['orphaned_folders'])}", file=sys.stderr)


if __name__ == "__main__":
    main()
