# Plan 114: Detection Bug Fixes from Live Testing

**Created:** 2026-07-26
**Priority:** P1
**Status:** DRAFT
**Source:** `planning/999-comments.txt` + PE metadata analysis from `/projects/game-text/`

---

## 1. Bug Summary

Live testing on `d:\games` revealed 7 detection bugs. Each is documented with root cause analysis from the actual file list and PE metadata.

| # | Bug | Severity | Root Cause |
|---|-----|----------|------------|
| B27 | bme2 picks Worldbuilder.exe instead of lotrbfme2.exe | HIGH | "builder" not in blacklist.json noise patterns |
| B28 | Divine Divinity picks ConfigTool.exe instead of div.exe | HIGH | ConfigTool.exe at `Run/configtool.exe` is root-level; scoring doesn't penalize config tools |
| B29 | Endless Legends displayed twice (Win32/Win64) | MEDIUM | Container scanner treats platform subdirs as separate games |
| B30 | Diablo III listed twice (x64 + x64 - Copy) | MEDIUM | "x64 - Copy" directory not filtered as backup |
| B31 | Diablo III RETAIL classified as Standalone | MEDIUM | Same as Bug 26 — ContainerScanner lacks BattleNet logic |
| B32 | Library roots show duplicate "Games" names | LOW | Display shows folder name, not full path |
| B33 | Tags not displayed in left lister or details pane | LOW | Tags field exists but not rendered in UI |

---

## 2. Detailed Analysis

### B27: bme2 picks Worldbuilder.exe instead of lotrbfme2.exe

**File data:**
```
D:\Games\bme2\eauninstall.exe      (344KB)  — Uninstall
D:\Games\bme2\extra_uninst.exe     (100KB)  — noise
D:\Games\bme2\lotrbfme2.exe        (495KB)  — empty PE Description
D:\Games\bme2\LotRIcon.exe         (53KB)   — icon file
D:\Games>bme2\Worldbuilder.exe     (33MB)   — Description: "The Battle for Middle-earth™ II World Builder"
```

**Root cause:** `Worldbuilder.exe` is NOT in `blacklist.json` noise patterns. The `DefaultNoiseExePatterns` includes `"builder"` but production uses `blacklist.json` which does NOT contain `"builder"`. So Worldbuilder passes all noise filters and enters scoring.

**Scoring analysis:**
- `lotrbfme2.exe`: +15 (name contains "bme2") + 10 (folder token match) = **25**
- `Worldbuilder.exe`: +1 (file size 33MB) = **1**

`lotrbfme2.exe` should win scoring. But `Worldbuilder.exe` is 33MB vs 495KB — the `FindPrimaryExecutable` fallback path (line 318-320) picks the **largest** exe when `FindExecutablesDeep` returns 0 candidates. If `lotrbfme2.exe` is being filtered somewhere, Worldbuilder wins by size.

**Hypothesis:** `lotrbfme2.exe` might be filtered by `IsNoiseExeByPath` if the blacklist patterns include a substring match. Or the scoring order is wrong. Need to add `"builder"` to blacklist.json tier_10.

**Fix:** Add `"builder"` to `blacklist.json` `tier_10_dev_editor_tools`. This penalizes Worldbuilder.exe (-10 tier penalty) and ensures lotrbfme2.exe wins.

---

### B28: Divine Divinity picks ConfigTool.exe instead of div.exe

**File data:**
```
D:\Games\Divine Divinity\Run\133_org_div.exe  (3.5MB) — Description: "Divine Divinity"
D:\Games\Divine Divinity\Run\configtool.exe    (188KB) — Description: "configtool MFC Application"
D:\Games\Divine Divinity\Run\div.exe           (2.6MB) — Description: "Divine Divinity"
D:\Games\Divine Divinity\Run\originaldiv.exe   (3.4MB) — Description: "Divine Divinity"
```

**Root cause:** All exes are in `Run/` subdirectory, not the game root. `FindExecutablesDeep` searches root first, then children. The `Run/` directory is not a noise directory, so all 4 exes are candidates.

