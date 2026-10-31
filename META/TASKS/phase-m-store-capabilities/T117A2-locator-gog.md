# Task T117A2: Locator — GOG

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Low
**Status:** completed (2026-10-31)
**Prerequisites:** Task T117 (service + models, done)

> **Test policy:** follow `planning/136-test-suite-reduction.md` — `TestSupport/TempDirectory`,
> one test per behavior, no back-compat tests.

## Objective

Add the GOG launcher locator. GOG is the remaining store whose **file signals** are already understood; the
launcher path is documented. This task does **not** read the Galaxy SQLite database and does **not** claim a
complete inventory.

The unknown data sources (EA, Ubisoft, Battle.net, Arc) are handled by the research task and their own
blocked locator tasks. Do not add them here.

## What Needs to Change

- [x] Probe `HKLM\SOFTWARE\WOW6432Node\GOG.com\GalaxyClient\paths\common` and the documented default Galaxy
      path. Verify a key spelling on a real install before changing its constant; tests use
      `MockRegistryReader`.
- [x] Validate the path exists before `Found`.
- [x] `InventorySource` = the Galaxy client root. The per-game registry/`.info` files remain the scanner's
      job; the Galaxy SQLite DB is **not** read.
- [x] `LauncherReadOnlySafety.NeedsVerification` (per-game registry/`.info` are safe, the complete Galaxy
      inventory is not verified).
- [x] Missing key/path → `NotFound` with a reason; never throw.

## Composition

- [x] Register the locator in `App.axaml.cs` alongside Steam/Epic.
- [x] No UI, no import, no policy wiring.

## Tests

- [x] Key present + path exists → `Found`, `NeedsVerification`, correct source.
- [x] Missing key → `NotFound`.
- [x] Key present but path missing → `NotFound`.
- [x] No real registry/filesystem access outside temp dirs.

## Verification

- [x] `dotnet build` 0 errors; `dotnet test` green.
- [x] No writes, directory creation, or network.
- [x] `DiscoverAll` includes GOG.

## Stop rules

- Do not read or add a dependency for Galaxy SQLite.
- Do not add EA/Ubisoft/Battle.net/Arc here.
- Do not treat `Found` as proof a game is installed.

## Completion Notes

- **Completed:** 2026-10-31
- **What was done:** Added `GogLauncherLocator` for the documented Galaxy path (`paths` key value `common`,
  then `client`) plus an injectable default path. It validates the directory, returns
  `LauncherReadOnlySafety.NeedsVerification`, and never reads the Galaxy database. Registered with
  `LauncherDiscoveryService` in the App composition.
- **Verification:** build 0 errors; **520 tests** (Core 148, App 371, Migration 1). Tests: common key, client
  fallback, default path, missing key, key with missing path. No writes/network.
- **Issues encountered:** the research note writes `paths\common`; it is a value named `common` under
  `...\GalaxyClient\paths`, which is what the locator reads. The Galaxy DB stays unread.
