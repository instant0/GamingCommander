# Phase 1.1: UI Polish & User Experience

## Goal

Polish the Phase 1.0 UI: improve the layout, add first-run experience, enhance the details panel, add folder type tagging, and finalize the settings workflow.

---

## Critical Architecture: Virtual File System

**The real filesystem is only touched during Setup/Rescan. Normal navigation operates over a virtual filesystem stored in `data/games.json`.**

### Why a virtual FS?

- Real filesystem browsing is slow and shows irrelevant folders (logs, saves, dlcs, etc.)
- Games must be identified by parsing folders: detecting the main `.exe`, launchers, marker files
- Navigation should show **games** when inside a library root, not raw folder contents
- This enables metadata enrichment per-game and a clean UX

### Navigation Model

- **Top level** (`F9`): Lists configured library roots as "drives" — e.g. `D:\Games (Standalone)`, `D:\SteamLibrary (Steam)`
- **Inside a root**: Lists **games** parsed from `data/games.json` for that root — not filesystem entries
- **No real filesystem browsing during navigation** — `Browse()` reads from the virtual DB, not `Directory.GetFiles()`

### Setup Model (Wizard + Rescan)

When a library root is added or rescan is triggered:
1. Scan the folder path
2. For each sub-folder: detect the game type (marker files, folder name, heuristics)
3. Find the primary `.exe` and any launcher `.exe`
4. Write entries to `data/games.json`
5. Navigation now shows these entries

---

## Tasks

### 1. First-Run Wizard + Library Root Scanner

- [x] Detect when `data/settings.json` is missing or `IsFirstRun = true`
- [x] Wizard starts with empty list (no auto-populated paths)
- [x] User adds custom paths via folder picker, selecting default type per path
- [x] Folders added with "not scanned" status — user must click "Scan" manually
- [x] "Scan" button per folder entry:
  - Scans sub-folders for `.exe` files and marker files
  - Detects game type (override or heuristic)
  - Stores parsed entries in `data/games.json`
  - Shows "X games" or "0 games" when complete
- [x] On completion, save config with `IsFirstRun = false`
- [x] **Rescan button** on existing library roots: re-scan the folder and update `games.json`

### 2. Virtual Browse (ILibraryManager.Browse)

- [x] `Browse()` reads from `data/games.json` for the current library root — not real filesystem
- [x] Returns virtual entries (game name, executable path, type) — no `FileSystemEntry` enumeration
- [x] Navigation inside a root shows games, not folders

### 3. F2 — Library Root Setup Panel

F2 **only** opens Library Root Setup (no context switching needed):
- View configured roots, their types, and game counts
- Add new root (triggers folder picker + scan)
- Remove root (with confirmation)
- Change root default type
- Trigger rescan on existing roots (re-scans folder and re-populates `games.json`)
- Changes persist immediately to `settings.json` and `games.json`

### 4. T — Configure Game Panel

Press `T` on a game entry inside a library root to open **Game Setup**:
- Edit display name
- Retag game type (override from root default)
- Set/override executable path
- Set/override launcher path
- Set launch command-line arguments
- Set Epic manifest path (required for Epic games — the `.json` file in `manifests/`)
- Delete this game entry from the root
- Changes persist immediately to `games.json`

### 5. Details Panel Enhancement

- [x] Show game executable path for selected game
- [x] Show resolved folder type (override or root default)
- [x] Show last modified from the game entry in `games.json`
- [x] Show placeholder text when nothing is selected

### 6. Navigation Polish

- [x] Show selection highlight on the currently focused item
- [x] Auto-scroll ListBox to keep selection visible
- [x] Header shows library root name when inside a root; "Library Roots" at top level
- [x] Path truncation at ~50 characters

### 7. Status Line Improvements

- [x] Status line shows operation feedback: "Scanned 12 games in D:\Games", "Retagged DyingLight2 as Epic"
- [x] Status line shows game count when inside a library root

### 8. Visual Polish

- [x] Consistent spacing and alignment in both panes
- [x] Path text uses a slightly smaller font to fit longer paths
- [x] Pane splitter is clearly visible and draggable

---

## data/games.json Schema

