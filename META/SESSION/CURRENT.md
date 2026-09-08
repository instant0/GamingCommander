# META/SESSION/CURRENT.md — Current Project State

**Nature:** Scratch. **Overwritten** every session handoff.
**Audience:** All agents. Read every session.
**Updated:** 2026-09-08 — Plan 124 (anchor-based two-file DB) implementation + test alignment complete.

---

## This session

**Plan 124 — Anchor-based Library + Game Database (two-file split): APP CODE COMPLETE + TESTS ALIGNED, all green.**

App-side refactor (prior sessions, verified this session):
- `Models/LibraryRoot.cs` → `Library` anchor model (`Name`, `GameSourceKind Type`, `Folders[]`).
- `Models/GameRoot.cs` removed; `GamesDatabase` flat (`List<GameEntry>`).
- `GameEntry` gained `Library` (anchor name) + `FolderPath`; `ComputeId = MD5("{Library}|{FolderPath}")`.
- `AppConfig` slimmed (`HiddenFolders`, `IsFirstRun`, `LastSeenVersion`, `EnableOnlineMetadata` — no `LibraryRoots`/`FolderOverrides`).
- `ILibraryManager` keyed by anchor name (`Libraries`, `GetGamesForLibrary`, `UpsertLibrary`, `RemoveLibrary`, `ScanFolder`, `RescanLibrary`, `SelectScannerAndScan`, `UpdateGameEntry`, `DeleteGameEntry`, `RetagGame`).
- `IGamesDatabaseService` flat (`Load`, `Save`, `GetGamesForLibrary`, `SetGamesForLibrary` [merge preserves user overrides/title pins], `RemoveGamesForLibrary`, `UpdateGameEntry`, `DeleteGameEntry`, `RetagGame`).
- `GamesDatabaseService` → flat `games[]`; `LibrariesDatabaseService` reads/writes `libraries.json`; Steam bootstrap produces ONE `Steam` anchor with all vdf folders (all games surface under `Steam`).

Test alignment (this session):
- `GamesDatabaseServiceTests.cs` rewritten to flat/anchor API (13 tests: load empty/valid/corrupt, save, round-trip preserves tags/overrides, per-library scoping, rescan override preservation, unknown-library empty, remove scoping, update, delete, retag).
- `ShellViewModelSearchTests.cs` rewritten: `FakeLibraryManager` implements new `ILibraryManager`; test data uses anchors (LibA/LibB) instead of old `LibraryRoot`; search semantics preserved (3-char threshold, cross-library wildcard, tag substring, refine, backspace/escape).
- `JsonConfigServiceTests.cs` → new `AppConfig` shape (missing-file first-run, save→reload round-trip incl. HiddenFolders/LastSeenVersion/EnableOnlineMetadata).
- `IdentityPipelineTests.cs` E8 test → `SetGamesForLibrary` merge path (pinned title + `TitleSource=PcgwPick` survive rescan).
- `MetadataLookupQueueTests`/`MetadataOnlineGateTests`/`MetadataServiceTests`: GameEntry `Library`+`FolderPath` added; `AppConfig` ctor updated.
- `EpicLibraryScannerTests`, `SteamBootstrapTests`, `LibraryManagerTests`, `Core.Tests` verified already on new API (no changes needed).
- Sep 07-08 compile-side sandbox quirk (MSBuild test compilation reported an inconsistent API surface) resolved empirically — after full alignment there are no old-API bindings left; compiler now reports only genuine new-API errors; runtime `dotnet test` is the source of truth.

**Verification:** full solution build 0 errors / 10 pre-existing warnings. `dotnet test`: **App 394/394 pass, Core 156/156 pass, Migration 1/1 pass** (562 total). Python detection suite untouched (matrix 22/22, baseline 13 clean — Plan 123 closed prior session).

## Next session: Read `META/SESSION/NEXT.md`.