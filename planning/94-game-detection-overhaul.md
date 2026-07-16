# Game Detection Overhaul

## Status: IMPLEMENTATION (Phase 1 ✅, Phase 2 ✅)

Two-tier architecture: Steam gets its own dedicated scanner (structural path detection + ACF cross-referencing), all other
platforms use the generic signal-based FolderScanner.

---

## Architecture

```
┌─ LibraryManager ──────────────────────────────────────────┐
│  Selects scanner based on root type:                      │
│                                                            │
│  GameSourceKind.Steam      → SteamLibraryScanner           │
│  GameSourceKind.Standalone → FolderScanner (generic)       │
│  Others (GOG, Epic, etc.) → FolderScanner (generic)        │
│                                                            │
│  Both scanners return `IReadOnlyList<GameEntry>`            │
└────────────────────────────────────────────────────────────┘
```

---

## SteamLibraryScanner — Dedicated Steam Detection

### Why a separate scanner?

Steam detection is fundamentally different from other platforms:

1. **Structural path is definitive**: `{library}/steamapps/common/{GameName}/` is the fixed Steam path pattern. If a game folder is at this path, it IS a Steam game — no marker files needed.

2. **ACF files provide authoritative metadata**: `appmanifest_<appid>.acf` in `steamapps/` contains `appid`, `name`, `installdir`, `StateFlags`, etc.

3. **Cross-library ACF detection**: A game folder may exist in one library while its ACF is in another (user moved the folder without updating Steam). The scanner must check ALL configured Steam libraries.

4. **Orphan detection**: Game in `steamapps/common/` with no matching ACF anywhere = orphaned.

### Input: Steam library root paths

From `AppConfig.LibraryRoots`, collect all roots with `DefaultType == GameSourceKind.Steam`:

```
D:\SteamLibrary
E:\SteamLibrary
P:\Program Files\Valve\Steam\SteamLibrary
```

Each library has the structure:
```
{library}/
  steamapps/
    libraryfolders.vdf       ← enumerates ALL Steam library paths
    appmanifest_<appid>.acf  ← per-game manifest
    common/
      <GameName>/            ← game folder (matches .acf installdir)
```

### Detection algorithm

```
for each library in configuredSteamLibraries:
    libraryfoldersPath = library/steamapps/libraryfolders.vdf
    allSteamPaths = parseLibraryFolders(libraryfoldersPath)
                    UNION (all other configured libraries from config)
    
    // Collect all ACFs from ALL libraries' steamapps/
    allAcfs = {}  // map: installdir → acfInfo
    for each path in allSteamPaths:
        for each acfFile in path/steamapps/appmanifest_*.acf:
            installdir = parseInstalldir(acfFile)
            allAcfs[installdir] = {
                library: path,
                acfPath: acfFile,
                appid: parseAppid(acfFile),
                name: parseName(acfFile),
                stateFlags: parseStateFlags(acfFile)
            }

    // Scan common/ folders in each library
    for each path in allSteamPaths:
        commonDir = path/steamapps/common
        for each gameFolder in commonDir:
            if allAcfs contains gameFolder.name:
                acf = allAcfs[gameFolder.name]
                if acf.library == path:
                    status = "Installed"    // normal: game + ACF in same library
                else:
                    status = "Moved"        // game folder in this library, ACF in another
                // Create GameEntry with ACF metadata
            else:
                status = "Orphaned"         // no ACF found anywhere
                // Create GameEntry with folder name only
```

### Status field

Stored in `GameEntry.Extra` dictionary under key `"SteamStatus"`:

| Status | Meaning | Extra entries |
|--------|---------|---------------|
| `Installed` | Game + ACF in same library | `SteamAppId`, `AcfLibraryPath`, `AcfSizeOnDisk`, `AcfBuildId`, `AcfStateFlags` |
| `Moved` | Game folder in this library, ACF in another | Same as Installed |
| `Orphaned` | No ACF found anywhere | `SteamAppId` = empty |

Launch behavior is determined by `CmdlineArgs`:
- **Installed/Moved**: `CmdlineArgs = "steam://rungameid/{appid}"` → UI launches via Steam URI
- **Orphaned**: `CmdlineArgs = ""` → UI falls through to direct executable launch

### ACF cross-referencing across libraries