**Scoring analysis (folderName = "Divine Divinity"):**
- `div.exe`: name="div", folderLower="divine divinity"
  - name.Contains("divine divinity")? No
  - folderLower.Contains("div")? Yes → +15
  - Tokens: ["divine", "divinity"] → name.Contains("divine")? No, name.Contains("divinity")? No → +0
  - fileSize = 2.6MB → +0
  - PE: Description = "Divine Divinity" → desc.Contains("retail")? No → +0
  - **Total: 15**

- `configtool.exe`: name="configtool", folderLower="divine divinity"
  - name.Contains("divine divinity")? No
  - folderLower.Contains("configtool")? No
  - Tokens: no match → +0
  - fileSize = 188KB → +0
  - PE: Description = "configtool MFC Application" → desc.Contains("retail")? No → +0
  - **Total: 0**

- `133_org_div.exe`: name="133_org_div"
  - Contains "org_" → backup penalty -20
  - **Total: -20**

- `originaldiv.exe`: name="originaldiv"
  - Contains "original" → backup penalty -15
  - **Total: -15**

So `div.exe` should win with score 15. But the user says ConfigTool was picked. This suggests ConfigTool is somehow winning. Maybe the issue is that `div.exe` is being filtered as noise (unlikely), or the scoring is not running correctly.

**Hypothesis:** The game root is `Divine Divinity/` but the exes are in `Divine Divinity/Run/`. The `Run/` directory might be treated as the game folder by the container scanner, or the exe discovery might be looking at the wrong directory.

**Fix:** Add `"configtool"` to `blacklist.json` `tier_10_dev_editor_tools` to penalize config tools. Also verify that `div.exe` is not being filtered.

---

### B29: Endless Legends displayed twice (Win32/Win64)

**File data:**
```
D:\Games\ENdlessLegend\unins000.exe                           (1.7MB)
D:\Games\ENdlessLegend\Win32\EndlessLegend.exe                (16MB)
D:\Games\ENdlessLegend\Win32\Public\WorldGenerator\Amplitude.WorldGenerator.exe (11KB)
D:\Games\ENdlessLegend\Win64\EndlessLegend.exe                (20MB)
D:\Games\ENdlessLegend\Win64\Public\WorldGenerator\Amplitude.WorldGenerator.exe (11KB)
```

**Root cause:** `ENdlessLegend/` has `Win32/` and `Win64/` subdirectories. The container scanner treats these as separate children with game signals (each contains `EndlessLegend.exe`). Since there are 2+ children with game signals, it recurses into both, creating two separate game entries.

**Fix:** Add `"win32"`, `"win64"`, `"x86"`, `"x64"` to `ContainerScanner.s_nonGameFolderNames` to prevent platform subdirectories from being treated as game entries during container recursion. The exe discovery already handles UE platform paths (`Binaries/Win64/`, etc.) — these are different (flat `Win32/`/`Win64/` dirs).

---

### B30: Diablo III listed twice (x64 + x64 - Copy)

**File data:**
```
D:\Games\Diablo III\x64\Diablo III64.exe          (27MB) — Description: "Diablo III Retail"
D:\Games\Diablo III\x64 - Copy\Diablo III64.exe   (41MB) — Description: "Diablo III Retail"
```

**Root cause:** `x64 - Copy` is a backup directory that passes all noise filters. The exe discovery finds `Diablo III64.exe` in both `x64/` and `x64 - Copy/`, and the container scanner treats both as game entries.

**Fix:** Add `"x64 - copy"`, `"x86 - copy"`, `" - copy"` to `blacklist.json` directory patterns or `FileSystemHelper.NoiseSubDirNames` to filter backup directories. Also add `"x64 - copy"` to `ContainerScanner.s_nonGameFolderNames`.

---

### B31: Diablo III RETAIL classified as Standalone

**Same as Bug 26.** `ContainerScanner` lacks BattleNet-aware logic. The `HasBattleNetGameSignal()` method exists but is only called in the top-level loop of `FolderScanner.Scan()`, not in `ContainerScanner.ScanContainerChildren()`.

