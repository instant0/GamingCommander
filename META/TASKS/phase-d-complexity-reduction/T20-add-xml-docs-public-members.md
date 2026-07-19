# Task T20: Add XML Docs to All Public Members (Phase D)

**Tier:** 1 — Documentation
**Phase:** D — Complexity Reduction
**Effort:** ~40 min
**Risk:** Minimal
**Status:** ✅ completed
**Prerequisites:** T18 must run first (removes 3 `AvailableTypes` properties that were incorrectly listed here)

---

## Objective

~20 public members across the codebase lack `/// <summary>` XML documentation. This makes the code harder to understand for junior developers and AI agents. Add documentation to every undocumented public member.

## What Needs to Change

### 1. `src/GamingCommander.App/GameSetupWindow.axaml.cs`

**Current state:** 7 public members without docs (`AvailableTypes` removed by T18)
**Actions:**
- [ ] Add `/// <summary>` to constructor: "F4 game editing dialog. Allows user to modify game metadata (name, type, executable, launcher, args)."
- [ ] Add `/// <summary>` to `DisplayName`: "The editable display name of the game."
- [ ] Add `/// <summary>` to `SelectedType`: "The currently selected game source type."
- [ ] Add `/// <summary>` to `ExecutablePath`: "The full path to the game's primary executable."
- [ ] Add `/// <summary>` to `LauncherPath`: "The full path to the game's launcher executable (if any)."
- [ ] Add `/// <summary>` to `CmdlineArgs`: "Command-line arguments passed to the game on launch."
- [ ] Add `/// <summary>` to `ManifestPath`: "The path to the game's store manifest file (Epic .item, etc.)."

### 2. `src/GamingCommander.App/WizardWindow.axaml.cs`

**Current state:** 1 public member without docs
**Actions:**
- [ ] Add `/// <summary>` to constructor: "First-run wizard window. Guides user through initial library root configuration."

### 3. `src/GamingCommander.App/LibrarySetupWindow.axaml.cs`

**Current state:** 1 public member without docs
**Actions:**
- [ ] Add `/// <summary>` to constructor: "F2 library setup window. Allows user to manage library roots and folder overrides."

### 4. `src/GamingCommander.App/MainWindow.axaml.cs`

**Current state:** 1 public member without docs
**Actions:**
- [ ] Add `/// <summary>` to constructor: "Primary application window. Manages dual-pane navigation, keyboard shortcuts, and game launching."

### 5. `src/GamingCommander.App/Services/HexToBrushConverter.cs`

**Current state:** 2 public members without docs
**Actions:**
- [ ] Add `/// <summary>` to `Convert()`: "Converts a hex color string (e.g., '#FF0000') to a SolidColorBrush. Empty string returns the default text brush."
- [ ] Add `/// <summary>` to `ConvertBack()`: "Not supported. Returns null."

### 6. `src/GamingCommander.App/Services/BlacklistLoader.cs`

**Current state:** 1 public member without docs
**Actions:**
- [ ] Add `/// <summary>` to constructor: "Loads noise patterns from data/blacklist.json. Falls back to hardcoded defaults if file is missing."

### 7. `src/GamingCommander.UI/ViewModels/ShellViewModel.cs`

**Current state:** ~19 public members without docs (`HasGameSelected` already has docs on line 95 — do NOT re-document it)
**Actions:**
- [ ] Add `/// <summary>` to constructor: "Creates the shell ViewModel with navigation, selection, and details panel state."
- [ ] Add `/// <summary>` to `DetailsName`: "Display name of the currently selected game, shown in the details panel."
- [ ] Add `/// <summary>` to `DetailsPath`: "Install path of the currently selected game."
- [ ] Add `/// <summary>` to `DetailsType`: "Source type of the currently selected game (e.g., Steam, GOG)."
- [ ] Add `/// <summary>` to `DetailsExecutable`: "Primary executable path of the currently selected game."
- [ ] Add `/// <summary>` to `DetailsLastModified`: "Last modification timestamp of the game's installation directory."
- [ ] Add `/// <summary>` to `DetailsResolvedType`: "The resolved source type as a human-readable string."
- [ ] Add `/// <summary>` to `DetailsPlatformId`: "Platform-specific identifier (Steam App ID, Epic Catalog ID, etc.)."
- [ ] Add `/// <summary>` to `HasPlatformId`: "True when a platform-specific identifier is available for the selected game."
- [ ] Add `/// <summary>` to `DetailsPlatformStatus`: "Platform status text (Installed, Moved, Orphaned, Missing)."
- [ ] Add `/// <summary>` to `HasPlatformStatus`: "True when platform status information is available."
- [ ] Add `/// <summary>` to `DetailsPlatformStatusColor`: "Hex color code for the platform status display."
- [ ] Add `/// <summary>` to `DetailsPlatformStatusDetail`: "Detailed status text (e.g., 'Moved — ACF expects: D:\\...')."
- [ ] Add `/// <summary>` to `HasPlatformStatusDetail`: "True when detailed platform status information is available."
- [ ] Add `/// <summary>` to `HasSelection`: "True when any item is selected in the left pane."
- [ ] Add `/// <summary>` to `HasOverride`: "True when the selected game has a user-defined folder override."
- [ ] Add `/// <summary>` to `Commands`: "Hotkey-to-action mappings displayed in the bottom command bar."
- [ ] Add `/// <summary>` to `CurrentRootPath`: "The full path of the currently browsed library root."
- [ ] Add `/// <summary>` to `ConfiguredRootsCount`: "Number of configured library roots."
- [ ] Add `/// <summary>` to `ItemCount`: "Number of items currently displayed in the left pane."
- [ ] Add `/// <summary>` to `GetCurrentRootPath()`: "Returns the full path of the currently browsed library root, or null if at root level."
- [ ] Add `/// <summary>` to `GetSelectedGameId()`: "Returns the ID of the currently selected game, or null if no game is selected."

### 8. `src/GamingCommander.App/Program.cs`

**Current state:** 2 public members without docs
**Actions:**
- [ ] Add `/// <summary>` to `Main()`: "Application entry point. Configures Avalonia and starts the app."
- [ ] Add `/// <summary>` to `BuildAvaloniaApp()`: "Configures the Avalonia application builder with platform-specific settings."

## Context

- All changes are documentation-only — no logic, no behavior
- XML docs improve IDE tooltips, IntelliSense, and AI agent comprehension
- `HasGameSelected` on line 95 already has `/// <summary>` — skip it
- `Commands`, `CurrentRootPath`, `ConfiguredRootsCount`, `ItemCount` were missing from the original task — added here
- T18 removes `AvailableTypes` from 3 files — those entries removed from this task

## Requirements

- [ ] All ~20 public members listed above have `/// <summary>` XML docs
- [ ] `HasGameSelected` is NOT re-documented (already has docs)
- [ ] Descriptions are 1-2 sentences, explaining purpose not implementation
- [ ] No logic changes
- [ ] No `using` statement changes

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] Spot-check: Open any of the listed files in an IDE — all public members should have tooltip summaries

## Completion Notes

- **Completed:** 2026-07-19
- **What was done:** Added `/// <summary>` XML docs to ~20 public members across GameSetupWindow, WizardWindow, LibrarySetupWindow, MainWindow, HexToBrushConverter, BlacklistLoader, ShellViewModel, and Program.
- **Verification:** Build clean, all tests passing.
- **Issues encountered:** None.
