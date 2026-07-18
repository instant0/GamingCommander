# Phase D: Complexity Reduction

## Goal

Reduce mental complexity across the C# codebase by: extracting shared utilities, eliminating duplicate code, improving naming, adding missing documentation, and **proactively splitting files before they grow**. Every change is a pure refactor — no behavior changes, no new features.

---

## Why This Comes Now

The codebase has grown organically through Phases 0–2. Several patterns have emerged that increase cognitive load:

- **13 identified duplications** across files (shared utilities copy-pasted, identical arrays defined 3 times, same JSON patterns in 3 services)
- **4 files over 400 lines** (FolderScanner at 749, MainWindow at 541, SteamLibraryScanner at 472, ShellViewModel at 362)
- **30 public members** without XML documentation
- **15+ vague variable names** (single letters, abbreviations like `swPath`, `sid`, `eid`)
- **Overlapping noise-check methods** (3 variants of exe noise checking that should be 1)

Before adding new features (metadata lookup, migration, categories), we need a clean, well-documented, low-duplication foundation.

---

## Growth-Aware Separation Principle

**Don't wait for files to hit limits — split when responsibilities are clear.**

| File | Current | After Phase D | Growth Risk | Proactive Split |
|------|---------|---------------|-------------|-----------------|
| FolderScanner.cs | 749 | ~250 | HIGH — new signals, scoring | Split into StoreSignalDetector + ExecutableDiscovery |
| SteamLibraryScanner.cs | 472 | ~250 | HIGH — more ACF fields, metadata | Split into SteamAcfParser + SteamLibraryScanner |
| MainWindow.axaml.cs | 541 | ~300 | MEDIUM — help dialog grows | Split into KeyboardDispatcher + HelpDialogBuilder |
| ShellViewModel.cs | 362 | ~200 | HIGH — categories, search coming | Split into ShellDetailsViewModel |

**Rule of thumb:** If a file has more than ONE clear responsibility, split it NOW — not when it hits 500 lines.

---

## Approach: Incremental, Safe Refactors

Every task follows the **Edit-Save-Delete** principle:
1. Create the new code first (new file, extracted method, renamed variable)
2. Update all call sites to use the new code
3. Delete the old code only after verification

Every task is independently buildable and testable. Tasks are ordered so later tasks can reference earlier ones.

---

## Task Breakdown

### Layer 1 — Shared Utilities (3 tasks)

| Task | Title | Effort | Risk | Removes Duplication |
|------|-------|--------|------|---------------------|
| T16 | Extract FileSystemHelper utility | ~25 min | Minimal | GetDirectoriesSafe, GetLastWriteTimeSafe |
| T17 | Extract JsonFileHelper utility | ~25 min | Minimal | JSON read/write pattern, JsonSerializerOptions |
| T18 | Extract shared AvailableTypes constant | ~15 min | Minimal | 3x identical string arrays |

### Layer 2 — Naming & Documentation (4 tasks)

| Task | Title | Effort | Risk | Impact |
|------|-------|--------|------|--------|
| T19 | Rename ambiguous variables across codebase | ~30 min | Low | 15+ variables renamed |
| T20 | Add XML docs to all public members (Phase D) | ~40 min | Minimal | 30 public members documented |
| T21 | Consolidate noise-check methods in FolderScanner | ~30 min | Low | 3 methods → 2 clear methods |
| T22 | Unify NormalizeDisplayName across scanners | ~20 min | Low | 2 divergent implementations → 1 |

### Layer 3 — Proactive File Splits (6 tasks)

| Task | Title | Effort | Risk | Why Split Now |
|------|-------|--------|------|---------------|
| T23 | Extract StoreSignalDetector from FolderScanner | ~40 min | Low | 10 signal methods + dispatcher = separate concern |
| T24 | Extract ExecutableDiscovery from FolderScanner | ~40 min | Low | Discovery + scoring = separate concern |
| T25 | Extract SteamAcfParser from SteamLibraryScanner | ~35 min | Low | ACF parsing grows with new fields |
| T26 | Extract KeyboardDispatcher from MainWindow | ~30 min | Low | F-key dispatch duplicated in 2 places |
| T27 | Extract HelpDialogBuilder from MainWindow | ~25 min | Low | 107 lines of pure UI construction |
| T28 | Extract ShellDetailsViewModel from ShellViewModel | ~30 min | Low | Details panel grows with categories/search |

