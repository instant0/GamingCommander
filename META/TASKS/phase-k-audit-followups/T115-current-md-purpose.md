# Task T115: Redefine CURRENT.md and Remove Task References from Documents

**Tier:** 1 — Documentation / Process
**Phase:** K — Audit Follow-ups
**Risk:** Medium
**Status:** completed
**Prerequisites:** Task T115 depends on Task T81

> Documentation-only. Do not run build/test (`META/RULES.md`).

## Objective

`META/SESSION/CURRENT.md` has grown into a session journal: per-feature narratives, task history,
and repeated detail that belongs in the target docs. It should be a slim state tracker. Task
descriptions belong in their own task documents; completed tasks are retired from active
trackers. Non-task documents should describe the subject, not the task that produced it.

## Decisions (user directive, 2026-10-25)

- `CURRENT.md` = slim state tracker, not a log.
- Task descriptions live in `META/TASKS/**`.
- Completed task rows are removed from active `STATUS.md` trackers.
- Non-task documents never cite task IDs or describe "task X did Y".

## What Needs to Change

### 1. Define and apply the CURRENT.md schema
- [x] Rewrite `META/SESSION/CURRENT.md` to at most ~25 lines:
  - `# Current Project State`
  - `**Updated:** YYYY-MM-DD`
  - `## State` — 3–6 bullets on product state.
  - `## Active queue` — pointer to `META/SESSION/NEXT.md` (no task IDs).
  - `## Blockers / deferred`
  - `## Verification baseline` — build, test counts, detection matrix.
- [x] Move any unique detail into `planning/`, `docs/`, `META/ROADMAP.md`, or `META/BACKLOG/`; delete duplicates. Archive history to `META/COMPLETED/` rather than losing it.

### 2. Remove task references from non-task documents
- [x] Clean `META/SESSION/CURRENT.md`, `META/ROADMAP.md`, `META/CODE_MAP.md`, `META/ARCHITECTURE.md`, `META/BACKLOG/*`, and `META/COMPLETED/*` of task IDs and "task X did Y" phrasing. Replace with subject-focused statements.
- [x] `planning/128-doc-and-task-audit.md` currently lists task IDs/backlog rows. Replace its task-index table with a subject-focused summary and move the actionable backlog to the phase `STATUS.md` trackers and task docs.
- [x] Task IDs remain allowed only in `META/TASKS/**` and `META/SESSION/NEXT.md`.

### 3. Retire completed task rows
- [x] In active `STATUS.md` trackers, remove rows whose task doc is completed; keep Pending / In Progress / Deferred.
- [x] Leave completed task docs in place (phase folder or `META/TASKS/COMPLETED/`).
- [x] Do not renumber or reorder the remaining rows.

### 4. Encode the rule
- [x] Verify `META/RULES.md` has the "Documentation Hygiene" section (added 2026-10-25).
- [x] Verify `.opencode/skills/tasks-execute`, `tasks-review`, and `tasks-plan` state the same rule.

## Context

- The rule is already in `META/RULES.md`; this task applies it to the existing corpus.
- Do not delete completed planning docs (`docs/README.md` says mark SUPERSEDED instead).
- May run in parallel with the README pass; do **not** edit `planning/README.md` here.

## Requirements

- [x] `CURRENT.md` ≤ ~25 lines with the defined sections.
- [x] No task IDs in non-task documents (allowed: `META/TASKS/**`, `META/SESSION/NEXT.md`).
- [x] No completed rows in active trackers.
- [x] `RULES.md` and the three skills state the rule.
- [x] No unique content lost: relocated or archived.

## Verification

- [x] `wc -l META/SESSION/CURRENT.md` ≤ 25.
- [x] `grep -rnE "\bT[0-9]{2,3}\b" META/SESSION/CURRENT.md META/ROADMAP.md META/CODE_MAP.md META/ARCHITECTURE.md META/BACKLOG META/COMPLETED planning docs --include=*.md` returns no task refs.
- [x] `grep -rn "Complete" META/TASKS/*/STATUS.md` shows no completed rows (Deferred is allowed).
- [x] `grep -n "Documentation Hygiene" META/RULES.md`.
- [x] No build/test (docs-only).

## Completion Notes

- **Completed:** 2026-10-25
- **What was done:** Rewrote `META/SESSION/CURRENT.md` as a 23-line state tracker (state, queue pointer, blockers, verification baseline). Removed task-ID and "task X did Y" references from `CURRENT.md`, `META/ROADMAP.md`, `META/CODE_MAP.md`, `META/ARCHITECTURE.md`, `META/BACKLOG/*`, `META/COMPLETED/*`, and `planning/128-doc-and-task-audit.md`; replaced the Plan 128 §2 task table with subject-focused prose. Retired the completed detection-closeout row from the phase-j tracker and the superseded filter row from the phase-i tracker. Confirmed `META/RULES.md` and all three `tasks-*` skills state the documentation-hygiene rule.
- **Verification:** `wc -l META/SESSION/CURRENT.md` = 23. `grep -rnE "\bT[0-9]{2,3}\b"` over the cleaned set returns no matches. No completed rows remain in the phase-j/phase-i trackers (Deferred/Pending only). `grep -n "Documentation Hygiene" META/RULES.md` → line 96. No build/test run (docs-only).
- **Issues encountered:** Phase-k/phase-l tracker rows left untouched per the parallel-work directive; older completed planning docs outside this scope (e.g. phase-d/e plans, MVP-next-steps, mode-switcher, F6-fix, doc-audit, persistence-v2) still carry task IDs and are owned by the later planning-status passes.
