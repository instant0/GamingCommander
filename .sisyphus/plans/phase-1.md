# Phase 1.0: Core UI & Infrastructure

## Goal

Deliver the functional Norton Commander-style dual-pane interface with a details panel, the configuration/setup engine, and core abstractions — building toward Phase 2 game detection.

## UX Model

### Left Pane — Navigation Panel (Virtual File System)

- **Virtual file system model**: Navigation reads from `data/games.json`, NOT the real filesystem.
- The real filesystem is only touched during **Setup/Rescan** operations.
- Header bar shows library root name (e.g. `D:\Games`) when inside a root.
- Content:
  - At library-root level: lists configured library roots as "drives" (e.g. `D:\Games (Standalone)`, `D:\SteamLibrary (Steam)`).
  - Inside a library root: lists **games** parsed from `games.json` — not raw filesystem entries.
  - No `..` entry needed — Backspace/F9 navigates between levels.
- **F9**: Jump directly to the library-root drive listing.
- Keyboard navigation: Arrow keys, Enter to drill in, Backspace to go up.
- Selection highlight follows current item.

### Right Pane — Details Panel

- Shows metadata for the currently selected item (left pane).
- Static header: `Details`.
- Initial fields to populate:
  - **Name** — display name of the game or folder.
  - **Path** — full filesystem path.
  - **Type** — launcher source (Steam, Epic, GOG, EA, Ubisoft, Standalone).
  - **Executable** — main exe path if known.
  - **Last Modified** — folder/file timestamp.
- Future fields (Phase 2+): cover art, play time, install size, save location, PCGamingWiki link.

### Setup Mode (First-Run / Settings)

- First-run wizard prompts user to add library roots.
- When adding a path, the user selects the folder's **default type**. This is stored alongside the path.
- **Critical**: On adding a path, the wizard immediately **scans** the folder and populates `data/games.json`.
- Folder type options: `Steam`, `Epic`, `EA App`, `GOG`, `Ubisoft Connect`, `Standalone`.
- **Why explicit classification?** Games can live outside their native launcher folders (e.g. Steam games copied manually, or EGS games with a `.egsstore` inside a non-EGS folder). A marker file found outside a known launcher folder is not a reliable source identifier — the user's intent is the ground truth.
- **Two-level classification model:**
  - **Root level:** each library root has a `defaultType`. All games inherit this type unless overridden.
  - **Game override:** individual games can be tagged with a different type. This overrides the root default.
- User can add/remove paths and rescan at any time via Library Root Setup (F2 at root level).
- User can retag individual games from within the browse view (T key).
- Configuration persisted as JSON (`data/settings.json`):
  ```json
  {
    "libraryRoots": [
      { "path": "D:\\SteamLibrary", "defaultType": "Steam" },
      { "path": "Y:\\Games",        "defaultType": "Standalone" }
    ]
  }
  ```
- Games database: `data/games.json` (see phase-1.1.md for schema).

### F-Key Summary

| Key | At Root Level | Inside Library Root |
|-----|---------------|---------------------|
| `F2` | Library Root Setup (add/remove/rescan roots) | Game Setup (edit selected game) |
| `F4` | — | Look up game metadata (Phase 2.2) |
| `F5` | — | Launch selected game (Phase 2+) |
| `F6` | — | SyncMove selected game (Phase 2.1) |
| `F9` | — (already at root level) | Jump to library-root drive listing |
| `Enter` | Drill into library root | Show game details |
| `Backspace` | — | Go back to library-root listing |
| Arrow keys | Navigate roots | Navigate games |
| `T` | — | Retag selected game (Phase 1.1) |

> **Virtual file system**: Normal navigation reads from `data/games.json`. The real filesystem is only touched during Setup/Rescan.
> **No copy, ren/mov, mkdir, or delete.** These are not general-purpose file management features. The only move operation is **SyncMove** (Phase 2.1) — a manifest-aware sync move for Steam/Epic/etc. games.
> **Two-level model:** root default type + per-game override. Override takes precedence.

---

## Tasks

### 1. Dual-Pane UI Implementation

- [x] Implement the classic dual-pane layout (left: browser, right: details).
- [x] Render `..` entry at top of file list.
- [x] Keyboard navigation: arrow keys, Enter, Backspace.
- [x] F9 shortcut to jump to library-root drive listing.
- [x] Adaptive layout: panes resize with window width.

### 2. Configuration Engine

- [x] JSON config file (`data/settings.json`) with typed library roots.
- [x] First-run wizard: prompt to add library roots — user selects folder type for each path.
- [x] Settings view: add/remove library roots, change folder type classification.
- [x] Config loaded at startup, hot-reload not required.

### 3. Core Abstractions

- [x] Define `IGame` interface (Name, Path, LauncherSource, ExecutablePath, etc.).
- [x] Define `ILauncher` interface (Detect(), Launch(IGame)).
- [x] Define `ILibraryManager` (Refresh(), GetGames(), AddRoot(), RemoveRoot(), Browse()).
- [x] Stub implementations for design-time and real filesystem browsing.

### 4. Details Panel — Initial Data

- [x] Display Name, Path, Type for selected item.
- [x] Show empty state when nothing selected.

---

## Deliverables

- [x] Working dual-pane UI with left browser + right details panel.
- [x] F9 shortcut to jump to library-root drive listing.
- [x] First-run setup wizard with library root management.
- [x] JSON configuration persisted to `data/config.json`.
- [x] `IGame`, `ILauncher`, `ILibraryManager` interfaces defined and stubbed.
- [x] Details panel shows Name, Path, Type for selected item.
- [x] App builds and runs cleanly on Windows.

---

## Exit Criteria

Phase 1.0 is complete when:
- The dual-pane UI renders correctly and is navigable with keyboard.
- F9 jumps to the library-root drive listing.
- The details panel updates when a left-pane item is selected.
- Configuration can be saved and loaded from `data/config.json`.
- Core interfaces (`IGame`, `ILauncher`, `ILibraryManager`) exist and are wired into the app.
- Research schemas from Phase 1.2 are available in `docs/research/` to inform Phase 2 implementation.
- The app builds and runs cleanly on Windows.
