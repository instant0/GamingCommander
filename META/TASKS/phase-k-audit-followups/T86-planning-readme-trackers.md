# Task T86: planning/README + Tracker Table

**Tier:** 1 — Documentation
**Phase:** K — Audit Follow-ups
**Risk:** Minimal
**Status:** pending
**Prerequisites:** Task T86 depends on Task T81 and Task T82

> Documentation-only. Do not run build/test.

## Objective

`planning/README.md` has stale statuses and omits the phase-j/k/l trackers, so it misdirects the
next session.

## What Needs to Change

### 1. Status rows
- [ ] `123` → substantially complete; only doc refresh/human sign-off remain.
- [ ] `103` → reference/superseded by `docs/detection/07`.
- [ ] `124` → COMPLETE (2026-09-08), link the completion record.
- [ ] `111` → PARTIAL (Task 1 open; Tasks 2–3 shipped).
- [ ] `110` → PARTIAL (Phases 1–5 shipped; future phases remain).
- [ ] `126` follow-ups → deferred (T78).
- [ ] `05-phase-3-category-browse` → SUPERSEDED/STALE.
- [ ] `04-phase-2-metadata-lookup` → SUPERSEDED by 119/120.

### 2. Trackers and contract
- [ ] Add `phase-j`, `phase-k`, `phase-l` to the task-tracker table.
- [ ] Make the naming contract consistently `nnn-short-name.md`; note that legacy 0–99 phase slots are two digits.

## Context

- Run after T81/T82 so statuses and paths are final.
- Do not reorder `planning/README.md` sections; only correct rows.

## Requirements

- [ ] Every status row matches its plan header / `CURRENT.md`.
- [ ] Every listed tracker path exists.
- [ ] No `nn-` / `nnn-` contradiction.

## Verification

- [ ] Open each linked path; confirm it exists.
- [ ] `grep -n "phase-j-followups\|phase-k-audit\|phase-l-detection" planning/README.md` shows all three.
- [ ] No build/test (docs-only).

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
