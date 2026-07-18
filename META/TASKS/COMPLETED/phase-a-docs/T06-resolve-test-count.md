# Task T06: Resolve Test Count Inconsistency

**Tier:** 1 — Documentation Cleanup
**Phase:** A — Documentation Safety Net
**Effort:** ~10 min
**Risk:** Minimal
**Status:** completed

---

## Objective

Multiple documentation files report different test counts (17 vs 18). Resolve to the actual count and update all references.

## What Needs to Change

### Verified Actual Count

```
GamingCommander.Migration.Tests:  1 test
GamingCommander.Core.Tests:       5 tests
GamingCommander.App.Tests:       11 tests
─────────────────────────────────────────
Total:                           17 tests
```

Confirmed by running `dotnet test` on 2026-07-18.

### Files to Update

1. **`META/SESSION/CURRENT.md` line 137**
   - Current: `**17 tests passing**` — Already correct. No change needed.

2. **`META/COMPLETED/phase-1-stabilization.md`**
   - Find the line referencing test count and verify it says 17 (or update if it says 18).

3. **`META/ROADMAP.md`**
   - Check for any test count references and verify they say 17.

4. **`META/CODE_MAP.md` line 218–225**
   - Current: `Core.Tests | 5`, `Migration.Tests | 1`, `App.Tests | 11` — Already correct. Verify total says 17.

## Context

- `CURRENT.md` says 17 (correct)
- `COMPLETED/phase-1-stabilization.md` reportedly says 18
- `CODE_MAP.md` says "17 tests total" (correct)
- Discrepancy likely from a test being added/removed after the stabilization doc was written

## Requirements

- [ ] Run `dotnet test` to confirm actual count
- [ ] Update any file that says 18 to say 17
- [ ] Ensure all test count references across META/ are consistent

## Verification

- [ ] `dotnet build` passes
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -r "18 test" META/ --include="*.md"` returns no hits (if it previously did)
- [ ] All test count references say 17

## Completion Notes

- **Completed:** 2026-07-18
- **What was done:** Fixed test count from 18 → 17 in 2 files:
  1. `META/COMPLETED/phase-1-stabilization.md` line 24
  2. `META/ROADMAP.md` line 57
- **Verification:** Build clean, 17 tests passing, no "18 test" references remain in live documentation
- **No issues encountered.**
