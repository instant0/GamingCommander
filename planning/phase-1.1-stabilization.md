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

## Structure

Steps across 5 layers, each step changing at most 1-2 files with <50 lines. Every step builds and tests independently.

**Many steps are already implemented in code but the plan was out of date.** This revision reflects the actual source state.

---

## Layer 0 — Folder Scanner Fixes (COMPLETE)

- [x] **Exclude non-game folders** — `FolderScanner.Scan()` now skips directories with zero `.exe` files and no game marker files (`steam_appid.txt`, `.egsstore`, `.egstore`, `goggame.yml`, etc.) via `HasGameMarkerFile()`.
- [x] **Fix primary exe detection heuristic** — Replaced size-only sort with `IsNonGameExe()` filtering (~25 patterns: anti-cheat, installers, uninstallers, launchers). Non-game exes are excluded from candidate pool.
- [x] **Add folder-name-matching bonus** — `ExeNameMatchesFolderName()` with bidirectional substring match + token-level matching. Name-matching exes beat size-based sort.
- [x] **Add user-configurable ignore list** — `HiddenFolders` added to `AppConfig` record + `settings.json` serialization. `FolderScanner` accepts `IEnumerable<string>` in constructor and skips matching subdirectory names during scan.

---

## Layer 1 — Navigation Fixes (ALL DONE)

### 1. Make game entries terminal (non-browsable) ✅
- **Status:** Already implemented in `ShellViewModel.cs` line 219: `Kind = FileSystemEntryKind.File`
- `IsBrowsable` in `ShellPaneItemViewModel.cs` returns `false` for `File` kind.
- Selecting a game updates the details panel; does not drill in.

### 2. ".." parent entry ✅
- **Status:** Already implemented in `ShellViewModel.cs` lines 198-209
- A `ParentDirectory` entry is inserted at index 0 of `Items` in `LoadGamesForRoot()`.
- `NavigateInto()` checks for `ParentDirectory` kind and calls `NavigateUp()`.

### 3. Backspace one level up ✅
- **Status:** Already implemented in `ShellViewModel.cs` lines 166-169
- `NavigateUp()` checks `IsAtRootLevel`; if false (inside a game list), calls `JumpToLibraryRoots()`.
- System has two levels: Library Roots (top) ↔ Game List (inside root). One Backspace = one level up.

### 4. Focus after navigation ✅
- **Status:** Already implemented in `MainWindow.axaml.cs` lines 60-69
- `NavigationChanged` event fires `Dispatcher.UIThread.Post(() => LeftListBox?.Focus())`.

### 5. SelectedIndex persistence ✅
- **Status:** Already implemented in `ShellViewModel.cs` lines 108, 153
- `_previousRootIndex` saved in `NavigateInto()` (line 153), restored in `JumpToLibraryRoots()` (line 108).

### 6. Scroll into view ✅
- **Status:** Already implemented in `MainWindow.axaml.cs` lines 55-57, 66-67
- `ScrollIntoView` fires on `PropertyChanged(SelectedIndex)` and after navigation.

---

## Layer 2 — Mouse Interaction Fixes (ALL DONE)

### 7. Command button clicks ✅
- **Status:** Already implemented in `MainWindow.axaml.cs` lines 234-259
- `CommandButtonPressed` handler wired via `PointerPressed` event on each button Border.
- Handles F2, F3, F5, F8, F9, F10.
- No `IsHitTestVisible="False"` exists in the AXAML for command buttons.

### 8. Double-tap drill-in ✅
- **Status:** Already implemented in `MainWindow.axaml.cs` lines 228-232
- `LeftListBox_DoubleTapped` wired via `DoubleTapped` attribute on ListBox in AXAML (line 37).
- Calls `NavigateInto()` only if `IsBrowsable == true`.

### 9. Mouse selection updates details ✅
- **Status:** Already implemented:
- `SelectedIndex` binding in AXAML line 36 → `SelectedIndex` setter in `ShellViewModel.cs` line 62-67 → calls `UpdateDetailsForSelection()`.
- Details pane fields (Name, Path, Type, Executable, LastModified) all derive from `SelectedItem`.

---

## Layer 3 — Keyboard Handlers (ALL DONE)

### 10. F3 placeholder ✅
- **Status:** Already implemented in `MainWindow.axaml.cs` lines 143-146
- Shows "Not yet implemented" in status bar.

### 11. F5 placeholder ✅
- **Status:** Already implemented in `MainWindow.axaml.cs` lines 148-151
- Shows "Launch not yet implemented" in status bar.

### 12. F8 placeholder ✅
- **Status:** Already implemented in `MainWindow.axaml.cs` lines 153-156
- Shows "Category view not yet implemented" in status bar.

### 13. F10 Quit ✅
- **Status:** Already implemented in `MainWindow.axaml.cs` lines 158-161
- Calls `Close()` on the main window.

### 14. S Search placeholder ✅
- **Status:** Already implemented in `MainWindow.axaml.cs` lines 163-169
- Shows "Search not yet implemented" in status bar.
- Guards on `e.KeyModifiers == KeyModifiers.None` to avoid collision with Ctrl+S or Shift+S.

