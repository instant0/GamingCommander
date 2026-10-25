# detect.py Port Status — C# Parity Analysis

**Status:** REFRESHED 2026-09-06 — Plan 123 T3 (Phase 1 parity audit). Prior review 2026-07-26; many gaps closed since (blizzard skip, GOG `gog_*`/`gog.ico`, Ubisoft loader patterns, crack + roman + abbreviation scoring, engine detection, PE metadata, in-app PCGW).
**Superseded by `docs/detection/07-csharp-parity.md`** (verified 2026-10-25). Keep this file as the historical parity audit; use `docs/detection/07-csharp-parity.md` and `docs/detection/09-blueprint.md` for current state.
**Reference:** `tools/detect.py` (2690 LOC, read in full for this audit)
**C# Scanner Files:** `FolderScanner.cs`, `StoreSignalDetector.cs`, `ExecutableDiscovery.cs`, `FallbackSignalDetector.cs`, `ContainerScanner.cs`, `EngineDetector.cs`, `GogInfoParser.cs`, `LnkParser.cs`, `EpicManifestParser.cs`, `EpicItemCatalog.cs`, `EpicLibraryScanner.cs`, `SteamLibraryScanner.cs`, `PcgwLookup.cs`, `MetadataService.cs`, `TitleText.cs`, `PcgwTitleFilter.cs`

---

## Executive Summary

Parity is now **high for detection signals and exe discovery**; the C# implementation is a superset of Python for store cataloging (Steam ACF VFS, Epic `.item` catalog, registry fallback) and title enrichment (PE FileDescription + ProductName, GOG `.info`, Ubisoft readme, PCGW lookup). Verified current state (2026-10-25):

