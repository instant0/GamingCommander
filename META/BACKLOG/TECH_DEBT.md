# META/BACKLOG/TECH_DEBT.md — Technical Debt & Known Issues

**Nature:** Mutable. Entries appended by Builder/Reviewer, moved to PLANNING/ when prioritized.
**Last verified:** 2026-07-19

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
- **Status:** Open — **P0 blocker**

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
- **Status:** Open — **Plan 106: `planning/106-unified-setup-screen.md`**

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
