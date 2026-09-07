#!/usr/bin/env python3
"""Plan 123 P2.2 — selective folder-visit list generator (symptom-driven).

Reads testdata/samples/full-d.txt and emits the MINIMAL set of corpus folders that
need PHYSICAL probing on a Windows machine (PE metadata reads, store-signal checks,
manifest-content checks) to validate the §2.3 scenario catalog and the §1 symptoms.

The file listing answers structural questions already; physical probing is only
needed where the listing CANNOT provide evidence:
  - PE FileDescription/ProductName (title identity: S5/S8/S9)
  - store-manifest contents (.item/.mancpn/.info internals)
  - actual primary-exe resolution under the corrected model

Selection is curated per symptom — one or two folders each, not every folder.

Output: JSON [ { "label", "symptom", "scenario", "reason", "evidence_to_collect" } ]

Usage: python tools/selective_visits.py [--json]
"""

from __future__ import annotations

import collections
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
FIXTURE = ROOT / "testdata" / "samples" / "full-d.txt"

# Curated probes: (label_match, symptom, scenario, reason, evidence)
# label_match is matched case-insensitively as a substring of the 3rd-level folder label.
CURATED: list[dict] = [
    {
        "label": "penumbra", "symptom": "S4", "scenario": "S-F",
        "reason": "Only real exe (PENUMBRA.EXE) sits in redist/ — validate E5 "
                  "redist fallback admission and that super_secret/-Penumbra are rejected.",
        "evidence": ["PE FileDescription/ProductName of redist/PENUMBRA.EXE",
                     "primary-exe decision under corrected model"],
    },
    {
        "label": "elex", "symptom": "S3", "scenario": "S-E",
        "reason": "Game exe only in system/ — validate parent-wins (parent titled "
                  "ELEX, system child not promoted) and PE title resolution.",
        "evidence": ["PE title of system/ELEX.exe", "parent promotion status"],
    },
    {
        "label": "deadlight", "symptom": "S3", "scenario": "S-E",
        "reason": "Game exe only in win32/ (LOTDGame.exe) — second engine-subfolder "
                  "sample; PE title must resolve the parent game.",
        "evidence": ["PE title of win32/LOTDGame.exe", "parent promotion status"],
    },
    {
        "label": "mmxl", "symptom": "S5", "scenario": "S-H",
        "reason": "Folder acronym mmxl with 'Might and Magic X Legacy.exe' at root — "
                  "PE title must beat the acronym folder name.",
        "evidence": ["PE FileDescription of 'Might and Magic X Legacy.exe'"],
    },
    {
        "label": "jag2", "symptom": "S9", "scenario": "S-H",
        "reason": "ja2.exe at jag2/ root — PE title must resolve 'Jagged Alliance 2'; "
                  "also verify ja2UB/ variants are not selected.",
        "evidence": ["PE FileDescription/ProductName of jag2/ja2.exe"],
    },
    {
        "label": "endless", "symptom": "S8", "scenario": "S-H",
        "reason": "exe-stem 'endless' produced wrong PCGW list — validate exact-match "
                  "identity key on the real exe stem (dungeonoftheendless.exe).",
        "evidence": ["PE title", "exe stem used for lookup"],
    },
    {
        "label": "neverwinter", "symptom": "S2", "scenario": "S-D",
        "reason": "arc-install/neverwinter_en/neverwinter.exe — validate deep-nested "
                  "discovery and that the game entry is neverwinter, not arc-install.",
        "evidence": ["exe resolution from container child", "PE title"],
    },
    {
        "label": "arc-install", "symptom": "S1", "scenario": "S-C",
        "reason": "Container with 17 game children (incl. neverwinter_en) produced "
                  "zero entries — validate container-before-filter ordering and child "
                  "typing.",
        "evidence": ["child store signals", "child game exes", "container decision"],
    },
    {
        "label": "neverwinter_en", "symptom": "S2", "scenario": "S-D",
        "reason": "arc-install/neverwinter_en/neverwinter.exe — validate deep-nested "
                  "discovery: the game entry must be neverwinter, not arc-install.",
        "evidence": ["exe resolution from container child", "PE title"],
    },
    {
        "label": "epic games", "symptom": "S1", "scenario": "S-C",
        "reason": "Epic launcher container — validate children typed by own signals "
                  "(.egstore/.mancpn), parent never promoted.",
        "evidence": ["child .egstore/.mancpn presence", "child game list"],
    },
    {
        "label": "origin", "symptom": "S1", "scenario": "S-C",
        "reason": "EA container — validate __Installer child typing and that the "
                  "origin launcher folder itself is not a game entry.",
        "evidence": ["child __Installer signals", "launcher-vs-game discrimination"],
    },
    {
        "label": "monkey", "symptom": "S6", "scenario": "S-D",
        "reason": "Lookup searched folder name only (monkeyisland) — validate PE/"
                  "exe-stem-driven identity pipeline on the real install.",
        "evidence": ["PE title", "name-candidate order"],
    },
    {
        "label": "beneath a steel sky", "symptom": "S2", "scenario": "S-D",
        "reason": "Exe only under scummvm/ — validate engine-subfolder parent rule "
                  "on a non-system subfolder (scummvm is the engine, not the game).",
        "evidence": ["PE title", "parent-wins decision"],
    },
    {
        "label": "trapped", "symptom": "S3", "scenario": "S-E",
        "reason": "TrappedDead.exe only in bin/ — third engine-subfolder sample for "
                  "the platform-child rule.",
        "evidence": ["PE title of bin/TrappedDead.exe", "parent promotion status"],
    },
    {
        "label": "elexii", "symptom": "S3", "scenario": "S-E",
        "reason": "ELEX2.exe only in system/ with VC-redist noise — validate parent-"
                  "wins + redist rejection together.",
        "evidence": ["PE title of system/ELEX2.exe", "rejected redist list"],
    },
]


