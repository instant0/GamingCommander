# META/SESSION/CURRENT.md — Current Project State

**Nature:** Scratch. **Overwritten** every session handoff.
**Audience:** All agents. Read every session.

---

## Phase
**Stabilization & Feature Completion** — Bug fixes, test coverage, Standalone/Steam gaps, metadata lookup

## Priority Roadmap
1. **Bug Fixes & Stabilization (P0)** — Fix noise check divergence, close stale tech debt, fill test gaps
2. **Standalone & Steam Completion (P1)** — Container detection, exe discovery, scoring, error handling
3. **PCGamingWiki Metadata (P1)** — Research complete, C# implementation at 0%
4. **Multi-Theme System (P2)** — Nice-to-have, downprioritized
5. **.NET 9 SDK Upgrade (P3)** — Lowest priority

## Objectives Achieved
1. ✅ Game detection overhaul — Phase 1+2 complete
2. ✅ Build versioning + re-wizard system (0.3.0)
3. ✅ In-memory VFS cache for GamesDatabaseService
4. ✅ Keyboard layout overhaul (10 F-keys, Enter=launch, Esc=up, double-tap)
5. ✅ Bug fixes & cleanup — Plan 95 complete
6. ✅ Theme extraction — all hardcoded colors/fonts centralized
7. ✅ VFS display enhancements — Plan 96 complete
8. ✅ Documentation & code structure cleanup — T01–T15 complete (3 phases: docs, XML docs, code splits)

## Known Issues Found During Investigation

### Bugs
- **Static vs instance noise check divergence (HIGH)** — `IsNoiseExePattern()` uses only 25 hardcoded patterns; `IsNonGameExe()` uses full JSON blacklist. Presence detection misses JSON patterns.
- **Blacklist tier flattening (MEDIUM)** — 21 tiers reduced to flat list at load; can't apply per-tier severity.
- **Scoring ignores JSON blacklist (MEDIUM)** — Only ~10 hardcoded launcher patterns penalized.
- **Stale TECH_DEBT entries** — Bugs 1-4 appear fixed in code but entries never closed.

### Test Coverage Gaps
- `SteamLibraryScanner` — zero tests
- `VdfParser` — zero tests
- `BlacklistLoader` — zero tests
- `GameEntryId` — zero tests
- `LibraryManager` — zero tests
- `GamesDatabaseService` — zero tests

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

## Test Status
**17 tests passing** (5 Core + 1 Migration + 11 App). Build clean, 0 errors. 4 Avalonia AVLN3001 warnings (cosmetic).

## Next Session Notes
- **detect.py refactoring needed** — ~1800 lines, needs planning for module split
- Detection hardening partially complete — FFXIV merge, launcher vs game exe choice still pending

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
