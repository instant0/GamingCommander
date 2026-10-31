# Task T117B2: First-Run Launcher Preview and Policy Wiring

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium (UI + composition)
**Status:** completed (2026-10-31)
**Prerequisites:** Task T117 (service, done), Task T120 (policy model/provider, done)

> **Test policy:** follow `planning/136-test-suite-reduction.md` — one test per behavior, no back-compat
> tests.
>
> Mode selection and its scan gating are Task T137. This task builds the mode-agnostic preview/policy
> component that T137 embeds; do not add the launcher discovery setting here.

## Objective

Show the read-only launcher discovery results in the setup window and persist per-store opt-ins through the
existing policy provider. This is independent of the remaining locators: a missing locator already yields an
`Unsupported` row, so the screen works before every store is probed.

## What Needs to Change

### 1. Composition
- [x] Construct `StorePolicyProvider` in `App.axaml.cs` and pass it to the setup window/view-model. It is
      currently constructed nowhere.
- [x] Construct `LauncherDiscoveryService` with the registered locators (empty is valid; rows are
      `Unsupported`).
- [x] Do not add a second policy provider or a parallel settings path.

### 2. View-model
- [x] Add a “Detect launchers” action that calls `DiscoverAll()`; do not run it automatically on open.
- [x] Expose one row per `LauncherKinds.All`: store label, found/not found/unsupported, install path,
      inventory source, safety, reason.
- [x] Three toggles per row: **Index this store**, **Check on every launch**, **Enrich new games**; all
      default off.
- [x] `Index` is enabled only when the row has a readable inventory source **and** an inventory reader
      exists. Until T118 lands, no reader exists, so the control is disabled with “import not available yet”.
- [x] EA, Battle.net, and Arc have no readable inventory (T136). Their rows show `Unsupported` without a
      probe; do not add a locator or reader for them in this task.
- [x] `Check on every launch` is enabled only when `Index` is on; `Enrich` follows the existing policy rule.
- [x] Saving persists via `IStorePolicyProvider`; no direct `settings.json` write.
- [x] A failing/absent locator shows the reason; it does not crash the dialog.

### 3. UI
- [x] Add the section/button to `LibrarySetupWindow.axaml` using the existing setup styling.
- [x] Do not start a folder scan; the preview only probes launchers.
- [x] Do not block the window while probing (busy state; no startup block).

### 4. Tests
- [x] View-model: `DiscoverAll` rows map to labels/status; missing locator shows unsupported.
- [x] Toggle → save → reload through `IStorePolicyProvider`; defaults are all off.
- [x] Index disabled without a reader; enabled with a fake reader.
- [x] No store enabled by default; no directory creation; no network.

## Verification

- [x] `dotnet build` 0 errors; `dotnet test` green.
- [x] `grep` shows `StorePolicyProvider` constructed in `App.axaml.cs` and used by the setup view-model.
- [x] UI smoke: first run opens setup; “Detect launchers” shows one row per store; toggles survive restart.

## Stop rules

- Do not add an inventory reader here (T118).
- Do not enable a store by default or infer “detected means enabled”.
- Do not write store files, registry, or a second settings file.
- Do not make the preview a startup blocker.

## Completion Notes

- **Completed:** 2026-10-31
- **What was done:** Added `LauncherPolicyViewModel`/`LauncherPolicyRow` (mode-agnostic preview + policy
  rows). Constructing them performs no I/O; `DetectLaunchersAsync` probes once via
  `LauncherDiscoveryService`. Each row shows status/source/reason, loads the current `StorePolicy`, and
  toggles Index / Check on launch / Enrich through `IStorePolicyProvider`. `Index` is disabled when no
  inventory reader is injected, which is the current state until T118; a reader delegate is already accepted
  so T118 can enable it. Wired a single `StorePolicyProvider` and the discovery service through `App` into
  both the first-run and F2 setup windows. Added the launcher section to `LibrarySetupWindow` with a Detect
  button and bound toggles.
- **Verification:** build 0 errors / 0 warnings; **528 tests** (Core 148, App 379, Migration 1). New tests
  cover no probe on construction, one probe per Detect, rows for all launchers, index disabled without a
  reader, enabled/persisted with a fake reader, dependent toggles reset, unsupported rows refused. No
  directory/network access.
- **Issues encountered:** all Index controls are intentionally disabled until T118 provides an inventory
  reader. The preview is still useful (found/not found/unsupported), but the policy toggles cannot be enabled
  yet. The setup-mode choice is T137 and is not in this task.
