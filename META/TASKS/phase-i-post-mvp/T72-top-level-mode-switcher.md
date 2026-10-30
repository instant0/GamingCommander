# Task T72: Top-Level View Modes (F9) — Library / Stores / Engines

**Tier:** 1 — Feature
**Phase:** I — Post-MVP Navigation
**Risk:** Medium
**Status:** ready (spec rewritten 2026-10-30)
**Prerequisites:** None. The engine data this task needs already shipped (Plan 102 Phase 2 / Plan 103).

> Flat mode is a separate task (T73). This task must leave a clean seam for it: one enum, one switch,
> one partial file. Do not add Flat here.

## Objective

Replace the single "Library Roots" top level with a small view-mode state machine. F9 cycles
**Library → Stores → Engines → Library**. Stores and Engines are group lists built from the in-memory
catalogue, with drill-in scoped to one group. No filesystem I/O in the aggregate views.

## What already exists — do not redo

| Old T72 step | Reality |
|---|---|
| `GameEngineKind` enum | `Core/Models/GameEngineKind.cs` |
| `EngineDetector` | `App/Services/EngineDetector.cs`, ported in Plan 102/103 |
| `GameEntry.GameEngine` | present, persisted by `GamesDatabaseService` |
| `FolderScanner` sets it | done |
| F9 binding | none — F9 is free (F1–F5, F8, F10 are bound) |
| `JumpToLibraryRoots()` | exists; it is the **reset** used by startup, filter clear, search end, and up-navigation (`ShellViewModel.Filtering.cs:32`, `.Search.cs:68`, `.cs:62`, `.Navigation.cs:103,139`) |

`ShellViewModel` is a partial class (`Navigation`, `Filtering`, `Search`, `Loading`, `Scanning`,
`Details*`, `Tags`). New mode code goes in a new `ShellViewModel.ViewModes.cs`.

## Design

### 1. State

New partial `ShellViewModel.ViewModes.cs`:

```csharp
public enum ShellViewMode { Library, Stores, Engines }   // T73 adds Flat

public ShellViewMode ViewMode { get; private set; }      // default Library
public string? ScopedGroup { get; private set; }         // store kind name, or engine label
public bool IsAggregateView => ViewMode != ShellViewMode.Library;

public void CycleViewMode();                             // Library → Stores → Engines → Library
```

Rules:

- `CycleViewMode()` resets `ScopedGroup`, rebuilds the list, updates title/hint/status, keeps the
  previous selection index where valid (clamped).
- `JumpToLibraryRoots()` becomes the **Library-mode reset**: it sets `ViewMode = Library` and
  `ScopedGroup = null` in addition to its current work. It must not cycle.
- Do **not** persist the mode. Every run starts in Library. (`settings.json` stays untouched.)
- Do **not** add `GetAllGames`/`GetGamesByStore` to `ILibraryManager`. There is no non-UI consumer yet,
  and the interface has a test fake. Aggregate in the ViewModel by iterating
  `_libraryManager.Libraries` + `GetGamesForLibrary(lib.Name)` — the pattern already used by
  `CollectFilterOptions()`.

### 2. Group lists

`LoadStoreGroups()`:

- group all games by `GameEntry.GameSource`;
- one row per non-empty group, ordered by enum value (`Unknown` last);
- title `GameSourceParser.ToDisplayName(kind)`, subtitle `(N games)`;
- `Kind = Directory`, `GroupKey = kind.ToString()`, `StoreBadge = BuildStoreBadge(kind)`;
- `PathSummary` must **not** carry the group key (it is used as a library name elsewhere). Add
  `public string GroupKey { get; init; } = string.Empty;` to `ShellPaneItemViewModel`.

`LoadEngineGroups()`:

- group by `GameEntry.GameEngine`, **skip `Unknown`** (a row called "Unknown" is noise);
- title `EngineDetector.ToTag(kind)`, subtitle `(N games)`;
- `GroupKey = kind.ToString()`.

