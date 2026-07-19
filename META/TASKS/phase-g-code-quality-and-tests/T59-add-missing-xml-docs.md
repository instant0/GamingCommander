# Task T59: Add Missing XML Docs Across Codebase

**Tier:** 2 — Code Quality
**Phase:** G — Code Quality & Tests
**Effort:** ~30 min
**Risk:** Minimal
**Status:** completed
**Prerequisites:** T58 (naming fixes first, so docs reference correct names)

---

## Objective

Audit found 29 missing XML doc comments across the codebase. These are private methods and public members without `/// <summary>` tags. While private method docs are optional per project guidelines, the affected methods are complex enough (signal detection, scanning logic, UI building) that documentation aids discoverability.

## What Needs to Change

### `src/GamingCommander.App/Services/FolderScanner.cs` (9 methods)
- [ ] Line 159: `HasSteamEmuDeepSignal` — "Checks for Steam emulator deep signals: root-level INI, child-level DLLs, and UE Steamworks path."
- [ ] Line 190: `HasUbisoftLegacySignal` — "Checks for legacy Ubisoft launcher signals: UbiStats.dll or Ubisoft.ini."
- [ ] Line 209: `HasUnrealLayoutSignal` — "Checks for Unreal Engine directory layout: Engine/ folder with Binaries/Win64/ containing exes."
- [ ] Line 238: `HasRootExecutableSignal` — "Checks if the game folder contains non-noise executables at the root level."
- [ ] Line 253: `HasRootLnkSignal` — "Checks for .lnk shortcut files at the root level that point to executables."
- [ ] Line 270: `ScanContainerChildren` — "Recursively scans child directories of a container (store/publisher folder) for game entries."
- [ ] Line 293: `IsNoiseDirectory` — "Checks if a directory name matches known noise patterns (saves, mods, workshops, etc.)."
- [ ] Line 327: `AddGameEntry` — "Creates a GameEntry from a scanned folder and adds it to the results list."

### `src/GamingCommander.App/Services/SteamLibraryScanner.cs` (5 methods)
- [ ] Line 159: `DiscoverAllSteamPaths` — "Discovers all Steam library paths from libraryfolders.vdf and configured paths."
- [ ] Line 181: `CollectAcfMap` — "Builds a map of AppId → ACF metadata from all known Steam library paths."
- [ ] Line 213: `CreateEntry` — "Creates a GameEntry for an installed Steam game from its ACF metadata."
- [ ] Line 251: `CreateOrphanedEntry` — "Creates a GameEntry for a Steam game whose ACF exists but game files are missing."
- [ ] Line 311: `FindPrimaryExe` — "Finds the primary executable in a Steam game's common/ directory."

### `src/GamingCommander.App/Services/ExecutableDiscovery.cs` (2 methods)
- [ ] Line 273: `IsNoiseExeByPath` — "Checks if an executable file path matches any noise pattern. Extracts filename before checking."
- [ ] Line 279: `IsNoiseDirectory` — "Checks if a directory name matches known noise patterns (saves, mods, etc.)."

### `src/GamingCommander.App/Services/BlacklistData.cs` (1 member)
- [ ] Line 21: `public static readonly BlacklistData Empty` — "Empty singleton instance representing no blacklist data."

### `src/GamingCommander.App/AppTheme.cs` (2 methods)
- [ ] Line 59: `Get(string key)` — "Resolves a SolidColorBrush from the Application resource dictionary by semantic key."
- [ ] Line 70: `GetDouble(string key)` — "Resolves a double value from the Application resource dictionary by semantic key."

### `src/GamingCommander.App/GameSetupWindow.axaml.cs` (2 methods)
- [ ] Line 188: `SaveAndClose()` — "Saves the edited game entry to the database and closes the dialog."
- [ ] Line 212: `DeleteAndClose()` — "Deletes the game entry from the database and closes the dialog."

### `src/GamingCommander.Core/Models/MigrationMode.cs` (1 member)
- [ ] Line 12: `MoveAndLink = 1` — Clarify in existing doc: "Move files and create a symbolic link at the original location pointing to the new location."

### `src/GamingCommander.App/Services/HelpDialogBuilder.cs` (extract helper)
- [ ] Lines 20-127: `ShowHelpAsync` is 108 lines — extract `BuildKeyboardSection()` and `BuildHeaderSection()` helpers, document each

## Context

- All 29 missing docs are on private methods or specific members
- The methods are in complex scanning/detection logic where context matters
- Public API members already have docs (from Phase D T20)
- Adding docs improves IDE tooltips and AI agent comprehension

## Requirements

- [ ] All 29 missing XML docs added
- [ ] Docs are 1 sentence, describing purpose not implementation
- [ ] HelpDialogBuilder refactor extracts 2 helpers (bonus, not blocking)

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes
- [ ] Spot-check: open FolderScanner.cs in IDE, verify all `Has*Signal` methods have tooltips

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
