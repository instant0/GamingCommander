# META/SESSION/CURRENT.md — Current Project State

**Nature:** Scratch. **Overwritten** every session handoff.
**Audience:** All agents. Read every session.

---

## Phase
**MVP track active** — Plan [`planning/100-mvp-next-steps.md`](../../planning/100-mvp-next-steps.md).  
Code quality Phases D–G largely done (T58–T60 complete; T48–T57 deferred until MVP gate).

## Priority Roadmap
1. **MVP ship gate (P0)** — Fix launch pipeline, first-run defaults, C# detection parity, Windows smoke → see Plan 100
2. **Standalone & Steam completion (P1)** — remaining container/launcher edges after MVP
3. **Phase G quality (P2)** — T48–T57 tests/constants after MVP
4. **PCGamingWiki Metadata (P2)** — Research complete, C# 0%
5. **Multi-Theme / .NET 9 (P3)** — Nice-to-have

## Objectives Achieved
1. ✅ Game detection overhaul — Phase 1+2 complete
2. ✅ Build versioning + re-wizard system (0.3.0)
3. ✅ In-memory VFS cache for GamesDatabaseService
4. ✅ Keyboard layout overhaul (10 F-keys, Enter=launch, Esc=up, double-tap)
5. ✅ Bug fixes & cleanup — Plan 95 complete
6. ✅ Theme extraction — all hardcoded colors/fonts centralized
7. ✅ VFS display enhancements — Plan 96 complete
8. ✅ Documentation & code structure cleanup — T01–T15 complete (3 phases: docs, XML docs, code splits)
9. ✅ Complexity reduction — T16–T22 complete (Phase D: shared helpers, extracted constants, unified methods, XML docs, JSON I/O)
10. ✅ Code quality — T58–T60 complete (naming fixes, XML docs, noise-check consolidation)

## Known Issues Found During Investigation

### Bugs
- ~~**Static vs instance noise check divergence (HIGH)**~~ ✅ Fixed by T21 — dead `IsNoiseExePattern()` deleted; all callers use `IsNoiseExeName`/`IsNoiseExeByPath` backed by full JSON blacklist.
- **Blacklist tier flattening (MEDIUM)** — 21 tiers reduced to flat list at load; can't apply per-tier severity.
- **Scoring ignores JSON blacklist (MEDIUM)** — Only ~10 hardcoded launcher patterns penalized.
- **Stale TECH_DEBT entries** — Bugs 1-4 appear fixed in code but entries never closed.

### Test Coverage Gaps
- `StoreSignalDetector` — zero tests (10 detection signals)
- `LibraryManager` — zero tests (central orchestrator)
- `GameSourceParser` — zero tests (string↔enum conversion)
- `JsonConfigService` — zero tests (settings persistence)

## Completed This Session

### Unified Game Detection Tool (Plan 98 — Complete)
Merged `detect_folder.py` and `list_standalone_games.py` into a single `tools/detect.py`:
- **Fast-then-deep architecture** — Phase 1 root scan (single os.scandir) → Phase 2 deep signal scan (unknowns only, .exe/.dll/.ini filtered) → Phase 3 container check → Phase 4 optional enrichment
- **9 store signals** — GOG, EA, Ubisoft Emulator, Ubisoft, Epic, Blizzard, Xbox, Rockstar, Steam Emulator
- **Deep signals** — steam_emu.ini (UE ThirdParty path), UbiStats.dll (legacy Ubisoft)
- **Engine detection** — Unreal Engine, Unity, RAGE, Frostbite (fast root-level probes)
- **GOG metadata extraction** — .info file parsing, main game preference, primary exe from playTasks
- **Noise filtering** — merged 21-tier exe blacklist (90+ patterns) + directory blacklist
- **Phase 4 enrichment** — PE metadata extraction (--metadata), PCGamingWiki lookup (--pcgw)
- **Performance** — E drive: 1.3s (was 2.5s), D drive: 5.4s (was 57s with detect_folder.py)
- **Old scripts deprecated** — `detect_folder.py` and `list_standalone_games.py` marked deprecated but kept for reference

### Detection Hardening (Plan 99 — Partially Complete)
**Exe scoring:**
- Backup penalties (`org_`, `copy`, `original`) → -20 to -30
- Tool penalties (20+ patterns) → -25
- Folder name matching (+10), exact match (+15), abbreviation (+8), Roman numeral (+12)
- Size heuristics (< 100KB → -15)

**Container detection:**
- Store/publisher folders (Blizzard/, UBI/, Epic Games/) correctly detected
- Launcher directories added to SKIP_NAMES
- Container recursion with `container=True` flag to filter data-only subfolders

