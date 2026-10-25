# Task T88: Superseded/Partial Plan Status Pass

**Tier:** 1 — Documentation
**Phase:** K — Audit Follow-ups
**Risk:** Minimal
**Status:** pending
**Prerequisites:** None

> Documentation-only. Do not run build/test.

## Objective

Three plans mix obsolete keybindings, removed models, and superseded designs into live guidance.

## What Needs to Change

### 1. `planning/05-phase-3-category-browse.md`
- [ ] Add `**Status:** SUPERSEDED/STALE (2026-10-25)`.
- [ ] Replace F8/F9/S assumptions with shipped reality: F8 = filter, type-to-search (Plan 122); F9 removed.
- [ ] Remove `GameEntry.Extra` and `ILibraryManager`/`GetGamesForRoot` guidance, or archive the plan body behind the status.

### 2. `planning/111-logging-toggle-readme-metadata.md`
- [ ] Add `**Status:** PARTIAL — Task 1 open; Tasks 2–3 shipped`.
- [ ] Replace `PlatformMetadata` references with typed records.
- [ ] Remove the "227 existing tests" criterion.

### 3. `planning/04-phase-2-metadata-lookup.md`
- [ ] Add a **SUPERSEDED** banner at the top linking `119-metadata-sidecar.md` and `120-pcgw-identity-and-details.md`.
- [ ] Do **not** delete the body — `docs/README.md` forbids removing completed plans; keep the old design as historical reference.

## Context

- Findings: `planning/128-doc-and-task-audit.md` §1.2.
- Do not delete completed plans; mark them SUPERSEDED in the header.

## Requirements

- [ ] Every live guidance block matches shipped behavior.
- [ ] No removed type/keybinding remains in live sections.
- [ ] Superseded plans link their replacements.

## Verification

- [ ] `grep -n "GameEntry.Extra\|GetGamesForRoot\|PlatformMetadata\|227 existing tests" planning/05-*.md planning/111-*.md planning/04-phase-2-metadata-lookup.md` returns nothing in live guidance.
- [ ] `wc -l planning/04-phase-2-metadata-lookup.md` is unchanged (content preserved).
- [ ] `planning/04-phase-2-metadata-lookup.md` starts with a SUPERSEDED banner linking 119/120.
- [ ] No build/test (docs-only).

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
