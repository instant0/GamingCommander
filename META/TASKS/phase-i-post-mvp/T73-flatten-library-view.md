# Task T73: Flat View and Quick Scope — "All Games" as the Home View

**Tier:** 1 — Feature
**Phase:** I — Post-MVP Navigation
**Risk:** Medium
**Status:** ready (spec written 2026-10-30)
**Prerequisites:** Task T72 (adds the view-mode state machine this extends)

## Objective

Add the fourth mode, **Flat**: every game from every library in one list, with no drilling. Make it the
default view, and make the switch between "all games" and "this store / this directory group" a single
key press or chord.

The catalogue is already in memory (`GamesDatabase` is loaded once and cached), so the rebuild is a
projection, not a scan. This task also makes that projection cheap enough to feel instant.

## Product decisions (recorded, not open)

1. **Flat is the home view.** With libraries configured, the app opens on All Games. The filesystem
   roots remain available as Library mode, one F9 away.
2. **Grouped views are not the default.** Stores/Engines exist to answer "show me only those" quickly.
3. **No persistence of view mode.** Every run starts Flat. A saved mode would surprise more than it
   helps.
4. **No new data model.** Flat and grouped views are projections over `LibraryManager` + cached
   `GamesDatabase`; nothing is written and no index is added.
5. **Toggling must not touch the disk.** No `Directory.*`/`File.*` in these loaders.

## What T72 leaves in place

- `ShellViewMode { Library, Stores, Engines }` and `ViewMode`/`ScopedGroup`/`CycleViewMode()` in
  `ShellViewModel.ViewModes.cs`.
- `NavigateUp()`: group list → `JumpToLibraryRoots()` (this task changes that target to Flat).
- Startup, filter clear, and search end call `JumpToLibraryRoots()` (this task re-points them to Flat).
- `ShellPaneItemViewModel.GroupKey`.
- F9 wired in `MainWindow.Navigation.cs`, `MainWindow.Commands.cs`, the Commands bar, and Help.

## Design

### 1. The mode

- Add `Flat` to the enum. Cycle order becomes **Flat → Stores → Engines → Library → Flat** (Flat next to
  Stores, one key between the two presentations the user alternates).
- `ViewMode` default becomes `Flat`.
- `IsAggregateView` stays `ViewMode != Library`.

### 2. Home

Add `GoHome()` in `ShellViewModel.ViewModes.cs`:

```csharp
public void GoHome()   // clears filter/search, loads Flat
```

- sets `ViewMode = Flat`, `ScopedGroup = null`;
- clears `ActiveFilter`, ends search, resets `_currentLibraryName`;
- calls `LoadFlatGames()`;
- `JumpToLibraryRoots()` keeps its current meaning (Library mode, roots) and is no longer the reset for
  transient state.

Re-point the three reset callers:

| Caller | Today | After |
|---|---|---|
| `ShellViewModel` ctor | `JumpToLibraryRoots()` | `GoHome()` |
| `ClearFilter()` (`Filtering.cs:32`) | `JumpToLibraryRoots()` | `GoHome()` |
| `EndSearch()` (`Search.cs:68`) | `JumpToLibraryRoots()` | `GoHome()` |

`NavigateUp()` at the top of a mode: scoped → groups → Flat → **no-op** (change the T72 table entry).
Library roots remain reachable only through F9.

`Reload()` must rebuild the current mode: filter → filtered list; Flat → flat list; scoped → scoped list;
group list → group list; Library → existing. This is what makes F5 and tag edits refresh correctly.

### 3. Flat list

`LoadFlatGames()`:

- enumerate all games once: `foreach (Library lib in _libraryManager.Libraries) foreach (var g in
  GetGamesForLibrary(lib.Name))`;
- project with the existing `ToPaneItem(g, g.Library, leftPathOverride: lib.Name)` so the left column
  shows the anchor name (where the game came from) instead of the exe name;
- sort by `DisplayName` (`StringComparer.OrdinalIgnoreCase`), tiebreak `Id`;
- no `..` row;
- `LeftPaneTitle` → `All games (N)`; empty catalogue → `All games (0)` and `StatusText` =
  "No games yet. Add a library (F2) and rescan (F5).";
- do not call the library-only extras (`FormatMovedDetail`, status colours, multi-exe hints). Those stay
  in the scoped and library lists until the row model carries them without string formatting.

### 4. Instant rebuild

`Items` is currently `get`-only and every loader does `Items.Clear()` + `Add` per row. For a 5k-game
flat list that is 5k `CollectionChanged` events and a UI update per row.

Change `Items` to `{ get; private set; }` (the XAML already binds `ItemsSource="{Binding Items}"`) and add:

