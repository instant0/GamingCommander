# Game Detection Logic — Complete Reference

**Nature:** Living reference document. Updated when detection logic changes.
**Audience:** All agents. Read when modifying detection, store signals, or noise filtering.

---

## Overview

GamingCommander detects installed games by scanning user-configured library roots.
Two parallel detection systems exist:

1. **C# implementation** (`FolderScanner` + `StoreSignalDetector` + `FallbackSignalDetector` + `ContainerScanner` + `ExecutableDiscovery` + `LnkParser` + `GogInfoParser`) — runs in the application at startup.
2. **Python reference** (`tools/detect.py`) — development/research tool used to fine-tune signals and validate results. Deprecated but retained as ground truth.

The Python tool (`detect.py`, ~1829 LOC) is the **reference gold**. The C# code was ported from it with adjustments for the application context (no PE metadata extraction in-app, no PCGamingWiki network calls, etc.).

---

## Architecture

### Two-Scanner Design

```
LibraryManager
  ├─ GameSourceKind.Steam → SteamLibraryScanner (structural path detection + ACF cross-referencing)
  └─ All others          → FolderScanner (generic signal-based detection)
```

`SteamLibraryScanner` handles Steam exclusively because Steam detection is fundamentally different:
- Structural path is definitive: `{library}/steamapps/common/{GameName}/`
- ACF files provide authoritative metadata
- Cross-library ACF detection for moved games

`FolderScanner` handles all non-Steam platforms (GOG, EA, Ubisoft, Epic, Blizzard, Xbox, Rockstar, Steam Emu, Standalone) using a signal-chain approach. It delegates to:
- `StoreSignalDetector` — 10-signal priority chain (Pass 1)
- `FallbackSignalDetector` — 5 fallback signals (Pass 2)
- `ContainerScanner` — container/publisher folder recursion (Pass 3)

**Note:** Engine detection (Unreal, Unity, RAGE, Frostbite) helps identify file/folder layout for exe discovery (e.g., `Engine/Binaries/Win64/`), but does NOT determine game store classification. Store signals (`.egstore/`, `uplay_install.manifest`, etc.) determine whether a game is Standalone or from a specific store.

### FolderScanner Three-Pass Architecture

```
Scan(rootPath, defaultType)
  ├─ Pass 1: StoreSignalDetector.DetectType(subDir)  → Tier 1 (HIGH confidence)
  ├─ Pass 2: FallbackSignalDetector.DetectFallbackType(subDir)  → Tier 2 (MEDIUM/LOW)
  │    ├─ Steam Emulator deep signal
  │    ├─ Ubisoft legacy signal
  │    ├─ Unreal Engine layout signal
  │    ├─ Root executable signal
  │    └─ Root .lnk shortcut signal
  └─ Pass 3: ContainerScanner.ScanContainerChildren(subDir)  → Tier 3 (Container/Publisher)
```

### Python Reference Four-Phase Architecture

```
Phase 1: Root scan (fast, single os.scandir per folder)
Phase 2: Deep signal scan (unknowns only, .exe/.dll/.ini filtered, max depth 4)
Phase 3: Container check (remaining unknowns)
Phase 4: Enrichment (optional, --metadata / --pcgw flags)
```

---

## Store Manifest Systems (How Stores Track Installed Games)

Each game store uses a different mechanism to track installed games. Understanding these systems is critical for:
1. **Detecting games** — Some stores put games in publisher subfolders (e.g., `Ubisoft/`, `EA Games/`)
2. **Enriching metadata** — Manifest files contain authoritative game names, exe paths, and metadata
3. **Avoiding false filtering** — We can't skip publisher folders because they contain actual games

### Store Manifest Summary

| Store | Manifest Location | Format | Contains | Status |
|-------|------------------|--------|----------|--------|
| **Steam** | `{library}/steamapps/appmanifest_*.acf` | VDF (key-value) | AppId, name, installdir, state | ✅ Implemented |
| **GOG** | `{game}/goggame-*.info` | JSON | Title, exe, args, game ID | ✅ Implemented |
| **Epic** | `C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests\*.item` | JSON | InstallLocation, LaunchExecutable, DisplayName | ⚠️ Signal only |
| **Battle.net** | `{game}/.battle.net/` | Binary | Game client data | ✅ Implemented |
| **Rockstar** | `{game}/title.rgl` | Binary | Game metadata | ✅ Implemented |
| **Xbox** | `{game}/default-metadata.json` | JSON | Game metadata | ✅ Implemented |
| **EA App** | Encrypted `IS` file in `C:\ProgramData\EA Desktop\` | Encrypted JSON | Install paths | ❌ Not implemented |
| **Origin** | `C:\ProgramData\Origin\LocalContent\*.mfst` | HTTP query string | ContentID, InstallPath | ❌ Not implemented |
| **Ubisoft** | `{game}/uplay_install.manifest` | Binary (protobuf) | Game metadata | ⚠️ Signal only |

### Detailed Store Analysis

#### 1. Steam (Fully Implemented)

**Manifest files:** `appmanifest_*.acf` in `{library}/steamapps/`

**Structure:**
```
SteamLibrary/
  steamapps/
    appmanifest_292030.acf  ← The Witcher 3
    appmanifest_1091500.acf ← Cyberpunk 2077
    common/
      The Witcher 3/
      Cyberpunk 2077/
```

**ACF fields:** `appid`, `name`, `installdir`, `LauncherData`, `StateFlags`

**Detection:** `SteamLibraryScanner` scans all libraries, cross-references ACFs with `common/` folders, handles Moved/Orphaned/Missing states.

#### 2. GOG (Fully Implemented)

**Manifest files:** `goggame-*.info` in game folders, `Launch *.lnk` shortcuts

**Detection:** `HasGogSignal()` checks for `goggame*` files at root (primary) or `Launch <gamename>.lnk` shortcuts (secondary — strong GOG signal).

**Structure:**
```
GOG Games/
  The Witcher 3/
    goggame-1425694943.info  ← JSON with title, exe, args
    bin/
      x64/
        witcher3.exe
