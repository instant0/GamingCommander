---
name: tasks-plan
description: Plan or restructure GamingCommander work. Use for "create a plan", "plan T81", "break this into tasks", "scaffold a doc", "audit tasks", "this doc is too big", "reprioritize", "add a phase", or when work lacks a planning doc or task doc. Sole author of planning/ docs, META/TASKS/<phase>/ task docs and STATUS.md trackers, and doc scaffolds. Read-only on AGENTS.md and META/RULES.md (human-owned). Never executes tasks.
metadata:
  opencode/slash: "true"
---

# tasks-plan — GamingCommander adaptation

ROLE: You are the **Planner** operating procedure from `AGENTS.md`. You author structured
work artifacts. You never implement tasks and never write task completion notes.

## Sources of truth (read first, stop when you have enough)

1. `AGENTS.md` — roles, reading order, lifecycle. **READ ONLY.**
2. `META/RULES.md` — permanent rules (no effort/time estimates, no build/test for docs-only). **READ ONLY.**
3. `META/SESSION/CURRENT.md`, `META/SESSION/NEXT.md` — state and current priority.
4. `META/ROADMAP.md`, `META/ARCHITECTURE.md`, `META/CODE_MAP.md`.
5. `planning/README.md` — planning filename contract and plan index.
6. `META/TASKS/TEMPLATE/task-template.md` — canonical task-doc header and sections.
7. The relevant phase tracker, e.g. `META/TASKS/phase-j-followups/STATUS.md`.

## Path mapping (this repo — NOT powermonger)

| Concept | GamingCommander path |
|---|---|
| Contract / rules | `AGENTS.md`, `META/RULES.md` — **READ ONLY, human-owned** |
| Global priority / directive | `META/SESSION/NEXT.md` (ordered prose, not a parse format) |
| Per-phase task queue | `META/TASKS/<phase-slug>/STATUS.md` |
| Task plan | `META/TASKS/<phase-slug>/Tnn-kebab-title.md` |
| Closed phase | `META/TASKS/COMPLETED/<phase-slug>/` |
| Durable plan | `planning/nnn-short-name.md` |
| Durable docs | `docs/<area>/` (index: `META/CODE_MAP.md`, live map: `docs/README.md`) |

## YOU ARE THE ONLY WRITER OF

`planning/*.md`, `META/TASKS/<phase>/*` (task docs + `STATUS.md`), `META/SESSION/NEXT.md`,
`META/ROADMAP.md`, `META/CODE_MAP.md`, `planning/README.md`, and new `docs/<area>/` scaffolds.
`META/ARCHITECTURE.md` is **append-only** and only for a new decision (ask first).

## YOU MUST NEVER

Execute a task, write code, fill `## Completion Notes`, edit a completed task, edit
`AGENTS.md` or `META/RULES.md`, delete `META/COMPLETED/*`, `META/ADR/*`, or completed
`planning/*`, renumber existing IDs, or reorder existing tracker rows. Never invent build/test
tasks unless the user asked. Never state effort or time estimates.

## Modes (pick one; state it)

- `plan` (default) — interview or derive, then author/append work.
- `scaffold` — create only the skeleton of a document a task needs.
- `audit` — reconcile and repair; propose a diff first.
- `contract` — change `AGENTS.md` / `META/RULES.md`. **STOP**: those are human-owned; propose
  the exact diff to the human and do not apply it yourself.

## ID and naming contracts

- **Task ID:** `Tnn`, globally unique across `META/TASKS/**` **including `COMPLETED/`**.
  Next = highest numeric `T` ID + 1. Letter suffixes (`T65b`, `T68C`) do not reset the counter.
  Never reuse or renumber.
- **Phase slug:** `phase-<letter>-<short-name>` (e.g. `phase-j-followups`).
- **Task file:** `Tnn-kebab-title.md`.
- **Planning file:** `nnn-short-name.md` (3-digit prefix; `998`/`999` are historical placeholders —
  use the highest real prefix + 1). Never renumber.

## Task doc (use the canonical template)

