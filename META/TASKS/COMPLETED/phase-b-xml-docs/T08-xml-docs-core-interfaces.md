# Task T08: XML Docs for Core Interfaces

**Tier:** 3 — XML Documentation
**Phase:** B — XML Documentation
**Effort:** ~30 min
**Risk:** Minimal
**Status:** completed

---

## Objective

Add `/// <summary>` XML documentation to all 4 Core interfaces. These are the public API contracts of the application — every method needs a clear description explaining its purpose, parameters, and return value.

## What Needs to Change

### 1. `src/GamingCommander.Core/IGame.cs`

Currently: 7 properties, zero documentation.

Add summary to:
- `IGame` interface — "Represents a discovered game with its metadata and paths."
- `Id` — "Deterministic unique identifier (MD5-based, 16-char hex)."
- `Title` — "Display name of the game."
- `Source` — "The store/platform this game was detected from."
- `InstallPath` — "Absolute path to the game's installation directory."
- `ExecutablePath` — "Absolute path to the game's primary executable file."
- `LaunchTarget` — "The file or URL used to launch the game (may differ from ExecutablePath)."
- `LastModified` — "Timestamp of the most recent modification to the game's installation directory."

### 2. `src/GamingCommander.Core/IConfigService.cs`

Currently: 2 methods, zero documentation.

Add summary to:
- `IConfigService` — "Loads and saves application configuration (library roots, overrides, preferences)."
- `Load()` — "Loads the application configuration from persistent storage. Returns defaults if no config exists."
- `Save(AppConfig)` — "Persists the given application configuration to storage."

### 3. `src/GamingCommander.Core/IGamesDatabaseService.cs`

Currently: 9 methods, zero documentation.

Add summary to:
- `IGamesDatabaseService` — "CRUD operations for the games database (data/games.json). Provides in-memory caching."
- `Load()` — "Loads the games database from disk. Returns cached version if already loaded."
- `Save(GamesDatabase)` — "Persists the games database to disk and updates the in-memory cache."
- `GetGamesForRoot(string)` — "Returns all game entries associated with the specified library root path."
- `AddRoot(string, GameSourceKind, IEnumerable<GameEntry>)` — "Adds a new library root with its game entries."
- `RemoveRoot(string)` — "Removes a library root and all its associated game entries."
- `RescanRoot(string, IEnumerable<GameEntry>)` — "Replaces all game entries for a root with freshly scanned results."
- `UpdateGameEntry(string, GameEntry)` — "Updates a single game entry within the specified root."
- `DeleteGameEntry(string, string)` — "Removes a game entry by ID from the specified root."
- `RetagGame(string, string, GameSourceKind)` — "Changes the source type of a game entry without modifying other fields."

### 4. `src/GamingCommander.Core/ILibraryManager.cs`

Currently: 9 methods, zero documentation.

Add summary to:
- `ILibraryManager` — "High-level library management: reads roots from config, delegates scanning and CRUD to services."
- `LibraryRoots` — "Currently configured library roots, read live from persisted config."
- `GetGamesForRoot(string)` — "Returns game entries for the specified root, reading from the database."
- `AddRoot(string, GameSourceKind, IReadOnlyList<GameEntry>)` — "Adds a new library root to both config and database."
- `RemoveRoot(string)` — "Removes a root from both config and database."
- `Refresh()` — "Reloads all library roots from config and refreshes the database cache."
- `RescanRoot(string, IReadOnlyList<GameEntry>)` — "Resans a root using the provided scanner results, updating the database."
- `UpdateGameEntry(string, GameEntry)` — "Updates a game entry in the database."
- `DeleteGameEntry(string, string)` — "Deletes a game entry from the database."
- `RetagGame(string, string, GameSourceKind)` — "Retags a game entry with a new source type."

## Context

- These 4 interfaces are the public API contracts used by all ViewModels and the App layer
- `LibraryManager.cs` already has partial docs (class-level + `LibraryRoots` property) — preserve and extend
- Other files (GameSourceParser, GameEntryId, VdfParser) already have adequate docs

## Requirements

- [ ] Add `/// <summary>` to every interface and every member
- [ ] Keep descriptions concise (1–2 sentences per member)
- [ ] Explain WHY for non-obvious members (e.g., why `Load()` returns cached version)
- [ ] Do NOT add `<param>` or `<returns>` tags unless the method signature is ambiguous
- [ ] Preserve any existing `using` statements

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] All 4 interface files have `/// <summary>` on every member
- [ ] XML doc renders correctly: `dotnet build` should produce XML documentation file without warnings

## Completion Notes

- **Completed:** 2026-07-18
- **What was done:** Added `/// <summary>` XML documentation to all 4 Core interfaces:
  1. `IGame.cs` — interface + 7 properties documented
  2. `IConfigService.cs` — interface + 2 methods documented
  3. `IGamesDatabaseService.cs` — interface + 9 methods documented
  4. `ILibraryManager.cs` — interface + 1 property + 8 methods documented
- **Verification:** Build clean, 17 tests passing
- **No issues encountered.**
