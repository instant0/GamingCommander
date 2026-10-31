# Task T119B: Status/Suggestion UI for All Stores

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Low
**Status:** pending
**Prerequisites:** Task T119B depends on Task T119A

## Objective

Surface the health states and plain-language suggestions for every store where evidence exists in the
existing shell UI, without adding any repair/write action. Stores with no inventory show only their
disk-derived state.

## What Needs to Change

### 1. Pane status
- [ ] Map `PlatformStatus` from `Acf.Status`, `Epic.Status`, and the new `InstallInfo` health for all stores.
- [ ] Keep the existing Steam/Epic colors and text.

### 2. Details
- [ ] Show the health reason and a suggestion, using only the evidence that store actually provides. For
      example:
  - Steam/Epic inventory says installed, folder missing → "Your launcher lists this game, but the folder is
    missing. Check the launcher's library settings."
  - folder present, no record in an inventory-backed store → "Found on disk but not in the launcher's list."
  - registry/marker store, folder present → installed; do **not** claim the launcher lists it.
  - moved → "The launcher expects this at <path>; it is now at <path>." (only when a recorded path exists)
- [ ] Informational only; no automatic changes.
- [ ] Never show a launcher-derived suggestion for a store with no inventory (Xbox; the best-effort stores
      beyond what was actually read).

### 3. Tests
- [ ] View-model test for the status/suggestion mapping.
- [ ] No regression to the Steam/Epic status tests.

## Context

- `ShellViewModel.Loading` and `ShellViewModel.Details.Status` currently special-case Steam/Epic.
- Proposal: `planning/129` §3.

## Requirements

- [ ] All store states display without errors.
- [ ] Suggestions are accurate and non-destructive.
- [ ] No writes.

## Verification

- [ ] `dotnet build` / `dotnet test` green.
- [ ] UI smoke check with fixture states.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
