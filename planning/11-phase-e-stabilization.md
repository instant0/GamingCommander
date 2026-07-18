# Phase E: Stabilization & Test Coverage

## Goal

Fix all known bugs, close stale tech debt entries, and fill critical test gaps. This phase ensures the codebase is reliable and regressions are caught automatically.

---

## Why This Comes After Phase D

Phase D reduces complexity and eliminates duplication, making bug fixes safer and test writing easier. Fixing bugs on top of duplicated code means fixing the same bug in multiple places.

---

## Known Bugs to Fix

| Bug | Severity | Where | Description |
|-----|----------|-------|-------------|
| Bug 6 | MEDIUM | BlacklistLoader.cs | Tier information discarded after loading — 21 tiers flattened to flat list |
| Bug 7 | MEDIUM | FolderScanner.cs | ScoreExecutable only penalizes ~10 hardcoded launcher patterns, ignores JSON blacklist |

### Bug 6: Blacklist Tier Flattening

**Current:** `BlacklistLoader.Load()` flattens all 21 tiers of `exe_name_patterns` into a single `IReadOnlyList<string>`.

**Impact:** Cannot apply per-tier severity. A "tier_1 universal noise" exe (like `unins000.exe`) gets the same treatment as a "tier_17 store bootstrap" exe.

**Fix approach:**
- Change `BlacklistData.ExeNamePatterns` from `IReadOnlyList<string>` to `IReadOnlyList<BlacklistTierEntry>` where `BlacklistTierEntry` is a record of `(string Pattern, int Tier)`
- Or: Keep the flat list but add a separate `TierMap` dictionary mapping pattern → tier
- Tier 1 = highest severity (universal noise), Tier 21 = lowest (store bootstraps)

### Bug 7: ScoreExecutable Ignores JSON Blacklist

**Current:** `ScoreExecutable()` only penalizes 10 hardcoded launcher patterns. JSON blacklist patterns like "patch", "activate", "trial", "config" get no scoring penalty.

**Fix approach:**
- Pass `_noiseExePatterns` to `ScoreExecutable()`
- Add penalty for any pattern in the noise list
- Scale penalty by tier if Bug 6 is fixed first

---

## Test Coverage Gaps

| Component | Tests | Priority | Notes |
|-----------|-------|----------|-------|
| SteamLibraryScanner | 0 | HIGH | ACF parsing, cross-library detection, Missing/Orphaned status |
| VdfParser | 0 | HIGH | Malformed input, nested blocks, escape sequences |
| BlacklistLoader | 0 | MEDIUM | Loading, parsing, error handling |
| IsNoiseExePattern vs IsNonGameExe | 0 | HIGH | Needs test proving Bug 5 divergence (now fixed but needs regression test) |
| ScoreExecutable | 0 | MEDIUM | Scoring logic correctness |
| GameEntryId | 0 | MEDIUM | Deterministic ID generation |
| LibraryManager | 0 | LOW | Route delegation, CRUD operations |
| GamesDatabaseService | 0 | LOW | JSON persistence, caching, DTO mapping |

---

## Task Breakdown

### Layer 1 — Close Stale Tech Debt (1 task)

| Task | Title | Effort | Risk |
|------|-------|--------|------|
| T31 | Close stale TECH_DEBT entries 1-4 and 5 | ~15 min | Minimal |

### Layer 2 — Bug Fixes (2 tasks)

| Task | Title | Effort | Risk |
|------|-------|--------|------|
| T32 | Fix BlacklistLoader tier preservation (Bug 6) | ~40 min | Medium |
| T33 | Fix ScoreExecutable to use JSON blacklist (Bug 7) | ~30 min | Medium |

### Layer 3 — Critical Test Coverage (4 tasks)

| Task | Title | Effort | Risk |
|------|-------|--------|------|
| T34 | Add VdfParser unit tests | ~40 min | Minimal |
| T35 | Add BlacklistLoader unit tests | ~30 min | Minimal |
| T36 | Add SteamLibraryScanner unit tests | ~50 min | Low |
| T37 | Add noise-check regression test | ~20 min | Minimal |

### Layer 4 — Secondary Test Coverage (3 tasks)

| Task | Title | Effort | Risk |
|------|-------|--------|------|
| T38 | Add ScoreExecutable unit tests | ~30 min | Minimal |
| T39 | Add GameEntryId unit tests | ~20 min | Minimal |
| T40 | Add GamesDatabaseService unit tests | ~40 min | Low |

---

## Total Estimate

- **10 tasks** across 4 layers
- **~6.5 hours total** (345 min)
- **Target:** 30-60 min per task
- **Tier 1:** T31 (docs)
- **Tier 3:** T32, T33 (bug fixes), T34–T40 (tests)

---

## Dependency Graph

```
Layer 1 (Tech Debt)
  T31 ─────────────────────────────┐
                                   ↓
Layer 2 (Bug Fixes)                │
  T32 (independent)                │
  T33 (depends on T32)             │
                                   ↓
Layer 3 (Critical Tests)           │
  T34 (independent)                │
  T35 (depends on T32)             │
  T36 (independent)                │
  T37 (depends on T33)             │
                                   ↓
Layer 4 (Secondary Tests)          │
  T38 (depends on T33)             │
  T39 (independent)                │
  T40 (depends on T32)             │
```

---

## Exit Criteria

Phase E is complete when:
- [ ] All 10 tasks pass build + test
- [ ] Bugs 6 and 7 are fixed and verified with tests
- [ ] TECH_DEBT.md entries 1-5 are closed
- [ ] All identified test gaps are filled
- [ ] Test count increases from 17 to ~35+
- [ ] No regressions in existing 17 tests

---

## Notes

- Bug 5 (static vs instance noise check divergence) was already fixed — T37 adds a regression test
- Bug 6 fix changes `BlacklistData` record — will affect `BlacklistLoader.cs` and `FolderScanner.cs` constructor
- Bug 7 fix is blocked by Bug 6 (needs tier info for scaled penalties) but can be done with flat list first
- All task files go in `META/TASKS/phase-e-stabilization/`
