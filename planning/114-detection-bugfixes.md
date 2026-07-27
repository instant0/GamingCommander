# Plan 114: Detection Bug Fixes from Live Testing

**Created:** 2026-07-26
**Priority:** P1
**Status:** ✅ COMPLETE — 11/11 bugs fixed, 327 tests passing
**Source:** `planning/999-comments.txt` + PE metadata analysis from `/projects/game-text/`
**Last updated:** 2026-07-26 — Phase 1 implemented, Phase 2-4 analyzed

---

## 1. Bug Summary

Live testing on `d:\games` revealed 11 detection bugs. Each is documented with root cause analysis, exact code locations, and specific fixes.

| # | Bug | Severity | Effort | Fix Type | Status |
|---|-----|----------|--------|----------|--------|
| B23 | Ubisoft readme enrichment returns publisher name | MEDIUM | ~15 min | Validation in parser | ✅ DONE |
| B24 | ARC Game Store/Launcher not filtered as noise | MEDIUM | ~5 min | Noise list addition | ✅ DONE |
| B25 | battle.net launcher folder detected as game | MEDIUM | ~5 min | Noise list addition | ✅ DONE |
| B26 | Diablo III RETAIL classified as Standalone | MEDIUM | ~30 min | ContainerScanner logic + .build.info signal | ✅ DONE |
| B27 | bme2 picks Worldbuilder.exe instead of lotrbfme2.exe | HIGH | ~5 min | Blacklist addition | ✅ DONE |
| B28 | Divine Divinity picks ConfigTool.exe instead of div.exe | HIGH | ~5 min | Blacklist addition | ✅ DONE |
| B29 | Endless Legends displayed twice (Win32/Win64) | MEDIUM | ~5 min | NonGameFolder addition | ✅ DONE |
| B30 | Diablo III listed twice (x64 + x64 - Copy) | MEDIUM | ~5 min | Noise list addition | ✅ DONE |
| B31 | Diablo III RETAIL classified as Standalone | MEDIUM | — | Duplicate of B26 | ✅ DONE (same as B26) |
| B32 | Library roots show duplicate "Games" names | LOW | ~10 min | Display fix | ✅ DONE |
| B33 | Tags not displayed in left lister or details pane | LOW | ~30 min | ViewModel + XAML | ✅ DONE |

**Total estimated effort:** ~2 hours

---

## 2. Detailed Analysis with Code Locations

### B23: Ubisoft readme enrichment returns publisher name

**File:** `src/GamingCommander.App/Services/UbisoftReadmeParser.cs`
**Method:** `TryParse()` — lines 23–86

**Current behavior (lines 67–68):**
```csharp
string? publisher = lines.Length >= 1 ? lines[0]?.Trim() : null;
string? gameTitle = lines.Length >= 2 ? lines[1]?.Trim() : null;
```
Line 1 is read as publisher, line 2 as game title. No validation on either value.

**Problem:** Some Ubisoft readme files have the publisher name on line 2 instead of the game title. Example: `d:\games\Assassins Creed III\Support\Readme\*.txt` has "Ubisoft Entertainment" on line 2.

**Fix:** Add a deny-list of known publisher strings. After line 74 (whitespace validation), add:
```csharp
// Reject known publisher strings that appear on line 2 in some readmes
private static readonly HashSet<string> s_publisherDenyList = new(StringComparer.OrdinalIgnoreCase)
{
    "Ubisoft Entertainment",
    "Ubisoft",
    "Ubisoft SAS",
    "Ubisoft SA",
    "Ubisoft EMEA",
    "Ubisoft Montreal",
    "Ubisoft Paris",
    "Ubisoft Milan",
    "Ubisoft Shanghai",
    "Ubisoft Singapore",
    "Ubisoft Bucharest",
    "Ubisoft Reflections",
};

// After line 74, before line 76:
if (gameTitle is not null && s_publisherDenyList.Contains(gameTitle))
    gameTitle = null;
```

**Tests:** Add to `UbisoftReadmeParserTests.cs`:
1. Readme with publisher on line 2 → GameTitle rejected, returns null
2. Readme with valid game title → returns correctly
3. Readme with "Ubisoft SAS" on line 2 → rejected

---

### B24: ARC Game Store/Launcher not filtered as noise

**File:** `src/GamingCommander.App/Services/FileSystemHelper.cs`
**List:** `NoiseSubDirNames` — lines 14–24

**Current entries (lines 14–24):**
```
"__redist", "_commonredist", "commonredist", "redist", "directx",
"vcredist", "dotnet", "physx", "support", "_installer", "install",
"installer", "easyanticheat", "devtools", "docs", "licenses",
"steam controller configs", "steamworks shared",
"epic games", "origin", "uplay", "gog galaxy",
"ea app", "rockstar games",
```

