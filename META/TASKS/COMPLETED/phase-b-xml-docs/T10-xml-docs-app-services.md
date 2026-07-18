# Task T10: XML Docs for App Services

**Tier:** 3 — XML Documentation
**Phase:** B — XML Documentation
**Effort:** ~30 min
**Risk:** Low
**Status:** completed

---

## Objective

Add `/// <summary>` XML documentation to App service classes that currently lack it. These services implement the Core interfaces and contain important behavior (caching, error handling, DTO mapping) that should be documented.

## What Needs to Change

### Files to Document

#### 1. `src/GamingCommander.App/Services/GamesDatabaseService.cs`
Already has class-level summary: none. Add class-level + public method docs.

- `GamesDatabaseService` class — "JSON-file implementation of IGamesDatabaseService with in-memory caching. Reads/writes data/games.json using DTO mapping."
- Constructor `GamesDatabaseService(string dbPath)` — "Creates a new database service targeting the specified JSON file path."
- `Load()` — "Loads the games database from disk. Returns cached version if already loaded. Creates empty database if file missing."
- `Save(GamesDatabase)` — "Serializes and persists the games database to disk. Updates the in-memory cache."
- `GetGamesForRoot(string)` — "Returns all game entries for the specified library root path."
- `AddRoot(string, GameSourceKind, IEnumerable<GameEntry>)` — "Adds a new library root with its game entries to the database."
- `RemoveRoot(string)` — "Removes a library root and all associated game entries."
- `RescanRoot(string, IEnumerable<GameEntry>)` — "Replaces all entries for a root with freshly scanned results."
- `UpdateGameEntry(string, GameEntry)` — "Updates a single game entry within the specified root."
- `DeleteGameEntry(string, string)` — "Removes a game entry by ID from the specified root."
- `RetagGame(string, string, GameSourceKind)` — "Changes the source type of a game entry."
- Note: Nested DTO classes (`GamesDatabaseDto`, `GameRootDto`, `GameEntryDto`) are internal — add brief docs but keep them short.

#### 2. `src/GamingCommander.App/Services/JsonConfigService.cs`
Already has class-level summary: none. Add class-level + public method docs.

- `JsonConfigService` class — "JSON-file implementation of IConfigService. Reads/writes settings.json with DTO mapping."
- Constructor `JsonConfigService(string configPath)` — "Creates a new config service targeting the specified JSON file path."
- `Load()` — "Loads application configuration from disk. Returns defaults (empty roots, isFirstRun=true) if file missing."
- `Save(AppConfig)` — "Serializes and persists the application configuration to disk."
- Note: Nested `ConfigDto` class is internal — brief doc only.

#### 3. `src/GamingCommander.App/Services/BlacklistLoader.cs`
Already has class-level summary and `Load()` docs. **Skip** — already documented.

#### 4. `src/GamingCommander.App/Services/LibraryManager.cs`
Already has class-level summary and `LibraryRoots` docs. **Extend** — add docs to undocumented methods.

- `GetGamesForRoot(string)` — Add summary if missing.
- `AddRoot(string, GameSourceKind, IReadOnlyList<GameEntry>)` — Add summary if missing.
- `RemoveRoot(string)` — Add summary if missing.
- `Refresh()` — Add summary if missing.
- `RescanRoot(string, IReadOnlyList<GameEntry>)` — Add summary if missing.
- `UpdateGameEntry(string, GameEntry)` — Add summary if missing.
- `DeleteGameEntry(string, string)` — Add summary if missing.
- `RetagGame(string, string, GameSourceKind)` — Add summary if missing.

#### 5. `src/GamingCommander.App/Services/SteamLibraryScanner.cs`
Already has class-level summary and `Scan()` docs. **Extend** — add docs to undocumented methods.

- `ScanAll()` — Add summary if missing.
- `DiscoverAllSteamPaths(string)` — Add summary if missing.
- `CollectAcfMap(List<string>)` — Add summary if missing.
- `CreateEntry(...)` — Add summary if missing.
- Any other public/internal methods without docs.

#### 6. `src/GamingCommander.App/Services/FolderScanner.cs`
Partially documented (`FindExecutablesDeep` and `ScoreExecutable` have docs). **Extend** — add docs to undocumented public methods.

- `Scan(string, GameSourceKind)` — Add summary.
- `DetectType(DirectoryInfo)` — Add summary.
- `FindExecutablesDeep(DirectoryInfo, GameSourceKind)` — Already has docs. Verify.
- `ScoreExecutable(string, DirectoryInfo, GameSourceKind)` — Already has docs. Verify.
- Any other public methods.

## Context

- `BlacklistLoader.cs` and `LibraryManager.cs` already have partial docs — preserve and extend
- `SteamLibraryScanner.cs` has class + `Scan()` docs — extend remaining methods
- `FolderScanner.cs` has `FindExecutablesDeep` + `ScoreExecutable` docs — extend remaining public methods
- `GamesDatabaseService.cs` and `JsonConfigService.cs` have zero docs — add from scratch
- Internal DTO classes get brief docs only (they're implementation details)

## Requirements

- [ ] Add `/// <summary>` to every public class and method
- [ ] Preserve existing documentation (extend, don't replace)
- [ ] Keep descriptions concise (1–2 sentences)
- [ ] For DTO classes: one-line summary is sufficient
- [ ] Do NOT add `<param>` tags unless parameter meaning is ambiguous

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] All public methods in App/Services/ have `/// <summary>` docs

## Completion Notes

- **Completed:** 2026-07-18
- **What was done:** Added `/// <summary>` XML documentation to 5 App service files:
  1. `GamesDatabaseService.cs` — class + constructor + 10 public methods documented
  2. `JsonConfigService.cs` — class + constructor + 2 public methods documented
  3. `LibraryManager.cs` — constructor + 8 public methods documented (class + LibraryRoots + Refresh + SelectScannerAndScan + LooksLikeSteamLibrary + NormalizeLibraryRoot already had docs)
  4. `SteamLibraryScanner.cs` — constructor documented (class + Scan + ScanAll already had docs)
  5. `FolderScanner.cs` — 3 constructors + Scan method documented (FindExecutablesDeep + ScoreExecutable already had docs)
- **Skipped:** `BlacklistLoader.cs` (already fully documented)
- **Verification:** Build clean, 17 tests passing
- **No issues encountered.**