---

## Layer 4 — Research & Mock Data (REMAINING WORK)

**None of these steps are implemented yet.** This is the actual work that remains.

### 15. Create mock Windows game folders ✅
- **Files:** `data/mock/` directory tree, `tools/setup_mock_data.py`
- **Status:** Done. Covers Steam, Epic, standalone, anti-cheat, non-game, container scenarios.
- [x] Step 15 complete

### 16. Create mock registry .reg files ✅
- **Files:** `data/mock/registry/` (5 .reg files), `tools/generate_mock_registry.py`
- **Status:** Done. Covers Steam, Epic, GOG, EA, Ubisoft registry keys.
- [x] Step 16 complete

### 17. Validate Steam ACF parsing against mock data ✅
- **Files:** `tools/parse_steam_acf.py` (pre-existing)
- **Status:** Done. Both mock ACF files (12345, 67890) parse correctly.
- [x] Step 17 complete

### 18. Research Epic .item format with Python ⏭️ SKIPPED
- **Status:** Not needed. `tools/decode_manifest.py` (369 lines) already handles Epic `.manifest` binary parsing AND `.item` generation end-to-end. `docs/EGS_ITEM_FORMAT.md` already documents the format. Created `parse_epic_item.py` then deleted it as redundant.

### 19. Test registry parsing in Python ✅
- **Files:** `tools/parse_registry.py` (new)
- **Status:** Done. Parses all 5 mock .reg files correctly, extracts launcher-specific paths.
- [x] Step 19 complete

---

## Layer 5 — Stabilization Tests (REMAINING WORK)

### 20. Add model/enum tests ✅
- **Files:** `tests/GamingCommander.Core.Tests/FileSystemEntryKindTests.cs` (new)
- **Test:** `FileSystemEntryKind` enum values are distinct, correctly ordered (0=Directory, 1=File, 2=ParentDirectory).
- [x] Step 20 complete

### 21. Add scanner filter tests ✅
- **Files:** `tests/GamingCommander.App.Tests/ScannerFilterTests.cs` (new, in new `GamingCommander.App.Tests` project)
- Also created: `tests/GamingCommander.App.Tests/GamingCommander.App.Tests.csproj`, `tests/GamingCommander.App.Tests/GlobalUsings.cs`
- **Test (5 cases):**
  1. Non-game folder without exe or markers → excluded
  2. Folder with only non-game exes (installer/setup) → included but with non-game exe as primary
  3. Steam game with steam_appid.txt → detected as Steam
  4. Steam-emu game with steam_api64.dll → detected via root default type
  5. Anti-cheat exe filtered from primary selection; folder-name match wins
  6. Hidden folders config skips matching names
- [x] Step 21 complete

### 22. Add mock-data end-to-end tests ✅
- **Files:** `tests/GamingCommander.App.Tests/MockDataIntegrationTests.cs` (new)
- **Test (5 cases):**
  1. Standalone root scans correct games (6 included, 1 excluded)
  2. Steam common root scans Steam games correctly
  3. All scanned entries have valid non-empty Ids
  4. All scanned entries have non-default scan timestamps
  5. Scanning the same folder twice is deterministic (same folder names)
- [x] Step 22 complete

---

## Verification Build

**Before any code changes:** Do a verification build to confirm the current source compiles and passes existing tests. This establishes a baseline.

- [x] Build clean: `dotnet build` (0 errors, 0 warnings)
- [x] All existing tests pass: `dotnet test` (18/18 pass)
- [x] This establishes that Layers 1-3 are already working

---

## Deliverables Checklist

### Already Implemented (Layers 0-3)
- [x] Folder scanner excludes non-game folders and picks correct executable
- [x] Game entries are terminal — selecting shows details, does not drill in
- [x] ".." entry rendered at top of every game list
- [x] Backspace goes up one level
- [x] Arrow keys work after every navigation action (Focus() restored)
- [x] SelectedIndex preserved across navigation (previous root restored)
- [x] ScrollIntoView works after data reload
- [x] Command buttons clickable and wired to actions
- [x] Double-click drills into folders, does nothing on game entries
- [x] Mouse selection populates details panel
- [x] F3 shows placeholder message
- [x] F5 shows placeholder message
- [x] F8 shows placeholder message
- [x] F10 quits the app
- [x] S shows search placeholder (no collision with T)

### Still To Do
- [x] Mock Windows game folders in `data/mock/`
- [x] Mock registry .reg files
- [x] Steam ACF parsing validated against mock data
- [x] Registry parser script
- [x] Model/enum tests (FileSystemEntryKind)
- [x] Scanner filter tests (6 cases)
- [x] End-to-end flow tests (5 integration tests)

---

## Exit Criteria

Phase 1.1a is complete when:
- The app builds and runs cleanly on Linux and Windows
- Mock data structure exists in `data/mock/` for offline testing
- Python parsing scripts validate against mock data (ACF, Epic .item, .reg files)
- All schema docs in `docs/research/` are updated to reflect verified formats
- Navigation tests, scanner filter tests, and end-to-end flow tests pass
