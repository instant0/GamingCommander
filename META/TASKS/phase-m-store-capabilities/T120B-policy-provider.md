# Task T120B: Policy Provider and Integration

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** completed
**Prerequisites:** Task T120B depends on Task T120A

## Objective

Provide a single policy provider and wire it into discovery, import, startup sync, and the Setup UI
so every component respects the same per-store opt-in.

## What Needs to Change

- [ ] `IStorePolicyProvider` + implementation over `StorePolicySettings`.
- [ ] Provider rejects `Standalone`, `Unknown`, `SteamEmu` as policy kinds.
- [ ] Discovery (T117): probe only `LauncherKinds.All` stores; optionally probe all for the Setup preview without importing.
- [ ] Expose gating helpers (`CanImport`, `CanSync`) for the later import/sync tasks; actual wiring lands in T118/T121/T122.
- [ ] Keep the provider thread-safe for the background sync.
- [ ] Enforce `CheckOnLaunch`/`EnrichNewGames` ⇒ `IndexEnabled` at the provider boundary.

## Context

- No UI in this task; this is the shared contract.
- Provider must never throw on unknown/removed kinds.

## Requirements

- [ ] One source of truth for policy reads/writes.
- [ ] Disabled stores cannot be imported or synced by accident.
- [ ] No store-file writes.

## Verification

- [ ] Unit tests: enabled/disabled gating for discovery, import, and sync.
- [ ] `dotnet build` / `dotnet test` green.

## Completion Notes

- **Completed:** 2026-10-25
- **What was done:** Added `IStorePolicyProvider` and a thread-safe `StorePolicyProvider` over
  `IConfigService`. Reads normalize stored policy, ignore unknown/non-launcher rows, and return a
  disabled default for missing stores. Writes reject non-launcher kinds with `ArgumentException`
  and re-normalize flags. Exposed `EnabledStores`, `CheckOnLaunchStores`, `CanImport`, and
  `CanSync` for the later discovery/import/sync work. Added provider gating, normalization,
  preservation, non-launcher, and concurrency tests. No T117/T118/T121/T122 components were
  touched; the provider is not registered in composition because no consumer exists yet.
- **Verification:** `dotnet build` succeeded with 0 errors (3 pre-existing Avalonia AVLN3001
  warnings); `dotnet test` passed 686 tests (App 485, Core 200, Migration 1); probe matrix
  validator all scenarios pass.
- **Issues encountered:** None.
