# Task T103C: Document the Steam-Library Rule

**Tier:** 1 — Documentation
**Phase:** L — Detection Follow-ups
**Risk:** Minimal
**Status:** completed
**Prerequisites:** Task T103C depends on Task T103B

> Documentation-only. Do not run build/test.

## Objective

State the location-authoritative Steam rule once, in the durable docs, and record the decision.

## What Needs to Change

### 1. `docs/detection/03-store-signals.md`
- [ ] Add a short "Steam library wins" subsection:
  - under `<library>/steamapps/common/` ⇒ Steam, ACF optional;
  - outside all Steam libraries + root `steam_api*.dll` ⇒ SteamEmu;
  - the Steam scanner still reports an ACF-less `common/` folder as Orphaned/fixable.

### 2. `docs/detection/09-blueprint.md`
- [ ] Update §14: mark the SteamEmu classification item decided/implemented and link the rule.
- [ ] Remove any stale "SteamEmu false positive is a design call" wording that now conflicts.

### 3. `META/ARCHITECTURE.md`
- [ ] Append the decision: location is authoritative; the DLL signal is a fallback outside libraries.

## Context

- C# and Python behavior are already changed; this segment only records it.
- Do not cite task IDs; describe the rule.

## Requirements

- [ ] One canonical statement, linked from the blueprint.
- [ ] No contradiction with the Steam scanner's Orphaned behavior.
- [ ] No task IDs in the docs.

## Verification

- [ ] `grep -n "Steam library" docs/detection/03-store-signals.md docs/detection/09-blueprint.md`.
- [ ] `grep -n "SteamEmu" docs/detection/09-blueprint.md` shows the updated status.
- [ ] No build/test (docs-only).

## Completion Notes

- **Completed:** 2026-10-25
- **What was done:** Recorded the location-authoritative Steam rule in `docs/detection/03-store-signals.md` (new "Steam library wins" subsection with the ACF-optional clarification), updated `docs/detection/09-blueprint.md` (per-store row, inconsistency entry, backlog row, and the former design-call note now state the decided rule; removed it from the "Still open" list), and appended a "Current Reality (2026-10-25, Steam location rule)" section to `META/ARCHITECTURE.md` including the fixture/matrix consequence.
- **Verification:** `grep "Steam library"` matches all three docs; `grep "SteamEmu"` in the blueprint shows the corrected/decided wording with no "design call required" or "Still open" contradiction. Docs-only; no build/test run.
- **Issues encountered:** None.
