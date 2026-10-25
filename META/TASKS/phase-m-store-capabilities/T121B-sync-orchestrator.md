# Task T121B: Startup Orchestrator and Time Budget

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** High
**Status:** pending
**Prerequisites:** Task T121B depends on Task T121A

## Objective

Run the incremental sync once per launch, in the background, within a strict time budget, and never
block or fail the app.

## What Needs to Change

- [ ] `StoreSyncService.RunOnStartupAsync(CancellationToken)` runs only stores with `CheckOnLaunch`.
- [ ] Call it from `App.axaml.cs` after services/config load; do not await it on the UI path.
- [ ] Per-store budget (~2 s) and overall budget (~5 s); cancel on timeout.
- [ ] Log one line per run: stores checked, added, updated, missing, elapsed.
- [ ] Any exception is caught and logged; the app continues.
- [ ] Skip instantly when no store has `CheckOnLaunch`.
- [ ] `EnrichNewGames` runs the normal folder scan only for added paths, after the diff.

## Context

- Readers and diff come from T118/T121A.
- The check must feel free on a normal launch; no folder walk unless enrichment is requested.

## Requirements

- [ ] No UI block; no crash on a bad store.
- [ ] Time budget enforced and tested.
- [ ] No writes to store files.

## Verification

- [ ] Test with a slow fake reader: cancellation returns control and logs.
- [ ] Test with no enabled stores: immediate no-op.
- [ ] `dotnet build` / `dotnet test` green.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
