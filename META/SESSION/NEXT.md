# META/SESSION/NEXT.md — Next Action

**Nature:** Scratch. **Overwritten** every session handoff.
**Audience:** Builder. Read before implementing.

---

## ✅ COMPLETED

### Theme Extraction
All hardcoded colors/fonts centralized to `App.axaml` Application.Resources with semantic names. `AppTheme.cs` provides code-behind access. All 4 windows + code-behind files fully converted.

### VFS Display Enhancements (Plan 96)
- Missing game detection (ACF-expects-but-missing) — `SteamLibraryScanner` reverse ACF lookup
- Cross-library mismatch display — `AcfExpectedPath`, `PlatformStatusDetail`
- Left-pane list coloring — `ItemStatusColor` + `HexToBrushConverter` binding

### Unified Detection Tool (Plan 98)
Merged `detect_folder.py` + `list_standalone_games.py` → single `tools/detect.py`. 9 store signals, engine detection, GOG metadata, Phase 4 enrichment (PE + PCGW). D drive: 57s→5.4s.

### Detection Hardening (Plan 99 — Partially Complete)
- Exe scoring with backup/tool penalties, folder name matching, Roman numerals
- Container detection with launcher dir exclusions
- Non-game classification (reshade, mod managers, data subdirs)
- Noise precision: explicit crashreport/crashhelper/crashdebug (not wildcard "crash")
- EA signals: +touchup.exe, activationui.exe
- GOG signals: +gog.ico, .info subdirs
- `.lnk` parser: exe name extraction + backup rename matching (-Penumbra.exe)
- UE-aware exe discovery: Engine/ → */Binaries/Win64/
- `--log FILE` flag for detailed detection logs
- Result: 157 games, 0 no-exe, 0 unknowns

### Documentation & Code Structure Cleanup (T01–T15)
- Phase A: Deleted stale docs, updated README, archived old plans, cleaned ghost references, fixed test counts, updated CODE_MAP
- Phase B: Added XML docs to 4 Core interfaces, 8 models, 5 services, 6 ViewModels, 2 Migration files
- Phase C: Split 3 multi-type Core files (4 new files), extracted BlacklistData, extracted 2 ViewModel nested types

### Complexity Reduction — Layers 1-2 (T16–T22)
- T16: Extracted `FileSystemHelper.cs` — shared filesystem utilities (GetDirectoriesSafe, GetFilesSafe, GetLastWriteTimeSafe, NormalizeDisplayName)
- T17: Extracted `JsonFileHelper.cs` — shared JSON read/write with parameterized options; integrated into 3 services
- T18: Extracted `GameSourceParser.AvailableTypes` — single definition in Core; removed from 4 App files
- T19: Renamed ambiguous variables across 6 files (p→pattern, a/b→versions, sid/eid→steamAppId/epicCatalogItemId, etc.)
- T20: Added XML docs to ~20 public members across 8 files
- T21: Deleted dead `IsNoiseExePattern`; renamed `IsNonGameExe`→`IsNoiseExeByPath`; added XML docs to noise-check methods
- T22: Unified `NormalizeDisplayName` in FileSystemHelper; removed from FolderScanner + SteamLibraryScanner

### Complexity Reduction — Proactive Splits (T23–T29)
- T23: Extracted `StoreSignalDetector.cs` — DetectType + 10 signal methods from FolderScanner
- T24: Extracted `ExecutableDiscovery.cs` — FindExecutablesDeep, ScoreExecutable, FindPrimaryExecutable, FindLauncherExecutable, ExeNameMatchesFolderName, FindEpicManifest from FolderScanner
- T25: Extracted `SteamAcfParser.cs` — ParseAcfFile, DiscoverLibraryPaths, NormalizePath, AcfInfo record from SteamLibraryScanner
- T27: Extracted `HelpDialogBuilder.cs` — ShowHelpAsync(Window) from MainWindow (107 lines)
- T26: Skipped — overengineered (two 10-case switch statements are clear as-is)
- T28: Skipped — high risk, low value (ShellViewModel under 500-line limit, XAML binding changes error-prone)
- T29: Skipped — trivial (LibraryManager.NormalizeLibraryRoot already exists, duplicate check is 1 line)

---

## Priority Order

### 1. Phase D: Complexity Reduction — ✅ COMPLETE

**Goal:** Reduce mental complexity, eliminate duplication, improve naming, add documentation, proactively split files.
**Plan:** `planning/10-phase-d-complexity-reduction.md`

**Layers (15 tasks, ~9 hours):**

