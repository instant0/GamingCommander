# Plan 112 — Scan Performance, Display Names, and Ubisoft Signals

**Status:** DRAFT
**Audience:** Builder
**Priority:** P1 (user-facing bugs + performance)
**Depends on:** None

---

## 0. Problem Statement

Three user-reported issues from testing `E:\games` scan:

1. **Scan is very slow** — RESMON shows GamingCommander reading entire .exe files (e.g., 4× 500MB Ghost Recon Breakpoint exes read in full). The scan provides no progress feedback.
2. **Display names are wrong** — Games show abbreviated folder names instead of real titles:
   - `arx` → should be `Arx Fatalis`
   - `doom6` → should be `DOOM` (or `DOOM 64`)
   - `galciv4` → should be `Galactic Civilizations IV: Supernova`
   - `DR3` → should be `Dead Rising 3`
   - `hadse2` → should be `Hades II`
   - `GearsJack` → should be `Gears Tactics`
   - `GOT` → should be `Game Of Thrones`
   - `HORGOW` → should be `Horizon Forbidden West`
3. **Ubisoft games not detected** — Ghost Recon Breakpoint has no `uplay_install.manifest` or loader DLLs at root. Current Ubisoft signals miss modern Ubisoft Connect games. Additional signals exist: `uplay_download/` folder, `support/Readme` metadata file, `*_UPP*` executables.

---

## 1. Root Cause Analysis

### 1A. Full File Reads — Ruled Out as FileVersionInfo, Root Cause Unknown

**What we know:**
- `FileVersionInfo.GetVersionInfo()` confirmed NOT the cause — user ran the same .NET Diagnostic API in PowerShell and there was no indication of full exe reads.
- `FileInfo.Length` calls `GetFileSizeEx` (metadata-only, not a content read).
- No other code path reads `.exe` file contents (verified via grep: no `ReadAllBytes`, `FileStream`, `BinaryReader` on `.exe` files).
- RESMON shows GamingCommander (not Defender) as the read source.
- The code paths that touch `.exe` files in order of scan: `FileSystemHelper.GetFilesSafe(subDir, "*.exe")` (directory listing only), `Directory.EnumerateFiles(..., "*.exe")` (directory listing only), `new FileInfo(exePath).Length` (metadata), `FileVersionInfo.GetVersionInfo(exePath)` (confirmed not the cause).

**What remains unexplained:**
- Something in GamingCommander is reading full .exe files per RESMON, but all known code paths are ruled out.
- Possible causes still under investigation:
  - A runtime or library side-effect not visible in application code
  - A file system filter driver that attributes reads to the requesting process
  - An interaction between multiple sequential file opens on the same large exe (e.g., `GetFilesSafe` + `FileInfo.Length` + `FileVersionInfo` on the same file triggering cumulative I/O attribution in RESMON)

**Action required:** This needs investigation on Windows with the actual game library. Add `Stopwatch`-based timing around the exe-touching operations in `ScoreExecutable()` to identify which operation is slow, and whether it correlates with the I/O RESMON reports.

### 1B. Display Names — Definitive Bug

**Current pipeline in `FolderScanner.AddGameEntry()`:**
```
displayName = FileSystemHelper.NormalizeDisplayName(subDir.Name)
→ GOG enrichment overrides if GogInfoParser finds title
→ EA enrichment overrides if EaInstallLogParser finds display name
→ Epic enrichment overrides if EpicManifestParser finds display name
→ Standalone/Ubisoft/SteamEmu/other: NO enrichment → stuck with folder name
```

**The data proves PE `FileDescription` has the correct title for almost every game:**

| Folder Name | PE FileDescription | Current Display |
|-------------|-------------------|-----------------|
| `arx` | (empty) | `arx` |
| `doom6` | `DOOM` | `doom6` |
| `galciv4` | `Galactic Civilizations IV: Supernova` | `galciv4` |
| `DR3` | `Dead Rising 3` | `DR3` |
| `DeathStranding` | `Death Stranding` | `DeathStranding` |
| `Diablo Immortal` | (empty on root exe, launcher has title) | `Diablo Immortal` |
| `Dragon Age Inquisition` | `Dragon Age™: Inquisition` | `Dragon Age Inquisition` |
| `Everspace2` | `EVERSPACE 2` (on shipping exe) | `Everspace2` |
| `Far Cry 3 Blood Dragon` | `Far Cry 3 Blood Dragon` (on bin/exe) | `Far Cry 3 Blood Dragon` ✓ |
| `Ghost Recon Breakpoint` | (empty on all 4 exes) | `Ghost Recon Breakpoint` ✓ |

