# Phase: KODI-Style Category Browsing & Quick Search

## Goal

Add a category-driven browsing mode alongside the existing library-root navigation, plus a universal quick-search. The default view remains **Library Roots** (configured paths → games inside them). Users can quickly toggle to **Browse by Category** (KODI-style drill-down) via F8, or press **S** to open a text search that matches across game names, genres, developers, publishers, and paths.

## UX Model

### Navigation Stack (breadcrumb-style)

The left pane becomes a 3-level drill tree depending on view mode:

```
Level 0 (View Mode Selector):
  ├── Library Roots          → existing per-root game list
  └── Browse by Category     → category folders

Level 1b (inside Categories):
  ├── Genre                  → values: Action, Platformer, RPG, …
  ├── Publisher              → values: CD Projekt, EA, …
  ├── Launcher               → values: Steam, Epic, Standalone, …
  ├── Year of Release        → values: 2015, 2020, 2024, …
  └── Gamerankings Rating    → values: 90%+, 80-89%, 70-79%, …

Level 2b (inside a category value):
  └── Games matching that value (e.g. all Steam games / all RPGs)
```

After reaching Level 2b, selecting a game shows its details in the right pane — identical to the library-root drill-in UX.

### Default View

On launch (and after pressing F9), the app shows **Library Roots** — the configured library paths as "drives" (e.g. `D:\SteamLibrary (Steam)`). Drilling into a root shows all scanned games inside it. This is the default, always-available navigation mode.

### Quick Mode Toggle (F8)

- **F8** toggles between `Library Roots` and `Browse by Category` at the top level.
- This is a **flat toggle**, not a nested menu — one press switches, second press switches back.
- When toggling back to Library Roots, the previously selected root is restored.
- When toggling to Categories, the category list (Level 1b) is shown directly.
- Status bar shows the current view mode and breadcrumb path, e.g.
  `Browse: Categories > Launcher > Steam (12 games)`
  `Browse: Library Roots > D:\SteamLibrary (45 games)`

### What Happens to Existing F-Keys

- F2 (Library Root Setup), F9 (Jump to Root), T (Retag), arrow keys, Enter, Backspace — all unchanged.
- F8 becomes the **View Mode Toggle** (Library Roots ↔ Categories). This is a new binding.
- **S** becomes the **Quick Search** hotkey (not Shift+S — plain S key).
- When in Category view, F2 could optionally open a "Category Setup" to define custom categories or toggle which categories appear.

---

## Quick Search (S Key)

### UX Flow

1. User presses **S** from any view mode (Library Roots, Categories, or inside a root/game list).
2. A **search input overlay** appears at the bottom of the screen (styled like the status bar):
   - Prompt text: `Search:`
   - Text input cursor with placeholder: `"Enter game name, genre, developer, or path — wildcards supported"`
   - As the user types, results update in **real time** in the left pane (replacing the current item list).
3. **Global scope — no context restriction.** Search always queries the entire virtual file system (every game across every library root). It does NOT scope to the current root, current category, or current view. Pressing S from inside `D:\SteamLibrary` or from the `RPG` category folder or from the root-level view all produce the same flat result set across all known games. The search is effectively "find any game matching this query anywhere."
4. Each result is a standard game entry (same template as normal game list):
   - Game title, source label, path summary
   - A **match reason** badge shown in the path line, e.g. `"matched: name"`, `"matched: genre (RPG)"`, `"matched: developer (CD Projekt Red)"`
5. User can arrow through results and press Enter to select a game (shows details in right pane).
6. User can press Enter on a result to "lock in" to that game (normal detail view).
7. Press **Escape** to dismiss search and return to the previous view/browse state.
8. Press **Enter with empty input** to dismiss search (no-op).

### Match Scope

The search query is matched **case-insensitively, partially** against ALL of the following fields per game:

| Field | Example Match | Data Source |
|---|---|---|
| `DisplayName` | "Cyberpunk" matches "Cyberpunk 2077" | Always present |
| `FolderName` | "dying" matches "DyingLight2StayHuman" | Always present |
| `GameSource` (as string) | "Steam" matches all Steam games | Always present |
| `Extra["Genre"]` | "RPG" matches all RPG-tagged games | Requires Phase 2.2 metadata |
| `Extra["Developer"]` | "CD Projekt" matches all CD Projekt games | Requires Phase 2.2 metadata |
| `Extra["Publisher"]` | "Electronic Arts" matches all EA games | Requires Phase 2.2 metadata |
| `ExecutablePath` | "d2r" matches any game with "d2r" in its exe path | Always present |
| `ManifestPath` | "1432104" matches game by manifest ID | Available after scan |

**Union matching**: If a query matches ANY of these fields, the game appears in results. For example, searching "RPG" returns:
- Games named "RPG" or containing "RPG" in their name (e.g. "RPG Maker")
- Games tagged with Genre "RPG"
- Games whose developer/publisher contains "RPG"

### Wildcard Support

- `*` matches any sequence of characters (e.g. `*2077` matches all games ending with "2077")
- `?` matches any single character
- Plain text (no wildcards) does substring matching: `cyber` matches "Cyberpunk 2077"
- Multiple space-separated terms match as AND: `rpg cd projekt` finds games that are both RPG genre AND CD Projekt developer (requires metadata)

### Search Results View

The left pane's item list is replaced with search results. Each entry uses the existing `ShellPaneItemViewModel` with extended fields:

```
Title:    "Cyberpunk 2077"
Source:   "Steam"
Path:     "D:\SteamLibrary\steamapps\common\Cyberpunk 2077"
Match:    "matched: name, genre (RPG, Open World), developer (CD Projekt Red)"
```

The match reason is shown in the `PathSummary` field (replacing the normal path display during search mode).

If no games match, the list shows a single entry:
```
Title:    "(no results)"
Source:   ""
Path:     "No games match 'search term' — try a different query"
Kind:     File (not browsable)
```

### Back Navigation from Search

- **Escape**: Dismiss search, restore the previous item list (roots, categories, or game list).
- The previous `Items` collection and `SelectedIndex` are cached before search mode activates.
- **Enter** on a result: selects that game, details panel updates. If the user presses Escape after selecting, they return to the pre-search view.

### Keyboard Flow During Search

| Key | Action |
|---|---|
| S (any view) | Open search overlay |
| Escape | Close search, restore previous view |
| Enter (with text) | Select highlighted result |
| Enter (empty) | Close search (no-op) |
| Up/Down | Navigate search results |
| Backspace (in input) | Delete last character |
| Any printable char | Type into search input |

### Search Input Widget

The search input is rendered as a new row in the main window grid (between the item panes and the status bar):

```
┌─────────────────────────────────┐
│  Search: Cyberpunk_             │
│  (enter game name, genre,      │
│   developer, or path —          │
│   wildcards: *, ? supported)    │
└─────────────────────────────────┘
```

Styled consistently with the existing status bar border. The input box takes keyboard focus when search is active; arrow keys navigate results.

---

### Source of Category Values

| Category | Data Source | Availability |
|---|---|---|
| **Launcher** | `GameEntry.GameSource` enum | Always available for every game |
| **Genre** | `GameEntry.Extra["Genre"]` (from Phase 2.2 metadata lookup) | Empty until user runs F4 metadata lookup |
| **Publisher** | `GameEntry.Extra["Publisher"]` | Empty until metadata lookup |
| **Year of Release** | `GameEntry.Extra["ReleaseYear"]` (extracted from release date in Phase 2.2) | Empty until metadata lookup |
| **Gamerankings Rating** | `GameEntry.Extra["Rating"]` or computed from aggregated scores | Empty until metadata lookup |

Categories with zero data should either be hidden or shown greyed-out with a note "No metadata — press F4 to look up."

### Navigation State (in ViewModel)

A new `CategoryBrowseState` struct or record to track the current drill position:

```
currentViewMode: LibraryRoots | Categories
currentCategory: string?            // e.g. "Genre", "Launcher"
currentCategoryValue: string?       // e.g. "Action", "Steam"
```

The `ShellViewModel` already has `_currentRootPath` and `_isAtRootLevel`. This adds a parallel state machine for category browsing.

### Category Folders (Virtual Entries)

