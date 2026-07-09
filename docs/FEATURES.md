# GamingCommander — Feature Status

> **Purpose:** Living document tracking all planned, in-progress, and completed features.
> Each feature has a 1–2 line description for easy reference and project management.
> 
> **Last Updated:** 2026-05-31

---

## Legend

| Status | Meaning |
|--------|---------|
| ✅ **Complete** | Feature implemented and verified |
| 🔄 **In Progress** | Actively being worked on |
| 🔜 **Next** | Prioritized next work item |
| 📋 **Planned** | Designed but not started |
| 🧪 **Research** | Needs research before implementation |
| ❌ **Blocked** | Blocked by dependency or external factor |

---

## Core Architecture

### Platform & Framework

| Feature | Status | Description |
|---------|--------|-------------|
| **Avalonia 11.x UI** | ✅ Complete | Cross-platform .NET UI framework chosen as primary stack (Linux-dev friendly, Windows-compatible) |
| **Norton Commander Layout** | ✅ Complete | Dual-pane (left: browser, right: details) resizable layout, not fixed 80-column |
| **Dotnet 8.0+** | ✅ Complete | Modern .NET SDK target, nullable reference types enabled |

### Domain Models

| Feature | Status | Description |
|---------|--------|-------------|
| **GameSourceKind Enum** | ✅ Complete | Canonical source types: Standalone, Steam, Gog, Epic, EaApp, UbisoftConnect |
| **IGame Interface** | ✅ Complete | Core game abstraction: Id, Title, Source, InstallPath, ExecutablePath, LaunchTarget |
| **ILauncher Interface** | ✅ Complete | Launcher abstraction with Detect() and Launch() methods |
| **ILibraryManager Interface** | ✅ Complete | Manages library roots, game collections, and per-game operations |
| **IGamesDatabaseService** | ✅ Complete | Persists games database (games.json) with root/game CRUD operations |
| **IConfigService** | ✅ Complete | Loads/saves app configuration (settings.json) |
| **IMigrationPlanner Interface** | ✅ Complete | Interface for building migration plans (dry-run support) |
| **IGameDiscoveryService Interface** | ✅ Complete | Interface for scanning/identifying installed games |

---

## User Interface

### Navigation

| Feature | Status | Description |
|---------|--------|-------------|
| **Dual-Pane Layout** | ✅ Complete | Left pane for browsing, right pane for details |
| **Keyboard Navigation** | ✅ Complete | Arrow keys, Enter to drill in, Backspace to go up |
| **F9 Library Root Jump** | ✅ Complete | Jump directly to library-root drive listing |
| **Selection Highlight** | ✅ Complete | Visual indicator of currently selected item |
| **Auto-Scroll** | ✅ Complete | ListBox auto-scrolls to keep selection visible |
| **Path Truncation** | ✅ Complete | Long paths truncated at ~50 chars to fit UI |
| **Virtual File System** | ✅ Complete | Navigation reads from data/games.json, not real filesystem |

### Details Panel

| Feature | Status | Description |
|---------|--------|-------------|
| **Basic Details Display** | ✅ Complete | Name, Path, Type, Executable, LastModified for selected item |
| **Executable Path** | ✅ Complete | Shows full executable path in details pane |
| **Resolved Type** | ✅ Complete | Shows override or root-default type |

### Function Keys

| Feature | Status | Description |
|---------|--------|-------------|
| **F1 Help** | 📋 Planned | Help text / keyboard shortcuts reference (button exists, no handler) |
| **F2 Setup** | ✅ Complete | Opens Library Root Setup dialog (F2 key works; mouse click disabled) — add/remove/rescan folders with type selection |
| **F3 View** | 📋 Planned | Placeholder — button exists but no handler assigned |
| **F4 Move** | 📋 Planned | Move game folder with re-registration (Phase 2) — button not yet in UI |
| **F5 Launch** | 📋 Planned | Launch selected game (button exists, no handler) |
| **F6 Details** | 📋 Planned | Show game details + PCGamingWiki lookup (Phase 2.2) — button not yet in UI |
| **F7 Fix** | 📋 Planned | Fix installation issues (button not yet in UI) |
| **F8 View** | 📋 Planned | Toggle between Library Roots and Browse by Category (Phase 3.5) |
| **F9 Drives** | ✅ Complete | Jump to library-root drive listing (F9 key works; mouse click disabled) |
| **F10 Quit** | 📋 Planned | Exit application gracefully (button exists, no handler) |
| **T Retag** | ✅ Complete | Retag selected game with different source type |
| **S Search** | 📋 Planned | Open quick-search overlay — match game name, genre, developer, publisher, path (Phase 3.5) |
| **Enter Drill-In** | ✅ Complete | Enter library root to browse games |
| **Backspace Navigate Up** | ✅ Complete | Navigate back from library root to root listing |

---

### NEW: Setup Wizard (F2)

| Feature | Status | Description |
|---------|--------|-------------|
| **Auto-Detect Stores** | 📋 Planned | Query Windows Registry for installed launchers (Steam, GOG, EA, Ubisoft, Epic) |
| **Store Detection Prompts** | 📋 Planned | Show detected stores with add confirmation |
| **Manual Folder Add** | ✅ Complete | User can add custom library folders with type selection |
| **Type Assignment** | ✅ Complete | Per-folder type (Standalone, Steam, Gog, etc.) |
| **Initial Scan** | 📋 Planned | Full scan of all added roots, populates games.json (currently scan is manual per-folder) |

### Store Detection Signals (Registry)

| Launcher | Registry Key | Value |
|----------|------------|-------|
| **Steam** | `HKLM\SOFTWARE\Valve\Steam` | InstallPath |
| **Epic** | `HKLM\SOFTWARE\WOW6432Node\Epic Games\EpicGamesLauncher` | AppDataPath |
| **GOG** | `HKLM\SOFTWARE\GOG.com\Games` | (enumerate keys) |
| **EA App** | `HKLM\SOFTWARE\EA Games\Electronic Arts` | `Origin Folder` or `EA Folder` |
| **Ubisoft** | `HKLM\SOFTWARE\WOW6432Node\Ubisoft\Launcher` | InstallDir |

---

### NEW: Game Details & Metadata (F6)

| Feature | Status | Description |
|---------|--------|-------------|
| **Local Details** | ✅ Complete | Show from games.json (name, path, type, exe) |
| **PCGamingWiki Lookup** | 🔜 Planned | Query pcgamingwiki.com API/search for game title |
| **Metadata Cache** | 🔜 Planned | Store in data/game-metadata.json (keyed by game ID or title) |
| **Cached Fields** | 🔜 Planned | Store: Genre, Developer, Publisher, ReleaseDate, StoreLinks, etc. |
| **Display in Details** | 🔜 Planned | Show enriched data in details pane |
| **Executable Selection** | 🔜 Planned | Show all .exe in folder, user picks, stored in DB |
| **Largest EXE Default** | 🔜 Planned | Auto-select largest .exe as default (not helper) |

---

### NEW: Virtual File System & Executable Detection

When user adds a folder path (e.g., `Y:\Games`), each immediate subfolder = ONE GAME.

**Executable Detection Logic**:
1. Recursively find all `.exe` files (max 2 levels deep to avoid helper files)
2. Filter out helper paths: `_CommonRedist`, `EasyAntiCheat`, `DevTools`, `Support`, `Docs`
3. Filter out non-game .exe: `*installer*`, `*crash_reporter*`, `*unins*`, `*loader*`
4. Sort by file size descending
5. **Largest = default** executable (user can override)