**Non-game classification:**
- Added: `wiiu`, `reshade`, `sweetfx`, `enbseries`, `enb`, mod managers, data subdirs
- Removed "server" from noise (Minecraft_Server.exe is valid)
- Changed "crash" to explicit entries: "crashreport", "crashhelper", "crashdebug", "crashlog"

**Store signals:**
- EA: Added `touchup.exe` and `activationui.exe` alongside `__Installer`
- GOG: Added `gog.ico`, `.info` files searched in root + subdirs
- Store signal returns now preserve exe info (was hardcoded to False)

**`.lnk` shortcut parsing:**
- Extracts exe name from raw .lnk bytes
- Searches all subdirs (3 levels) for exact match
- Handles backup renames (`-Penumbra.exe` matches `PENUMBRA.EXE`)
- Prefers exact match over backup

**UE-aware exe discovery:**
- When `Engine/` detected, searches `*/Binaries/Win64/` first
- Falls back to generic 3-level scan for non-UE games
- Handles UE3 (`Binaries/Win32/`), UE4-5 (`GameName/Binaries/Win64/`), Steam (`*/Binaries/Steam/`)

**GOG `.info` metadata:**
- Searches root + one level of subdirs for `goggame-*.info`
- Extracts `playTasks[].path` (exe) and `playTasks[].arguments` (launch args)
- Used as fallback when root/lnk/subdir scans fail

**Detection log:**
- `--log FILE` flag writes detailed detection log
- Shows: Phase 1 root scan, .lnk parsing, tier classification, subdir scan results, skip reasons

**Results:** 157 games (120 D + 37 E), 0 with no exe, 0 unknowns

### Theme Extraction (Complete)
All hardcoded colors and font sizes centralized to `App.axaml` Application.Resources:
- **App.axaml** — 23 `SolidColorBrush` resources + 8 font size resources with semantic names (WindowBg, PaneBg, TextPrimary, etc.)
- **AppTheme.cs** — static accessor class for code-behind files (resolves resources at runtime via `TryFindResource`)
- **MainWindow.axaml** — all hardcoded values replaced with `{DynamicResource ...}` bindings
- **WizardWindow.axaml** — fully converted
- **GameSetupWindow.axaml** — fully converted
- **LibrarySetupWindow.axaml** — fully converted
- **MainWindow.axaml.cs** — `ShowHelpAsync()` uses `AppTheme.*` for all colors/fonts
- **WizardWindow.axaml.cs** — `RenderEntries()` uses `AppTheme.*`
- **GameSetupWindow.axaml.cs** — `RenderFields()`, `MakeFieldRow()`, `MakeComboRow()` use `AppTheme.*`
- **LibrarySetupWindow.axaml.cs** — `RenderRoots()` uses `AppTheme.*`
- **HexToBrushConverter.cs** — empty string returns `TextPrimary` (default for non-game items); hex strings parsed to SolidColorBrush
- **NortonCommander.axaml** — standalone theme file (kept as reference but App.axaml has direct resources for runtime access)

### VFS Display Enhancements (Plan 96 — Complete)

**Bundle A: Missing game detection (ACF-expects-but-missing)**
- `SteamLibraryScanner.Scan()`/`ScanAll()` — after the common/ scan loop, iterates ALL ACFs and checks if each installdir exists in any library's common/. Creates "Missing" GameEntry for ACFs with no matching folder.
- `CreateMissingAcfEntry()` — builds a GameEntry from ACF metadata alone (no directory to read)
- `Extra["SteamStatus"] = "Missing"` — new status value alongside Installed/Moved/Orphaned
- `App.axaml` + `NortonCommander.axaml` — added `StatusMissing` brush resource

**Bundle B: Cross-library mismatch display**
- `SteamLibraryScanner.CreateEntry()` — Moved games now store `AcfExpectedPath` in Extra (the path the ACF expects the game to be at)
- `ShellPaneItemViewModel.PlatformStatusDetail` — new field for richer status text
- `ShellViewModel.LoadGamesForRoot()` — computes detail text: "Moved — ACF expects: D:\..." or "Missing — ACF exists but game files not found"
- `MainWindow.axaml` — new status detail row in right-pane details panel (below Status row)

**Bundle C: Left-pane list coloring**
- `ShellPaneItemViewModel.ItemStatusColor` — new field, hex color for the game title foreground
- `ShellViewModel.LoadGamesForRoot()` — maps Moved→yellow, Orphaned/Missing→red, Installed/empty→default
- `MainWindow.axaml` left-pane ListBox — Title TextBlock bound to `ItemStatusColor` via `HexToBrushConverter`
- `HexToBrushConverter` — empty string now returns `TextPrimary` (so non-game items keep default color)

