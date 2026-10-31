# Task T117B: Locators for Battle.net and Rockstar + First-Run Preview (Xbox unsupported)

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** pending
**Prerequisites:** Task T117B depends on Task T117 and Task T120

## Objective

Add the remaining locators, wire the policy provider, and add the opt-in first-run preview: "Detect
installed launchers?" showing which stores were found. Xbox has no locator in this version; it reports
`Unsupported`. No inventory import yet (T118).

## What Needs to Change

### 1. Battle.net
- [ ] Probe the documented Agent install key; verify the exact path on a real install before coding it.
- [ ] Do **not** read `%ProgramData%\Battle.net\Agent\product.db` in this task. It is an unverified SQLite
      store; report the inventory source as `Unsupported` and rely on game-root markers for detection.

### 2. Xbox
- [ ] No package API in this version. Do not add a WinRT/Windows SDK dependency and do not enumerate
      `WindowsApps` as a folder.
- [ ] Report `Unsupported` for inventory; detection remains marker-based (`default-metadata.json`) plus the
      normal folder scan.

### 3. Rockstar
- [ ] Probe `HKCU`/`HKLM` `Rockstar Games\Launcher\InstallFolder`; verify the key on a real install first.
- [ ] Detect `%LOCALAPPDATA%\Rockstar Games\Launcher\Settings.json`; mark it `NeedsVerification` until its
      schema is confirmed. Do not read it as inventory in this task.

### 4. First-run opt-in
- [ ] In `LibrarySetupViewModel`, add a "Detect launchers" action that runs `LauncherDiscoveryService`.
- [ ] Show one row per store: found/not found, install path, inventory source, safety.
- [ ] Per store, offer three opt-ins: **Index this store**, **Check on every launch**, **Enrich new games**
      (all off by default). Offer **Index** only for stores whose inventory source is readable.
- [ ] Persist the choices through the store policy model (T120); do not start a full folder scan.
- [ ] **Wire `StorePolicyProvider` into the composition root here.** It exists but is constructed nowhere
      today; without this, the toggles persist and do nothing.
- [ ] "Index" runs the import (T118); "Check on every launch" enables the startup sync (T121).
- [ ] Do not block startup on the probe.

## Context

- UI hooks: `LibrarySetupWindow.axaml`, `LibrarySetupWindow.axaml.cs`, `LibrarySetupViewModel` (mirror the Steam/Epic `CanAdd*` pattern).
- No writes to store files.
- The preview reads launcher state only; it does not read the catalogue to decide what to probe.

## Requirements

- [ ] All locators return Found/Not found/Unsupported without throwing.
- [ ] The preview is skippable and never blocks startup.
- [ ] The preview never enables a store by default and never creates a directory.
- [ ] No store is offered "Index" when its inventory source is `Unsupported`.
- [ ] `StorePolicyProvider` is constructed and used; stubbing the provider is a failure of this task.
- [ ] No write operations and no network access.

## Verification

- [ ] Locator tests for each store.
- [ ] UI smoke test or view-model test for the preview list.
- [ ] `dotnet build` / `dotnet test` green.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
