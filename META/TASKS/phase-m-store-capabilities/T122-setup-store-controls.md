# Task T122: Setup Screen Store Controls

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** pending
**Prerequisites:** Task T122 depends on Task T117 and Task T120

## Objective

Add a Stores section to the Setup screen where the user can enable/disable each store's indexing,
launch check, and enrichment, and test/scan it on demand.

## What Needs to Change

- [ ] A "Stores" section (inside `LibrarySetupWindow` or a dedicated settings view) with one row per store:
  - detected status (from T117),
  - `Index this store`,
  - `Check on every launch`,
  - `Enrich new games`,
  - optional inventory path override,
  - `Test detection`,
  - `Scan now`.
- [ ] All toggles default off; saving persists via `IStorePolicyProvider` (T120).
- [ ] Stores not detected show "Not found" and still allow a manual inventory path.
- [ ] A manual inventory path is validated as an existing directory before it is saved; relative and UNC paths are refused.
- [ ] No full folder scan from the Setup screen unless `EnrichNewGames` is on.

## Context

- Existing setup: `LibrarySetupWindow`, `LibrarySetupViewModel`, `MainWindow.LibrarySetup`.
- First-run opt-in (T117B) and this screen use the same policy model.
- No writes to store files.
- Repair and store-file writes are out of scope for this screen. If a repair action is surfaced later, it must use the guarded repair contract; do not add a direct writer here.

## Requirements

- [ ] Every store can be enabled/disabled independently.
- [ ] Settings survive restart.
- [ ] Test/scan actions never block the UI or write store files.
- [ ] No store is enabled by default.
- [ ] The screen exposes no repair or store-write action.

## Verification

- [ ] View-model tests: toggle → save → reload.
- [ ] `dotnet build` / `dotnet test` green.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
