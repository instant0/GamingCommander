# Task T131: Epic Store Lookup Gate

**Tier:** 3 — Logic/Behavior
**Phase:** N — Security and Release Integrity
**Risk:** Medium
**Status:** deferred
**Prerequisites:** Task T131 depends on Task T124D

> Deferred. Do not start until the Epic store lookup is scheduled. This task exists so the client is
> kept, and so it cannot be called by default.

## Objective

Wire `EpicStoreSearch` behind the existing online-metadata gate as an **identity lookup only**: resolve
canonical title, namespace, and catalog id for a game local files cannot fully identify, and resolve the
title before a PCGW/Steam lookup. Until then, keep it unreferenced and documented as deferred; do not
delete it.

## What Needs to Change

### 1. Gate
- [ ] One gate routine, two call sites, no others:
      1. **identify a detected game** — a folder known/claimed to be Epic with no usable `.item`/`.mancpn`,
         or with incomplete local ids; at most one search per game per lookup run;
      2. **resolve the title before PCGW/Steam** — reuses the result of (1); it must not issue a second
         search.
- [ ] Put both behind the existing online-metadata switch; do not add a second global toggle. When the
      gate is off, skip the lookup and keep using local data.
- [ ] Never call from scan, startup sync, launch, health, or a detector.
- [ ] One request per game per lookup run; timeout; no retry storm; no account or API key.

### 2. Pipeline
- [ ] Route through `MetadataService` / the lookup queue, not a detector.
- [ ] Store only identity: canonical title, `namespace`, catalog id. Do not treat the result as developer,
      genre, release date, or cover metadata — local Epic manifests plus PCGW/Steam supply those.
- [ ] Never overwrite existing local ids. Show the hit when it differs from the local title.
- [ ] Do not use this path to create or regenerate an `.item`. That is a deferred subject and needs its
      own contract.

### 3. Tests
- [ ] Guard: scan, startup sync, launch, and health make zero requests.
- [ ] Use 1: one request per game per lookup run; one parse; no retry.
- [ ] Use 2: no second request when the identity is already resolved.
- [ ] Gate off: no request; local identity is used unchanged.
- [ ] Failure path: timeout/HTTP error leaves local metadata and ids unchanged.
- [ ] Use a fake transport; no network in tests.

### 4. Docs
- [ ] `docs/ONLINE-AND-DATA.md` endpoint row matches the implementation.
- [ ] `META/CODE_MAP.md` marks the client as active, not deferred, when this ships.

## Context

- `src/GamingCommander.App/Services/EpicStoreSearch.cs` implements one `searchStore(keywords)` query
  against `https://store.epicgames.com/graphql` and returns `(Title, Namespace, CatalogId)` only.
- `planning/109-epic-manifest-enrichment.md` and `planning/119-metadata-sidecar.md` reserve online Epic
  as a later fallback.
- `docs/research/epic_item_format.md` documents the regen use and the "do not overwrite local IDs" rule.
- The existing metadata pipeline already has an online gate and a queue.

## Requirements

- [ ] No default code path reaches the client.
- [ ] At most one request per game per lookup run.
- [ ] Behind the existing online-metadata switch; no per-provider toggle.
- [ ] The lookup is identity-only; existing local ids are never overwritten.
- [ ] No `.item` creation or regeneration is performed from this path.
- [ ] No new network client, package, or endpoint is added.

## Verification

- [ ] Test: default configuration makes zero requests.
- [ ] Test: each of the two uses makes at most one request.
- [ ] Test: the second use issues no request after the first resolved the identity.
- [ ] Test: HTTP failure leaves the entry unchanged.
- [ ] `dotnet build` / `dotnet test` green.
- [ ] Manual check that scan and launch paths do not reference the client.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
