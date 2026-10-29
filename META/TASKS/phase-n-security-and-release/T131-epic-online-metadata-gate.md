# Task T131: Epic Online Metadata Opt-In Gate

**Tier:** 3 — Logic/Behavior
**Phase:** N — Security and Release Integrity
**Risk:** Medium
**Status:** deferred
**Prerequisites:** Task T131 depends on Task T124D

> Deferred. Do not start until Epic online metadata is scheduled. This task exists so the client is
> kept, and so it cannot be called by default.

## Objective

Wire `EpicStoreSearch` behind the existing online-metadata opt-in. Until then, keep it unreferenced
and documented as deferred; do not delete it.

## What Needs to Change

### 1. Gate
- [ ] Put the call behind the existing online-metadata switch; do not add a second global toggle.
- [ ] Require an explicit user opt-in for Epic online lookup; default off.
- [ ] Call only for a game that lacks local `.item`/`.mancpn` metadata.
- [ ] One request per game; timeout; no retry storm.
- [ ] Never call from scan, startup sync, or launch.

### 2. Pipeline
- [ ] Route through the normal metadata queue/service, not a direct call from a detector.
- [ ] Record the source (`Epic Store`) and the confidence of the returned title.
- [ ] Do not overwrite local catalog IDs with the returned IDs (documented in `docs/research/epic_item_format.md`).

### 3. Tests
- [ ] Default path: no HTTP request.
- [ ] Enabled path: one request, one parse, no retry.
- [ ] Failure path: timeout/HTTP error leaves local metadata unchanged.
- [ ] Use a fake transport; no network in tests.

### 4. Docs
- [ ] `docs/ONLINE-AND-DATA.md` endpoint row matches the implementation.
- [ ] `META/CODE_MAP.md` marks the client as active, not deferred, when this ships.

## Context

- `src/GamingCommander.App/Services/EpicStoreSearch.cs` implements `searchStore` against
  `https://store.epicgames.com/graphql`.
- `planning/109-epic-manifest-enrichment.md` and `planning/119-metadata-sidecar.md` reserve online Epic
  as a later fallback.
- `docs/research/epic_item_format.md` documents the regen use and the "do not overwrite local IDs" rule.
- The existing metadata pipeline already has an online gate and a queue.

## Requirements

- [ ] No default code path reaches the client.
- [ ] The client is called at most once per game per lookup run.
- [ ] The user can disable Epic online lookup independently of other online metadata.
- [ ] Local metadata wins over the online result.
- [ ] No new network client, package, or endpoint is added.

## Verification

- [ ] Test: default configuration makes zero requests.
- [ ] Test: enabled configuration makes one request and records the source.
- [ ] Test: HTTP failure leaves the entry unchanged.
- [ ] `dotnet build` / `dotnet test` green.
- [ ] Manual check that scan and launch paths do not reference the client.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