**Problem:** `ScoreExecutable()` already reads `FileVersionInfo` for every candidate but discards the `FileDescription` after scoring. The data is there, it's just not used for naming.

**Edge cases:**
- Some exes have empty FileDescription (Ghost Recon Breakpoint, Arx Fatalis)
- Some exes have localized descriptions (Chinese for Diablo Immortal tools)
- Folder name is sometimes already correct (Far Cry 3 Blood Dragon)

### 1C. Ubisoft Signal Gap — Definitive Bug

**Current Ubisoft detection signals:**
| Signal | Method | Status |
|--------|--------|--------|
| `uplay_install.manifest` at root | `HasUbisoftSignal()` | ✅ Works for older games |
| `uplay_r*_loader*.dll` at root | `HasUbisoftSignal()` | ✅ Works for older games |
| `uplay_loader*` + INI with Username/AccountId | `HasUbisoftEmulatorSignal()` | ✅ Works for emulated |
| `UbiStats.dll` at root/child | `HasUbisoftLegacySignal()` | ✅ Works for legacy |

**Missing signals (from user observation on Ghost Recon Breakpoint):**
| Signal | Evidence | Priority |
|--------|----------|----------|
| `uplay_download/` folder | Present in Ghost Recon Breakpoint | HIGH — reliable indicator of Ubisoft Connect installation |
| `support/Readme` folder with publisher info | Ubisoft games ship `Support/Readme/` containing first 4 lines with publisher and game title | HIGH — detection signal AND metadata enrichment source |
| `*_UPP*.exe` executables | `GRB_UPP.exe`, `GRB_UPP_vulkan.exe` — Uplay Plus subscription variants | HIGH — signal for Ubisoft detection (distinct from noise filtering for exe picking) |

**Ghost Recon Breakpoint has NONE of the current signals.** It falls through to Pass 2 → `HasRootExecutableSignal()` → Standalone. This is wrong — it should be `UbisoftConnect`.

**Note on GRB noise filtering:** `_upp` is in `blacklist.json` at `tier_12_trial_demo_stub`. However, due to a C# DTO property name mismatch (see Section 8 Finding #1), `_upp` is **NOT currently loaded** by the C# code. Regardless, the signal detection for `*_UPP*` (Step 3C) is independent of noise filtering. If the tier bug is fixed, `GRB_UPP.exe` and `GRB_UPP_vulkan.exe` would be filtered as noise during `FindExecutablesDeep()`. The actual candidates would then be `GRB.exe` (536MB) and `GRB_vulkan.exe` (519MB) — 2 candidates, not 4. Both have empty FileDescription and empty InternalName in the PE data.

---

## 2. Implementation Plan

### Step 1 (DEFERRED): Investigate Full File Read Behavior

**Goal:** Identify which file-touching operation causes the I/O RESMON attributes to GamingCommander.

**Status: DEFERRED.** The root cause is suspected to be external (runtime/file system filter driver), and all known code paths have been ruled out. Instrumentation can be added later if needed. The remaining detection/performance fixes provide more immediate value.

### PE Metadata Reading — Evaluation

**Current approach:** `System.Diagnostics.FileVersionInfo.GetVersionInfo(exePath)` — built into .NET, no external dependencies.

**How it works on Windows:** Calls Win32 `GetFileVersionInfoEx` which reads the `VERSIONINFO` resource from the PE file. This reads only the resource section (typically a few KB), not the entire exe. The I/O is disk-seek-intensive (one seek per file), not CPU-intensive.

**Alternatives evaluated:**