**Example Output**:
```json
{
  "folder": "SPACEMAR2",
  "store": "Standalone",
  "exes": [
    {"path": "client_pc/root/bin/pc/Warhammer 40000 Space Marine 2 - Retail.exe", "size": 84735544},
    {"path": "start_protected_game.exe", "size": 3937008},
    {"path": "Warhammer 40000 Space Marine 2.exe", "size": 1482016}
  ],
  "exe": "client_pc/root/bin/pc/Warhammer 40000 Space Marine 2 - Retail.exe"
}
```

**User Override**: User can pick any exe from list, stored in `games.json` ExecutablePath.

### NEW: Game Database Schema

#### data/games.json (existing)
```json
{
  "Roots": [
    {
      "RootPath": "Y:\\Games\\",
      "DefaultType": "Steam",
      "Games": [
        {
          "Id": "...", "FolderName": "...", "DisplayName": "...",
          "GameSource": 1, "Override": false,
          "ExecutablePath": "...", "LauncherPath": "", "CmdlineArgs": "",
          "ManifestPath": "...", "LastScanned": "...", "LastModified": "..."
        }
      ]
    }
  ]
}
```

#### data/game-metadata.json (NEW - for F6 lookup)
```json
{
  "games": {
    "<game-id>": {
      "title": "Game Title",
      "pcgamingwiki_id": "Game_Title",
      "genre": "Action RPG",
      "developer": "Dev Studio",
      "publisher": "Publisher",
      "release_date": "2023-11-01",
      "store_links": {
        "steam": "https://store.steampowered.com/app/...",
        "gog": "https://www.gog.com/...",
        "epic": "..."
      },
      "last_updated": "2026-04-26"
    }
  }
}
```

---

### NEW: Fix Installation (F7)

#### Missing Detection Types

| Type | Detection | Fix Action |
|------|-----------|-----------|
| **Missing Folder** | ACF exists but `steamapps\common\<installdir>` missing | Create folder, prompt reinstall, or restore from backup |
| **Missing ACF** | Folder in `common\` but no `appmanifest_<id>.acf` | Regenerate ACF from detected game data |
| **Orphaned ACF** | ACF reference to non-library path | Prompt user for manual folder selection |
| **Broken Path** | ExecutablePath doesn't exist on disk | Prompt user to locate exe or fix path |
| **Corrupt Manifest** | .item/.mfst JSON parse fails | Prompt reinstall or generate from metadata |

#### Re-Registration Per Launcher

| Launcher | Where Registered | Update Field |
|----------|----------------|--------------|
| **Steam** | `steamapps\appmanifest_<id>.acf` | `installdir` + update libraryfolders.vdf |
| **Epic** | `ProgramData\Epic\EpicGamesLauncher\Data\Manifests\*.item` | `InstallLocation` in JSON |
| **GOG** | `HKLM\SOFTWARE\GOG.com\Games\<gameName>` | `gameName` → InstallDir |
| **EA App** | `HKLM\SOFTWARE\<Dev>\<Game>` | `Install Dir` registry value |
| **Ubisoft** | `HKLM\SOFTWARE\Ubisoft\Launcher\Installs` | `InstallDir` registry value |

---

### NEW: Virtual File System

| Feature | Status | Description |
|---------|--------|-------------|
| **Steam Mapping** | 🔜 Planned | Map `steamapps\` → virtual folder, show as `\steam\` |
| **Steam Manifests** | 🔜 Planned | Show `appmanifest_*.acf` files in virtual view |
| **Missing Indicators** | 🔜 Planned | Show "Missing Folder" for orphan ACF, "Missing ACF" for orphan folders |
| **GOG/Epic Virtual** | 🔜 Planned | Similar structure for GOG Galaxy and Epic manifests |

#### Virtual Layout Example (Steam Library)
```
Y:\                                      (Steam Library Root, e.g., \\mnt\e\steamlibrary)
  libraryfolders.vdf                      (lists all library paths)
  steamapps\
    appmanifest_123456.acf                (ACF for each app)
    appmanifest_789012.acf
    common\                              (actual game installations)
      ACValhalla\
        ACValhalla.exe
      AW2\
        AlanWake2.exe
```

#### How It Maps to Virtual View
| Virtual Path | Actual Path (inside library) |
|--------------|------------------------------|
| `\libraryfolders.vdf` | `libraryfolders.vdf` |
| `\steamapps\appmanifest_*.acf` | `steamapps\appmanifest_*.acf` |
| `\game_name\` | `steamapps\common\<game_name>\` |

#### Virtual Layout Example (Multi-Launcher)
```
Y:\
  steam\                      → Y:\steamapps\
    [game1]\
    [game2]\
    manifests\
      appmanifest_123.acf
  epic\                       → C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests\
    games\
      SomeGame.item
  gog\                        → HKLM\SOFTWARE\GOG.com\Games (registry view)
    games\
