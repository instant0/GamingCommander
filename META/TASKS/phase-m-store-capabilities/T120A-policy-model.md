# Task T120A: Store Policy Model and Persistence

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Low
**Status:** completed
**Prerequisites:** Task T120A depends on Task T120

## Objective

Implement the per-store policy records and their `settings.json` persistence.

## What Needs to Change

### 1. Core model
- [ ] `StorePolicy` and `StorePolicySettings` with safe defaults (`IndexEnabled`/`CheckOnLaunch`/`EnrichNewGames` false; `SkipUnchangedManifests` true).
- [ ] Reuse `GameSourceKind`; add/use `LauncherKinds.All`. Do not add a duplicate enum.
- [ ] DTO uses `string Kind` (enum name), parsed with `Enum.TryParse`; unknown/non-launcher kinds are ignored.
- [ ] Validation: null/absent list → defaults; `CheckOnLaunch`/`EnrichNewGames` forced false when `IndexEnabled` is false.

### 2. Persistence
- [ ] Extend `AppConfig` and `JsonConfigService` (load/save).
- [ ] Keep the existing schema-version pattern; no migration for old files.
- [ ] Do not write policy into `libraries.json`.

### 3. Tests
- [ ] Round-trip all flags and an inventory override path.
- [ ] Missing file, partial map, unknown kind.
- [ ] Defaults are all-off.

## Context

- `AppConfig` currently holds hidden folders, seeded state, online-metadata toggle, and scan depth.
- Settings are disposable pre-ship; no back-compat burden.

## Requirements

- [ ] Persisted shape is stable and documented in `docs/DATA-FORMAT.md`.
- [ ] All flags default false.
- [ ] No store-file writes.

## Verification

- [ ] `dotnet build` / `dotnet test` green.
- [ ] JSON fixture shows the expected shape.

## Completion Notes

- **Completed:** 2026-10-25
- **What was done:** Added `LauncherKinds` (eight store launchers only), `StorePolicy`, and
  `StorePolicySettings`; extended `AppConfig` with optional `StorePolicies`. `JsonConfigService`
  now maps a DTO that stores `Kind` as the enum name, ignores unknown/non-launcher kinds, applies
  safe defaults (`SkipUnchangedManifests` true, all other flags false), and forces
  `CheckOnLaunch`/`EnrichNewGames` off when `IndexEnabled` is false. Documented the persisted
  shape in `docs/DATA-FORMAT.md`. Added persistence and normalization tests.
- **Verification:** `dotnet build` succeeded with 0 errors (3 pre-existing Avalonia AVLN3001
  warnings); `dotnet test` passed 686 tests (App 485, Core 200, Migration 1); probe matrix
  validator all scenarios pass.
- **Issues encountered:** None.
