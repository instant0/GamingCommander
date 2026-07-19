# Task T41: Fix TECH_DEBT.md Bugs 6 & 7 Status

**Tier:** 1 — Documentation
**Phase:** F — Docs & Bug Fixes
**Effort:** ~5 min
**Risk:** Minimal
**Status:** ✅ completed

---

## Objective

TECH_DEBT.md still lists Bug 6 (Blacklist tier flattening) and Bug 7 (ScoreExecutable ignores JSON blacklist) as "Open." Both were fixed by T32 and T33 respectively. Update entries to reflect their fixed state.

## What Needs to Change

### `META/BACKLOG/TECH_DEBT.md`

**Current state:** Bug 6 and Bug 7 marked "Open" with no fix references.
**Actions:**
- [ ] Update Bug 6 status to "✅ Fixed" — reference T32 (BlacklistTierEntry record, TieredExePatterns property)
- [ ] Update Bug 7 status to "✅ Fixed" — reference T33 (ScoreExecutable accepts noise patterns + tier lookup)
- [ ] Add fix evidence (file paths and line numbers) consistent with how Bugs 1-5 are documented

## Context

- Bug 6: `BlacklistLoader` previously flattened 21 tiers into a flat list. T32 added `BlacklistTierEntry` record and `TieredExePatterns` property.
- Bug 7: `ScoreExecutable` previously only penalized ~10 hardcoded launcher patterns. T33 added `noiseExePatterns` + `tierLookup` parameters with tier-based penalties.

## Requirements

- [x] Both bugs marked as fixed with evidence
- [x] No other entries accidentally modified

## Verification

- [x] `grep "Open" META/BACKLOG/TECH_DEBT.md` — Bugs 6 & 7 no longer appear as Open

## Completion Notes

- **Completed:** 2026-07-19
- **What was done:** Updated Bug 6 and Bug 7 status from "Open" to "✅ Fixed" with fix evidence and verification dates.
- **Verification:** Both bugs now properly documented as fixed.
- **Issues encountered:** None.