```

---

### NEW: Game Move (F4 - Phase 2)

| Feature | Status | Description |
|---------|--------|-------------|
| **Source→Target Select** | 📋 Planned | Select source folder, choose target drive/folder |
| **Preflight Check** | 📋 Planned | Verify disk space, permissions, target doesn't exist |
| **Backup Manifests** | 📋 Planned | Backup original ACF/item/registry entries before move |
| **File Move** | 📋 Planned | Move files (or create junction/symlink + copy) |
| **Manifest Update** | 📋 Planned | Update all registration fields to new path |
| **Dry-Run Mode** | 📋 Planned | Show what would happen, require confirmation |
| **Rollback** | 📋 Planned | Log reversal steps, allow undo within session |

### Setup & GUI Stabilization (Phase 1.1a) ⬅️ CURRENT PRIORITY

All items below must be fixed before any new features. Plan: `planning/99-stabilization.md`

| Bug | Priority | Area | Fix Summary |
|-----|----------|------|-------------|
| **Exe Detection Picks Wrong File** | 🔴 Critical | Scanner | Add anti-cheat/installer/setup/redist to launcher exclusion list; prefer exe name matching folder name over file size |
| **Non-Game Folders in Scan** | 🔴 Critical | Scanner | Skip folders with zero `.exe` and no game markers; add user-configurable ignore list |
| **Game Entries Navigable (Wrong)** | 🔴 Critical | Navigation | Change `Kind` from `Directory` to `File` for game entries — games are terminal, selecting shows details |
| **Backspace Jumps Two Levels** | 🔴 Critical | Navigation | Go up exactly one level, not straight to Library Roots |
| **Arrow Key Focus After Backspace** | 🔴 Critical | Navigation | Call `Focus()` on ListBox after data reload; sync SelectedIndex |
| **".." Entry Missing** | 🔴 Critical | Navigation | Render `..` at top of every non-root list for visual navigation cue |
| **Mouse Click on Buttons** | 🔴 Critical | Mouse | Remove `IsHitTestVisible=False`; wire click handlers to OnKeyDown actions |
| **Mouse Double-Click Drill-In** | 🔴 Critical | Mouse | Wire `DoubleTapped` event on ListBox items to call NavigateInto |
| **F10 Quit Not Wired** | 🟡 High | Keyboard | Add F10 → Close() handler |
| **F3/F5/F8/S Placeholders** | 🟡 High | Keyboard | Add placeholder handlers with status bar feedback ("not yet implemented") |

### UI Polish (Post-Stabilization)

| Feature | Status | Description |
|---------|--------|-------------|
| **Theme Polish** | 📋 Planned | Norton Commander retro theme refinements |
| **Search/Filter** | 📋 Planned | In-UI search and filtering |
| **Visual Splitter** | ✅ Complete | Clearly visible, draggable pane splitter |

---

## Configuration & Setup

### First-Run Experience

| Feature | Status | Description |
|---------|--------|-------------|
| **First-Run Wizard** | ✅ Complete | Detects missing config, prompts user to add library roots |
| **Folder Type Selection** | ✅ Complete | User selects default type per library root path |
| **Manual Scan Per Folder** | ✅ Complete | User clicks "Scan" per folder to detect games |

### Configuration Management

| Feature | Status | Description |
|---------|--------|-------------|
| **JSON Settings Persistence** | ✅ Complete | data/settings.json stores library roots and overrides |
| **JSON Games Database** | ✅ Complete | data/games.json stores all scanned game entries |
| **Library Root Management** | ✅ Complete | Add, remove, rescan library roots via F2 |
| **Game Entry Editing** | ✅ Complete | Edit display name, executable, launcher, args, manifest path per game |
| **Per-Game Override** | ✅ Complete | Individual games can override root default type |
| **Folder Override System** | ✅ Complete | data/settings.json supports FolderOverride list |

### Data Files

| Feature | Status | Description |
|---------|--------|-------------|
| **games.json Schema** | ✅ Complete | Structured: roots[] with rootPath, defaultType, games[] |
| **GameEntry Fields** | ✅ Complete | Id, FolderName, DisplayName, GameSource, Override, ExecutablePath, LauncherPath, CmdlineArgs, ManifestPath, LastScanned, LastModified, Extra |
| **settings.json Schema** | ✅ Complete | LibraryRoots[], FolderOverrides[], IsFirstRun |
| **Startup Logging** | ✅ Complete | data/startup.log tracks initialization events |

---

## Game Detection

### Folder Scanning

| Feature | Status | Description |
|---------|--------|-------------|
| **Generic EXE Detection** | ✅ Complete | Scans sub-folders for .exe files as potential game launchers |
| **Steam Marker Detection** | ✅ Complete | Detects steam_api.dll / steam_appid.txt as Steam game signal |
| **Epic Marker Detection** | ✅ Complete | Detects .egsstore subfolder as Epic Games Store signal |
| **GOG Marker Detection** | ✅ Complete | Detects goggame-*.info files as GOG signal |
| **EA Marker Detection** | ✅ Complete | Detects EA App conventions as signal |
| **Ubisoft Marker Detection** | ✅ Complete | Detects Ubisoft Connect conventions as signal |

### Steam Integration

| Feature | Status | Description |
|---------|--------|-------------|
| **Steam Library Folder Detection** | 🧪 Research | Read libraryfolders.vdf to enumerate all Steam library paths |
| **Steam ACF Parsing** | 🧪 Research | Parse appmanifest_*.acf files for game metadata |
| **VDF Parsing** | 🧪 Research | Parse Valve Data Format (VDF) for library configuration |
| **Steam Game Listing** | 📋 Planned | List all Steam games from detected libraries |
| **Steam Launch via Steam://** | 📋 Planned | Launch Steam games via steam://run/<appid> URI scheme |
| **Steam Manifest Patching** | 📋 Planned | Update install paths in Steam ACF manifests |

### Epic Games Store Integration

| Feature | Status | Description |
|---------|--------|-------------|
| **Epic Manifest Discovery** | 🧪 Research | Locate .item JSON files in ProgramData/Epic |
| **Epic Manifest Parsing** | 🧪 Research | Parse Epic .item JSON for InstallLocation, AppId, LaunchExecutable |
| **Epic Game Listing** | 📋 Planned | List all Epic games from detected manifests |
| **Epic Manifest Patching** | 📋 Planned | Update InstallLocation in Epic .item manifests |
| **Epic Manifest Generation** | 📋 Planned | Generate missing .item manifest files for games without them |
| **Epic Launch via EGL** | 📋 Planned | Launch Epic games through Epic Games Launcher |

### GOG Galaxy Integration

| Feature | Status | Description |
|---------|--------|-------------|
| **GOG Game Discovery** | 🧪 Research | Locate GOG games via registry and local manifest files |
| **GOG Manifest Parsing** | 🧪 Research | Parse goggame-*.info files for game metadata |
| **GOG Game Listing** | 📋 Planned | List all GOG Galaxy games |
| **GOG Launch** | 📋 Planned | Launch GOG games via GOG Galaxy |

### EA App Integration

| Feature | Status | Description |
|---------|--------|-------------|
| **EA App Game Discovery** | 🧪 Research | Locate EA games via registry and local database |
| **EA Game Listing** | 📋 Planned | List all EA App games |
| **EA Launch** | 📋 Planned | Launch EA games via EA App |

### Ubisoft Connect Integration

| Feature | Status | Description |
|---------|--------|-------------|
| **Ubisoft Game Discovery** | 🧪 Research | Locate Ubisoft games via cache/*.json and registry |
| **Ubisoft Manifest Parsing** | 📋 Planned | Parse Ubisoft Connect cache manifests |
| **Ubisoft Game Listing** | 📋 Planned | List all Ubisoft Connect games |
| **Ubisoft Launch** | 📋 Planned | Launch Ubisoft games via Ubisoft Connect |

### Standalone Game Detection

| Feature | Status | Description |
|---------|--------|-------------|
| **Standalone EXE Detection** | ✅ Complete | Detect standalone .exe files in user-supplied folders |
| **Standalone Game Listing** | ✅ Complete | List standalone games from library roots |
| **Standalone Launch** | 📋 Planned | Launch standalone games via direct executable invocation |

---

## Category Browsing (KODI-Style)

Default view on launch is **Library Roots** — configured paths as "drives". Press F8 to toggle to **Browse by Category** for KODI-style drill-down discovery. Press **S** from any view to open a universal quick-search.

### Navigation Modes

| Feature | Status | Description |
|---------|--------|-------------|
| **Default: Library Roots** | ✅ Complete | Configured paths → games inside — the standard flat-per-root view |
| **F8 Quick Toggle** | 📋 Planned | Flat toggle between Library Roots and Browse by Category (one press, no nested menus) |
| **Category Drill-Down** | 📋 Planned | Category → Value → Filtered Games (breadcrumb navigation) |
| **Launcher Category** | 📋 Planned | Filter by GameSourceKind (Steam, Epic, Standalone, etc.) — always populated |
| **Genre Category** | 📋 Planned | Filter by genre tag — requires metadata from Phase 2.2 |
| **Publisher Category** | 📋 Planned | Filter by publisher — requires metadata from Phase 2.2 |
| **Year of Release Category** | 📋 Planned | Group by year or decade — requires metadata from Phase 2.2 |
| **Gamerankings Rating Category** | 📋 Planned | Bucketed into ranges (90%+, 80-89%, etc.) — requires metadata |
| **Cross-Root Aggregation** | 📋 Planned | Category results span all library roots, not just one |
| **Empty-State Handling** | 📋 Planned | Categories with no data shown greyed-out or hidden |

### Quick Search (S Key)

| Feature | Status | Description |
|---------|--------|-------------|
| **Search Overlay** | 📋 Planned | Press S to open search input at bottom of window from any view mode |
| **Cross-Field Matching** | 📋 Planned | Matches game name, folder name, genre, developer, publisher, launcher, path — union logic |
| **Wildcard Support** | 📋 Planned | `*` (multi-char), `?` (single-char), plain text does substring match |
| **Multi-Term AND** | 📋 Planned | Space-separated terms narrow results: `rpg cd projekt` = RPG genre AND CD Projekt dev |
| **Real-Time Results** | 📋 Planned | Results update in left pane as user types (debounced) |
| **Match Reason Badge** | 📋 Planned | Each result shows why it matched: `matched: name, genre (RPG)` |
| **Escape to Dismiss** | 📋 Planned | Returns to previous browse state, restores item list |
| **Global Scope — No Context** | 📋 Planned | Search ALWAYS queries the entire virtual file system, ignoring current root/category/view — pressing S from anywhere searches everything |

Full plan: `planning/05-phase-3-category-browse.md`

---

## Game Migration (SyncMove)

### Migration Planning

| Feature | Status | Description |
|---------|--------|-------------|
| **Migration Modes** | ⚠️ Needs Update | MoveOnly, MoveAndLink, ManifestRepairOnly modes defined — MoveAndLink is deprecated; replaced by Fix Registration mode |
| **Dry-Run Planning** | ✅ Complete | IMigrationPlanner.BuildDryRunPlan() simulates operations |
| **Manifest Backup Detection** | ✅ Complete | Detects Steam/Epic games requiring manifest backup |

### F6 SyncMove Dialog

| Feature | Status | Description |
|---------|--------|-------------|
| **F6 SyncMove Dialog** | 📋 Planned | Opens dialog with source shown, destination picker, mode selector |
| **Preflight Validation** | 📋 Planned | Validates disk space, accessibility, target existence before mutation |
| **Progress Feedback** | 📋 Planned | Status line shows operation progress |
| **Post-Op Reversal Docs** | 📋 Planned | Show reversal instructions in status line after operation |

### File Operations

| Feature | Status | Description |
|---------|--------|-------------|
| **Game Folder Move** | 📋 Planned | Move game files to new location with safety checks |
| **Directory Junction Creation** | 📋 Planned | Create junction at original location pointing to new location |
| **Manifest-Only Repair** | 📋 Planned | Update manifest without moving files (when folder already moved) |

### Manifest Management

| Feature | Status | Description |
|---------|--------|-------------|
| **Steam ACF Backup** | 📋 Planned | Backup manifest to data/backups/<gameid>_manifest_backup.acf |
| **Steam ACF Update** | 📋 Planned | Update installdir field in Steam ACF manifests |
| **Epic JSON Backup** | 📋 Planned | Backup .item manifest to data/backups/ before modification |
| **Epic JSON Update** | 📋 Planned | Update InstallLocation field in Epic .item manifests |
| **Cross-Library-Root Moves** | 📋 Planned | Handle Steam moves between library roots (update folder + ACF) |

### Logging & Recovery

| Feature | Status | Description |
|---------|--------|-------------|
| **Migration Logging** | 📋 Planned | data/migration_log.jsonl records all migration operations |
| **Backup Manifests** | 📋 Planned | data/backups/ stores original manifest copies |
| **Resumable Operations** | 📋 Planned | Detect already-moved files and skip on re-run |

---

## Metadata Enrichment

### F4 Metadata Lookup

| Feature | Status | Description |
|---------|--------|-------------|
| **F4 Lookup Trigger** | 📋 Planned | F4 looks up metadata for selected game via online sources |
| **Cascading Source Fallback** | 📋 Planned | Query sources in priority order: PCGW → SteamDB → IGDB |
| **Local Cache** | 📋 Planned | Cache results to data/games_db.json with merge logic |
| **Status Line Feedback** | 📋 Planned | Show progress and result summary during lookup |

### Data Sources

| Feature | Status | Description |
|---------|--------|-------------|
| **GamingResourcesManifest** | 📋 Planned | Central registry of all metadata sources, query formats, field schemas |
| **PCGamingWiki Provider** | 📋 Planned | Query PCGW Cargo API for developers, publishers, genres, save locations (no key) |
| **SteamDB Provider** | 📋 Planned | Fetch SteamDB JSON for name, tags, player counts (no key) |
| **Steam Store Provider** | 📋 Planned | Query Steam Store API for descriptions, images, requirements (no key) |
| **IGDB Provider** | 📋 Planned | Query IGDB API for rich metadata (API key required) |
| **IGDB OAuth2** | 📋 Planned | Twitch OAuth2 flow for IGDB bearer token management |

### Enriched Details Display

| Feature | Status | Description |
|---------|--------|-------------|
| **Developer/Publisher** | 📋 Planned | Show enriched developer and publisher from lookup |
| **Genre Tags** | 📋 Planned | Display genre tags in details panel |
| **Save Locations** | 📋 Planned | Show known save file locations from PCGW |
| **PCGW Link** | 📋 Planned | Clickable link to PCGamingWiki page |
| **Cover Art** | 📋 Planned | Display cover art image if URL available |
| **Release Date** | 📋 Planned | Show release date from metadata |
| **IGDB Score** | 📋 Planned | Display IGDB rating if available |
| **System Requirements** | 📋 Planned | Show minimum/recommended requirements |

---

## Manifest & Data Integrity

### Orphaned Manifest Detection

| Feature | Status | Description |
|---------|--------|-------------|
| **VDF Manifest Detection** | 📋 Planned | Detect when Steam libraryfolders.vdf or game ACF is found on a different disk than the game folder |
| **Manifest Path Validation** | 📋 Planned | Cross-reference manifest paths against actual game install locations |
| **Orphaned Manifest Recovery** | 📋 Planned | Option to move/update orphaned manifests to match game location |
| **Manifest Location Heuristics** | 📋 Planned | When manifest is missing from expected location, search alternative paths |

### Epic Manifest Integrity

| Feature | Status | Description |
|---------|--------|-------------|
| **Missing Manifest Detection** | 📋 Planned | Detect games with .egsstore but no corresponding .item in ProgramData |
| **Manifest Regeneration** | 📋 Planned | Generate new .item manifest files for Epic games missing them |
| **Manifest Path Tracking** | ✅ Complete | GameEntry stores manifestPath for canonical Epic manifest location |

### Game Location Validation

| Feature | Status | Description |
|---------|--------|-------------|
| **Executable Presence Check** | 📋 Planned | Verify game executable still exists at expected path |
| **Manifest vs Filesystem Cross-Check** | 📋 Planned | Compare manifest InstallLocation against actual folder location |
| **Stale Entry Detection** | 📋 Planned | Flag games with outdated LastModified or missing executables |

---

## Testing & Validation

### Unit Tests

| Feature | Status | Description |
|---------|--------|-------------|
| **Core Model Tests** | ✅ Complete | GamingCommander.Core.Tests: 1 test (GameRecord) |
| **Detection Tests** | ✅ Complete | GamingCommander.Detection.Tests: 1 test |
| **Migration Tests** | ✅ Complete | GamingCommander.Migration.Tests: 1 test |
| **Steam ACF Parsing Tests** | 📋 Planned | Unit tests for Steam manifest parsing logic |
| **Epic Manifest Parsing Tests** | 📋 Planned | Unit tests for Epic .item JSON parsing |
| **Migration Dry-Run Tests** | 📋 Planned | Unit tests for migration planning logic |
| **Library Manager Tests** | 📋 Planned | Unit tests for ILibraryManager operations |

### Integration Tests

| Feature | Status | Description |
|---------|--------|-------------|
| **Migration Integration Tests** | 📋 Planned | Integration tests with temp directories and fake manifests |
| **Steam Integration Tests** | 📋 Planned | Fixture-based tests with real (or mock) Steam data |
| **Detector Integration Tests** | 📋 Planned | Integration tests for IGameDiscoveryService |

---

## Advanced Features

### Online Synchronization

| Feature | Status | Description |
|---------|--------|-------------|
| **Metadata Repository Sync** | 📋 Planned | Sync game metadata via GitHub-hosted source |
| **Remote Manifest Fetch** | 📋 Planned | Fetch game metadata from online repositories |
| **Config Export/Import** | 📋 Planned | Export/import configuration to/from file |

### Advanced Operations

| Feature | Status | Description |
|---------|--------|-------------|
| **Batch Operations** | 📋 Planned | Select multiple games for bulk operations |
| **Install Size Calculation** | 📋 Planned | Calculate game folder sizes for disk space planning |
| **Play Time Tracking** | 📋 Planned | Track and display total play time per game |
| **Favorite/Tagging System** | 📋 Planned | User-defined favorites and custom tags |

### Robustness & Safety

| Feature | Status | Description |
|---------|--------|-------------|
| **Non-Fatal Launcher Failures** | ✅ Complete | Launcher detection failures are non-fatal and diagnosable |
| **Safe Migration Rollback** | 📋 Planned | Rollback metadata/logging for failed migrations |
| **Operation Recovery** | 📋 Planned | Resume interrupted migration operations |

---

## Developer Experience

### Build & DevOps

| Feature | Status | Description |
|---------|--------|-------------|
| **Windows Build** | ✅ Complete | Solution builds on Windows with .NET 8 SDK |
| **Linux Build** | ❌ Blocked | Linux build blocked by Windows-specific runtime host package |
| **Deterministic Builds** | ✅ Complete | Directory.Build.props enables deterministic output |
| **Code Analysis** | ✅ Complete | EnforceCodeStyleInBuild enabled with latest AnalysisLevel |
| **Nullable Reference Types** | ✅ Complete | All projects have nullable reference types enabled |

### Logging & Diagnostics

| Feature | Status | Description |
|---------|--------|-------------|
| **Startup Logging** | ✅ Complete | data/startup.log tracks initialization |
| **Operation Logging** | 📋 Planned | Migration operations emit step-by-step logs |
| **Error Diagnostics** | 📋 Planned | All launcher detection failures are diagnosable without leaking sensitive data |

---

## Universal Game Store Integration Framework

### Detection Signal Types

Each game store uses a different mechanism to track installed games. GamingCommander must support all five patterns:

| Category | Store | Detection Method | Primary Source |
|----------|-------|-----------------|----------------|
| **A: Registry-Based** | GOG Galaxy, EA App (legacy) | Windows registry keys per game | HKLM\SOFTWARE\... |
| **B: Fixed Manifest** | Epic Games Store | JSON `.item` files in fixed directory | ProgramData\Epic\... |
| **C: Master + Per-Game** | Steam | `libraryfolders.vdf` + `appmanifest_*.acf` | steamapps\... |
| **D: Encrypted Local DB** | EA Desktop (new) | AES-encrypted JSON in AppData | C:\ProgramData\EA Desktop\... |
| **E: HTTP Manifest** | Origin (legacy) | `.mfst` files containing HTTP query strings | ProgramData\Origin\LocalContent |

### Complete Launcher Comparison Matrix

| Aspect | Steam | Epic | GOG | EA Desktop | Origin | Ubisoft |
|--------|-------|------|-----|------------|--------|---------|
| **Registry Keys** | SteamPath in HKCU | None primary | HKLM\GOG.com\Games | `HKLM\SOFTWARE\<Dev>\<Game>\Install Dir` | None primary | `HKLM\SOFTWARE\Ubisoft\Launcher\Installs` |
| **Manifest Location** | `steamapps/appmanifest_*.acf` | `ProgramData\Epic\...\Manifests\*.item` | None (registry only) | `C:\ProgramData\EA Desktop\<hash>\IS` (encrypted) | `ProgramData\Origin\LocalContent\*.mfst` | `cache\*.json` |
| **Master Index** | `libraryfolders.vdf` | None | None | None | None | None |
| **Detection Signal** | ACF file exists + folder in `common/` | `.item` file exists | Registry key exists | Encrypted IS file or registry `Install Dir` | `.mfst` with `dipInstallPath` | Registry key exists |
| **Re-Register After Move** | Update `installdir` in ACF | Update `InstallLocation` in `.item` | Update `gameName` in registry | Update `baseInstallPath` (encrypted) or registry | Update `dipInstallPath` in `.mfst` | Update `InstallDir` in registry |
| **Missing Detection** | Folder not in `steamapps\common\<installdir>` | `InstallLocation` path doesn't exist | `gameName` path doesn't exist | `baseInstallPath` doesn't exist | `dipInstallPath` empty or missing | `InstallDir` path doesn't exist |

---

### GOG Galaxy Integration

#### Detection Signals
| Signal | Location | Format |
|--------|----------|--------|
| **Primary** | `HKLM\SOFTWARE\GOG.com\Games` (and `HKLM\SOFTWARE\WOW6432Node\GOG.com\Games`) | Registry keys, one per game |
| **Game ID** | Sub-key name (e.g., `1234567890`) | String |
| **Install Path** | `gameName` value in sub-key | Registry string |
| **Exe Path** | `exe` value in sub-key | Registry string |
| **Launch Arguments** | `launchCommand` value in sub-key | Registry string |

#### Re-Registration After Move
1. Update `gameName` value in registry to new path
2. Update `exe` value if executable moved
3. Restart GOG Galaxy (or trigger refresh)

#### Missing Game Detection
- Check if registry entry exists but `gameName` path does not exist
- Flag as "registered but files missing"

#### Key Registry Structure
```
HKLM\SOFTWARE\GOG.com\Games\
  <GameID>\
    gameName = "D:\Games\My Game"
    exe = "D:\Games\My Game\game.exe"
    launchCommand = "--arg1 --arg2"
    gameID = "1234567890"
    productName = "My Game"
