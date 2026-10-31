# Task T135: Remove App-Version Backward Compatibility

**Tier:** 2 — Code Quality / Persistence
**Phase:** G — Code Quality and Tests
**Risk:** Medium (touches every JSON store; no shipped data exists)
**Status:** completed (2026-10-31)
**Prerequisites:** None. Run before T134.

## Objective

Remove the schema-version machinery and every code path that exists only to read data written by another
build. There is no released version to be compatible with; persisted state is disposable. Keeping a
version guard creates a second read path, extra states, and tests for a problem that does not exist.

**Scope boundary:** "no back-compat" means **our own persisted JSON between app versions**. It does not
mean external file formats (Steam ACF, Epic `.item`, GOG `.info`, blacklist layout) or game-file migration.

## What is removed

| Item | Where |
|---|---|
| `SchemaVersioning` (`Current`, `IsReadable`) | `Core/Services/SchemaVersioning.cs` |
| Root `Version` field in each store DTO | `GamesDatabaseService`, `LibrariesDatabaseService`, `JsonConfigService`, `MetadataStore`, `CollectionsDatabaseService` |
| `JsonDocumentStore.Read(..., Func<T,bool>? isReadable)` | `JsonDocumentStore` |
| `StoreReadStatus.Unreadable` and its branch | `JsonDocumentStore` |
| "newer build" handling in the services | all five stores |
| Version tests and assertions | `SchemaVersioningTests`, `JsonDocumentStoreTests`, service tests |

After this, the store has **three** read states: `Ok`, `Missing`, `Corrupt`. A file written by a future
build is just a file: if its shape still parses, it is read; if it does not, it is `Corrupt` and the
`.bak`/`.corrupt` recovery applies. There is no special "newer version" promise.

## What is explicitly kept

- `.bak` + `.corrupt` recovery and the verified write sequence — durability, not compatibility.
- `AppConfig.LastSeenVersion` — it drives the one-time setup prompt after an app update. It is upgrade UX,
  not data compatibility.
- `InstallInfo.Version` — the **game's** version, not a schema version. Do not touch it.
- External format fallbacks: Epic `.json` manifest, flat blacklist list, legacy EA/Ubisoft formats. Those
  are other applications' formats, not our app-version history.
- `GamingCommander.Migration` — plans game-file moves; unrelated to schema compatibility.

## What Needs to Change

### 1. Store
- [x] Delete `SchemaVersioning`.
- [x] Remove the root `Version` property from the five DTOs and stop writing it.
- [x] Remove `isReadable` from `JsonDocumentStore.Read`; delete the `Unreadable` branch and enum member.
- [x] Update the five service read calls; no `Version` logic remains.
- [x] A parsed file with an unknown extra member is accepted (System.Text.Json already ignores unknown
      members). Do not add a check for it.

### 2. Tests
- [x] Delete `SchemaVersioningTests`.
- [x] Delete the newer-version/unreadable tests and the test-only serializer that forced a read failure by
      version, if it has no other use.
- [x] Remove `Version` setup/assertions from service tests; keep the round-trip, corruption, `.bak`,
      `.corrupt`, first-run, and guard tests.
- [x] Replace the “newer version is preserved” assertion with the actual rule: a file whose shape does not
      parse is `Corrupt` and recovered from `.bak`.

### 3. Docs and plans
- [x] `docs/DATA-FORMAT.md`: remove the root `version` fields and the schema-versioning section.
- [x] `docs/ONLINE-AND-DATA.md`: remove the “newer version is left untouched” sentence; keep `.bak`/`.corrupt`.
- [x] `docs/FAQ.md`: remove “a file written by a newer version is left untouched”; keep the corruption recovery.
- [x] `GamingCommander.Readme.txt`: no version-compatibility promise.
- [x] `META/CONNECTION_MAP.md`: four-state store → three-state.
- [x] `planning/132-persistence-write-integrity.md`: add a one-line “superseded by the no-back-compat
      decision” note, so it is not reimplemented.
- [x] `META/COMPLETED/backward-compatibility-removed.md` (new, append-only): what was removed, why, and how
      to reintroduce it later (root `version` field + guard + migration hook). No code kept for it.

## Requirements

- [x] `grep -rn "SchemaVersioning\|Unreadable\|isReadable" src tests` returns nothing.
- [x] No store DTO has a root `Version` property. `InstallInfo.Version` remains.
- [x] No service rejects a file based on a version number.
- [x] Corruption recovery is unchanged: `.corrupt` keeps the bytes, `.bak` restores.
- [x] No new dependency; no new read path.
- [x] The archive note exists and names the removed API.

## Verification

- [x] `dotnet build` 0 errors; `dotnet test` green.
- [x] First-run, round-trip, damaged-file, `.bak`, `.corrupt`, and guard tests pass.
- [x] `grep` checks above are empty.
- [x] Docs contain no “newer version” claim.
- [x] The connection map says three read states.

## Stop rules

- Do not remove durability. `.bak`/`.corrupt` is not compatibility.
- Do not remove external-format fallbacks or the migration project.
- Do not touch `LastSeenVersion` or `InstallInfo.Version`.
- No schema migration code is added “just in case”.

## Completion Notes

- **Completed:** 2026-10-31
- **What was done:** Removed `SchemaVersioning`, the root `Version` field from all five store DTOs, the
  `isReadable` parameter, and the `Unreadable` state. `JsonDocumentStore` now has `Ok`/`Missing`/`Corrupt`;
  services no longer reject a file by version. Deleted `SchemaVersioningTests`, the newer-version store test,
  the future-version service tests, and DTO `Version` assertions. Updated `DATA-FORMAT`, `ONLINE-AND-DATA`,
  `FAQ`, the connection map, `CODE_MAP`, and added a superseded note to `planning/132`. Archived the
  decision and reintroduction recipe in `META/COMPLETED/backward-compatibility-removed.md`.
- **Verification:** build 0 errors (3 pre-existing Avalonia notices); **773 tests** pass (Core 232, App 540,
  Migration 1; was 780). `grep -rn "SchemaVersioning|Unreadable|isReadable" src tests` is empty. No root
  `Version` DTO field remains; `InstallInfo.Version` and `LastSeenVersion` are untouched. Corruption,
  `.bak`, `.corrupt`, first-run, and guard tests pass.
- **Issues encountered:** `T130` still had status `ready` in its task document despite being marked Done in
  the tracker; set to completed and annotated with the three-state supersession. `planning/132` still
  described the four-state design; annotated as superseded rather than rewritten, since its durability rules
  remain current.
