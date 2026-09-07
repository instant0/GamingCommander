#!/usr/bin/env python3
"""Plan 123 P2.3 — baseline divergence report.

Compares Python's PRODUCTION scanner (scan_directory) against the CORRECTED
model rendered by --probe, over the scenario corpus in testdata/mock/probe/.

Pure-Python analysis tool. It runs on real Linux fixture directories; the
Windows-style listing (testdata/samples/full-d.txt) is NOT consumed here —
that is the job of selective_visits.py (which already normalizes '\\' and '/').

The C# scanner is deliberately NOT part of this run: experiments happen in
Python first (Plan §2), and C# is ported in Phase 4.

Usage: python tools/baseline_compare.py [--json]
"""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PROBE_DIR = ROOT / "testdata" / "mock" / "probe"
DETECT = ROOT / "tools" / "detect.py"

# Scenario -> fixture dir (probe target for --probe) + expected corrected outcome.
# expected: (tier, primary_exe_or_None, store_or_None)
SCENARIOS: dict[str, dict] = {
    "S-A valid standalone": {
        "fixture": "GameAlpha", "tier": "Candidate", "primary": "GameAlpha.exe",
        "store": None,
    },
    "S-B store game (Epic)": {
        "fixture": "EpicGameGamma", "tier": "Secure", "primary": "GameGamma.exe",
        "store": "Epic",
    },
    "S-C container": {
        "fixture": "PublisherCollection", "tier": "Unknown", "primary": None,
        "store": None,  # parent not promoted; children are the entries
        "child_is_game": True,
    },
    "S-D deep-nested game": {
        "fixture": "Neverwinter/neverwinter_en", "tier": "Candidate",
        "primary": "neverwinter.exe", "store": None,
    },
    "S-E engine-subfolder game": {
        "fixture": "Elex", "tier": "Candidate", "primary": "system/ELEX.exe",
        "store": None,
    },
    "S-F redist-only (E5)": {
        "fixture": "Penumbra", "tier": "Candidate", "primary": "redist/PENUMBRA.EXE",
        "store": None,
    },
    "S-G multi-exe": {
        "fixture": "MultiRunner", "tier": "Candidate", "primary": "MultiRunner.exe",
        "store": None,
    },
    "S-H acronym title": {
        "fixture": "Mmxl", "tier": "Candidate", "primary": "Mmxl.exe",
        "store": None,
    },
    "S-I no signal": {
        "fixture": "NoSignal", "tier": "Unknown", "primary": None, "store": None,
    },
    "S-J false game folder": {
        "fixture": "SoundtrackFolder", "tier": "Unknown", "primary": None,
        "store": None,
    },
}


def run_scan(root: Path) -> list[dict]:
    """Run production scan_directory over the whole probe tree."""
    proc = subprocess.run(
        [sys.executable, str(DETECT), str(root), "--json"],
        capture_output=True, text=True, check=False,
    )
    if proc.returncode != 0:
        raise RuntimeError(f"scan_directory failed: {proc.stderr}")
    return json.loads(proc.stdout)


def run_probe(fixture: Path) -> dict:
    proc = subprocess.run(
        [sys.executable, str(DETECT), "--probe", str(fixture)],
        capture_output=True, text=True, check=False,
    )
    if proc.returncode != 0:
        raise RuntimeError(f"probe failed: {proc.stderr}")
    return json.loads(proc.stdout)


def scan_find(entries: list[dict], fixture: Path) -> dict | None:
    """Find the production scan entry for a probe target.

    Returns (entry, relation) where relation is:
      "exact"  — entry folder name == fixture name (game folder itself)
      "child"  — entry folder is a DEEPER path under the fixture (a child was
                 promoted, e.g. 'Elex/system' under fixture 'Elex')
      None     — no entry anywhere under the fixture
    """
    want = fixture.name.lower()
    for g in entries:
        folder = g["folder"].lower()
        parts = folder.split("/")
        if parts[-1] == want:
            return g, "exact"
    for g in entries:
        folder = g["folder"].lower()
        parts = folder.split("/")
        if any(p == want for p in parts):
            return g, "child"
    return None, None