```

---

### EA Desktop (New) Integration

#### Detection Signals
| Signal | Location | Format |
|--------|----------|--------|
| **Primary** | `C:\ProgramData\EA Desktop\<hash>\IS` | AES-256-CBC encrypted JSON |
| **Fallback** | Game-specific registry keys | HKLM\SOFTWARE\<Developer>\<Game> |
| **Installer Data** | Game folder `__Installer\installerdata.xml` | XML with DiPManifest schema |

#### Encrypted Manifest Decryption
EA Desktop encrypts its game list using:
- **Algorithm**: AES-256-CBC
- **Key Derivation**: SHA3-256 of (hardwareInfoHash + allUsersGenericId + "IS")
- **IV**: SHA3-256 of (allUsersGenericId + "IS") — same for all users
- **Hardware Info**: CPU, GPU, motherboard, BIOS, C: drive serial (changes = key lost)

#### Decrypted JSON Structure (installInfos array)
```json
{
  "baseInstallPath": "M:\\Games\\Apex\\",
  "baseSlug": "apex-legends",
  "softwareId": "Origin.SFT.50.0000848",
  "installedVersion": "1.1.0.7",
  "detailedState": {
    "installStatus": 3,
    "installPhase": 2
  },
  "installCheck": "[HKEY_LOCAL_MACHINE\\SOFTWARE\\Respawn\\Apex\\Install Dir]__Installer\\installerdata.xml",
  "executableCheck": "[HKEY_LOCAL_MACHINE\\SOFTWARE\\Respawn\\Apex\\Install Dir]EasyAntiCheat_launcher.exe",
  "localInstallProperties": {
    "launchers": [
      {
        "exePath": "[HKEY_LOCAL_MACHINE\\SOFTWARE\\Respawn\\Apex\\Install Dir]EasyAntiCheat_launcher.exe",
        "cmdArgs": "",
        "requires64BitOs": true
      }
    ]
  }
}
```

#### Re-Registration After Move
1. Decrypt the encrypted IS file (requires hardware-derived key)
2. Update `baseInstallPath` in decrypted JSON
3. Re-encrypt and save back (EA App must be closed)
4. OR: Update game-specific registry `Install Dir` value directly
5. Restart EA Desktop

#### Registry Keys for Individual Games
EA games store install paths in developer-specific registry keys:
```
HKLM\SOFTWARE\<DeveloperName>\<GameName>\
  Install Dir = "D:\\Games\\GameName"
