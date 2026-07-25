# Task T65: GOG goggame-*.info Parser

**Tier:** 3 — Logic/Behavior
**Phase:** H — MVP
**Effort:** ~45 min
**Risk:** Medium
**Status:** Complete
**Prerequisites:** None
**WP:** WP-3 (3.1)

---

## Objective

GOG games install `goggame-{id}.info` JSON files containing the game title, primary executable path, and launch arguments. The C# scanner detects GOG via `goggame*` glob but never parses these files, so GOG games without a root-level `.exe` fail to get an executable path, and launch arguments are always lost. Port the parsing logic from `detect.py` `_extract_gog_metadata`.

## What Needs to Change

### 1. New file: `src/GamingCommander.App/Services/GogInfoParser.cs`

**Current state:** Does not exist.

**Actions:**
- [ ] Create `GogInfoParser` static class in `GamingCommander.App.Services` namespace
- [ ] Add `/// <summary>` XML doc: "Parses GOG goggame-*.info JSON files for game metadata."
- [ ] Create record type for return value:
  ```csharp
  internal record GogGameInfo(
      string Title,
      string GameId,
      string ExePath,      // absolute path, resolved from relative
      string LaunchArgs);
  ```
- [ ] Implement `TryParse(DirectoryInfo gameDir, out GogGameInfo? info)`:
  - Search root + 1 level of non-noise subdirs for `goggame-*.info` files
  - For each file, parse as JSON (`System.Text.Json`)
  - Find the main game entry: `gameId == rootGameId` (DLC files have different IDs)
  - Extract from `playTasks` array: find task where `isPrimary == true`
  - Read `path` (exe) and `arguments` (launch args) from the primary playTask
  - **Resolve relative exe paths** to absolute using `Path.GetFullPath(Path.Combine(infoFile.DirectoryName, taskPath))`
  - Return `GogGameInfo` with title, absolute exe path, and args
- [ ] Handle edge cases:
  - No `.info` files found → return false
  - Malformed JSON → catch `JsonException`/`IOException`, skip file, try next
  - No `playTasks` → return false
  - No primary task → use first task with a `path`
  - Relative exe paths → resolve to absolute (GOG .info paths are relative to game root per `docs/research/gog_format.md`)

### 2. `src/GamingCommander.App/Services/FolderScanner.cs` — `AddGameEntry()`

**Current state:** Line ~344-357 creates `GameEntry` with `CommandLineArguments: string.Empty` for all games. GOG detection (line ~64) returns a boolean but no metadata.

**Actions:**
- [ ] After `StoreSignalDetector.DetectType()` returns `GameSourceKind.Gog`, call `GogInfoParser.TryParse(subDir, out var gogInfo)`
- [ ] If `gogInfo` is not null:
  - **Title:** Use `gogInfo.Title` for `DisplayName` (GOG .info is the official title). Store the folder-name-derived title in `PlatformMetadata["AutoDetectedTitle"]` for future use by a title-selection UI.
  - **Exe:** Use `gogInfo.ExePath` as a **fallback** when `ExecutableDiscovery` finds nothing. When `ExecutableDiscovery` finds multiple exes, the GOG .info exe path can help disambiguate (prefer the one matching `gogInfo.ExePath`). This is a hint, not a hard override.
  - Set `CommandLineArguments = gogInfo.LaunchArgs`
  - Populate `PlatformMetadata["GogGameId"] = gogInfo.GameId`
  - Populate `PlatformMetadata["TitleSource"] = "GogInfo"` (metadata source tracking — see Design Decisions)

### 3. Path resolution helper (if needed)

**Current state:** No path resolution helper exists in `FileSystemHelper`. GOG .info `playTasks.path` values are **relative to the game root** (confirmed by `docs/research/gog_format.md` line 57). `ExecutablePath` must always be absolute — `Process.Start` with `UseShellExecute=true` does NOT resolve relative paths against `WorkingDirectory`.

**Actions:**
- [ ] Resolve relative paths inside `GogInfoParser.TryParse()` using `Path.GetFullPath(Path.Combine(searchDir.FullName, taskPath))`
- [ ] No new `FileSystemHelper` method needed — the resolution is local to GOG parsing

## Context

- **Reference:** `detect.py` lines 811-859 (`_extract_gog_metadata`)
- **GOG .info format** (from `docs/research/gog_format.md`):
  ```json
  {
    "gameId": "12345",
    "rootGameId": "12345",
    "name": "Game Title",
    "playTasks": [
      { "isPrimary": true, "path": "bin/x64/witcher3.exe", "arguments": "--windowed" },
      { "isPrimary": false, "path": "setup.exe", "arguments": "" }
    ]
  }
  ```
