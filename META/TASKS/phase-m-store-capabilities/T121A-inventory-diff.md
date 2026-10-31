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
- [ ] Match by `StoreKey` first (via `GameStoreIndex`); fall back to normalized install path (`PathText`).
      A display-name match is allowed only inside the same store and only when exactly one entry has that
      name; otherwise the record is added.
- [ ] `Updated` when install path, store state, or exe existence changed; include version/size only for
      stores that actually expose them.
- [ ] `Missing` only for an **inventory-backed** store (Steam ACF set, Epic `.item` catalog) whose inventory
      no longer contains a persisted entry. A registry/marker-only store must never produce `Missing` from
      absence.
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
- [ ] `Missing` is produced only by an inventory-backed store; registry/marker absence is not removal.
- [ ] Merge precedence: the store record owns store identity and the *recorded* path; the disk folder owns
      the effective install path and install state; the file scan fills exe/title/engine gaps; user
      overrides win. A mismatch between recorded and effective path is a health result, not a silent
      rewrite of the store's claim.
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
