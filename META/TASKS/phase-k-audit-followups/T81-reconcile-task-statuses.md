# Task T81: Reconcile Task Statuses and Fix the Task Template

**Tier:** 1 — Documentation
**Phase:** K — Audit Follow-ups
**Risk:** Minimal
**Status:** completed
**Prerequisites:** None

> Documentation-only. Do not run build/test (`META/RULES.md`).

## Objective

Trackers and task headers disagree across phases, so an executor cannot trust `STATUS.md`.
The template also still requires `**Effort:**`, which `AGENTS.md` forbids for plans.

## What Needs to Change

### 1. Phase G — mark shipped tasks complete
- [ ] `META/TASKS/phase-g-code-quality-and-tests/T48-add-store-signal-detector-tests.md` → Complete (`StoreSignalDetectorTests.cs` exists).
- [ ] `.../T50-add-library-manager-tests.md` → Complete (`LibraryManagerTests.cs` exists).
- [ ] `.../T51-extract-path-helpers-to-shared-utility.md` → Complete, and rewrite its note: the shipped helper is `AppDataPaths` (`src/GamingCommander.App/AppDataPaths.cs`), not `FileSystemHelper`; `GetDirectoriesSafe`/`GetLastWriteTimeSafe` remain in `FileSystemHelper` for filesystem reads. Drop stale line numbers.
- [ ] Leave T52–T57 pending; confirm T57 is genuinely undone (`GamingCommander.Detection` still exists).

### 2. Phase F
- [ ] `.../T42-update-stale-phase-d-tasks.md` → Complete (its stated condition is already met; fill notes).

### 3. Phase H
- [ ] Set `**Status:** Complete (<date>)` on T61, T69, T70, T71, T75, using each doc's completion-note date. Do not invent dates.

### 4. Phase I tracker
- [ ] `META/TASKS/phase-i-post-mvp/STATUS.md`: T73 → `Pending (no doc; see T99)`; T74 → `Superseded/Complete (F4/F8 shipped)`; T72 → `Pending (stale; see T99)`.

### 5. Phase J
- [ ] `T80-syncmove-phase-2-1.md` header → `Deferred` (the tracker already says Deferred).
- [ ] `T79-detection-tier-marking-and-e6a.md`: replace the `PlatformMetadataKeys` instruction with the shipped `IdentificationTier`/`DetectionInfo`/`DetectionTierDeriver`; keep the task Completed.

### 6. Template
- [ ] `META/TASKS/TEMPLATE/task-template.md`: remove `**Effort:**` from the header and both examples; note it is legacy-only.
- [ ] Extend the tier table with `4 — Cleanup` and `5 — Removal` (T51/T57 already use them), or normalize those tasks to 1–3. State which.

## Context

- Findings: `planning/128-doc-and-task-audit.md` §1.1.
- Repo lifecycle: whole phase dirs move to `META/TASKS/COMPLETED/` when closed (T82).

## Requirements

- [ ] No task with completion notes remains `pending`.
- [ ] Every `STATUS.md` row links an existing doc or states why it has none.
- [ ] The template omits `Effort`; the tier table covers every tier used by live tasks.
- [ ] No IDs renumbered, no tracker rows reordered.

## Verification

- [ ] `grep -rn "^\*\*Status:\*\* *Pending" META/TASKS` shows only genuinely pending tasks.
- [ ] `grep -rn "Effort:" META/TASKS/TEMPLATE` returns nothing.
- [ ] `grep -rn "PlatformMetadataKeys" META/TASKS/phase-j-followups/T79-*.md` returns nothing.
- [ ] No build/test (docs-only).

## Completion Notes

- **Completed:** 2026-10-25
- **What was done:**
  - Phase G: marked T48, T50, T51 completed. T51 now records the shipped `AppDataPaths` helper (`src/GamingCommander.App/AppDataPaths.cs`) and notes that `GetDirectoriesSafe`/`GetLastWriteTimeSafe` remain in `FileSystemHelper`; stale line numbers removed. T52–T57 left pending.
  - Phase F: marked T42 completed and recorded that its six phase-D docs are already completed (2026-07-19).
  - Phase H: set `Complete (<date>)` on T61 (2026-07-25), T69/T70/T71/T75 (2026-07-26) from their completion notes.
  - Phase I tracker: T72 → `Pending (stale; see T99)`, T73 → `Pending (no doc; see T99)`, T74 → `Superseded/Complete (F4/F8 shipped)`; T74's header aligned with its row.
  - Phase J: T80 header → `Deferred`; T79's stale `PlatformMetadataKeys` instruction replaced with the shipped `IdentificationTier`/`DetectionInfo`/`DetectionTierDeriver` design and remains Complete.
  - Template: removed the `Effort` field from the header and both examples, added a legacy-only note, and extended the tier table with `4 — Cleanup` and `5 — Removal`.
- **Verification:**
  - `ls tests/GamingCommander.App.Tests/StoreSignalDetectorTests.cs` → exists (T48).
  - `ls tests/GamingCommander.App.Tests/LibraryManagerTests.cs` → exists (T50).
  - `ls src/GamingCommander.App/AppDataPaths.cs` → exists; `FileSystemHelper.cs` retains `GetDirectoriesSafe`/`GetLastWriteTimeSafe` (T51).
  - `ls -d src/GamingCommander.Detection` → exists, so T57 is genuinely undone; T52–T57 remain `pending`.
  - Phase-D docs T16/T18/T19/T20/T21/T22 → `✅ completed` with 2026-07-19 notes (T42).
  - `grep -rn "^\*\*Status:\*\* *Pending" META/TASKS` → only T72 (`Pending`), which is genuinely pending/stale.
  - `grep -rn "Effort:" META/TASKS/TEMPLATE` → no matches (exit 1).
  - `grep -rn "PlatformMetadataKeys" META/TASKS/phase-j-followups/T79-*.md` → no matches (exit 1).
- **Issues encountered:** None. No build/test run (docs-only).
