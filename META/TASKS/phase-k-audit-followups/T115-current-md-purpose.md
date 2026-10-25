# Task T115: Redefine CURRENT.md and Remove Task References from Documents

**Tier:** 1 — Documentation / Process
**Phase:** K — Audit Follow-ups
**Risk:** Medium
**Status:** pending
**Prerequisites:** Task T115 depends on Task T81 and Task T86

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
- [ ] Rewrite `META/SESSION/CURRENT.md` to at most ~25 lines:
  - `# Current Project State`
  - `**Updated:** YYYY-MM-DD`
  - `## State` — 3–6 bullets on product state.
  - `## Active queue` — pointer to `META/SESSION/NEXT.md` (no task IDs).
  - `## Blockers / deferred`
  - `## Verification baseline` — build, test counts, detection matrix.
- [ ] Move any unique detail into `planning/`, `docs/`, `META/ROADMAP.md`, or `META/BACKLOG/`; delete duplicates. Archive history to `META/COMPLETED/` rather than losing it.

### 2. Remove task references from non-task documents
- [ ] Clean `META/SESSION/CURRENT.md`, `META/ROADMAP.md`, `META/CODE_MAP.md`, `META/ARCHITECTURE.md`, `META/BACKLOG/*`, and `META/COMPLETED/*` of task IDs and "task X did Y" phrasing. Replace with subject-focused statements.
- [ ] `planning/128-doc-and-task-audit.md` currently lists task IDs/backlog rows. Replace its task-index table with a subject-focused summary and move the actionable backlog to the phase `STATUS.md` trackers and task docs.
- [ ] Task IDs remain allowed only in `META/TASKS/**` and `META/SESSION/NEXT.md`.

### 3. Retire completed task rows
- [ ] In active `STATUS.md` trackers, remove rows whose task doc is completed; keep Pending / In Progress / Deferred.
- [ ] Leave completed task docs in place (phase folder or `META/TASKS/COMPLETED/`).
- [ ] Do not renumber or reorder the remaining rows.

### 4. Encode the rule
- [ ] Verify `META/RULES.md` has the "Documentation Hygiene" section (added 2026-10-25).
- [ ] Verify `.opencode/skills/tasks-execute`, `tasks-review`, and `tasks-plan` state the same rule.

## Context

- The rule is already in `META/RULES.md`; this task applies it to the existing corpus.
- Do not delete completed planning docs (`docs/README.md` says mark SUPERSEDED instead).
- Runs after the status and README passes so the trackers are final.

## Requirements

- [ ] `CURRENT.md` ≤ ~25 lines with the defined sections.
- [ ] No task IDs in non-task documents (allowed: `META/TASKS/**`, `META/SESSION/NEXT.md`).
- [ ] No completed rows in active trackers.
- [ ] `RULES.md` and the three skills state the rule.
- [ ] No unique content lost: relocated or archived.

## Verification

- [ ] `wc -l META/SESSION/CURRENT.md` ≤ 25.
- [ ] `grep -rnE "\bT[0-9]{2,3}\b" META/SESSION/CURRENT.md META/ROADMAP.md META/CODE_MAP.md META/ARCHITECTURE.md META/BACKLOG META/COMPLETED planning docs --include=*.md` returns no task refs.
- [ ] `grep -rn "Complete" META/TASKS/*/STATUS.md` shows no completed rows (Deferred is allowed).
- [ ] `grep -n "Documentation Hygiene" META/RULES.md`.
- [ ] No build/test (docs-only).

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