```csharp
private void ReplaceItems(IReadOnlyList<ShellPaneItemViewModel> rows)
{
    Items = new ObservableCollection<ShellPaneItemViewModel>(rows);
    OnPropertyChanged(nameof(Items));
    OnPropertyChanged(nameof(ItemCount));
    _selectedIndex = rows.Count > 0 ? 0 : -1;
    OnPropertyChanged(nameof(SelectedIndex));
    UpdateDetailsForSelection();
    NavigationChanged?.Invoke();
}
```

- Use `ReplaceItems` for Flat and the scoped loaders (one collection reset).
- Existing Library/group loaders may keep `Clear()`+`Add` (small lists), but they must not break the
  settable property.
- `ShellViewModelSearchTests` reads `vm.Items`; a settable property keeps that working.
- Build the row list off the UI thread? No. The projection is memory-only and small; measure first. Do not
  add async or virtualization in this task.

Target (manual, not a test): under 16 ms for 5,000 games on the reference machine. If it is not, the
first suspect is per-row `BuildTagBadges`/sidecar lookups, and the fix is to keep tags out of the flat
row, not to add caching.

### 5. Quick scope

Two chords on a selected game row. Both are one key and both land in an existing mode; no new state.

| Chord | Action | Result |
|---|---|---|
| `Ctrl+Enter` | `ScopeToSelectedStore()` | `ViewMode = Stores`, `ScopedGroup = game.GameSource`, scoped list |
| `Ctrl+Shift+Enter` | `ScopeToSelectedLibrary()` | `ViewMode = Library`, drill into `game.Library` (that directory group's games) |

- Implement both in the ViewModel so they are unit-testable; the key handler only routes.
- `MainWindow.Navigation.cs`: inside `case Key.Enter`, check modifiers **before** the plain
  `NavigateInto()` path, so Ctrl+Enter never launches a game.
- Return path: scoped store → Backspace (group list) → Backspace (Flat). Status line shows the scope and
  count: `Store: Steam (12 games)`.
- Do not add a chord for engines; engines are not a common "show me these" request, and F9 reaches them.

### 6. Help, hint, command bar

- Help: `("F9", "Cycle view: All games / Stores / Engines / Library")` and
  `("Ctrl+Enter", "Show only this game's store")`.
- `InteractionHint`: `... | F9: View | Ctrl+Enter: this store`.
- Commands bar stays `F9 / View` (labels are init-only).

### 7. No writes

- No new persisted field; `settings.json` unchanged.
- No `data/` writes.
- No `Directory.CreateDirectory`; T130 owns that rule.

## Requirements

- [ ] `ShellViewMode.Flat` added; cycle order Flat → Stores → Engines → Library; default Flat.
- [ ] `GoHome()` added; startup, filter clear, and search end use it.
- [ ] `JumpToLibraryRoots()` unchanged in meaning (Library mode) and no longer used as a reset.
- [ ] `NavigateUp()` chain: scoped → groups → Flat → no-op.
- [ ] `LoadFlatGames()` with stable ordering, anchor in the left column, no `..` row, empty state.
- [ ] `Items` settable + `ReplaceItems()`; Flat and scoped loaders use it.
- [ ] `Reload()` rebuilds the current mode (F5/tag edits).
- [ ] `ScopeToSelectedStore()` / `ScopeToSelectedLibrary()` + Ctrl+Enter / Ctrl+Shift+Enter routing.
- [ ] Help and hint updated.
- [ ] No disk access in the aggregate loaders; no new dependency; no persistence change.

## Verification

- [ ] Unit tests (fake `ILibraryManager`, extend `ShellViewModelViewModeTests`):
      flat count across two libraries; ordering; no `..` row; `GoHome` from a filter and from search;
      Backspace chain scoped → groups → flat → no-op; `Ctrl+Enter` scope target; `Ctrl+Shift+Enter`
      library drill; `Reload` stays in mode.
- [ ] Existing Library/search/filter tests green (the `Items` setter must not break them).
- [ ] `dotnet build` 0 errors; `dotnet test` green.
- [ ] Manual: start → All games; F9 → Stores → Enter → Steam only; Backspace ×2 → All games;
      Ctrl+Enter on a GOG game → GOG only; Ctrl+Shift+Enter → that anchor's games; F5 in All games
      rescans all; restart → All games.
- [ ] Manual timing: 5,000-game flat list renders under 16 ms; toggling does not spin the disk.

## Deliberate non-goals

- Virtualized list, async projection, background sorting.
- Sorting UI (alphabetical is fixed for now), grouping headers inside the flat list.
- Saved views, per-store favourites, engine filters as tags.
- Persisting the last mode.

## Stop rules

- **Do not make the flat list the only view.** Library mode and grouped modes stay; the filesystem
  browser is not removed.
- **Do not add a second catalogue.** Flat reads the same cached `GamesDatabase`; no snapshot, no copy in
  `data/`, no index file.
- **Do not put per-row filesystem checks in the projection.** A missing-folder flag is a scan/health
  result, not a render-time question.
- **One collection reset per rebuild.** Do not reintroduce per-row `Add` in the flat path.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
