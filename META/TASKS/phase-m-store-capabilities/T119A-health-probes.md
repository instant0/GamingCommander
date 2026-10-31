# Task T119A: Health Model and Per-Store Probes

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** pending
**Prerequisites:** Task T119A depends on Task T119

## Objective

Define the launcher-vs-disk mismatch model and implement the per-store probes that populate
`InstallState` where evidence exists, not just Steam/Epic. Stores without an inventory keep their
disk-derived state.

## What Needs to Change

### 1. Model
- [ ] Add a `HealthReason` enum/record: `Present`, `LauncherOnly` (missing folder), `DiskOnly` (orphan), `Moved` (known root changed), `Duplicate`.
- [ ] Attach it to `InstallInfo` or a small `HealthInfo` record; persist only stable values.
- [ ] Keep the existing `InstallState` values.

### 2. Probes
- [ ] `StoreHealthProbe` compares an imported `StoreInventoryEntry` to the disk (folder + exe).
- [ ] Steam: keep the current Moved/Orphaned/Missing logic; extend with ACF-missing-but-common-present = Orphaned.
- [ ] GOG/EA/Ubisoft/Battle.net/Rockstar: best-effort registry/marker path vs disk. Absence of a registry
      entry is not a removal signal.
- [ ] Epic: `.item` catalog path vs disk. Xbox: no probe — marker detection only, no inventory.
- [ ] No writes.

### 3. Wiring
- [ ] Replace `FolderScanner`'s hardcoded `Installed` where an inventory record exists.
- [ ] Do not regress disk-only entries (no launcher record).

## Context

- `InstallState` / `InstallInfo` already exist; only Steam/Epic populate them.
- Proposal: `planning/129` §3.

## Requirements

- [ ] Every store produces the states its evidence supports; stores without inventory are not forced into
      `Moved`/`Missing`.
- [ ] A missing folder produces `Missing`, not `Installed`.
- [ ] No writes.

## Verification

- [ ] Unit tests per state per store group.
- [ ] `dotnet build` / `dotnet test` green.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
