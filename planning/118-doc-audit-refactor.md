# Plan 118 — Documentation Audit Refactor

**Nature:** Planning. Addresses findings from the 2026-08-07 librarian audit.
**Audience:** Builder.

---

## What Changes

Fix governance drift discovered in the full documentation audit. Four structural problems and one stale-data problem.

### Problem 1: TECH_DEBT.md says 11 bugs are Open but they're fixed

CURRENT.md and NEXT.md confirm all Bugs 23–33 were fixed in Plan 114. TECH_DEBT.md still shows `Status: Open` for every one. Agents reading TECH_DEBT will re-fix solved bugs.

### Problem 2: `PLANNING/` capitalization is wrong everywhere

Nine files reference uppercase `PLANNING/` but the actual directory is lowercase `planning/`. On Linux, `PLANNING/<file>.md` is a broken path.

### Problem 3: CURRENT.md is 639 lines of accumulated session history

The contract says this file is "scratch, overwritten every session handoff." It now contains ~400 lines of completed session logs that belong in COMPLETED/.

### Problem 4: Tech debt duplicates / dead stubs

Bug 11 and Bug 15 are the same issue (Orphaned vs Missing UI confusion), and Bug 17–19 status disagrees with `docs/EPIC-MANIFEST-ENRICHMENT.md` which says Plan 109 is unimplemented.

### Problem 5: planning/README.md is missing 25 active plans

Plans 101–117 exist on disk but aren't listed. Dead stubs (`05-phase-3.md`, `06-phase-4.md`) are still listed as future work. `90-sdk-upgrade.md` says PLANNED but is actually deferred indefinitely.

---

## Files Affected

### TECH_DEBT clean-up

| File | Change |
|------|--------|
| `META/BACKLOG/TECH_DEBT.md` | Mark Bugs 23–33 as `✅ Fixed — Plan 114` with verification date; merge Bug 11 + Bug 15; update Bug 17–19 Epic manifest status to match actual code |

### Capitalization fix

| File | Lines | Change |
|------|-------|--------|
| `AGENTS.md` | 7, 78, 99 | `PLANNING/` → `planning/` |
| `META/RULES.md` | 44 | `PLANNING/` → `planning/` |
| `META/BACKLOG/TECH_DEBT.md` | 3 | `PLANNING/` → `planning/` |

### CURRENT.md compaction

| File | Change |
|------|--------|
| `META/SESSION/CURRENT.md` | Cut to ~150 lines: keep Phase, Priority Roadmap, Known Issues, Test Status, Key Architecture Decisions. Move "Completed This Session" bulk to a new `META/COMPLETED/phase-post-mvp-sessions.md` |

### planning/README.md update

| File | Change |
|------|--------|
| `planning/README.md` | Add Plans 101–117 to Active/Completed sections; mark `05-phase-3.md` and `06-phase-4.md` as archived stubs; mark `90-sdk-upgrade.md` as deferred; add `117-left-pane-layout.md` |

### Orphan cleanup

| File | Change |
|------|--------|
| `META/SESSION/DETECTION_ANALYSIS.md` | Archive to `META/COMPLETED/detection-analysis-2026-07.md` — content is already in GAME-DETECTION-LOGIC.md |
| `docs/input.md` | Archive to `META/COMPLETED/` — bootstrap-phase doc, all "Next Steps" completed |
| `docs/development-environment.md` | Archive — Phase 0 env doc, UI stack chosen and built |
| `docs/ui-direction.md` | Archive — Phase 0 proof-of-concept, all gaps resolved |
| `docs/ui-stack-decision.md` | Archive — Phase 0 decision, Avalonia implemented |
| `docs/windows-validation-checklist.md` | Archive — all checkboxes unchecked, post-MVP now |
| `docs/F2-WIZARD-UNIFICATION-ANALYSIS.md` | Archive — verification of Plan 106, which is COMPLETE |

---

## Success Criteria

1. `grep "Status: Open" META/BACKLOG/TECH_DEBT.md` returns only genuinely unfixed bugs (8, 10, 11/15, 12, 13)
2. `grep "PLANNING/" AGENTS.md META/RULES.md META/BACKLOG/TECH_DEBT.md` returns zero matches (all lowercase `planning/`)
3. `wc -l META/SESSION/CURRENT.md` ≤ 200 lines
4. `grep -c "^- \[" planning/README.md` shows all 30 planning files accounted for
5. Zero build or code changes — documentation only