### Code Quality — T58–T60:
- T58: Fixed 37 naming inaccuracies (Priority 1: public API renames — `Override`→`IsSourceOverridden`, `CmdlineArgs`→`CommandLineArguments`, `Extra`→`PlatformMetadata`, `Path`→`RootPath`, `Type`→`OverrideType`, `Size`→`SizeInBytes`, `Compute`→`ComputeId`, `AvailableTypes`→`SourceDisplayNames`; Priority 2: private field renames — `_db`→`_databaseService`, `idx`/`gIdx`→`rootIndex`/`gameIndex`, `_original`→`_originalGame`, `fieldIdx`→`fieldIndex`, `bgColor`/`textColor`→`backgroundBrush`/`bodyBrush`, VdfParser `idx`/`pos`→`lineIndex`/`charPos`, BlacklistLoader abbreviations→full names). JSON serialization boundary preserved via DTOs.
- T59: Added 21 XML docs to private methods across FolderScanner (8), SteamLibraryScanner (5), ExecutableDiscovery (2), BlacklistData (1), AppTheme (2), GameSetupWindow (2), MigrationMode (1).
- T60: Consolidated noise-check duplication into `FileSystemHelper.IsNoiseExeName` and `FileSystemHelper.IsNoiseDirectory`. Updated FolderScanner and ExecutableDiscovery to delegate to FileSystemHelper. SteamLibraryScanner kept separate (intentionally different 7-item subset).

### MVP — T61: Fix Launch Target Resolution (Complete)
- Added `CommandLineArguments` property to `ShellPaneItemViewModel`
- `LoadGamesForRoot()` now resolves `LaunchTarget`: prefers `steam://` URI over raw `ExecutablePath` when `CommandLineArguments` starts with `steam://`
- `LaunchSelectedGameAsync()` now passes `CommandLineArguments` as `ProcessStartInfo.Arguments` for non-URI launches (guard prevents passing URI as args)
- Steam games launch via protocol; standalone games launch with stored args
- Build clean, 99 tests passing

### MVP — T62: Fix Launch Execution (Complete)
- Updated no-exe guard: message now reads `"No launch target for {item.Title}"` with proper null handling
- Moved `args` computation before URI/filesystem branching so it's available for status display
- Status bar now shows `"Launching: {target} {args}"` when args are present
- URI launches do not set `Arguments` (the URI is the entire target)
- Fixed CS8602 nullable warning introduced by including `item.Title` in the status message
- Build clean, 99 tests passing

### MVP — T63: Launch Pipeline Tests (Cancelled)
- Cancelled: launch resolution logic is a single ternary; testing via ShellViewModel requires Avalonia runtime; extraction to pure function not worth the refactor for 2 lines of code

### MVP — T64: First-Run Config Defaults (Complete)
- Fixed `JsonConfigService.Load()` to check `File.Exists` before read — `IsFirstRun` now correctly returns `true` when `settings.json` doesn't exist
- Created `JsonConfigServiceTests.cs` with 3 tests: missing file → IsFirstRun=true, save+load → IsFirstRun=false, missing games.json → empty database
- Build clean, 102 tests passing

### MVP — T65: GOG goggame-*.info Parser (Complete)
- Created `GogInfoParser.cs` — parses `goggame-*.info` JSON for title, exe, args, game ID
- Searches root + 1 level of non-noise subdirs; filters DLC by `gameId == rootGameId`
- Resolves relative exe paths to absolute via `Path.GetFullPath(Path.Combine(...))`
- Integrated into `FolderScanner.AddGameEntry()` — GOG .info enriches title, exe (fallback), args, and `PlatformMetadata` (`GogGameId`, `TitleSource`, `AutoDetectedTitle`)
- Created `GogInfoParserTests.cs` — 10 tests covering parsing, paths, DLC, edge cases
- Build clean, 112 tests passing

### Documentation & Code Structure Cleanup (T01–T15 — Complete)

**Phase A — Documentation Cleanup (T01–T07):**
- T01: Deleted stale `AGENTS.md.old`
- T02: Updated `README.md` with current phase, layout, build instructions
- T03: Archived `.sisyphus/plans/` → `META/COMPLETED/.sisyphus-plans-archive/`
- T04: Replaced 1340-line stale `docs/FEATURES.md` with 12-line redirect stub
- T05: Cleaned ILauncher ghost references in 4 files
- T06: Fixed test count 18→17 in 2 files
- T07: Updated `META/CODE_MAP.md` Python tools table

