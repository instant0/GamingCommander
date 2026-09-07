# Detection Logic — 07: C# ↔ Python Parity (current gaps)

**Reference:** `planning/103-detect-py-port-status.md` (detailed matrix) + 2026-09-07 changes.
**Audience:** Anyone porting Python fixes to C#.

---

## 1. The relationship

- **C# is a superset** in product features (Steam ACF VFS, Epic `.item` catalog + orphan discovery,
  registry fallback, in-app PCGW, PE ProductYear, tiered blacklist, Ubisoft readme titles).
- **Python is the reference** for the generic detection path (signal chain, container logic,
  exe scoring). Every behavior change is validated in Python first, then ported.

---

## 2. Python fixes applied 2026-09-07 that MUST be ported to C#

**STATUS: ALL PORTED (2026-09-07).** The following fixes were implemented in C# and verified
with new parity tests (`tests/GamingCommander.App.Tests/PythonParityDetectionTests.cs`, 10 tests).

| Fix | Python location | C# location | Status |
|-----|-----------------|-------------|--------|
| **Terminating rule (T1.5)** | `_find_exact_folder_match`, gate in `scan_directory` | `ExecutableDiscovery.FindExactFolderMatch` + `FolderScanner.Scan` Pass 1.5 | ✅ PORTED |
| **Whole-name match (GAP A)** | `_find_exact_folder_match` token + whole-name, backup-guard | `FindExactFolderMatch` (token + whole-name normalized + backup guard) | ✅ PORTED |
| **Parent-bound promotion (E4/E5)** | `_PARENT_BOUND_CHILD_DIRS` + parent-wins gate | `ExecutableDiscovery.FindParentBoundExe` + `FolderScanner.Scan` Pass 1.6 | ✅ PORTED |
| **UE-wrapped container recognition (GAP B)** | deep container check reuses `_find_exe_in_subdirs` | `FallbackSignalDetector.HasUnrealLayoutSignal` (already covered Binaries/Win64 + Engine layouts) | ✅ VERIFIED |
| **BattleNet `.build.info`/`.product.db`/`.patch.result`** | `_scan_root` → Blizzard | `StoreSignalDetector.HasBlizzardSignal` (.battle.net, .build.info, .product.db, + .patch.result) | ✅ PORTED |
| **Store tier preservation** | stage-13 no longer downgrades Secure | `FolderScanner.Scan` ordering (store Pass 1/1c wins before Pass 1.5) | ✅ VERIFIED |
| **`epicgames` noise refinement** | `_is_noise_exe` special-case | `FileSystemHelper.IsNoiseExeName` (launcher/bootstrap variants only) | ✅ PORTED |
| **UE-aware non-game filter (E3)** | `_platform_child_has_game_exe` 2-level descent | `ContainerScanner.IsNonGameFolder` layers 2-3 (child-all-non-game + file-type) + store-marker guard | ✅ PORTED |

New C# tests (PythonParityDetectionTests.cs): Neverwinter terminating rule (nested GameClient
excluded), Diablo III whole-name (x64 backup not primary), Dead Space 3 normalized match, Penumbra
backup-dash exclusion + redist promotion, Ashen UE-wrap, epicgames noise refinement,
.patch.result BattleNet, data-only folder skip.

---

## 3. Remaining C# gaps vs Python (from parity doc, 2026-09-06)

**Updated 2026-09-07:** the two HIGH gaps are now closed (non-game layers + parent-bound/terminating).
Remaining gaps are LOW/intentional.

| Gap | Python | C# | Impact |
|-----|--------|-----|--------|
| **3-level generic subdir exe fallback** | `_find_exe_in_subdirs` generic (3 levels) | `FindExecutablesDeep` capped at 2 | Missed deep-nested exes (S2/S4 shapes) — MED |
| **Deep 4-level signal walk** | `_deep_signal_scan` + `_match_markers` | Targeted probes only | Missed buried store markers — LOW (intentional perf tradeoff) |
| **`DetectionLogger`** | `_detlog` | Status messages only | Diagnostics parity — LOW |

---

## 4. C# features Python lacks

| Feature | Where |
|---------|-------|
| Steam ACF catalog VFS (Installed/Moved/Missing/Orphaned) | `SteamLibraryScanner` |
| Epic ProgramData `.item` catalog + Installed/Missing/Orphaned | `EpicItemCatalog`, `EpicLibraryScanner` |
| Epic binary `.manifest` header read | `EpicBinaryManifest` |
| Registry fallback (EA/Ubi/GOG/Rockstar) | `FallbackSignalDetector` |
| In-app PCGW metadata + extras (F3/F4) | `MetadataService`, `PcgwLookup` |
| Tiered blacklist scoring + embedded restore | `BlacklistLoader` |
| Ubisoft readme title enrichment | `UbisoftReadmeParser` |
| Container depth bound + game-signal-count gating | `ContainerScanner` |

---

## 5. Port order (Phase 4, agreed 2026-09-07)

1. **Proven Python fixes first** (highest value, corpus-verified):
   E4 parent-bound, E5 redist fallback, deterministic tie-break, collection rules, terminating
   rule + GAP A/B/C, E2 BattleNet typing, E3 UE-aware paths.
2. Then remaining accepted experiments (E6a deferred, E6/E7/E8 C#-side).

---

## 6. Current verification state (2026-09-07)

- **Python validation matrix: 22/22 PASS** (`tools/validate_probe_matrix.py`)
- **Python baseline: 13 scenarios, 0 missed / 0 wrong-folder / 0 FP** (`tools/baseline_compare.py`)
- **Detection-pattern test vs original corpus: 1 issue** (was 29) — 100% noise + scoring + backup
  accuracy (`tools/test_detection_patterns.py`)
- **C# full suite: 549 tests, 0 failures** (402 App + 146 Core + 1 Migration), including 10 new
  Python-parity tests
- **C# build: 0 errors** (4 pre-existing XAML warnings)