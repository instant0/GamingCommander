# Task T117: Launcher Discovery Service

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** pending
**Prerequisites:** Task T117 depends on Task T116

## Objective

Add a read-only service that probes known locations and reports, per launcher: found/not found,
install path, the inventory source we can read, and whether that source is safe to read. This is the
foundation for the first-run "Detect launchers?" offer.

## What Needs to Change

### 1. Models
- [ ] Reuse `GameSourceKind`; add `LauncherKinds.All` (the eight store values). Do not add a duplicate enum.
- [ ] `LauncherDiscoveryResult`: `GameSourceKind Kind`, `Found`, `InstallPath?`, `InventorySource?`, `ReadOnlySafety` (`Safe` / `NeedsVerification` / `Unsupported`), `Reason`.
- [ ] `ILauncherLocator`: `GameSourceKind Kind`, `LauncherDiscoveryResult Probe()`.

### 2. Service
- [ ] `LauncherDiscoveryService` runs every registered locator, isolates failures, and never throws.
- [ ] Reuse the existing registry abstraction; no direct `Registry` calls in the service.
- [ ] No filesystem writes; probes only.

### 3. Composition
- [ ] Register locators in the App composition root.
- [ ] No UI in this task (T117B adds the first-run preview).

## Context

- Plan: `planning/129-store-capabilities-and-launcher-discovery.md`.
- Existing precedent: `SteamInstallPathLocator`.
- Locator implementations are split into T117A (file-backed stores) and T117B (DB/package stores + UI).

## Requirements

- [ ] Every store returns `Found`, `Not found`, or `Unsupported`; no exceptions escape.
- [ ] Results are deterministic and testable with a mock registry.
- [ ] No writes.

## Verification

- [ ] `dotnet build` 0 warnings / 0 errors.
- [ ] `dotnet test` passes; new locator-service tests green.
- [ ] A fixture where one locator throws still yields results for the others.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
