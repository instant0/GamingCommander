# Task T123: Store Key, Detection Origin, and Fast Local Index

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** completed
**Prerequisites:** None

## Objective

Make the local game database cheap to inventory by store and preserve where each entry came from.
The store sync must answer "which EA games do we already know?" with an O(1) lookup, not a scan of
every game.

## What Needs to Change

### 1. Provenance and identity (Core model)
- [ ] `DetectionOrigin` enum: `StoreManifest`, `FileScan`, `User`, `Merged`.
- [ ] Add `DetectionInfo.Origin`. Keep the existing `DetectionInfo.ManifestKind` as the specific manifest (`Acf`, `EpicItem`, `GogInfo`, …); `Origin` is the high-level source path.
- [ ] Add `StoreInfo.StoreKey` — stable `"<GameSourceKind>:<storeId>"` (e.g. `EaApp:<contentId>`, `Steam:<appid>`); persisted.
- [ ] Add the missing store IDs to `StoreInfo`: `EaContentId`, `UbisoftGameId`, `XboxPackageFamily`, `RockstarTitleId`; update `IsEmpty`.
- [ ] Add `InstallInfo.StoreReportedState` (enum `Unknown`/`Installed`/`UpdateRequired`/`Incomplete`) and `LastStoreSeenUtc`; map the raw state into the existing `State`/`UpdateAvailable`.
- [ ] Keep the disk-derived `InstallState` separate from `StoreReportedState`.

### 2. Fast local index
- [ ] `GameStoreIndex` in `GamingCommander.Core/Services`, built from `IEnumerable<GameEntry>`:
  - `ByStoreKey` (primary),
  - `ByPath` (normalized),
  - `ByStore`.
- [ ] Read-side only; writes stay in `IGamesDatabaseService`. Build from `Load().Games` (or update on mutation); no new persisted file.
- [ ] O(1) lookup; expose a snapshot for the background sync.
- [ ] Do **not** add a second persisted store list to `games.json`; rebuild the index on load.
- [ ] Duplicate-key detection: two entries with the same `StoreKey` are a data error, not a silent overwrite.

### 3. Tests
- [ ] 9 known games + a 10-entry manifest → the index identifies the missing `StoreKey`.
- [ ] Lookup by `StoreKey`, normalized path, and store.
- [ ] Duplicate `StoreKey` is surfaced.
- [ ] Persistence round-trip preserves `StoreKey`, `Origin`, `StoreReportedState`, `LastStoreSeenUtc` (update `GamesDatabaseService.ToDto`/`ToEntry`).
- [ ] Index build performs no directory I/O.

## Context

- Proposal: `planning/129` §3.3–§3.4.
- The store diff (T121A) and import (T118) consume this index.
- The index is derived data; `games.json` remains the source of truth.

## Requirements

- [ ] Store lookups are O(1), not a full-list scan.
- [ ] Provenance survives load/save.
- [ ] Store-reported state is not conflated with disk state.
- [ ] No directory scan in the index.

## Verification

- [ ] `dotnet build` / `dotnet test` green.
- [ ] The 9-vs-10 fixture test passes.
- [ ] No new persisted file unless explicitly justified.

## Completion Notes

- **Completed:** 2026-10-25
- **What was done:** Added `DetectionOrigin` (nullable `DetectionInfo.Origin`; `ManifestKind` retained), `StoreInfo.StoreKey` plus `EaContentId`/`UbisoftGameId`/`XboxPackageFamily`/`RockstarTitleId` and updated `IsEmpty`, and `InstallInfo.StoreReportedState`/`LastStoreSeenUtc` alongside the existing disk-derived state. Added read-only `GameStoreIndex` (`ByStoreKey`, normalized `ByPath`, `ByStore`, `Duplicates`, `GetMissingStoreKeys`) with no filesystem access. Existing model DTOs round-trip all new fields through `GamesDatabaseService`; no new persisted file. Added Core index tests and an App persistence round-trip test.
- **Verification:** `dotnet build GamingCommander.sln` succeeded, 0 errors (3 pre-existing Avalonia AVLN3001 warnings). `dotnet test GamingCommander.sln` passed 666/666 (Core 200, App 465, Migration 1; Detection has no discovered tests). `python3 tools/validate_probe_matrix.py` all scenarios pass.
- **Issues encountered:** The solution emits three pre-existing Avalonia `AVLN3001` warnings from `MainWindow.axaml`, `GameSetupWindow.axaml`, and `LibrarySetupWindow.axaml`; they are unrelated to this change.
