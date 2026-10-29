# Task T124D: Epic Online Metadata Client Disposition

**Tier:** 1 — Documentation / Architecture
**Phase:** M — Store Capabilities
**Risk:** Low (analysis only)
**Status:** pending
**Prerequisites:** None

> Documentation-only. Do not run build/test. Do not remove code in this task.

## Objective

Correct the classification of `EpicStoreSearch`. It is a **deferred, opt-in Epic online metadata
client**, not dead code. Define the gate and the document wording, then produce
`planning/134-epic-online-metadata-disposition.md`.

## What Needs to Change

### 1. Evidence
- [ ] Read `planning/109-epic-manifest-enrichment.md` (GraphQL deferred as a fallback).
- [ ] Read `planning/111-logging-toggle-readme-metadata.md` (future endpoint and payload).
- [ ] Read `planning/119-metadata-sidecar.md` ("online Epic is a later fallback").
- [ ] Read `docs/research/epic_item_format.md` (`searchStore` regen step).
- [ ] Confirm `EpicStoreSearch` has no C# callers today and is compiled into the app assembly.

### 2. Decision
- [ ] Keep the client. Withdraw the earlier "remove it" proposal.
- [ ] Record why: it is the prepared Epic leg of the online metadata feature, not orphaned code.
- [ ] Record the gate:
      - only when online metadata is enabled,
      - explicit user opt-in for Epic online lookup,
      - one request per game that lacks local metadata,
      - never during scan or launch,
      - timeout and no retry storm,
      - no API key or account.

### 3. Docs
- [ ] `docs/ONLINE-AND-DATA.md`: the endpoint is **not called in v1** and is reserved for opt-in Epic
      metadata. Do not write "never contacted".
- [ ] `docs/detection/09-blueprint.md` §11: the dead-code line says `EpicStoreSearch` was removed. It
      was not; correct it to "deferred, gated, no callers".
- [ ] `META/CODE_MAP.md`: mark the client as deferred.
- [ ] `docs/README.md` live map: no change unless a new doc is created.

### 4. Tests
- [ ] Define a guard test: the default scan/launch path makes no request.
- [ ] Define the enabled-path test: one request, one parse, no retry.
- [ ] The test seam must not require network access.

### 5. Deliverable
- [ ] `planning/134-epic-online-metadata-disposition.md` with the evidence, the gate, and the consumer
      area (no task IDs in the plan).

## Context

- `src/GamingCommander.App/Services/EpicStoreSearch.cs` implements `searchStore` against
  `https://store.epicgames.com/graphql`.
- The existing metadata pipeline already has an online gate; Epic online lookup must use it, not a
  second switch.
- T126 updates the contract wording. T131 implements the gate when the feature is scheduled.

## Requirements

- [ ] The plan states that removal is withdrawn and why.
- [ ] The plan names every plan that already reserved this feature.
- [ ] The gate is described in terms of the existing online-metadata switch, not a new one.
- [ ] No code, no removal, no build/test.

## Verification

- [ ] `test -f planning/134-epic-online-metadata-disposition.md`.
- [ ] The plan cites the four source documents above.
- [ ] `grep -rn "EpicStoreSearch" src tests` still shows no caller (unchanged).
- [ ] The plan contains a no-network test definition.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
