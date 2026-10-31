# Task T121: Incremental Store Sync on Launch

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** High
**Status:** pending
**Prerequisites:** Task T121 depends on Task T118, Task T120, Task T123, and Task T130

## Objective

On every launch, for stores the user allowed, quickly read only the launcher inventory, diff it
against the persisted games, add newly installed games, update changed ones, and mark missing ones.
Enrich newly added games only if the user opted in.

## What Needs to Change

### 1. Diff model
- [ ] `StoreInventoryDiff`: `Added`, `Updated`, `Missing`, `ExeMissing`.
- [ ] Match by `StoreKey` first (via `GameStoreIndex`); fall back to normalized install path (`PathText`). A name-only match is allowed only inside the same store and only when exactly one entry has that name; otherwise the record is added.
- [ ] An unchanged inventory must produce an empty diff (no writes).
- [ ] Preserve `DetectionOrigin` and user data (tags, launch profiles, relations) through the merge.

### 2. Orchestrator
- [ ] `StoreSyncService.RunOnStartupAsync(CancellationToken)`: only stores with `CheckOnLaunch`, i.e.
      `IStorePolicyProvider.CanSync(kind)` is true. Do not read the policy twice or cache a stale answer.
- [ ] Only run a store that has a reader. A store with no inventory (Xbox; EA/Battle.net until verified) is
      skipped, not "checked and found empty".
- [ ] Skip a store when its manifest fingerprint is non-null, unchanged, and `SkipUnchangedManifests` is on.
      No fingerprint (no manifest) means no skip — never treat "no fingerprint" as "unchanged".
- [ ] Persist fingerprint + last-run summary in a small disposable `data/store-sync.json` cache (not `settings.json`, not `games.json`) through the shared JSON store; no direct file write.
- [ ] Read the inventory once and use what the enumeration/read returned; check existence only where the
      state itself is the answer (`planning/135` read rule). **No folder walk, no probe-before-read.**
- [ ] The sync cache is written through the shared JSON store (`planning/132`). This task must not land
      before that store exists, and must not add a second write path.
- [ ] Build/refresh the `GameStoreIndex` from the loaded entries once per run; do not scan `games.json`
      per store.
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
- [ ] Removed store game becomes `Missing`, not deleted — **only for inventory-backed stores**.
- [ ] A store with no reader is skipped, not treated as an empty inventory.
- [ ] No duplicate entries on repeated launches.
- [ ] No recursive enumeration in the sync path.
- [ ] The diff uses `GameStoreIndex` (O(manifest)), not a scan of `games.json`.
- [ ] An unchanged manifest fingerprint skips parsing and produces no writes; a missing fingerprint never
      skips.
- [ ] A no-change launch stays within the time budget.
- [ ] No writes to store files and no directory creation.
- [ ] The sync cache contains paths and fingerprints only; no secrets or user content.
- [ ] The launch path performs no redundant registry or filesystem call (`planning/135`).

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