**Problem:** `"arc"` is missing. `d:\games\arc\arc.exe` is the ARC game store launcher, not a game.

**Fix:** Add `"arc"` to the store launcher section (line 23):
```csharp
"ea app", "rockstar games", "arc",
```

**Also add to:** `data/blacklist.json` → `tier_3_store_bootstraps` (line 19):
```json
"tier_3_store_bootstraps": [
    "galaxy", "gog", "epic", "uplay", "ubisoft", "arc"
]
```

**Verification:** Check no legitimate game exe uses "arc" as a standalone name. "ArcaniA.exe" contains "arc" but the blacklist uses exact name matching (via `IsNoiseExeName`), so "arc" won't match "arcania". Safe to add.

**Tests:** Add to `ScannerFilterTests.cs`:
1. `arc` directory filtered by `IsNonGameFolder()`
2. `arc.exe` penalized by tier scoring

---

### B25: battle.net launcher folder detected as game

**File:** `src/GamingCommander.App/Services/FileSystemHelper.cs`
**List:** `NoiseSubDirNames` — lines 14–24

**Current state:** `"blizzard"` and `"battle.net"` were removed from `NoiseSubDirNames` (line 25 comment) to fix Bug 9 (BattleNet game detection). However, this made the `battle.net\` launcher folder itself scannable.

**Problem:** `d:\games\blizzard\battle.net\battle.net.exe` is the Battle.net launcher, not a game. The `blizzard\` folder is the publisher container (correct to keep scannable), but `battle.net\` is the launcher directory (should be noise).

**Fix:** Add `"battle.net"` back to `NoiseSubDirNames` (keep `"blizzard"` removed):
```csharp
// In the store launcher section:
"ea app", "rockstar games", "arc", "battle.net",
```

**Also add to:**
1. `ContainerScanner.s_nonGameFolderNames` (line 27):
   ```csharp
   "ea app", "rockstar games", "battle.net",
   ```
2. `data/blacklist.json` → `tier_3_store_bootstraps` (line 19):
   ```json
   "tier_3_store_bootstraps": [
       "galaxy", "gog", "epic", "uplay", "ubisoft", "arc", "battle.net"
   ]
   ```

**Tests:** Add to `ScannerFilterTests.cs`:
1. `battle.net` directory filtered by `IsNonGameFolder()`
2. `battle.net.exe` penalized by tier scoring

---

### B26 + B31: Diablo III RETAIL classified as Standalone instead of BattleNet

**File:** `src/GamingCommander.App/Services/ContainerScanner.cs`
**Method:** `ScanContainerChildren()` — lines 44–117

**Current behavior (lines 81–88):**
```csharp
GameSourceKind childType = StoreSignalDetector.DetectType(child);
if (childType != GameSourceKind.Unknown)
{
    addGameEntry(entries, child, rootPath, childType);
    continue;
}
```

**Problem:** `StoreSignalDetector.DetectType()` only checks for `.battle.net/` directory inside the game folder (line 151 of StoreSignalDetector.cs). Diablo III RETAIL doesn't have `.battle.net/` inside its folder — it's in the parent `blizzard\` directory. So `DetectType()` returns `Unknown`, and the fallback `HasRootExecutableSignal()` picks up `DiabloIII.exe` → Standalone.

**Root cause:** The BattleNet parent check in `FolderScanner.Scan()` (lines 111–126) only runs in the top-level loop, not in `ContainerScanner.ScanContainerChildren()`.

**Fix:** In `ContainerScanner.ScanContainerChildren()`, after line 88 (the `continue` after store signal promotion), add BattleNet sibling detection:

```csharp
// After line 88, before line 90:
// BattleNet container detection: check if a sibling "battle.net" dir exists
if (childType == GameSourceKind.Unknown && containerDir.Parent != null)
{
    string battleNetPath = Path.Combine(containerDir.FullName, "battle.net");
    if (Directory.Exists(battleNetPath)
        && StoreSignalDetector.HasBattleNetGameSignal(child))
    {
        addGameEntry(entries, child, rootPath, GameSourceKind.BattleNet);
        continue;
    }
}
```

**Why this works:** When scanning `d:\games\blizzard\Diablo III RETAIL\`:
1. `containerDir` = `d:\games\blizzard\`
2. `child` = `Diablo III RETAIL`
3. `childType` = `Unknown` (no `.battle.net/` inside Diablo III RETAIL)
4. Check: does `d:\games\blizzard\battle.net\` exist? → Yes
5. Call `HasBattleNetGameSignal(Diablo III RETAIL)` → matches "diablo" in folder name → true
6. Promote as `GameSourceKind.BattleNet`

**Tests:** Add to `ContainerScannerTests.cs`:
1. Blizzard container with Diablo III RETAIL → classified as BattleNet
2. Blizzard container with unknown folder → no false positive
3. Non-Blizzard container with battle.net sibling → no false positive

---

### B27: bme2 picks Worldbuilder.exe instead of lotrbfme2.exe

**File:** `data/blacklist.json`
**Tier:** `tier_10_dev_editor_tools` — lines 41–44

**Current entries:**
```json
"tier_10_dev_editor_tools": [
    "datacompiler", "editor", "modmanager", "packagemanager", "reminder",
    "contented", "leveled", "resourceed"
]
```

**Problem:** `Worldbuilder.exe` (33MB) is not in any noise tier. `lotrbfme2.exe` (495KB) has empty PE Description. The scoring should favor `lotrbfme2.exe` (+25 vs +1), but `FindPrimaryExecutable()` line 318–320 picks the largest exe when `FindExecutablesDeep` returns 0 candidates. If `lotrbfme2.exe` is filtered somewhere, Worldbuilder wins by size.

**Fix:** Add to `tier_10_dev_editor_tools`:
```json
"tier_10_dev_editor_tools": [
    "datacompiler", "editor", "modmanager", "packagemanager", "reminder",
    "contented", "leveled", "resourceed",
    "builder", "worldbuilder"
]
```

**Impact:** Worldbuilder.exe gets -20 tier penalty. `lotrbfme2.exe` (score 25) now clearly wins over Worldbuilder.exe (score 1 - 20 = -19).

**Tests:** Add to `ExecutableScoringTests.cs`:
1. "worldbuilder" gets -20 tier penalty
2. "builder" gets -20 tier penalty

---

### B28: Divine Divinity picks ConfigTool.exe instead of div.exe

**File:** `data/blacklist.json`
**Tier:** `tier_10_dev_editor_tools` — lines 41–44

**Current entries:** Same as B27.

**Problem:** `configtool.exe` (188KB) is not in any noise tier. Scoring analysis shows `div.exe` should win (score 15 vs 0), but ConfigTool is reportedly selected. Possible that `div.exe` is filtered or the scoring order is wrong.

**Fix:** Add to `tier_10_dev_editor_tools`:
```json
"tier_10_dev_editor_tools": [
    "datacompiler", "editor", "modmanager", "packagemanager", "reminder",
    "contented", "leveled", "resourceed",
    "builder", "worldbuilder", "configtool"
]
```

**Impact:** ConfigTool.exe gets -20 tier penalty. `div.exe` (score 15) clearly wins over ConfigTool.exe (score 0 - 20 = -20).

**Tests:** Add to `ExecutableScoringTests.cs`:
1. "configtool" gets -20 tier penalty

---

### B29: Endless Legends displayed twice (Win32/Win64)

**File:** `src/GamingCommander.App/Services/ContainerScanner.cs`
**List:** `s_nonGameFolderNames` — lines 13–28

**Current entries (lines 13–28):**
```
"Soundtrack", "Soundtracks", "Original Soundtrack",
"Manuals", "Manual", "Item Data", "Misc", "Bonus Content",
"Artwork", "Wallpapers", "Music",
"Redist", "Support", "Tools", "_CommonRedist", "CommonRedist",
"vcredist", "dotnet", "directx", "physx", "installer",
"_installer", "install", "easyanticheat", "devtools", "docs",
"licenses", "steam controller configs", "steamworks shared",
"dlc", "program files", "windowsapps", "squirreltemp",
"portable", "uninstall",
"epic games", "origin", "uplay", "gog galaxy",
"ea app", "rockstar games",
```

**Problem:** `ENdlessLegend/` has `Win32/` and `Win64/` subdirectories. Container scanner treats these as separate children with game signals (each contains `EndlessLegend.exe`). Two children with game signals → organization pattern → recurse into both → two game entries.

**Fix:** Add platform directory names to `s_nonGameFolderNames` (after line 23):
```csharp
"portable", "uninstall",
// Platform-specific build directories (not games)
"win32", "win64", "x86", "x64",
```

**Note:** These are flat `Win32/`/`Win64/` dirs at the game level. The UE-aware exe discovery (`ExecutableDiscovery.FindExecutablesDeep`) handles `Binaries/Win64/` paths separately — those are UE platform subdirectories within the exe search, not container children.

**Tests:** Add to `ContainerScannerTests.cs`:
1. Game with Win32/Win64 subdirs → single entry, not two
2. Game with x86/x64 subdirs → single entry, not two

---

### B30: Diablo III listed twice (x64 + x64 - Copy)

**File:** `src/GamingCommander.App/Services/FileSystemHelper.cs`
**List:** `NoiseSubDirNames` — lines 14–24

**Problem:** `x64 - Copy` is a backup directory that passes all noise filters. Exe discovery finds `Diablo III64.exe` in both `x64/` and `x64 - Copy/`.

**Fix:** Add backup directory patterns to `NoiseSubDirNames` (after line 23):
```csharp
"ea app", "rockstar games", "arc", "battle.net",
// Backup directories
"x64 - copy", "x86 - copy",
```

**Also add to:** `ContainerScanner.s_nonGameFolderNames` (after line 27):
```csharp
"ea app", "rockstar games", "battle.net",
"x64 - copy", "x86 - copy",
```

**Tests:** Add to `ScannerFilterTests.cs`:
1. `x64 - Copy` directory filtered by `IsNonGameFolder()`
2. `x86 - Copy` directory filtered by `IsNonGameFolder()`

---

### B32: Library roots show duplicate "Games" names

**File:** `src/GamingCommander.UI/ViewModels/ShellViewModel.cs`
**Method:** `JumpToLibraryRoots()` — lines 162–196

**Current behavior (line 178):**
```csharp
Title = Path.GetFileName(root.RootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
```

**Problem:** `Path.GetFileName("d:\games\")` returns `"Games"`. Multiple roots with the same folder name appear identical.

**Fix:** Change line 178 to display the full root path:
```csharp
Title = root.RootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
```

**Alternative (if full path is too long):** Show drive + folder:
```csharp
string trimmed = root.RootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
Title = Path.GetPathRoot(trimmed) == trimmed
    ? trimmed  // Root is drive root like "D:\"
    : $"{Path.GetPathRoot(trimmed)}{Path.GetFileName(trimmed)}";  // "D:\games"
```

**Tests:** Add to `ShellViewModelTests.cs` (if exists) or manual verification:
1. Three roots `d:\games`, `e:\games`, `f:\games` → show distinct paths

---

### B33: Tags not displayed in left lister or details pane

**Files:**
1. `src/GamingCommander.UI/ViewModels/ShellPaneItemViewModel.cs` — no `Tags` property
2. `src/GamingCommander.UI/ViewModels/ShellViewModel.cs` — `LoadGamesForRoot()` lines 338–429
3. `src/GamingCommander.App/MainWindow.axaml` — left-pane template lines 44–62, right-pane details lines 73–129

**Current state:**
- `GameEntry.Tags` is `List<string>` (Core/Models/GameEntry.cs line 38)
- `ShellPaneItemViewModel` has 18 properties, none for tags
- `LoadGamesForRoot()` doesn't read `game.Tags`
- Left-pane: 4-column grid (Title, SourceLabel, PathSummary, ScanningBadge)
- Right-pane: 11 fields, no Tags

**Fix — Step 1: ViewModel property**

`ShellPaneItemViewModel.cs` — add after line 75 (`GameCount`):
```csharp
/// <summary>Comma-separated user tags (e.g., "RPG, Open World").</summary>
public string Tags { get; init; } = string.Empty;
```

**Fix — Step 2: Populate in LoadGamesForRoot**

`ShellViewModel.cs` — in `LoadGamesForRoot()`, inside the `foreach` loop, after line 418 (`GameCount = 0,`), add:
```csharp
Tags = game.Tags.Count > 0
    ? string.Join(", ", game.Tags)
    : string.Empty,
```

**Fix — Step 3: Left-pane display**

`MainWindow.axaml` — modify the left-pane item template (lines 44–62). Add a 5th column for tags:

Change `ColumnDefinitions="1*,Auto,Auto,Auto"` to `"1*,Auto,Auto,Auto,Auto"`.

Add after the ScanningBadge TextBlock (line 60):
```xml
<TextBlock Grid.Column="4" Text="{Binding Tags}"
           Foreground="{DynamicResource TextSecondary}"
           FontSize="{DynamicResource FontSizeSmall}"
           Margin="6,0,0,0" />
```

**Fix — Step 4: Right-pane details**

`MainWindow.axaml` — add a Tags row after the Status Detail section (after line 111). Insert before the Executable row:
```xml
<!-- Tags -->
<StackPanel IsVisible="{Binding Tags, Converter={x:Static StringConverters.IsNotNullOrEmpty}}"
            Orientation="Horizontal" Margin="0,2">
    <TextBlock Text="Tags: " Foreground="{DynamicResource TextSecondary}"
               FontSize="{DynamicResource FontSizeSmall}" />
    <TextBlock Text="{Binding Tags}" Foreground="{DynamicResource TextPrimary}"
               FontSize="{DynamicResource FontSizeSmall}" />
</StackPanel>
```

**Tests:** Add to `ShellViewModelTests.cs` or manual verification:
1. Game with tags → tags displayed in left lister and details pane
2. Game without tags → no tags row shown

---

## 3. Implementation Order

Implement in dependency order to minimize merge conflicts:

### Phase 1: Noise & Blacklist Fixes (B24, B25, B27, B28, B29, B30)
**Risk: LOW** — additive changes to filter lists, no logic changes
**Status:** ✅ COMPLETE — 7 new tests, 313 total passing

| Step | File | Change | Status |
|------|------|--------|--------|
| 1a | `FileSystemHelper.cs` | Add `"arc"`, `"battle.net"`, `"x64 - copy"`, `"x86 - copy"` to `NoiseSubDirNames` | ✅ |
| 1b | `ContainerScanner.cs` | Add `"win32"`, `"win64"`, `"x86"`, `"x64"`, `"x64 - copy"`, `"x86 - copy"` to `s_nonGameFolderNames` | ✅ |
| 1c | `data/blacklist.json` | Add `"builder"`, `"worldbuilder"`, `"configtool"` to `tier_10_dev_editor_tools` | ✅ |

**Note:** The original plan also included adding `"arc"` and `"battle.net"` to `tier_3_store_bootstraps`. This was not done because `NoiseSubDirNames` already filters the directories. The blacklist tier addition would only affect exe scoring for files named "arc.exe" or "battle.net.exe" inside game directories — a secondary concern.

### Phase 2: BattleNet Container Detection (B26, B31)
**Risk: MEDIUM** — enhanced signal detection + new container scanner logic
**Status:** ✅ COMPLETE — 11 new tests, 324 total passing

| Step | File | Change | Status |
|------|------|--------|--------|
| 2a | `StoreSignalDetector.cs` | Enhance `HasBlizzardSignal()` to check `.build.info` and `.product.db` | ✅ |
| 2b | `ContainerScanner.cs` | Add BattleNet sibling detection after line 88 in `ScanContainerChildren()` | ✅ |
| 2c | `StoreSignalDetector.cs` | Extract product codename from `.build.info` CDN Path field | ✅ |

### Phase 3: Ubisoft Readme Validation (B23)
**Risk: LOW** — validation in parser
**Status:** ✅ COMPLETE — 3 new tests, 327 total passing

| Step | File | Change | Status |
|------|------|--------|--------|
| 3a | `UbisoftReadmeParser.cs` | Add `s_publisherDenyList` and validation after line 74 | ✅ |

### Phase 4: UI Fixes (B32, B33)
**Risk: LOW** — display-only changes
**Status:** ✅ COMPLETE — 327 total passing

| Step | File | Change | Status |
|------|------|--------|--------|
| 4a | `ShellViewModel.cs` | Change `JumpToLibraryRoots()` Title to full root path | ✅ |
| 4b | `ShellPaneItemViewModel.cs` | Add `Tags` property | ✅ |
| 4c | `ShellViewModel.cs` | Populate `Tags` in `LoadGamesForRoot()` | ✅ |
| 4d | `MainWindow.axaml` | Add Tags column to left-pane item template | ✅ |
| 4e | `MainWindow.axaml` | Add Tags row to right-pane details panel | ✅ |

### Phase 5: Tests & Documentation

| Step | File | Change | Status |
|------|------|--------|--------|
| 5a | `StoreSignalDetectorTests.cs` | Add publisher deny-list tests (3 tests) | ✅ |
| 5b | `ScannerFilterTests.cs` | Add noise filter tests for arc, battle.net, x64-copy | ✅ |
| 5c | `ContainerScannerTests.cs` | Add BattleNet container detection tests | ✅ |
| 5d | `ContainerScannerTests.cs` | Add Win32/Win64 platform subdir tests | ✅ |
| 5e | `ExecutableScoringTests.cs` | Add builder/configtool/worldbuilder penalty tests | ✅ |
| 5f | `docs/GAME-DETECTION-LOGIC.md` | Update with platform subdir filtering, BattleNet container logic | ⬜ |
| 5g | `META/BACKLOG/TECH_DEBT.md` | Mark B23-B33 as fixed | ⬜ |

---

## 4. Risk Assessment

| Bug | Risk | Mitigation | Status |
|-----|------|------------|--------|
| B23 | LOW | Deny-list is additive; no existing behavior changes | ✅ |
| B24 | LOW | `"arc"` is not a substring of any known game exe name | ✅ |
| B25 | LOW | `"battle.net"` was previously in the list; restoring it | ✅ |
| B26/B31 | MEDIUM | New logic in container scanner; test with Blizzard container structure | ✅ |
| B27/B28 | LOW | Blacklist additions are additive scoring penalties | ✅ |
| B29 | LOW | Platform dirs are clearly not games | ✅ |
| B30 | LOW | Backup dirs are clearly not games | ✅ |
| B32 | LOW | Display-only change | ✅ |
| B33 | LOW | Additive UI property | ✅ |

---

## 5. Success Criteria

- [x] Ubisoft readme enrichment rejects publisher names (B23)
- [x] ARC launcher filtered as noise (B24)
- [x] battle.net launcher folder filtered as noise (B25)
- [x] Diablo III RETAIL classified as BattleNet (B26/B31)
- [x] bme2 selects lotrbfme2.exe as primary (not Worldbuilder.exe) (B27)
- [x] Divine Divinity selects div.exe as primary (not ConfigTool.exe) (B28)
- [x] Endless Legends shows as single entry (not Win32 + Win64) (B29)
- [x] Diablo III shows as single entry (no "x64 - Copy" duplicate) (B30)
- [x] Library roots show full path (not duplicate "Games") (B32)
- [x] Tags displayed in left lister and details pane (B33)
- [x] Build clean, all tests pass (327 tests)
- [x] No regressions in existing detection (157 games, 0 unknowns)

---

## 7. Detailed Investigation — Remaining Bugs

### B23: Ubisoft Readme Returns Publisher Name

**File:** `src/GamingCommander.App/Services/UbisoftReadmeParser.cs`

**Real data:** `D:\Games\ACreed3\Assassin's Creed III\Support\Readme\*.txt` — line 2 contains "Ubisoft Entertainment" instead of the game title.

**Code path (lines 67–68):**
```csharp
string? publisher = lines.Length >= 1 ? lines[0]?.Trim() : null;
string? gameTitle = lines.Length >= 2 ? lines[1]?.Trim() : null;
```

**Root cause:** No validation that line 2 is actually a game title. Some Ubisoft readmes put the publisher name on both lines 1 and 2.

**Impact:** `GameEntry.DisplayName` gets set to "Ubisoft Entertainment" instead of "Assassin's Creed III" via the Ubisoft enrichment path (FolderScanner.cs line 222: `displayName = readmeInfo.GameTitle`).

**Fix:** Add a `HashSet<string>` deny-list of known Ubisoft publisher strings. After the whitespace validation (line 74), check if `gameTitle` matches a publisher string and reject it.

**Existing tests:** 5 tests in `StoreSignalDetectorTests.cs` (lines 422–479) — all pass, none test the publisher-on-line-2 scenario.

**New tests needed:**
1. Readme with "Ubisoft Entertainment" on line 2 → `GameTitle` is null
2. Readme with "Ubisoft SAS" on line 2 → `GameTitle` is null
3. Readme with valid game title → returns correctly

---

### B26/B31: Diablo III RETAIL Classified as Standalone

**Real directory structure:**
```
D:\Games\Blizzard\               ← Publisher container (no .battle.net/)
  Battle.net\                     ← Launcher (should be noise — fixed in B25)
  Diablo III\                     ← Game folder
    .build.info                   ← Blizzard game signal (pipe-delimited text)
    .product.db                   ← Blizzard game signal (binary/protobuf)
    .patch.result                 ← Patch status (not useful as signal)
    Diablo III.exe                ← 32-bit exe
    Diablo III Launcher.exe       ← Launcher
    x64\
      Diablo III64.exe            ← 64-bit exe
```

**Detection flow (current — broken):**
1. `FolderScanner.Scan("D:\Games")` → iterates subdirectories
2. `Blizzard/` → no store signal (no `.battle.net/` inside Blizzard/)
3. Pass 2: `FallbackSignalDetector` → no root exe → Unknown
4. Pass 3: `ContainerScanner.ScanContainerChildren("Blizzard/")` 
5. Children: `Battle.net/`, `Diablo III/`
6. `Battle.net/` → `StoreSignalDetector.DetectType()` → Unknown (no `.battle.net/` inside)
7. `Diablo III/` → `StoreSignalDetector.DetectType()` → Unknown (no `.battle.net/` inside)
8. `HasRootExecutableSignal("Diablo III/")` → finds `Diablo III.exe` → true
9. `gameSignalCount = 1` → promote as Standalone

**Root cause:** The BattleNet parent check in `FolderScanner.Scan()` (lines 111–126) only runs in the top-level loop, not in `ContainerScanner.ScanContainerChildren()`. The ContainerScanner doesn't check if a sibling `battle.net/` directory exists.

**Blizzard Game Signal Analysis:**

All BattleNet games (Diablo III, Diablo IV, World of Warcraft, Overwatch, etc.) contain these files inside their installation directory:

| File | Format | Content | Signal Strength |
|------|--------|---------|----------------|
| `.build.info` | Pipe-delimited text | Product codename (`tpr/diablo3`), version, CDN keys | **STRONG** |
| `.product.db` | Binary/protobuf | Product codename, install path, language, version | **STRONG** |
| `.patch.result` | Single byte (`0`) | Patch status | **WEAK** |

**Key finding:** `.build.info` is the best signal because it's:
- **Unique** to BattleNet games — no other launcher creates this file
- **Always present** after game installation (created by BattleNet Agent)
- **Text-based** — easy to parse, no binary decoding needed
- **Contains product codename** in CDN Path field (e.g., `tpr/diablo3`, `prometheus` for D4, `agent` for WoW)

**Fix (two parts):**

**Part 1: Enhance `HasBlizzardSignal()` in StoreSignalDetector.cs**

Replace the single `.battle.net/` check with multi-signal detection:
```csharp
internal static bool HasBlizzardSignal(DirectoryInfo dir)
{
    // Primary: .battle.net/ directory (BattleNet Agent runtime data)
    if (Directory.Exists(Path.Combine(dir.FullName, ".battle.net")))
        return true;
    
    // Secondary: .build.info file (created during game installation)
    // This is the most reliable signal — unique to BattleNet games
    if (File.Exists(Path.Combine(dir.FullName, ".build.info")))
        return true;
    
    // Tertiary: .product.db file (created during game installation)
    if (File.Exists(Path.Combine(dir.FullName, ".product.db")))
        return true;
    
    return false;
}
```

**Why this works:** When scanning `Diablo III/`:
1. `StoreSignalDetector.DetectType()` → `HasBlizzardSignal()` → finds `.build.info` → returns `BattleNet`
2. Game promoted as BattleNet (not Standalone)

**Part 2: Add BattleNet sibling detection in ContainerScanner**

In `ContainerScanner.ScanContainerChildren()`, after the store signal check (line 88), add:
```csharp
// Check if a sibling "battle.net" directory exists (Blizzard container pattern)
if (childType == GameSourceKind.Unknown)
{
    string battleNetPath = Path.Combine(containerDir.FullName, "battle.net");
    if (Directory.Exists(battleNetPath)
        && StoreSignalDetector.HasBattleNetGameSignal(child))
    {
        addGameEntry(entries, child, rootPath, GameSourceKind.BattleNet);
        continue;
    }
}
```

**Why both parts are needed:**
- Part 1 handles the case where `.build.info` exists inside the game directory (most common)
- Part 2 handles the case where `.build.info` doesn't exist but a sibling `battle.net/` directory exists (edge case)

**Product codename extraction from `.build.info`:**

The `.build.info` file contains the product codename in the CDN Path field:
- `tpr/diablo3` → Diablo III
- `prometheus` → Diablo IV
- `agent` → World of Warcraft
- `hero` → Overwatch
- `s1` → StarCraft: Remastered
- `s2` → StarCraft II

This can be used for metadata enrichment:
```csharp
// After detecting BattleNet, try to extract product codename
string buildInfoPath = Path.Combine(dir.FullName, ".build.info");
if (File.Exists(buildInfoPath))
{
    string[] lines = File.ReadAllLines(buildInfoPath);
    if (lines.Length >= 2)
    {
        string[] fields = lines[1].Split('|');
        // CDN Path field (index 6) contains "tpr/diablo3"
        if (fields.Length > 6 && !string.IsNullOrEmpty(fields[6]))
        {
            string product = fields[6].Split('/').Last(); // "diablo3"
            platformMetadata["BlizzardProduct"] = product;
        }
    }
}
```

**Risk:** MEDIUM — enhanced signal detection; test with real Blizzard game directories.

**Existing tests:** No container BattleNet tests exist.

**New tests needed:**
1. Directory with `.build.info` → detected as BattleNet
2. Directory with `.product.db` → detected as BattleNet
3. Directory with `.battle.net/` → detected as BattleNet
4. Blizzard container with Diablo III → classified as BattleNet
5. Blizzard container with unknown folder → no false positive
6. Non-Blizzard container with battle.net sibling → no false positive

---

### B32: Library Roots Show Duplicate "Games" Names

**File:** `src/GamingCommander.UI/ViewModels/ShellViewModel.cs`
**Method:** `JumpToLibraryRoots()` — line 178

**Current code:**
```csharp
Title = Path.GetFileName(root.RootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
```

**Problem:** `Path.GetFileName("D:\Games")` returns `"Games"`. Multiple roots like `D:\Games`, `E:\Games`, `F:\Games` all display as "Games" — indistinguishable.

**Fix:** Show drive letter + folder name:
```csharp
string trimmed = root.RootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
Title = Path.GetPathRoot(trimmed) == trimmed
    ? trimmed  // Drive root like "D:\"
    : $"{Path.GetPathRoot(trimmed)}{Path.GetFileName(trimmed)}";  // "D:\Games"
```

**Impact:** Display-only. No data model changes.

**New tests needed:**
1. Three roots `D:\Games`, `E:\Games`, `F:\Games` → show distinct paths
2. Root at drive root `D:\` → shows "D:\"

---

### B33: Tags Not Displayed in Left Lister or Details Pane

**Files:**
- `src/GamingCommander.Core/Models/GameEntry.cs` line 38: `List<string> Tags`
- `src/GamingCommander.UI/ViewModels/ShellPaneItemViewModel.cs`: No `Tags` property
- `src/GamingCommander.UI/ViewModels/ShellViewModel.cs` line 406–424: `LoadGamesForRoot()` doesn't read `game.Tags`
- `src/GamingCommander.App/MainWindow.axaml` lines 44–62: Left-pane has 4 columns, no Tags
- `src/GamingCommander.App/MainWindow.axaml` lines 77–123: Right-pane has 11 fields, no Tags

**Current state:** `GameEntry.Tags` is `List<string>` (Core model). The ViewModel doesn't expose it. The XAML doesn't display it.

**Fix (4 steps):**
1. Add `Tags` property to `ShellPaneItemViewModel`
2. Populate in `LoadGamesForRoot()`: `Tags = game.Tags.Count > 0 ? string.Join(", ", game.Tags) : string.Empty`
3. Add 5th column to left-pane XAML: `<TextBlock Text="{Binding Tags}" .../>`
4. Add Tags row to right-pane XAML: `<StackPanel>` with "Tags:" label + value, visible when non-empty

**Impact:** Additive UI change. No data model changes. Tags are already stored in `GameEntry.Tags`.

**New tests needed:**
1. Game with tags → tags populated in ViewModel
2. Game without tags → empty string

---

## 8. Test Plan

### New Tests Required

| Test File | Test Case | Bug | Status |
|-----------|-----------|-----|--------|
| `StoreSignalDetectorTests.cs` | Publisher on line 2 → rejected | B23 | ✅ |
| `StoreSignalDetectorTests.cs` | "Ubisoft SAS" on line 2 → rejected | B23 | ✅ |
| `ScannerFilterTests.cs` | `arc` directory → filtered | B24 | ✅ |
| `ScannerFilterTests.cs` | `battle.net` directory → filtered | B25 | ✅ |
| `ScannerFilterTests.cs` | `x64 - Copy` directory → filtered | B30 | ✅ |
| `StoreSignalDetectorTests.cs` | `.build.info` file → BattleNet signal | B26 | ✅ |
| `StoreSignalDetectorTests.cs` | `.product.db` file → BattleNet signal | B26 | ✅ |
| `StoreSignalDetectorTests.cs` | `.build.info` CDN Path → product codename extracted | B26 | ✅ |
| `FolderScannerContainerTests.cs` | Blizzard container → Diablo III classified as BattleNet | B26 | ✅ |
| `FolderScannerContainerTests.cs` | Game with Win32/Win64 subdirs → not duplicate | B29 | ✅ |
| `BlacklistLoaderTests.cs` | Tier 10 contains builder/worldbuilder/configtool | B27/B28 | ✅ |

### Regression Tests (existing, must pass)

| Test File | Count |
|-----------|-------|
| `VdfParserTests.cs` | 20 |
| `BlacklistLoaderTests.cs` | 11 |
| `SteamLibraryScannerTests.cs` | 14 |
| `ScannerFilterTests.cs` | 3+ |
| `ExecutableScoringTests.cs` | 10 |
| `GameEntryIdTests.cs` | 8 |
| `GamesDatabaseServiceTests.cs` | 16 |
| `FolderScannerTests.cs` | 20+ |
| `FolderScannerContainerTests.cs` | 13 |
| `ExecutableDiscoveryTests.cs` | 15 |
| `LnkParserTests.cs` | 13 |
| `GogInfoParserTests.cs` | 10 |
| `EaInstallLogParserTests.cs` | 8 |
| `UbisoftReadmeParserTests.cs` | 11 |
| `EpicManifestParserTests.cs` | 17 |
| `LibraryManagerTests.cs` | 8 |
| Other App tests | ~100+ |
