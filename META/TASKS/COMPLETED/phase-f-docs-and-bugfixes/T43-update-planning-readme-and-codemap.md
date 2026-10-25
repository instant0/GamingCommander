# Task T43: Update Planning README and CODE_MAP

**Tier:** 1 — Documentation
**Phase:** F — Docs & Bug Fixes
**Effort:** ~10 min
**Risk:** Minimal
**Status:** ✅ completed

---

## Objective

`planning/README.md` lists Phase D as "ACTIVE" and Phase E as "PLANNED." `META/CODE_MAP.md` test count says "17 tests." Both are stale.

## What Needs to Change

### `planning/README.md`
- [ ] Move `10-phase-d-complexity-reduction.md` from "Active" to "Completed / Archived"
- [ ] Move `11-phase-e-stabilization.md` from "Active" to "Completed / Archived"

### `META/CODE_MAP.md`
- [ ] Update "Test Coverage" section from "17 tests total (5+1+11)" to "99 tests total (33 Core + 1 Migration + 65 App)"
- [ ] Update test file listing to include all new test files from Phase E:
  - `VdfParserTests.cs` (20 tests)
  - `BlacklistLoaderTests.cs` (11 tests)
  - `SteamLibraryScannerTests.cs` (14 tests)
  - `ExecutableScoringTests.cs` (10 tests)
  - `GameEntryIdTests.cs` (8 tests)
  - `GamesDatabaseServiceTests.cs` (16 tests)
  - `ScannerFilterTests.cs` (9 tests, was 6, +3 regression tests from T37)

## Context

- planning/README.md is the authoritative index for planning docs. Its status table is misleading to agents reading it.
- CODE_MAP.md is the most detailed codebase reference document. A factual error in test count undermines trust.

## Requirements

- [x] planning/README.md shows both Phase D and E as completed
- [x] CODE_MAP.md test count matches actual count (99 tests)
- [x] All new test files listed in CODE_MAP.md

## Verification

- [x] `grep "ACTIVE\|PLANNED" planning/README.md` — Phase D and E no longer appear

## Completion Notes

- **Completed:** 2026-07-19
- **What was done:** Moved Phase D and E from Active to Completed/Archived in planning/README.md. Updated CODE_MAP.md test count from 17 to 99 with complete breakdown.
- **Verification:** Both files now accurate.
- **Issues encountered:** None.