def _find_label_3rd_level(label: str) -> str | None:
    """Find a folder whose name contains the label (case-insensitive).
    Matches the 3rd-level label first; falls back to any deeper component.
    Returns the matched folder label or None."""
    want = label.lower()
    seen: set[str] = set()
    for raw in FIXTURE.read_text(encoding="utf-8", errors="replace").splitlines():
        ln = raw.rstrip("\n")
        if not ln.strip():
            continue
        parts = ln.replace("\\", "/").split("/")
        if len(parts) < 4:
            continue
        # Prefer 3rd-level; collect deeper matches as fallback
        third = parts[2]
        if want in third.lower() and third.lower() not in seen:
            seen.add(third.lower())
            return third
    for raw in FIXTURE.read_text(encoding="utf-8", errors="replace").splitlines():
        ln = raw.rstrip("\n")
        if not ln.strip():
            continue
        parts = ln.replace("\\", "/").split("/")
        if len(parts) < 4:
            continue
        for comp in parts[3:]:
            if want in comp.lower():
                return comp
    return None


def main() -> int:
    visits: list[dict] = []
    not_found: list[str] = []
    seen_labels: set[str] = set()
    for probe in CURATED:
        found_label = _find_label_3rd_level(probe["label"])
        if found_label is None:
            not_found.append(probe["label"])
            continue
        key = found_label.lower()
        if key in seen_labels:
            continue  # dedupe (e.g. neverwinter_en matched via S1 and S2)
        seen_labels.add(key)
        visits.append({
            "label": found_label,
            "symptom": probe["symptom"],
            "scenario": probe["scenario"],
            "reason": probe["reason"],
            "evidence_to_collect": probe["evidence"],
        })

    if "--json" in sys.argv:
        print(json.dumps(visits, indent=2))
    else:
        print(f"Selective visit list: {len(visits)} folders need physical probing\n")
        for v in visits:
            print(f"[{v['symptom']}/{v['scenario']}] {v['label']}")
            print(f"        {v['reason']}")

    if not_found:
        print(f"\nWARNING: {len(not_found)} curated labels not found in fixture: "
              f"{not_found}", file=sys.stderr)
    return 0


if __name__ == "__main__":
    sys.exit(main())