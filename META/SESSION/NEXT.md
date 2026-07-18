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

---

## Priority Order

### 1. Bug Fixes & Stabilization (P0)

**Known bugs to fix:**
- [ ] **Static vs instance noise check divergence** — `IsNoiseExePattern()` (static, hardcoded 25 patterns) vs `IsNonGameExe()` (instance, full JSON blacklist). `HasRootExecutableSignal()` and `HasUnrealLayoutSignal()` don't see the JSON blacklist. HIGH severity.
- [ ] **Close stale TECH_DEBT entries** — Bugs 1-4 in `META/BACKLOG/TECH_DEBT.md` appear fixed in code but entries were never closed.
- [ ] **Remove dead `CollectAllCommonFolderNames`** in `SteamLibraryScanner.cs` (defined but unused).

**Test gaps to fill (Stabilization):**
- [ ] `SteamLibraryScanner` — zero tests (ACF parsing, cross-library detection, Missing/Orphaned)
- [ ] `VdfParser` — zero tests (malformed input, nested blocks, escape sequences)
- [ ] `BlacklistLoader` — zero tests (loading, parsing, error handling)
- [ ] `IsNoiseExePattern` vs `IsNonGameExe` divergence — needs test proving the bug
- [ ] `ScoreExecutable` — zero tests

### 2. Standalone & Steam Feature Completion (P1)

**Standalone gaps:**
- [ ] Container detection only promotes Tier-1 signals — pure standalone games under container parents are dropped
- [ ] Launcher exe discovery is shallow (root-level only)
- [ ] Scoring ignores JSON blacklist patterns (only ~10 hardcoded launcher patterns penalized)

**Steam gaps:**
- [ ] `FindPrimaryExe` is shallow (root-level only, 7-item noise filter) — misses exes in subfolders
- [ ] Silent `catch { }` blocks — no user feedback on corrupt manifests
- [ ] No cross-library deduplication in `ScanAll`

### 3. EA Install Metadata (Future — Application Feature)
EA install logs contain rich metadata:
- `__Installer/InstallLog.txt` — game name, studio, install path, registry keys, redistributables
- `__Installer/installerdata.xml` — content IDs, title, locale, EULA paths

**Status:** Detection logic done. Application metadata parsing planned for later.

### 4. SCUMMVM/DOSBox Game Category (Future — Application Feature)
GOG SCUMMVM and DOSBox games could have a "Retro" or "DOS Game" category,
similar to how Engine metadata is already tracked.

**Status:** Detection done. Category type planned for application.

### 5. PCGamingWiki Metadata Lookup (P1)

**Status:** Research 100% complete, C# implementation 0%. Full plan at `planning/04-phase-2-metadata-lookup.md`.

**Blocking dependencies:**
- [ ] `GameMetadata` C# model (not defined yet)
- [ ] `IGameMetadataProvider` interface
- [ ] HTTP client infrastructure with rate limiting
- [ ] F3/F4 key conflict resolution (F4 is retag, plan says metadata)

### 6. Multi-Theme System (P2 — nice-to-have)

**Planning doc:** `planning/97-multi-theme-system.md`
- Theme definitions (WindowsCommander, GrayScale)
- ThemeManager + AppConfig persistence
- F2 Settings theme selector

### 8. detect.py Refactoring (P1 — needs planning)

**Problem:** `tools/detect.py` is ~1800 lines. Violates "Avoid Massive Source Files" principle.

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
