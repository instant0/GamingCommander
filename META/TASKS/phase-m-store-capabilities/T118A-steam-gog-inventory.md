# Task T118A: Steam + GOG Inventory Readers

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** pending
**Prerequisites:** Task T118A depends on Task T118

## Objective

Read the Steam and GOG launcher inventories read-only and map them to `StoreInventoryEntry`,
including entries whose folder is missing.

## What Needs to Change

### 1. Steam
- [ ] **Do not treat `libraryfolders.vdf` `apps` blocks as the installed set.** The VDF is a path locator;
      `appmanifest_*.acf` + `steamapps/common` are the content authority. The `apps` map may be read only as
      a diagnostic/fingerprint, never as proof that an app is installed.
- [ ] Reconcile each appid to its `appmanifest_*.acf`; key by appid, not first-wins `installdir`.
- [ ] `steamapps\common\<folder>` with no ACF → `Orphaned` (Steam game, repairable), not foreign.
- [ ] Persist `LastUpdated`. Treat `StateFlags` 4 as installed and 6 as "update required but still
      installed"; do not invent an install state or a health state for updates.
- [ ] Read all VDF library paths; keep `common` as the content authority.

### 2. GOG
- [ ] Enumerate `GOG.com\Games` registry as the **best available** inventory (the Galaxy DB is unverified);
      label registry-only records as lower confidence, not as a complete catalogue.
- [ ] Read `path`, `gameName`, `gameID`, `exe`/`exeFile`, `launchCommand`, `BUILDID`, `INSTALLDATE`.
- [ ] Merge all `goggame-*.info` files; prefer the `.info` exe when the file exists.
- [ ] Persist the GOG ID from the registry even when no `.info` exists.
- [ ] Missing folder → `Missing`; registry-only records must still import. Absence from the registry is not
      evidence of removal and must not mark an entry `Missing`.

## Context

- Existing readers: `SteamAcfParser`, `SteamLibraryScanner`, `GogInfoParser`, `RegistryFallbackDetector`.
- Matrix: `docs/detection/10-store-capabilities.md`.
- Read-only: never write ACF, VDF, registry, or `.info`.

## Requirements

- [ ] Steam: ACF-present, ACF-missing-but-common-present, and missing-folder cases all produce entries.
- [ ] GOG: registry + `.info` merged; multi-`.info` handled; missing folder surfaced.
- [ ] No writes.

## Verification

- [ ] Fixtures for the three Steam cases and GOG registry/`.info`.
- [ ] `dotnet build` / `dotnet test` green.
- [ ] No detection-matrix regression.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
