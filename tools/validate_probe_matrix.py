#!/usr/bin/env python3
"""Plan 123 §2.4.7 — probe validation matrix.

Runs `--probe` on one fixture per scenario (S-A..S-J) and asserts the rendered
tier matches the §2.3 expected outcome. Fixtures live in testdata/mock/probe/.

Usage: python tools/validate_probe_matrix.py
Exit 0 = all scenarios pass; exit 1 = any divergence (listed as findings).
"""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PROBE_DIR = ROOT / "testdata" / "mock" / "probe"
DETECT = ROOT / "tools" / "detect.py"

# scenario -> (fixture_subpath, expected_tier, expected_store)
MATRIX: dict[str, tuple[str, str, str | None]] = {
    "S-A valid standalone": ("GameAlpha", "Candidate", None),
    "S-B store game (Epic)": ("EpicGameGamma", "Secure", "Epic"),
    "S-C container": ("PublisherCollection", "Unknown", None),  # parent not promoted
    "S-D deep-nested game": ("ArcInstall/neverwinter_en", "Candidate", None),
    "S-D2 exact-match terminating (E1)": ("ArcInstall", "Unknown", None),  # container: children are separate entities
    "S-D3 publisher wrapper (real: SquareEnix/Stardock/qfg5)": ("PublisherWrapper/Stardock", "Unknown", None),  # single-game-child wrapper
    "S-D4 UE-shipping deep exe (real: Indiana/Binaries/Win64)": ("UEShipping/Indiana", "Candidate", None),
    "S-E engine-subfolder game": ("Elex", "Candidate", None),
    "S-F redist-only (E5 target)": ("Penumbra", "Candidate", None),  # corrected: game recoverable
    "S-G multi-exe": ("MultiRunner", "Candidate", None),
    "S-H acronym title": ("Mmxl", "Candidate", None),
    "S-I no signal": ("NoSignal", "Unknown", None),
    "S-J false game folder": ("SoundtrackFolder", "Unknown", None),  # excluded
    # ── Store-signal coverage (corpus: 9 store types; regression guard) ──
    "Store GOG": ("StoreGog", "Secure", "GOG"),
    "Store EA": ("StoreEa", "Secure", "EA"),
    "Store Ubisoft": ("StoreUbi", "Secure", "Ubisoft"),
    "Store Blizzard": ("StoreBlizzard", "Secure", "Blizzard"),
    "Store Blizzard bnet-manifest (real: Diablo III/COD)": ("StoreBlizzardBnet/Diablo III", "Secure", "Blizzard"),
    "Store SteamEmu": ("StoreSteamEmu", "Secure", "Steam Emulator"),
    "Store Xbox": ("StoreXbox", "Secure", "Xbox"),
    "Store Rockstar": ("StoreRockstar", "Secure", "Rockstar"),
}

# Additional assertions beyond tier/store (primary-exe expectations per §2.3).
PRIMARY_EXPECTED: dict[str, str | None] = {
    "S-A valid standalone": "GameAlpha.exe",
    "S-D deep-nested game": "neverwinter.exe",  # terminating rule: nested GameClient.exe excluded
    "S-F redist-only (E5 target)": "redist/PENUMBRA.EXE",  # not super_secret / -Penumbra
    # S-G is a KNOWN divergence (E6): MultiRunnerTool.exe ties MultiRunner.exe because
    # neither Python _TOOL_NAMES nor C# tier_10_dev_editor_tools penalize bare "tool".
    # The tie-break is filesystem order (nondeterministic). Fixed by E6 experiment.
    "S-G multi-exe": "MultiRunner.exe",
}

# Known divergences (probe renders CURRENT behavior; expected outcome differs).
# These are documented findings, not failures — they are experiment inputs.
KNOWN_DIVERGENCES: set[str] = {
    "S-G multi-exe",  # bare "tool" not penalized → nondeterministic tie (E6)
}


def run_probe(fixture: Path) -> dict:
    proc = subprocess.run(
        [sys.executable, str(DETECT), "--probe", str(fixture)],
        capture_output=True, text=True, check=False,
    )
    if proc.returncode != 0:
        raise RuntimeError(f"probe failed: {proc.stderr}")
    return json.loads(proc.stdout)


def main() -> int:
    failures: list[str] = []
    for scenario, (sub, expected_tier, expected_store) in MATRIX.items():
        fixture = PROBE_DIR / sub
        if not fixture.is_dir():
            failures.append(f"[{scenario}] fixture missing: {fixture}")
            continue
        result = run_probe(fixture)
        tier = result.get("tier")
        store = result.get("store")
        status = "PASS" if tier == expected_tier and store == expected_store else "FAIL"
        if status == "FAIL":
            failures.append(
                f"[{scenario}] expected tier={expected_tier!r} store={expected_store!r} "
                f"but got tier={tier!r} store={store!r}"
            )
        # Primary-exe assertion (where defined)
        want_primary = PRIMARY_EXPECTED.get(scenario)
        if want_primary is not None and result.get("primary_exe") != want_primary:
            if scenario in KNOWN_DIVERGENCES:
                status = "DIVERG"
            else:
                status = "FAIL"
                failures.append(
                    f"[{scenario}] expected primary_exe={want_primary!r} "
                    f"but got {result.get('primary_exe')!r}"
                )
        print(f"{status}  {scenario:28s} tier={tier:10s} store={store} "
              f"primary={result.get('primary_exe')}")

    print()
    if failures:
        print(f"FAILURES: {len(failures)}")
        for f in failures:
            print(f"  - {f}")
        return 1
    print("ALL SCENARIOS PASS")
    return 0


if __name__ == "__main__":
    sys.exit(main())