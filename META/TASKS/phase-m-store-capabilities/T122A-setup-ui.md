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
- [ ] Row content: name, detected status, three toggles, override path, action buttons.
- [ ] View-model properties per store; load from `IStorePolicyProvider`; save on change/close.
- [ ] Disable flags that require detection when the store is not found, except the manual path.
- [ ] Theme/immutable styling consistent with the existing setup window.

## Context

- Existing pattern: `LibrarySetupViewModel` Steam/Epic `CanAdd*` properties.
- No business logic in the view; call the provider/service.

## Requirements

- [ ] Toggles round-trip through persistence.
- [ ] No store is enabled by default.
- [ ] No store-file writes.

## Verification

- [ ] View-model test: toggle every store → save → reload → equal.
- [ ] `dotnet build` / `dotnet test` green.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
