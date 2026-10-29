# Task T118: Read-Only Store Inventory Import

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** High
**Status:** pending
**Prerequisites:** Task T118 depends on Task T117, Task T120, and Task T123

## Objective

Read each launcher's own installed-game inventory (read-only), map it into normal `GameEntry`
records, cross-check the disk, and persist through the existing game database. This is the core of
the "populate the inventory like a normal file scanner" proposal.

## What Needs to Change

### 1. Models
- [ ] `StoreInventoryEntry`: `StoreId`, `Name`, `InstallPath`, `LaunchExe?`, `StoreState` (store-reported), `FolderExists`, `ExeExists`, `Metadata` (typed per store).
- [ ] `IStoreInventoryReader`: `GameSourceKind Kind`, `StoreManifestFingerprint? Fingerprint`, `IReadOnlyList<StoreInventoryEntry> Read()`.

### 2. Import service
- [ ] `StoreInventoryImportService.Import(kind)`: read the manifest once → map to `GameEntry` → stat each folder/exe (no recursion) → set `DetectionOrigin.StoreManifest` → persist through the shared JSON store if it exists, otherwise `JsonFileHelper` — never a new direct `File.WriteAllText`.
- [ ] Imported `LaunchExe` must resolve to an existing file under the reported install root; otherwise store `null` and mark `ExeMissing`.
- [ ] Do **not** call `FolderScanner` in this path. Enrichment is a separate opt-in step for new games only.
- [ ] Set `Detection` confidence: authoritative store record → `Secure`/`Locked`; disk-only → `Candidate`.
- [ ] Keep import **idempotent**: re-running must not duplicate entries; match by `StoreKey` (fall back to normalized path/name).
- [ ] Expose the same import entry point to first-run (T117B) and startup sync (T121).
- [ ] Never write to store files.

### 3. Identity
- [ ] Add/confirm stable IDs per store (`StoreInfo`/`GameIdentity`): GOG, EA content ID, Ubisoft game ID, Battle.net product code, Xbox package family, Rockstar title ID.
- [ ] Add contract/pin tests for any persisted ID.

### 4. Subtasks
- [ ] T118A Steam + GOG.
- [ ] T118B Epic + EA.
- [ ] T118C Ubisoft + Battle.net + Rockstar.
- [ ] Xbox package inventory is handled with the Xbox locator/reader in T118C.

## Context

- Existing readers to reuse: `SteamLibraryScanner`, `SteamAcfParser`, `EpicLibraryScanner`, `EpicItemCatalog`.
- Matrix: `docs/detection/10-store-capabilities.md`; proposal: `planning/129`.
- Disk wins over the launcher record for `Installed`.

## Requirements

- [ ] Each reader is read-only and returns records even when the folder is missing.
- [ ] Each imported entry has the correct source, install path, and state.
- [ ] Import performs no recursive enumeration; only `Directory.Exists`/`File.Exists`.
- [ ] Import uses `GameStoreIndex` for O(1) duplicate checks.
- [ ] Enrichment is the existing pipeline, not a parallel one.
- [ ] No store-file writes, no directory creation, and no network access.
- [ ] Imported paths are validated before persistence; a record cannot point outside its reported install root.
- [ ] Persistence uses the shared JSON store (atomic replace, `.bak`, visible failure); no direct `File.WriteAllText`.

## Verification

- [ ] Fixture per store; missing-folder case yields `Missing`.
- [ ] Round-trip: import → `games.json` → reload preserves source/id/state.
- [ ] `dotnet build` / `dotnet test` green; detection matrix unaffected.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
