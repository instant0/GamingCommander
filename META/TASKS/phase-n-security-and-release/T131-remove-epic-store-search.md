# Task T131: Remove the Unreferenced Epic Store Client

**Tier:** 4 — Cleanup
**Phase:** N — Security and Release Integrity
**Risk:** Low
**Status:** pending
**Prerequisites:** Task T131 depends on Task T124

## Objective

Remove the compiled Epic store GraphQL client that the application never calls, so the "never
contacted" claim in the live documents is true by construction.

## What Needs to Change

- [ ] Confirm `EpicStoreSearch` has no callers outside its own file (`grep -rn "EpicStoreSearch"`).
- [ ] Delete `src/GamingCommander.App/Services/EpicStoreSearch.cs`.
- [ ] Remove any dead DTOs or constants that exist only for it.
- [ ] Update `META/CODE_MAP.md` so the file is no longer listed.
- [ ] If the consolidation review classifies it as research evidence, move it out of the compiled project
      instead of deleting it; do not leave an unused type in the app assembly.

## Context

- The client posts to `https://store.epicgames.com/graphql`.
- `docs/ONLINE-AND-DATA.md` lists the storefront endpoint as never contacted.
- Epic inventory work reads local manifests only; it does not need a store client.

## Requirements

- [ ] No reference to `EpicStoreSearch` remains in the solution.
- [ ] No unused Epic store DTO remains in the app assembly.
- [ ] Code map and live documents no longer contradict the code.
- [ ] Detection and inventory behaviour are unchanged.

## Verification

- [ ] `grep -rn "EpicStoreSearch" src tests` returns nothing.
- [ ] `dotnet build` / `dotnet test` green.
- [ ] No new network client appears in the app assembly.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