| Layer | Tasks | Status |
|-------|-------|--------|
| 1 — Shared Utilities | T16-T18 | ✅ Complete |
| 2 — Naming & Docs | T19-T22 | ✅ Complete |
| 3 — Proactive Splits | T23-T28 | ✅ Complete (T26, T28 skipped) |
| 4 — Pattern Extraction | T29-T30 | ✅ Complete (T29 skipped, T30 merged into T17) |

### 2. Phase E: Stabilization & Test Coverage — ✅ COMPLETE (T31-T40)

**Goal:** Fix known bugs, close tech debt, fill test gaps.
**Plan:** `planning/11-phase-e-stabilization.md`

**Layers (10 tasks, ~6.5 hours):**

| Layer | Tasks | Status |
|-------|-------|--------|
| 1 — Tech Debt | T31 | ✅ Complete |
| 2 — Bug Fixes | T32-T33 | ✅ Complete |
| 3 — Critical Tests | T34-T37 | ✅ Complete |
| 4 — Secondary Tests | T38-T40 | ✅ Complete |

### 3. Phase F: Docs & Bug Fixes (T41-T47) — ✅ COMPLETE

**Goal:** Fix documentation drift, close stale task files, fix Process handle leak, remove dead code.
**Tasks:** `META/TASKS/phase-f-docs-and-bugfixes/`

| Layer | Tasks | Status |
|-------|-------|--------|
| 1 — Documentation | T41-T44 | ✅ Complete |
| 2 — Bug Fixes | T45 | ✅ Complete |
| 3 — Cleanup | T46-T47 | ✅ Complete |

### 4. Phase G: Code Quality & Tests (T48-T57) — NEXT (T58-T60 complete)

**Goal:** Fill critical test gaps, extract constants, add debug logging, clean up deprecated project.
**Tasks:** `META/TASKS/phase-g-code-quality-and-tests/`

| Layer | Tasks | Status |
|-------|-------|--------|
| 1 — Tests | T48-T50, T56 | ⏳ Pending |
| 2 — Code Quality | T51-T55 | ⏳ Pending |
| 3 — Cleanup | T57 | ⏳ Pending |
| 4 — Naming & Docs | T58-T60 | ✅ Complete |

### 5. Standalone & Steam Feature Completion (P2) — FUTURE

**Status:** Needs planning before implementation. No plan exists yet.

**Standalone gaps:**
- [ ] Container detection only promotes Tier-1 signals — pure standalone games under container parents are dropped
- [ ] Launcher exe discovery is shallow (root-level only)

**Steam gaps:**
- [ ] No cross-library deduplication in `ScanAll`
- [ ] Silent `catch { }` blocks — no user feedback on corrupt manifests

### 6. EA Install Metadata (Future — Application Feature)
EA install logs contain rich metadata:
- `__Installer/InstallLog.txt` — game name, studio, install path, registry keys, redistributables
- `__Installer/installerdata.xml` — content IDs, title, locale, EULA paths

**Status:** Detection logic done. Application metadata parsing planned for later.

### 7. SCUMMVM/DOSBox Game Category (Future — Application Feature)
GOG SCUMMVM and DOSBox games could have a "Retro" or "DOS Game" category,
similar to how Engine metadata is already tracked.

**Status:** Detection done. Category type planned for application.

### 8. PCGamingWiki Metadata Lookup (P2)

**Status:** Research 100% complete, C# implementation 0%. Full plan at `planning/04-phase-2-metadata-lookup.md`.

**Blocking dependencies:**
- [ ] `GameMetadata` C# model (not defined yet)
- [ ] `IGameMetadataProvider` interface
- [ ] HTTP client infrastructure with rate limiting
- [ ] F3/F4 key conflict resolution (F4 is retag, plan says metadata)

### 9. Multi-Theme System (P3 — nice-to-have)

**Planning doc:** `planning/97-multi-theme-system.md`
- Theme definitions (WindowsCommander, GrayScale)
- ThemeManager + AppConfig persistence
- F2 Settings theme selector

### 10. detect.py Refactoring (P2 — needs planning)

**Problem:** `tools/detect.py` is ~1829 lines. Violates "Avoid Massive Source Files" principle.

**Needs planning for:**
- Split into modules (detection/, scoring/, logging/)
- Extract reusable functions
- Keep CLI interface clean
- Don't break existing functionality

**Status:** Not started. Plan needed before implementation.

---

## Key Architecture Decisions
- **Four Steam statuses: Installed/Moved/Orphaned/Missing** — "Missing" = ACF exists, game files gone from all libraries
- **HexToBrushConverter returns TextPrimary for empty** — non-game items keep default color
- **Theme centralized in App.axaml** — 23 color brushes + 8 font sizes as Application.Resources
