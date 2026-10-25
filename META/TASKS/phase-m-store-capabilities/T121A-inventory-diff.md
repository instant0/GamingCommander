# Task T121A: Inventory Diff and Reconciliation

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** pending
**Prerequisites:** Task T121A depends on Task T121

## Objective

Compute `Added` / `Updated` / `Missing` by stable store ID and apply the result without duplicates
or deletions.

## What Needs to Change

- [ ] `StoreInventoryDiff` record and a pure diff function (inventory × persisted games).
- [ ] Match by `StoreKey` first (via `GameStoreIndex`); fall back to normalized install path, then display name.
- [ ] `Updated` when install path, version, size, store state, or exe existence changed.
- [ ] `Missing` when a persisted store entry has no inventory record.
- [ ] `ExeMissing` when the store record/folder exists but the recorded exe is absent — never downgrade to `Missing`.
- [ ] Set/keep `DetectionOrigin` (`StoreManifest`/`Merged`) and `LastStoreSeenUtc`.
- [ ] Apply rules: add new; update changed; mark missing; **never delete**; preserve tags, launch profiles, user state, and relations.
- [ ] Re-running the same inventory produces an empty diff.

## Context

- Reuse the stable IDs added by T118.
- Health rules live in T119; this task only computes and applies the diff.

## Requirements

- [ ] No duplicate entries after repeated runs.
- [ ] No user data lost on `Missing`.
- [ ] Merge precedence: store record owns store identity/path/state; file scan fills exe/title/engine gaps; user overrides win.
- [ ] `ExeMissing` never deletes or marks the game `Missing`.
- [ ] Unknown/corrupt inventory records are skipped, not fatal.

## Verification

- [ ] Unit tests: new, removed, moved, changed version, unchanged, duplicate record.
- [ ] `dotnet build` / `dotnet test` green.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