```
Examples:
- `HKLM\SOFTWARE\Respawn\Apex` → Install Dir
- `HKLM\SOFTWARE\EA Games\Need for Speed Heat` → Install Dir
- `HKLM\SOFTWARE\BioWare\Mass Effect` → Install Dir

#### EA App Data Locations
| Location | Path | Contents |
|----------|------|----------|
| **App Install** | `C:\Program Files\Electronic Arts\EA Desktop\EA Desktop` | EA App executable |
| **Encrypted DB** | `C:\ProgramData\EA Desktop\<hash>\IS` | AES-256-CBC encrypted game list |
| **Per-User Config** | `%LOCALAPPDATA%\Electronic Arts\EA Desktop\` | `user_*.ini` with `user.downloadinplacedir` |
| **Logs** | `%LOCALAPPDATA%\Electronic Arts\EA Desktop\Logs` | EABackgroundService.log |
| **Legacy Game Data** | `C:\Program Files (x86)\Origin Games` | Origin-era default game folder |

#### EA App Re-Registration Methods
| Method | Steps | Notes |
|--------|-------|-------|
| **Registry Direct** | 1. Close EA App 2. Update `Install Dir` in `HKLM\SOFTWARE\<Dev>\<Game>` 3. Restart EA App | Simplest for individual games |
| **Encrypted IS File** | 1. Close EA App 2. Decrypt IS file (AES-256-CBC, hardware-derived key) 3. Update `baseInstallPath` 4. Re-encrypt 5. Restart | Complex; key lost if hardware changes |
| **User Config** | 1. Close EA App 2. Edit `user_*.ini` with `user.downloadinplacedir` 3. Restart | Per-user default path setting |
| **Reinstall Flow** | 1. Start installing game 2. Cancel when download starts 3. Start again → EA App validates existing files | Community workaround |

#### EA App Missing Game Detection
- **Registered but missing**: Game in encrypted IS file (or registry) but `baseInstallPath`/`Install Dir` doesn't exist
- **Repair option**: Start download → immediately cancel → "Repair" button appears → validates existing files
- **Status codes** in decrypted IS: `installStatus`: 0=not installed, 3=installed, 5=unknown

#### EA App Background Service
- **Process**: `EABackgroundService.exe` — manages downloads, updates, activation
- **IPC**: Internal IPC server for communication (visible in logs)
- **No public API**: EA App does not expose a documented API for third-party game registration
- **Offline support**: Up to 30 days offline after online verification; requires at least one online launch

#### Missing Game Detection
- Check `baseInstallPath` in decrypted IS file
- If path doesn't exist → "registered but missing"
- Check `detailedState.installStatus`: 0=not installed, 3=installed, 5=unknown

---

### Origin (Legacy) Integration

#### Detection Signals
| Signal | Location | Format |
|--------|----------|--------|
| **Primary** | `C:\ProgramData\Origin\LocalContent\` | `.mfst` files with HTTP query strings |
| **Fallback** | Registry keys | Similar to EA Desktop |

#### Manifest Format (`.mfst` files)
Each `.mfst` file contains an HTTP query string:
```
?id=Origin.OFR.50.0001456&dipInstallPath=C%3a%5cGames%5cTitanfall2
```
- Case-insensitive key lookup required (some files have duplicate keys)
- DLC/Addons may have manifests without installation path
- Steam-installed games have IDs ending in `@steam`

#### Re-Registration After Move
1. Update `dipInstallPath` value in `.mfst` file
2. URL-decode the path when parsing
3. Restart Origin/EA App

---

### Steam Integration

#### Detection Signals
| Signal | Location | Format |
|--------|----------|--------|
| **Master Index** | `<SteamPath>\steamapps\libraryfolders.vdf` | VDF format |
| **Per-Game Manifest** | `<LibraryPath>\steamapps\appmanifest_<AppID>.acf` | VDF format |
| **Default Library** | `<SteamPath>\steamapps\` | Always included |

#### VDF Format Structure
```
"libraryfolders"
{
  "0"
  {
    "path"  "C:\\Program Files (x86)\\Steam"
    "contentid"  "1616900521946793171"
    "apps"
    {
      "228980"  "404262992"
    }
  }
  "1"
  {
    "path"  "M:\\SteamLibrary"
    "apps"
    {
      "292030"  "61513260285"
    }
  }
}
```

#### ACF (AppManifest) Format
```
"AppState"
{
  "appid"    "292030"
  "name"    "The Witcher 3"
  "installdir"    "The Witcher 3"
  "StateFlags"    "4"
  "LastUpdated"    "1669051495"
  "SizeOnDisk"    "95565268921"
  "buildid"    "8551212"
  "InstalledDepots"
  {
    "782332"
    {
      "manifest"    "6157149951972263437"
      "size"    "492528532"
    }
  }
}
```
- `installdir` is the folder name relative to `steamapps/common/`
- Full path = `<LibraryPath>\steamapps\common\<installdir>`

#### Re-Registration After Move
1. **Same library**: Move folder → update `installdir` in ACF if folder name changes
2. **Different library**: Move folder to new library → remove from old libraryfolders.vdf `apps` block → add to new libraryfolders.vdf `apps` block
3. Create junction at original location (optional, for launcher compatibility)
4. Restart Steam

#### Missing Game Detection
- Check if `installdir` folder exists in `steamapps\common\`
- Check `StateFlags`: 4=fully installed, 2=needs update, 1=uninstalled
- ACF exists but folder missing → "registered but missing"

---

### Epic Games Store Integration

#### Detection Signals
| Signal | Location | Format |
|--------|----------|--------|
| **Primary** | `C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests\` | `.item` JSON files |
| **Fallback Registry** | `HKCU\Software\Epic Games\EOS` → `ModSdkMetadataDir` | Alternative manifests path |

#### Manifest Format (`.item` JSON)
```json
{
  "CatalogItemId": "b8538c739273426aa35a98220e258d55",
  "AppName": "UnrealTournamentDev",
  "DisplayName": "Unreal Tournament",
  "InstallLocation": "M:\\Games\\EGS\\UnrealTournament",
  "ManifestLocation": "M:\\Games\\EGS\\UnrealTournament/.egstore",
  "LaunchExecutable": "Engine/Binaries/Win64/UE4-Win64-Shipping.exe",
  "LaunchCommand": "UnrealTournament",
  "InstallSize": 20771500286,
  "bIsApplication": true,
  "bIsManaged": false
}
```

#### Re-Registration After Move
1. Update `InstallLocation` field in `.item` JSON file
2. Update `ManifestLocation` if `.egstore` folder moved too
3. Restart Epic Games Launcher (or trigger metadata refresh)
4. **No manifest regeneration tool exists** — reinstall required if `.item` file lost

#### Missing Manifest Detection
- Game has `.egsstore` folder but no `.item` in `ProgramData\Epic\...`
- **Cannot regenerate** — Epic provides no tool for this
- Options: reinstall game, or manually create `.item` from game folder metadata

#### Error Codes
- IS-0007/IS-0009: "Manifest failed to load" — corrupted or missing manifest

---

### Ubisoft Connect Integration

#### Detection Signals
| Signal | Location | Format |
|--------|----------|--------|
| **Primary** | Registry `HKLM\SOFTWARE\Ubisoft\Launcher\Installs` | Registry keys per game |
| **Cache** | Ubisoft data folder `cache\*.json` | JSON cache files |

#### Registry Structure
```
HKLM\SOFTWARE\Ubisoft\Launcher\Installs\
  <GameID>  (e.g., "1")
    InstallDir  = "C:\\Games\\GameName"
    Version     = "1.0.0.0"
    Language    = "en"
