# Task T117: Launcher Discovery Service

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** completed (2026-10-31)
**Prerequisites:** Task T117 depends on Task T116

> **Test policy (2026-10-31):** new tests follow `planning/136-test-suite-reduction.md` — use
> `TestSupport/TempDirectory` and `TestGame`, one test per behavior/failure, no app-version back-compat
> tests, no new per-file temp fixture.

## Objective

Add a read-only service that probes known locations and reports, per launcher: found/not found,
install path, the inventory source we can read, and whether that source is safe to read. This is the
foundation for the first-run "Detect launchers?" offer.

## What Needs to Change

### 1. Models
- [x] Reuse `GameSourceKind`; `LauncherKinds.All` already exists (Core/Models) — do not add a duplicate enum or list.
- [x] `LauncherReadOnlySafety` (`Safe` / `NeedsVerification` / `Unsupported`) in `Core/Models`.
- [x] `LauncherDiscoveryResult`: `GameSourceKind Kind`, `Found`, `InstallPath?`, `InventorySource?`,
      `LauncherReadOnlySafety Safety`, `Reason`. Add static `NotFound` and `Unsupported` factories.
- [x] `ILauncherLocator` (`Core/Services`): `GameSourceKind Kind`, `LauncherDiscoveryResult Probe()`.

### 2. Service
- [x] `LauncherDiscoveryService` (`Core/Services`) takes `IEnumerable<ILauncherLocator>`.
- [x] `DiscoverAll()` returns one result per `LauncherKinds.All`, in that order. A kind with no registered
      locator returns `Unsupported` (“no locator in this build”) — the preview needs a row for every store.
- [x] `Discover(GameSourceKind)` returns that store’s one result, using the same rule.
- [x] A locator that throws is caught and returned as `Unsupported` with the exception type in `Reason`;
      the other stores still run.
- [x] Duplicate locators for one kind: first registered wins; deterministic.
- [x] Reuse the existing registry abstraction; no direct `Registry` calls in the service.
- [x] No filesystem writes; probes only.

**Position in the product:** launcher discovery is a convenience on top of folder/registry detection, not
a prerequisite. A store that cannot be located must not stop a folder scan from finding its games, and no
discovery result may be treated as proof that a game is installed.

### 3. Composition
- [x] No App registration in this task: there are no locators yet. `T117A`/`T117B` register their locators
      with this service when they add them.
- [x] No UI in this task (`T117B` adds the first-run preview).

## Context

- Plan: `planning/129-store-capabilities-and-launcher-discovery.md`.
- Existing precedent: `SteamInstallPathLocator`.
- Locator implementations are split into T117A (file-backed stores) and T117B (DB/package stores + UI).

## Requirements

- [x] Every store returns `Found`, `Not found`, or `Unsupported`; no exceptions escape.
- [x] Results are deterministic and testable with a mock registry.
- [x] No writes, no directory creation, and no network access.

## Verification

- [x] `dotnet build` 0 warnings / 0 errors.
- [x] `dotnet test` passes; new service tests green.
- [x] Tests: every kind returns a row in order; a missing locator returns `Unsupported`; a throwing locator
      does not stop the others; duplicate kinds are first-wins; `Discover(kind)` matches `DiscoverAll`.
- [x] No filesystem, registry, or network access in the service tests; fake locators only.

## Completion Notes

- **Completed:** 2026-10-31
- **What was done:** Added `LauncherReadOnlySafety`, `LauncherDiscoveryResult`, `ILauncherLocator`, and
  `LauncherDiscoveryService` in Core. `DiscoverAll()` returns one row per `LauncherKinds.All` in policy
  order; a missing locator is `Unsupported`, a throwing locator is isolated with the exception type in the
  reason, and duplicate kinds are first-wins. No App registration yet — the locator tasks add theirs.
- **Verification:** build 0 errors (3 pre-existing Avalonia XAML analyzer notices when XAML is recompiled);
  **505 tests** (Core 148, App 356, Migration 1), all green. Six service tests cover order, fields, missing
  locator, throwing locator, duplicate kind, and `Discover`/`DiscoverAll` parity.
- **Issues encountered:** the original composition bullet was impossible before locators existed; moved
  registration to T117A/T117B. No filesystem/registry/network access in this task.
