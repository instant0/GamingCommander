# Task T117A3: Locator — Ubisoft Registry Inventory

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** completed (2026-10-31)
**Prerequisites:** Task T117 (service + models, done). T136 approved the registry inventory.

> **Test policy:** follow `planning/136-test-suite-reduction.md` — one test per behavior/failure.

## Objective

Add a minimal Ubisoft locator for the source T136 approved: the per-game registry list
`Ubisoft\Launcher\Installs\{gameId}\InstallDir`. The locator says **the registry inventory is available** and
where it lives; it does not parse the encrypted manifest or `uplay_install.state`.

EA is **not** in this task: T136 left it detection-only. Battle.net is detection-only too.

## What Needs to Change

- [x] Probe `HKLM\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs` (and a documented alternate only if
      confirmed).
- [x] `Found=true` when the key exists and contains at least one game id; `InventorySource` = the registry
      key path; `InstallPath` = null.
- [x] `LauncherReadOnlySafety.NeedsVerification` (documented and already used for classification; no complete
      fixture).
- [x] Missing key / no game ids → `NotFound`.
- [x] No manifest/state parsing, no launcher executable path, no writes.
- [x] Register with `LauncherDiscoveryService`.

## Tests

- [x] Key with one/multiple game ids → `Found`, `NeedsVerification`, correct source.
- [x] Missing key → `NotFound`.
- [x] Key exists but empty → `NotFound`.
- [x] Mock registry only; no real Ubisoft install.

## Verification

- [x] `dotnet build` 0 errors; `dotnet test` green.
- [x] `DiscoverAll` includes Ubisoft.
- [x] No registry write, no manifest parse, no new dependency.

## Stop rules

- Do not add EA or Battle.net (T136: detection-only).
- Do not add a launcher executable locator; the registry inventory is the useful source.
- Do not parse the manifest.

## Completion Notes

- **Completed:** 2026-10-31
- **What was done:** Added `UbisoftLauncherLocator` for the T136-approved source. It enumerates
  `HKLM\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs`; one or more game ids means `Found`, with the key
  path as `InventorySource`, `InstallPath` null, and `NeedsVerification`. It does not read `InstallDir`
  values, the encrypted manifest, or `uplay_install.state`. Registered in the App composition.
- **Verification:** build 0 errors; **522 tests** (Core 148, App 373, Migration 1). Tests: one/multiple ids,
  missing key, key with no game ids. No writes/network/manifest parsing.
- **Issues encountered:** the locator checks key presence, not the value shape, so it stays
  `NeedsVerification` until the registry fixture is confirmed.
