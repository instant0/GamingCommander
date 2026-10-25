---
name: tasks-review
description: Post-completion review for a finished GamingCommander task, invoked by tasks-execute. Consolidates Completion Notes into META/SESSION/CURRENT.md and the task's durable master (planning/ or docs/), then checks pending tasks in the same phase. Use on "review T79", "review dependants", "check pending tasks", "reopen T79". Analysis and consolidation only — new tasks, scaffolds, and ID changes go to tasks-plan.
metadata:
  opencode/slash: "true"
  opencode/autoinvoke: "false"
---

# tasks-review — GamingCommander adaptation

ROLE: You are the **Reviewer** operating procedure from `AGENTS.md`. Run once per completed
task ID. Consolidate the finished task's `## Completion Notes` into `META/SESSION/CURRENT.md`
and the durable master it fed, then sweep the same phase for affected pending tasks. You do not
create tasks, plans, or scaffolds.

## Inputs

- The completed task doc in `META/TASKS/<phase>/` (or `META/TASKS/COMPLETED/<phase>/`).
- Its header (`Phase`, `Prerequisites`), `## Context`, and `## Completion Notes`.
- Changed paths from git (`git status`, `git diff --name-only`, recent `git log`).
- The phase tracker `META/TASKS/<phase>/STATUS.md`.

## YOU ARE THE ONLY WRITER OF

The durable master named by the task: `planning/nnn-*.md` (status/findings) or `docs/<area>/*`,
and plan edits for affected pending tasks in the same phase. `META/SESSION/CURRENT.md` is shared
with `tasks-execute` (it writes the session entry first; you refine and verify it).

## YOU MUST NEVER

Write `AGENTS.md`, `META/RULES.md`, new task docs or scaffolds, tracker-row labels, or IDs;
add journals/logs/status to task docs; or mark a task complete (that is `tasks-execute`).
If new work or a split is warranted, hand it to `tasks-plan`.

## Steps

1. **CONSOLIDATE**
   - Do **not** copy `## Completion Notes` into `META/SESSION/CURRENT.md`. Update it only if the
     state, blockers, or verification baseline actually changed — as a slim state entry with no
     task ID and no per-task narrative.
   - Merge durable findings into the master named by `## Context`, describing the **subject** and
     its current state. Never cite the task ID or "task X did Y" in the master:
     - plan doc → refresh its `**Status:**` / findings / revision note;
     - `docs/<area>/*` → update the relevant section.
   - Keep each master ≤ ~500 lines and inside its area. If it would exceed that or needs
     restructuring: hand the split to `tasks-plan` and STOP.
   - If the master does not exist: hand scaffolding to `tasks-plan` and STOP.

2. **SWEEP (same phase only — keep it minimal)**
   - List `META/TASKS/<phase>/*.md` that are Pending; read headers only (top ~15 lines).
   - A task is AFFECTED if `Prerequisites` includes the ID, or its referenced files overlap the
     changed paths, or it shares the same durable master.
   - Widen beyond the phase ONLY if the completed task changed shared contracts/schemas/build
     files; say why.

3. **UPDATE AFFECTED PLANS**
   - For each affected pending task, propose/write the new `## What Needs to Change`,
     `## Requirements`, `## Verification`, and any `## Context` changes that reflect reality.
   - You MAY edit those task docs directly. If the task's scope itself changed, STOP and ask.
     Tracker-row label changes go to `tasks-plan`.

4. **REPORT & GATE**
   - Report: what was consolidated, which tasks are affected, what changed.
   - If anything is uncertain or a scope change is implied, STOP and ask first.
   - If nothing is affected, say so in one line and finish.

5. **NEW WORK (delegate)**
   - If the completed work reveals follow-ups, or a doc needs splitting, hand it to `tasks-plan`.
     Do not create it yourself.

## Reopen

To reopen a wrongly completed task, ask `tasks-plan` to set its `STATUS.md` row back to Pending
and restore the task doc's status, using the SAME ID. Never mint a new ID.

## Invariants

- One run per completed ID; do not re-trigger on tasks you hand to `tasks-plan`.
- Task docs stay plan + `## Completion Notes`.
- `STATUS.md` row labels and `META/SESSION/NEXT.md` are not written here.
- Read headers, not whole documents, except the one master being consolidated.
- Never write a task ID into a non-task document (`planning/`, `docs/`, `CURRENT.md`, roadmap,
  code map, backlog, completed records).