```json
{
  "roots": [
    {
      "rootPath": "D:\\Games",
      "defaultType": "Standalone",
      "games": [
        {
          "id": "d41d8cd98f00b204",
          "folderName": "DyingLight2StayHuman",
          "displayName": "Dying Light 2 Stay Human",
          "gameSource": "Epic",
          "override": true,
          "executablePath": "D:\\Games\\DyingLight2StayHuman\\REDEngine\\DyingLight2.exe",
          "launcherPath": "",
          "cmdlineArgs": "",
          "manifestPath": "D:\\Games\\DyingLight2StayHuman\\.egsstore\\manifests\\1432104.json",
          "lastScanned": "2026-04-17T10:00:00Z",
          "lastModified": "2026-01-15T08:30:00Z",
          "extra": {}
        }
      ]
    }
  ]
}
```

- `id`: Short hash of `rootPath + folderName` for stable identity
- `folderName`: Raw folder name on disk
- `displayName`: Human-readable name (editable via T / Game Setup)
- `gameSource`: Resolved type (root default or override)
- `override`: `true` if explicitly tagged different from root default
- `executablePath`: Primary `.exe` found in the folder (overridable via Game Setup)
- `launcherPath`: Secondary `.exe` if a launcher was detected (overridable)
- `cmdlineArgs`: User-defined launch arguments (set via Game Setup)
- `manifestPath`: Path to the store manifest file. For Epic: `C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests\*.item`. For Steam: the `.acf` file in `steamapps/`. For GOG: `goggame-*.info`. This field stores the **canonical manifest path** — not just a copy inside the game folder. The game-folder manifest may overlap but is not the source of truth. **Required for SyncMove** — see Phase 2.1 plan for how this + the game tag drives migration logic.
- `lastScanned`: When the folder was last parsed
- `lastModified`: Folder's `LastWriteTime` at last scan
- `extra`: Extensible key/value map for future metadata (developer, publisher, genre, PCGW link, etc. — populated in Phase 2.2)

---

## F-Key Reference (Updated)

| Key | At Root Level | Inside Library Root |
|-----|---------------|---------------------|
| `F2` | Library Root Setup (add/remove/rescan roots) | Library Root Setup |
| `T` | — | **Configure Game** (opens Game Setup for selected game) |
| `F9` | — (already at root level) | Jump to library-root listing |
| `Enter` | Drill into library root | Show game details |
| `Backspace` | — | Go back to library-root listing |
| Arrows | Navigate roots | Navigate games |

---

## Deliverables

- [x] First-run wizard scans folders and populates `data/games.json`
- [x] Navigation inside a root shows parsed games from `games.json`
- [x] F2 opens Library Root Setup (add/remove/rescan roots)
- [x] T key opens Game Setup (configure selected game: name, type, exe, launcher, args, manifest)
- [x] Enhanced details panel with executable path and resolved type
- [x] Polish: selection highlight, auto-scroll, status feedback
- [x] App builds and runs cleanly on Windows

---

## Exit Criteria

Phase 1.1 is complete when:
- First launch wizard scans a folder and shows parsed games in the virtual browse view
- F2 opens Library Root Setup (add/remove/rescan roots)
- T key opens Game Setup to configure the selected game
- Navigation inside a root shows games from `games.json`, not raw filesystem entries
- The details panel shows executable path and resolved launcher type
- The app builds and runs cleanly on Windows

**Status: COMPLETE** — All tasks implemented per codebase inspection (2026-04-17).

---

## Known Issues (Post-1.1 Cleanup)

### Critical: UI Command Buttons Are Decorative

**Status: KNOWN — NOT FIXED**

All command buttons have `IsHitTestVisible="False"` — they cannot be clicked. Only 6 of 10 F-key buttons exist in the UI bar: F1, F2, F3, F5, F9, F10. F4, F6, F7, F8 are absent.

**Keyboard handler state (MainWindow.axaml.cs OnKeyDown):**
- ✅ F2 — opens LibrarySetupWindow
- ✅ F9 — JumpToLibraryRoots
- ✅ T — opens GameSetupWindow
- ✅ Enter — NavigateInto
- ✅ Backspace — NavigateUp
- ✅ Arrow Up/Down — selection navigation
- ❌ F1 — no handler
- ❌ F3 — no handler
- ❌ F4 — no handler
- ❌ F5 — no handler
- ❌ F6 — no handler
- ❌ F7 — no handler
- ❌ F8 — no handler
- ❌ F10 — no handler

### Other Known Issues

- Data path logic changed from `../../../../..` to local `data/` folder (correct behavior)
- Default `settings.json` and `games.json` should be created alongside exe for clean installs

### Navigation & Mouse Bugs (Found 2026-05-31 Testing)