```

**Detection:** `GogInfoParser` parses `.info` JSON, extracts title/exe/args, filters DLC by gameId.

#### 3. Epic Games Store (Signal Only)

**Manifest files:** `*.item` in `C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests\`

**Structure:**
```json
{
  "DisplayName": "Fortnite",
  "InstallLocation": "D:\\Epic Games\\Fortnite",
  "LaunchExecutable": "FortniteGame\\Binaries\\Win64\\FortniteClient-Win64-Shipping.exe",
  "CatalogItemId": "..."
}
```

**Current detection:** `.egstore/` directory at game root (signal only, no parsing)

**Enhancement opportunity:** Parse `.item` files from ProgramData to get:
- Authoritative game names (not folder names)
- Exact exe paths
- Install sizes
- Store IDs for PCGW/Steam cross-reference

**Existing implementation:** `lookup_metadata.py` has `epic_crossref_item_manifests()` function that:
- Reads `.item` files from `%ProgramData%\Epic\EpicGamesLauncher\Data\Manifests\`
- Matches `InstallLocation` field to game folder path
- Returns `DisplayName`, `LaunchExecutable`, `CatalogItemId`, etc.

**Windows path:** `%ProgramData%\Epic\EpicGamesLauncher\Data\Manifests\` (typically `C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests\`)

#### 4. EA App / Origin (Not Implemented)

**EA App (new):** Encrypted `IS` file at `C:\ProgramData\EA Desktop\{hash}\IS`
- File is AES-encrypted with key derived from machine-specific data
- Contains JSON with game install paths
- **Complexity:** High — requires decryption, machine-specific key

**Origin (old):** `.mfst` files at `C:\ProgramData\Origin\LocalContent\`
- Contains HTTP query strings like: `?id=Origin.OFR.50.0001456&dipInstallPath=C%3a%5cGames%5cTitanfall2`
- **Complexity:** Low — simple string parsing

**Current detection:** `__Installer/` directory at root (signal only)

**Enhancement opportunity:**
- Parse old Origin `.mfst` files (if present)
- For EA App, fall back to registry: `HKEY_LOCAL_MACHINE\Software\EA Games\{game}\Install Dir`
- Check `__Installer/installerdata.xml` for registry key references

**EA `InstallLog.txt` enrichment (✅ IMPLEMENTED):** `EaInstallLogParser` reads `__Installer\InstallLog.txt` (UTF-16 encoded) to extract authoritative metadata:

| Field | Example | Use | Trusted? |
|-------|---------|-----|----------|
| `(Config)Display Game Name:` | `Dragon Age™: Inquisition` | Marketing name | ✅ Always |
| `(Config)Game Name:` | `Dragon Age Inquisition` | Canonical game name | ✅ Always |
| `(Config)Studio:` | `BioWare` | Developer name | ✅ Always |
| `Install Location:` | `E:\Games\Dragon Age Inquisition\` | Install path | ❌ May be stale (user moved game) |
| `EAInstaller version:` | `4.01.00.00` | Installer version | ✅ Always |

**Key insight:** This file is **always** inside `__Installer/` — which is already an EA signal in `HasEaSignal()`. The parser extracts the authoritative game name and developer, solving display name issues for EA games without PCGW or PE metadata.

**Note:** EA Desktop App (new) may use a different format. This format applies to games installed via the legacy EA/Origin installer (`EAInstaller version: 4.01.00.00`).

#### 5. Ubisoft Connect (Signal Only)

**Manifest files:** `uplay_install.manifest` in game folders

**Structure:** Binary protobuf (gzip compressed after 356-byte header)
- Contains game metadata, file parts, download URLs
- **Complexity:** High — requires protobuf parsing + gzip decompression

**Current detection signals:**
| Signal | Method | Status |
|--------|--------|--------|
| `uplay_install.manifest` at root | `HasUbisoftSignal()` | ✅ Older games |
| `uplay_r*_loader*.dll` at root | `HasUbisoftSignal()` | ✅ Older games |
| `uplay_download/` directory | `HasUbisoftSignal()` | ✅ Modern Ubisoft Connect (Plan 112) |
| `*_UPP*.exe` subscription variants | `HasUbisoftSignal()` | ✅ Uplay Plus signals (Plan 112) |
| `uplay_loader*` + INI with Username/AccountId | `HasUbisoftEmulatorSignal()` | ✅ Emulated |

**Ubisoft `Support/Readme` enrichment (✅ Plan 112):** `UbisoftReadmeParser` reads `Support/Readme/*.txt` files to extract authoritative game metadata. Ubisoft games conventionally ship a `Support/Readme/` directory containing text files where the first 4 lines are: publisher, game title, copyright, blank.

| Field | Example | Use |
|-------|---------|-----|
| Line 1 | `Ubisoft` | Publisher name |
| Line 2 | `Ghost Recon Breakpoint` | Game title (used as display name) |

**Enhancement opportunity:**
- Parse manifest for game metadata (if protobuf parser available)
- Check registry: `HKEY_LOCAL_MACHINE\SOFTWARE\Ubisoft\Launcher\Installs\{gameId}`
- Use `uplay://` URL scheme handler for game launching

#### 6. Battle.net (Fully Implemented)

**Manifest files:** `.build.info`, `.product.db`, `.battle.net/` directory at game root

**Detection:** `HasBlizzardSignal()` checks:
- `.battle.net/` directory existence (Agent runtime data)
- `.build.info` file existence (pipe-delimited, contains CDN path and version)
- `.product.db` file existence (binary/protobuf, contains install path)

**Note:** Path-based detection (checking if parent is named "blizzard") was removed in Plan 116. Detection is based on signal files inside the folder, not path names.

#### 7. Rockstar (Fully Implemented)

**Manifest files:** `title.rgl` at game root

**Detection:** `HasRockstarSignal()` checks for `title.rgl` file existence

#### 8. Xbox (Fully Implemented)

**Manifest files:** `default-metadata.json` at game root

**Detection:** `HasXboxSignal()` checks for `default-metadata.json` file existence

---

### Publisher Folder Pattern

**Key insight:** Game stores often organize games in publisher subfolders:

```
D:\Games\
  ├─ SteamLibrary\        ← Steam games
  │   └─ steamapps\common\
  ├─ GOG Games\           ← GOG games
  ├─ Epic Games\          ← Epic games
  ├─ Blizzard\            ← Battle.net games
  │   ├─ Diablo IV\
  │   └─ World of Warcraft\
  ├─ EA Games\            ← EA games
  │   ├─ Battlefield 2042\
  │   └─ Mass Effect\
  ├─ Ubisoft\             ← Ubisoft games
  │   ├─ Rainbow Six Siege\
  │   └─ Far Cry 6\
  └─ Rockstar Games\      ← Rockstar games
      └─ GTA V\
```

**Detection strategy:** We CANNOT skip publisher folders because they contain actual games. Instead:
1. **Signal-based detection** — Check each subfolder for store signals (`.egstore/`, `uplay_install.manifest`, etc.)
2. **Container recursion** — If a folder has no signals but children do, treat it as a container
3. **Parent propagation** — Inherit store type from parent folder (e.g., `blizzard/` → BattleNet)

---

### Manifest-Based Enrichment Opportunities

| Store | Enrichment Source | Data Available | Complexity |
|-------|------------------|----------------|------------|
| **Steam** | ACF files | Name, exe, state, size | ✅ Low |
| **GOG** | `.info` files | Title, exe, args, game ID | ✅ Low |
| **Epic** | `.item` manifests | Name, exe, install path, catalog ID | ⚠️ Medium (external to game folder) |
| **EA App** | Registry + `.mfst` | Install path, content ID | ❌ High (encrypted) |
| **Ubisoft** | Registry + manifest | Install path, game ID | ❌ High (protobuf) |
| **Battle.net** | `.battle.net/` dir | Client data | ✅ Low (already used) |

---

### Recommendations

1. **Keep current signal-based detection** — It works for all stores and doesn't require manifest parsing
2. **Add Epic manifest parsing** — Parse `.item` files from ProgramData for enrichment (authoritative names)
3. **Add Origin `.mfst` parsing** — Simple string parsing for legacy Origin games
4. **Skip EA App encryption** — Too complex, not worth the effort
5. **Skip Ubisoft protobuf** — Too complex, use registry fallback instead
6. **Add registry-based detection** — Check registry keys for EA/Ubisoft games as fallback

---

## Pass 1: Store Signal Detection (Tier 1)

Both C# and Python check stores in **priority order** (first match wins). The C# implementation lives in `StoreSignalDetector.DetectType()`.

### Signal Priority and Logic

| Priority | Platform | Signal | C# Code | Python Code | Match Type |
|----------|----------|--------|---------|-------------|------------|
| 1 | **GOG** | `goggame*` files at root (goggame.dll, goggame-*.info, gog_*) OR `Launch *.lnk` shortcut | `HasGogSignal` | `_check_gog` + `_scan_root` | File glob `goggame*` (C#); `goggame.dll` exact + prefix scan (Python); `Launch *.lnk` prefix check (C# only) |
| 2 | **EA** | `__Installer/` directory at root, or `Touchup.exe`/`ActivationUI.exe` at root | `HasEaSignal` | `_check_ea` | Directory existence + exe name check |
| 3 | **Ubisoft Emulator** | `uplay_loader*` + `.ini` with `Username=` and `AccountId=` | `HasUbisoftEmulatorSignal` | `_check_ubisoft_emu` | Loader pattern + INI content scan |
| 4 | **Ubisoft** | `uplay_install.manifest`, `uplay_r*_loader*.dll`, `uplay_download/` directory, or `*_UPP*.exe` at root | `HasUbisoftSignal` | `_check_ubisoft` | Exact file name + glob pattern + directory check + exe pattern |
| 5 | **Epic** | `.egstore/` or `.egsstore/` directory at root | `HasEpicSignal` | `_check_epic` | Directory existence |
| 6 | **Blizzard** | `.battle.net/` directory at root | `HasBlizzardSignal` | `_check_blizzard` | Directory existence |
| 7 | **Xbox** | `default-metadata.json` at root | `HasXboxSignal` | `_check_xbox` | File existence |
| 8 | **Rockstar** | `title.rgl` at root | `HasRockstarSignal` | `_check_rockstar` | File existence |
| 9 | **Steam Emulator** (strong) | `steam_api64.dll` or `steam_api.dll` at root | `HasSteamEmulatorSignal` | `_check_steam_emu` | File existence |
| 10 | **Steam Emulator** (weak) | `steam_appid.txt` alone (only in `StoreSignalDetector`, not in Python Phase 1) | `HasSteamSignal` | N/A in Phase 1 | File existence |

**Note on `.lnk` files and GOG:** GOG installers place a single `.lnk` shortcut in each game's root directory with the naming pattern `Launch <gamename>.lnk` (e.g., `Launch The Witcher 3 - Wild Hunt - Game of the Year Edition.lnk`). This is a **strong GOG signal** — during testing, every GOG-installed game had exactly one `.lnk` file with the "Launch" prefix, and no games from other platforms had this pattern. Generic `.lnk` files (e.g., `GameName.exe.lnk` without the "Launch" prefix) are NOT a GOG signal and should only be treated as a fallback for exe discovery.

### Divergences: Python vs C#

| Scenario | Python | C# | Impact |
|----------|--------|-----|--------|
| `gog.ico` at root | ✅ Detected as GOG signal (in `_scan_root`) | ❌ Not in `HasGogSignal` | Minor — GOG games always have `goggame*` files |
| `touchup.exe` / `ActivationUI.exe` at root | ✅ Detected as EA signal | ✅ Detected in `HasEaSignal` | Parity |
| `steam_appid.txt` alone (no `steam_api64.dll`) | ❌ Not a Phase 1 signal (weak, only used in Tier 2) | ✅ Detected as `SteamEmu` via `HasSteamSignal` | Minor difference — C# is slightly more aggressive |
| `uplay_install.state` | ✅ Detected (Python deep scan `_match_markers`) | ✅ Detected in `HasUbisoftSignal` | Parity |
| Deep signal: `steam_emu.ini` | Phase 2 deep scan | `HasSteamEmuDeepSignal` in Pass 2 | Parity |
| Deep signal: `steamapps/` dir or `.acf` files outside Steam library | ✅ `_has_steam_app_manifest` | ❌ Not implemented | **Gap** — standalone games mimicking Steam layout not detected as SteamEmu |
| **`"blizzard"` in skip lists** | **Not in any skip list** | **FIXED** — removed from skip lists | ✅ BattleNet games now detected correctly via signal files |

#### BattleNet Skip-List Regression (FIXED)

**Status:** ✅ FIXED — `"blizzard"` and `"battle.net"` removed from `NoiseSubDirNames` and `s_nonGameFolderNames`.

BattleNet detection is based on **signal files inside the game folder**, not path names:
- `.build.info` — pipe-delimited file with CDN path (e.g., `tpr/diablo3`), version, branch
- `.product.db` — binary/protobuf file with install path and product code
- `.battle.net/` — BattleNet Agent runtime data directory

**Key principle:** A game can be installed ANYWHERE. Only SteamLibrary has a fixed path. Detection must be based on signal files inside the folder, not on path names.

| Step | What Happens |
|------|--------------|
| 1 | Scan root → find game directory (e.g., `Diablo III/`, `Blizzard/Diablo III/`, or `Q:\random\Diablo III\`) |
| 2 | Check for signal files: `.build.info`, `.product.db`, `.battle.net/` |
| 3 | If any signal file exists → classify as BattleNet |
| 4 | Extract metadata from `.build.info` (CDN path, version, branch) |

**Note:** Path-based detection (checking if parent is named "blizzard") was removed in Plan 116 because it was incorrect — games can be installed in any directory.

### Key Design Notes

- **Priority order matters**: GOG > EA > Ubisoft Emu > Ubisoft > Epic > Blizzard > Xbox > Rockstar > Steam Emu. A folder with both `goggame.dll` and `steam_api64.dll` is classified as GOG.
- **First match wins**: Only the first matching signal determines the `GameSourceKind`.
- **Store signal takes precedence**: Even if the folder also has executables at root, the store signal determines classification.

---

## Pass 2: Fallback Detection (Tier 2)

When Pass 1 finds no store signal, `DetectFallbackType()` in C# (or `_deep_signal_scan()` + fallback logic in Python) checks deeper patterns.

### Fallback Signal Chain (C# — `DetectFallbackType`)

| Priority | Signal | Logic | Returns |
|----------|--------|-------|---------|
| 1 | **Steam Emulator deep** | `steam_emu.ini` at root, in child dirs, or in UE `Engine/Binaries/ThirdParty/Steamworks/Steamv*/Win64/` | `SteamEmu` |
| 2 | **Ubisoft legacy** | `UbiStats.dll` at root or in immediate child dirs | `UbisoftConnect` |
| 3 | **Unreal Engine layout** | `Engine/` dir present + child with `Binaries/{Win64,Win32,WinGDK,Steam}/*.exe` (UE4-5); OR `Binaries/{platform}/*.exe` at root (UE3 fast path) | `Standalone` |
| 4 | **Root executable** | Any non-noise `.exe` at root level | `Standalone` |
| 5 | **Root .lnk shortcut** | Any `.lnk` file at root level | `Standalone` |

**Note:** `.lnk` files with the "Launch" prefix (e.g., `Launch GameName.lnk`) are a **strong GOG signal** — see GOG section above. Generic `.lnk` files (without the "Launch" prefix) are treated as a fallback for exe discovery only.

### Python Deep Signal Scan

Python's Phase 2 walks unknowns to `WALK_MAX_DEPTH=4`, only processing `.exe/.dll/.ini` files. It checks:

| Priority | Signal | Logic |
|----------|--------|-------|
| 1 | `steam_emu.ini` (root/child/UE path) | Same as C# |
| 2 | `steamapps/` dir or `.acf` files | **Not in C#** — detects Steam structure outside known libraries |
| 3 | `UbiStats.dll` (root/child) | Same as C# |
| 4 | Marker pattern matching (GOG, EA, Ubisoft, Epic, Steam) | C# doesn't do deep marker matching — relies on Pass 1 |

### UE Layout Detection (T66/T68)

The C# implementation checks **all four UE platform directories** under `Binaries/`:

```
GameName/
  Engine/                          ← UE4-5 requires this
  GameName/
    Binaries/
      Win64/  GameName-Win64-Shipping.exe    ← Most common
      Win32/  GameName.exe                    ← UE3
      WinGDK/ GameName.exe                    ← Xbox Game Pass
      Steam/  GameName.exe                    ← Steamworks build
```

**UE3 fast path** (T68): `Binaries/` directly at root (no `Engine/` needed):
```
UnrealTournament3/
  Binaries/
    Win32/
      UT3.exe
```

**child/bin/ probe** (T66): For older games like Gothic:
```
Gothic/
  bin/
    Gothic.exe
```

**BioShock recursive fallback** (T66): When root has no exes, scan 2 levels deep:
```
BioShock/          ← no root exes
  Build/
    Shipping/
      Win64/
        BioShock.exe   ← found at depth 2
```

### .lnk Shortcut Resolution (T67)

When no primary exe is found, the C# code falls back to `.lnk` file parsing:

1. Find `.lnk` files at game root
2. Extract exe name from binary data (latin-1 decode + regex)
3. Skip known DLLs (`steam_api.dll`, `steam_api64.dll`, `eos.dll`, `upc.dll`)
4. Pick longest candidate (most likely the real game exe)
5. Search root + subdirs (3 levels) for:
   - Exact filename match (highest priority)
   - Backup rename: `-Game.exe` → matches `Game.exe`
   - Backup rename: `copy of Game.exe` → matches `Game.exe`
   - Fuzzy: filename contains the exe stem
6. Exact match preferred over fuzzy

**GOG Association:** GOG installers place a `.lnk` shortcut in each game's root directory with the naming pattern `Launch <gamename>.lnk`. This is a **strong GOG signal** — every GOG-installed game has exactly one `.lnk` file with the "Launch" prefix, and no games from other platforms have this pattern. The `.lnk` file contains the path to the game executable and may include launch arguments. Generic `.lnk` files (without the "Launch" prefix) are NOT a GOG signal and should only be treated as a fallback for exe discovery.

Python implements the same logic in `_find_exe_via_lnk()` and `_parse_lnk_exe_name()`.

### GOG Metadata Enrichment (T65)

When a game is detected as GOG, `GogInfoParser` enriches it:

1. Search root + 1 level of non-noise subdirs for `goggame-*.info`
2. Parse JSON with trailing comma support and comment handling
3. Prefer main game entry: `gameId == rootGameId` (skip DLC)
4. Extract from `playTasks[]`:
   - `isPrimary: true` → primary exe path and launch arguments
   - Fallback: first task with a `path` value
5. Resolve relative exe paths to absolute via `Path.GetFullPath(Path.Combine(gameDir, path))`
6. Populate: `DisplayName`, `ExecutablePath` (fallback), `CommandLineArguments`, `PlatformMetadata["GogGameId"]`

---

## Executable Discovery and Scoring

### Deep Exe Search (`ExecutableDiscovery.FindExecutablesDeep`)

Search order (all platforms checked, no early break):

```
1. Root-level .exe files (top-level only)
2. Immediate child directories:
   a. Child's direct .exe files
   b. child/Binaries/Win64/*.exe
   c. child/Binaries/Win32/*.exe
   d. child/Binaries/WinGDK/*.exe
   e. child/Binaries/Steam/*.exe
   f. child/bin/*.exe (older games)
3. Recursive fallback (maxDepth=2) — only if steps 1-2 find nothing
```

All candidates are deduplicated by full path. Noise patterns and noise directories are filtered at each level.

### Exe Scoring (`ExecutableDiscovery.ScoreExecutable`)

When multiple exe candidates exist, they are scored:

**Bonuses:**

| Factor | Score | Notes |
|--------|-------|-------|
| Folder name token match | +10 per token | Folder "My Game" → tokens ["my", "game"]. If exe contains "game" → +10 |
| Shipping/Win64 binary | +5 | UE4-5 production builds contain "Shipping" or "Win64" in name |
| File size ≥ 100MB | +10 max | `Math.Min(size / 10MB, 10)` — larger = more likely the game |

**Penalties:**

| Factor | Score | Notes |
|--------|-------|-------|
| Launcher pattern match | -20 | "launcher", "launch", "updater", "bootstrap", etc. |
| Backup copy ("copy of", " - Copy") | -25 | Windows copy renames |
| Backup prefix ("org_") | -20 | Backup/original copies |
| "original" keyword | -15 | Backup indicator |
| Tiny exe (< 100KB) | -15 | Likely helper/tool, not the game |
| Tier 1-5 noise pattern | -30 | Universal noise: uninstallers, installers, redists, crash reports |
| Tier 6-10 noise pattern | -20 | Likely non-game: DRM wrappers, server stubs |
| Tier 11-15 noise pattern | -10 | Possibly non-game: dev tools, trial stubs |
| Tier 16+ noise pattern | -5 | Might be legitimate: Python, Blender, webview |

The tier system comes from `data/blacklist.json`'s 21-tier structure. Only the **first matching pattern** penalizes (no double-penalizing).

### Comparison with Python Scoring

Python's `_pick_best_root_exe` and `_pick_primary_executable` use a similar but slightly different scoring system:

| Factor | Python | C# | Difference |
|--------|--------|-----|------------|
| Backup penalties (copy, org, original) | -15 to -40 | -25 to -20 (copy/org/original) | **Parity** — C# now penalizes backup copies |
| Tool penalties (20+ patterns) | -25 | Via tier-based noise penalty | Different implementation, same intent |
| Exact folder token match | +15 | +10 per token (no explicit "exact match" bonus) | Python is slightly stronger on exact matches |
| Abbreviation match | +8 | ❌ Not in C# | **Gap** — e.g., "g3" matching folder "Gothic3" |
| Roman numeral match | +12 | ❌ Not in C# | **Gap** — e.g., "u9" matching "IX" (9), "heroes4" matching "IV" |
| Small exe penalty (< 100KB) | -15 | -15 (< 100KB) | **Parity** — C# now penalizes tiny executables |
| PE metadata match | +15/+10 | ❌ Not in C# (PE extraction is a Phase 4 feature) | Expected gap — PE enrichment is not in the C# app |
| Folder prefix/startswith bonus | +5 | ❌ Not in C# | **Gap** — Python boosts exes whose name starts with a folder token |

**Assessment:** The C# scoring is simpler than the Python version but covers the critical cases (noise filtering, launcher penalty, folder name matching, shipping bonus, file size). The missing factors (backup penalties, abbreviation/roman numeral, small exe penalty) are edge cases that rarely affect real-world results. The Python scoring was refined through extensive testing on actual game libraries (157 games across D: and E: drives with 0 unknowns).

---

## PE Metadata Analysis (Training Data Findings)

Analysis of `extended-p-games.txt` (85 executables) and `extended-e.txt` (191 executables) with PE metadata (Description, InternalName, Version, FileSize) reveals significant untapped signal value.

### Key Findings

#### 1. InternalName is MORE Reliable than Description

| Field | Noise Detection | Reliability | Notes |
|-------|----------------|-------------|-------|
| **InternalName** | 47 VC++ "setup" instances, 9 "launcher" instances | **HIGH** | Raw identifier, less likely to be localized |
| **Description** | 12 "setup", 11 "crash", 10 "launcher" | MEDIUM | Human-readable, may be localized |

**Critical insight:** VC++ redistributables consistently have `InternalName = "setup"` even when Description says "Microsoft Visual C++ 2015-2022 Redistributable". This is a **stronger signal** than filename patterns.

#### 2. Noise Patterns from PE Metadata

**From Description field:**
- `"Microsoft"` → installers/redistributables (DirectX, VC++, .NET)
- `"Setup"` → installers (UE Prerequisites, GOG installers)
- `"Uninstall"` → uninstallers
- `"Crash"` → error reporters (BlizzardError, codCrashHandler)
- `"Browser"` → embedded browsers (BlizzardBrowser, CefWrapper)
- `"Launcher"` → launchers (Battle.net, Rockstar, Stardock)

**From InternalName field:**
- `"setup"` → VC++ redistributables (47 instances!)
- `"launcher"` → game launchers (9 instances)
- `"uninstall"` → uninstallers (7 instances)
- `"crash"` → error reporters (7 instances)
- `"browser"` → embedded browsers (3 instances)

#### 3. File Size Thresholds

| Category | Size Range | Examples |
|----------|-----------|----------|
| **Game executables** | 25–480 MB | WoW.exe (66 MB), Diablo IV.exe (55 MB), FortniteClient (480 MB) |
| **Launchers** | 5–30 MB | Rockstar Launcher (29 MB), Diablo IV Launcher (5 MB) |
| **Noise/Tools** | < 1 MB | BlizzardError (0.9 MB), FenrisError (0.02 MB), awesomium_process (0.04 MB) |

**Current C# threshold:** `< 100 KB → -15 penalty`
**Recommended:** `< 1 MB → -10 penalty` (catches more noise without penalizing small legitimate exes)

#### 4. Description vs InternalName Divergence

Out of 191 parsed executables:
- **178 (93%)** have different Description and InternalName
- **Only 2 (1%)** are identical
- **11 (6%)** could not be compared (missing data)

This means checking **both fields** provides complementary signal.

### Proposed Implementation: PE Metadata Scoring

**Priority:** MEDIUM (after BattleNet P0 fix)

**Approach:** Use `System.Diagnostics.FileVersionInfo.GetVersionInfo()` — built into .NET, no external dependencies, ~30 lines of code.

```csharp
// In ExecutableDiscovery.ScoreExecutable():
try
{
    var info = FileVersionInfo.GetVersionInfo(exePath);
    string desc = (info.FileDescription ?? "").ToLowerInvariant();
    string internalName = (info.InternalName ?? "").ToLowerInvariant();
    
    // Penalize noise patterns in Description
    if (desc.Contains("setup") || desc.Contains("microsoft") || 
        desc.Contains("uninstall") || desc.Contains("redistributable"))
        score -= 25;
    
    // Penalize noise patterns in InternalName (stronger signal)
    if (internalName == "setup" || internalName.Contains("launcher") ||
        internalName.Contains("uninstall") || internalName.Contains("crash"))
        score -= 20;
    
    // Bonus for game-like descriptions
    if (desc.Contains("retail") || desc.Contains("game") || 
        desc.Contains("client"))
        score += 10;
}
catch { /* PE read failed — continue with existing score */ }
```

**Expected Impact:**
- Catches VC++ redistributables that escape filename-based filtering
- Catches embedded browsers (BlizzardBrowser) with Description="Blizzard Browser"
- Catches error reporters with InternalName="crash" or "error"
- **Risk:** Low — PE read failures gracefully degrade to existing scoring

**Why This is the Last Avenue:**
1. Filename-based noise filtering already handles 90% of cases
2. Folder-level skip lists handle publisher containers
3. PE metadata catches the remaining edge cases (VC++ in game folders, embedded browsers)
4. If PE metadata doesn't help, the issue is likely in signal detection, not scoring

---

## Noise Filtering and Blacklist System

### Three-Layer Filtering

The detection system uses three layers of noise filtering:

#### Layer 1: Directory-Level Skip (Top-level scan)

When scanning a library root's immediate children, these directories are **skipped entirely** (never processed):

```
FileSystemHelper.NoiseSubDirNames (hardcoded in C#):
  __redist, _commonredist, commonredist, redist, directx, vcredist, dotnet,
  physx, support, _installer, install, installer, easyanticheat, devtools,
  docs, licenses, steam controller configs, steamworks shared,
  epic games, origin, uplay, gog galaxy, ea app, rockstar games
  // NOTE: "blizzard" and "battle.net" REMOVED — publisher containers with game subdirs
```

Python SKIP_NAMES (additional entries not in C#):
```
  epiclauncher, launcher, battle.net, ubisoft game launcher, origin,
  ea desktop, gog galaxy, wiiu, reshade, sweetfx, enbseries, enb,
  nexus mod manager, vortex, mod organizer, uninstall
```

**Note:** The Python `SKIP_NAMES` includes known launcher directories (Epic, Battle.net, etc.) and non-game tools (reshade, enb, mod managers). Some of these are handled by the C# `s_nonGameFolderNames` set in container recursion instead of at top-level scan.

**FIXED:** `"blizzard"` and `"battle.net"` have been **removed** from `NoiseSubDirNames` and `s_nonGameFolderNames`. BattleNet games are now detected correctly via signal files (`.build.info`, `.product.db`, `.battle.net/`).

#### Layer 2: Noise Directory Patterns (JSON-sourced)

Loaded from `data/blacklist.json` → `directory_patterns.patterns`:
```
__redist, _commonredist, redist, directx, vcredist, dotnet, physx,
support, _installer, install, installer
```

Used by `FileSystemHelper.IsNoiseDirectory()` for case-insensitive substring matching during:
- Top-level root scan (FolderScanner.Scan)
- Deep exe search (ExecutableDiscovery.FindExecutablesDeep)
- Recursive fallback (ExecutableDiscovery.FindExesRecursive)

#### Layer 3: Noise Executable Patterns (JSON-sourced, tiered)

Loaded from `data/blacklist.json` → `exe_name_patterns` (21 tiers, 90+ patterns).
Flattened into `BlacklistData.ExeNamePatterns` for filtering and `BlacklistData.TieredExePatterns` for scoring.

**Filtering usage** (`FileSystemHelper.IsNoiseExeName`):
- Case-insensitive substring match against exe name (without extension)
- Used in: `HasRootExecutableSignal`, `HasUnrealLayoutSignal`, `HasBinariesAtRoot`, `ExecutableDiscovery.FindExecutablesDeep`, `ExecutableDiscovery.FindExesRecursive`

**Scoring usage** (`FolderScanner.GetExePatternTier`):
- Returns severity tier for matched pattern
- Higher tier = lower severity penalty in scoring

### The 21-Tier Blacklist Structure

| Tier | Name | Examples | Severity |
|------|------|----------|----------|
| 1 | Universal Noise | `cleanup`, `touchup`, `installer`, `unins`, `setup`, `redist`, `vcredist`, `dxsetup`, `dotnet`, `directx`, `physx`, `eos` | Highest |
| 2 | Launcher Stubs | `launcher`, `updater`, `patcher`, `startup`, `bootstrapper` | Very High |
| 3 | Store Bootstraps | `galaxy`, `gog`, `epic`, `steam`, `uplay`, `ubisoft` | High |
| 4 | Anti-cheat / DRM | `easyanticheat`, `battleye`, `punkbuster`, `denuvo`, `vmprotect` | High |
| 5 | Unreal Build/Debug | `crashreportclient`, `unrealcefsubprocess`, `symboldump` | High |
| 6 | Crash Reporting | `crs-`, `bugsplat` | Medium-High |
| 7 | DRM Wrappers | `xlive` | Medium |
| 8 | Installer Utilities | `autorun`, `7za`, `xdelta` | Medium |
| 9 | Server/Loader/Stub | `server`, `stub`, `update`, `loader`, `browser`, `dowser` | Medium |
| 10 | Distribution Tools | `sdcr`, `tachyon` | Medium |
| 11 | Dev/Editor Tools | `datacompiler`, `editor`, `modmanager`, `reminder` | Medium-Low |
| 12 | Utilities/Debug | `install`, `debug`, `utils`, `sndrpt`, `exception`, `activation` | Medium-Low |
| 13 | Trial/Demo/Stub | `trial`, `_upp` | Low |
| 14 | Media/Codec Tools | `ffmpeg`, `ffplay`, `ffprobe` | Low |
| 15 | Installer Frameworks | `squirrel`, `wininst`, `w9xpopen` | Low |
| 16 | Runtime Interpreters | `python`, `blender` | Low |
| 17 | Web UI/Overlay | `coherentui`, `cefhost`, `awesomium`, `webview`, `overlay`, `scummvm` | Low |
| 18 | Repair/Service/Helper | `repair`, `service`, `helper` | Low |
| 19 | Unreal Build Tools | `unrealpak` | Low |
| 20 | Patch/Update | `patch` | Very Low |
| 21 | Utility Tools | `winscp`, `activate` | Lowest |

### Important Filtering Decisions

| Pattern | Decision | Rationale |
|---------|----------|-----------|
| `server` | **Noise** (Tier 9) | Dedicated servers are noise. Note: `Minecraft_Server.exe` IS a valid game, but dedicated server builds are generally not what users want to launch. |
| `crash` | **Noise** (Tier 1) | `crashreport`, `crashhelper`, `crashdebug`, `crashlog` are always noise. "crash" alone is broad enough to catch all variants. |
| `error` | **Noise** (Tier 1) | `BlizzardError`, `CrypticError`, `FenrisError`, `REDEngineErrorReporter` are always error reporters, not games. |
| `launcher` | **NOT Noise** (scoring penalty only) | Launchers are penalized in scoring (-20) but NOT filtered as noise, because some games use launchers as entry points (e.g., `AoW3Launcher.exe`). |
| `eos` | **NOT Noise** (removed) | `eos` was incorrectly filtering `FortniteClient-Win64-Shipping_EAC_EOS.exe` (actual game exe with EOS integration). |
| `scummvm` | **Noise** (Tier 17) | GOG SCUMMVM games are handled via `.info` file parsing (launch args + exe), not via `scummvm.exe`. |
| `editor` | **Noise** (Tier 11) | Content/level editors ship with games but are not the game itself. |
| `overlay` | **Noise** (Tier 17) | Steam/Discord overlay helpers are not games. |

### Pattern Analysis Results (2026-07-26)

Analysis of 165 games (1191 executables) from training data:

**Noise Filtering:**
- 545 exes filtered as noise (45.8%)
- 646 exes kept as non-noise (54.2%)
- 4 false negatives identified and fixed:
  - `CrashReport` → fixed by adding generic `crash` pattern
  - `BlizzardError` → fixed by adding generic `error` pattern
  - `UnityCrashHandler64` → fixed by adding generic `crash` pattern
  - `UnityCrashHandler32` → fixed by adding generic `crash` pattern

**Scoring Accuracy:**
- 100% accuracy on 67 tested games
- Correctly handles: backup copies, UE shipping builds, launcher vs game exe selection

**Python/C# Parity:**
- Removed `launcher` from C# noise patterns (was incorrectly filtering legitimate game launchers)
- Removed `eos` from C# noise patterns (was breaking Fortnite detection)
- Added `error` pattern to both Python and C# for error reporters
- Added generic `crash` pattern to Python (replaces 6 specific crash patterns)

### Non-Game Folder Names (Container Recursion)

During Pass 3 (container detection), these folder names are **never promoted as games**:

```
s_nonGameFolderNames (C#):
  Soundtrack, Soundtracks, Original Soundtrack, Manuals, Manual,
  Item Data, Misc, Bonus Content, Artwork, Wallpapers, Music,
  Redist, Support, Tools, _CommonRedist, CommonRedist, vcredist,
  dotnet, directx, physx, installer, _installer, install,
  easyanticheat, devtools, docs, licenses,
  steam controller configs, steamworks shared,
  dlc, program files, windowsapps, squirreltemp, portable, uninstall,
  battle.net, blizzard    ← CRITICAL: "blizzard" blocks BattleNet container detection
```

Python's `_NON_GAME_DIR_NAMES` includes additional entries:
```
wiiu, portable, reshade, sweetfx, enbseries, enb,
nexus mod manager, vortex, mod organizer,
dotnet35, dotnetfx35, msvc2012, msvc2012_x64, msvc2013, msvc2013_x64,
uninstall
```

**Note:** Python does NOT include `"blizzard"` in `_NON_GAME_DIR_NAMES`. The `blizzard/` folder is treated as a legitimate container (publisher folder) and its children are scanned for game signals. The C# inclusion of `"blizzard"` in `s_nonGameFolderNames` is a regression that blocks BattleNet game detection.

---

## Pass 3: Container Detection (Tier 3)

When a folder has no store signal and no fallback signal, it may be a **container** — an organizer folder whose children are the actual games.

### Container Detection Logic (C# — `ScanContainerChildren`)

```
For each child of the unknown folder:
  1. Count children with game signals (store signal, root exe, UE layout)
  2. If ≥1 child has signals:
     - Children with store signals → promoted as individual games
     - Children with root exe/UE layout → promoted as Standalone
     - If ≥2 children have signals → recurse into ALL children (organization)
  3. If no children have signals but folder is dirs-only (no files):
     - Publisher folder pattern → recurse into grandchildren
```

**Depth bounding:** Max depth 2 (container → child → grandchild). Prevents runaway recursion into deeply nested data directories.

### Container Detection Logic (Python)

Python's container detection in `_scan` is similar but uses a `container=True` flag:

```
When inside a container (container=True):
  - Skip non-game dir names (_NON_GAME_DIR_NAMES)
  - Skip folders with no exe AND no store-signal children
  - Only promote folders that have: store signal, root exe, root lnk, or store-signal children

Publisher folder pattern:
  - Root has ONLY dirs, no files at root → check grandchildren for exes
  - If any grandchild has a non-noise exe → is_container = True → recurse
```

### Divergences: Container Detection

| Scenario | Python | C# | Impact |
|----------|--------|-----|--------|
| Data-only subfolder inside container (Item Data, vo_soundsets) | Skipped via `_NON_GAME_DIR_NAMES` + `container=True` check | Skipped via `s_nonGameFolderNames` + `IsNonGameFolder` | Parity |
| Launcher directories (epiclauncher, battle.net) | Skipped via `SKIP_NAMES` | Not skipped at top level — would fall through to container detection | Minor — launcher dirs have no exes, so they produce no games |
| Organization detection threshold | ≥1 child with store signal → recurse all | ≥2 game children → recurse all; ≥1 game child → promote standalone children | Slight difference — C# requires 2+ for full recursion |

---

## Special Cases and Edge Cases

### 1. Games with No Root Exe

**Before T66/T67/T68:** 30+ games had no detected exe.
**After:** 0 games with no exe on both drives.

Resolution chain:
1. UE-aware exe discovery (T66) — finds exes in `Binaries/{platform}/`
2. `child/bin/` probe (T66) — finds exes in older UE games
3. Recursive fallback (T66) — BioShock pattern
4. `.lnk` shortcut parsing (T67) — finds exes via Windows shortcuts
5. GOG `.info` metadata (T65) — extracts exe from GOG metadata as last resort

### 2. Backup/Crack Exe Selection

Games with backup renames (e.g., `-Penumbra.exe`, `copy of Game.exe`):
- **Python**: Heavy penalties in scoring (`-30` for "copy of", `-40` for "org_")
- **C#**: `.lnk` fuzzy matching handles this case; scoring doesn't have explicit backup penalties
- **Result**: Both handle this, but via different mechanisms

### 3. Multi-Folder Games (FFXIV Pattern)

Games like FFXIV have separate `boot/` and `game/` subdirectories under a publisher folder:
```
SquareEnix/
  FINAL FANTASY XIV - A Realm Reborn/
    boot/
    game/
      ffxiv.exe
```

**Current behavior**: Detected as container, both `boot/` and `game/` promoted as separate entries.
**Known limitation** (documented in planning/99): Should be merged into one entry. Not yet implemented in either Python or C#.

### 4. GOG SCUMMVM Games

GOG DOS games wrapped in SCUMMVM have `.info` files with SCUMMVM-specific arguments:
```json
{
  "name": "Simon the Sorcerer",
  "playTasks": [{"isPrimary": true, "path": "scummvm.exe", "arguments": "-p data Simon"}]
}
```

**C# behavior**: `GogInfoParser` extracts title, exe, and launch args from `.info`. The `scummvm.exe` name IS in the noise blacklist (Tier 17), so `ExecutableDiscovery` won't find it as a root exe — but `GogInfoParser` overrides the exe from `.info` metadata.

**Python behavior**: Same — GOG `.info` metadata overrides exe detection.

### 5. EA Games Without `__Installer/`

Some EA games ship with `Touchup.exe` or `ActivationUI.exe` at root but no `__Installer/` directory:
- **Python**: ✅ Detected via `_scan_root` (checks for these exe names)
- **C#**: ❌ Only checks for `__Installer/` directory
- **Impact**: **Gap** — EA games without `__Installer/` will fall through to fallback detection (likely detected as Standalone via root exe, which is acceptable but loses the EA source classification)

### 6. Steam Emulator vs Real Steam

Distinguishing emulated Steam games from real Steam games:

**Real Steam**: Detected by `SteamLibraryScanner` via structural path detection. The game lives in `{library}/steamapps/common/` and has a matching ACF.

**Steam Emulator**: Detected by `FolderScanner` when `steam_api64.dll` or `steam_api.dll` is at the game's root, OUTSIDE a Steam library path. Also detected by `steam_emu.ini` in child dirs or UE ThirdParty path.

**Key insight from Python**: The UE `Engine/Binaries/ThirdParty/Steamworks/` path is NOT a Steam emulator signal — it's the Steamworks SDK bundled by Unreal Engine. Only `steam_emu.ini` in that path indicates emulation.

---

## Detection Results Summary

### Python Reference Results (validate.py on actual hardware)

| Metric | Value |
|--------|-------|
| D: drive games | 120 (was 129 with old scripts, 57s → 3s) |
| E: drive games | 37 (was 37, 2.5s → 1.3s) |
| Total | 157 games |
| Games with no exe | 0 |
| Unknowns | 0 |
| Performance | ~4.3s total (was 57s + 2.5s) |

### C# Test Coverage

| Test File | Tests | Scenarios Covered |
|-----------|-------|-------------------|
| `FolderScannerContainerTests` | 13 | UE3/UE4/Win32/WinGDK, organization, non-game, publisher, depth |
| `ExecutableDiscoveryTests` | 15 | Win64, Win32, WinGDK, Steam, child/bin, recursive fallback, noise, multi-platform |
| `LnkParserTests` | 13 | Extraction, resolution, fuzzy matching, backup renames, edge cases |
| `GogInfoParserTests` | 10 | Parsing, paths, DLC, edge cases |
| `BlacklistLoaderTests` | 11 | Loading, patterns, tier preservation, error handling |
| `ExecutableScoringTests` | 10 | Token matching, launcher penalties, noise tiers, shipping, file size |
| `ScannerFilterTests` | 9 | Non-game folders, noise exe exclusion, hidden folders, Bug 5 regression |
| **Total detection tests** | **133** | |

---

## Known Gaps and Future Work

### C# Gaps vs Python Reference

| Gap | Severity | Python Feature | C# Status | Notes |
|-----|----------|---------------|-----------|-------|
| **BattleNet skip-list regression** | ~~CRITICAL~~ | Container detection of `blizzard/` publisher folder | **✅ FIXED** | `"blizzard"` and `"battle.net"` removed from `NoiseSubDirNames` and `s_nonGameFolderNames`. C# now has richer detection than Python. |
| EA `touchup.exe` / `ActivationUI.exe` signals | — | Root-level EA signal detection | ✅ Implemented | Parity achieved |
| `gog.ico` as GOG signal | Low | GOG signal detection | ❌ Not implemented | All GOG games have `goggame*` files too |
| `steamapps/` dir outside Steam library | Low | Steam emulator detection | ❌ Not implemented | Edge case for pirated/cracked games |
| Backup/copy penalties in scoring | — | `-30` to `-40` penalties | ✅ Implemented | -25 for "copy of", -20 for "org_", -15 for "original" |
| Abbreviation matching (+8) | Low | Token prefix matching | ❌ Not implemented | e.g., "g3" matching "Gothic3" |
| Roman numeral matching (+12) | Low | "u9" ↔ "IX", "heroes4" ↔ "IV" | ❌ Not implemented | Edge case for game numbering |
| Small exe penalty (< 100KB) | — | `-15` penalty | ✅ Implemented | Parity achieved |
| PE metadata scoring boost | **Medium** | +15 for FileDescription match | **✅ Implemented** | C# `ScoreExecutable()` now reads `FileVersionInfo.GetVersionInfo()` — penalizes noise Description/InternalName (-25/-20), bonuses game-like descriptions (+10). Graceful degradation on broken PE headers. |
| Engine detection (Unity, RAGE, Frostbite) | Low | `_detect_engine()` | Not implemented | Not used in game detection, only metadata |
| Extension-filtered deep walk | Low | `.exe/.dll/.ini` only | Not implemented | C# scans all files but filters noise |
| Phase 4 enrichment (PCGamingWiki) | Medium | `--metadata` / `--pcgw` flags | Not implemented | Future feature for unknown games |
| **Epic manifest parsing** | Medium | Parse `.item` files from ProgramData | **Planned** | Authoritative game names, exe paths, catalog IDs |
| **Origin `.mfst` parsing** | Low | Parse HTTP query strings | **Planned** | Legacy Origin games only |
| **EA App registry fallback** | Low | Check `HKLM\Software\EA Games\` | **Planned** | Simple registry read |
| **Ubisoft registry fallback** | Low | Check `HKLM\SOFTWARE\Ubisoft\Launcher\Installs\` | **Planned** | Simple registry read |

### Known Limitations (Both Systems)

| Limitation | Status | Notes |
|-----------|--------|-------|
| Multi-folder games (FFXIV) | Known | `boot/` + `game/` detected as separate entries |
| Epic `.item` manifest support | **Planned** | Parse from `C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests\` |
| EA App encrypted manifests | Deferred | `IS` file is AES-encrypted, not worth the complexity |
| Ubisoft protobuf manifests | Deferred | Binary protobuf format, use registry fallback instead |
| `.bat` launcher configuration | In progress | Parsing done in Python, UI not implemented |
| User blacklist editor | Future | `planning/91-user-blacklist-editor.md` |

---

## File Reference

### C# Source Files

| File | Lines | Purpose |
|------|-------|---------|
| `src/App/Services/FolderScanner.cs` | 232 | Three-pass detection orchestrator + AddGameEntry assembly |
| `src/App/Services/StoreSignalDetector.cs` | 163 | 10-signal priority chain for store/platform detection |
| `src/App/Services/FallbackSignalDetector.cs` | 193 | 5 fallback signals: Steam Emu deep, Ubisoft legacy, UE layout, root exe, root .lnk |
| `src/App/Services/ContainerScanner.cs` | 116 | Container/publisher folder recursion with organization detection |
| `src/App/Services/ExecutableDiscovery.cs` | ~400 | Deep exe search (5 strategies), scoring (PE metadata + tier-based), launcher detection |
| `src/App/Services/LnkParser.cs` | 140 | .lnk binary parsing, exe resolution with backup rename matching |
| `src/App/Services/GogInfoParser.cs` | 163 | GOG goggame-*.info JSON parsing, DLC filtering |
| `src/App/Services/EaInstallLogParser.cs` | ~90 | EA __Installer/InstallLog.txt parsing — game name, display name, studio |
| `src/App/Services/BlacklistLoader.cs` | 212 | JSON blacklist loading, tier preservation |
| `src/App/Services/BlacklistData.cs` | 28 | Data model for blacklist patterns |
| `src/App/Services/FileSystemHelper.cs` | 115 | Shared filesystem utilities, noise checks, display name normalization |
| `src/Core/Models/GameEntry.cs` | 33 | Game entry record with all detection metadata |

### Python Reference Files

| File | Lines | Purpose |
|------|-------|---------|
| `tools/detect.py` | 1829 | Unified 4-phase detection tool (reference gold) |
| `tools/detect_folder.py` | 702 | Deprecated — original signal detection script |
| `tools/list_standalone_games.py` | N/A | Deprecated — fast-then-deep architecture source |
| `tools/lookup_metadata.py` | 1672 | Metadata enrichment: PCGW, Epic manifest cross-ref, Steam API |
| `tools/parse_registry.py` | ~350 | Registry parsing for Epic manifest paths, EA/Ubisoft install dirs |

### Test Files

| File | Tests | Focus |
|------|-------|-------|
| `tests/App.Tests/StoreSignalDetectorTests.cs` | 31 | All 10 store signals, priority order, no-signal cases |
| `tests/App.Tests/FallbackSignalTests.cs` | 16 | 5 fallback signals: Steam Emu deep, Ubisoft legacy, UE layout, root exe, root .lnk |
| `tests/App.Tests/FolderScannerContainerTests.cs` | 13 | Container/UE/layout detection |
| `tests/App.Tests/ExecutableDiscoveryTests.cs` | 15 | Deep exe search across platforms |
| `tests/App.Tests/ExecutableScoringTests.cs` | 15 | Token matching, launcher/backup penalties, noise tiers, shipping, file size |
| `tests/App.Tests/LnkParserTests.cs` | 13 | .lnk parsing and resolution |
| `tests/App.Tests/GogInfoParserTests.cs` | 10 | GOG metadata parsing |
| `tests/App.Tests/BlacklistLoaderTests.cs` | 11 | Blacklist loading and tiers |
| `tests/App.Tests/ScannerFilterTests.cs` | 9 | Noise filtering and regression |

### Data Files

| File | Purpose |
|------|---------|
| `data/blacklist.json` | 21-tier noise patterns (exe names, directory names, PE metadata blacklist, PCGW title noise) with inline test cases |

---

## Test Cases from `blacklist.json`

The blacklist includes inline test cases for validation. Key examples:

| Filename | Expected | Reason |
|----------|----------|--------|
| `galaxy_no_mans_sky_2.12.0.15.exe` | Noise | Contains "galaxy" — GOG Galaxy stub |
| `NMS.exe` | **Not noise** | Actual game executable |
| `ES2-Win64-Shipping.exe` | **Not noise** | Game exe (UE4 shipping build) |
| `ACOrigins.exe` | **Not noise** | Assassin's Creed Origins (NOT filtered by "origin") |
| `RainbowSix_BE.exe` | **Not noise** | BattlEye variant — actual game exe |
| `GRB_vulkan.exe` | **Not noise** | Vulkan render variant — actual game exe |
| `scummvm.exe` | Noise | ScummVM emulator (GOG games use .info metadata instead) |
| `u9patch118.exe` | Noise | Contains "patch" — game patch, not the game |

---

## Detection Pipeline Flowchart

```
START: Scan library root
  │
  ├─ For each subdirectory:
  │   │
  │   ├─ Is it in HiddenFolders? → SKIP
  │   ├─ Is it a noise directory? → SKIP
  │   ├─ Is it a NoiseSubDirName? → SKIP
  │   │
  │   ├─ PASS 1: StoreSignalDetector.DetectType()
  │   │   ├─ GOG signal? → GameSourceKind.Gog → AddGameEntry
  │   │   ├─ EA signal? → GameSourceKind.EaApp → AddGameEntry
  │   │   ├─ Ubisoft Emu signal? → GameSourceKind.UbisoftConnect → AddGameEntry
  │   │   ├─ Ubisoft signal? → GameSourceKind.UbisoftConnect → AddGameEntry
  │   │   ├─ Epic signal? → GameSourceKind.Epic → AddGameEntry
  │   │   ├─ Blizzard signal? → GameSourceKind.BattleNet → AddGameEntry
  │   │   ├─ Xbox signal? → GameSourceKind.Xbox → AddGameEntry
  │   │   ├─ Rockstar signal? → GameSourceKind.Rockstar → AddGameEntry
  │   │   ├─ Steam Emu strong? → GameSourceKind.SteamEmu → AddGameEntry
  │   │   └─ Steam Emu weak? → GameSourceKind.SteamEmu → AddGameEntry
  │   │
      │   ├─ PASS 2: FallbackSignalDetector.DetectFallbackType()
  │   │   ├─ steam_emu.ini deep? → SteamEmu → AddGameEntry
  │   │   ├─ UbiStats.dll? → UbisoftConnect → AddGameEntry
  │   │   ├─ UE layout (Engine/ + Binaries/ or Binaries/ at root)?
  │   │   │   └─ → Standalone → AddGameEntry
  │   │   ├─ Root non-noise .exe?
  │   │   │   └─ → Standalone → AddGameEntry
  │   │   └─ Root .lnk file?
  │   │       └─ → Standalone → AddGameEntry
  │   │
      │   └─ PASS 3: ContainerScanner.ScanContainerChildren()
  │       ├─ Count children with game signals
  │       ├─ ≥1 game child:
  │       │   ├─ Children with store signals → promoted
  │       │   ├─ Children with exe/UE → promoted as Standalone
  │       │   └─ ≥2 game children → recurse into ALL (organization)
  │       └─ Dirs-only root (no files) → recurse (publisher pattern)
  │
  └─ For each AddGameEntry call:
      ├─ ExecutableDiscovery.FindPrimaryExecutable()
      │   ├─ FindExecutablesDeep() → root + children + UE paths + recursive
      │   └─ ScoreExecutable() → pick best candidate, capture FileDescription
      ├─ LnkParser.ResolveExeFromLnk() (if no exe found)
      ├─ ExecutableDiscovery.FindLauncherExecutable()
      ├─ ExecutableDiscovery.FindEpicManifest()
      ├─ FileSystemHelper.NormalizeDisplayName()
      ├─ GogInfoParser.TryParse() (if GOG)
      │   ├─ Override title, exe (fallback), args, game ID
      ├─ EaInstallLogParser.TryParse() (if EA App)
      │   ├─ Override display name, studio, game name
      ├─ EpicManifestParser (if Epic)
      │   ├─ Override display name, catalog IDs
      ├─ PE FileDescription enrichment (if no store enrichment)
      │   ├─ Use primary exe's FileDescription as display name
      │   ├─ Guard: reject short strings, generic placeholders
      ├─ UbisoftReadmeParser.TryParse() (if Ubisoft, no prior enrichment)
      │   ├─ Override display name from Support/Readme/ metadata
      └─ Create GameEntry with all metadata
```
