# Task T68C: Detection Robustness & Module Organization Plan

**Tier:** 2 — Research/Documentation
**Phase:** H — MVP
**Effort:** ~45 min (documentation only, no code changes)
**Risk:** None (research only)
**Status:** Complete
**Prerequisites:** T68 (container recursion complete)
**WP:** WP-3 (3.5)

---

## Objective

Analyze the current C# detection module organization, identify where files have grown too large or mix unrelated concerns, and create a prioritized plan for closing detection robustness gaps vs the Python reference (`detect.py`). This task produces a **plan document only** — no code changes.

### Why this task exists

T65–T68 added 5 new detection files and ~600 LOC. The total detection subsystem is now 7 files / 1647 LOC. Some files mix multiple concerns (e.g., `ExecutableDiscovery` does search + scoring + launcher detection + Epic manifest). The plan identifies which splits are worth doing now (post-MVP) vs which are over-engineering.

---

## Current Module Analysis

### File Inventory (Post T65–T68)

| File | LOC | Responsibilities | Assessment |
|------|-----|------------------|------------|
| `FolderScanner.cs` | 506 | Orchestrator (3-pass), 5 fallback signal methods, container detection, noise helpers, `AddGameEntry` assembly | **One concern too many** — fallback signals and container detection are logically separate from orchestration |
| `StoreSignalDetector.cs` | 159 | 10 store signal methods (GOG, EA, Ubisoft, Epic, etc.) | **Clean** — single responsibility, well-bounded |
| `ExecutableDiscovery.cs` | 352 | Deep exe search (5 strategies), scoring (6 factors), launcher detection, Epic manifest discovery | **Two concerns** — search/discovery vs scoring/selection |
| `LnkParser.cs` | 140 | .lnk binary parsing, exe resolution with backup rename matching | **Clean** — single responsibility |
| `GogInfoParser.cs` | 163 | GOG goggame-*.info JSON parsing, DLC filtering | **Clean** — single responsibility |
| `BlacklistLoader.cs` | 212 | JSON blacklist loading, tier preservation | **Clean** — single responsibility |
| `BlacklistData.cs` | 28 | Data model for blacklist patterns | **Clean** — data record |
| `FileSystemHelper.cs` | 115 | Shared filesystem utilities, noise checks, display name normalization | **Clean** — shared utility |

### Responsibility Map

```
FolderScanner (506 LOC)
  ├─ Scan()                         ← Orchestrator (81-126)
  ├─ DetectFallbackType()           ← Tier 2 signal chain (132-155)
  │   ├─ HasSteamEmuDeepSignal()    ← Signal check (160-189)
  │   ├─ HasUbisoftLegacySignal()   ← Signal check (192-209)
  │   ├─ HasUnrealLayoutSignal()    ← Signal check (219-251)
  │   ├─ HasBinariesAtRoot()        ← UE3 fast path (257-280)
  │   ├─ HasRootExecutableSignal()  ← Signal check (283-296)
  │   └─ HasRootLnkSignal()         ← Signal check (299-307)
  ├─ ScanContainerChildren()        ← Pass 3 container logic (335-395)
  ├─ IsNonGameFolder()              ← Container helper (398-402)
  ├─ IsNoiseDirectory()             ← Delegates to FileSystemHelper (409-412)
  ├─ IsNoiseExeName()               ← Delegates to FileSystemHelper (419-422)
  ├─ GetExePatternTier()            ← Tier lookup (429-437)
  └─ AddGameEntry()                 ← Entry assembly + GOG enrichment (440-504)

ExecutableDiscovery (352 LOC)
  ├─ FindExecutablesDeep()          ← Deep search, 5 strategies (23-95)
  ├─ FindExesRecursive()            ← Recursive fallback (109-145)
  ├─ ScoreExecutable()              ← Scoring: token match, penalties, bonuses (157-216)
  ├─ FindPrimaryExecutable()        ← Search + score + pick best (228-256)
  ├─ FindLauncherExecutable()       ← Launcher detection (265-282)
  ├─ ExeNameMatchesFolderName()     ← Name matching (288-302)
  └─ FindEpicManifest()             ← Epic manifest discovery (308-332)
```

---

## Proposed Module Organization (Post-MVP)

### Principle: Split only when a file exceeds ~300 LOC or mixes unrelated concerns

The current codebase is manageable. These splits are **recommended but not urgent** — they improve clarity and testability without changing behavior.

### Option A: Minimal Splits (Recommended)

Extract two concerns from `FolderScanner`:

1. **`FallbackSignalDetector.cs`** (~180 LOC) — the 5 fallback signal methods + `DetectFallbackType()` chain
   - `HasSteamEmuDeepSignal()`
   - `HasUbisoftLegacySignal()`
   - `HasUnrealLayoutSignal()` + `HasBinariesAtRoot()`
   - `HasRootExecutableSignal()`
   - `HasRootLnkSignal()`
   - `DetectFallbackType()` (orchestrates the chain)
   - `s_uePlatformNames` (shared with ExecutableDiscovery)

2. **`ContainerScanner.cs`** (~100 LOC) — Pass 3 container detection
   - `ScanContainerChildren()` (recursive)
   - `IsNonGameFolder()`
   - `s_nonGameFolderNames`

**Result:** `FolderScanner.cs` drops from 506 → ~220 LOC (orchestrator + AddGameEntry only).

### Option B: Full Split (More granular, more files)

Everything in Option A, plus:

3. **`ExeScorer.cs`** (~80 LOC) — scoring logic extracted from ExecutableDiscovery
   - `ScoreExecutable()`
   - `ExeNameMatchesFolderName()`

4. **`EpicManifestFinder.cs`** (~40 LOC) — Epic manifest discovery
   - `FindEpicManifest()`

**Result:** `ExecutableDiscovery.cs` drops from 352 → ~230 LOC (search only).

### Recommendation

**Option A** is sufficient for now. The split is clean (signal detection vs orchestration vs container logic), each new file has a clear single responsibility, and the 506 LOC `FolderScanner` is the file that benefits most. Option B can wait until scoring or Epic logic grows.

### Files NOT worth splitting

| File | Why |
|------|-----|
| `StoreSignalDetector.cs` (159 LOC) | Already clean, 10 short methods in one file |
| `BlacklistLoader.cs` (212 LOC) | Single-purpose loader, no sub-concerns |
| `GogInfoParser.cs` (163 LOC) | Single-purpose parser |
| `LnkParser.cs` (140 LOC) | Single-purpose parser |
| `FileSystemHelper.cs` (115 LOC) | Shared utility, stays small |

---

## Robustness Gaps (C# vs Python Reference)

Prioritized by impact on real-world game detection.

### Priority 1: Medium Impact (Recommended for next iteration)

| Gap | Python Feature | C# Status | Impact | Effort |
|-----|---------------|-----------|--------|--------|
| EA `touchup.exe` / `ActivationUI.exe` signals | Root-level EA signal in `_scan_root` | ❌ Only `__Installer/` dir | EA games without `__Installer/` fall to Standalone classification | Small — add to `StoreSignalDetector.HasEaSignal` |
| Backup/copy penalties in scoring | `-30` to `-40` for `copy of`, `org_`, `original` | ❌ Not in scoring | `copy of Game.exe` could score higher than `Game.exe` in edge cases | Small — add to `ScoreExecutable` |
| Small exe penalty (< 100KB) | `-15` penalty | ❌ Not in scoring | Tiny helper exes could win over main game | Small — add to `ScoreExecutable` |

### Priority 2: Low Impact (Nice-to-have)

| Gap | Python Feature | C# Status | Impact | Effort |
|-----|---------------|-----------|--------|--------|
| `gog.ico` as GOG signal | Detected in `_scan_root` | ❌ Not in `HasGogSignal` | Negligible — all GOG games have `goggame*` files | Trivial — add one file check |
| `steamapps/` dir outside Steam library | `_has_steam_app_manifest` in deep scan | ❌ Not implemented | Pirated/cracked games mimicking Steam layout | Small — add to deep fallback |
| Abbreviation matching (+8) | Token prefix in scoring | ❌ Not implemented | "g3" won't match "Gothic3" | Medium — scoring enhancement |
| Roman numeral matching (+12) | Roman numeral conversion in scoring | ❌ Not implemented | "u9" won't match "IX" | Medium — scoring enhancement |
| Folder prefix/startswith bonus (+5) | Exe name starts with folder token | ❌ Not implemented | Minor scoring difference | Small — add to `ScoreExecutable` |

### Priority 3: Not Applicable to C# App

| Gap | Reason |
|-----|--------|
| PE metadata scoring (+15/+10) | PE extraction is a Phase 4 feature (CLI tool only) |
| Engine detection (Unity, RAGE, Frostbite) | Metadata only, not used in detection classification |
| PCGamingWiki lookup | Network call — not suitable for in-app detection |
| Extension-filtered deep walk | C# scans all files but filters noise — same result, different mechanism |

### Known Limitations (Both Systems)

