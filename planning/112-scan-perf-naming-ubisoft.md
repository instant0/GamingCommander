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

**Note on GRB noise filtering:** `_upp` is already in `blacklist.json` Tier 13. So `GRB_UPP.exe` and `GRB_UPP_vulkan.exe` are already filtered as noise during `FindExecutablesDeep()`. The actual candidates scored are `GRB.exe` (536MB) and `GRB_vulkan.exe` (519MB) — 2 candidates, not 4. Both have empty FileDescription and empty InternalName in the PE data.

---

## 2. Implementation Plan

### Step 1: Investigate Full File Read Behavior

**Goal:** Identify which file-touching operation causes the I/O RESMON attributes to GamingCommander.

**Approach:**
- Add `Stopwatch`-based timing around the three file-touching operations in `ScoreExecutable()`:
  1. First `new FileInfo(exePath).Length` (line 199)
  2. Second `new FileInfo(exePath).Length` (line 235) 
  3. `FileVersionInfo.GetVersionInfo(exePath)` (line 245)
- Log: exe path, file size, time per operation
- Run on Windows against Ghost Recon Breakpoint (2 candidates at ~500MB each)
- Correlate timing data with RESMON I/O observations

**File:** `ExecutableDiscovery.cs` — add Stopwatch around lines 199, 235, and 245

**Note:** This is a diagnostic step. The timing data will determine whether further investigation is needed or if the issue is external to the application code.

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
- `_upp` is already present in `blacklist.json` Tier 13 for **exe picking** noise filtering (confirmed working — `GRB_UPP.exe` and `GRB_UPP_vulkan.exe` are already filtered as non-primary candidates).
- **However, `*_UPP*` executables in a folder are also SIGNAL for Ubisoft Connect detection.** The presence of Uplay Plus subscription variants indicates a Ubisoft Connect installation. This should be added as a detection signal in `StoreSignalDetector.HasUbisoftSignal()` by checking for `*_UPP*.exe` files at root.
- The distinction: `_upp` is noise when picking which exe to launch, but signal when detecting which store platform the game belongs to.

### Step 4: Reduce Candidate Count

**Goal:** Fewer `.exe` files opened = fewer PE reads = faster scan + less I/O.

**4A. Early exit in `FindExecutablesDeep` (CAUTION — needs testing):**
- After collecting root-level exes, if we have ≥3 candidates, skip child directory scan for UE Binaries paths.
- **Risk:** For UE games where root exes are launchers/setup and the real game is in `Binaries/Win64/`, early exit would miss the correct exe. Scoring might compensate, but this is not guaranteed.
- **Safer alternative:** Only skip if root candidates already include a Shipping/build exe (name contains "shipping" or "win64"). This indicates the real game exe is at root.
- For GRB specifically: both candidates (GRB.exe, GRB_vulkan.exe) are at root level — early exit wouldn't change the candidate count.

**4B. Deduplicate `FileInfo.Length` calls:**
- `ScoreExecutable()` calls `new FileInfo(exePath).Length` twice (lines 199 and 235)
- Cache the value in a local variable at the start of the method
- Also change `FindPrimaryExecutable()` fallback (line 298) to avoid `OrderByDescending(f => new FileInfo(f).Length)` which creates FileInfo for every candidate

**4C. Skip PE read for obviously-noise candidates:**
- If the exe name already matches a high-severity noise pattern (Tier 1-5, penalty -30), skip the `FileVersionInfo.GetVersionInfo()` call entirely. The PE data won't rescue a -30 penalty to a winning score.

### Step 5: Scan Progress Feedback

**Goal:** Show the user what's happening during scan.