Each category at Level 1b is rendered as a `ShellPaneItemViewModel` with:
- `Title` = category display name (e.g. "Genre", "Launcher")
- `SourceLabel` = "Category"
- `PathSummary` = description or game count
- `Kind` = `Directory` (so `IsBrowsable = true`)
- `GameCount` = total games with at least one value for this category

Each category value at Level 2b is rendered with:
- `Title` = value name (e.g. "Action", "90%+")
- `SourceLabel` = parent category name
- `PathSummary` = "X game(s)"
- `Kind` = `Directory`
- `GameCount` = number of games matching

Drilling into a value shows the actual game entries (same rendering as current per-root game list).

---

## Multi-Game Entries (Cross-Root Aggregation)

Category browsing **aggregates across all library roots**. A genre, publisher, or launcher category shows games from every configured root, not just one. This is the key difference from the per-root game view.

**Implementation:** `ILibraryManager` or a new helper class collects all `GameEntry` objects from all roots (via `GetGamesForRoot` for each root) and applies the category filter in memory.

**Potential issue:** If two roots have games with the same ID (same folder name on different drives), display both but note the duplicate. In practice, each `GameEntry.Id` is derived from `rootPath + folderName` via `ComputeId`, so IDs are unique per root — no collision.

---

## Category Value Normalization

Category values need consistent casing and grouping:

- **Genre** — stored as comma-separated tags (e.g. "RPG, Open World, Story Rich"). Split into individual values for the folder list. Trim whitespace.
- **Publisher** — stored as single string. Group exact matches together. Consider a normalization pass: "Electronic Arts" / "EA" / "EA Games" → merge rules.
- **Launcher** — derived from `GameSourceKind` enum, always clean.
- **Year** — extract 4-digit year from a date string. Group as decade fold-out or flat year list. Example: `[2024 (3), 2023 (5), 2022 (2)]`.
- **Rating** — bucket into ranges: `90-100%`, `80-89%`, `70-79%`, `<70%`. Display as stars or progress bar in the list.

---

## Necessary Code Changes (No Code Yet — Design Only)

### New/Modified Interface Members

`ILibraryManager` additions:
```
IReadOnlyList<GameEntry> GetAllGames();                    // aggregate across all roots
IReadOnlyList<string> GetCategoryValues(string category);  // distinct values for a category
IReadOnlyList<GameEntry> GetGamesByCategory(string category, string value);
IReadOnlyList<string> AvailableCategories { get; }         // ["Genre", "Publisher", "Launcher", "Year", "Rating"]
```

Or alternatively, a new `ICategoryBrowseService` to keep `ILibraryManager` focused on library management.

### ShellViewModel Changes

- Add `ViewMode` enum: `LibraryRoots | Categories`
- Add `CurrentCategory` and `CurrentCategoryValue` properties
- Modify `JumpToLibraryRoots()` → becomes `JumpToViewSelector()` or keep F9 bound to the view selector
- Modify `NavigateInto()` / `NavigateUp()` to handle category drill levels
- Add category-specific detail display (show category breakdown for a selected value)
- Add `F8` handler for view mode toggle

### New Category Icons/Display

Consider adding visual distinction for category entries vs library root entries vs game entries. This could be:
- Different `SourceLabel` styling (already per-item)
- An icon column (future)
- A category breadcrumb in the header bar

---

## Potential Issues & Edge Cases

1. **Empty categories** — If no games have metadata, Genre/Publisher/Year/Rating folders are empty or hidden. Launcher is always populated because `GameSourceKind` is always set. Solution: hide empty categories or show "No data — run metadata lookup" inline.

2. **Performance** — Aggregating across all roots and filtering in memory is fine for hundreds of games. For thousands, consider pre-built indexes or SQLite. Phase 1 doesn't need this.

3. **Category naming conflicts** — A game folder named "Action" could collide with a category value named "Action". Avoid by having a dedicated view mode that toggles the entire pane interpretation, not mixed entries.

4. **Empty state** — When no library roots are configured, both view modes show "No library roots configured." The category view is not available without at least one root with games.

5. **Metadata dependency** — Most categories are empty until Phase 2.2 metadata lookup runs. The feature should still work with just the Launcher category populated and the rest showing "No data" placeholders.

