# Task T117A1: Locators — Steam and Epic (known sources)

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Low
**Status:** completed (2026-10-31)
**Prerequisites:** Task T117 (service + models, done)

> **Test policy:** follow `planning/136-test-suite-reduction.md` — `TestSupport/TempDirectory`,
> one test per behavior, no back-compat tests.

## Objective

Add the two locators whose install and inventory paths are already known and used by the app: Steam and
Epic. Register them with `LauncherDiscoveryService`. No UI, no import.

## Steam

- [x] The result is a **valid Steam library**, not `Steam.exe`: `steamapps` plus at least one of
      `libraryfolders.vdf`, `common/`, or an `appmanifest_*.acf`.
- [x] Check the configured library roots first. If one is a valid Steam library, return it and make **zero
      registry reads**.
- [x] Only when no configured root is a Steam library, read one registry value to locate the main library:
      `HKLM\SOFTWARE\Valve\Steam\InstallPath`, then `HKCU\Software\Valve\Steam\SteamPath`. No 32-bit probe,
      no default-path scan.
- [x] `InstallPath` = the library root; `InventorySource` = `libraryfolders.vdf` when present, otherwise
      `steamapps` (single library).
- [x] `LauncherReadOnlySafety.Safe`.
- [x] Missing registry keys and a path without `steamapps` return `NotFound` with a reason; never throw.

## Epic

- [x] Use `EpicManifestPaths.GetManifestsDir()` (ProgramData by default, env override supported); do not
      invent a second path.
- [x] Validate the manifests directory exists and contains at least one `*.item` before `Found`.
- [x] `InventorySource` = the manifests directory.
- [x] `LauncherReadOnlySafety.Safe` (identification only; no registration; `.item` regeneration is out of
      scope).
- [x] Missing directory or no items → `NotFound` with a reason.

## Composition

- [x] Construct both locators in `App.axaml.cs` and pass them to `LauncherDiscoveryService`.
- [x] Do not wire the policy provider or UI here (T117B2).

## Tests

- [x] Steam configured root with `common/` → found, source `steamapps`, **zero registry reads**.
- [x] Steam configured root with vdf → found, source vdf, zero registry reads.
- [x] Steam configured root with ACFs but no `common/` → found.
- [x] Steam non-Steam configured root → registry fallback.
- [x] Steam no root, HKLM and HKCU fallbacks.
- [x] Steam no root/registry → not found; registry path without `steamapps` → not found.
- [x] Epic manifests dir with an item → found; missing dir/empty dir → not found.
- [x] Temp dirs; no real Steam/ProgramData access.

## Verification

- [x] `dotnet build` 0 errors; `dotnet test` green.
- [x] `grep` shows no `Directory.CreateDirectory`/write in the locators.
- [x] No network; no UI changes.

## Stop rules

- Do not add GOG/EA/Ubisoft/Battle.net/Rockstar/Xbox here; they have their own tasks.
- Do not change `SteamInstallPathLocator` behaviour beyond the probe fallbacks; existing setup use must keep
  working.
- Do not treat a locator result as proof that a game is installed.

## Completion Notes

- **Completed:** 2026-10-31 (revised same day after review)
- **What was done:** A Steam result is a **valid library**, not the Steam.exe install path.
  `SteamLauncherLocator` checks configured roots first (`steamapps` + vdf/common/ACF) and makes **no registry
  reads** when one is valid. Only if no root qualifies does it read HKLM then HKCU to locate the main library;
  the 32-bit probe and default-path scan were removed. Epic uses the ProgramData manifests directory with at
  least one `*.item`. Both registered with `LauncherDiscoveryService` in `App.LauncherDiscovery`.
- **Verification:** build 0 errors; **515 tests** (Core 148, App 366, Migration 1). Tests prove zero registry
  reads for a configured library, library validity without a vdf, registry fallback only when needed, and
  Epic found/missing/empty. No writes or network.
- **Issues encountered:** the first implementation read 3 registry keys plus defaults and treated the install
  path as the result. Corrected after review: the registry is only a fallback hint for finding the library
  index, and `InstallPath` now means the library root.
