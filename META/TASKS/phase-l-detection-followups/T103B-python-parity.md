# Task T103B: Python Parity + Probe Fixture

**Tier:** 3 — Logic/Behavior
**Phase:** L — Detection Follow-ups
**Risk:** Medium
**Status:** completed
**Prerequisites:** Task T103B depends on Task T103A

## Objective

Mirror the Steam-library rule in the Python reference and prove it through the probe matrix. No C#
changes and no docs edits in this segment.

## What Needs to Change

### 1. `tools/detect.py`
- [ ] Add `_is_in_steam_library(path)` using the same structural rule: adjacent `steamapps/common` segments, case-insensitive.
- [ ] The probe's Stage 2 already returns `Locked`/`Steam`; replace its inline `d.parts` check with the helper so adjacency is required (parity with C#).
- [ ] In `_scan_root`, after collecting names/exes but before the store-signal checks, return `("Steam", "steam_library", ...)` when the folder is inside a Steam library. This fixes normal scans (`scan_directory`) that currently return `Steam Emulator`.
- [ ] Ensure the normal-scan entry builder and tier mapping recognize `"Steam"` (Locked/Secure); do not mark it `Unknown`.
- [ ] Do not change the `--steam-libraries` exclusion behavior.

### 2. Fixture
- [ ] Create `testdata/mock/probe/SteamLibrary/steamapps/common/SteamGameWithDll/` with:
  - `SteamGameWithDll.exe` (small real-enough binary),
  - `steam_api64.dll` (small file),
  - **no `.acf`**.
- [ ] Keep it under the existing probe tree so the validator can find it.

### 3. Matrix
- [ ] Add a scenario to `tools/validate_probe_matrix.py`:
  - fixture `SteamLibrary/steamapps/common/SteamGameWithDll`,
  - expected store `Steam`,
  - expected tier `Locked` (the probe's structural Stage 2 tier).
- [ ] Keep `Store SteamEmu` (outside a library) expecting `Steam Emulator`.

## Context

- `--probe` calls `_probe_folder` → `_probe_store_at` → `_scan_root`; it does not pass `steam_library_paths`.
- The structural check is what makes the fixture self-contained.

## Requirements

- [ ] The new scenario returns `Steam`, not `Steam Emulator`.
- [ ] All existing scenarios still pass.
- [ ] No unrelated detection changes.

## Verification

- [ ] `python3 tools/validate_probe_matrix.py` — all scenarios pass, including the new one.
- [ ] `python3 tools/detect.py testdata/mock/probe/SteamLibrary/steamapps/common --json` returns the game with store `Steam`, not `Steam Emulator`.
- [ ] `python3 -m py_compile tools/detect.py tools/validate_probe_matrix.py`.

## Completion Notes

- **Completed:** 2026-10-25
- **What was done:** Added `_is_in_steam_library()` (resolved path, adjacent case-insensitive `steamapps/common` segments) and used it in `_probe_folder` Stage 2 and `_scan_root` (new `Steam`/`steam_library` signal before emulator checks). Added fixture `testdata/mock/probe/SteamLibrary/steamapps/common/SteamGameWithDll/` with exe + `steam_api64.dll` and no `.acf`. Added the `Store Steam library no ACF` matrix scenario expecting `Locked`/`Steam`; `Store SteamEmu` unchanged.
- **Verification:** `python3 tools/validate_probe_matrix.py` — ALL SCENARIOS PASS (22). `python3 tools/detect.py testdata/mock/probe/SteamLibrary/steamapps/common --json` — store `Steam`, signal `steam_library`. `python3 -m py_compile tools/detect.py tools/validate_probe_matrix.py` — OK.
- **Issues encountered:** None.
