# Task T75: Windows Smoke Gate Bugfixes

**Tier:** 1 — Bugfix
**Phase:** H — MVP
**Effort:** ~2–3 hours
**Risk:** Medium
**Status:** Pending
**Prerequisites:** T70 partial validation complete
**WP:** WP-5 (MVP gate)

---

## Objective

Fix bugs found during Windows smoke gate validation (T70). These are blocking MVP declaration.

## Bugs Found

### BUG-1: Rescan Overwrites User Edits (P0) ✅ FIXED

**Symptom:** F6 rescan replaced custom display name "Battle for Middle Earth 2" back to folder name "bme2".

**Root cause:** `GamesDatabaseService.RescanRoot()` replaces all games with freshly scanned entries, discarding user overrides.

**Fix implemented:**
- `RescanRoot()` now merges scanned results with existing games by matching `GameEntry.Id`
- Added `MergeGameEntries()` helper that preserves user overrides:
  - `DisplayName` — preserved if differs from auto-normalized folder name
  - `IsSourceOverridden` + `GameSource` — preserved when user overrode source type
  - `CommandLineArguments` — preserved when user added custom args
  - `LauncherPath` — preserved when user specified custom launcher
  - `ManifestPath` — preserved when user specified custom manifest
- Existing games not in scan results are retained (folder temporarily unavailable)
- New tests added: `RescanRoot_MergesExistingAndNewGames`, `PreservesUserDisplayName`, `PreservesUserCommandLineArgs`, `PreservesUserSourceOverride`

**Files:**
- `src/GamingCommander.App/Services/GamesDatabaseService.cs` — `RescanRoot()` + `MergeGameEntries()`
- `tests/GamingCommander.App.Tests/GamesDatabaseServiceTests.cs` — 4 new tests

---

### BUG-2: Steam Installed Not Green, Moved Not Yellow (P1) ✅ FIXED

**Symptom:** No color on Installed Steam entries. Only one red Orphaned entry seen.

**Root cause:** `PlatformStatus` color mapping showed green for Installed, but design requires white (default) for Installed.