| Approach | Pros | Cons | Verdict |
|----------|------|------|---------|
| `FileVersionInfo.GetVersionInfo()` (current) | Built-in, well-tested, returns rich object (FileDescription, InternalName, ProductName, etc.) | I/O-bound on HDDs (seek per file), no control over read pattern | **Keep** — sufficient for our use case |
| `PEReader` (System.Reflection.PortableExecutable) | Built-in, `PrefetchMetadata` option reads only metadata section, more I/O control | Returns raw PE headers — must manually parse VERSIONINFO resource for FileDescription; more complex code | **Future optimization** — if profiling shows FileVersionInfo is a bottleneck |
| PeNet (third-party) | Full PE parser, easy API | External dependency, overkill for FileDescription only | **Not needed** |
| Direct PE header parsing | Maximum control, minimum I/O | Significant implementation effort, fragile | **Not needed** |

**Recommendation:** Keep `FileVersionInfo.GetVersionInfo()`. The main performance wins come from:
1. **Step 4C:** Skipping PE read entirely for noise candidates (most impactful)
2. **Step 4B:** Caching FileInfo.Length to avoid redundant syscalls
3. **Step 5:** Async scanning to move I/O off the UI thread

If profiling later shows `FileVersionInfo` itself is slow (unlikely — it's just a resource read), `PEReader` with `PrefetchMetadata` is the upgrade path. No code change needed now.

### Step 1: Fix Blacklist JSON/C# DTO Tier Name Mismatch

**Goal:** Make the C# `BlacklistLoader` correctly deserialize all 20 tiers from `blacklist.json`.

**Problem:** Tiers 5-12 in `blacklist.json` use key names like `tier_5_error_crash_reporting` but the C# DTO properties expect `tier_5_unreal_build_debug`. This silently drops ~40 noise patterns from the tiered list.

**File:** `BlacklistLoader.cs` — rename `[JsonPropertyName]` attributes to match current JSON keys.

| Current Property Name | Current JsonPropertyName | Correct JsonPropertyName |
|----------------------|-------------------------|-------------------------|
| `Tier5UnrealBuildDebug` | `tier_5_unreal_build_debug` | `tier_5_error_crash_reporting` |
| `Tier6CrashReporting` | `tier_6_crash_reporting` | `tier_6_drm_wrappers` |
| `Tier7DrmWrappers` | `tier_7_drm_wrappers` | `tier_7_installer_utilities` |
| `Tier8InstallerUtilities` | `tier_8_installer_utilities` | `tier_8_server_loader_stub` |
| `Tier9ServerLoaderStub` | `tier_9_server_loader_stub` | `tier_9_distribution_tools` |
| `Tier10DistributionTools` | `tier_10_distribution_tools` | `tier_10_dev_editor_tools` |
| `Tier11DevEditorTools` | `tier_11_dev_editor_tools` | `tier_11_utilities_debug` |
| `Tier12UtilitiesDebug` | `tier_12_utilities_debug` | `tier_12_trial_demo_stub` |
| `Tier13TrialDemoStub` | `tier_13_trial_demo_stub` | `tier_13_media_codec_tools` |

**Also rename the C# property names** to match the content (e.g., `Tier5ErrorCrashReporting`) for clarity. Update `GetTieredTiers()` yield statements accordingly. This is a rename-only change — no logic changes.

**Risk:** Low. Deserialization uses `[JsonPropertyName]` which is just string matching. Renaming properties + updating JSON key references is mechanical.

**Test:** Existing `BlacklistLoaderTests` should continue passing. Add a test that verifies `_upp` and `crash` are present in the loaded `TieredExePatterns`.

### Step 2: Use PE FileDescription for Display Name

**Goal:** When no store-specific enrichment is available, use PE `FileDescription` from the primary executable as the display name.

**Changes to `ExecutableDiscovery.cs`:**
- `ScoreExecutable()` already reads `FileVersionInfo` — capture `FileDescription` alongside the score
- New return type: `(int Score, string? FileDescription)` or add an output parameter
- `FindPrimaryExecutable()` propagates the `FileDescription` from the winning candidate

**Changes to `FolderScanner.cs` — `AddGameEntry()`:**
- After exe discovery and enrichment, for games where `displayName` still equals `NormalizeDisplayName(folderName)`:
  - If `FileDescription` from PE is non-empty and doesn't match the exe name → use it
  - Store original folder name in `PlatformMetadata["AutoDetectedTitle"]` and `"TitleSource" = "PeFileDescription"`
- Guard conditions (don't override if):
  - `FileDescription` is empty
  - `FileDescription` equals the exe filename (e.g., `"DOOM"` for `DOOM.exe` when folder is `doom6` — actually this IS useful)
  - `FileDescription` is a noise string (e.g., `"Setup/Uninstall"`, `"Microsoft Visual C++"`)
  - The display name was already enriched by GOG/EA/Epic

**Expected results:**
| Folder | PE FileDescription | New Display Name |
|--------|-------------------|------------------|
| `arx` | (empty) | `arx` (unchanged — no PE data) |
| `doom6` | `DOOM` | `DOOM` |
| `galciv4` | `Galactic Civilizations IV: Supernova` | `Galactic Civilizations IV: Supernova` |
| `DR3` | `Dead Rising 3` | `Dead Rising 3` |
| `DeathStranding` | `Death Stranding` | `Death Stranding` |
| `Everspace2` | (root exe empty; shipping exe has `EVERSPACE 2`) | `EVERSPACE 2` |
| `GearsJack` | `Gears Tactics` (on primary exe) | `Gears Tactics` |
| `HORGOW` | `Horizon Forbidden West™ Complete Edition` | `Horizon Forbidden West Complete Edition` |
| `Ghost Recon Breakpoint` | (empty) | `Ghost Recon Breakpoint` (unchanged — no PE data, folder name is already good) |

**Note:** GRB has empty FileDescription on ALL exes (including GRB.exe, GRB_vulkan.exe). The folder name "Ghost Recon Breakpoint" is already the best available name. The Ubisoft `support/Readme` enrichment (Step 3B) would provide an alternative source for this game.

### Step 3: Add Ubisoft Signals

**File:** `StoreSignalDetector.cs`

**3A. Add `uplay_download/` folder signal:**
```csharp
// In HasUbisoftSignal() — add:
if (Directory.Exists(Path.Combine(dir.FullName, "uplay_download")))
    return true;
```

**3B. Add Ubisoft `support/Readme` metadata enrichment:**

New method `UbisoftReadmeParser.TryParse()`:
- Search `Support/Readme/` (case-insensitive) for text files
- Read first 4 lines — Ubisoft convention: line 1 = publisher, line 2 = game title, line 3 = copyright, line 4 = blank
- Return: `(string? Publisher, string? GameTitle)`

Integration in `FolderScanner.AddGameEntry()`:
```csharp
if (resolvedType == GameSourceKind.UbisoftConnect)
{
    var ubiInfo = UbisoftReadmeParser.TryParse(subDir);
    if (ubiInfo?.GameTitle is not null)
    {
        platformMetadata["AutoDetectedTitle"] = displayName;
        displayName = ubiInfo.GameTitle;
        platformMetadata["TitleSource"] = "UbisoftReadme";
    }
}
```

**3C. UPLAY PLUS executables as Ubisoft signal:**
- `_upp` is in `blacklist.json` at `tier_12_trial_demo_stub` (see Section 8 for the loading bug). If/when the tier bug is fixed, `GRB_UPP.exe` and `GRB_UPP_vulkan.exe` would be filtered as noise during `FindExecutablesDeep()`.
- **Regardless of noise filtering, `*_UPP*` executables in a folder are a SIGNAL for Ubisoft Connect detection.** The presence of Uplay Plus subscription variants indicates a Ubisoft Connect installation. This should be added as a detection signal in `StoreSignalDetector.HasUbisoftSignal()` by checking for `*_UPP*.exe` files at root.
- The distinction: `_upp` is noise when picking which exe to launch, but signal when detecting which store platform the game belongs to.

### Step 4: Reduce Candidate Count

**Goal:** Fewer `.exe` files opened = fewer PE reads = faster scan + less I/O.

**4A. ~~Early exit in `FindExecutablesDeep`~~ DEFERRED:**
- **Risk: HIGH.** Early exit when ≥3 root candidates could miss the real game exe in UE Binaries paths.
- Even the "safer alternative" (skip only if root already has a Shipping exe) doesn't help for GRB where both candidates are at root.
- Performance gain is marginal vs. risk of missing valid exes.
- **Recommendation:** DEFER to a future performance plan after profiling confirms this is a bottleneck.

**4B. Deduplicate `FileInfo.Length` calls:**
- `ScoreExecutable()` calls `new FileInfo(exePath).Length` twice (lines 199 and 235)
- Cache the value in a local variable at the start of the method
- Also change `FindPrimaryExecutable()` fallback (line 298) to avoid `OrderByDescending(f => new FileInfo(f).Length)` which creates FileInfo for every candidate

**4C. Skip PE read for obviously-noise candidates:**
- If the exe name already matches a high-severity noise pattern (Tier 1-4, penalty -30), skip the `FileVersionInfo.GetVersionInfo()` call entirely. The PE data won't rescue a -30 penalty to a winning score.
- **Note:** Tiers 5-12 are currently not loaded correctly due to JSON/C# DTO mismatch (Section 8). Until that bug is fixed, only tiers 1-4 are reliable for this optimization. Consider fixing the tier bug as a prerequisite or part of this plan.

### Step 5: Async Scanning with Progress Feedback

**Goal:** Scan library roots in the background without freezing the UI. Show per-root scanning status. Allow navigation to other libraries while one is scanning.

**Current behavior:** `RefreshCurrentRootAsync()` is synchronous — blocks the UI thread during `_libraryManager.Refresh()` and `SelectScannerAndScan()`. The user sees "Scanning..." in the status bar but cannot interact with the app.

**Target behavior:**
- Scanning runs on a background thread via `Task.Run()`
- Status bar shows "Scanning {rootName}..." while in progress
- Left pane shows "⏳ Scanning..." badge on the root being scanned (reuse `LibraryRootEntry.IsScanning`)
- User can navigate to other roots and view their games while scan is in progress
- When scan completes, the root's game list refreshes and badge updates to "✓ N games"

**Changes to `LibraryManager.cs`:**
- Add `RefreshAsync(IProgress<ScanProgress>? progress = null)` method
- `ScanProgress` record: `(string RootPath, string CurrentFolder, int FoldersCompleted, int TotalFolders)`
- Each root scan wrapped in `Task.Run()` to offload from UI thread
- Progress callback invoked per-folder during `FolderScanner.Scan()`

**Changes to `ILibraryManager.cs`:**
- Add `Task RefreshAsync(IProgress<ScanProgress>? progress = null)` to interface

**Changes to `FolderScanner.cs`:**
- `Scan()` accepts optional `IProgress<string>?` — reports each folder name as it's scanned
- Alternatively: `Scan()` returns `IAsyncEnumerable<string>` yielding folder names (more idiomatic but requires C# 8+ async streams)

**Changes to `MainWindow.axaml.cs`:**
- `RefreshCurrentRootAsync()` becomes truly async:
  ```csharp
  private async Task RefreshCurrentRootAsync()
  {
      if (_isRefreshing) return;
      _isRefreshing = true;
      try
      {
          var progress = new Progress<ScanProgress>(p => 
              SetStatusWithAutoClear($"Scanning {Path.GetFileName(p.RootPath)} — {p.CurrentFolder}..."));
          await _libraryManager.RefreshAsync(progress);
          _viewModel.Reload();
      }
      finally { _isRefreshing = false; }
  }
  ```

**Changes to `ShellViewModel.cs`:**
- `LoadGamesForRoot()` reads from database (already does this) — no change needed for background scan
- Add per-root scanning state: `Dictionary<string, bool> RootScanningStatus`
- Left pane badge shows "⏳ Scanning..." when `RootScanningStatus[root]` is true

**Changes to `MainWindow.axaml`:**
- Left pane already has status badge infrastructure (from LibrarySetupWindow pattern)
- Bind badge to scanning state from ViewModel

**Note:** This step does NOT change `FolderScanner.Scan()` logic — it adds an `IProgress<string>` parameter. The scanning itself remains synchronous internally; the async wrapper in `LibraryManager` runs it on a thread pool thread.

### Step 6: Documentation Updates

**6A. Update `docs/GAME-DETECTION-LOGIC.md`:**
- Add `uplay_download/` to Ubisoft signals table
- Add `support/Readme` metadata enrichment section for Ubisoft
- Add PE FileDescription display name enrichment section
- Document the file read investigation results (Step 1)

**6B. Update `META/TECH_DEBT.md`:**
- Add entry for full file read investigation (Step 1)

---

## 3. Files Changed

| File | Change |
|------|--------|
| `ExecutableDiscovery.cs` | Cache FileInfo.Length, skip PE read for Tier 1-4 noise, propagate FileDescription from ScoreExecutable |
| `FolderScanner.cs` | Use PE FileDescription for display name enrichment (standalone/Ubisoft/SteamEmu), add Ubisoft readme enrichment, add `IProgress<string>` parameter to `Scan()` |
| `StoreSignalDetector.cs` | Add `uplay_download/` directory signal AND `*_UPP*` exe signal to `HasUbisoftSignal()` |
| `UbisoftReadmeParser.cs` | **New** — parse `Support/Readme/` text files for publisher and game title |
| `BlacklistLoader.cs` | **Fix** — rename C# DTO properties and `[JsonPropertyName]` attributes to match current JSON tier key names; add test for `_upp`/`crash` loading |
| `LibraryManager.cs` | Add `RefreshAsync(IProgress<ScanProgress>?)` method — runs root scans on thread pool |
| `ILibraryManager.cs` | Add `Task RefreshAsync(...)` to interface |
| `ShellViewModel.cs` | Add per-root scanning state dictionary for left-pane badge display |
| `MainWindow.axaml.cs` | Make `RefreshCurrentRootAsync()` truly async with `await` |
| `MainWindow.axaml` | Bind left-pane scanning badge to ViewModel state |
| `FileSystemHelper.cs` | (possibly) Add FileDescription noise filter helper |
| `data/blacklist.json` | No change needed — JSON is correct, C# DTO needs updating |
| `docs/GAME-DETECTION-LOGIC.md` | Document new signals and enrichment |
| `tests/App.Tests/UbisoftReadmeParserTests.cs` | **New** — tests for readme parsing |
| `tests/App.Tests/StoreSignalDetectorTests.cs` | Add tests for `uplay_download/` and `*_UPP*` signals |

---

## 4. Tests

| Test | Description |
|------|-------------|
| `BlacklistLoader_AllTiersLoaded` | All 20 JSON tiers deserialize correctly; `_upp` and `crash` present in TieredExePatterns |
| `BlacklistLoader_UppTierNumberCorrect` | `_upp` has correct tier number (12) in TieredExePatterns |
| `UbisoftReadmeParser_ParsesStandardFormat` | First 4 lines: publisher, title, copyright, blank → returns title |
| `UbisoftReadmeParser_MissingFile` | No Support/Readme/ dir → returns null |
| `UbisoftReadmeParser_EmptyFile` | Empty readme → returns null |
| `UbisoftReadmeParser_CaseInsensitive` | `support/readme/` works same as `Support/Readme/` |
| `UbisoftReadmeParser_NonStandardFormat` | Readme with fewer than 4 lines → returns what's available |
| `StoreSignalDetector_DetectsUplayDownload` | Folder with `uplay_download/` → UbisoftConnect |
| `StoreSignalDetector_UplayDownloadWithManifest` | Folder with both → still UbisoftConnect (no conflict) |
| `StoreSignalDetector_DetectsUppExe` | Folder with `*_UPP*.exe` → UbisoftConnect |
| `StoreSignalDetector_UppIsNoiseButSignal` | `*_UPP*.exe` is noise for exe picking but signal for Ubisoft detection |
| `ExecutableDiscovery_ScoreExecutable_ReturnsFileDescription` | PE FileDescription returned alongside score |
| `ExecutableDiscovery_ScoreExecutable_SkipsPeReadForTier1Noise` | Tier 1-4 noise exe → no PE read (fast path) |
| `FolderScanner_AddGameEntry_UsesPeDescriptionForDisplayName` | Non-store game with PE FileDescription → display name from PE |
| `FolderScanner_AddGameEntry_PeDescriptionEmpty_KeepsFolderName` | Empty FileDescription → folder name unchanged |
| `LibraryManager_RefreshAsync_RunsInBackground` | RefreshAsync completes without blocking, progress callback invoked per root |
| `FolderScanner_Scan_ReportsProgress` | Scan with IProgress reports folder names as they're processed |

---

## 5. Success Criteria

- [ ] All 20 blacklist tiers correctly loaded (JSON/C# DTO key names match)
- [ ] Display names resolved from PE `FileDescription` for non-store-enriched games
- [ ] `uplay_download/` folder detected as Ubisoft signal
- [ ] `*_UPP*` executables detected as Ubisoft signal (distinct from noise filtering for exe picking)
- [ ] Ubisoft `support/Readme` metadata extracted for display name
- [ ] `FileInfo.Length` called once per exe (not twice)
- [ ] PE read skipped for confirmed-noise candidates (Tier 1-4)
- [ ] Scanning runs on background thread — UI remains responsive
- [ ] Left pane shows "⏳ Scanning..." badge on root being scanned
- [ ] User can navigate to other roots while scan is in progress
- [ ] Status bar shows current folder being scanned
- [ ] Build clean, all tests pass
- [ ] Documentation updated

---

## 6. Investigation Required

### Full File Read Mystery — DEFERRED

`FileVersionInfo.GetVersionInfo()` has been **ruled out** by the user testing the same .NET API in PowerShell with no full-file reads observed. The full-file reads reported by RESMON with GamingCommander as the source remain unexplained.

**Status: DEFERRED from this plan.** All known code paths have been verified. The issue may be external (runtime or file system filter driver). Instrumentation can be added as a follow-up if needed.

---

## 7. Overlap with Existing Plans

| Plan | Overlap | Notes |
|------|---------|-------|
| Plan 109 (Epic Manifest) | None | Different store |
| Plan 110 (User Tags) | None | Different feature |
| `META/SESSION/NEXT.md` item 8 | Direct match | "P1 — EA/Ubisoft Registry Fallback" — this plan partially addresses Ubisoft detection |
| `META/SESSION/NEXT.md` item 9 | Related | "P2 — EA/Ubisoft Registry Fallback" — separate concern (registry vs filesystem signals) |
| `docs/GAME-DETECTION-LOGIC.md` | Documentation update | Existing doc needs new signals added |
| TECH_DEBT "Blacklist tier flattening" | Pre-existing bug | Tier 5-12 JSON keys don't match C# DTO — fixed as Step 1 of this plan |

---

## 8. Review Findings (2026-07-26)

### CRITICAL: Blacklist JSON/C# DTO Tier Name Mismatch (Bug)

**Finding:** The plan states "`_upp` is already in `blacklist.json` Tier 13". This is **partially correct** — the pattern exists in the JSON but **is NOT being loaded by the C# code** due to a key name mismatch.

**Evidence:**
- JSON key: `"tier_12_trial_demo_stub": ["trial", "_upp"]` (blacklist.json line 49)
- C# DTO property: `[JsonPropertyName("tier_13_trial_demo_stub")] Tier13TrialDemoStub` (BlacklistLoader.cs line 123)
- Result: `_upp` (and `trial`) are **silently dropped** — never loaded into the tiered patterns list

**Scope:** Tiers 5–12 in JSON have key names that don't match ANY C# property:
| JSON Key | C# Property (stale) | Pattern Impact |
|----------|---------------------|----------------|
| `tier_5_error_crash_reporting` | `Tier5UnrealBuildDebug` (`tier_5_unreal_build_debug`) | `crash`, `error`, `crs-`, `bugsplat` NOT loaded |
| `tier_6_drm_wrappers` | `Tier6CrashReporting` (`tier_6_crash_reporting`) | `xlive` NOT loaded |
| `tier_7_installer_utilities` | `Tier7DrmWrappers` (`tier_7_drm_wrappers`) | `autorun`, `7za`, `xdelta` NOT loaded |
| `tier_8_server_loader_stub` | `Tier8InstallerUtilities` (`tier_8_installer_utilities`) | `dedicatedserver`, `stub`, `update`, `loader`, `browser`, `dowser` NOT loaded |
| `tier_9_distribution_tools` | `Tier9ServerLoaderStub` (`tier_9_server_loader_stub`) | `sdcr`, `tachyon`, `movie`, `intro` NOT loaded |
| `tier_10_dev_editor_tools` | `Tier10DistributionTools` (`tier_10_distribution_tools`) | `editor`, `modmanager`, `packagemanager` NOT loaded |
| `tier_11_utilities_debug` | `Tier11DevEditorTools` (`tier_11_dev_editor_tools`) | `install`, `debug`, `utils`, `sndrpt` NOT loaded |
| `tier_12_trial_demo_stub` | `Tier12UtilitiesDebug` (`tier_12_utilities_debug`) | `trial`, `_upp` NOT loaded |

**Impact on Plan 112:**
- Step 3C (`_UPP*` as signal) — works fine (signal detection, not scoring-dependent)
- Step 4C (skip PE read for Tier 1-5) — CANNOT work as described because tiers 5-12 don't load correctly
- The flat `ExeNamePatterns` list only contains patterns from tiers 1-4 and 13-20 (skipping 5-12 entirely)

**Resolution:** This is a pre-existing bug, not introduced by Plan 112. It should be logged as a separate P1 bug in TECH_DEBT and fixed as part of Plan 112 or separately. Fix: rename C# DTO property names to match the current JSON keys, or renumber the JSON to match C# properties.

### Verified Claims

| Claim | Status | Notes |
|-------|--------|-------|
| `FileVersionInfo.GetVersionInfo()` reads full files | ✅ Confirmed NOT the cause | Code path verified — built-in .NET API uses PE header parsing, not full reads |
| `ScoreExecutable()` calls `FileInfo.Length` twice | ✅ Verified | Lines 199 and 235 — both in try-catch, no caching |
| `FindPrimaryExecutable()` fallback creates FileInfo per candidate | ✅ Verified | Line 298: `OrderByDescending(f => new FileInfo(f).Length)` — allocates FileInfo for every candidate |
| `_upp` in blacklist.json | ⚠️ Present but not loaded | See CRITICAL finding above |
| `uplay_download/` not checked anywhere | ✅ Verified | No reference in any source file |
| Ubisoft `support/Readme` not parsed | ✅ Verified | "support" is in `NoiseSubDirNames` (skipped during exe search, but Readme parser is metadata — separate concern) |
| `Support/Readme/` case sensitivity | ℹ️ Windows-only target | Filesystem is case-insensitive on Windows; code should use `OrdinalIgnoreCase` for robustness |
| `NormalizeDisplayName` already strips "Edition" | ✅ Verified | FileSystemHelper.cs line 131 — strips "Remastered", "Definitive Edition", etc. |

### Step 4A Risk Assessment

**Plan proposes:** Early exit in `FindExecutablesDeep()` when ≥3 root candidates found.

**Risk: HIGH.** The safer alternative (skip only if root already contains a Shipping exe) is recommended. However, for the specific GRB case, both candidates are at root — early exit doesn't help. **Recommendation: DEFER Step 4A** — the performance benefit is marginal and the risk of missing valid exes is real.

### Additional Observations

1. **Step 2 guard conditions incomplete:** Plan lists when NOT to use PE FileDescription but misses:
   - FileDescription equals current displayName (no change needed)
   - FileDescription is ≤2 characters (likely noise)
   - FileDescription matches exe filename stem when folder name is already descriptive

2. **Step 4C implementation detail:** The `ScoreExecutable()` method applies the noise penalty BEFORE the PE section (line 222), and the PE section is at the end (lines 240-269). The skip check should go between the noise loop and the PE section. The current `break` on first noise match (line 223) means only one penalty is applied — the method doesn't accumulate penalties.

3. **Missing test coverage:** Plan's test list doesn't include:
   - `ScoreExecutable` returning FileDescription
   - `FindPrimaryExecutable` propagating FileDescription
   - PE skip optimization behavior
   - FileInfo.Length deduplication correctness

4. **Step 5 (progress feedback):** Correctly identified as lower-priority UI work. Should be explicitly marked DEFERRED from this plan.

---

**Last updated:** 2026-07-26
