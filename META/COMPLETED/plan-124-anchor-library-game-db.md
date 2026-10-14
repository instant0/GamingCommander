# Plan 124 — Anchor-based Library + Game Database (complete)

**Date:** 2026-09-08
**Plan:** `planning/124-anchor-library-game-db.md`

Shipped the two-file split: `libraries.json` holds named `Library` anchors (`Name`, `Type`,
`Folders[]`); `games.json` is a **flat** `games[]` list where each `GameEntry` links to an anchor by
`Library` name and records its own physical `FolderPath`. `AppConfig` reduced to non-library settings
(`HiddenFolders`, `IsFirstRun`, `LastSeenVersion`, `EnableOnlineMetadata`).

**C# files:** `Core/Models/Library.cs`, flat `Core/Models/GamesDatabase.cs`, `Core/ILibrariesService.cs`,
`App/Services/LibrariesDatabaseService.cs`; removed `Core/Models/GameRoot.cs`, `Core/Models/LibraryRoot.cs`.
`ILibraryManager`/`IGamesDatabaseService` re-keyed by anchor name (see `META/CODE_MAP.md`).

**Declined:** data migration of old `settings.json`/`games.json` (documented as no back-compat).

**Contract / verification:** `docs/DATA-FORMAT.md` (rewritten to the anchor target); all tests green.
Predecessor to Plan 125.
