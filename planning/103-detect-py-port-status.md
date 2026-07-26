# detect.py Port Status — C# Parity Analysis

**Status:** REVIEWED — 2026-07-26  
**Reference:** `tools/detect.py` (1829 LOC)  
**C# Scanner Files:** `FolderScanner.cs`, `StoreSignalDetector.cs`, `ExecutableDiscovery.cs`, `FallbackSignalDetector.cs`, `ContainerScanner.cs`, `GogInfoParser.cs`, `LnkParser.cs`

---

## Executive Summary

The C# codebase has achieved **~75% functional parity** with `detect.py` for the features that matter for the MVP product (store detection, exe discovery, container scanning, GOG metadata, .lnk resolution). The remaining gaps are:

1. **BattleNet skip-list regression** — `"blizzard"` in C# skip lists blocks BattleNet detection entirely; Python has no such skip. C# has richer detection code (parent propagation, name heuristics, exe heuristics) that is unreachable.
2. **Engine detection** — completely absent (4 engine types)
3. **Exe scoring** — missing Roman numerals, abbreviation matching, "org" group heuristic, PE tiebreaker
4. **Non-game folder heuristics** — name-only (missing child-all-non-game check, file-type analysis)
5. **Detection logger** — no equivalent (debugging/diagnostics gap)
6. **PE metadata extraction** — no `pefile` equivalent
7. **PCGW enrichment** — no API integration
8. **Deep signal scan** — partial (missing `_match_markers` walk, `steamapps` manifest check)

---

## Detailed Feature Matrix

### 1. Root Store Signals (Phase 1)