```

#### Dynamic IDs
Ubisoft uses `CLUB_APPID` and `CLUB_GENOME_ID` fetched at runtime from Ubisoft's API (not hardcoded). These change occasionally and require plugin updates.

#### Re-Registration After Move
1. Update `InstallDir` value in registry key
2. Update Ubisoft Connect cache if present
3. Restart Ubisoft Connect

#### Missing Game Detection
- Registry entry exists but `InstallDir` path doesn't exist
- Cache file exists but referenced folder missing

---

### Standalone Games Integration

#### Detection Signals
| Signal | Location | Format |
|--------|----------|--------|
| **Primary** | User-configured library folders | File system scan |
| **Markers** | `.egsstore` folder, `goggame-*.info`, `steam_appid.txt` | File system markers |

#### Detection Heuristics
- Scan folders for `.exe` files
- Check for launcher-specific markers:
  - `steam_api64.dll` / `steam_api.dll` → Steam game
  - `.egsstore` subfolder → Epic game
  - `goggame-*.info` → GOG game
  - `__Installer\installerdata.xml` → EA game

#### Re-Registration After Move
- Standalone games have no central registry
- GamingCommander tracks in `games.json` database
- Update `ExecutablePath` in database after move

---

### Missing Game Detection (Universal)

For all launchers, detect "registered but missing" games:

| Launcher | Registration Source | Missing Detection |
|----------|---------------------|-------------------|
| Steam | ACF files in `steamapps\` | Folder not in `steamapps\common\<installdir>` |
| Epic | `.item` in `ProgramData\Epic\` | `InstallLocation` path doesn't exist |
| GOG | Registry `HKLM\GOG.com\Games` | `gameName` path doesn't exist |
| EA Desktop | Encrypted IS file or registry | `baseInstallPath` doesn't exist |
| Origin | `.mfst` in `ProgramData\Origin\` | `dipInstallPath` doesn't exist |
| Ubisoft | Registry `HKLM\Ubisoft\Launcher` | `InstallDir` path doesn't exist |

---

### Game Launch Implementation

| Launcher | Launch Method | URI/Command |
|----------|---------------|-------------|
| Steam | `steam://run/<appid>` | `Process.Start("steam://run/292030")` |
| Epic | Launch via EGL process | Start `EpicGamesLauncher.exe` with game ID |
| GOG | Launch via Galaxy | Use Galaxy API or direct exe |
| EA Desktop | Launch via EA App | Start game exe directly or via EA Desktop |
| Ubisoft | `uplay://launch/<gameid>` | `Process.Start("uplay://launch/1")` |
| Standalone | Direct exe launch | `Process.Start(exePath, args)` |