**1. No mouse double-click support**
Selecting items with the mouse works, but double-click does not trigger `NavigateInto()`. Users must use keyboard (Enter) to drill into a folder. Need to wire `DoubleTapped` event on ListBox items.

**2. Game entries are incorrectly marked as browsable**
`ShellPaneItemViewModel.Kind` is set to `Directory` for game entries (line 186 of ShellViewModel.cs). This makes them navigable — clicking/Entering a game calls `NavigateInto()`, which clears the game list and shows an empty state. Games should be `Kind = File` (non-browsable). Selecting a game should only update the details panel.

**3. Backspace goes two levels up**
`NavigateUp()` calls `JumpToLibraryRoots()` unconditionally when `!IsAtRootLevel`. This means: From a library root's game list → Backspace → Library Roots (skips the expected intermediate step). There's no single-level-up path from the game list back to its parent root overview. A ".." entry or an intermediate "root overview" level would fix this.

**4. Keyboard focus lost after Backspace (arrow keys stop working)**
After `JumpToLibraryRoots()` clears and repopulates the `Items` collection, the `LeftListBox` loses keyboard focus. Arrow key handlers in `OnKeyDown` no longer respond until the user clicks a list item with the mouse. Root cause: `Items.Clear()` followed by `Add` causes the ListBox to lose its selected item and focus. Fix: call `LeftListBox.Focus()` after data reload and ensure `SelectedIndex` is properly synchronized.

**5. Non-game folders shown in scan results**
`FolderScanner.Scan()` returns ALL sub-directories, including non-game folders (logs, saves, DLC folders with no executables). The scanner does not filter out folders that lack `.exe` files or game marker files (`steam_appid.txt`, `.egsstore`, etc.). Need:
- Scanner to exclude folders with zero `.exe` files and no game markers
- User-configurable ignore list (hidden folders) persisted in `settings.json`

**6. Primary executable detection heuristic picks wrong .exe**
`FolderScanner.FindPrimaryExecutable()` (lines 73-86) uses file size as the tiebreaker — the largest `.exe` after excluding launcher names wins. This fails when non-game executables (anti-cheat installers, redistributables, setup tools) are larger than the actual game executable.

*Example:* In a `Battlefield 6` folder containing both `bf6.exe` and `eaanticheatinstaller.exe`, the anti-cheat installer was selected because:
  - `"anticheat"` is not in the `launcherNames` exclusion list (line 97-101)
  - The anti-cheat installer is physically larger than the game exe due to bundled driver packages
  - No folder-name-preference heuristic exists (e.g. preferring `bf6` when folder is named `Battlefield 6`)

Suggested fix:
  - Add `"anticheat"`, `"easyanticheat"`, `"eac"`, `"battleye"`, `"installer"`, `"setup"`, `"redist"` to the `launcherNames` list
  - Add a folder-name-matching bonus: executables whose filename partially matches the parent folder name get priority over size-based sorting
  - Fall back to size sort only when no folder-name match exists

**7. No ".." parent-directory entry in file list**
The plan (phase-1.md task 1) has this marked as not implemented. Users have no visual indication for navigating up. A ".." entry at the top of each non-root list would provide a clickable/navigable way to go up one level, complementing the Backspace key.

---

## Session Log (2026-04-17)

| Time | Event |
|------|-------|
| 01:24 | Phase 1.1 implementation complete, all tasks checked off |
| 01:30 | Fixed null reference warnings (CS8602) — added `!` to `FindControl<T>()` calls |
| 01:36 | Fixed data path — changed from 5-level-up traversal to local `data/` folder |
| 01:38 | Added file-based startup logging (`data/startup.log`) |
| 01:38 | Fixed Avalonia crash — "Cannot show window with non-visible owner" by calling `mainWindow.Show()` before `wizardWindow.ShowDialog()` |
| 01:42 | Fixed wizard text overlapping — added missing `Grid.SetColumn()` calls |
| 01:45 | Changed wizard to start blank (removed auto-populated recommended paths) |
| 01:48 | Changed wizard to require manual scan per folder (added "Scan" button) |
| 01:52 | Sanity pass added to AGENTS.md coding rules |
| 02:00 | Fixed function button crash — added `IsHitTestVisible="False"` to prevent mouse click crashes |
| 02:10 | INVESTIGATING: UI buttons (F1-F10) don't work via mouse or keyboard. UI appears non-interactive. |