- `playTasks.path` values are **relative to the game root** — must be resolved to absolute
- DLC `.info` files have `gameId != rootGameId` — these should be skipped (DLC never has its own exe)
- `goggame-*.info` files may be in root or one level of non-noise subdirs
- Some GOG games only have the `.info` exe path (no root-level `.exe`)
- `FileSystemHelper.IsNoiseDirectory()` already exists for filtering irrelevant subdirectories
- If multiple `.info` files exist (DLC), prefer `gameId == rootGameId`; if no main game found, use first available

## Design Decisions

### Exe selection priority
1. `ExecutableDiscovery` is the primary source (filesystem scan finds actual exes)
2. GOG .info `playTasks.path` is a **fallback** when no exe is found
3. When multiple exes are found, GOG .info path acts as a **hint** to prefer the matching one
4. Future: **Exe selection dialog** — when the user first launches a game with multiple candidates, show a picker. Out of scope for T65.

### Title selection and metadata source tracking
The scan phase picks the best available title, but the user should be able to override it. To support this:

- **Scan phase:** Use GOG .info title as `DisplayName` (official source). Store folder-name-derived title in `PlatformMetadata["AutoDetectedTitle"]`.
- **`PlatformMetadata["TitleSource"]`** tracks where the title came from:
  - `"GogInfo"` — from GOG .info file
  - `"SteamStore"` — from Steam metadata (future)
  - `"AutoDetect"` — from folder name normalization
  - `"UserSupplied"` — manually entered by user (future)
- **Future: Title selection dialog** — similar to exe selection. On first launch or via F4 (edit tags), show: "We detected: `{AutoDetectedTitle}` but GOG says: `{GogInfoTitle}`. Pick one or enter custom title." Store result as `DisplayName` with `TitleSource = "UserSupplied"`. Out of scope for T65.

### PlatformMetadata convention
All game stores should populate `PlatformMetadata` with relevant identifiers for consistency:
- Steam: `SteamAppId`, `SteamStatus`, `AcfExpectedPath`, `AcfLibraryPath`
- GOG: `GogGameId`, `TitleSource`, `AutoDetectedTitle` (this task)
- Future: Epic, EA, etc.

This enables the UI layer (`ShellViewModel`) to display platform-specific status and IDs uniformly.

## Requirements

- [ ] `GogInfoParser` class created with XML docs
- [ ] Parses `goggame-*.info` JSON correctly
- [ ] Extracts primary exe path and launch args from `playTasks`
- [ ] Filters DLC by `gameId == rootGameId`
- [ ] Searches root + 1 level of non-noise subdirs
- [ ] Resolves relative exe paths to absolute via `Path.GetFullPath(Path.Combine(...))`
- [ ] Handles missing files, malformed JSON (`JsonException`/`IOException`), no playTasks gracefully
- [ ] `FolderScanner.AddGameEntry()` uses GOG info for display name, exe (fallback + multi-exe hint), args, and `PlatformMetadata` (`GogGameId`, `TitleSource`, `AutoDetectedTitle`)
- [ ] Existing scanner tests still pass

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (no regressions)
- [ ] Unit test: create temp dir with `goggame-123.info` JSON containing playTasks → verify exe path (absolute) and args extracted
- [ ] Unit test: DLC `.info` (different `gameId`) → skipped
- [ ] Unit test: malformed JSON → no crash, returns false
- [ ] Unit test: relative exe path in `.info` → resolved to absolute path

## Completion Notes

- **Completed:** 2026-07-25
- **What was done:**
  - Created `GogInfoParser.cs` — static class with `TryParse()` method
  - Parses `goggame-*.info` JSON files from root + 1 level of non-noise subdirs
  - Filters DLC by `gameId == rootGameId`, falls back to DLC if no main game found
  - Resolves relative exe paths to absolute via `Path.GetFullPath(Path.Combine(...))`
  - Extracts primary exe and launch args from `playTasks`
  - Handles malformed JSON (`JsonException`/`IOException`), missing files, no playTasks
  - Integrated into `FolderScanner.AddGameEntry()` — GOG .info provides title, exe (fallback), args, and `PlatformMetadata` (`GogGameId`, `TitleSource`, `AutoDetectedTitle`)
  - Created `GogInfoParserTests.cs` — 10 tests covering basic parsing, relative/absolute paths, DLC filtering, edge cases
- **Verification:** Build clean (0 errors), 112 tests passing (0 regressions, +10 new)
- **Issues encountered:** DLC `.info` files were overwriting main game exe — fixed by only extracting `playTasks` from main game entries (or when no exe found yet)
