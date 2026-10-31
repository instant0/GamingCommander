# Task T117B1: Locators — Rockstar and Xbox

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Low
**Status:** completed (2026-10-31)
**Prerequisites:** Task T117 (service + models, done)

> **Test policy:** follow `planning/136-test-suite-reduction.md` — `TestSupport/TempDirectory`,
> one test per behavior, no back-compat tests.

## Objective

Add the Rockstar locator and the explicit Xbox row. Battle.net is separated because its data source is
unverified (T117B3); do not add it here.

## Rockstar

- [x] Probe `HKCU`/`HKLM` `Rockstar Games\Launcher\InstallFolder`; if the exact key is unconfirmed on a real
      machine, keep the constant in one place and mark `NeedsVerification`.
- [x] Detect `%LOCALAPPDATA%\Rockstar Games\Launcher\Settings.json`; do **not** parse it here.
- [x] `InventorySource` = the Settings.json path; `LauncherReadOnlySafety.NeedsVerification`.
- [x] Missing key/path → `NotFound`.

## Xbox

- [x] No package API. Do not add a WinRT/Windows SDK dependency and do not enumerate `WindowsApps`.
- [x] Always `Found=false`, `LauncherReadOnlySafety.Unsupported`, reason “marker-only detection; no inventory
      reader in this build”.
- [x] Detection stays `default-metadata.json` + the folder scan.

## Composition

- [x] Register both locators with `LauncherDiscoveryService` in `App.axaml.cs`.
- [x] No UI/policy wiring.

## Tests

- [x] Rockstar: key + Settings.json → `Found`/`NeedsVerification`; missing → `NotFound`.
- [x] Xbox: always `Unsupported`, no folder probe.
- [x] `DiscoverAll` contains both rows.
- [x] Mock registry + temp dirs; no real filesystem/ProgramData.

## Verification

- [x] `dotnet build` 0 errors; `dotnet test` green.
- [x] No writes, directory creation, network, or package API.

## Stop rules

- Do not add Battle.net (T117B3).
- Do not parse Rockstar `Settings.json` or read a package database.
- Real-install key verification is a one-line constant change; tests use documented shapes.

## Completion Notes

- **Completed:** 2026-10-31
- **What was done:** Added `RockstarLauncherLocator` and `XboxLauncherLocator` in
  `LauncherLocators.cs`, and registered both in `App.axaml.cs`.
  - Rockstar reads `InstallFolder` from `HKCU\Software\Rockstar Games\Launcher`, then
    `HKLM\SOFTWARE\WOW6432Node\Rockstar Games\Launcher`. Found requires that value plus an existing
    `%LOCALAPPDATA%\Rockstar Games\Launcher\Settings.json`; the settings file is reported as
    `InventorySource` and is never parsed. Safety is `NeedsVerification`. Missing value or file →
    `NotFound`. The two key constants and the JSON path are in one place, so real-machine verification is a
    one-line change.
  - Xbox always returns `Found=false`, `LauncherReadOnlySafety.Unsupported`, reason "marker-only detection;
    no inventory reader in this build". It performs no folder/package probe and adds no dependency.
- **Verification:** `dotnet build` succeeds; `dotnet test` green, **562 tests** (Core 157, App 404,
  Migration 1). New tests cover Rockstar key+settings (`NeedsVerification`), the machine-key fallback,
  missing key/settings (`NotFound`), Xbox always unsupported, and both rows in `DiscoverAll`. Tests use
  `MockRegistryReader` and `TempDirectory` only; no real registry, ProgramData, writes, directory creation,
  network, or package API.
- **Issues encountered:** the exact Rockstar launcher key is unverified, as recorded in the task; both hives
  are tried with a single documented shape and the result is `NeedsVerification`.
