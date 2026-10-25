# Plan 127 — Persistence v2: Typed Game Records, Identity & Detection Provenance (A/B complete)

**Date:** 2026-10-25
**Plan:** `planning/127-persistence-v2.md`
**Predecessors:** Plan 124 (anchor library/game DB), Plan 125 (stabilization refactor).

## Shipped (A1–A6)

- **A1** — `GameEntry` composed from `Identity` / `Location` / `Launch` positional records plus body
  records; flat names retained as read-only accessors; DTO and all constructor sites migrated to the
  nested JSON shape.
- **A2** — `PlatformMetadata` string bag and `PlatformMetadataKeys` removed; replaced by typed
  `DetectionInfo`, `StoreInfo`, `SteamAcfInfo`, `EpicInfo`, `LocationInfo`. All readers/writers migrated.
- **A3** — `IdentificationTier` enum, `DetectionInfo` record, `DetectionTierDeriver`; wired into
  `FolderScanner`, `SteamLibraryScanner`, and `EpicLibraryScanner`; persisted.
- **A4** — `SchemaVersioning.Current` + `IsReadable`; all four `data/` files write the current version
  and reject a newer-version file on read. No speculative migrator registry.
- **A5** — `IdentityKey` / `IdentityKeyKinds`, `IdentityInfo.IdentityKeys`,
  `LocationInfo.PreviousLocations`, and move reconciliation in `GamesDatabaseService.SetGamesForLibrary`
  (strong store-id keys match regardless of disk; the weak folder-name key matches only when the old
  folder is gone; `Id` and sidecar preserved; old path appended to `PreviousLocations`).
- **A6** — Sidecar ownership made explicit: fetched `SteamAppId` / `GogGameId` / `Engine` renamed to
  `FetchedSteamAppId` / `FetchedGogGameId` / `FetchedEngine`; `games.json` authoritative, sidecar owns
  fetched enrichment only.

## Shipped (B7–B13)

- **B7** — `TrackingInfo` (first seen, last played, launch count, best-effort play time) persisted;
  launch tracking wired in `MainWindow.RecordLaunch`.
- **B8** — `InstallInfo` + `InstallState`; `Install.State` set by the Steam/Epic/Folder scanners via
  `InstallStateMap`. Size/build/state-flag detail stays on `SteamAcfInfo` (one owner per fact).
- **B9** — `UserStateInfo` + `CompletionState` (favourite/hidden/pinned/archived/rating/notes/completion),
  model + persistence.
- **B10** — `LaunchProfile[]` on `LaunchInfo`.
- **B11** — `RelationsInfo` + `GameKind` on `GameEntry`.
- **B12** — `Collection` + `ICollectionsService` + `CollectionsDatabaseService`, owning
  `data/collections.json` with schema-versioned reads/writes.
- **B13** — Typed `Tag` / `TagKind` replacing `List<string> Tags` across the codebase.

## Deferred (C)

`GameEntry.Extensions{}` and the emulator/ROM model (`Extensions.emulator`, library kind for ROM image
sets, ROM-extension detection) are **not implemented**. Design remains in `planning/127-persistence-v2.md` §5.

## Verification

Full solution build 0 errors; **607 tests pass / 0 fail** (Core 173 + Migration 1 + App 433).

## Docs updated

- `docs/DATA-FORMAT.md` — current nested schema, typed records, schema versioning, and sidecar ownership.
- `META/CODE_MAP.md` — composed `GameEntry`, typed records, collections service, detection noise, live
  test baseline.

## Known follow-ups

Launch-profile picker UI; `Relations.Kind` scanner wiring; collections UI; per-tag colour; play-time
tracking beyond direct-exe launches.
