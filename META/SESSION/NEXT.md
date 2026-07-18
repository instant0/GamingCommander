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

---

## Priority Order

### 1. Phase D: Complexity Reduction (P0 — Current Focus)

**Goal:** Reduce mental complexity, eliminate duplication, improve naming, add documentation, proactively split files.
**Plan:** `planning/10-phase-d-complexity-reduction.md`
**Task Template:** `META/TASKS/TEMPLATE/task-template.md`

**Layers (15 tasks, ~9 hours):**

| Layer | Tasks | Focus |
|-------|-------|-------|
| 1 — Shared Utilities | T16-T18 | Extract FileSystemHelper, JsonFileHelper, AvailableTypes |
| 2 — Naming & Docs | T19-T22 | Rename variables, add XML docs, consolidate noise checks |
| 3 — Proactive Splits | T23-T28 | Extract StoreSignalDetector, ExecutableDiscovery, SteamAcfParser, KeyboardDispatcher, HelpDialogBuilder, ShellDetailsViewModel |
| 4 — Pattern Extraction | T29-T30 | Extract folder-picker pattern, integrate JsonFileHelper |

**Start with:** T16 (Extract FileSystemHelper) — no prerequisites, highest impact

### 2. Phase E: Stabilization & Test Coverage (P1)

**Goal:** Fix known bugs, close tech debt, fill test gaps.
**Plan:** `planning/11-phase-e-stabilization.md`

**Layers (10 tasks, ~6.5 hours):**

| Layer | Tasks | Focus |
|-------|-------|-------|
| 1 — Tech Debt | T31 | Close stale TECH_DEBT entries 1-5 |
| 2 — Bug Fixes | T32-T33 | Fix BlacklistLoader tiers (Bug 6), ScoreExecutable blacklist (Bug 7) |
| 3 — Critical Tests | T34-T37 | VdfParser, BlacklistLoader, SteamLibraryScanner, noise-check regression |
| 4 — Secondary Tests | T38-T40 | ScoreExecutable, GameEntryId, GamesDatabaseService |

### 3. Standalone & Steam Feature Completion (P2)

**Standalone gaps:**
- [ ] Container detection only promotes Tier-1 signals — pure standalone games under container parents are dropped
- [ ] Launcher exe discovery is shallow (root-level only)
- [ ] Scoring ignores JSON blacklist patterns (only ~10 hardcoded launcher patterns penalized) — FIXED in T30

**Steam gaps:**
- [ ] `FindPrimaryExe` is shallow (root-level only, 7-item noise filter) — misses exes in subfolders
- [ ] Silent `catch { }` blocks — no user feedback on corrupt manifests
- [ ] No cross-library deduplication in `ScanAll`

### 4. EA Install Metadata (Future — Application Feature)
EA install logs contain rich metadata:
- `__Installer/InstallLog.txt` — game name, studio, install path, registry keys, redistributables
- `__Installer/installerdata.xml` — content IDs, title, locale, EULA paths

**Status:** Detection logic done. Application metadata parsing planned for later.

### 5. SCUMMVM/DOSBox Game Category (Future — Application Feature)
GOG SCUMMVM and DOSBox games could have a "Retro" or "DOS Game" category,
similar to how Engine metadata is already tracked.

**Status:** Detection done. Category type planned for application.

### 6. PCGamingWiki Metadata Lookup (P2)

**Status:** Research 100% complete, C# implementation 0%. Full plan at `planning/04-phase-2-metadata-lookup.md`.

**Blocking dependencies:**
- [ ] `GameMetadata` C# model (not defined yet)
- [ ] `IGameMetadataProvider` interface
- [ ] HTTP client infrastructure with rate limiting
- [ ] F3/F4 key conflict resolution (F4 is retag, plan says metadata)

### 7. Multi-Theme System (P3 — nice-to-have)

**Planning doc:** `planning/97-multi-theme-system.md`
- Theme definitions (WindowsCommander, GrayScale)
- ThemeManager + AppConfig persistence
- F2 Settings theme selector

### 8. detect.py Refactoring (P2 — needs planning)

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