**Phase B — XML Documentation (T08–T12):**
- T08: Added `/// <summary>` to 4 Core interfaces
- T09: Added XML docs to 8 Core model files
- T10: Added XML docs to 5 App service files
- T11: Added XML docs to 6 ViewModel files
- T12: Added XML docs to 2 Migration files

**Phase C — Code Structure (T13–T15):**
- T13: Split `GameEntry.cs` → `GameRoot.cs` + `GamesDatabase.cs`; split `LibraryRoot.cs` → `FolderOverride.cs`; split `FileSystemEntry.cs` → `FileSystemEntryKind.cs` (4 new files)
- T14: Extracted `BlacklistData` record from `BlacklistLoader.cs` → `BlacklistData.cs`
- T15: Extracted `LibraryRootEntry` and `WizardLibraryEntry` to their own files (2 new files)

**Phase D — Complexity Reduction (T16–T22):**
- T16: Extracted `FileSystemHelper.cs` — shared `GetDirectoriesSafe`, `GetFilesSafe`, `GetLastWriteTimeSafe` (removed from FolderScanner + SteamLibraryScanner)
- T17: Extracted `JsonFileHelper.cs` — shared `ReadFromFile<T>`/`WriteToFile<T>` with parameterized options; integrated into GamesDatabaseService, JsonConfigService, BlacklistLoader
- T18: Extracted `GameSourceParser.AvailableTypes` — single definition in Core; removed from 4 App files
- T19: Renamed ambiguous variables — `p`→`pattern`/`steamPath`, `a/b`→`leftVersion/rightVersion`, `sid/eid`→`steamAppId/epicCatalogItemId`, `ov`→`folderOverride`, `swPath/svDir`→`steamworksPath/steamworksVersionDir`
- T20: Added XML docs to ~20 public members across 8 files (GameSetupWindow, WizardWindow, LibrarySetupWindow, MainWindow, HexToBrushConverter, BlacklistLoader, ShellViewModel, Program)
- T21: Deleted dead `IsNoiseExePattern`; renamed `IsNonGameExe`→`IsNoiseExeByPath`; kept `IsNoiseExeName` with XML docs
- T22: Unified `NormalizeDisplayName` — extracted to FileSystemHelper with suffix stripping; removed from FolderScanner + SteamLibraryScanner

**Phase D — Proactive Splits (T23–T29):**
- T23: Extracted `StoreSignalDetector.cs` — `DetectType` + 10 signal methods (GOG, EA, Ubisoft, Epic, Blizzard, Xbox, Rockstar, Steam, SteamEmu) from FolderScanner
- T24: Extracted `ExecutableDiscovery.cs` — `FindExecutablesDeep`, `ScoreExecutable`, `FindPrimaryExecutable`, `FindLauncherExecutable`, `ExeNameMatchesFolderName`, `FindEpicManifest` from FolderScanner. Moved `NoiseSubDirNames` to FileSystemHelper.
- T25: Extracted `SteamAcfParser.cs` — `ParseAcfFile`, `DiscoverLibraryPaths`, `NormalizePath`, `AcfInfo` record from SteamLibraryScanner
- T27: Extracted `HelpDialogBuilder.cs` — `ShowHelpAsync(Window)` from MainWindow (107 lines)
- T26: Skipped — overengineered (two 10-case switch statements are clear as-is)
- T28: Skipped — high risk, low value (ShellViewModel under 500-line limit, XAML binding changes error-prone)
- T29: Skipped — trivial (LibraryManager.NormalizeLibraryRoot already exists, duplicate check is 1 line)

**Phase E — Stabilization (T31–T40):**
- T31: Verified Bugs 1-5 in TECH_DEBT.md are fixed, added verification dates
- T32: Added `BlacklistTierEntry` record, `TieredExePatterns` property to BlacklistData, updated BlacklistLoader to populate tiered entries
- T33: Updated `ScoreExecutable` to accept noise patterns and tier lookup, added tier-based penalty logic
- T34: Created VdfParserTests.cs — 20 tests covering basic parsing, edge cases, error handling, Steam formats, ExtractFields
- T35: Created BlacklistLoaderTests.cs — 11 tests covering loading, pattern verification, tier preservation, error handling
- T36: Created SteamLibraryScannerTests.cs — 14 tests covering scanning, ACF parsing, cross-library detection, status fields, ScanAll
- T37: Added 3 noise-check regression tests for Bug 5 to ScannerFilterTests.cs
- T38: Created ExecutableScoringTests.cs — 10 tests covering token matching, launcher penalties, noise tier penalties, shipping/Win64 bonuses, file size, combined factors
- T39: Created GameEntryIdTests.cs — 8 tests covering determinism, uniqueness, format, edge cases
- T40: Created GamesDatabaseServiceTests.cs — 16 tests covering Load/Save, CRUD, caching, rescan, multi-root isolation

