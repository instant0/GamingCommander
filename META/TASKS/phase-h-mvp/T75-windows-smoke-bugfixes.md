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

### BUG-1: Rescan Overwrites User Edits (P0)

**Symptom:** F6 rescan replaced custom display name "Battle for Middle Earth 2" back to folder name "bme2".

**Root cause:** `GamesDatabaseService.RescanRoot()` replaces all games with freshly scanned entries, discarding user overrides.

**Fix:** In `RescanRoot()`, before replacing games, preserve user-edited fields:
- `DisplayName` (if user changed it)
- `IsSourceOverridden` (if user changed source type)
- `CommandLineArguments` (if user added custom args)
- `Tags` (user-added tags)

**Approach:** Match by `Id` (deterministic from rootPath + folderName). For each existing game, if `Id` matches a newly scanned game, merge: take scanned fields but keep user overrides.

**Files:**
- `src/GamingCommander.App/Services/GamesDatabaseService.cs` — `RescanRoot()`

---

### BUG-2: Steam Installed Not Green, Moved Not Yellow (P1)

**Symptom:** No color on Installed Steam entries. Only one red Orphaned entry seen.

**Root cause:** Need to investigate — either `PlatformStatus` not set correctly, or `HexToBrushConverter` not binding.

**Design change:** Installed Steam entries should be **white** (default), not green. Only show colors for problems:
- Installed → white (no highlight)
- Moved → yellow
- Orphaned → red
- Missing → red

**Files:**
- `src/GamingCommander.UI/ViewModels/ShellViewModel.cs` — `LoadGamesForRoot()` status color mapping

---

### BUG-3: Status Bar Never Clears (P1)

**Symptom:** "Loaded 2 roots" message persists forever. No "Scanning..." feedback.

**Fix:**
1. Add auto-clear timer: status messages clear after 5 seconds (configurable)
2. Show "Scanning {root}..." when F6 rescan starts
3. Show "Added root: {path}" briefly after F7 add

**Files:**
- `src/GamingCommander.App/MainWindow.axaml.cs` — status updates
- `src/GamingCommander.UI/ViewModels/ShellViewModel.cs` — `StatusText` property

---

### BUG-4: Empty Folder Listed in VFS (P1)

**Symptom:** Empty folder added as library root shows in VFS with no games.

**Fix:**
1. In `FolderScanner.Scan()` or `LibraryManager.AddRoot()`: if scan finds 0 games, don't add the root (or mark as empty)
2. In wizard/add root flow: warn user "This folder contains no games" and don't add
3. If root already exists and rescan finds 0 games: show "No games found in {root}" status

**Files:**
- `src/GamingCommander.App/MainWindow.axaml.cs` — `AddRootAsync()`
- `src/GamingCommander.App/Services/LibraryManager.cs` — `AddRoot()`

---

### BUG-5: Battle.net Detected as Game (P1)

**Symptom:** `d:\games\blizzard\battle.net` detected as a game entry.

**Root cause:** Store launcher directory not filtered out. Battle.net is a store, not a game.

**Fix:** Add store launcher directories to noise/skip list:
- `battle.net` (directory name)
- Other store launchers: `epic games`, `origin`, `uplay`, `gog galaxy`, etc.

**Files:**
- `src/GamingCommander.App/Services/FolderScanner.cs` — `s_nonGameFolderNames` or `NoiseSubDirNames`
- `tools/data/blacklist.json` — add launcher directory patterns

---

### BUG-6: F4 Field Order Wrong (P2)

**Symptom:** "Launch Args" shown below "Launcher Path". Args are for exe, not launcher.

**Current order:**
1. Display Name
2. Game Type
3. Executable Path
4. Launcher Path
5. Launch Args
6. Epic Manifest
7. Folder

**Correct order:**
1. Display Name
2. Game Type
3. Executable Path
4. Launch Args (for exe)
5. Launcher Path
6. Epic Manifest (hidden for non-Epic)
7. Folder (remove — redundant)

**Files:**
- `src/GamingCommander.App/GameSetupWindow.axaml.cs` — `RenderFields()`

---

### BUG-7: F4 Shows Epic Manifest for Non-Epic Games (P2)

**Symptom:** Epic Manifest field shown for all games, even non-Epic.

**Fix:** Conditional visibility — only show when `GameSource == Epic`.

**Files:**
- `src/GamingCommander.App/GameSetupWindow.axaml.cs` — `RenderFields()`

---

### BUG-8: F4 Folder Field Redundant (P2)

**Symptom:** "Folder" field at bottom of F4 dialog duplicates the path shown at top.

**Fix:** Remove the Folder field entirely.

**Files:**
- `src/GamingCommander.App/GameSetupWindow.axaml.cs` — `RenderFields()`

---

### BUG-9: Diablo III Not Detected as BattleNet (P1)

