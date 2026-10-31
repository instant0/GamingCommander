# App-Version Backward Compatibility — Removed

**Date:** 2026-10-31
**Decision:** persisted JSON state is disposable pre-release. The app does not promise to read data written
by another build. No version field, no read guard, no migration hook is kept in code.

## Removed

- `GamingCommander.Core/Services/SchemaVersioning.cs` (`Current`, `IsReadable`).
- Root `Version` property in the five JSON store DTOs: `games.json`, `libraries.json`, `settings.json`,
  `games_metadata.json`, `collections.json` (the sidecar used JSON name `version`).
- `JsonDocumentStore.Read(Func<T> defaults, Func<T,bool>? isReadable)` → `Read(Func<T> defaults)`.
- `StoreReadStatus.Unreadable`; the store now has `Ok`, `Missing`, `Corrupt`.
- The "newer build" branches in `JsonConfigService`, `GamesDatabaseService`, `LibrariesDatabaseService`,
  `CollectionsDatabaseService`, `MetadataStore`.
- `SchemaVersioningTests`, the newer-version store test, the future-version service tests, and DTO
  `Version` assertions.

## What was kept, and why

- `.bak` / `.corrupt` recovery and the verified write — data durability, not compatibility between builds.
- `AppConfig.LastSeenVersion` — prompts setup after an app update; upgrade UX, not persisted-data compat.
- `InstallInfo.Version` — the **game's** version, not a schema version.
- External-format fallbacks (Epic `.json` manifest, flat blacklist, legacy EA/Ubisoft formats) — other
  applications' formats, not our app-version history.
- `GamingCommander.Migration` — plans game-file moves, unrelated to schema compatibility.

## If it is ever needed again

Reintroduce it deliberately, as a new feature:

1. Add a root `Version` field to each DTO and write the current value on save.
2. Add one read guard with a single policy: reject a version this build does not understand.
3. Decide explicitly what a rejected file does (preserve + report, or reset), and add a migration hook for
   each breaking change.
4. Add tests for the first version, a same-version read, and a rejected version.

Until then, a file whose shape parses is read; one that does not is `Corrupt` and recovered from `.bak`.
