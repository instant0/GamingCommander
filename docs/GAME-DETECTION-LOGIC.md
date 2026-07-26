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

## Pass 1: Store Signal Detection (Tier 1)

Both C# and Python check stores in **priority order** (first match wins). The C# implementation lives in `StoreSignalDetector.DetectType()`.

### Signal Priority and Logic

| Priority | Platform | Signal | C# Code | Python Code | Match Type |
|----------|----------|--------|---------|-------------|------------|
| 1 | **GOG** | `goggame*` files at root (goggame.dll, goggame-*.info, gog_*) | `HasGogSignal` | `_check_gog` + `_scan_root` | File glob `goggame*` (C#); `goggame.dll` exact + prefix scan (Python) |
| 2 | **EA** | `__Installer/` directory at root, or `Touchup.exe`/`ActivationUI.exe` at root | `HasEaSignal` | `_check_ea` | Directory existence + exe name check |
| 3 | **Ubisoft Emulator** | `uplay_loader*` + `.ini` with `Username=` and `AccountId=` | `HasUbisoftEmulatorSignal` | `_check_ubisoft_emu` | Loader pattern + INI content scan |
| 4 | **Ubisoft** | `uplay_install.manifest` or `uplay_r*_loader*.dll` at root | `HasUbisoftSignal` | `_check_ubisoft` | Exact file name + glob pattern |
| 5 | **Epic** | `.egstore/` or `.egsstore/` directory at root | `HasEpicSignal` | `_check_epic` | Directory existence |
| 6 | **Blizzard** | `.battle.net/` directory at root | `HasBlizzardSignal` | `_check_blizzard` | Directory existence |
| 7 | **Xbox** | `default-metadata.json` at root | `HasXboxSignal` | `_check_xbox` | File existence |
| 8 | **Rockstar** | `title.rgl` at root | `HasRockstarSignal` | `_check_rockstar` | File existence |
| 9 | **Steam Emulator** (strong) | `steam_api64.dll` or `steam_api.dll` at root | `HasSteamEmulatorSignal` | `_check_steam_emu` | File existence |
| 10 | **Steam Emulator** (weak) | `steam_appid.txt` alone (only in `StoreSignalDetector`, not in Python Phase 1) | `HasSteamSignal` | N/A in Phase 1 | File existence |

### Divergences: Python vs C#

| Scenario | Python | C# | Impact |
|----------|--------|-----|--------|
| `gog.ico` at root | ✅ Detected as GOG signal (in `_scan_root`) | ❌ Not in `HasGogSignal` | Minor — GOG games always have `goggame*` files |
| `touchup.exe` / `ActivationUI.exe` at root | ✅ Detected as EA signal | ✅ Detected in `HasEaSignal` | Parity |
| `steam_appid.txt` alone (no `steam_api64.dll`) | ❌ Not a Phase 1 signal (weak, only used in Tier 2) | ✅ Detected as `SteamEmu` via `HasSteamSignal` | Minor difference — C# is slightly more aggressive |
| `uplay_install.state` | ✅ Detected (Python deep scan `_match_markers`) | ✅ Detected in `HasUbisoftSignal` | Parity |
| Deep signal: `steam_emu.ini` | Phase 2 deep scan | `HasSteamEmuDeepSignal` in Pass 2 | Parity |
| Deep signal: `steamapps/` dir or `.acf` files outside Steam library | ✅ `_has_steam_app_manifest` | ❌ Not implemented | **Gap** — standalone games mimicking Steam layout not detected as SteamEmu |
| **`"blizzard"` in skip lists** | **Not in any skip list** | **In `NoiseSubDirNames` + `s_nonGameFolderNames`** | **CRITICAL GAP — C# skips the entire Blizzard publisher folder; Python processes it. C# has richer BattleNet detection (parent propagation, name heuristics, exe heuristics) but it's unreachable because the folder is skipped before detection runs.** |

#### BattleNet Skip-List Regression (Detailed)

This is the most significant divergence between the two systems. The C# implementation has **richer** BattleNet detection than Python:
- `HasBattleNetGameSignal()` checks folder names (`warcraft`, `diablo`, `overwatch`, etc.)
- `HasBattleNetGameSignal()` checks exe names (`DiabloIII.exe`, `Warcraft III.exe`, etc.)
- Parent propagation checks if a child's parent has BattleNet signals

**None of this code is ever reached** because:
1. `FileSystemHelper.NoiseSubDirNames` (line 22) contains `"blizzard"` → `FolderScanner.Scan()` skips the entire directory
2. `ContainerScanner.s_nonGameFolderNames` (line 26) contains `"blizzard"` → container recursion also skips it

In `detect.py`, `"blizzard"` is absent from both `SKIP_NAMES` and `_NON_GAME_DIR_NAMES`. The publisher folder is processed normally as a container, and its children (game folders) are scanned for `.battle.net/` signals.

**Scenario:** Library root is `d:\games\`, contains `d:\games\blizzard\Diablo III\` with `.battle.net/` directory.

| Step | Python | C# |
|------|--------|-----|
| 1 | Scan `d:\games\` → find `blizzard/` | Scan `d:\games\` → find `blizzard/` |
| 2 | `blizzard` not in `SKIP_NAMES` → proceed | `blizzard` in `NoiseSubDirNames` → **SKIP entire folder** |
| 3 | Check children: `Diablo III/` has `.battle.net/` → BattleNet | Never reached |
| 4 | Result: **Diablo III detected as BattleNet** | Result: **Diablo III never discovered** |

**Fix:** Remove `"blizzard"` from both `NoiseSubDirNames` and `s_nonGameFolderNames`. Keep `"battle.net"` in skip lists (it's the launcher executable directory, not a game container).

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
  battle.net, blizzard    ← CRITICAL: "blizzard" blocks BattleNet detection
```

Python SKIP_NAMES (additional entries not in C#):
```
  epiclauncher, launcher, battle.net, ubisoft game launcher, origin,
  ea desktop, gog galaxy, wiiu, reshade, sweetfx, enbseries, enb,
  nexus mod manager, vortex, mod organizer, uninstall
```

**Note:** The Python `SKIP_NAMES` includes known launcher directories (Epic, Battle.net, etc.) and non-game tools (reshade, enb, mod managers). Some of these are handled by the C# `s_nonGameFolderNames` set in container recursion instead of at top-level scan.

**CRITICAL BUG:** `"blizzard"` is in the C# skip list but NOT in Python's `SKIP_NAMES`. This means the entire `blizzard/` publisher folder (containing game subdirectories like `Diablo III/`, `World of Warcraft/`) is silently skipped by C# but processed normally by Python. The C# implementation has richer BattleNet detection (parent propagation, name heuristics, exe heuristics) but it's unreachable because the folder is skipped before detection runs. See "BattleNet Skip-List Regression" in the Divergences section above.

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
| `scummvm` | **Noise** (Tier 17) | GOG SCUMMVM games are handled via `.info` file parsing (launch args + exe), not via `scummvm.exe`. |
| `editor` | **Noise** (Tier 11) | Content/level editors ship with games but are not the game itself. |
| `overlay` | **Noise** (Tier 17) | Steam/Discord overlay helpers are not games. |

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
| **BattleNet skip-list regression** | **CRITICAL** | Container detection of `blizzard/` publisher folder | **❌ REGRESSION** | C# has richer detection than Python (parent propagation, name heuristics, exe heuristics) but `"blizzard"` in `NoiseSubDirNames` + `s_nonGameFolderNames` blocks the entire folder. Python has no such skip. See BattleNet section above. |
| EA `touchup.exe` / `ActivationUI.exe` signals | — | Root-level EA signal detection | ✅ Implemented | Parity achieved |
| `gog.ico` as GOG signal | Low | GOG signal detection | ❌ Not implemented | All GOG games have `goggame*` files too |
| `steamapps/` dir outside Steam library | Low | Steam emulator detection | ❌ Not implemented | Edge case for pirated/cracked games |
| Backup/copy penalties in scoring | — | `-30` to `-40` penalties | ✅ Implemented | -25 for "copy of", -20 for "org_", -15 for "original" |
| Abbreviation matching (+8) | Low | Token prefix matching | ❌ Not implemented | e.g., "g3" matching "Gothic3" |
| Roman numeral matching (+12) | Low | "u9" ↔ "IX", "heroes4" ↔ "IV" | ❌ Not implemented | Edge case for game numbering |
| Small exe penalty (< 100KB) | — | `-15` penalty | ✅ Implemented | Parity achieved |
| PE metadata scoring boost | Low | +15 for FileDescription match | N/A (Phase 4 only) | PE enrichment is optional in Python, not in C# app |
| Engine detection (Unity, RAGE, Frostbite) | Low | `_detect_engine()` | Not implemented | Not used in game detection, only metadata |
| Extension-filtered deep walk | Low | `.exe/.dll/.ini` only | Not implemented | C# scans all files but filters noise |
| Phase 4 enrichment (PCGamingWiki) | Medium | `--metadata` / `--pcgw` flags | Not implemented | Future feature for unknown games |

### Known Limitations (Both Systems)

| Limitation | Status | Notes |
|-----------|--------|-------|
| Multi-folder games (FFXIV) | Known | `boot/` + `game/` detected as separate entries |
| Epic `.item` manifest support | Future | Epic store metadata not parsed (Phase 3 planned) |
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
| `src/App/Services/ExecutableDiscovery.cs` | 352 | Deep exe search (5 strategies), scoring, launcher detection |
| `src/App/Services/LnkParser.cs` | 140 | .lnk binary parsing, exe resolution with backup rename matching |
| `src/App/Services/GogInfoParser.cs` | 163 | GOG goggame-*.info JSON parsing, DLC filtering |
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
      │   └─ ScoreExecutable() → pick best candidate
      ├─ LnkParser.ResolveExeFromLnk() (if no exe found)
      ├─ ExecutableDiscovery.FindLauncherExecutable()
      ├─ ExecutableDiscovery.FindEpicManifest()
      ├─ FileSystemHelper.NormalizeDisplayName()
      ├─ GogInfoParser.TryParse() (if GOG)
      │   ├─ Override title, exe (fallback), args, game ID
      └─ Create GameEntry with all metadata
```
