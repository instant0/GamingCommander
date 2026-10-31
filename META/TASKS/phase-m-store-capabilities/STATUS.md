# Phase M — Store Capabilities (Task Tracker)

**Plan:** [`planning/129-store-capabilities-and-launcher-discovery.md`](../../../planning/129-store-capabilities-and-launcher-discovery.md)
**Scope:** verify each store's launcher/inventory/metadata, then add per-store opt-in policy, launcher discovery, read-only inventory import, launch-time sync, setup controls, and health suggestions.

---

## Status Legend

| Status | Meaning |
|--------|---------|
| Pending | Not started |
| In Progress | Active work in progress |
| Blocked | Needs a user decision or external verification |
| Deferred | Intentionally postponed |

---

## Task Tracker

| # | Task | Tier | Priority | Status |
|---|------|------|----------|--------|
| T124 | [Duplicate-capability decision gate](T124-consolidation-review.md) | 1 | P1 | Done (D1; D2–D7 deferred) |
| T124A | [Connection map + per-store decision tree](T124A-connection-map.md) | 1 | P1 | Done |
| T124B | [Persistence write-integrity review](T124B-persistence-review.md) | 1 | P1 | Done |
| T124C | [Repair-write safety review](T124C-repair-write-review.md) | 1 | P1 | Done (Steam create-only, Epic modify-only) |
| T124D | [Epic store lookup client disposition](T124D-epic-client-disposition.md) | 1 | P2 | Closed (merged into T126) |
| T132 | [Registry and filesystem call audit — read once, write once](T132-io-call-audit.md) | 2 | P1 | Done |
| T133 | [Install identity by path, not folder name](T133-install-identity-by-path.md) | 2 | P1 | Done |
| T136 | [Unknown store data sources — documentation/planning](T136-unknown-store-data-sources.md) | 1 | P1 | Done |
| T117 | [Launcher discovery service](T117-launcher-discovery.md) | 3 | P1 | Done |
| T117A | [Locators: file-backed stores](T117A-locators-file-stores.md) | 3 | P1 | Split → A1/A2/A3 |
| T117A1 | [Locators: Steam + Epic (known)](T117A1-locators-steam-epic.md) | 3 | P1 | Done |
| T117A2 | [Locator: GOG](T117A2-locator-gog.md) | 3 | P1 | Done |
| T117A3 | [Locator: Ubisoft registry inventory](T117A3-locator-ubisoft.md) | 3 | P1 | Done |
| T117B | [Battle.net/Rockstar + first-run preview](T117B-locators-db-stores-ui.md) | 3 | P1 | Split → B1/B2/B3 |
| T117B2 | [First-run preview + policy wiring](T117B2-first-run-preview-policy.md) | 3 | P1 | Done |
| T117B3 | [Locator: Battle.net (cancelled)](T117B3-locator-battlenet.md) | 3 | P1 | Cancelled (T136) |
| T137 | [Launcher discovery setup choice](T137-launcher-discovery-setup.md) | 3 | P1 | Done |
| T118 | [Store inventory import](T118-store-inventory-import.md) | 3 | P1 | Pending |
| T118B2 | [EA inventory reader (cancelled)](T118B2-ea-inventory.md) | 3 | P1 | Cancelled (T136) |
| T118C2 | [Ubisoft inventory reader](T118C2-ubisoft-inventory.md) | 3 | P1 | Ready (registry, NeedsVerification) |
| T118C3 | [Battle.net inventory reader (cancelled)](T118C3-battlenet-inventory.md) | 3 | P1 | Cancelled (T136) |
| T121 | [Incremental store sync on launch](T121-launch-sync.md) | 3 | P1 | Pending |
| T121A | [Inventory diff and reconciliation](T121A-inventory-diff.md) | 3 | P1 | Pending |
| T121B | [Startup orchestrator and time budget](T121B-sync-orchestrator.md) | 3 | P1 | Pending |
| T121C | [Store sync integration tests](T121C-sync-tests.md) | 3 | P1 | Pending |
| T122 | [Setup screen store controls](T122-setup-store-controls.md) | 3 | P1 | Pending |
| T122A | [Setup UI rows and binding](T122A-setup-ui.md) | 3 | P1 | Pending |
| T122B | [Test detection and scan now](T122B-test-and-scan.md) | 3 | P1 | Pending |
| T119 | [Inventory health + suggestions](T119-inventory-health.md) | 3 | P1 | Pending |
| T119A | [Health model + per-store probes](T119A-health-probes.md) | 3 | P1 | Pending |
| T119B | [Status/suggestion UI for all stores](T119B-health-ui.md) | 3 | P1 | Pending |

T116A–C were folded into T116; their docs remain as reference.

---

## Execution Order

```
T123/T120 complete → T124 (done: path rule) / T124B (done: persistence) / T124C (done: repair policy)
     → T124A (done: connection map)
     → T130 (done: shared JSON store) and T129 (done: guarded repair writes)
     → T132 (done: read/write audit)
     → T126 (done: contract pass, carries the Epic identity disposition)
     → T133 (done: install identity by path)
     → T117 → T117A1 (done) / T117A2 GOG / T117B1 Rockstar+Xbox / T117B2 UI
     → T136 (done) → EA/Battle.net cancelled; Ubisoft locator + registry reader ready
     → T137 (launcher discovery setting + FileOnly gating) after T117B2
     → T118 → T118A (done: Steam + GOG) / T118B (done: Epic) / T118C (done: Rockstar) / T118C2  (T118 requires T130; EA/Battle.net readers cancelled)
     → T119 → T119A/T119B        (health rules; must precede repair and sync)
     → T80 (registration repair; consumes T119A, no move/delete)
     → T121 → T121A/T121B/T121C  (T121 requires T118, T119, T130)
     → T122 → T122A/T122B
```

**Queue rationale:** readers first because health, setup scan-now, and sync all consume the same
inventory; health before repair so the mismatch model is defined once; repair before sync because it is the
explicit user fix; sync before setup because setup reports the diff the sync service produces.

T132 (registry and filesystem call audit) sits after T129/T130 and before T121: audit the final read and
write paths, then apply the result before a redundant call is repeated on every launch. It also waits on
T124A for the call-site index. It is independent of T117/T118 and must not delay them.

T124's companion reviews are complete: the connection map, persistence contract, and repair-write policy
are done; the Epic disposition was merged into the contract pass.

T117A1 (Steam/Epic), T117A2 (GOG), and T117B1 (Rockstar/Xbox) are independent locator tasks; T117B2
(preview/policy) depends only on the service. T136 decided EA and Battle.net are detection-only, Ubisoft uses
a registry locator + inventory reader, and Arc is not a store; the EA/Battle.net locator and reader tasks are
cancelled. Steam is resolved from files; Epic from the known ProgramData manifests. T118A/B/C/T118C2 may run
in parallel after T117. T121A/B/C and T122A/B are sequential. T119 runs after T118 and before T121.

T133 landed before T118B: both scanners now key identity by full path (with a uniqueness-guarded weak hint
for moves), so two libraries with the same folder name no longer collapse into one entry.

T124 is closed for path text only. Store identity, locator, inventory reader, scan writer, policy gate
and the stat helper are decided inside the task that first needs them, against `planning/130`.

Phase N's guarded repair writes and safe local-state writes have landed; no new repair entry point or
second JSON path may be added.