def main() -> int:
    scan_entries = run_scan(PROBE_DIR)
    report: list[dict] = []

    for scenario, spec in SCENARIOS.items():
        fixture = PROBE_DIR / spec["fixture"]
        if not fixture.is_dir():
            report.append({"scenario": scenario, "error": "fixture missing"})
            continue

        probe = run_probe(fixture)
        entry, relation = scan_find(scan_entries, fixture)

        # Production result: does the folder appear as an entry? how is it titled?
        prod_found = entry is not None
        prod_folder = entry["folder"] if entry else None
        prod_store = entry["store"] if entry else None

        # Divergence classification vs corrected model.
        divergences: list[str] = []
        if relation == "child" and spec.get("child_is_game", False):
            # Container children ARE the games (S-C) — recursion worked. No divergence.
            pass
        elif relation == "child":
            # Production promoted a platform/build CHILD as the game
            # (Elex/system, Penumbra/redist) — the parent is the game (S3/S4).
            divergences.append(
                f"WRONG FOLDER (child '{prod_folder}' promoted as game, "
                f"expected '{fixture.name}')"
            )
        elif not prod_found and spec["tier"] in ("Candidate", "Secure", "Locked"):
            divergences.append("MISSED GAME")
        elif prod_found and spec["tier"] == "Unknown" and not spec["primary"]:
            # e.g. S-C parent: production must NOT promote the container parent
            divergences.append("FALSE POSITIVE (container parent promoted as game)")

        # Corrected-model reference (probe output).
        corrected_tier = probe.get("tier")
        corrected_primary = probe.get("primary_exe")

        report.append({
            "scenario": scenario,
            "fixture": spec["fixture"],
            "production_found": prod_found,
            "production_entry": prod_folder,
            "production_store": prod_store,
            "corrected_tier": corrected_tier,
            "corrected_primary": corrected_primary,
            "expected_tier": spec["tier"],
            "divergences": divergences or ["—"],
        })

    # ── Collection-with-stray-files regression (user scenario 2026-09-06) ──
    # A store collection folder (epicgames/) with launcher-residue files at its
    # root must still be recognized as a collection; its game children are the
    # entries, and the collection parent is NOT an entry.
    col_fixture = PROBE_DIR / "CollectionWithStray"
    # ── Collection-with-stray-files regression (user scenario 2026-09-06) ──
    # A store collection folder (epicgames/) with launcher-residue files at its
    # root must still be recognized as a collection; its game children are the
    # entries, and the collection parent is NOT an entry.
    col_failures: list[str] = []
    col_fixture = PROBE_DIR / "CollectionWithStray"
    if col_fixture.is_dir():
        col_scan = run_scan(col_fixture)
        col_entries = sorted(g["folder"] for g in col_scan)
        expected_col = ["epicgames/othergame", "epicgames/snuffbox"]
        col_ok = col_entries == expected_col
        print(f"{'PASS' if col_ok else 'FAIL'}  Collection w/ stray files      "
              f"entries={col_entries}")
        if not col_ok:
            col_failures.append(
                f"[collection w/ stray files] expected {expected_col} got {col_entries}"
            )

    # ── Deep-nested collection regression (user model 2026-09-06) ──
    # A collection whose games have DEEP exes (monsterhunter/win64/binaries/) —
    # each game's folder is the entry (parent wins), never its platform child.
    deep_fixture = PROBE_DIR / "CollectionDeepNest"
    if deep_fixture.is_dir():
        deep_scan = run_scan(deep_fixture)
        deep_entries = sorted(g["folder"] for g in deep_scan)
        expected_deep = ["epiccollection/game3", "epiccollection/monsterhunter",
                         "epiccollection/snuffbox"]
        deep_ok = deep_entries == expected_deep
        print(f"{'PASS' if deep_ok else 'FAIL'}  Collection deep-nested games  "
              f"entries={deep_entries}")
        if not deep_ok:
            col_failures.append(
                f"[collection deep-nested] expected {expected_deep} got {deep_entries}"
            )

    # Summary
    missed = sum(1 for r in report if any("MISSED GAME" in d for d in r["divergences"]))
    wrong_folder = sum(1 for r in report if any("WRONG FOLDER" in d for d in r["divergences"]))
    fp = sum(1 for r in report if any("FALSE POSITIVE" in d for d in r["divergences"]))

    if "--json" in sys.argv:
        print(json.dumps(report, indent=2))
    else:
        print("Plan 123 P2.3 — Python production scanner vs corrected model (probe)\n")
        print(f"{'Scenario':28s} {'Prod found':10s} {'Prod entry':30s} {'Corrected tier':14s} Divergence")
        for r in report:
            print(f"{r['scenario']:28s} {str(r['production_found']):10s} "
                  f"{(r['production_entry'] or '-'):30s} {str(r['corrected_tier']):14s} "
                  f"{', '.join(r['divergences'])}")
        print(f"\nSUMMARY: {len(report)} scenarios — {missed} missed games, "
              f"{wrong_folder} wrong-folder promotions, {fp} false positives")
    if col_failures:
        print("COLLECTION FAILURES:", *col_failures, file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())