---

## Implementation Task List

### Phase 1: Store Auto-Detection (F2 Enhancement)

- [ ] **T001**: Create Python script to detect installed game stores via Windows Registry
  - Query HKLM\SOFTWARE keys for Steam, Epic, GOG, EA, Ubisoft
  - Return list of detected stores with install paths
- [ ] **T002**: Integrate store detection into F2 Setup wizard
  - Show detected stores with checkboxes
  - User confirms which to add
- [ ] **T003**: Auto-scan detected stores on first add
  - For each confirmed store, trigger full scan
  - Populate games.json entries

### Phase 2: Steam Integration (Priority)

- [ ] **T010**: Create Python script to parse Steam ACF files
  - Parse appmanifest_*.acf files
  - Extract: appid, installdir, name, LastUpdated
- [ ] **T011**: Create Python script to find all Steam libraries
  - Parse libraryfolders.vdf
  - Build list of all steamapps\ paths
- [ ] **T012**: Create Python script to detect all Steam games
  - Scan all libraryfolders
  - Match ACF files to folders in common\
  - Return game list with metadata
- [ ] **T013**: Detect "Missing Folder" (ACF without folder)
  - Get ACF list, check each installdir exists
  - Mark orphaned ACF entries
- [ ] **T014**: Detect "Missing ACF" (folder without ACF)
  - Get common\ folders, check ACF exists for each
  - Mark orphaned folders
- [ ] **T015**: Generate new ACF from game folder metadata
  - Input: folder name, game ID
  - Output: valid appmanifest_<id>.acf file
- [ ] **T016**: C# wrapper to invoke Python Steam detection
  - GamingCommander.Core adds SteamLibraryDetector class

### Phase 3: Epic Integration

- [ ] **T020**: Create Python script to parse Epic .item manifests
  - Parse JSON in ProgramData\Epic\EpicGamesLauncher\Data\Manifests\
  - Extract: AppName, InstallLocation, LaunchExecutable, etc.
- [ ] **T021**: Create Python script to detect all Epic games
  - Enumerate manifests, resolve install paths
  - Return game list
- [ ] **T022**: Create Python script to update Epic manifest
  - Input: game ID, new InstallLocation
  - Modify .item JSON file
- [ ] **T023**: Detect "Missing Manifest" (folder without .item)
  - Check for .egsstore folders without manifests

### Phase 4: GOG Integration

- [ ] **T030**: Create Python script to query GOG registry
  - Query HKLM\SOFTWARE\GOG.com\Games
  - Return game list with InstallDir paths
- [ ] **T031**: Create Python script to update GOG registry
  - Input: game name, new InstallDir
  - Write to registry

### Phase 5: EA App Integration

- [ ] **T040**: Create Python script to query EA registry
  - Query HKLM\SOFTWARE\EA Games\Electronic Arts
  - Query HKLM\SOFTWARE\<Dev>\<Game> for each installed game
- [ ] **T041**: Create Python script to decrypt EA IS file
  - Locate: ProgramData\EA Desktop\ts\*.isdat
  - Decrypt using AES-256-CBC with machine-specific key
  - Extract: baseInstallPath, game ID
- [ ] **T042**: Create Python script to re-encrypt EA IS file
  - Modify extracted data, re-encrypt in place

### Phase 6: Ubisoft Integration

- [ ] **T050**: Create Python script to query Ubisoft registry
  - Query HKLM\SOFTWARE\Ubisoft\Launcher\Installs
  - Return game list with InstallDir paths
- [ ] **T051**: Create Python script to update Ubisoft registry
  - Input: game ID, new InstallDir
  - Write to registry

### Phase 7: Virtual File System

- [ ] **T060**: Implement Steam virtual folder view in UI
  - Show \steam\ as virtual root containing:
    - \common\ (actual game folders)
    - \manifests\ (ACF files as readable text)
- [ ] **T061**: Show "Missing Folder" indicator
  - Display for orphan ACF (register exists, folder missing)
- [ ] **T062**: Show "Missing ACF" indicator
  - Display for orphan folder (folder exists, ACF missing)
- [ ] **T063**: Implement Epic/GOG virtual views

### Phase 8: Game Move (F4)

- [ ] **T070**: Create Python script to move Steam game
  - Copy folder to new location
  - Update ACF installdir
  - Update libraryfolders.vdf if needed
- [ ] **T071**: Create Python script to move Epic game
  - Copy folder to new location
  - Update .item InstallLocation
- [ ] **T072**: Create Python script to move GOG game
  - Copy folder to new location
  - Update registry InstallDir
- [ ] **T073**: Create Python script to move EA game
  - Copy folder to new location
  - Decrypt IS, update baseInstallPath, re-encrypt
- [ ] **T074**: Create Python script to move Ubisoft game
  - Copy folder to new location
  - Update registry InstallDir
- [ ] **T075**: Add dry-run mode to all move scripts
  - Show what would happen without executing
- [ ] **T076**: Add rollback capability
  - Log original state
  - Allow reversal within session

### Phase 9: Fix Installation (F7)

- [ ] **T080**: Implement "Fix Missing Folder"
  - Create placeholder or prompt reinstall guide
- [ ] **T081**: Implement "Fix Missing ACF"
  - Generate new ACF from folder data
- [ ] **T082**: Implement "Fix Broken Path"
  - Prompt user to locate executable
