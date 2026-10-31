# Task T118A: Steam + GOG Inventory Readers

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** completed (2026-10-31)
**Prerequisites:** None (delivers the shared reader contract that T118 consumes)

## Objective

Read the Steam and GOG launcher inventories read-only and map them to `StoreInventoryEntry`,
including entries whose folder is missing.

## What Needs to Change

### 1. Steam
- [x] **Do not treat `libraryfolders.vdf` `apps` blocks as the installed set.** The VDF is a path locator;
      `appmanifest_*.acf` + `steamapps/common` are the content authority. The `apps` map may be read only as
      a diagnostic/fingerprint, never as proof that an app is installed.
- [x] Reconcile each appid to its `appmanifest_*.acf`; key by appid, not first-wins `installdir`.
- [x] `steamapps\common\<folder>` with no ACF → `Orphaned` (Steam game, repairable), not foreign.
- [x] Persist `LastUpdated`. Treat `StateFlags` 4 as installed and 6 as "update required but still
      installed"; do not invent an install state or a health state for updates.
- [x] Read all VDF library paths; keep `common` as the content authority.

### 2. GOG
- [x] Enumerate `GOG.com\Games` registry as the **best available** inventory (the Galaxy DB is unverified);
      label registry-only records as lower confidence, not as a complete catalogue.
- [x] Read `path`, `gameName`, `gameID`, `exe`/`exeFile`, `launchCommand`, `BUILDID`, `INSTALLDATE`.
- [x] Merge all `goggame-*.info` files; prefer the `.info` exe when the file exists.
- [x] Persist the GOG ID from the registry even when no `.info` exists.
- [x] Missing folder → `Missing`; registry-only records must still import. Absence from the registry is not
      evidence of removal and must not mark an entry `Missing`.

## Context

- Existing readers: `SteamAcfParser`, `SteamLibraryScanner`, `GogInfoParser`, `RegistryFallbackDetector`.
- Matrix: `docs/detection/10-store-capabilities.md`.
- Read-only: never write ACF, VDF, registry, or `.info`.

## Requirements

- [x] Steam: ACF-present, ACF-missing-but-common-present, and missing-folder cases all produce entries.
- [x] GOG: registry + `.info` merged; multi-`.info` handled; missing folder surfaced.
- [x] No writes.

## Verification

- [x] Fixtures for the three Steam cases and GOG registry/`.info`.
- [x] `dotnet build` / `dotnet test` green.
- [x] No detection-matrix regression.

## Completion Notes

- **Completed:** 2026-10-31
- **What was done:**
  - Added the shared reader contract in Core: `StoreInventoryEntry` (common fields plus typed `Metadata`),
    `IStoreInventoryMetadata` with `SteamInventoryMetadata`/`GogInventoryMetadata`, and
    `IStoreInventoryReader` + `StoreManifestFingerprint`. The parent import task consumes these.
  - `SteamInventoryReader` reads ACFs across the configured roots and every library reachable through
    `libraryfolders.vdf`, keys by AppID, and never treats `apps` blocks as installed. It reports
    ACF-present-with-folder, ACF-present-without-folder, and `steamapps/common` without an ACF (orphan,
    empty store id). `StateFlags` 4 = `Installed`, 6 = `UpdateRequired` with the folder still installed.
    `LastUpdated`/`BuildId` are kept in typed metadata. No recursive enumeration.
  - `GogInventoryReader` enumerates `GOG.com\Games`, reads `path`, `gameName`, `gameID`, `exe`/`exeFile`,
    `launchCommand`, `BUILDID`, `INSTALLDATE`, and merges `goggame-*.info` through the existing parser.
    The `.info` exe wins only when the file exists; otherwise the registry exe is used. Missing folders
    still produce entries (registry-only, no removal inference).
  - Both readers are read-only and perform one top-level read; no writes, registry writes, or directory
    creation.
- **Verification:** `dotnet build` 0 errors / 0 warnings; `dotnet test` **572 green** (Core 157, App 415).
  Eleven new tests cover the three Steam cases, `apps`-block exclusion, VDF-discovered libraries, the
  update flag, GOG registry import with metadata, `.info` exe preference, multiple `.info` files, the
  missing-folder case, and an empty registry. `python3 tools/validate_probe_matrix.py` passes all 22
  scenarios.
- **Issues encountered:** the parent task (inventory import service, diff, persistence) is still pending;
  this task deliberately stops at the readers and the shared contract. Orphan Steam folders have no
  AppID, so their `StoreId` is empty and the import service must match them by normalized path.