**Approach:**
- `LibraryManager.Refresh()` reports progress via a callback or event
- `FolderScanner.Scan()` logs each folder as it's processed
- UI shows: "Scanning {folderName}..." in status bar
- This is UI work — lower priority than the detection fixes

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
| `ExecutableDiscovery.cs` | Cache FileInfo.Length, skip PE read for Tier 1-5 noise, propagate FileDescription from ScoreExecutable, conditional early exit in FindExecutablesDeep |
| `FolderScanner.cs` | Use PE FileDescription for display name enrichment (standalone/Ubisoft/SteamEmu), add Ubisoft readme enrichment |
| `StoreSignalDetector.cs` | Add `uplay_download/` directory signal AND `*_UPP*` exe signal to `HasUbisoftSignal()` |
| `UbisoftReadmeParser.cs` | **New** — parse `Support/Readme/` text files for publisher and game title |
| `FileSystemHelper.cs` | (possibly) Add FileDescription noise filter helper |
| `data/blacklist.json` | No change needed — `_upp` already at Tier 13 |
| `docs/GAME-DETECTION-LOGIC.md` | Document new signals and enrichment |
| `tests/App.Tests/UbisoftReadmeParserTests.cs` | **New** — tests for readme parsing |
| `tests/App.Tests/StoreSignalDetectorTests.cs` | Add tests for `uplay_download/` signal |

---

## 4. Tests

| Test | Description |
|------|-------------|
| `UbisoftReadmeParser_ParsesStandardFormat` | First 4 lines: publisher, title, copyright, blank → returns title |
| `UbisoftReadmeParser_MissingFile` | No Support/Readme/ dir → returns null |
| `UbisoftReadmeParser_EmptyFile` | Empty readme → returns null |
| `UbisoftReadmeParser_CaseInsensitive` | `support/readme/` works same as `Support/Readme/` |
| `StoreSignalDetector_DetectsUplayDownload` | Folder with `uplay_download/` → UbisoftConnect |
| `StoreSignalDetector_UplayDownloadWithManifest` | Folder with both → still UbisoftConnect (no conflict) |
| `StoreSignalDetector_DetectsUppExe` | Folder with `*_UPP*.exe` → UbisoftConnect |
| `StoreSignalDetector_UppIsNoiseButSignal` | `*_UPP*.exe` is noise for exe picking but signal for Ubisoft detection |

---

## 5. Success Criteria

- [ ] Display names resolved from PE `FileDescription` for non-store-enriched games
- [ ] `uplay_download/` folder detected as Ubisoft signal
- [ ] `*_UPP*` executables detected as Ubisoft signal (distinct from noise filtering for exe picking)
- [ ] Ubisoft `support/Readme` metadata extracted for display name
- [ ] `FileInfo.Length` called once per exe (not twice)
- [ ] PE read skipped for confirmed-noise candidates (Tier 1-5)
- [ ] Full file read behavior instrumented with timing data (root cause TBD)
- [ ] Build clean, all tests pass
- [ ] Documentation updated

---

## 6. Investigation Required

### Full File Read Mystery (Needs Windows profiling)

`FileVersionInfo.GetVersionInfo()` has been **ruled out** by the user testing the same .NET API in PowerShell with no full-file reads observed. The full-file reads reported by RESMON with GamingCommander as the source remain unexplained.

**Next step:** Add `Stopwatch`-based timing instrumentation around all file-touching operations in `ScoreExecutable()`:
1. `new FileInfo(exePath).Length` (first call, line 199)
2. `new FileInfo(exePath).Length` (second call, line 235)  
3. `FileVersionInfo.GetVersionInfo(exePath)` (line 245)

Run against Ghost Recon Breakpoint (2 candidates at ~500MB each) on Windows. Compare wall-clock time and check if RESMON I/O correlates with any specific operation. This will narrow down the cause without guessing.

---

## 7. Overlap with Existing Plans

| Plan | Overlap | Notes |
|------|---------|-------|
| Plan 109 (Epic Manifest) | None | Different store |
| Plan 110 (User Tags) | None | Different feature |
| `META/SESSION/NEXT.md` item 8 | Direct match | "P2 — EA/Ubisoft Registry Fallback" — this plan partially addresses Ubisoft detection |
| `docs/GAME-DETECTION-LOGIC.md` | Documentation update | Existing doc needs new signals added |

---

**Last updated:** 2026-07-26
