# META/TASKS — Task Index

There is no `TASKS.md`. This directory holds the task documents and per-phase trackers; the **canonical
execution order lives in [`META/SESSION/NEXT.md`](../SESSION/NEXT.md)**.

## How work is selected

1. Read `META/SESSION/NEXT.md` — the ordered queue, and the only place that says what runs next.
2. Open the task document for that ID.
3. Read the plan it names (planning docs are background, not instructions).
4. Follow the task's stop rules; update its Completion Notes when done.

Per-phase order and status live in each phase's `STATUS.md`. If a phase tracker and `NEXT.md` disagree,
`NEXT.md` wins for "what is next"; the phase tracker wins for "what is done".

## Phases

| Directory | Status file | Scope |
|---|---|---|
| `phase-g-code-quality-and-tests/` | `STATUS.md` | test/quality follow-ups |
| `phase-i-post-mvp/` | `STATUS.md` | view modes and flat view (P2) |
| `phase-j-followups/` | `STATUS.md` | deferred Epic/SyncMove work |
| `phase-k-audit-followups/` | `STATUS.md` | audit follow-ups |
| `phase-l-detection-followups/` | `STATUS.md` | detection follow-ups |
| `phase-m-store-capabilities/` | `STATUS.md` | store discovery, import, sync, health, file/registry call discipline |
| `phase-n-security-and-release/` | `STATUS.md` | persistence and repair-write safety, release path, contract accuracy |
| `COMPLETED/` | — | finished work, kept as record |

`TEMPLATE/` holds the task skeleton. Copy it; do not invent a new format.

## Conventions

- **IDs** are global and monotonic. Next free ID: **T134**.
- A task document is one unit of work with an objective, scope, requirements, verification, and completion
  notes. It may name a plan; plans do not name task IDs.
- **Statuses:** `pending` → `ready` (spec complete) → `completed`, or `deferred` / `cancelled`.
- **Prerequisites** are listed as task IDs. Do not start a task whose prerequisite is open unless the
  detection work it touches is unaffected.
- **Stop rules** in a task are binding: they say what the task must not touch.
- **No effort estimates.** Describe what changes, why, and how it is verified.

## Reading order for an implementing session

`AGENTS.md` → `META/RULES.md` → `META/SESSION/CURRENT.md` → `META/SESSION/NEXT.md` →
`META/CODE_MAP.md` → `META/CONNECTION_MAP.md` → the task document → its plan.
