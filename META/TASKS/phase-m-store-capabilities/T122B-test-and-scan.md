# Task T122B: Test Detection and Scan Now Actions

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Low
**Status:** pending
**Prerequisites:** Task T122B depends on Task T122A

## Objective

Add the on-demand actions so the user can verify a store's detection and import it without waiting
for the next launch.

## What Needs to Change

- [ ] `Test detection`: run the T117 locator for that store and show found/not found, path, inventory source, and safety.
- [ ] `Scan now`: run the T118 import for that store and report the number of games added/updated/missing.
      Offer it only when that store has a reader; otherwise show "no inventory source".
- [ ] Both run off the UI thread with a busy state and cancellation.
- [ ] Errors show a short message; never crash the setup screen.
- [ ] No full folder scan unless `EnrichNewGames` is on.

## Context

- Reuse the T117/T118 services directly; do not duplicate detection logic.
- The actions are read-only.

## Requirements

- [ ] A failed scan leaves existing data intact.
- [ ] `Scan now` is not offered for a store with no inventory reader.
- [ ] Results are visible to the user.
- [ ] No store-file writes.

## Verification

- [ ] View-model/service tests with a fake locator/reader.
- [ ] `dotnet build` / `dotnet test` green.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