| Limitation | Status | Notes |
|-----------|--------|-------|
| Multi-folder games (FFXIV pattern) | Known | `boot/` + `game/` detected as separate entries — documented in `planning/99` |
| `.bat` launcher configuration | Partial | Parsing done in Python, UI not implemented |
| Epic `.item` manifest support | Future | Epic metadata parsing not implemented |

---

## Testing Strategy

### Current Coverage

81 detection-focused tests across 7 test files. Coverage is strong for:
- Container/UE detection (13 tests)
- Deep exe search (15 tests)
- .lnk parsing (13 tests)
- GOG metadata (10 tests)
- Blacklist loading (11 tests)
- Exe scoring (10 tests)
- Noise filtering (9 tests)

### Gaps

| Area | Tests Needed | Priority |
|------|-------------|----------|
| `StoreSignalDetector` | 0 tests — all 10 signals untested | High — add signal existence tests for each platform |
| `FallbackSignalDetector` (if extracted) | Tests for each of the 5 fallback signals | High — depends on Option A split |
| Container scanner (if extracted) | Tests already exist in `FolderScannerContainerTests` — just rename test file | Low — rename only |
| Scoring edge cases | Backup penalties, abbreviation/roman if implemented | Medium — add to `ExecutableScoringTests` |
| EA `touchup.exe` signal | Test that EA games without `__Installer/` are detected | High — add to `StoreSignalDetectorTests` (new file) |

### Recommended New Tests

1. **`StoreSignalDetectorTests.cs`** (new file, ~15 tests)
   - Test each of the 10 signals: GOG, EA, EA-touchup, Ubisoft-emu, Ubisoft, Epic, Blizzard, Xbox, Rockstar, SteamEmu
   - Test priority order (GOG > EA > others when both present)
   - Test no signal → Unknown

2. **`FallbackSignalTests.cs`** (new file, ~8 tests) — if Option A is implemented
   - `steam_emu.ini` at root → SteamEmu
   - `steam_emu.ini` in child → SteamEmu
   - `UbiStats.dll` at root → UbisoftConnect
   - UE layout (Engine/ + Binaries/Win64/) → Standalone
   - UE3 layout (Binaries/Win32/ at root) → Standalone
   - Root .exe → Standalone
   - Root .lnk → Standalone
   - All noise exes → Unknown

3. **Expand `ExecutableScoringTests.cs`** (~5 new tests)
   - `copy of Game.exe` → lower score than `Game.exe`
   - Tiny exe (< 100KB) → lower score
   - Folder prefix bonus

---

## Execution Plan (Post-MVP)

| Step | Task | Effort | Depends On |
|------|------|--------|------------|
| 1 | Create `StoreSignalDetectorTests.cs` — test all 10 signals | 30 min | Nothing |
| 2 | Add EA `touchup.exe`/`ActivationUI.exe` to `HasEaSignal` | 10 min | Nothing |
| 3 | Add backup penalties + small exe penalty to `ScoreExecutable` | 20 min | Nothing |
| 4 | (If Option A) Extract `FallbackSignalDetector.cs` from `FolderScanner` | 30 min | Step 1-3 |
| 5 | (If Option A) Extract `ContainerScanner.cs` from `FolderScanner` | 20 min | Step 4 |
| 6 | Create `FallbackSignalTests.cs` | 30 min | Step 4 |
| 7 | Expand `ExecutableScoringTests.cs` with new penalty tests | 20 min | Step 3 |
| 8 | Update `docs/GAME-DETECTION-LOGIC.md` with changes | 15 min | Steps 1-7 |

**Total estimated effort:** ~3 hours for full implementation (post-MVP).

---

## Context

- **Reference:** `tools/detect.py` (1829 LOC) — Python reference implementation
- **Reference:** `planning/99-detection-hardening.md` — Partially completed, gaps documented
- **Reference:** `docs/GAME-DETECTION-LOGIC.md` — Current C# detection logic reference
- **Test count:** 154 passing (33 Core + 1 Migration + 120 App)
- **Detection results:** 157 games (120 D + 37 E), 0 unknowns, 0 with no exe

---

## Requirements

- [ ] Analyze current module organization and document assessment
- [ ] Identify which files benefit from splitting (Option A vs B)
- [ ] List all C# vs Python robustness gaps with priority
- [ ] Define testing strategy for detection subsystem
- [ ] Create execution plan with estimated effort
- [ ] No code changes — this is a planning task only

---

## Verification

- [ ] Document created in `META/TASKS/phase-h-mvp/T68C-detection-robustness-plan.md`
- [ ] `dotnet build` passes (no code changes)
- [ ] `dotnet test` passes (no code changes)
- [ ] Document is referenced from `STATUS.md` task tracker