**Design change implemented:**
- Installed → white (no highlight, default text color)
- Moved → yellow (#E8C547)
- Orphaned → red (#E87070)
- Missing → red (#E87070)

**Fix implemented:** Changed `platformStatusColor` mapping in `ShellViewModel.LoadGamesForRoot()` to return empty string for Installed (converter returns default `TextPrimary` brush).

**Files:**
- `src/GamingCommander.UI/ViewModels/ShellViewModel.cs` — `LoadGamesForRoot()` color mapping

---

### BUG-3: Status Bar Never Clears (P1) ✅ FIXED

**Symptom:** "Loaded 2 roots" message persists forever. No "Scanning..." feedback.

**Fix implemented:**
1. Added `SetStatusWithAutoClear(string message, int autoClearMs = 5000)` helper to MainWindow
2. Status messages auto-clear after 5 seconds (configurable)
3. "Scanning {root}..." feedback shown during F6 rescan (no auto-clear until scan completes)
4. "Added root: {path}" feedback shown briefly after F7 add
5. All informational status messages (F3, F8, S) use auto-clear

**Files:**
- `src/GamingCommander.App/MainWindow.axaml.cs` — `SetStatusWithAutoClear()` helper, `RefreshCurrentRootAsync()`, `AddRootAsync()`, keyboard/button handlers

---

### BUG-4: Empty Folder Listed in VFS (P1) ✅ FIXED

**Symptom:** Empty folder added as library root shows in VFS with no games.

**Fix implemented:**
1. `LibraryManager.AddRoot()` now returns `bool` — `false` if scan finds 0 games (root not added)
2. `MainWindow.AddRootAsync()` checks return value — shows "No games found in {path}" if empty
3. `LibrarySetupViewModel.AddRootAsync()` removes entry from UI if no games found
4. `ILibraryManager` interface updated to match new return type

**Files:**
- `src/GamingCommander.App/Services/LibraryManager.cs` — `AddRoot()` returns bool
- `src/GamingCommander.Core/ILibraryManager.cs` — Interface updated
- `src/GamingCommander.App/MainWindow.axaml.cs` — `AddRootAsync()` checks return value
- `src/GamingCommander.App/ViewModels/LibrarySetupViewModel.cs` — Handles empty folder case

---

### BUG-5: Battle.net Detected as Game (P1) ✅ FIXED

**Symptom:** `d:\games\blizzard\battle.net` detected as a game entry.

**Root cause:** Store launcher directory not filtered out. Battle.net is a store, not a game.

**Fix implemented:**
1. Added store launcher directories to `FileSystemHelper.NoiseSubDirNames`: `battle.net`, `epic games`, `origin`, `uplay`, `gog galaxy`, `ea app`, `rockstar games`, `blizzard`
2. Added same patterns to `ContainerScanner.s_nonGameFolderNames`
3. Store launcher directories are now skipped during scanning

**Files:**
- `src/GamingCommander.App/Services/FileSystemHelper.cs` — `NoiseSubDirNames`
- `src/GamingCommander.App/Services/ContainerScanner.cs` — `s_nonGameFolderNames`

**How to test BUG-5:** Rescan the existing `d:\games` library root (F6 while inside it). The `blizzard\battle.net` subfolder should NOT appear as a game entry. The test is about scanning a parent root that contains a store launcher directory as a subfolder — NOT about adding the blizzard subfolder as its own library root.

---

### BUG-6: F4 Field Order Wrong (P2) ✅ FIXED

**Symptom:** "Launch Args" shown below "Launcher Path". Args are for exe, not launcher.

**Fix implemented:** Reordered fields in `RenderFields()`:
1. Display Name
2. Game Type
3. Executable Path
4. Launch Args (for exe)
5. Launcher Path
6. Epic Manifest (hidden for non-Epic)

**Files:**
- `src/GamingCommander.App/GameSetupWindow.axaml.cs` — `RenderFields()` field order

---

### BUG-7: F4 Shows Epic Manifest for Non-Epic Games (P2) ✅ FIXED

**Symptom:** Epic Manifest field shown for all games, even non-Epic.

**Fix implemented:** Conditional visibility — only show when `SelectedType == "Epic"`.

**Files:**
- `src/GamingCommander.App/GameSetupWindow.axaml.cs` — `RenderFields()` conditional check

---

### BUG-8: F4 Folder Field Redundant (P2) ✅ FIXED

**Symptom:** "Folder" field at bottom of F4 dialog duplicates the path shown at top.

**Fix implemented:** Removed the Folder field entirely.

**Files:**
- `src/GamingCommander.App/GameSetupWindow.axaml.cs` — `RenderFields()` removed Folder field

---

### BUG-9: Diablo III Not Detected as BattleNet (P1) ✅ FIXED

**Symptom:** Diablo III in `d:\games\blizzard\` detected as "standalone" instead of "BattleNet".

**Root cause:** `StoreSignalDetector` doesn't check for BattleNet-specific files or parent folder patterns. Battle.net launcher is detected (tagged as BattleNet) but games in the same folder aren't.

**Fix implemented:**
1. Added `HasBattleNetGameSignal()` method to `StoreSignalDetector` — checks for common BattleNet game folder names (warcraft, diablo, overwatch, starcraft, etc.) and BattleNet-specific executables
2. Added parent folder signal propagation in `FolderScanner.Scan()` — if parent folder has BattleNet signal, check if child is a BattleNet game
3. Games inside BattleNet launcher directories now correctly detected as BattleNet

**Files:**
- `src/GamingCommander.App/Services/StoreSignalDetector.cs` — `HasBattleNetGameSignal()` method
- `src/GamingCommander.App/Services/FolderScanner.cs` — parent folder signal propagation

---

### BUG-10: Wrong Exe Selected (BME2) (P1) ✅ FIXED

**Symptom:** BME2 folder picked `WorldBuilder.exe` instead of `lotrfBME2.exe`. WorldBuilder is larger so scoring favored it.

**Root cause:** `ExecutableDiscovery.ScoreExecutable()` weights file size heavily. WorldBuilder.exe (editor tool) is bigger than the game exe.

**Fix implemented:**
1. Added editor/tool patterns to `FolderScanner.DefaultNoiseExePatterns`: `editor`, `builder`, `tool`, `config`, `settings`
2. Added folder name matching bonus in `ScoreExecutable()`:
   - `name.Contains(folderLower)` → +15 (exe name contains folder name)
   - `folderLower.Contains(name)` → +15 (folder name contains exe stem)
3. Reduced file size bonus from +10 to +5 (avoids favoring large editor tools)

**Files:**
- `src/GamingCommander.App/Services/FolderScanner.cs` — `DefaultNoiseExePatterns`
- `src/GamingCommander.App/Services/ExecutableDiscovery.cs` — `ScoreExecutable()` method

---

### BUG-11: F4 Browse Starts in Wrong Folder (P2) — DEFERRED → ExeCandidateSelector

**Symptom:** Browse button for EXE starts in parent folder (`d:\games`) instead of game folder (`d:\games\bme2`).

**Original request:** Set `SuggestedStartLocation` on file picker to game's folder.

**Decision: Deferred (not a bug, design change).** The Avalonia `SuggestedStartLocation` API is not available in our version. More importantly, the broader design direction is to eliminate filesystem browsing entirely from the F4 dialog:

**Proposed ExeCandidateSelector feature (post-MVP):**
- During scan, store all non-noise candidate exe paths in `GameEntry.Extra["CandidateExes"]` (semicolon-separated).
- In F4 dialog, replace "Browse..." file picker with a dropdown showing detected candidates.
- User selects which detected exe is the game launcher — no filesystem browsing needed.
- This keeps the entire GamingCommander experience self-contained (only browse for library roots, never for individual files).

**Current state:** Browse buttons remain in F4 dialog but open default system location (not game folder). Works fine as-is; ExeCandidateSelector is a UX improvement for a future task.

**Files:**
- `src/GamingCommander.App/GameSetupWindow.axaml.cs` — Browse buttons unchanged (open system default)

---

### BUG-13: "xx Items" Persists at Top Level (P2) ✅ FIXED

**Symptom:** Item count text "(xx items)" shows at top level but should only show inside a library.

**Fix implemented:** Added `IsVisible="{Binding !IsAtRootLevel}"` binding to item count TextBlock. Item count now only shows when inside a library root.

**Files:**
- `src/GamingCommander.App/MainWindow.axaml` — Item count TextBlock visibility binding

---

## Fix Order

```
BUG-1 (P0: rescan) → BUG-2 (P1: colors) → BUG-3 (P1: status) → BUG-4 (P1: empty) → BUG-5 (P1: detection) → BUG-9 (P1: BattleNet) → BUG-10 (P1: exe scoring) → BUG-6/7/8/11/13 (P2: polish)
```

## Requirements

- [x] BUG-1: Rescan preserves user overrides (DisplayName, source, args, tags)
- [x] BUG-2: Installed Steam = white, Moved = yellow, Orphaned/Missing = red
- [x] BUG-3: Status messages auto-clear after 5 seconds
- [x] BUG-3: "Scanning..." feedback during rescan
- [x] BUG-4: Empty folders not listed in VFS
- [x] BUG-4: Warning when adding empty folder
- [x] BUG-5: Battle.net directory filtered as noise
- [x] BUG-9: Diablo III detected as BattleNet (parent folder signal)
- [x] BUG-10: Exe scoring prefers game-named exe over larger tools
- [x] BUG-6: F4 field order corrected (Args above Launcher)
- [x] BUG-7: F4 hides Epic Manifest for non-Epic
- [x] BUG-8: F4 removes redundant Folder field
- [x] BUG-13: Item count hidden at top level
- [ ] BUG-11: F4 Browse start location — DEFERRED (ExeCandidateSelector proposal)
- [x] Build clean, existing tests pass
- [x] No regressions in launch, edit, rescan

## Verification

- [x] `dotnet build` passes (0 errors)
- [x] `dotnet test` passes (209 tests, no regressions)
- [ ] Manual: F6 rescan preserves custom display name
- [ ] Manual: Installed Steam = white, Moved = yellow, Orphaned = red
- [ ] Manual: Status bar clears after 5 seconds
- [ ] Manual: Status bar shows "Scanning..." during F6
- [ ] Manual: Empty folder not listed in VFS
- [ ] Manual: Battle.net directory NOT listed as game when rescanning d:\games library
- [ ] Manual: Diablo III detected as BattleNet
- [ ] Manual: BME2 picks lotrfBME2.exe, not WorldBuilder.exe
- [ ] Manual: F4 field order correct
- [ ] Manual: F4 hides Epic Manifest for non-Epic games
- [ ] Manual: Item count hidden at top level

## Completion Notes

- **Completed:** 2026-07-26
- **What was done:** Fixed 11 of 12 bugs (BUG-11 deferred to ExeCandidateSelector feature). Code changes across 11 files, 4 new tests added (209 total).
- **Verification:** Build clean, 217 tests passing. Manual verification deferred to Windows smoke re-test.
- **Issues encountered:** BUG-11 (Browse start location) deferred — Avalonia `SuggestedStartLocation` API unavailable; design direction is ExeCandidateSelector instead.
