# Task T82: Archive Closed Phase Directories

**Tier:** 2 — Repository Layout
**Phase:** K — Audit Follow-ups
**Risk:** Low
**Status:** pending
**Prerequisites:** Task T82 depends on Task T81

> Documentation/repository-only. Do not run build/test.

## Objective

Phases D, E, F, and H are closed but remain in the active `META/TASKS/` tree, so the active
queue is ambiguous. Move each whole phase directory to `META/TASKS/COMPLETED/`, matching the
existing phase-a/b/c convention.

## What Needs to Change

### 1. Move completed phase directories
- [ ] `git mv META/TASKS/phase-d-complexity-reduction META/TASKS/COMPLETED/`
- [ ] `git mv META/TASKS/phase-e-stabilization META/TASKS/COMPLETED/`
- [ ] `git mv META/TASKS/phase-f-docs-and-bugfixes META/TASKS/COMPLETED/`
- [ ] `git mv META/TASKS/phase-h-mvp META/TASKS/COMPLETED/`
- [ ] Keep `phase-g` (T52–T57 pending), `phase-i`, `phase-j`, `phase-k`, `phase-l` active.

### 2. Fix references
- [ ] Update paths inside `META/TASKS/` that point at the moved dirs (phase trackers, cross-task links).
- [ ] Do **not** edit `planning/README.md` — T86 owns that pass and runs after this task.

## Context

- Active-tree expectation from `planning/128-doc-and-task-audit.md` §1.1.
- Phase H is declared MVP-complete; only T65b is deferred. Phase D/E are all complete; Phase F has only T42 (T81 marks it complete).

## Requirements

- [ ] The four directories exist under `META/TASKS/COMPLETED/` and nowhere else.
- [ ] No relative Markdown link in `META/TASKS/` or `planning/README.md` is broken.
- [ ] Task IDs are unchanged.

## Verification

- [ ] `find META/TASKS -maxdepth 1 -type d` lists only `TEMPLATE`, `COMPLETED`, `phase-g-*`, `phase-i-*`, `phase-j-*`, `phase-k-*`, `phase-l-*`.
- [ ] `grep -rn "phase-h-mvp/\|phase-f-docs-and-bugfixes/\|phase-e-stabilization/\|phase-d-complexity-reduction/" META | grep -v "COMPLETED"` returns nothing.
- [ ] `planning/README.md` left untouched (T86 handles it).
- [ ] No build/test (docs-only).

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