- [ ] **T083**: Implement "Fix Corrupt Manifest"
  - Attempt regenerate or prompt reinstall

### Phase 10: Game Details & Metadata (F6)

- [ ] **T090**: Create data/game-metadata.json schema
  - Keyed by game ID or title hash
  - Cache fields from PCGamingWiki
- [ ] **T091**: Create Python script to query PCGamingWiki
  - Use search API to find game article
  - Parse: genre, developer, publisher, release date
- [ ] **T092**: Implement metadata cache lookup
  - Check local cache first
  - Query API if not cached
- [ ] **T093**: Display metadata in details pane

### Phase 11: Python Test Scripts

- [ ] **T100**: Create tools/detect_stores.py
  - Detect all installed game stores via Registry
- [ ] **T101**: Create tools/detect_steam_games.py
  - Parse ACF files, enumerate Steam games
- [ ] **T102**: Create tools/detect_epic_games.py
  - Parse .item manifests
- [ ] **T103**: Create tools/detect_gog_games.py
  - Query GOG registry
- [ ] **T104**: Create tools/detect_ea_games.py
  - Decrypt IS files, query registry
- [ ] **T105**: Create tools/detect_ubisoft_games.py
  - Query Ubisoft registry
- [ ] **T106**: Create tools/move_steam_game.py
  - Move + update Steam registration
- [ ] **T107**: Create tools/move_epic_game.py
  - Move + update Epic manifest
- [ ] **T108**: Create tools/move_gog_game.py
  - Move + update GOG registry
- [ ] **T109**: Create tools/move_ea_game.py
  - Move + update EA IS file
- [ ] **T110**: Create tools/move_ubisoft_game.py
  - Move + update Ubisoft registry
- [ ] **T111**: Create tools/generate_steam_acf.py
  - Generate appmanifest from folder data
- [ ] **T112**: Create tools/patch_steam_acf.py
  - Patch installdir in existing ACF
- [ ] **T113**: Create tools/decrypt_ea_is.py
  - Decrypt and extract EA IS data
- [ ] **T114**: Create tools/encrypt_ea_is.py
  - Re-encrypt modified EA IS data

### Phase TEST: Game Detection Test (First Python Feature)

| Feature | Status | Description |
|---------|--------|-------------|
| **detect_folder.py** | 🔜 Planned | Input: folder path, Output: GameFolder, GameExe, StoreType |
| **Game Name Detection** | 🔜 Planned | Resolve proper game name from manifest/store data |
| **PCGamingWiki Lookup** | 🔜 Planned | Query PCGW API for game metadata and URL |

#### detect_folder.py - Required Output
```
Input:  python tools/detect_folder.py "Y:\Games\ACValhalla"

Output (JSON):
{
  "input_path": "Y:\\Games\\ACValhalla",
  "game_folder": "ACValhalla",
  "game_exe": "ACValhalla.exe",
  "store_type": "Steam",          // Steam, Epic, GOG, EA, Ubisoft, Standalone
  "confidence": "High",          // High, Medium, Low
  "signals_found": ["steam_api64.dll", "appmanifest_*.acf"],
  "game_name": "Assassin's Creed Valhalla",
  "pcgamingwiki_url": "https://www.pcgamingwiki.com/wiki/Assassin%27s_Creed_Valhalla",
  "pcgamingwiki_id": "123456"
}
```

#### Detection Logic (in priority order)

| Check | Store Type | How to Get Game Name |
|-------|-----------|-------------------|
| Look for `libraryfolders.vdf` | **Steam Library** | Parse ACF for `name` field |
| Look for `appmanifest_*.acf` | **Steam Game** | ACF `name` field |
| Look for `steam_api64.dll` | **Steam Emulator** | Check for .item or query Steam API |
| Look for `*.item` in Manifests | **Epic Game** | JSON `DisplayName` field |
| Look for `goggame-*.info` | **GOG Game** | Parse JSON for `title` |
| Look for `__Installer\installerdata.xml` | **EA Game** | Parse XML for `GameName` |
| Registry: GOG/Ubisoft | **Store** | Query registry for gameName |
| Folder scan → `.exe` files | **Standalone** | Use folder name as fallback |

#### PCGamingWiki Integration

| Method | API Endpoint |
|---------|-----------|
| **Search by title** | `opensearch` action |
| **Query by Steam AppID** | `cargoquery` with `Steam_AppID` |
| **Query by GOG ID** | `cargoquery` with `GOG_ID` |
| **Direct redirect** | `https://pcgamingwiki.com/api/appid.php?appid=<APPID>` |

**PCGW API Examples**:
```
# Find page by Steam AppID
https://www.pcgamingwiki.com/w/api.php?action=cargoquery&tables=Infobox_game&fields=Infobox_game._pageName&where=Infobox_game.Steam_AppID%20HOLDS%20%221245620%22&format=json

# OpenSearch by game name
https://www.pcgamingwiki.com/w/api.php?action=opensearch&search=Assassin%27s%20Creed%20Valhalla&format=json
```

---

### Python Script Testing

All Python scripts must be tested on REAL data before integration:
- Verify Steam library detection works on actual Steam install
- Verify ACF parsing produces correct game data
- Verify manifest updates persist correctly
- Verify game move re-registration actually works (launcher recognizes game)

---

## Quick Reference: Python Script Naming

```
tools/
  detect_stores.py          # Registry-based store detection
  detect_steam/
    parse_acf.py            # Parse single ACF
    parse_libraryfolders.py  # Parse VDF
    find_all_games.py        # Full Steam detection
  detect_epic/
    parse_manifest.py       # Parse .item
    find_all_games.py       # Full Epic detection
  detect_gog/
    query_registry.py      # GOG registry query
  detect_ea/
    decrypt_is.py         # Decrypt IS file
  detect_ubisoft/
    query_registry.py      # Ubisoft registry query
  move/
    move_steam_game.py    # Steam game move
    move_epic_game.py     # Epic game move
    move_gog_game.py     # GOG game move
    move_ea_game.py      # EA game move
    move_ubisoft_game.py # Ubisoft game move
  generate/
    generate_steam_acf.py # Generate ACF from scratch
  patch/
    patch_steam_acf.py   # Patch ACF installdir
  ea/
    decrypt_is.py       # Decrypt EA IS
    encrypt_is.py       # Re-encrypt EA IS
```

---

## Database Schema Summary

### data/settings.json
```json
{
  "LibraryRoots": [{ "Path": "Y:\\Games\\", "Type": 1 }],
  "FolderOverrides": [],
  "IsFirstRun": false
}
```

### data/games.json
```json
{
  "Roots": [{
    "RootPath": "Y:\\Games\\",
    "DefaultType": 1,
    "Games": [{
      "Id": "uuid",
      "FolderName": "GameFolder",
      "DisplayName": "Game Name",
      "GameSource": 1,
      "Override": false,
      "ExecutablePath": "Y:\\Games\\Game\\game.exe",
      "ManifestPath": "steamapps\\appmanifest_123456.acf",
      "ManifestData": {},  // Full manifest content for re-registration
      "LastScanned": "2026-...",
      "LastModified": "2026-..."
    }]
  }]
}
```

### data/game-metadata.json (NEW)
```json
{
  "games": {
    "<game-id>": {
      "title": "Game Title",
      "pcgamingwiki_id": "Game_Title",
      "genre": "Action RPG",
      "developer": "Dev",
      "publisher": "Pub",
      "release_date": "2023-11-01",
      "store_links": { "steam": "...", "gog": "..." },
      "last_updated": "2026-04-26"
    }
  }
}
```

---

*This document is the single source of truth for feature status tracking.*
*Update after completing any feature, phase, or change.*