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

- [ ] Detect when `data/settings.json` is missing or `IsFirstRun = true`
- [ ] Show wizard listing recommended library paths: `D:\Games`, common Steam library paths from registry, `E:\Games`
- [ ] User adds custom paths via folder picker, selecting default type per path
- [ ] **On adding a path, immediately scan it**:
  - List sub-folders under the root path
  - For each sub-folder: find `.exe` files, check for marker files (`.egsstore`, `steam_appid.txt`, `app.info`, etc.)
  - Detect game type (override or heuristic)
  - Store parsed entries in `data/games.json`
- [ ] On completion, save config with `IsFirstRun = false`
- [ ] **Rescan button** on existing library roots: re-scan the folder and update `games.json`

### 2. Virtual Browse (ILibraryManager.Browse)

- [ ] `Browse()` reads from `data/games.json` for the current library root — not real filesystem
- [ ] Returns virtual entries (game name, executable path, type) — no `FileSystemEntry` enumeration
- [ ] Navigation inside a root shows games, not folders

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

- [ ] Show game executable path for selected game
- [ ] Show resolved folder type (override or root default)
- [ ] Show last modified from the game entry in `games.json`
- [ ] Show placeholder text when nothing is selected

### 6. Navigation Polish

- [ ] Show selection highlight on the currently focused item
- [ ] Auto-scroll ListBox to keep selection visible
- [ ] Header shows library root name when inside a root; "Library Roots" at top level
- [ ] Path truncation at ~50 characters

### 7. Status Line Improvements

- [ ] Status line shows operation feedback: "Scanned 12 games in D:\Games", "Retagged DyingLight2 as Epic"
- [ ] Status line shows game count when inside a library root

### 8. Visual Polish

- [ ] Consistent spacing and alignment in both panes
- [ ] Path text uses a slightly smaller font to fit longer paths
- [ ] Pane splitter is clearly visible and draggable

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
| `F5` | — | Launch selected game (Phase 2+) |
| `F6` | — | SyncMove selected game (Phase 2.1) |
| `F9` | — (already at root level) | Jump to library-root listing |
| `Enter` | Drill into library root | Show game details |
| `Backspace` | — | Go back to library-root listing |
| Arrows | Navigate roots | Navigate games |

---

## Deliverables

- [ ] First-run wizard scans folders and populates `data/games.json`
- [ ] Navigation inside a root shows parsed games from `games.json`
- [ ] F2 opens Library Root Setup (add/remove/rescan roots)
- [ ] T key opens Game Setup (configure selected game: name, type, exe, launcher, args, manifest)
- [ ] Enhanced details panel with executable path and resolved type
- [ ] Polish: selection highlight, auto-scroll, status feedback
- [ ] App builds and runs cleanly on Windows

---

## Exit Criteria

Phase 1.1 is complete when:
- First launch wizard scans a folder and shows parsed games in the virtual browse view
- F2 opens Library Root Setup (add/remove/rescan roots)
- T key opens Game Setup to configure the selected game
- Navigation inside a root shows games from `games.json`, not raw filesystem entries
- The details panel shows executable path and resolved launcher type
- The app builds and runs cleanly on Windows
