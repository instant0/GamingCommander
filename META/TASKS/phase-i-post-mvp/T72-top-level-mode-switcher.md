# Task T72: Top-Level Mode Switcher (Library / Store / Engine)

**Tier:** 1 — Feature
**Phase:** I — Post-MVP Navigation
**Effort:** ~2–3 hours
**Risk:** Medium
**Status:** Pending
**Prerequisites:** MVP complete (T70), T71 (F5 removed)
**WP:** Post-MVP (Plan 101, Phase 1)

---

## Objective

Replace F9's "Jump to Library Roots" behavior with a **3-mode cycle** that toggles the top-level view between Library (filesystem roots), Store (game platforms), and Engine (game engines). This gives users semantic browsing instead of only filesystem-path browsing.

## What Needs to Change

### 1. New File: `src/GamingCommander.Core/Models/GameEngineKind.cs`

Create enum:
```csharp
public enum GameEngineKind
{
    Unknown = 0,
    UnrealEngine = 1,
    Unity = 2,
    Rage = 3,
    Frostbite = 4,
}
```

### 2. New File: `src/GamingCommander.App/Services/EngineDetector.cs`

Port `_detect_engine()` from `tools/detect.py` (lines 769–804):

- `DetectEngine(DirectoryInfo dir)` → `GameEngineKind`
- `_hasUnrealEngine(dir)` → `Engine/` dir + `Binaries/` child
- `_hasUnity(dir)` → `UnityPlayer.dll` + `*_Data/` dir
- `_hasRage(dir)` → `title.rgl` + `common.rpf`
- `_hasFrostbite(dir)` → `Engine.BuildInfo_Win64_retail.dll`

### 3. `src/GamingCommander.Core/Models/GameEntry.cs`

Add field:
```csharp
GameEngineKind GameEngine = GameEngineKind.Unknown
```

### 4. `src/GamingCommander.App/Services/FolderScanner.cs`

In `AddGameEntry()`, after exe discovery:
- Call `EngineDetector.DetectEngine(gameDir)` 
- Set `GameEngine` on the `GameEntry`

### 5. `src/GamingCommander.App/Services/GamesDatabaseService.cs`

Add `GameEngine` to `GameEntryDto`:
```csharp
public GameEngineKind GameEngine { get; set; }
```

Update Load/Save mapping to include `GameEngine`. Backward-compatible: missing field defaults to `Unknown`.

### 6. `src/GamingCommander.UI/ViewModels/ShellViewModel.cs`

Add view mode enum and cycling logic:

```csharp
public enum TopLevelViewMode { Library, Store, Engine }

private TopLevelViewMode _viewMode = TopLevelViewMode.Library;
```

**New methods:**
- `CycleTopLevelViewMode()` — cycles Library → Store → Engine → Library
- `LoadStoreView()` — populates Items with game stores (grouped by `GameSourceKind`)
- `LoadEngineView()` — populates Items with game engines (grouped by `GameEngine`)

**Modify `JumpToLibraryRoots()`:**
- Now checks `_viewMode` and dispatches to `LoadStoreView()` or `LoadEngineView()` as appropriate
- Left pane title changes: `"Library Roots"` / `"Game Stores"` / `"Game Engines"`

**Store/Engine entries:**
Each store/engine is a `ShellPaneItemViewModel` with:
- `Title` = display name (e.g., "Steam", "Unreal Engine")
- `SourceLabel` = mode name (e.g., "Store", "Engine")
- `PathSummary` = game count (e.g., "45 games")
- `Kind` = `Directory` (drillable)
- `GameCount` = number of games in group

**Drill-in:**
`NavigateInto()` for Store/Engine entries:
- Store entry → call `GetGamesByStore(GameSourceKind)` 
- Engine entry → call `GetGamesByEngine(GameEngineKind)`
- Both aggregate across all roots

### 7. `src/GamingCommander.App/Services/LibraryManager.cs` (or `ILibraryManager`)

Add methods:
```csharp
IReadOnlyList<GameEntry> GetAllGames();                           // aggregate all roots
IReadOnlyList<GameEntry> GetGamesByStore(GameSourceKind store);  // filter by store
IReadOnlyList<GameEntry> GetGamesByEngine(GameEngineKind engine); // filter by engine
```

Implementation: iterate all `LibraryRoots`, collect games from each, filter in memory.

### 8. `src/GamingCommander.App/MainWindow.axaml.cs`

**F9 handler (line 160):**
Replace `_viewModel.JumpToLibraryRoots()` with `_viewModel.CycleTopLevelViewMode()`.

**Command dispatcher (line 434):**
Same change for `case "F9":`.

### 9. `src/GamingCommander.App/MainWindow.axaml`

Update F9 button label binding (currently hardcoded `Label = "Library Roots"` in Commands collection — already dynamic via `ShellViewModel.Commands`, so label updates automatically when Commands[8].Label changes).

### 10. `src/GamingCommander.App/Services/HelpDialogBuilder.cs`

Update F9 description:
- Current: `"Jump to Library Roots"`
- New: `"Cycle view mode: Library / Store / Engine"`

### 11. InteractionHint update

In `ShellViewModel.cs` constructor, update hint:
- Current: `"...F9: Library Roots"`
- New: `"...F9: Mode [current]"` (e.g., `"F9: Mode [Library]"`, `"F9: Mode [Store]"`, `"F9: Mode [Engine]"`)

The hint updates dynamically when mode changes.

## Context

- **Engine detection** exists in `detect.py` (`_detect_engine()`, lines 769–804) covering Unreal, Unity, RAGE, Frostbite. The C# `FallbackSignalDetector.HasUnrealLayoutSignal()` already checks for Unreal layout but doesn't persist the result. `EngineDetector` extracts this into a standalone, reusable component.
- **Store grouping** uses existing `GameEntry.GameSource` (`GameSourceKind` enum with 11 values). No new data needed.
- **Cross-root aggregation** is new — `LibraryManager` currently only serves per-root queries. `GetAllGames()` / `GetGamesByStore()` / `GetGamesByEngine()` aggregate in memory.
- F9 cycling is discoverable: status bar shows current mode, hint text says "F9: mode". Users learn by pressing it.
- Backspace and ".." already handle "go up" navigation, so no dedicated "jump to top" button is needed.

## Requirements

- [ ] `GameEngineKind` enum created
- [ ] `EngineDetector` class created with 4 engine probes
- [ ] `GameEntry.GameEngine` field added with backward-compatible default
- [ ] `FolderScanner` sets `GameEngine` during scan
- [ ] `GamesDatabaseService` persists/loads `GameEngine`
- [ ] `LibraryManager` has `GetAllGames()`, `GetGamesByStore()`, `GetGamesByEngine()`
- [ ] `ShellViewModel` has `TopLevelViewMode` enum and `CycleTopLevelViewMode()`
- [ ] Store view groups games by `GameSourceKind` across all roots
- [ ] Engine view groups games by `GameEngine` across all roots
- [ ] F9 cycles Library → Store → Engine → Library
- [ ] Left pane title reflects current mode
- [ ] Status bar shows mode and game count
- [ ] InteractionHint updated to `"...F9: mode"`
- [ ] HelpDialogBuilder F9 description updated
- [ ] Build clean, existing tests pass

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (no regressions)
- [ ] `grep -rn "Jump to Library Roots" src/` returns no matches
- [ ] Manual: press F9 → mode cycles, status bar updates
- [ ] Manual: drill into a store → shows games from that store
- [ ] Manual: drill into an engine → shows games from that engine
- [ ] Manual: Backspace returns to mode list
- [ ] Existing Library mode still works as before

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
