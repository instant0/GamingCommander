# META/BACKLOG/TECH_DEBT.md — Technical Debt & Known Issues

**Nature:** Mutable. Entries appended by Builder/Reviewer, moved to PLANNING/ when prioritized.
**Last verified:** 2026-07-26

---

## C# Detection Bugs (Found During Phase 1.2 Research)

### Bug 1: GOG detection checks exact filename
- **Discovered:** 2026-Q1
- **Where:** FolderScanner.DetectType()
- **Issue:** Checks for `goggame.info` (exact filename match). Real GOG files are `goggame-<id>.info` — prefix match needed.
- **Impact:** GOG games not detected.
- **Suggested fix:** Change to prefix match: `Path.GetFileName(f).StartsWith("goggame-")`
- **Status:** ✅ Fixed — `HasGogSignal()` now uses `GetFilesSafe(dir, "goggame*")` (prefix match)
- **Verified:** 2026-07-19 — StoreSignalDetector.cs line 66

### Bug 2: EA detection needs __Installer/ directory check
- **Discovered:** 2026-Q1
- **Where:** FolderScanner.DetectType()
- **Issue:** Checks for `eaapp_` prefix / `.ea.web` / folder name. Real EA installs have a `__Installer/` directory.
- **Impact:** EA games not detected correctly.
- **Suggested fix:** Check for `__Installer/` subdirectory as primary EA signal.
- **Status:** ✅ Fixed — `HasEaSignal()` now checks for `__Installer/` directory
- **Verified:** 2026-07-19 — StoreSignalDetector.cs line 72

### Bug 3: Ubisoft detection needs uplay_install.manifest check
- **Discovered:** 2026-Q1
- **Where:** FolderScanner.DetectType()
- **Issue:** Checks for `ubisoft game launcher url` / folder name. Real detection needs `uplay_install.manifest` or `uplay_r*_loader*.dll`.
- **Impact:** Ubisoft games not detected correctly.
- **Suggested fix:** Check for `uplay_install.manifest` file or `uplay_r*_loader*.dll` pattern.
- **Status:** ✅ Fixed — `HasUbisoftSignal()` now checks for `uplay_install.manifest`
- **Verified:** 2026-07-19 — StoreSignalDetector.cs line 111

### Bug 4: Recursive Directory.GetFiles performance issue
- **Discovered:** 2026-Q1
- **Where:** FolderScanner.DetectType()
- **Issue:** `Directory.GetFiles("*", SearchOption.AllDirectories)` is recursive — should use root-level scan.
- **Impact:** Slow scanning on large game folders.
- **Suggested fix:** Use `SearchOption.TopDirectoryOnly` for initial scan.
- **Status:** ✅ Fixed — all `Directory.GetFiles` calls use `TopDirectoryOnly`
- **Verified:** 2026-07-19 — 12 occurrences across 5 files all use TopDirectoryOnly

---

## Active Bugs (Found 2026-07-17)

### Bug 5: Static vs instance noise check divergence
- **Discovered:** 2026-07-17
- **Where:** `FolderScanner.cs` — `IsNoiseExePattern()` (static, line 594) vs `IsNonGameExe()` (instance, line 605)
- **Issue:** Two parallel noise-check codepaths don't share the same data. `IsNoiseExePattern()` is static and uses only 25 hardcoded patterns (`DefaultNoiseExePatterns`). `IsNonGameExe()` is an instance method using the full JSON blacklist (130+ patterns across 21 tiers). `HasRootExecutableSignal()` and `HasUnrealLayoutSignal()` call the static version, so they miss JSON-only patterns like "blender", "python", "scummvm", "server", "editor".
- **Impact:** HIGH — Folders with only JSON-blacklisted exes (but not hardcoded ones) are incorrectly detected as game folders.
- **Suggested fix:** Make `IsNoiseExePattern()` instance (or pass the full pattern list) so presence detection uses the same data as candidate filtering.
- **Status:** ✅ Fixed — Added `IsNoiseExeName()` instance method using full `_noiseExePatterns`. `HasRootExecutableSignal()` and `HasUnrealLayoutSignal()` now non-static, call `IsNoiseExeName()`. `DetectFallbackType()` also made non-static to support the call chain.
- **Verified:** 2026-07-19 — FolderScanner.cs lines 224, 241, 303

