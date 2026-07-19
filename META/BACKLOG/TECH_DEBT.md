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
- **Issue:** All command buttons have `IsHitTestVisible="False"` — cannot be clicked. Only 6 of 10 F-key buttons exist (F1, F2, F3, F5, F9, F10).
- **Status:** Open
- **Note:** Keyboard handlers exist for F2, F9, T, Enter, Backspace, arrows. Missing: F1, F3, F4, F5, F6, F7, F8, F10 handlers.

### Default settings/games files not created alongside exe
- **Discovered:** 2026-04-17
- **Where:** App startup
- **Issue:** Default `settings.json` and `games.json` should be created alongside exe for clean installs.
- **Status:** Open

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
- **Issue:** Currently on .NET 8. Plan to upgrade to .NET 9 exists at planning/90-sdk-upgrade.md.
- **Status:** Open
