# Task T121: Incremental Store Sync on Launch

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** High
**Status:** pending
**Prerequisites:** Task T121 depends on Task T118, Task T120, and Task T123

## Objective

On every launch, for stores the user allowed, quickly read only the launcher inventory, diff it
against the persisted games, add newly installed games, update changed ones, and mark missing ones.
Enrich newly added games only if the user opted in.

## What Needs to Change

### 1. Diff model
- [ ] `StoreInventoryDiff`: `Added`, `Updated`, `Missing`, `ExeMissing`.
- [ ] Match by `StoreKey` first (via `GameStoreIndex`); fall back to normalized install path then name.
- [ ] An unchanged inventory must produce an empty diff (no writes).
- [ ] Preserve `DetectionOrigin` and user data (tags, launch profiles, relations) through the merge.

### 2. Orchestrator
- [ ] `StoreSyncService.RunOnStartupAsync(CancellationToken)`: only stores with `CheckOnLaunch`.
- [ ] Skip a store when its manifest fingerprint (path + mtime + size) is unchanged and `SkipUnchangedManifests` is on.
- [ ] Persist fingerprint + last-run summary in a small disposable `data/store-sync.json` cache (not `settings.json`, not `games.json`) through the shared JSON store; no direct file write.
- [ ] Read the inventory once; stat each folder/exe only — **no folder walk**.
- [ ] Per-store budget (e.g. 2 s) and overall budget (e.g. 5 s); cancel cleanly; emit `StoreSyncReport`.
- [ ] Added → import via T118; updated → path/version refresh; missing → `InstallState.Missing` (never delete); exe missing → `ExeMissing`.
- [ ] `EnrichNewGames` → run the normal folder scan only on newly added paths.

### 3. Wiring
- [ ] Call after services/config load in `App.axaml.cs`, non-blocking.
- [ ] Skip entirely when no store has `CheckOnLaunch`.
- [ ] Failures are non-fatal and never block the UI.

## Context

- Reuse T118 readers and T119 health rules.
- The startup check is a *hint* refresh, not a full rescan.

## Requirements

- [ ] New store game appears on the next launch without a manual rescan.
- [ ] Removed store game becomes `Missing`, not deleted.
- [ ] No duplicate entries on repeated launches.
- [ ] No recursive enumeration in the sync path.
- [ ] The diff uses `GameStoreIndex` (O(manifest)), not a scan of `games.json`.
- [ ] An unchanged manifest fingerprint skips parsing and produces no writes.
- [ ] A no-change launch stays within the time budget.
- [ ] No writes to store files and no directory creation.
- [ ] The sync cache contains paths and fingerprints only; no secrets or user content.

## Verification

- [ ] Fixture: import once → next launch adds a new record → next launch marks a removed one.
- [ ] Re-running with no changes produces no diff.
- [ ] Budget test with a slow fake reader cancels without blocking.
- [ ] `dotnet build` / `dotnet test` green.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
