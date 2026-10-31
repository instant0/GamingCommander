# Task T124D: Epic Store Lookup Client Disposition

**Tier:** 1 — Documentation / Architecture
**Phase:** M — Store Capabilities
**Risk:** Low (analysis only)
**Status:** pending
**Prerequisites:** None

> Documentation-only. Do not run build/test. Do not remove code in this task.

## Objective

Correct the classification of `EpicStoreSearch`. It is a **deferred Epic identity lookup**, not dead
code, and its only job is to resolve the canonical title, `namespace`, and catalog id for a game that
local files cannot fully identify. Reading local `.item`/`.mancpn` manifests already supplies the
metadata; PCGW and the Steam lookup supply the rest. Regenerating `.item` files is a different subject and
is deferred. Produce `planning/134-epic-store-identity-disposition.md` with the purpose, the gate, the doc
corrections, and the test definitions.

### What this is not

- Not a metadata source. Local Epic manifests give the fields; PCGW/Steam fill the gaps.
- Not a `.item` generator. Regeneration is deferred and is not a reason to keep or wire this client.
- Not a registration fix. Plan 126 proved an identification-only `.item` does not register an app that
  EOSH does not report. That is a registration problem, not a reading/lookup problem.

## What Needs to Change

### 1. Evidence
- [ ] Read `planning/109-epic-manifest-enrichment.md` (GraphQL deferred as a fallback).
- [ ] Read `planning/111-logging-toggle-readme-metadata.md` (future endpoint and payload).
- [ ] Read `planning/119-metadata-sidecar.md` ("online Epic is a later fallback").
- [ ] Read `docs/research/epic_item_format.md` (`searchStore` regen step).
- [ ] Read `EpicStoreSearch`: it sends one `searchStore(keywords)` query and returns
      `Hit(Title, Namespace, CatalogId)` only. It has no query-by-id and no richer metadata fields.
- [ ] Note the endpoint disagreement: the plan records `graphql.epicgames.com`; the code uses
      `store.epicgames.com`. The code is authoritative; the plan line is stale.
- [ ] Confirm `EpicStoreSearch` has no C# callers today and is compiled into the app assembly.

### 2. Decision

- [ ] Keep the client. Withdraw the removal proposal: reading the local manifests and looking up Epic
      both work for their intended purpose; the broken path was **registration**, which is a different
      subject and is deferred.
- [ ] Record the single purpose and its two uses. This is the only reason the app may contact the Epic
      store:

| Use | When | Input | Output |
|---|---|---|---|
| **A — identify a detected game** | a folder is known/claimed to be Epic but has no usable `.item`/`.mancpn` (orphan, moved, renamed), or its local ids are incomplete | folder/game title | canonical title, `namespace`, catalog id |
| **B — resolve the title before PCGW/Steam** | the local title is ambiguous for the other source | current title | canonical title for the PCGW/Steam query; ids stored |

  A and B are one request family: at most one search per game per lookup run. B reuses A's result; it does
  not issue a second request.

- [ ] Record what local manifests already provide, so the client is never presented as a metadata source:
      `EpicManifestParser` reads display name, install location, launch executable, catalog namespace,
      catalog item id, app name, incomplete-install and application flags, and categories. PCGW and the
      Steam store lookup supply the remaining fields.
- [ ] Record what is explicitly out of scope and must not be cited as a reason to wire the client:
      - `.item` regeneration/creation (deferred; separate subject);
      - developer/genre/release/cover enrichment (local manifests + PCGW/Steam);
      - registration repair (the path that failed; Plan 126).
- [ ] Record the limits:
      - never from scan, startup sync, launch, health, or a detector;
      - behind the existing online-metadata gate; no second toggle. When the gate is off, skip the
        identity step and keep using local data;
      - one search per game per lookup run; timeout; no retry storm; no API key or account;
      - keyword search can return a different edition. Show the hit and never overwrite existing local ids.
- [ ] Record what the client **does not** return: developer, publisher, genres, release date, cover, or
      AppName. It resolves identity only.

### 3. Docs
- [ ] `docs/ONLINE-AND-DATA.md`: the endpoint is **not called in v1**; when it is wired it is an identity
      lookup only (uses A and B). Do not write "never contacted", and do not list Epic GraphQL among the
      providers the app never uses. Do not describe it as metadata enrichment or `.item` generation.
      The contract-accuracy task owns the rest of this file; this task rewrites only the Epic row and the
      purpose statement.
- [ ] `docs/detection/09-blueprint.md`: reconcile **both** places. The deferred-work note says "unused but
      retained"; §11's dead-code list says "no callers. Removed." Keep the retained line, remove
      `EpicStoreSearch` from the removed list, and describe it as a deferred identity lookup.
- [ ] `META/CODE_MAP.md`: add an entry for `EpicStoreSearch` marked deferred (the file currently has
      none), naming the identity purpose and the two uses.
- [ ] `docs/README.md` live map: no change unless a new doc is created.

### 4. Tests (definitions only — the seam arrives with the implementation)
- [ ] Guard: scan, startup sync, launch, and health probes make zero requests.
- [ ] Use A: one request per game per lookup run; one parse; no retry.
- [ ] Use B: reuses use A's result; no second search.
- [ ] Gate off: no request; local identity is used unchanged.
- [ ] Failure: a timeout or HTTP error leaves local metadata and local ids unchanged.
- [ ] Never overwrite existing local ids with returned ids.
- [ ] The test seam is a fake transport; no network access in tests.

### 5. Deliverable
- [ ] `planning/134-epic-store-identity-disposition.md` with the evidence, the purpose, the gate, and the
      consumer area (no task IDs in the plan).

## Context

- `src/GamingCommander.App/Services/EpicStoreSearch.cs` sends one `searchStore(keywords)` query to
  `https://store.epicgames.com/graphql` and returns `Hit(Title, Namespace, CatalogId)`. No query by id,
  no richer metadata.
- The existing metadata pipeline already has an online gate; the identity lookup must use it, not a
  second switch.
- Local Epic manifests are the metadata source (`EpicManifestParser`); PCGW and the Steam lookup fill the
  remaining fields. The lookup exists only for identity gaps.
- Registration (`.item` acceptance by Epic) and `.item` regeneration are separate, deferred subjects; they
  are not evidence that the lookup is broken.
- T126 updates the contract wording. T131 implements the gate when the feature is scheduled.

## Requirements

- [ ] The plan states that removal is withdrawn and why (registration failed, not reading/lookup).
- [ ] The plan names every plan that already reserved this feature.
- [ ] The plan states the single purpose and its two uses, and says no other reason is permitted.
- [ ] The plan records what the client returns (title, namespace, id) and what it does not.
- [ ] The plan states that `.item` regeneration and metadata enrichment are out of scope.
- [ ] The gate is described in terms of the existing online-metadata switch, not a new one.
- [ ] No code, no removal, no build/test.

## Verification

- [ ] `test -f planning/134-epic-store-identity-disposition.md`.
- [ ] The plan cites the four source documents above.
- [ ] The plan lists the single purpose and the two uses, and no others.
- [ ] `grep -rn "EpicStoreSearch" src tests` still shows no caller (unchanged).
- [ ] The plan contains a no-network test definition for each use, including the guard.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
