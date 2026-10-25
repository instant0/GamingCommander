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
- [ ] Parse `libraryfolders.vdf` `apps` blocks as the launcher's installed set (today they are ignored).
- [ ] Reconcile each appid to its `appmanifest_*.acf`; key by appid, not first-wins `installdir`.
- [ ] `steamapps\common\<folder>` with no ACF → `Orphaned` (Steam game, repairable), not foreign.
- [ ] Persist `LastUpdated`; interpret `StateFlags` (4 = installed, 6 = update required) into `InstallState`.
- [ ] Read all VDF library paths; keep `common` as the content authority.

### 2. GOG
- [ ] Enumerate `GOG.com\Games` registry as inventory, independent of a disk scan.
- [ ] Read `path`, `gameName`, `gameID`, `exe`/`exeFile`, `launchCommand`, `BUILDID`, `INSTALLDATE`.
- [ ] Merge all `goggame-*.info` files; prefer the `.info` exe when the file exists.
- [ ] Persist the GOG ID from the registry even when no `.info` exists.
- [ ] Missing folder → `Missing`; registry-only records must still import.

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
