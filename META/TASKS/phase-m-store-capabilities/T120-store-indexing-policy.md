# Task T120: Store Indexing Policy and Settings

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** completed
**Prerequisites:** Task T120 depends on Task T116

## Objective

Persist a per-store opt-in policy and expose it to discovery, import, startup sync, and the Setup
screen. Nothing is indexed unless the user explicitly enables it.

## What Needs to Change

### 1. Model (Core)
- [ ] `StorePolicy`: `GameSourceKind Kind`, `bool IndexEnabled`, `bool CheckOnLaunch`, `bool EnrichNewGames`, `bool SkipUnchangedManifests` (default true), `string? InventoryPathOverride`.
- [ ] `StorePolicySettings`: `IReadOnlyList<StorePolicy> Stores`; missing stores default to all-false.
- [ ] Use the existing `GameSourceKind`; do **not** add a duplicate launcher enum. Add `LauncherKinds.All` (Steam, Epic, Gog, EaApp, UbisoftConnect, BattleNet, Xbox, Rockstar) and reject/ignore `Standalone`, `Unknown`, `SteamEmu` as policy kinds.
- [ ] DTO stores `Kind` as its enum **name** (string) for readable/stable JSON; parse with `Enum.TryParse`, ignore unknown names.
- [ ] `CheckOnLaunch` is only effective when `IndexEnabled` is true; validate on load/save.

### 2. Persistence
- [ ] Add `StorePolicies` to `AppConfig` / `settings.json` (schema version; disposable pre-ship, no migration).
- [ ] Missing/partial settings load as defaults; unknown store kinds are ignored, not fatal.
- [ ] No writes to store files.

### 3. Provider
- [ ] `IStorePolicyProvider`: `Get(kind)`, `Set(policy)`, `EnabledStores`, `CheckOnLaunchStores`.
- [ ] Used by discovery (which stores to probe), import (which to index), startup sync (which to check), and Setup UI.

## Context

- Proposal: `planning/129` §3–§4.
- Config is per-user; store policy is not part of `libraries.json`.
- All features default **off**.

## Requirements

- [ ] `IndexEnabled`, `CheckOnLaunch`, and `EnrichNewGames` default false; `SkipUnchangedManifests` defaults true.
- [ ] Only the 8 launcher kinds are accepted; others are ignored.
- [ ] `CheckOnLaunch` and `EnrichNewGames` are forced false when `IndexEnabled` is false.
- [ ] `CheckOnLaunch` without `IndexEnabled` is treated as disabled.
- [ ] `SkipUnchangedManifests` defaults true.
- [ ] Policy survives restart and is validated on load.
- [ ] Unknown/removed store kinds do not break startup.
- [ ] No store-file writes.

## Verification

- [ ] Round-trip test: set policy → save → reload → equal.
- [ ] Missing/partial/unknown config loads with defaults.
- [ ] `dotnet build` / `dotnet test` green.

## Completion Notes

- **Completed:** 2026-10-25
- **What was done:** Implemented per-store opt-in policy end to end: launcher-kind helper,
  policy records, `settings.json` persistence with string enum names and safe validation, and a
  single thread-safe policy provider with import/sync gating. All stores remain disabled by
  default; no store files are written. The persisted shape is documented in
  `docs/DATA-FORMAT.md`.
- **Verification:** `dotnet build` succeeded with 0 errors (3 pre-existing Avalonia AVLN3001
  warnings); `dotnet test` passed 686 tests (App 485, Core 200, Migration 1); probe matrix
  validator all scenarios pass.
- **Issues encountered:** None.
