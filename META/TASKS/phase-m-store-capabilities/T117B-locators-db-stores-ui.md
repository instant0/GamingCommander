# Task T117B: Locators for Battle.net, Xbox, Rockstar + First-Run Preview

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** pending
**Prerequisites:** Task T117B depends on Task T117 and Task T120

## Objective

Add the remaining locators and the opt-in first-run preview: "Detect installed launchers?" showing
which stores were found. No inventory import yet (T118).

## What Needs to Change

### 1. Battle.net
- [ ] Probe `HKLM`/`HKCU` `Battle.net\InstallPath`.
- [ ] Detect `%ProgramData%\Battle.net\Agent\product.db`; mark it `NeedsVerification` (SQLite schema unverified).

### 2. Xbox
- [ ] Use the Windows package API (`PackageManager`/`Get-AppxPackage` equivalent) or a documented fallback.
- [ ] Report `Unsupported` on non-Windows; never enumerate `WindowsApps` as a normal folder.
- [ ] Inventory source = package registration; `ReadOnlySafety.NeedsVerification`.

### 3. Rockstar
- [ ] Probe `HKCU`/`HKLM` `Rockstar Games\Launcher\InstallFolder`.
- [ ] Detect `%LOCALAPPDATA%\Rockstar Games\Launcher\Settings.json`; mark `NeedsVerification` until its schema is confirmed.

### 4. First-run opt-in
- [ ] In `LibrarySetupViewModel`, add a "Detect launchers" action that runs `LauncherDiscoveryService`.
- [ ] Show one row per store: found/not found, install path, inventory source, safety.
- [ ] Per store, offer three opt-ins: **Index this store**, **Check on every launch**, **Enrich new games** (all off by default).
- [ ] Persist the choices through the store policy model (T120); do not start a full folder scan.
- [ ] "Index" runs the import (T118); "Check on every launch" enables the startup sync (T121).
- [ ] Do not block startup on the probe.

## Context

- UI hooks: `LibrarySetupWindow.axaml`, `LibrarySetupWindow.axaml.cs`, `LibrarySetupViewModel` (mirror the Steam/Epic `CanAdd*` pattern).
- No writes to store files.
- The preview reads launcher state only; it does not read the catalogue to decide what to probe.

## Requirements

- [ ] All three locators return Found/Not found/Unsupported without throwing.
- [ ] The preview is skippable and never blocks startup.
- [ ] The preview never enables a store by default and never creates a directory.
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
