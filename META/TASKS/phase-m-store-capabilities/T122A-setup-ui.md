# Task T122A: Setup UI Rows and Binding

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** pending
**Prerequisites:** Task T122A depends on Task T122

## Objective

Build the per-store rows, bind them to the policy provider, and save changes.

## What Needs to Change

- [ ] Add a Stores section to `LibrarySetupWindow` (or a settings tab) with one row per `LauncherKinds.All` entry.
- [ ] Row content: name, detected status, three toggles, override path (file-based inventories only), action buttons.
- [ ] View-model properties per store; load from `IStorePolicyProvider`; save on change/close.
- [ ] Disable `Index` and `Scan now` when the store has no inventory reader; disable `Check on every launch`
      when it is not indexed. Keep `Test detection` available so the user can see the reason.
- [ ] Do not offer an inventory-path override for registry/package stores (EA encrypted, Battle.net,
      Xbox, Rockstar until verified).
- [ ] Theme/immutable styling consistent with the existing setup window.

## Context

- Existing pattern: `LibrarySetupViewModel` Steam/Epic `CanAdd*` properties.
- No business logic in the view; call the provider/service.

## Requirements

- [ ] Toggles round-trip through persistence.
- [ ] No store is enabled by default.
- [ ] No store-file writes.
- [ ] `Test detection` and `Scan now` are read-only; they perform no directory creation and no repair.

## Verification

- [ ] View-model test: toggle every store → save → reload → equal.
- [ ] `dotnet build` / `dotnet test` green.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
