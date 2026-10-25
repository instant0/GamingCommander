---
name: tasks-execute
description: Execute one GamingCommander task. Use for "execute task T79", "do the next task", "work the top pending task", "run NEXT.md". Claims the task, evaluates it, implements, fills Completion Notes, updates the phase STATUS.md and META/SESSION/CURRENT.md, then triggers tasks-review.
metadata:
  opencode/slash: "true"
---

# tasks-execute — GamingCommander adaptation

ROLE: You are the **Builder** operating procedure from `AGENTS.md`. Execute exactly ONE task
per invocation (unless the user says continue). You produce code/tests and the task's completion
record. You do not author tasks and you do not write shared planning docs.

## Sources of truth (load only what you need)

1. `AGENTS.md`, `META/RULES.md` — **READ ONLY.**
2. `META/SESSION/CURRENT.md` → `META/SESSION/NEXT.md` → the task doc.
3. `META/CODE_MAP.md` before touching code.
4. The task's referenced `planning/<plan>.md` and, for detection work, `docs/detection/*`.
5. Python reference `tools/detect.py` (test ideas there first; C# scanners ship).

## YOU ARE THE ONLY WRITER OF

Code/tests; the task doc's `**Status:**` field and `## Completion Notes`; the task's row in its
phase `STATUS.md`; and `META/SESSION/CURRENT.md`. On completion you **remove** the row rather
than leaving a completed entry.

## YOU MUST NEVER

Write `planning/` docs, add/reorder task docs or tracker rows, edit `AGENTS.md` or
`META/RULES.md`, rewrite `META/ARCHITECTURE.md` (append-only for a decision — ask first), write
status into another task doc, cite the task ID in a non-task document, or attempt a large
refactor / architecture change without asking.

## Protocol

1. **SELECT**
   - If the user names an ID (e.g. `T79`), use it.
   - Else read `META/SESSION/NEXT.md` for the current directive, then take the top **Pending**
     (not Deferred) row in the active phase `STATUS.md`. Skip `Deferred`/`[!]`.
   - If NEXT.md and `STATUS.md` disagree about what is next, STOP and hand the decision to
     `tasks-plan`.

2. **EVALUATE before working (standing directive)**
   - Sanity-check the task against current code and docs.
   - If a referenced path is missing/renamed, the work is already done, a `Prerequisites` ID is
     unresolved, the task is stale, or the scope implies a large refactor/architecture change:
     propose the fix, hand authoring to `tasks-plan`, then STOP. If ambiguous, ask, then STOP.

3. **BASELINE**
   - Code task: run `dotnet build GamingCommander.sln` and `dotnet test GamingCommander.sln`;
     record pass/fail counts.
   - Documentation-only task: skip build/test (`META/RULES.md`).
   - Detection-matrix changes: also run `python3 tools/validate_probe_matrix.py`.

4. **CLAIM**
   - Set the task doc `**Status:** in_progress`.
   - Set its `STATUS.md` row to `In Progress`.
   - One editor per task; never steal another agent's claim. Never rewrite tracker structure.

5. **EXECUTE**
   - Follow `## What Needs to Change`; make the smallest correct change.
   - Detection: behavior-preserving, safe-first; Windows paths are opaque strings — no
     Linux-specific filesystem assumptions.
   - Persisted schemas are disposable pre-ship — no back-compat, no migration, no dual-write.
   - If reality changes scope, STOP and ask; otherwise note it in chat.

6. **VERIFY**
   - Satisfy every `## Requirements` and `## Verification` item.
   - Run a full build + suite after each code step (never skip for code).
   - After green: `Build clean, continuing with <next task>`.

7. **COMPLETE**
   - Fill `## Completion Notes`: `Completed`, `What was done`, `Verification`, `Issues encountered`.
   - Set `**Status:** completed` (or `Complete (YYYY-MM-DD)`).
   - **Remove the task's row from `STATUS.md`.** Completed tasks are retired from the tracker;
     the task doc is the record. Never renumber or reorder the remaining rows.
   - **Leave the task doc in its phase folder** — this repo moves whole phase directories to
     `META/TASKS/COMPLETED/` when the phase closes (Planner/Librarian), not per task.
   - Update `META/SESSION/CURRENT.md` only if state/blockers/verification changed: a slim state
     entry, no task ID, no per-task narrative.
   - Update `META/SESSION/NEXT.md` only if the next priority actually changed.

8. **TRIGGER** the `tasks-review` skill by exact name for the finished ID.

9. **REPORT** concisely: task done, files touched, verification, review result. Do not start the
   next task unless the user says continue.

## Invariants

- One task at a time.
- Task docs stay plan + `## Completion Notes`; no journals, logs, owner, or status bloat.
- Build/test are never skipped for code and never run for docs-only.
- Create before delete — never remove the working version until the replacement is proven.
- Tracker rows are append-only; never renumber or reorder.
- Detection changes go in behind the corpus matrix; never silently alter a known-answer fixture.
- Non-task documents never cite the task ID; describe the subject.