### Bug 6: Blacklist tier information discarded after loading
- **Discovered:** 2026-07-17
- **Where:** `BlacklistLoader.cs` line 46-48
- **Issue:** All 21 tiers of `exe_name_patterns` are flattened into a single `IReadOnlyList<string>`. Tier severity (universal noise vs store bootstraps vs anticheat) is lost. Scoring cannot differentiate between a "tier_1 universal noise" exe and a "tier_17 store bootstrap" exe.
- **Impact:** MEDIUM — Less accurate exe scoring and filtering.
- **Status:** ✅ Fixed — Added `BlacklistTierEntry` record, `TieredExePatterns` property to `BlacklistData`, `GetTieredTiers()` method in `BlacklistLoader`, `GetExePatternTier()` in `FolderScanner`
- **Verified:** 2026-07-19 — BlacklistData.cs, BlacklistLoader.cs, FolderScanner.cs

### Bug 7: Exe scoring ignores JSON blacklist patterns
- **Discovered:** 2026-07-17
- **Where:** `FolderScanner.cs` line 524
- **Issue:** `ScoreExecutable()` only penalizes ~10 hardcoded `_launcherPatterns` (e.g. "launcher", "updater", "bootstrapper"). JSON blacklist patterns like "patch", "activate", "trial", "config" are not penalized in scoring.
- **Impact:** MEDIUM — Suboptimal primary exe selection.
- **Status:** ✅ Fixed — Updated `ScoreExecutable` signature to accept `noiseExePatterns` and `tierLookup` parameters. Added tier-based penalty logic (-5 to -30 based on tier severity).
- **Verified:** 2026-07-19 — ExecutableDiscovery.cs lines 94-138

---

## Phase 1.1 Known Issues

### UI Command Buttons Are Decorative
- **Discovered:** 2026-04-17 (Phase 1.1 completion)
- **Where:** MainWindow command bar
- **Issue:** All command buttons have `IsHitTestVisible="False"` — cannot be clicked. F5 (launch) and F7 (add root) have been removed entirely; F4/F6/F9/F10 buttons exist but are not clickable.
- **Status:** Open

### Default settings/games files not created alongside exe
- **Discovered:** 2026-04-17
- **Where:** App startup
- **Issue:** Default `settings.json` and `games.json` should be created alongside exe for clean installs.
- **Status:** ✅ Fixed — T64: `JsonConfigService.Load()` checks `File.Exists` before read; `IsFirstRun` returns true when settings.json missing.
- **Verified:** 2026-07-26

---

## EA Format Caveat
- **Discovered:** 2026-Q1
- **Where:** docs/research/ea_format.md
- **Issue:** EA format doc based on staged install only — needs verification against a complete EA game install.
- **Status:** Open

---

## SDK Upgrade
- **Discovered:** 2026
- **Where:** Project-wide
- **Issue:** Currently on .NET 8. Upgrade to .NET 9 documented at `planning/90-sdk-upgrade.md` but not planned — lowest priority, deferred indefinitely.
- **Status:** Deferred (not planned)

---

## Post-MVP Bugs (Found 2026-07-26 Windows Testing)

### Bug 8: F6 Rescan crashes application (CRITICAL)
- **Discovered:** 2026-07-26
- **Where:** `MainWindow.axaml.cs` → `RefreshCurrentRootAsync()` → `LibraryManager.Refresh()` → `ContainerScanner.ScanContainerChildren()`
- **Issue:** Multiple crash paths in F6 rescan:
  1. **ContainerScanner line 104-105:** Raw `child.GetFiles("*", SearchOption.TopDirectoryOnly)` and `child.GetDirectories()` without try-catch. Throws `UnauthorizedAccessException` on locked/permission-restricted directories (common on Windows with game folders, OneDrive junctions, symlinks).
  2. **GamesDatabaseService.RescanRoot line 137:** `ToDictionary(g => g.Id)` throws `ArgumentException` if duplicate IDs exist in database (corruption or race condition).
  3. **No top-level try-catch** in `RefreshCurrentRootAsync` — any exception propagates to `async void OnKeyDown` and terminates the process.
  4. **No re-entrancy guard** — pressing F6 twice rapidly causes concurrent `Refresh()` calls racing on `_cachedDb`.
