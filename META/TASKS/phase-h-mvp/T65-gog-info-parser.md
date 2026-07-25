# Task T65: GOG goggame-*.info Parser

**Tier:** 3 — Logic/Behavior
**Phase:** H — MVP
**Effort:** ~45 min
**Risk:** Medium
**Status:** Pending
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
      string ExePath,
      string LaunchArgs);
  ```
- [ ] Implement `TryParse(DirectoryInfo dir, out GogGameInfo? info)`:
  - Search root + 1 level of subdirs for `goggame-*.info` files
  - For each file, parse as JSON (`System.Text.Json`)
  - Find the main game entry: `gameId == rootGameId` (DLC files have different IDs)
  - Extract from `playTasks` array: find task where `isPrimary == true`
  - Read `path` (exe) and `arguments` (launch args) from the primary playTask
  - Return `GogGameInfo` with title, exe path, and args
- [ ] Handle edge cases:
  - No `.info` files found → return false
  - Malformed JSON → skip file, try next
  - No `playTasks` → return false
  - No primary task → use first task with a `path`
  - Relative exe paths → resolve against the `.info` file's directory

### 2. `src/GamingCommander.App/Services/FolderScanner.cs` — `AddGameEntry()`

**Current state:** Line ~344-357 creates `GameEntry` with `CommandLineArguments: string.Empty` for all games. GOG detection (line ~64) returns a boolean but no metadata.

**Actions:**
- [ ] After `StoreSignalDetector.DetectType()` returns `GameSourceKind.Gog`, call `GogInfoParser.TryParse(subDir, out var gogInfo)`
- [ ] If `gogInfo` is not null:
  - Use `gogInfo.Title` for `DisplayName` if available (override `NormalizeDisplayName(subDir.Name)`)
  - Use `gogInfo.ExePath` for `ExecutablePath` if `exePath` from `ExecutableDiscovery` is empty
  - Set `CommandLineArguments = gogInfo.LaunchArgs`
- [ ] Ensure the GOG `.info` exe path is resolved relative to the `.info` file's directory (not the game root)

## Context

- **Reference:** `detect.py` lines 811-859 (`_extract_gog_metadata`)
- GOG `.info` format:
  ```json
  {
    "gameId": "12345",
    "rootGameId": "12345",
    "name": "Game Title",
    "playTasks": [
      { "isPrimary": true, "path": "game.exe", "arguments": "--windowed" },
      { "isPrimary": false, "path": "setup.exe", "arguments": "" }
    ]
  }
  ```
- DLC `.info` files have `gameId != rootGameId` — these should be skipped
- `goggame-*.info` files may be in root or one level of subdirs
- The `.info` directory is the working directory for relative exe paths
- Some GOG games only have the `.info` exe path (no root-level `.exe`)

## Requirements

- [ ] `GogInfoParser` class created with XML docs
- [ ] Parses `goggame-*.info` JSON correctly
- [ ] Extracts primary exe path and launch args from `playTasks`
- [ ] Filters DLC by `gameId == rootGameId`
- [ ] Searches root + 1 level of subdirs
- [ ] Handles missing files, malformed JSON, no playTasks gracefully
- [ ] `FolderScanner.AddGameEntry()` uses GOG info for display name, exe, and args when available
- [ ] Existing scanner tests still pass

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (no regressions)
- [ ] Unit test: create temp dir with `goggame-123.info` JSON containing playTasks → verify exe path and args extracted
- [ ] Unit test: DLC `.info` (different `gameId`) → skipped
- [ ] Unit test: malformed JSON → no crash, returns false

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