**Fix:** In `ContainerScanner.ScanContainerChildren()`, after `StoreSignalDetector.DetectType(child)` returns `Unknown`, check if a sibling directory named `"battle.net"` exists in the same parent (indicating a Blizzard container), and if so, call `StoreSignalDetector.HasBattleNetGameSignal(child)`. If true, classify as BattleNet instead of Standalone.

---

### B32: Library roots show duplicate "Games" names

**Root cause:** `JumpToLibraryRoots()` uses `Path.GetFileName(root.RootPath)` which returns "Games" for `d:\games\`, `e:\games\`, `f:\games\`. Multiple roots with the same folder name appear identical.

**Fix:** In `JumpToLibraryRoots()`, display the full path (or at least drive letter + folder name) instead of just the folder name. For example, show "d:\games" instead of "Games".

---

### B33: Tags not displayed in left lister or details pane

**Root cause:** `Tags` field exists on `GameEntry` (added in Plan 110) but `ShellPaneItemViewModel` and `ShellViewModel.LoadGamesForRoot()` don't read or display it. The right-pane details panel doesn't have a Tags row.

**Fix:** 
1. Add `Tags` property to `ShellPaneItemViewModel`
2. In `LoadGamesForRoot()`, populate `Tags` from `game.Tags`
3. In `MainWindow.axaml` left-pane item template, show tags after game name (e.g., "Game Name [RPG, Open World]")
4. In `MainWindow.axaml` right-pane details panel, add a Tags row

---

## 3. Implementation Steps

### Step 1: Blacklist Fixes (B27, B28, B30)

**File:** `data/blacklist.json`
- Add `"builder"` to `tier_10_dev_editor_tools`
- Add `"configtool"` to `tier_10_dev_editor_tools`
- Add `"worldbuilder"` to `tier_10_dev_editor_tools`

**File:** `FileSystemHelper.cs`
- Add `"x64 - copy"`, `"x86 - copy"` to `NoiseSubDirNames`

**File:** `ContainerScanner.cs`
- Add `"win32"`, `"win64"`, `"x86"`, `"x64"`, `"x64 - copy"`, `"x86 - copy"` to `s_nonGameFolderNames`

### Step 2: BattleNet Container Detection (B31)

**File:** `ContainerScanner.cs`
- In `ScanContainerChildren()`, after `StoreSignalDetector.DetectType(child)` returns `Unknown`:
  - Check if any sibling directory is named `"battle.net"` (case-insensitive)
  - If yes, call `StoreSignalDetector.HasBattleNetGameSignal(child)`
  - If true, classify as `GameSourceKind.BattleNet` instead of `Standalone`

### Step 3: Library Root Display (B32)

**File:** `ShellViewModel.cs`
- In `JumpToLibraryRoots()`, change `Title` from `Path.GetFileName(...)` to display the full root path (or truncated path with drive letter)

### Step 4: Tags Display (B33)

**File:** `ShellPaneItemViewModel.cs`
- Add `public string Tags { get; init; } = string.Empty;`

**File:** `ShellViewModel.cs`
- In `LoadGamesForRoot()`, populate `Tags` from `game.Tags`

**File:** `MainWindow.axaml`
- Left-pane item template: add Tags text after Title (dimmed color)
- Right-pane details panel: add Tags row

### Step 5: Documentation

- Update `META/BACKLOG/TECH_DEBT.md` with B27-B33
- Update `META/SESSION/CURRENT.md`
- Update `docs/GAME-DETECTION-LOGIC.md` with platform subdir filtering

---

## 4. Success Criteria

- [ ] bme2 selects lotrbfme2.exe as primary (not Worldbuilder.exe)
- [ ] Divine Divinity selects div.exe as primary (not ConfigTool.exe)
- [ ] Endless Legends shows as single entry (not Win32 + Win64)
- [ ] Diablo III shows as single entry (no "x64 - Copy" duplicate)
- [ ] Diablo III RETAIL classified as BattleNet (not Standalone)
- [ ] Library roots show full path (not duplicate "Games")
- [ ] Tags displayed in left lister and details pane
- [ ] Build clean, all tests pass
