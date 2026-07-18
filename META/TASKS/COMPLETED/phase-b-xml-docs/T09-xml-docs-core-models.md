# Task T09: XML Docs for Core Models

**Tier:** 3 — XML Documentation
**Phase:** B — XML Documentation
**Effort:** ~30 min
**Risk:** Minimal
**Status:** completed

---

## Objective

Add `/// <summary>` XML documentation to all Core model records and enums. These are the domain types used across the entire application — clear documentation prevents misuse.

## What Needs to Change

### Files and Types

#### 1. `src/GamingCommander.Core/Models/AppConfig.cs`
- `AppConfig` — "Application configuration persisted to settings.json."
- `LibraryRoots` — "Configured library root paths with default game source types."
- `FolderOverrides` — "Per-folder source type overrides that take precedence over root defaults."
- `HiddenFolders` — "Folder names to exclude from game scanning."
- `IsFirstRun` — "True if the first-run wizard has not yet completed."
- `LastSeenVersion` — "Last application version that was launched (for upgrade detection)."
- `EnableOnlineMetadata` — "Whether to query online metadata sources (PCGamingWiki, etc.)."

#### 2. `src/GamingCommander.Core/Models/GameEntry.cs`
- `GameEntry` — "A discovered game entry stored in the games database."
- `Id` — "Deterministic unique identifier (MD5-based)."
- `FolderName` — "Name of the game's installation folder."
- `DisplayName` — "Human-readable game name shown in the UI."
- `GameSource` — "Detected or overridden store/platform type."
- `Override` — "True if the user manually changed the source type."
- `ExecutablePath` — "Path to the primary game executable."
- `LauncherPath` — "Path to the game's launcher executable (if any)."
- `CmdlineArgs` — "Command-line arguments passed to the executable on launch."
- `ManifestPath` — "Path to the launcher manifest file (e.g., Steam ACF)."
- `LastScanned` — "Timestamp of the most recent scan that produced this entry."
- `LastModified` — "Timestamp of the game directory's most recent modification."
- `Extra` — "Platform-specific metadata (e.g., SteamStatus, SteamAppId, AcfExpectedPath)."
- `GameRoot` — "A library root with its associated game entries."
- `GamesDatabase` — "Top-level database of all library roots and their games."

#### 3. `src/GamingCommander.Core/Models/GameRecord.cs`
- `GameRecord` — "Full game record implementing IGame, with accessibility capability flags."
- `SupportsPointerInteraction` — "True if the game supports mouse/pointer input."
- `SupportsKeyboardOnlyFlow` — "True if the game can be navigated with keyboard only."

#### 4. `src/GamingCommander.Core/Models/GameSourceKind.cs`
- `GameSourceKind` — "Identifies the store or platform a game was detected from."
- `Unknown` — "Source could not be determined."
- `Standalone` — "Standalone game with no launcher integration."
- `Steam` — "Valve Steam platform."
- `Gog` — "GOG.com (DRM-free)."
- `Epic` — "Epic Games Store."
- `EaApp` — "EA App (formerly Origin)."
- `UbisoftConnect` — "Ubisoft Connect (formerly Uplay)."
- `BattleNet` — "Blizzard Battle.net."
- `Xbox` — "Xbox / Microsoft Store."
- `Rockstar` — "Rockstar Games Launcher."
- `SteamEmu` — "Steam emulator (e.g., CreamAPI, GreenLuma)."

#### 5. `src/GamingCommander.Core/Models/FileSystemEntry.cs`
- `FileSystemEntryKind` — "Type of filesystem entry in the virtual filesystem model."
- `Directory` — "A browsable directory (game folder or library root)."
- `File` — "A file entry (game executable)."
- `ParentDirectory` — "The '..' parent directory entry."
- `FileSystemEntry` — "A single entry in the virtual filesystem (directory, file, or parent)."
- `Name` — "Display name of the entry."
- `FullPath` — "Absolute path to the filesystem entry."
- `Kind` — "Whether this is a directory, file, or parent entry."
- `LastModified` — "Timestamp of the last modification."
- `Size` — "File size in bytes (0 for directories)."

#### 6. `src/GamingCommander.Core/Models/LibraryRoot.cs`
- `LibraryRoot` — "A configured library root path with its default game source type."
- `Path` — "Absolute path to the library root directory."
- `DefaultType` — "Default source type assigned to games found under this root."
- `FolderOverride` — "A per-folder source type override that takes precedence over the root default."
- `FolderPath` — "Absolute path to the folder being overridden."
- `Type` — "The source type to assign to games in this folder."

#### 7. `src/GamingCommander.Core/Models/MigrationMode.cs`
- `MigrationMode` — "Determines what actions are taken when migrating a game to a new location."
- `MoveOnly` — "Move game files only; no manifest repair or link creation."
- `MoveAndLink` — "Move game files and create a symbolic link at the original location (deprecated)."
- `ManifestRepairOnly` — "Repair launcher registration without moving files."

#### 8. `src/GamingCommander.Core/Models/MigrationPlanSummary.cs`
- `MigrationPlanSummary` — "Dry-run output describing what a migration would do."
- `GameId` — "ID of the game being migrated."
- `SourcePath` — "Current installation path."
- `TargetPath` — "Proposed new installation path."
- `Mode` — "Migration mode (move, link, or manifest repair)."
- `RequiresManifestBackup` — "True if the launcher manifest must be backed up before migration."
- `RequiresLinkCreation` — "True if a symbolic link should be created at the original path (deprecated)."
- `IsDryRunOnly` — "True if this is a dry-run plan (no changes will be made)."

#### 9. `src/GamingCommander.Core/Models/GameSourceParser.cs` — Already has docs. Skip.

## Context

- `GameSourceParser.cs` already has `/// <summary>` — preserve it
- `GameEntryId.cs` already has `/// <summary>` — preserve it
- These models are used by Core, Detection, UI, and App layers
- The `Extra` dictionary on `GameEntry` is a catch-all for platform-specific data — document common keys

## Requirements

- [ ] Add `/// <summary>` to every record, enum, and property
- [ ] Keep descriptions concise (1 sentence per member)
- [ ] For enums: explain each variant
- [ ] For `GameEntry.Extra`: document common keys (SteamStatus, SteamAppId, AcfExpectedPath, AcfLibraryPath)
- [ ] Preserve any existing documentation

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] All model files in `Core/Models/` have `/// <summary>` on every type and member

## Completion Notes

- **Completed:** 2026-07-18
- **What was done:** Added `/// <summary>` XML documentation to 8 Core model files:
  1. `AppConfig.cs` — record + 6 properties documented
  2. `GameEntry.cs` — 3 records (GameEntry, GameRoot, GamesDatabase) + all properties documented
  3. `GameRecord.cs` — record + 9 properties documented
  4. `GameSourceKind.cs` — enum + 11 variants documented
  5. `FileSystemEntry.cs` — enum (3 variants) + record (5 properties) documented
  6. `LibraryRoot.cs` — 2 records + all properties documented
  7. `MigrationMode.cs` — enum + 3 variants documented
  8. `MigrationPlanSummary.cs` — record + 7 properties documented
- **Skipped:** `GameSourceParser.cs` and `GameEntryId.cs` (already had docs)
- **Verification:** Build clean, 17 tests passing
- **No issues encountered.**
