# Phase 1.1a: Setup & GUI Stabilization

## Goal

Fix all broken setup, scanning, and GUI interaction before any new features. The initial folder add → scan → browse flow must work end-to-end with both keyboard and mouse. All known Phase 1.1 bugs are addressed here.

---

## Why This Comes Before Everything Else

The current application can add folders and scan games, but:
- Navigation breaks after going back up a level
- Mouse interaction is disabled on all command buttons
- Games are treated as folders (drilling into them shows an empty state)
- The scanner picks wrong executables and shows non-game folders
- Backspace skips navigation levels
- Double-click does nothing

Nothing else (Steam integration, metadata, migration, categories) is usable until this core flow is solid.

---

## Tasks

### 1. Folder Scanner Fixes

- [x] **Exclude non-game folders** — `FolderScanner.Scan()` now skips directories with zero `.exe` files and no game marker files (`steam_appid.txt`, `.egsstore`, `.egstore`, `goggame.yml`, etc.) via `HasGameMarkerFile()`.
- [x] **Fix primary exe detection heuristic** — Replaced size-only sort with `IsNonGameExe()` filtering (~25 patterns: anti-cheat, installers, uninstallers, launchers). Non-game exes are excluded from candidate pool.
- [x] **Add folder-name-matching bonus** — `ExeNameMatchesFolderName()` with bidirectional substring match + token-level matching. Name-matching exes beat size-based sort.
- [x] **Add user-configurable ignore list** — `HiddenFolders` added to `AppConfig` record + `settings.json` serialization. `FolderScanner` accepts `IEnumerable<string>` in constructor and skips matching subdirectory names during scan.

### 2. Navigation Fixes

- [x] **Make game entries terminal (non-browsable)** — `Kind` changed from `Directory` to `File` in `LoadGamesForRoot()`. Games are now terminal — selecting updates details only.
- [x] **Fix Backspace to go one level up** — `NavigateUp()` correctly goes from game list to library roots (one level). Focus restored via `NavigationChanged` event.
- [x] **Add ".." parent-directory entry** — Rendered at top of game list with `ParentDirectory` kind. Enter/Double-click calls `NavigateUp()`.
- [x] **Fix arrow key focus after Backspace** — `NavigationChanged` event fires after all navigation ops. `MainWindow` subscribes and calls `LeftListBox.Focus()` + `ScrollIntoView()`.

### 3. Mouse Interaction Fixes

- [x] **Enable command button clicks** — Removed `IsHitTestVisible="False"` from command buttons. Added `PointerPressed` handler dispatching to correct actions (F2, F3, F5, F8, F9, F10).
- [x] **Add double-click drill-in** — `DoubleTapped` handler on `LeftListBox` calls `NavigateInto()` for browsable items (roots, "..").
- [x] **Ensure mouse selection updates details panel** — Already worked via `SelectedIndex` binding triggering `UpdateDetailsForSelection()`.

### 4. Keyboard Handler Fixes

- [x] **Wire F3 (no-op for now)** — `Key.F3` handler shows "Not yet implemented" in status bar.
- [x] **Wire F5 Launch (placeholder)** — `Key.F5` handler shows "Launch not yet implemented" in status bar.
- [x] **Wire F10 Quit** — `Key.F10` handler calls `Close()` on the main window.
- [x] **Add F8 placeholder** — `Key.F8` handler shows "Category view not yet implemented" in status bar.
- [x] **Add S key placeholder** — `Key.S` handler (no modifiers) shows "Search not yet implemented" in status bar. No collision with T key.

### 5. ListBox Focus & Selection Stability

- [x] **Call Focus() on navigation** — `NavigationChanged` event fires from both navigation methods; `MainWindow` subscribes and calls `LeftListBox.Focus()`.
- [x] **Persist SelectedIndex across navigation** — `_previousRootIndex` saved on drill-in, restored on `JumpToLibraryRoots()`, clamped against bounds.
- [x] **Scroll selected item into view** — Already implemented via `PropertyChanged` handler for `SelectedIndex`.

---

## Deliverables

- [x] Folder scanner excludes non-game folders and picks the correct executable
- [x] Game entries are terminal — selecting shows details, does not drill in
- [x] Backspace goes up one level, not two
- [x] ".." entry rendered at top of every non-root list
- [x] Arrow keys work after every navigation action (no focus loss)
- [x] Mouse double-click drills into folders
- [x] Command buttons are clickable and wired to actions
- [x] F3, F5, F8, F10, S keys have at minimum placeholder handlers
- [x] F10 Quit exits the application
- [x] Application is fully usable with both keyboard and mouse for the setup→scan→browse flow

---

## Exit Criteria

Phase 1.1a is complete when:
- Adding a library root, scanning it, and browsing its games works reliably
- The correct game executable is detected (anti-cheat installers excluded)
- Non-game folders are not shown in the game list
- Clicking a game shows its details in the right pane — no empty sub-navigation
- Double-clicking a folder drills in, double-clicking a game does nothing (terminal)
- Backspace goes up exactly one level at a time
- ".." entry is visible and clickable for navigation
- Arrow keys work immediately after every Backspace / Enter action
- F2, F9, T keys work via keyboard
- F10 quits the app
- All command buttons can be clicked with the mouse
- The app builds and runs cleanly on Linux and Windows