Example: User has 3 libraries:
- `D:\SteamLibrary\steamapps\common\GameA\` — game folder exists
- `P:\...\SteamLibrary\steamapps\appmanifest_12345.acf` — ACF says `installdir = "GameA"`

This means the game folder was moved from P: to D: but the ACF wasn't updated. The scanner detects this by:
1. Finding `GameA` folder in `D:\SteamLibrary\steamapps\common\`
2. Looking up `"GameA"` in the allAcfs map
3. Finding the ACF in `P:\...SteamLibrary\steamapps\`
4. Setting status = `Moved` and storing the ACF's library path

### File changes

| File | Change |
|------|--------|
| `App/Services/SteamLibraryScanner.cs` | **New** — dedicated Steam scanner |
| `Core/Models/GameSourceKind.cs` | Already has `Steam` |
| `Core/Models/GameEntry.cs` | No change needed; use `Extra` dict or new field |
| `App/Services/LibraryManager.cs` | Route Steam roots to SteamLibraryScanner |
| `App/ViewModels/ShellViewModel.cs` | Display SteamStatus in details pane |
| `App/MainWindow.axaml` | Add Steam status indicator |

---

## FolderScanner (Generic) — Already Implemented

The generic scanner handles all non-Steam platforms. Already rewired in Phase 1:

### Pass 1 — Root-Level Signals (Priority Order)

| # | Platform | Signal |
|---|----------|--------|
| 1 | GOG | `goggame*` files at root |
| 2 | EA | `__Installer/` directory at root |
| 3 | Ubisoft Emu | `uplay_loader*` + INI with Username=/AccountId= |
| 4 | Ubisoft | `uplay_install.manifest`, `uplay_r*_loader*.dll` |
| 5 | Epic | `.egstore/` or `.egsstore/` directory |
| 6 | Blizzard | `.battle.net/` directory |
| 7 | Xbox | `default-metadata.json` |
| 8 | Rockstar | `title.rgl` |
| 9 | Steam | `steam_appid.txt` (fallback for standalone Steam installs) |
| 10 | Steam Emu | `steam_api64.dll` / `steam_api.dll` at root |

### Pass 2 — Deep Fallback Signals

| # | Signal | Checks |
|---|--------|--------|
| 1 | Steam Emu deep | `steam_emu.ini` (root, child, UE Steamworks path) |
| 2 | Ubisoft legacy | `UbiStats.dll` in root or child |
| 3 | Standalone (Unreal) | `Engine/` + `*/Binaries/Win64/*.exe` |
| 4 | Standalone (root exe) | Non-noise `.exe` at root |
| 5 | Standalone (root lnk) | `.lnk` shortcut at root |

### Pass 3 — Container Detection

Folder with no signals, but immediate child has Pass 1 signals → promote child.

### Noise filtering

320 exe patterns (23 tiers) from `data/blacklist.json`. Deep executable search
checks root → child dirs → `Binaries/Win64/` → `Binaries/WinGDK/`.

---

## Implementation Order

### Phase 1 ✅ — Generic FolderScanner + Signal overhaul
- `GameSourceKind` enum: 4 new values (BattleNet, Xbox, Rockstar, SteamEmu) ✅
- `FolderScanner.DetectType()`: priority-ordered 10-signal chain ✅
- `FolderScanner` deep executable discovery + scoring ✅
- `FolderScanner` container detection ✅
- All UI switch statements updated (GameSetupWindow, WizardVM, LibrarySetupVM) ✅
- Tests updated and passing (17/17) ✅

### Phase 2 ✅ — SteamLibraryScanner (implemented)
- Create `SteamLibraryScanner` class ✅
- Parse `libraryfolders.vdf` for all Steam library paths ✅
- Parse `appmanifest_*.acf` for game metadata ✅
- Cross-reference ACFs across all configured libraries ✅
- Detect orphaned/moved games ✅
- Wire into `LibraryManager` ✅
- Display Steam status in UI (stored in GameEntry.Extra) ✅
- GameEntry launched via `steam://rungameid/{appid}` when CmdlineArgs is set ✅

### Phase 3 🔲 — Epic .item manifest support
- Future: read Epic `.item` manifests for metadata enrichment
- Handle missing .item files (detect via `.egstore/` alone)

### Phase 4 🔲 — PE metadata background enrichment
- Future: extract FileDescription/ProductName from executables
- Run as background enrichment pass, not inline during scan