If a mode has no groups, show an empty list and set `StatusText` ("No store groups yet. Scan a library
first."). Do not fabricate a row.

### 3. Scoped lists

`LoadGamesForScope()` uses `ScopedGroup` and `ViewMode`:

- Stores → games where `GameSource` matches;
- Engines → games where `GameEngine` matches;
- add the existing `..` parent row at the top ("Back to stores" / "Back to engines");
- project game rows through the existing `ToPaneItem(game, game.Library, leftPathOverride: null)` and
  apply the same status colour/detail/multi-exe extras as `LoadGamesForLibrary`. Extract that extras
  block into `ApplyLibraryExtras(item, game)` so both loaders share it (no duplicated projection).

### 4. Navigation semantics

`NavigateInto()` order:

1. `ParentDirectory` → `NavigateUp()`.
2. Aggregate mode at group level and the row has `GroupKey` → set `ScopedGroup`, load the scoped list.
3. `File` → launch (existing).
4. Library mode → existing root/drill behaviour.

`NavigateUp()`:

| Current state | Result |
|---|---|
| scoped list | group list of the same mode |
| group list (Stores/Engines) | `JumpToLibraryRoots()` (T73 changes this target to Flat) |
| Library, inside a root | roots (existing) |
| Library, at roots | no-op (existing) |

Filters and search:

- Applying an F8 filter or typing a search is a **transient pseudo-mode**. `ApplyFilter` keeps its
  current behaviour but also sets `ViewMode = Library` and clears `ScopedGroup`.
- F9 while a filter or search is active: clear it (`ClearFilter()`/`CancelSearch()`), then cycle. One
  key, no dead state. Document it in Help.

### 5. Titles, hint, command bar

- `LeftPaneTitle`: keep the existing search/filter cases; then
  `Stores` → `"Stores"`, `Engines` → `"Engines"`, scoped → `"Store: Steam"` / `"Engine: Unreal Engine"`,
  Library → existing `"Library Roots"` / path.
- `InteractionHint`: append `| F9: View` (static; the current property has a private setter and no
  binding to a dynamic label).
- `Commands`: add `new ShellCommandViewModel { Hotkey = "F9", Label = "View" }` before F10.
  `ShellCommandViewModel.Label` is init-only, so it stays static; the status bar carries the mode.
- `HelpDialogBuilder`: add `("F9", "Cycle view: Library / Stores / Engines")`; T73 extends the list
  with Flat.

### 6. Key and command wiring

- `App/MainWindow.Navigation.cs`: `case Key.F9: _viewModel.CycleViewMode(); e.Handled = true;`
- `App/MainWindow.Commands.cs`: `case "F9": _viewModel.CycleViewMode(); break;`
- No other key changes. Ctrl+Enter / chords belong to T73.

### 7. F5 rescan

`RefreshCurrentRootAsync` currently rescans all libraries when `IsAtRootLevel || IsFilterActive`.
Change the condition to `IsAtRootLevel || IsFilterActive || IsAggregateView`, so F5 in Stores/Engines
rescans all libraries, then reloads the current mode (the ViewModel already reloads after a scan).

### 8. Performance

- Group rebuild is O(games) over the cached `GamesDatabase`; no disk, no allocation per group beyond the
  rows.
- Do not add caching, indexes, or background work. `GameStoreIndex.ByStore` exists but is not needed for
  a single pass; use it only if a future consumer needs lookups.
- No `Directory.*`/`File.*` call in the aggregate loaders. Enforced by review and by the fake manager in
  tests.

## Requirements

- [ ] `ShellViewMode` + state + `CycleViewMode()` in `ShellViewModel.ViewModes.cs`.
- [ ] `ShellPaneItemViewModel.GroupKey` added (init-only).
- [ ] Store and engine group loaders with counts, ordering, and empty-state status.
- [ ] Scoped game loaders reuse `ToPaneItem` and the shared extras helper.
- [ ] `NavigateInto`/`NavigateUp` follow the table above, including the `..` row.
- [ ] `JumpToLibraryRoots()` sets Library mode and clears the scope; it still does not cycle.
- [ ] Filter/search interaction as specified (F9 clears then cycles).
- [ ] `LeftPaneTitle`, `InteractionHint`, Commands, Help updated.
- [ ] F9 wired in the key handler and the command dispatcher.
- [ ] F5 rescans all libraries from aggregate views.
- [ ] No persistence change; no `ILibraryManager` change; no new dependency.
- [ ] Library mode behaviour is unchanged (roots, drill-in, tags, launch).

## Verification

- [ ] New `ShellViewModelViewModeTests` (fake `ILibraryManager`, same pattern as
      `ShellViewModelSearchTests`): cycle order, scope reset, store group counts, engine groups skip
      `Unknown`, scoped list contents, Backspace chain, filter→F9 behaviour.
- [ ] Library-mode regression: existing navigation/search/filter tests unchanged and green.
- [ ] `dotnet build` 0 errors; `dotnet test` green.
- [ ] Manual: F9 cycles; Enter on a store shows only that store; Enter on a game launches; Backspace
      returns to groups, then to roots; F5 in Stores rescans all; F8 then F9 exits the filter and cycles.
- [ ] Manual: mode is Library again after restart.

## Deliberate non-goals

- Flat mode, scope chords, sorting options — T73.
- Category/folder trees, engine filters as tags, per-store icons, saved views.
- Grouping by library anchor in a fourth group list (the Library mode already shows anchors).
- Any persistence of the last mode.

## Stop rules

- **Do not modify `JumpToLibraryRoots()` into a cycle.** Four existing callers rely on it as a reset.
- **Do not add aggregation methods to `ILibraryManager`** without a non-UI consumer.
- **Do not duplicate the game-row projection.** Extract the extras block, do not copy it a third time.
- **Do not touch `data/` or scanning behaviour** beyond the F5 condition.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
