# Task T11: XML Docs for ViewModels

**Tier:** 3 — XML Documentation
**Phase:** B — XML Documentation
**Effort:** ~30 min
**Risk:** Low
**Status:** completed

---

## Objective

Add `/// <summary>` XML documentation to ViewModels and their properties. ViewModels are the glue between UI and services — their responsibilities and property semantics need clear documentation.

## What Needs to Change

### Files to Document

#### 1. `src/GamingCommander.UI/ViewModels/ShellViewModel.cs`
Currently: 14 public methods, ~20 properties, zero documentation.

Add class-level summary:
- `ShellViewModel` — "Primary dual-pane shell ViewModel. Manages navigation between library roots and game entries, item selection, details panel, status bar, and platform metadata display."

Add docs to key properties:
- `LeftPaneTitle` — "Title displayed in the left pane header (root name or truncated path)."
- `RightPaneTitle` — "Title displayed in the right pane header ('Details')."
- `IsAtRootLevel` — "True when viewing the top-level library root list (not inside a root)."
- `Items` — "Observable collection of items displayed in the left pane."
- `SelectedIndex` — "Index of the currently selected item in the left pane. -1 if nothing selected."
- `SelectedItem` — "The currently selected ShellPaneItemViewModel, or null."
- `StatusText` — "Text shown in the bottom status bar."
- `InteractionHint` — "Context-sensitive hint text shown below the item list."
- `HasGameSelected` — "True when a game file (not a directory or parent) is selected."

Add docs to key methods:
- `JumpToLibraryRoots()` — "Populates the item list with configured library roots."
- `LoadGamesForRoot(string)` — "Populates the item list with games from the specified root. Maps platform metadata to display colors and status text."
- `NavigateInto()` — "Drills into the selected item (root → games, directory → sub-entries) or launches a game."
- `NavigateUp()` — "Goes up one level (games → roots, or no-op if already at roots)."
- `RetagSelected(GameSourceKind)` — "Updates the source type of the selected game."
- `Reload()` — "Refreshes the current view by re-scanning or re-loading from database."

Add docs to events:
- `NavigationChanged` — "Raised after navigation completes. Subscribers should re-focus the left pane."
- `RequestLaunch` — "Raised when a game should be launched. Subscribers handle the actual process start."

#### 2. `src/GamingCommander.UI/ViewModels/ShellPaneItemViewModel.cs`
Partially documented (PlatformId, PlatformStatus, PlatformStatusColor, PlatformStatusDetail, ItemStatusColor have docs).

Add docs to undocumented properties:
- `Title` — "Display name of the item (game name or directory name)."
- `SourceLabel` — "Short label for the game's source type (e.g., 'Steam', 'Standalone')."
- `PathSummary` — "Truncated path summary for display (~50 chars max)."
- `LaunchTarget` — "Path used to launch the game when Enter is pressed."
- `Kind` — "Whether this is a directory, file, or parent directory entry."
- `IsBrowsable` — "True if this item can be drilled into (directory or parent)."
- `LastModified` — "Timestamp of the last modification to this item."
- `ResolvedType` — "The effective source type after applying overrides."
- `HasOverride` — "True if the user manually changed this item's source type."
- `GameId` — "Database ID of the game, or null for non-game items."
- `GameCount` — "Number of games in this root (only set for root-level entries)."

#### 3. `src/GamingCommander.UI/ViewModels/ReactiveObject.cs`
Currently: 2 methods, zero documentation.

- `ReactiveObject` — "Base class for ViewModels implementing INotifyPropertyChanged with a SetProperty helper."
- `OnPropertyChanged(string?)` — "Raises the PropertyChanged event for the specified property."
- `SetProperty<T>(ref T, T, string?)` — "Sets the backing field and raises PropertyChanged if the value changed. Returns true if the value was updated."

#### 4. `src/GamingCommander.UI/ViewModels/ShellCommandViewModel.cs`
Currently: 2 properties, zero documentation.

- `ShellCommandViewModel` — "Represents a single command button in the bottom command bar."
- `Hotkey` — "The F-key or keyboard shortcut that triggers this command (e.g., 'F1', 'F5')."
- `Label` — "Display text shown on the command button."

#### 5. `src/GamingCommander.App/ViewModels/WizardViewModel.cs`
Currently: 7 public methods, several properties, zero documentation.

- `WizardViewModel` class — "ViewModel for the first-run wizard. Scans configured library folders, presents results, and saves initial configuration."
- Key properties: `_selectedType`, `_gameCount`, `_isScanned`, `_isScanning` — document each.
- Key methods: document each public method.

#### 6. `src/GamingCommander.App/ViewModels/LibrarySetupViewModel.cs`
Currently: 5 public methods, nested class, zero documentation.

- `LibrarySetupViewModel` class — "ViewModel for the F2 Library Setup dialog. Manages adding, removing, and rescanning library roots."
- Key methods: document each public method.
- Nested `LibraryRootEntry` class — document purpose.

## Context

- `ShellPaneItemViewModel` already has partial docs (5 of 14 properties) — preserve and extend
- `ShellViewModel` is the most complex ViewModel (340 lines) — focus on public API, not internals
- `ReactiveObject` is tiny (25 lines) — quick doc pass
- `WizardViewModel` and `LibrarySetupViewModel` are in the App project, not UI — still ViewModels

## Requirements

- [ ] Add `/// <summary` to every public class, property, method, and event
- [ ] Preserve existing documentation in ShellPaneItemViewModel
- [ ] Keep descriptions concise (1 sentence per member)
- [ ] For ShellViewModel: focus on public API, skip internal methods
- [ ] For events: document when they fire and what subscribers should do

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] All ViewModel files have `/// <summary>` on every public member

## Completion Notes

- **Completed:** 2026-07-18
- **What was done:** Added `/// <summary>` XML documentation to 6 ViewModel files:
  1. `ShellViewModel.cs` — class + 2 events + 9 properties + 6 methods documented
  2. `ShellPaneItemViewModel.cs` — class + 14 properties documented (extended existing partial docs)
  3. `ReactiveObject.cs` — class + 2 methods documented
  4. `ShellCommandViewModel.cs` — class + 2 properties documented
  5. `WizardViewModel.cs` — class + 4 properties + 5 methods documented; WizardLibraryEntry class + 5 properties documented
  6. `LibrarySetupViewModel.cs` — class + 1 property + 5 methods documented; LibraryRootEntry class + 3 properties documented
- **Verification:** Build clean, 17 tests passing
- **No issues encountered.**