### Layer 4 — Pattern Extraction (2 tasks)

| Task | Title | Effort | Risk | Impact |
|------|-------|--------|------|--------|
| T29 | Extract folder-picker-and-add pattern | ~25 min | Low | 3x identical folder picker logic |
| T30 | Extract JSON save/load pattern to services | ~20 min | Low | 3x identical file I/O pattern (already in T17, this integrates) |

---

## Total Estimate

- **15 tasks** across 4 layers
- **~9 hours total** (510 min)
- **Target:** 30-60 min per task for junior developer / AI agent
- **All Tier 2** (code structure) except T19 (naming) and T20 (docs) which are Tier 1

---

## Dependency Graph

```
Layer 1 (Utilities)                    Layer 2 (Naming/Docs)
  T16 ────┐                             T19 (independent)
  T17 ────┤                             T20 (independent)
  T18 ────┤                             T21 (depends on T16)
           ↓                            T22 (independent)
                                        ↓
Layer 3 (Proactive Splits)
  T23 (depends on T16, T21) ──→ FolderScanner: 749 → ~250
  T24 (depends on T16, T21) ──→ FolderScanner: 749 → ~250
  T25 (independent) ──────────→ SteamLibraryScanner: 472 → ~250
  T26 (independent) ──────────→ MainWindow: 541 → ~400
  T27 (independent) ──────────→ MainWindow: 541 → ~300
  T28 (independent) ──────────→ ShellViewModel: 362 → ~200
                                        ↓
Layer 4 (Pattern Extraction)
  T29 (depends on T18) ──────→ 3x folder picker → 1
  T30 (depends on T17) ──────→ 3x JSON I/O → 1
```

---

## File Size Projections

### Before Phase D
| File | Lines |
|------|-------|
| FolderScanner.cs | 749 |
| MainWindow.axaml.cs | 541 |
| SteamLibraryScanner.cs | 472 |
| ShellViewModel.cs | 362 |
| GamesDatabaseService.cs | 235 |
| GameSetupWindow.axaml.cs | 216 |
| App.axaml.cs | 212 |
| WizardViewModel.cs | 184 |
| BlacklistLoader.cs | 183 |
| LibraryManager.cs | 172 |

### After Phase D
| File | Lines | Change |
|------|-------|--------|
| FolderScanner.cs | ~250 | -499 |
| StoreSignalDetector.cs | ~150 | new |
| ExecutableDiscovery.cs | ~180 | new |
| SteamLibraryScanner.cs | ~250 | -222 |
| SteamAcfParser.cs | ~150 | new |
| MainWindow.axaml.cs | ~300 | -241 |
| KeyboardDispatcher.cs | ~100 | new |
| HelpDialogBuilder.cs | ~120 | new |
| ShellViewModel.cs | ~200 | -162 |
| ShellDetailsViewModel.cs | ~100 | new |
| FileSystemHelper.cs | ~60 | new |
| JsonFileHelper.cs | ~80 | new |
| LibraryRootHelper.cs | ~40 | new |
| KeyboardCommandDispatcher.cs | ~80 | new |

---

## Exit Criteria

Phase D is complete when:
- [ ] All 15 tasks pass build + test
- [ ] **No file in `src/` exceeds 300 lines** (except App.axaml.cs which is bootstrap)
- [ ] **Every file has ONE clear responsibility** (single-concern principle)
- [ ] Every public member has `/// <summary>` XML docs
- [ ] Zero exact duplicate code blocks (>5 lines) across files
- [ ] Zero single-letter or abbreviated variable names
- [ ] `grep -r "GetDirectoriesSafe" src/` shows only FileSystemHelper.cs
- [ ] `grep -rn "AvailableTypes" src/` shows exactly one definition
- [ ] All 17+ tests pass

---

## Notes

- **detect.py** (1829 lines) is excluded from Phase D — it needs separate planning (see `META/SESSION/NEXT.md` item 8)
- **Theme system** (Plan 97) is excluded — it's a feature, not a refactor
- **PCGamingWiki metadata** is excluded — it's a feature
- All task files go in `META/TASKS/phase-d-complexity-reduction/`
- **Growth-awareness:** Tasks T25, T27, T28 are proactive splits — the files aren't over limit yet, but WILL grow with upcoming features (metadata, categories, search). Split now to avoid splitting later.