1. **Non-game folder heuristics — PORTED.** `ContainerScanner.IsNonGameFolder` has all three layers: exact name (`s_nonGameFolderNames` + `FileSystemHelper.NoiseSubDirNames`), child-all-non-game, and file-type analysis (`DetectionNoise.NonGameExe` + meaningful-file check).
2. **Recursive exe fallback — PORTED.** `ExecutableDiscovery.FindExecutablesDeep` / `FindExesRecursive` honour `AppConfig.ScanDepth` (default 3, clamped 1–5), so the 3-level generic subdir fallback is present.
3. **Container deep-signal parity — PRESENT.** `ContainerScanner` promotes children whose only evidence is a deep SteamEmu or Ubisoft-legacy marker; a deep marker is authoritative over the data-only layer.
4. **Remaining intentional differences:** `_has_steam_app_manifest` uses a different mechanism (`SteamLibraryScanner` + `steam_appid.txt`), and the general **deep 4-level store-marker walk** remains intentionally unimplemented (targeted probes + container recursion instead; LOW). `DetectionLogger` remains Python-only (C# status messages; LOW).

Everything else is **port parity** (root signals, exe discovery, exe scoring, .lnk, GOG metadata, engine) or **C#-only enrichment**.

---

## Detailed Feature Matrix

### 1. Root Store Signals (Phase 1)

| # | Signal | detect.py | C# | Status |
|---|--------|-----------|-----|--------|
| 1 | GOG | `goggame.dll`, `goggame-*`, `gog_*`, `gog.ico`, `Launch *.lnk` | Same (goggame*, `gog_*`, `gog.ico`, Launch .lnk) | ✅ PORTED |
| 2 | EA | `__Installer/`, `touchup.exe`, `activationui.exe` | Same + `__Installer` | ✅ PORTED |
| 3 | Ubisoft Emu | `uplay_loader*` + `.ini` with `Username=`/`AccountId=` | Same logic | ✅ PORTED |
| 4 | Ubisoft | `uplay_install.manifest`, `uplay_r*_loader*.dll` | Same + `uplay_download/`, `uplay_install.state`, `*_UPP*.exe` | ✅ PORTED (C# richer) |
| 5 | Epic | `.egstore/` or `.egsstore/` | Same | ✅ PORTED |
| 6 | Blizzard | `.battle.net/` | Same + `.build.info`, `.product.db` | ✅ PORTED (C# richer) |
| 7 | Xbox | `default-metadata.json` | Same | ✅ PORTED |
| 8 | Rockstar | `title.rgl` | Same | ✅ PORTED |
| 9 | Steam Emu | `steam_api64.dll` / `steam_api.dll` | Same (strong) + `steam_appid.txt` (weak) | ✅ PORTED (C# richer) |
| 10 | (C# only) | — | Registry fallback (EA/Ubi/GOG/Rockstar) | ➕ C# ADDED |

**Priority order matches** in both (GOG → EA → Ubi-Emu → Ubi → Epic → Blizzard → Xbox → Rockstar → SteamEmu).

### 2. Exe Scoring (`_pick_best_root_exe` / `_pick_primary_executable` vs `ScoreExecutable`)

| Feature | detect.py | C# | Status |
|---------|-----------|-----|--------|
| Single exe shortcut | Returns immediately | Returns immediately | ✅ |
| Folder token extraction | Splits `_`, `-`, space | Splits space, `_`, `-`, `.`, `:` | ✅ (C# richer) |
| "copy of" penalty | `-30` | `-25` (or `-25` for `-copy`) | ⚠️ DIFF (minor) |
| "org" group heuristic | `-40` if clean exe exists else `-20` | `-25` for `org_`/`-org-`/`^\d+-\d+-org`, `-15` for `original` | ⚠️ DIFF (C# static tier, Python conditional) |
| "crack" penalty | `-25` | `-25` | ✅ |
| "launcher" penalty | `-20` | `-20` via launcherPatterns | ✅ |
| `_TOOL_NAMES` | 16 tool names `-25` | Tiered blacklist `-10`…`-30` | ✅ (C# tier-based) |
| Folder token match | `+10` per token | `+10` per token | ✅ |
| Exact stem match | `+15` token-exact | `+40` folder-name exact / platform-stripped; `+15` bidirectional substring | ✅ (C# stronger, ADR-012) |
| Abbreviation match | `+8` short stems sharing first letter | `+8` ordered abbreviation (2–4 chars) | ✅ |
| Roman numeral match | `+12` bidirectional | `+12` bidirectional | ✅ |
| Small exe penalty | `-15` (<100KB), `-5` (<500KB) | `-15` (<100KB) | ⚠️ DIFF (C# lacks `-5` tier) |
| File size bonus | `+min(size/10M, 10)` | `+min(size/20M, 5)` | ⚠️ DIFF (C# capped lower) |
| UE shipping/win64 bonus | `+5` | `+28` shipping name, `+12` binaries path, `+5` win64 | ✅ (C# stronger) |
| PE metadata | `+15` desc match, `+10` product match (top-3 only) | `-25` noise desc, `-20` noise internal name, `+10` retail/client/shipping; FileDescription captured for display | ⚠️ DIFF (different strategy — C# penalizes noise, does not reorder by desc match) |
| Classic names | — | `game`/`start`/`play` `+18` | ➕ C# ADDED |

**Note:** Python `_pick_best_root_exe` (root exes) and `_pick_primary_executable` (deep exes + PE tiebreak) are **two different scorers**. C# has one unified `ScoreExecutable` used by `FindPrimaryExecutable`. This is a structural improvement, not a gap.

### 3. Exe Discovery (`_find_game_executables` / `_find_exe_in_subdirs` vs `FindExecutablesDeep`)

| Feature | detect.py | C# | Status |
|---------|-----------|-----|--------|
| Root-level exes | Yes | Yes | ✅ |
| Immediate child exes | Yes | Yes | ✅ |
| UE `Binaries/{Win64,Win32,Steam,Linux}` in children | Win64/WinGDK in `_find_game_executables`; Win32/Steam/Linux in `_find_exe_in_subdirs` | Win64/Win32/WinGDK/Steam (no Linux) | ✅ (Linux intentionally dropped) |
| `child/bin/` (older UE) | Only in `_find_exe_in_subdirs` UE3 path | Always checked | ✅ (C# richer) |
| 2-level recursive fallback | `_add_exes_recursive(max_depth=2)` when root has no exes | `FindExesRecursive` when no candidates | ✅ |
| 3-level generic subdir scan (deep) | `_find_exe_in_subdirs` generic fallback (3 levels) | `FindExecutablesDeep` recursive fallback honours `AppConfig.ScanDepth` (default 3, clamp 1–5) | ✅ PORTED — ScanDepth covers the generic subdir fallback |
| Launcher-at-root deeper find | `_find_exe_in_subdirs` when root exe is launcher | `FindLauncherExecutable` + deep candidate scan always runs | ✅ (C# always deep-scans) |
| Noise dir skip | `_is_noise_dir` substring | `IsNoiseDirectory` + `NoiseSubDirNames` | ✅ |
| Forbidden launch exe | `unins`/`uninstall`/`unwise` via noise | `IsForbiddenLaunchExe` (unins*/uninstall/unwise) + noise | ✅ (C# has extra hard guard) |

**Key finding for Plan 123 S2/S4:** the generic subdir fallback is **ported** — `FindExecutablesDeep` probes root + children + UE paths + `bin/`, then falls back to `FindExesRecursive` bounded by `AppConfig.ScanDepth` (default 3, clamped 1–5). The earlier cap/depth claim was a documentation error; it is not a remaining gap.

### 4. .lnk Resolution

| Feature | detect.py | C# | Status |
|---------|-----------|-----|--------|
| Binary read + latin-1 decode | `read_bytes()` + `decode("latin-1")` | `File.ReadAllBytes` + `Encoding.Latin1` | ✅ |
| Regex for exe names | Same pattern | Same pattern | ✅ |
| Skip DLLs | `steam_api*.dll`, `eos.dll`, `upc.dll` | Same set | ✅ |
| Longest name heuristic | `max(candidates, key=len)` | Picks longest | ✅ |
| Subdir search (3 levels) | `os.walk` depth 3 | `FindExesInSubdirs` maxDepth 3 | ✅ |
| Backup fuzzy match (`-Name.exe`, `copy of Name.exe`) | Prefix checks | Same | ✅ |

**FULLY PORTED.**

### 5. GOG Metadata Extraction

| Feature | detect.py | C# | Status |
|---------|-----------|-----|--------|
| Search scope | Root + 1 level non-noise subdirs | Same | ✅ |
| `.info` glob | `goggame-*.info` | Same | ✅ |
| Main game preference | `gameId == rootGameId` | Same | ✅ |
| DLC fallback | First non-main | Same | ✅ |
| playTasks (primary exe + args) | `isPrimary` + `path` + `arguments` | Same | ✅ |
| Exe resolution | Relative path kept | Resolved to absolute | ✅ (C# improvement) |

**FULLY PORTED.**

### 6. Container Detection (Phase 3)

| Feature | detect.py | C# | Status |
|---------|-----------|-----|--------|
| Child store signal check | `_scan_root` per child | `StoreSignalDetector.DetectType` | ✅ |
| Child exe check (non-data) | `c_has_exe and not _is_non_game_folder` | `HasRootExecutableSignal` + `HasUnrealLayoutSignal` | ✅ |
| Publisher folder (no files at root) | 2-level deep scan for grandchild exes | `files.Length == 0 && dirs > 0` → recurse | ✅ |
| Organization recursion | Always recurses into all children | ≥2 game children → recurse; ≥1 → promote standalone | ⚠️ DIFF (C# stricter — bounded depth 1, signal-count gated) |
| Non-game subfolder names | `_NON_GAME_DIR_NAMES` + `_NON_GAME_SUBDIR_NAMES` | `s_nonGameFolderNames` + `NoiseSubDirNames` | ⚠️ DIFF (C# list is larger; Python set differs) |
| `"blizzard"` in skip lists | Not present | Removed (Plan 114/116) | ✅ ALIGNED |
| **Child-all-non-game check** | If ALL children are non-game subdirs → skip | `ContainerScanner.IsNonGameFolder` layer 2 | ✅ PORTED |
| **File-type analysis** | No non-noise exe + no meaningful file → not a game | `ContainerScanner.IsNonGameFolder` layer 3 (`DetectionNoise.NonGameExe`) | ✅ PORTED |
| **Deep child signals** | SteamEmu / Ubisoft-legacy / root `.lnk` markers at depth | `FallbackSignalDetector.HasSteamEmuDeepSignal` etc.; deep marker beats data-only | ✅ PORTED |

**Findings for Plan 123 S1/S2/S3:** Python container logic is depth-unbounded (recurses into all children); C# is deliberately stricter (≥2 game children to recurse, depth 1). The `_is_non_game_folder` layers are **ported**, so C# rejects installer/data-only folders (`arc-install` shape) rather than promoting them, and container deep-signal parity is present.

### 7. Deep Signal Scan (Phase 2)

| Feature | detect.py | C# | Status |
|---------|-----------|-----|--------|
| Walk depth | 4 levels, `.exe/.dll/.ini` filtered | Targeted probes + container recursion | ⚠️ INTENTIONAL DIFF (LOW) — general 4-level store-marker walk intentionally absent |
| `steam_emu.ini` | Root, child, UE Steamworks | Same (root, child, UE Steamworks) | ✅ |
| `UbiStats.dll` legacy | Root or child | Same | ✅ |
| `_has_steam_app_manifest` (`steamapps/` or `.acf`) | Steam-Emu signal | Handled by `SteamLibraryScanner`; C# adds `steam_appid.txt` weak signal | ⚠️ DIFF (equivalent intent, different mechanism) |
| `_match_markers` (deep marker match) | GOG/EA/Ubi/Epic/Steam-Emu from walk | Covered by targeted probes/registry/container deep signals | ⚠️ INTENTIONAL DIFF (LOW) — no general walk |

**Finding:** the 4-level walk is the **only** way Python finds store markers buried arbitrarily deep. C# relies on root signals + targeted probes + container recursion + deep SteamEmu/Ubisoft-legacy signals, which covers the corpus. The remaining difference is the general deep walk itself — LOW priority and an intentional performance trade-off; it is tracked in `docs/detection/09-blueprint.md` §14.

### 8. Non-Game Folder Detection

| Feature | detect.py | C# | Status |
|---------|-----------|-----|--------|
| Exact name match | `_NON_GAME_DIR_NAMES` (16) | `s_nonGameFolderNames` (30+) + `NoiseSubDirNames` | ✅ (C# list larger) |
| Child-all-non-game check | Present | `ContainerScanner.IsNonGameFolder` layer 2 | ✅ |
| File-type analysis (`_NON_GAME_FILE_EXTS`, `_SUPPORT_FILE_EXTS`) | Present | `ContainerScanner.IsNonGameFolder` layer 3 (`DetectionNoise.NonGameExe`) | ✅ |

**All three layers are present** — the child-all-non-game and file-type layers are the false-positive suppressors for "folder as game title" complaints.

### 9. Engine Detection

| Feature | detect.py | C# | Status |
|---------|-----------|-----|--------|
| Unreal | `Engine/` + `Binaries/` or child `Binaries/Win64` | Same (Engine/Binaries or `<name>/Binaries/Win64`) | ✅ |
| Unity | `UnityPlayer.dll` + `*_Data` | Same | ✅ |
| RAGE | `title.rgl` + `common.rpf` | Same | ✅ |
| Frostbite | `Engine.BuildInfo_Win64_retail.dll` | Same | ✅ |
| Enum kinds | — | Unreal/Unity/Rage/Frostbite/Source/Godot/CryEngine | ➕ C# richer (display tags) |

**PORTED** (Plan 102 Phase 2).

### 10. PE Metadata & Title Enrichment

| Feature | detect.py | C# | Status |
|---------|-----------|-----|--------|
| PE read | `pefile` optional | `System.Diagnostics.FileVersionInfo` (built-in) | ✅ |
| Fields read | FileDescription, ProductName, OriginalFilename, CompanyName | FileDescription, ProductName, InternalName + ProductYear | ✅ (C# richer) |
| Enrichment target | Phase 4 unknowns only (`--metadata`) | Scan-time display name + F3/queue | ✅ (C# richer, guarded) |
| GOG `.info` title | Tier 1 GOG only | Same via `GogInfoParser` | ✅ |
| Ubisoft readme title | — | `UbisoftReadmeParser` (deny-list guarded) | ➕ C# ADDED |
| PCGW lookup | `--pcgw` Phase 4 (rate-limited 0.6s) | `PcgwLookup` in-app + `MetadataLookupQueue` + `PcgwTitleFilter` | ✅ (C# richer, no app GraphQL) |

### 11. What Python has that C# does not (remaining gaps)

| Gap | Priority | Status / mapping |
|-----|----------|------------------|
| Deep 4-level signal walk + `_match_markers` | LOW | Open, intentional (perf). Tracked in `docs/detection/09-blueprint.md` §14 |
| `DetectionLogger` | LOW | Open, diagnostics only. C# status messages are sufficient for the shipped app |
| `-5` small-exe tier (<500KB) | LOW | Open, minor scoring diff |
| SteamEmu false-positive refinement | P1 | Open design call (see blueprint §14) |

The 3-level generic exe fallback and the `_is_non_game_folder` child-all-non-game/file-type layers are **ported** and are no longer gaps.

### 12. What C# has that Python does not (C#-only features)

| Feature | Where |
|---------|-------|
| Steam ACF catalog VFS (Installed/Moved/Missing/Orphaned) | `SteamLibraryScanner` |
| Epic ProgramData `.item` catalog + Installed/Missing/Orphaned | `EpicItemCatalog`, `EpicLibraryScanner`, `EpicOrphanDiscovery` |
| Epic binary `.manifest` header read | `EpicBinaryManifest` |
| Epic identification `.item` write (`.mancpn`/`.ovt` recovery) | `EpicItemWriter` |
| Registry fallback (EA/Ubi/GOG/Rockstar) | `FallbackSignalDetector`/registry reader |
| In-app PCGW metadata + extras (F3/F4) | `MetadataService`, `PcgwLookup`, `MetadataStore` |
| PE ProductYear guess + FileDescription display guard | `PeProductYear`, `ExecutableDiscovery` |
| Ubisoft readme title enrichment | `UbisoftReadmeParser` |
| Tiered blacklist scoring + embedded blacklist restore | `BlacklistLoader`/`BlacklistData` |
| Identity pipeline (`TitleText.LookupName`, `SearchQueries`, `PcgwTitleFilter`) | `TitleText`, `PcgwTitleFilter` |
| Container depth bound + game-signal-count gating | `ContainerScanner` |

---

## Answer to the equivalence question

**C# is not strictly "equal" to Python — it is a superset in product features and now matches Python's false-positive controls.** The C# implementation:

- ✅ Detects **more stores** (registry fallback, BattleNet `.build.info`, Ubisoft `_UPP`) than Python.
- ✅ Catalogs **Steam and Epic at the manifest level** (Python cannot — it's a folder scanner only).
- ✅ Scores executables with **more signals** (PE noise penalties, shipping bonus, ADR-012 exact-match precedence).
- ✅ Enriches titles **more aggressively and more safely** (guarded PE description, GOG, Ubisoft readme, PCGW).
- ✅ **Rejects non-game folders by name, child composition, and file type** — all three `IsNonGameFolder` layers are ported.
- ✅ **Finds deep exes** via the `ScanDepth`-bounded recursive fallback and container recursion; only the general 4-level store-marker walk remains intentionally absent.

**Verdict:** the earlier high-value parity gaps (non-game layers, 3-level fallback) are **closed**. The remaining Python-only items are LOW and intentional: the general deep store-marker walk and `DetectionLogger`. Exe-scoring deltas are minor (small-exe tier, size cap).

---

## Remaining port priorities (if ever needed)

1. **LOW — deep marker walk** — only if corpus evidence shows buried store signals matter; measure perf first.
2. **LOW — exe-scoring micro-diffs** (`-5` tier, size cap, copy-of `-30`) — only with corpus evidence.
3. **LOW — `DetectionLogger`** — diagnostics convenience, not a detection behavior gap.

No behavior change is made by this audit. `docs/detection/07-csharp-parity.md` is the canonical current-state reference.