- **Impact:** CRITICAL — application crashes on F6 rescan with existing library configuration.
- **Suggested fix:**
  1. Wrap `ContainerScanner.ScanContainerChildren` lines 104-105 in try-catch or use `FileSystemHelper` safe wrappers.
  2. Add top-level try-catch in `RefreshCurrentRootAsync` around both branches.
  3. Add `_isRefreshing` re-entrancy guard to prevent concurrent rescans.
  4. Replace `ToDictionary` with `ToLookup` or manual loop with duplicate handling.
  5. Add per-root try-catch in `LibraryManager.Refresh()` so one failing root doesn't skip the rest.
- **Status:** ✅ Fixed — 2026-07-26. F6→F5 rebinding done simultaneously.
  - Added `FileSystemHelper.GetFilesSafe()` for safe file enumeration
  - `ContainerScanner.ScanContainerChildren` lines 104-105 now use `FileSystemHelper.GetFilesSafe` + `GetDirectoriesSafe`
  - `GamesDatabaseService.RescanRoot` uses manual dictionary loop instead of `ToDictionary` (handles duplicate IDs)
  - `LibraryManager.Refresh` has per-root try-catch (one failing root doesn't skip others)
  - `MainWindow.RefreshCurrentRootAsync` has `_isRefreshing` re-entrancy guard + top-level try-catch
  - F6 rescan keybind moved to F5 (universal refresh convention)
  - 2 new tests added for duplicate ID handling

### Bug 9: Battle.net detection fails — Diablo III detected as Standalone
- **Discovered:** 2026-07-26
- **Where:** `FileSystemHelper.cs` line 22, `FolderScanner.cs` lines 100-101
- **Issue:** `"blizzard"` is hardcoded in `FileSystemHelper.NoiseSubDirNames`. When the library root is `d:\games\`, the `blizzard\` subdirectory is **skipped entirely** by the noise filter before any store signal detection runs. Diablo III is never discovered.
- **If root is `d:\games\blizzard\`:** Diablo III is discovered but `HasBlizzardSignal` checks for `.battle.net/` directory which often doesn't exist in game folders. Parent propagation also fails if `blizzard\` doesn't contain `.battle.net/`. Falls through to `FallbackSignalDetector.HasRootExecutableSignal` → classified as Standalone.
- **Impact:** HIGH — BattleNet games not detected when under a publisher folder. T75 BUG-9 fix is effectively bypassed by the noise filter.
- **Root cause:** Two blocking points:
  1. `NoiseSubDirNames.Contains("blizzard")` — skips the entire directory
  2. `HasBlizzardSignal` requires `.battle.net/` which doesn't always exist
- **Suggested fix:**
  1. Remove `"blizzard"` from `NoiseSubDirNames` — it's a publisher container, not noise.
  2. Remove `"blizzard"` from `ContainerScanner.s_nonGameFolderNames`.
  3. Add name-based BattleNet detection fallback (parent folder name matches known store names).
- **Status:** ✅ Fixed — `"blizzard"` and `"battle.net"` removed from `NoiseSubDirNames` and `ContainerScanner.s_nonGameFolderNames`.
- **Verified:** 2026-07-26

### Bug 10: "Steam Controller Config" listed as a game (Orphaned)
- **Discovered:** 2026-07-26
- **Where:** Steam library scanning
- **Issue:** `Steam Controller Configs` folder in Steam library appears as an "Orphaned" game entry. This is a Steam internal folder, not a game.
- **Impact:** LOW — noise entry in game list.
- **Suggested fix:** Add `"steam controller configs"` to `FileSystemHelper.NoiseSubDirNames` or `SteamLibraryScanner` skip list.
- **Status:** Open

### Bug 11: Orphaned status meaning unclear
- **Discovered:** 2026-07-26
- **Where:** UI display
- **Issue:** User confused about "Orphaned" vs "Missing" status. "Orphaned" means: physical folder exists but no ACF file references it. "Missing" means: ACF exists but game files not found. The distinction is not explained anywhere in the UI.
- **Impact:** LOW — UX confusion.
- **Suggested fix:** Add tooltip or status detail text explaining the status. Already partially addressed in `PlatformStatusDetail` but may not be visible enough.
- **Status:** Open — **Plan 108: `planning/108-steam-status-messages.md`** — actionable guidance with fix instructions

### Bug 12: Library type selector too small to show full text
- **Discovered:** 2026-07-26
- **Where:** Wizard / F2 Library Setup window
- **Issue:** ComboBox for library type (Standalone, Battle.net, etc.) is too narrow to show the full text.
- **Impact:** LOW — cosmetic.
- **Suggested fix:** Set `MinWidth` on the ComboBox or use a wider layout.
- **Status:** Open

### Bug 13: Noise filter gaps — common runtime folders not filtered
- **Discovered:** 2026-07-26
- **Where:** `FileSystemHelper.NoiseSubDirNames`, `FolderScanner.IsGameFolder()`
- **Issue:** Several common non-game directories are not in the noise filter and get scanned as potential games: `DirectX`, `VCRedist`, `redistributable`, `dotnet`, `jdk`. Also `Steam Controller Configs` is a Steam internal folder that appears as an "Orphaned" game (Bug 10).
- **Impact:** LOW — noise entries in game list.
- **Suggested fix:** Add these entries to `FileSystemHelper.NoiseSubDirNames`:
  - `"steam controller configs"`, `"steam"`, `"directx"`, `"vcredist"`, `"redistributable"`, `"dotnet"`, `"jdk"`
- **Status:** Open — addressed in Plan 107

### Bug 14: Wizard + F2 are duplicate setup screens
- **Discovered:** 2026-07-26
- **Where:** `WizardWindow` / `WizardViewModel` vs `LibrarySetupWindow` / `LibrarySetupViewModel`
- **Issue:** Two separate setup screens with ~60-70% overlapping logic. Wizard bypasses `ILibraryManager` — creates its own `FolderScanner` directly. F2 uses `ILibraryManager` properly. Wizard has online metadata toggle, scan progress badges. F2 has existing-root loading and empty-state messaging. User correctly asks: "Why do we have two different setup screens that are supposed to do the same thing?"
- **Impact:** MEDIUM — maintenance burden, inconsistent behavior, confused users.
- **Suggested fix:** Merge into single `LibrarySetupWindow` (F2). Add Wizard's missing features (metadata toggle, scan progress badges) to F2. Delete Wizard.
- **Status:** ✅ Fixed — Plan 106 implemented. Wizard + F2 merged into single `LibrarySetupWindow`. WizardWindow, WizardViewModel, WizardLibraryEntry deleted.

### Bug 15: Orphaned vs Missing status semantics not documented in UI
- **Discovered:** 2026-07-26
- **Where:** UI status display
- **Issue:** User confused about what "Orphaned" vs "Missing" means. **Orphaned** = physical folder exists but no ACF file references it (common for manually installed games or leftover folders). **Missing** = ACF file exists but game files not found on disk. The distinction is only partially explained in `PlatformStatusDetail` and not visible in the main game list.
- **Impact:** LOW — UX confusion.
- **Suggested fix:** Add tooltip or status detail text. Consider renaming to clearer labels (e.g., "Unlinked" instead of "Orphaned", "Files Missing" instead of "Missing").
- **Status:** Open

### Bug 16: blacklist.json ships with user data instead of app directory
- **Discovered:** 2026-07-26
- **Where:** `data/blacklist.json` — loaded from user's data directory
- **Issue:** `blacklist.json` is a shipped reference file, not user data. When the user deleted their `data/` folder to reset, they had to re-copy `blacklist.json` back. Should load from `AppContext.BaseDirectory` (alongside the exe) instead.
- **Impact:** MEDIUM — confusing for users who manage their data folder.
- **Suggested fix:** Move `blacklist.json` loading to `AppContext.BaseDirectory`. Keep `data/` directory for user-only data (settings, games DB).
- **Status:** Open

### Bug 17: FindEpicManifest searches wrong file extension
- **Discovered:** 2026-07-26
- **Where:** `ExecutableDiscovery.FindEpicManifest()` line 379
- **Issue:** Searches `*.json` but Epic uses `.item` and `.mancpn` extensions. The `.json` search returns no results for most Epic games.
- **Impact:** HIGH — Epic manifest path is never populated; no metadata can be extracted.
- **Suggested fix:** Change `GetFiles("*.json")` to search `*.item`, `*.mancpn`, and `*.json` in that order of preference.
- **Status:** ✅ Fixed — Plan 109 Phase 1. `FindEpicManifest()` now searches `*.item`, `*.mancpn`, `*.json` in preference order.
- **Verified:** 2026-07-26 — ExecutableDiscovery.cs lines 364-392

### Bug 18: Epic manifest data never extracted
- **Discovered:** 2026-07-26
- **Where:** `FolderScanner.AddGameEntry()` line 201
- **Issue:** `FindEpicManifest()` returns a file path, but no data is parsed from it. `ManifestPath` is stored on `GameEntry` but `DisplayName`, `CatalogItemId`, `CatalogNamespace`, `LaunchExecutable` are never extracted.
- **Impact:** HIGH — Epic games show codename/folder names instead of marketing names. No store IDs available for cross-referencing.
- **Suggested fix:** Implement `EpicManifestParser` (Plan 109 Phase 2) to parse `.item`/`.mancpn` files and extract metadata; integrate into FolderScanner (Phase 4).
- **Status:** ✅ Fixed — Plan 109 Phases 2+4. `EpicManifestParser.ExtractLocalIdentifiers()` + `FolderScanner` integration. `EpicCatalogItemId`, `EpicCatalogNamespace`, `EpicAppName` stored in `PlatformMetadata`.
- **Verified:** 2026-07-26 — FolderScanner.cs lines 263-313

### Bug 19: No global .item cross-reference
- **Discovered:** 2026-07-26
- **Where:** Not implemented
- **Issue:** The authoritative `.item` manifests live in `C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests\`, separate from game folders. The Python tool `lookup_metadata.py` implements `epic_crossref_item_manifests()` but no C# equivalent exists.
- **Impact:** MEDIUM — local `.mancpn` may have dev namespace; global `.item` always has correct public namespace and marketing name.
- **Suggested fix:** Implement `EpicManifestParser.CrossReferenceGlobalManifests()` (Plan 109 Phase 3).
- **Status:** ✅ Fixed — Plan 109 Phase 3. `EpicManifestParser.CrossReferenceGlobalManifests()` with case-insensitive path normalization and trailing separator stripping.
- **Verified:** 2026-07-26 — EpicManifestParser.cs lines 210-260

---

## Plan 112 Fixes (2026-07-26)

### Bug 20: Blacklist JSON/C# DTO Tier Name Mismatch
- **Discovered:** 2026-07-26
- **Where:** `BlacklistLoader.cs` — `ExeNamePatternsDto` properties
- **Issue:** Tiers 5-20 in `blacklist.json` use key names (e.g., `tier_5_error_crash_reporting`) that don't match the C# DTO `[JsonPropertyName]` attributes (e.g., `tier_5_unreal_build_debug`). This silently drops ~40 noise patterns from the tiered list.
- **Impact:** HIGH — `_upp`, `trial`, `crash`, `error`, `xlive`, `autorun`, `7za`, `dedicatedserver`, `editor`, `debug`, `install`, `unrealpak`, `patch`, `winscp` not loaded.
- **Status:** ✅ Fixed — Plan 112 Step 1. Renamed C# DTO properties and `[JsonPropertyName]` attributes to match current JSON keys. Removed stale Tier 21 property.
- **Verified:** 2026-07-26 — BlacklistLoader.cs, 4 new regression tests

### Bug 21: Display names use folder names instead of game titles
- **Discovered:** 2026-07-26
- **Where:** `FolderScanner.AddGameEntry()` — display name pipeline
- **Issue:** Non-store-enriched games (Standalone, Ubisoft, SteamEmu) use `NormalizeDisplayName(folderName)` which produces abbreviated names like `arx`, `doom6`, `galciv4`. PE `FileDescription` has the correct title but is discarded after scoring.
- **Impact:** MEDIUM — user-facing display name quality.
- **Status:** ✅ Fixed — Plan 112 Step 2. `ScoreExecutable()` now returns `ExeScoreResult` with `FileDescription`. `FindPrimaryExecutable()` returns `PrimaryExeResult` with `FileDescription`. `FolderScanner.AddGameEntry()` uses PE `FileDescription` as display name with guard conditions (length > 2, not a placeholder, no prior store enrichment).
- **Verified:** 2026-07-26 — ExecutableDiscovery.cs, FolderScanner.cs, 2 new tests

### Bug 22: Modern Ubisoft Connect games not detected
- **Discovered:** 2026-07-26
- **Where:** `StoreSignalDetector.HasUbisoftSignal()`
- **Issue:** Modern Ubisoft titles (e.g., Ghost Recon Breakpoint) lack older signals (`uplay_install.manifest`, loader DLLs) but contain `uplay_download/` directory and `*_UPP*.exe` subscription variants.
- **Impact:** MEDIUM — Ubisoft games misclassified as Standalone.
- **Status:** ✅ Fixed — Plan 112 Step 3. Added `uplay_download/` directory check and `*_UPP*.exe` pattern check to `HasUbisoftSignal()`. Added `UbisoftReadmeParser` for `Support/Readme/` metadata enrichment.
- **Verified:** 2026-07-26 — StoreSignalDetector.cs, UbisoftReadmeParser.cs, 11 new tests

---

## Detection Bugs (2026-07-26 — Live Build)

### Bug 23: Ubisoft readme enrichment returns publisher name as display name
- **Discovered:** 2026-07-26
- **Where:** `UbisoftReadmeParser.cs` line 68, `FolderScanner.cs` lines 349-361
- **Evidence:** `d:\games\Assassins Creed III\assassinscreed3.exe` displays as "Ubisoft Entertainment" instead of "Assassin's Creed III"
- **Issue:** `UbisoftReadmeParser` reads `Support/Readme/*.txt` and blindly assigns `lines[1]` as `GameTitle` with no validation. Some Ubisoft readme files have the publisher name on line 2 instead of the game title. Since `assassinscreed3.exe` has no valid PE `FileDescription` (or it's filtered by pe_metadata_blacklist), `storeEnrichedDisplayName` stays `false`, and the readme enrichment runs unchecked.
- **Impact:** MEDIUM — wrong display name for Ubisoft games with non-standard readme format.
- **Suggested fix:** Add validation in `UbisoftReadmeParser.TryParse()` or `FolderScanner.AddGameEntry()` to reject known publisher strings ("Ubisoft Entertainment", "Ubisoft", "Ubisoft SAS") from `GameTitle`. Alternatively, add a deny-list of publisher-like values.
- **Status:** Open

### Bug 24: ARC Game Store/Launcher not filtered as noise
- **Discovered:** 2026-07-26
- **Where:** `FileSystemHelper.NoiseSubDirNames`, `blacklist.json`
- **Evidence:** `d:\games\arc\arc.exe` listed as "ARC" game (Standalone) — this is the ARC game store/launcher, not a game
- **Issue:** `"arc"` is missing from both `NoiseSubDirNames` (so the `arc\` directory is scanned) and `blacklist.json` tiers (so `arc.exe` passes all noise filters). `FallbackSignalDetector.HasRootExecutableSignal` finds `arc.exe` as a valid root executable and classifies the folder as Standalone.
- **Impact:** MEDIUM — launcher/store detected as a game.
- **Suggested fix:** Add `"arc"` to `FileSystemHelper.NoiseSubDirNames` alongside other store launcher directories. Also add `"arc"` to `blacklist.json` `tier_3_store_bootstraps` as a scoring penalty. Verify no legitimate game exe contains "arc" as a substring (e.g., "ArcaniA.exe") — if so, use a more targeted approach.
- **Status:** Open

### Bug 25: battle.net launcher folder detected as a game
- **Discovered:** 2026-07-26
- **Where:** `FileSystemHelper.NoiseSubDirNames`, `ContainerScanner.s_nonGameFolderNames`
- **Evidence:** `d:\games\blizzard\battle.net\battle.net.exe` shows up as a game entry
- **Issue:** When `"blizzard"` and `"battle.net"` were removed from `NoiseSubDirNames` (to fix BattleNet game detection), the `battle.net\` launcher folder itself became scannable. `FallbackSignalDetector.HasRootExecutableSignal` finds `battle.net.exe` as a non-noise root exe and classifies the folder as Standalone. The fix for Bug 9 (allowing blizzard game detection) inadvertently exposed the launcher folder.
- **Impact:** MEDIUM — launcher directory appears as a game entry.
- **Suggested fix:** Add `"battle.net"` back to `NoiseSubDirNames` (keep `"blizzard"` removed). The `blizzard` folder is the publisher container with game children; the `battle.net` folder is the launcher executable directory. Also add `"battle.net"` to `ContainerScanner.s_nonGameFolderNames` and `blacklist.json` `tier_3_store_bootstraps`.
- **Status:** Open

### Bug 26: Diablo III RETAIL classified as Standalone instead of BattleNet
- **Discovered:** 2026-07-26
- **Where:** `ContainerScanner.ScanContainerChildren()` lines 77-84, `FolderScanner.Scan()` lines 112-125
- **Evidence:** `d:\games\blizzard\Diablo III RETAIL\` shows as "Standalone" instead of "BattleNet"
- **Issue:** `HasBattleNetGameSignal()` exists and correctly identifies BattleNet games by folder name/exe pattern, but it's only called in the **top-level loop** of `FolderScanner.Scan()` (line 121) when the parent directory has a BattleNet signal. Diablo III RETAIL is a grandchild of `d:\games\` (inside `blizzard\`), so it's discovered by `ContainerScanner`, which has no BattleNet-aware logic. The container scanner only checks `StoreSignalDetector.DetectType` (needs `.battle.net/` in the game folder) and `FallbackSignalDetector.HasRootExecutableSignal` (picks up `DiabloIII.exe` → Standalone).
- **Impact:** MEDIUM — BattleNet games misclassified as Standalone when inside a publisher container.
- **Suggested fix:** In `ContainerScanner.ScanContainerChildren()`, after `StoreSignalDetector.DetectType(child)` returns `Unknown`, check if a sibling directory named `"battle.net"` exists in the same parent (indicating a Blizzard container), and if so, call `StoreSignalDetector.HasBattleNetGameSignal(child)`. If true, classify as BattleNet instead of Standalone.
- **Status:** Open

---

## Live Testing Bugs (2026-07-26 — Plan 114)

### Bug 27: bme2 selects Worldbuilder.exe instead of lotrbfme2.exe
- **Discovered:** 2026-07-26
- **Where:** `ExecutableDiscovery.ScoreExecutable()`, `data/blacklist.json`
- **Evidence:** `d:\games\bme2\` shows "The Battle for Middle-earth™ II World Builder" as display name
- **Issue:** `Worldbuilder.exe` (33MB) is NOT in `blacklist.json` noise patterns. The `DefaultNoiseExePatterns` includes `"builder"` but production uses blacklist-loaded patterns which lack it. Worldbuilder passes all noise filters. Meanwhile `lotrbfme2.exe` (495KB) has empty PE Description, so its display name comes from folder name normalization.
- **Impact:** HIGH — wrong primary exe selected, wrong display name.
- **Suggested fix:** Add `"builder"` and `"worldbuilder"` to `blacklist.json` `tier_10_dev_editor_tools`.
- **Status:** Open

### Bug 28: Divine Divinity selects ConfigTool.exe instead of div.exe
- **Discovered:** 2026-07-26
- **Where:** `ExecutableDiscovery.ScoreExecutable()`, `data/blacklist.json`
- **Evidence:** `d:\games\Divine Divinity\Run\` — ConfigTool.exe (188KB) selected over div.exe (2.6MB)
- **Issue:** `configtool.exe` is not in any noise tier. Both exes are in `Run/` subdirectory. ConfigTool has PE Description "configtool MFC Application" which doesn't match any noise filter. The scoring may be picking ConfigTool due to directory traversal order or size heuristics.
- **Impact:** HIGH — wrong primary exe selected.
- **Suggested fix:** Add `"configtool"` to `blacklist.json` `tier_10_dev_editor_tools`.
- **Status:** Open

### Bug 29: Endless Legends displayed twice (Win32/Win64)
- **Discovered:** 2026-07-26
- **Where:** `ContainerScanner.ScanContainerChildren()` lines 55-68
- **Evidence:** `d:\games\ENdlessLegend\` shows two entries: "Win32" and "Win64"
- **Issue:** `ENdlessLegend/` has `Win32/` and `Win64/` subdirectories, each containing `EndlessLegend.exe`. Container scanner treats these as separate children with game signals, creating two game entries.
- **Impact:** MEDIUM — duplicate game entries.
- **Suggested fix:** Add `"win32"`, `"win64"`, `"x86"`, `"x64"` to `ContainerScanner.s_nonGameFolderNames`.
- **Status:** Open

### Bug 30: Diablo III listed twice (x64 + x64 - Copy)
- **Discovered:** 2026-07-26
- **Where:** `ExecutableDiscovery.FindExecutablesDeep()`, `FileSystemHelper.NoiseSubDirNames`
- **Evidence:** `d:\games\Diablo III\` has `x64\Diablo III64.exe` and `x64 - Copy\Diablo III64.exe`
- **Issue:** `x64 - Copy` is a backup directory that passes all noise filters. Exe discovery finds `Diablo III64.exe` in both directories.
- **Impact:** MEDIUM — duplicate game entries.
- **Suggested fix:** Add `"x64 - copy"`, `"x86 - copy"`, `" - copy"` to `FileSystemHelper.NoiseSubDirNames` or `ContainerScanner.s_nonGameFolderNames`.
- **Status:** Open

### Bug 31: Diablo III RETAIL classified as Standalone (duplicate of Bug 26)
- **Discovered:** 2026-07-26
- **Where:** Same as Bug 26
- **Evidence:** `d:\games\Diablo III\` classified as Standalone instead of BattleNet
- **Issue:** Same root cause as Bug 26 — `ContainerScanner` lacks BattleNet-aware logic.
- **Impact:** MEDIUM — BattleNet games misclassified.
- **Suggested fix:** Same as Bug 26.
- **Status:** Open (same fix as Bug 26)

### Bug 32: Library roots show duplicate "Games" names
- **Discovered:** 2026-07-26
- **Where:** `ShellViewModel.JumpToLibraryRoots()` line 162
- **Evidence:** `d:\games`, `e:\games`, `f:\games` all show as "Games" in the root list
- **Issue:** `Path.GetFileName(root.RootPath)` returns "Games" for all three roots.
- **Impact:** LOW — confusing but not broken.
- **Suggested fix:** Display full path (or drive letter + folder name) instead of just folder name.
- **Status:** Open

### Bug 33: Tags not displayed in left lister or details pane
- **Discovered:** 2026-07-26
- **Where:** `ShellViewModel.LoadGamesForRoot()`, `MainWindow.axaml`
- **Evidence:** Tags field exists (Plan 110) but not rendered in UI
- **Issue:** `ShellPaneItemViewModel` doesn't have a `Tags` property. `LoadGamesForRoot()` doesn't read `game.Tags`. UI templates don't display tags.
- **Impact:** LOW — feature incomplete.
- **Suggested fix:** Add `Tags` to `ShellPaneItemViewModel`, populate in `LoadGamesForRoot()`, add to left-pane item template and right-pane details panel.
- **Status:** Open