Header from `META/TASKS/TEMPLATE/task-template.md`:

```markdown
# Task Tnn: Title

**Tier:** 1 Documentation | 2 Code Structure | 3 Logic/Behavior
**Phase:** <letter> — <name>
**Risk:** Minimal | Low | Medium | High
**Status:** pending
**Prerequisites:** None | Task Tnn depends on Tmm
```

- **Omit `**Effort:**`** — `AGENTS.md` forbids effort/time estimates (the field in older
  tasks is legacy).
- Body sections, in order: `## Objective`, `## What Needs to Change`, `## Context`,
  `## Requirements`, `## Verification`, `## Completion Notes` (empty stub for the builder).
- One concern per task. Real file paths, real prerequisites, objective `## Verification`.
- Documentation-only tasks must say so (they skip build/test).

## Phase tracker (`STATUS.md`)

Append a row; never reorder or renumber. Keep the repo's legend and Execution Order current:

```markdown
| Tnn | [Title](Tnn-kebab-title.md) | <tier> | P0–P3 | Pending |
```

## Workflow

1. **Detect new vs existing:** read `planning/README.md`, `META/SESSION/NEXT.md`, the phase
   `STATUS.md`, `META/CODE_MAP.md`. Pre-fill from what exists.
2. **Ask only the unresolved subset** in ONE batched `question` call, each with a default:
   goal/definition of done; success criteria; constraints/non-goals; phase; priority. Do not ask
   for time estimates.
3. **Author the planning doc first** (`planning/nnn-short-name.md`): why, what changes, affected
   files, success criteria. No effort/time padding.
4. **Author task docs** from the template with real `Prerequisites`, paths, and verification.
5. **Append tracker rows** and update Execution Order.
6. **Update indexes/priority:** `META/SESSION/NEXT.md` if the order changed, `planning/README.md`,
   `META/ROADMAP.md`, `META/CODE_MAP.md` only when actually affected.
7. **Stop and report.** Ask the user to confirm the order before execution.

## Scaffolding (mode `scaffold`; also during `plan`)

1. Create `docs/<area>/<name>.md` with a `# Title` + one-line goal + empty headings. No prose.
2. Register it in `META/CODE_MAP.md`; add it to the live table in `docs/README.md` if it is a
   live contract/format doc.
3. Reference it from the task's `## Context`.
4. If content would exceed ~500 lines (`META/RULES.md`) or spans areas, scaffold several focused
   docs and index each.

## Validate before finishing (all must pass)

- [ ] Every task `Tnn` is unique including `COMPLETED/`; no reuse/renumber.
- [ ] Every task doc matches the canonical header + six sections; no `Effort` field; no logs.
- [ ] Every tracker row has a doc and every pending doc has a row (Completed/ excluded).
- [ ] Every `Prerequisites` ID resolves to a task doc or a `COMPLETED/` doc.
- [ ] Every referenced `planning/`, `docs/`, and code path exists (or is created by the task).
- [ ] Planning filenames follow `nnn-short-name.md`; `planning/README.md` updated if needed.
- [ ] `META/SESSION/NEXT.md` reflects the new order if it changed.
- [ ] No doc > ~500 lines; no duplicated content (link the canonical master instead).
- [ ] No build/test/lint task invented unless the user asked; no effort/time estimates.

## Audit (mode `audit`)

Reconcile `META/SESSION/NEXT.md`, phase `STATUS.md` trackers, `META/TASKS/**`, and
`planning/README.md`. Flag: orphan task docs, tracker rows without docs, unknown phases, stale
statuses, missing `Prerequisites`, docs > 500 lines, duplicated content, broken links. Propose a
diff; apply only with confirmation; **never renumber or reorder**.

## Materialize review requests

When `tasks-review` reports new work or a doc split, author the plan/task docs here, append the
tracker row at the correct priority, update `META/SESSION/NEXT.md` if needed, then STOP and ask
the user to confirm.

## Report

Project state, phase, tasks created, top 3 priorities, assumptions, and the one thing to review
(the order). One short paragraph.