---

## Documentation To Create/Update

- Add a category-browse section to `docs/FEATURES.md` with the feature table
- Add navigation flow diagram or table to `docs/ui-direction.md`
- Add F8 binding to the F-key reference tables in `03-phase-1-ui-polish.md` and `00-overview.md`
- Update the ShellViewModel architecture notes for the new state machine

---

## Tasks

### 1. Data Layer
- [ ] Add `GetAllGames()`, `GetCategoryValues()`, `GetGamesByCategory()` to `ILibraryManager` or new `ICategoryBrowseService`
- [ ] Implement category value extraction from `GameEntry.GameSource` + `GameEntry.Extra`
- [ ] Implement value normalization and de-duplication

### 2. ViewModel State Machine
- [ ] Add `ViewMode` enum, `CurrentCategory`, `CurrentCategoryValue` to `ShellViewModel`
- [ ] Implement `NavigateInto()` for category levels
- [ ] Implement `NavigateUp()` for category levels
- [ ] Add breadcrumb/path tracking for status bar

### 3. View
- [ ] Render category folders at Level 1b
- [ ] Render category values at Level 2b
- [ ] Render filtered game list at Level 3b (reuse existing game entry template)
- [ ] Add view mode indicator in header or status bar

### 4. F8 Binding (View Mode Toggle)
- [ ] Add F8 keyboard handler in `MainWindow.axaml.cs`
- [ ] Add F8 = "View" label button to command bar (between F7 and F9)
- [ ] Toggle between Library Roots and Categories modes
- [ ] Default to library roots on first launch
- [ ] Preserve navigation state when toggling back (remember last root/category selection)

### 5. Quick Search (S Key)
- [ ] Add S key handler in `MainWindow.axaml.cs` (plain S, not Shift+S — T is already the retag hotkey, ensure no collision)
- [ ] Implement search input overlay widget (styled border row below main panes)
- [ ] Implement `SearchGames(string query)` in data layer: cross-root, cross-field union matching
- [ ] Support wildcard patterns (`*`, `?`) and space-separated AND terms
- [ ] Cache and restore previous item list when search opens/closes
- [ ] Show match reason badge in search result entries (`matched: name, genre, developer`)
- [ ] Handle empty/no-match state gracefully
- [ ] Escape dismisses, Enter on result locks selection
- [ ] Real-time result updates as user types (debounced)

### 6. Edge Case Handling
- [ ] Empty categories (no metadata) — show greyed-out or hidden
- [ ] Cross-root game aggregation — no ID collision
- [ ] Normalization of publisher names (e.g. EA = Electronic Arts)
- [ ] Rating bucketing logic

---

## Deliverables

- [ ] Category browsing mode with drill-down navigation
- [ ] Launcher category always populated (from `GameSourceKind`)
- [ ] Genre, Publisher, Year, Rating categories functional when metadata exists (Phase 2.2)
- [ ] F8 toggles between Library Roots and Categories view (quick flat toggle)
- [ ] S opens quick search overlay from any view mode
- [ ] Search matches across name, genre, developer, publisher, launcher, path (union)
- [ ] Wildcard (`*`, `?`) and multi-term AND support in search
- [ ] Real-time result updates as user types
- [ ] Match reason shown in search result entries
- [ ] Escape returns to previous navigation state
- [ ] Status bar shows current breadcrumb
- [ ] Cross-root game aggregation (categories + search)
- [ ] Empty-state handling for categories without data

---

## Exit Criteria

Phase is complete when:
- F8 switches between library-root browsing and category browsing (quick flat toggle)
- F8 recall: toggling back to Library Roots restores the previously selected root
- Category view shows genre/publisher/launcher/year/rating folders
- Drilling into a category shows distinct values for that category
- Drilling into a value shows matching games from all roots
- Launcher category works without any metadata lookup
- S opens search input overlay from any view mode
- Search matches game name, genre, developer, publisher, launcher, and path in a single query
- Wildcards (`*`, `?`) and multi-term AND searches work correctly
- Search results show match reason badge per entry
- Escape dismisses search and restores the previous browse state
- Backspace returns up the category tree
- Status bar shows the current navigation breadcrumb