**Symptom:** Diablo III in `d:\games\blizzard\` detected as "standalone" instead of "BattleNet".

**Root cause:** `StoreSignalDetector` doesn't check for BattleNet-specific files or parent folder patterns. Battle.net launcher is detected (tagged as BattleNet) but games in the same folder aren't.

**Fix:** 
1. Check parent folder name for store signals (if game is in `blizzard/` or `battle.net/` directory)
2. Add BattleNet detection: `Warcraft III`, `Diablo`, `Overwatch`, `StarCraft` folder names as signals
3. Or: check for BattleNet manifest files (`*.agent`, `catalog.json`)

**Files:**
- `src/GamingCommander.App/Services/StoreSignalDetector.cs` — BattleNet detection
- `src/GamingCommander.App/Services/FolderScanner.cs` — parent folder signal propagation

---

### BUG-10: Wrong Exe Selected (BME2) (P1)

**Symptom:** BME2 folder picked `WorldBuilder.exe` instead of `lotrfBME2.exe`. WorldBuilder is larger so scoring favored it.

**Root cause:** `ExecutableDiscovery.ScoreExecutable()` weights file size heavily. WorldBuilder.exe (editor tool) is bigger than the game exe.

**Fix:**
1. Add "WorldBuilder" to noise/tool exe patterns (or generic: `*editor*`, `*builder*`, `*launcher*`)
2. Reduce size bonus weight — name match should outweigh size
3. Or: add bonus for exe name matching folder name (BME2 → lotrfBME2.exe has partial match)

**Files:**
- `src/GamingCommander.App/Services/ExecutableDiscovery.cs` — `ScoreExecutable()` and `s_noisePatterns`
- `tools/data/blacklist.json` — add editor/builder patterns

---

### BUG-11: F4 Browse Starts in Wrong Folder (P2)

**Symptom:** Browse button for EXE starts in parent folder (`d:\games`) instead of game folder (`d:\games\bme2`).

**Root cause:** File picker default directory not set to game's folder.

**Fix:** Set `SuggestedStartLocation` to game's executable folder or root path.

**Files:**
- `src/GamingCommander.App/GameSetupWindow.axaml.cs` — file picker in `MakeFieldRow()`

---

### BUG-13: "xx Items" Persists at Top Level (P2)

**Symptom:** Item count text "(xx items)" shows at top level but should only show inside a library.

**Fix:** Conditional visibility — hide item count when `IsAtRootLevel == true`.

**Files:**
- `src/GamingCommander.App/MainWindow.axaml` — ItemCount TextBlock visibility binding

---

## Fix Order

```
BUG-1 (P0: rescan) → BUG-2 (P1: colors) → BUG-3 (P1: status) → BUG-4 (P1: empty) → BUG-5 (P1: detection) → BUG-9 (P1: BattleNet) → BUG-10 (P1: exe scoring) → BUG-6/7/8/11/13 (P2: polish)
```

## Requirements

- [ ] BUG-1: Rescan preserves user overrides (DisplayName, source, args, tags)
- [ ] BUG-2: Installed Steam = white, Moved = yellow, Orphaned/Missing = red
- [ ] BUG-3: Status messages auto-clear after 5 seconds
- [ ] BUG-3: "Scanning..." feedback during rescan
- [ ] BUG-4: Empty folders not listed in VFS
- [ ] BUG-4: Warning when adding empty folder
- [ ] BUG-5: Battle.net directory filtered as noise
- [ ] BUG-9: Diablo III detected as BattleNet (parent folder signal)
- [ ] BUG-10: Exe scoring prefers game-named exe over larger tools
- [ ] BUG-11: F4 Browse starts in game folder
- [ ] BUG-6: F4 field order corrected (Args above Launcher)
- [ ] BUG-7: F4 hides Epic Manifest for non-Epic
- [ ] BUG-8: F4 removes redundant Folder field
- [ ] BUG-13: Item count hidden at top level
- [ ] Build clean, existing tests pass
- [ ] No regressions in launch, edit, rescan

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (no regressions)
- [ ] Manual: F6 rescan preserves custom display name
- [ ] Manual: Installed Steam = white, Moved = yellow, Orphaned = red
- [ ] Manual: Status bar clears after 5 seconds
- [ ] Manual: Status bar shows "Scanning..." during F6
- [ ] Manual: Empty folder not listed in VFS
- [ ] Manual: Battle.net directory not listed as game
- [ ] Manual: Diablo III detected as BattleNet
- [ ] Manual: BME2 picks lotrfBME2.exe, not WorldBuilder.exe
- [ ] Manual: F4 Browse starts in game folder
- [ ] Manual: F4 field order correct
- [ ] Manual: F4 hides Epic Manifest for non-Epic games
- [ ] Manual: Item count hidden at top level

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