### MVP — T66: UE-Aware Executable Discovery (Complete)
- Replaced hardcoded Win64/WinGDK probes with platform loop (`Win64`, `Win32`, `WinGDK`, `Steam`) matching `detect.py _find_game_executables` behavior
- Added `child/bin/` probe for older UE games (Gothic, Jagged Alliance)
- Added `FindExesRecursive` (maxDepth=2) for BioShock pattern (root with no exes, scan subdirs)
- Created `ExecutableDiscoveryTests.cs` — 15 tests covering Win64, Win32, WinGDK, Steam, child/bin, recursive fallback, noise filtering, multiple platforms
- Build clean, 107 tests passing

### MVP — T67: .lnk Shortcut Exe Resolution (Complete)
- Created `LnkParser.cs` — parses .lnk binary files via latin-1 decode + regex to extract exe names
- `ResolveExeFromLnk` searches root + subdirs (3 levels) for the target exe, handles backup renames
- Integrated into `FolderScanner.AddGameEntry()` as fallback when primary exe discovery fails
- Created `LnkParserTests.cs` — 13 tests covering extraction, resolution, fuzzy matching, edge cases
- Build clean, 107 tests passing
- Replaced hardcoded Win64/WinGDK probes with platform loop (`Win64`, `Win32`, `WinGDK`, `Steam`) matching `detect.py _find_game_executables` behavior
- Added `child/bin/` probe for older UE games (Gothic, Jagged Alliance)
- Added `FindExesRecursive` (maxDepth=2) for BioShock pattern (root with no exes, scan subdirs)
- Fixed C#12 collection expression `[...]` syntax incompatibility with net8.0 test project
- Created `ExecutableDiscoveryTests.cs` — 15 tests covering Win64, Win32, WinGDK, Steam, child/bin, recursive fallback, noise filtering, multiple platforms
- Build clean, 128 tests passing

## Test Status
**107 tests passing** (33 Core + 1 Migration + 73 App). Build clean, 0 errors, 0 warnings.

## Next Session Notes
- **MVP plan written** — `planning/100-mvp-next-steps.md`; session NEXT re-aimed
- **P0 product bug FIXED (T61+T62):** `LaunchTarget` now resolves `steam://` URI when present in `CommandLineArguments`; standalone launch args passed to `Process.Start`; status bar shows args when present. T63 (tests) remains.
- **detect.py** (~1829 LOC) is reference gold; C# needs GOG playTasks / UE paths / container parity before considering Python split
- Phase G T48–T57 deferred until MVP acceptance criteria pass

## Key Architecture Decisions
- **Theme centralized in App.axaml** — All 23 color brushes and 8 font sizes live as Application.Resources with semantic names (e.g. `TextAccent`, `ButtonBgAction`). `AppTheme.cs` provides static accessor for code-behind. To re-theme: swap the resources in App.axaml. `NortonCommander.axaml` retained as reference.
- **AppTheme name (not Theme)** — Avoids collision with `Avalonia.Controls.Theme` which is a type in Avalonia's namespace.
- **SolidColorBrush in resources (not Color)** — `DynamicResource` bindings for `Background`/`Foreground` require brush types, not plain `Color`.
- **Four Steam statuses: Installed/Moved/Orphaned/Missing** — "Missing" is the reverse of "Orphaned": ACF exists but game files are gone from all libraries. Detection is a second pass in SteamLibraryScanner that iterates ACFs rather than directories.
- **HexToBrushConverter returns TextPrimary for empty** — Ensures non-game items (directories, "..") keep default text color when bound to ItemStatusColor.
- **ILauncher retired** — ADR-008 described `ILauncher` but the pragmatic two-tier scanner architecture (`LibraryManager` → `FolderScanner`/`SteamLibraryScanner`) replaced it.
- **GameSourceParser** in Core — shared by all ViewModels that need to convert between display strings and `GameSourceKind` enum values.
- **GameEntryId** in Core — single source of truth for deterministic game entry IDs.
- **MainWindow stores LibraryManager** — eliminates repeated construction of temporary LibraryManager instances.

---

**Next session: Read META/SESSION/NEXT.md before starting.**