| # | Signal | detect.py | C# | Status | Gap |
|---|--------|-----------|-----|--------|-----|
| 1 | GOG | `goggame.dll`, `goggame-*`, `gog_*`, `gog.ico` | `goggame*` glob | ⚠️ DIFF | C# misses `gog_*` prefix and `gog.ico` |
| 2 | EA | `__Installer/`, `touchup.exe`, `activationui.exe` | Same 3 checks | ✅ PORTED | — |
| 3 | Ubisoft Emu | `uplay_loader*` + `.ini` with config | Same logic | ✅ PORTED | — |
| 4 | Ubisoft | `uplay_install.manifest`, `uplay_r*_loader*.dll` | Hardcoded 4 DLLs + `uplay_install.state` | ⚠️ DIFF | C# misses non-standard loader DLLs |
| 5 | Epic | `.egstore/` or `.egsstore/` | Same | ✅ PORTED | — |
| 6 | Blizzard | `.battle.net/` | Same | ✅ PORTED | — |
| 7 | Xbox | `default-metadata.json` | Same | ✅ PORTED | — |
| 8 | Rockstar | `title.rgl` | Same | ✅ PORTED | — |
| 9 | Steam Emu | `steam_api64.dll` / `steam_api.dll` | Same | ✅ PORTED | — |
| 10 | (C# only) | — | `steam_appid.txt` | ➕ ADDED | C# improvement |
| 11 | (C# only) | — | `HasBattleNetGameSignal()` | ➕ ADDED | C# improvement — BUT UNREACHABLE due to skip-list regression |

**CRITICAL: BattleNet Skip-List Regression**

`"blizzard"` is in both `FileSystemHelper.NoiseSubDirNames` (line 22) and `ContainerScanner.s_nonGameFolderNames` (line 26). In `detect.py`, `"blizzard"` is absent from both `SKIP_NAMES` and `_NON_GAME_DIR_NAMES`. This means:

- Python: `blizzard/` folder is processed as a container → children scanned for `.battle.net/` → games detected
- C#: `blizzard/` folder is silently skipped → games never discovered

C# has RICHER BattleNet detection than Python (parent propagation, folder name heuristics like "warcraft"/"diablo", exe name heuristics) — but none of it runs because the folder is skipped before detection. The fix is to remove `"blizzard"` from both skip lists.

**Gaps to close (low priority):**
- Add `gog_*` prefix check to `HasGogSignal()`
- Add `gog.ico` check to `HasGogSignal()`
- Use pattern matching instead of hardcoded Ubisoft DLL names

### 2. Exe Scoring (`_pick_best_root_exe` vs `ScoreExecutable`)

| Feature | detect.py | C# | Status |
|---------|-----------|-----|--------|
| Single exe shortcut | Returns immediately | Returns immediately | ✅ PORTED |
| Folder token extraction | Splits on `_`, `-`, space | Splits on space, `_`, `-`, `.`, `:` | ✅ (C# adds more) |
| "copy of" penalty | `-30` | `-25` | ⚠️ DIFF |
| "_copy" / " copy" penalty | `-25` | `-25` | ✅ PORTED |
| "org" group heuristic | `-40` if clean exe exists, else `-20` | `-20` for `org_` | ❌ MISSING |
| "original" penalty | `-15` | `-15` | ✅ PORTED |
| "crack" penalty | `-25` | Not present | ❌ MISSING |
| "launcher" penalty | `-20` | `-20` via launcherPatterns | ✅ PORTED |
| `_TOOL_NAMES` penalty | 16 specific tool names → `-25` | Covered by noise patterns (tier-based) | ⚠️ DIFF |
| Folder token match | `+10` per token | `+10` per token | ✅ PORTED |
| Exact stem match | `+15` (token-exact) | `+15` (bidirectional substring) | ⚠️ DIFF |
| Abbreviation match | `+8` for short stems sharing first letter | Not present | ❌ MISSING |
| Roman numeral match | `+12` bidirectional (u9↔ix, heroes4↔iv) | Not present | ❌ MISSING |
| Small exe penalty | `-15` (<100KB), `-5` (<500KB) | `-15` (<100KB) | ⚠️ DIFF |
| File size bonus | `+min(size/10M, 10)` up to +10 | `+min(size/20M, 5)` up to +5 | ⚠️ DIFF |
| UE path bonus | `+5` for "shipping"/"win64" | `+5` for "shipping"/"win64" | ✅ PORTED |
| PE metadata tiebreaker | `+15` FileDescription match, `+10` ProductName match | Not present | ❌ MISSING |

**Gaps to close (medium priority for detection accuracy):**
- Add "crack" penalty to scoring
- Add abbreviation match (`+8` for short stems)
- Add Roman numeral match (`+12`)
- Consider adding "org" group heuristic
- Consider PE metadata tiebreaker (requires `pefile` or C# equivalent)

### 3. Exe Discovery (`_find_exe_in_subdirs` vs `FindExecutablesDeep`)

| Feature | detect.py | C# | Status |
|---------|-----------|-----|--------|
| UE4-5 fast path (Engine/ + child/Binaries/) | Checks `Engine/` in dir_names, scans child `Binaries/{Win64,Win32,Steam,Linux}/` | Scans `child/Binaries/{Win64,Win32,WinGDK,Steam}/` | ⚠️ DIFF |
| UE3 (root Binaries/) | Checks `binaries` in dir_names | `FallbackSignalDetector.HasBinariesAtRoot` | ✅ PORTED |
| child/bin/ layout | Not present | Checked in `FindExecutablesDeep` | ➕ ADDED |
| Generic fallback (3 levels) | 3-level nested `os.scandir` with noise filtering | `FindExesRecursive` with max depth 2 | ⚠️ DIFF |
| Linux platform | Included | Dropped (Windows-only app) | ✅ INTENTIONAL |

**Gaps to close (low priority):**
- Increase generic fallback to 3 levels (currently 2)
- The `Engine/` directory check in UE4-5 fast path is a useful optimization

### 4. .lnk Resolution

| Feature | detect.py | C# | Status |
|---------|-----------|-----|--------|
| Binary read + latin-1 decode | `read_bytes()` + `decode("latin-1")` | `File.ReadAllBytes` + `Encoding.Latin1` | ✅ PORTED |
| Regex for exe names | Same pattern | Same pattern | ✅ PORTED |
| Skip DLLs | Same set | Same set | ✅ PORTED |
| Longest name heuristic | `max(candidates, key=len)` | Picks longest | ✅ PORTED |
| Subdir search (3 levels) | `os.walk` depth 3 | `FindExesInSubdirs` maxDepth 3 | ✅ PORTED |
| Backup fuzzy match (`-Name.exe`) | Prefix check | Same | ✅ PORTED |
| Backup fuzzy match (`copy of Name.exe`) | Prefix check | Same | ✅ PORTED |
| Stem substring match | `exe_stem in fn_lower` | `foundName.Contains(exeStem)` | ✅ PORTED |

**Status: FULLY PORTED** — no gaps.

### 5. GOG Metadata Extraction

| Feature | detect.py | C# | Status |
|---------|-----------|-----|--------|
| Search scope | Root + 1 level of non-noise subdirs | Same | ✅ PORTED |
| `.info` glob | `glob("goggame-*.info")` | `GetFilesSafe(searchDir, "goggame-*.info")` | ✅ PORTED |
| Main game preference | `gameId == rootGameId` | Same with null guard | ✅ PORTED |
| DLC fallback | First non-main entry | Same | ✅ PORTED |
| playTasks extraction | `isPrimary` + `path` + `arguments` | Same with first-task fallback | ✅ PORTED |
| Exe path resolution | Returns relative path | Resolves to absolute | ✅ (C# improvement) |

**Status: FULLY PORTED** — no gaps.

### 6. Container Detection (Phase 3)

| Feature | detect.py | C# | Status |
|---------|-----------|-----|--------|
| Child store signal check | `_scan_root` on each child | `StoreSignalDetector.DetectType` | ✅ PORTED |
| Child exe check (non-data) | `c_has_exe and not _is_non_game_folder` | `HasRootExecutableSignal` | ✅ PORTED |
| Publisher folder (no files at root) | 2-level deep scan for grandchildren exes | `files.Length == 0 && dirs > 0` then recurse | ⚠️ DIFF |
| `_is_non_game_folder` heuristics | 3 layers: name + children + file-type | Name-only check | ❌ PARTIAL |
| Organization recursion | Always recurses into all children | Bounded depth 1, requires ≥2 game children | ⚠️ DIFF (C# is stricter) |
| Non-game subfolder names | Checks `_NON_GAME_DIR_NAMES` + `_NON_GAME_SUBDIR_NAMES` | `s_nonGameFolderNames` (30 names) | ⚠️ PARTIAL |
| **`"blizzard"` in skip lists** | **Not in `_NON_GAME_DIR_NAMES`** | **In `s_nonGameFolderNames`** | **❌ REGRESSION — blocks BattleNet container detection** |

**Gaps to close (medium priority):**
- Add child-all-non-game check to `IsNonGameFolder`
- Add file-type analysis (no exes, no meaningful files → not a game)

### 7. Deep Signal Scan (Phase 2)

| Feature | detect.py | C# | Status |
|---------|-----------|-----|--------|
| Walk depth | 4 levels, extension-filtered (.exe/.dll/.ini) | No walk — targeted file checks only | ❌ MISSING |
| `_has_steam_emu_ini` | Root, all child dirs, UE Steamworks path | Root, immediate children, UE Steamworks | ⚠️ DIFF |
| `_has_steam_app_manifest` | Checks `steamapps/` dir or `.acf` files | Not present | ❌ MISSING |
| `_has_ubisoft_legacy` | `UbiStats.dll` at root or child | Same | ✅ PORTED |
| `_match_markers` | Matches collected names from walk against patterns | Not present | ❌ MISSING |

**Gaps to close (low priority):**
- `_match_markers` is a secondary signal — the primary signals (root-level) are sufficient for most games
- `_has_steam_app_manifest` is redundant with SteamLibraryScanner
- The 4-level walk is only needed for deeply nested container structures

### 8. Non-Game Folder Detection

| Feature | detect.py | C# | Status |
|---------|-----------|-----|--------|
| Exact name match (`_NON_GAME_DIR_NAMES`, 16 names) | Yes | `ContainerScanner.IsNonGameFolder` checks `s_nonGameFolderNames` | ✅ PORTED |
| Child all-non-game check | Checks if ALL children are in `_NON_GAME_SUBDIR_NAMES` | Not present | ❌ MISSING |
| File-type analysis | Scans files: `_NON_GAME_FILE_EXTS`, `_SUPPORT_FILE_EXTS` | Not present | ❌ MISSING |

**Gaps to close (low priority):**
- Add child-all-non-game check
- Add file-type analysis for folders with no exes

---

## What's NOT Ported (and Why)

### 1. Engine Detection — NOT PORTED

**Why:** Not required for MVP. Engine detection is a local filesystem probe — trivial to port when needed.

**Port effort:** ~2 hours. 4 engine checks, each 5-10 lines of code.

**See:** Plan 102, Phase 2.

### 2. Detection Logger — NOT PORTED

**Why:** CLI debugging tool. The GUI app uses status messages instead. Not needed for production.

**Port effort:** ~4 hours if desired. Would be useful for debug mode.

### 3. PE Metadata Extraction — NOT PORTED

**Why:** Requires `pefile` Python library or C# equivalent (`System.Reflection.PortableExecutable`). Nice-to-have for exe scoring tiebreaker, not critical.

**Port effort:** ~3-4 hours. Needs `PEReader` or NuGet package.

### 4. PCGW Enrichment — NOT PORTED

**Why:** Part of Plan 102 Phase 3 (metadata scraping). Deferred to post-MVP.

**Port effort:** ~8-12 hours for full implementation.

### 5. `_match_markers` — NOT PORTED

**Why:** Secondary signal for Phase 2 deep scan. Only catches games where store signals are in deeply nested subdirectories. Primary signals (root-level) catch 95%+ of real games.

**Port effort:** ~2-3 hours. Requires 4-level walk infrastructure.

### 6. `_has_steam_app_manifest` — NOT PORTED

**Why:** Redundant with `SteamLibraryScanner` which handles all Steam library detection. The standalone scanner doesn't need this.

**Port effort:** N/A — already handled architecturally.

---

## Prioritized Gap Closure

### CRITICAL (blocking real-game detection)

| Gap | Effort | Impact | Files |
|-----|--------|--------|-------|
| **Remove `"blizzard"` from `NoiseSubDirNames` + `s_nonGameFolderNames`** | **10 min** | **CRITICAL — all BattleNet games are currently undetected** | `FileSystemHelper.cs`, `ContainerScanner.cs` |

### High Priority (should fix for detection accuracy)

| Gap | Effort | Impact | Files |
|-----|--------|--------|-------|
| GOG: add `gog_*` prefix + `gog.ico` | 10 min | Low — few games use these | `StoreSignalDetector.cs` |
| Ubisoft: use pattern match instead of hardcoded DLLs | 15 min | Low — covers edge cases | `StoreSignalDetector.cs` |
| Exe scoring: add "crack" penalty | 5 min | Low | `ExecutableDiscovery.cs` |

### Medium Priority (improve detection quality)

| Gap | Effort | Impact | Files |
|-----|--------|--------|-------|
| Engine detection | 2 hrs | Medium — enables Plan 101/102 | New: `EngineDetector.cs` |
| Non-game: child-all-non-game check | 1 hr | Medium — reduces false positives | `ContainerScanner.cs` |
| Non-game: file-type analysis | 1 hr | Medium — catches data-only folders | `ContainerScanner.cs` |
| Exe scoring: Roman numerals + abbreviations | 2 hrs | Medium — improves exe selection | `ExecutableDiscovery.cs` |

### Low Priority (nice-to-have)

| Gap | Effort | Impact | Files |
|-----|--------|--------|-------|
| Detection logger | 4 hrs | Low — debug tool | New: `DetectionLogger.cs` |
| PE metadata extraction | 3 hrs | Low — scoring tiebreaker | New: `PeMetadataExtractor.cs` |
| Deep signal walk (4 levels) | 3 hrs | Low — catches deeply nested signals | `FallbackSignalDetector.cs` |
| `_match_markers` | 3 hrs | Low — secondary signal | `FallbackSignalDetector.cs` |
| Exe scoring: "org" group heuristic | 1 hr | Low — edge case | `ExecutableDiscovery.cs` |

---

## Recommendation

The C# port is **functionally sufficient for MVP** except for one critical regression: the `"blizzard"` skip-list entry blocks all BattleNet game detection. The remaining gaps are edge cases that affect <5% of real game libraries. Prioritize:

1. **Remove `"blizzard"` from skip lists** — 10 min fix, unblocks all BattleNet games
2. **Engine detection** (Plan 102 Phase 2) — high value, low effort
3. **GOG/Ubisoft signal fixes** — quick wins, 25 min total
4. **Exe scoring improvements** — Roman numerals, abbreviations, "crack" penalty
5. Everything else is post-MVP polish

The Python `detect.py` should be kept as a **reference tool** for validation but does not need to be ported 1:1. The C# architecture (separate scanner classes, in-memory cache, GUI integration) is fundamentally different from the Python CLI tool.

**Key insight:** The C# implementation is actually **richer** than Python for BattleNet detection (parent propagation, folder name heuristics, exe name heuristics). The issue is not missing features — it's a skip-list regression that prevents the existing features from running. This is a 10-line fix, not a multi-hour port.
