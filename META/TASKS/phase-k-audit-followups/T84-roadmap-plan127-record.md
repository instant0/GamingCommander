# Task T84: ROADMAP + Plan 127 Completion Record

**Tier:** 1 — Documentation
**Phase:** K — Audit Follow-ups
**Risk:** Minimal
**Status:** completed
**Prerequisites:** None

> Documentation-only. Do not run build/test.

## Objective

Plan 127 A/B shipped but `META/ROADMAP.md` has no milestone for it and `META/COMPLETED/` has no
record. Add both so the roadmap and archive match `CURRENT.md`.

## What Needs to Change

### 1. `META/ROADMAP.md`
- [ ] Add a shipped milestone for **Plan 127 A/B** (composed `GameEntry`, typed metadata, tier/provenance, schema versioning, identity reconciliation, sidecar ownership, tracking/install/user-state, launch profiles, relations, collections, typed tags).
- [ ] Move typed persistence into the "Shipped" table.
- [ ] Set the current track to: O9 title canonicalization → SteamEmu decision; SyncMove deferred.
- [ ] Note Plan 127 C (`Extensions{}`/emulator) as deferred.

### 2. `META/COMPLETED/plan-127-persistence-v2.md`
- [ ] Create a completion record: scope, A1–A6, B7–B13, C deferred, test result (607), docs updated (`docs/DATA-FORMAT.md`, `META/CODE_MAP.md`).

## Context

- `planning/127-persistence-v2.md` is the source; `META/SESSION/CURRENT.md` has the shipped list.
- Do not describe C as shipped.

## Requirements

- [ ] ROADMAP shipped/current-track text agrees with `CURRENT.md` and `NEXT.md`.
- [ ] The completion record exists and links the plan.
- [ ] No code claims beyond what shipped.

## Verification

- [ ] `grep -n "127" META/ROADMAP.md` shows the milestone.
- [ ] `test -f META/COMPLETED/plan-127-persistence-v2.md`.
- [ ] No build/test (docs-only).

## Completion Notes

- **Completed:** 2026-10-25
- **What was done:** Added the Plan 127 A/B shipped milestone and roadmap rows (typed persistence, launch-profile/relations models, collections service, typed tags), updated the phase summary and current-track text to O9 title canonicalization → SteamEmu decision with SyncMove and Plan 127 C deferred. Created `META/COMPLETED/plan-127-persistence-v2.md` covering A1–A6, B7–B13, C deferred, the 607-test result, and updated docs.
- **Verification:** `rg -n "127|O9|SteamEmu|SyncMove" META/ROADMAP.md` shows the milestone and track. `test -f META/COMPLETED/plan-127-persistence-v2.md` succeeds. No task IDs were added to ROADMAP/CODE_MAP/the completion record. No build/test run (docs-only).
- **Issues encountered:** None. Plan 127 A/B facts cross-checked against `planning/127-persistence-v2.md`; C is explicitly recorded as deferred, not shipped.
