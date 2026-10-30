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
| T124A | [Connection map + per-store decision tree](T124A-connection-map.md) | 1 | P1 | Ready (Proposal B adopted) |
| T124B | [Persistence write-integrity review](T124B-persistence-review.md) | 1 | P1 | Done |
| T124C | [Repair-write safety review](T124C-repair-write-review.md) | 1 | P1 | Done (Steam create-only, Epic modify-only) |
| T124D | [Epic online metadata client disposition](T124D-epic-client-disposition.md) | 1 | P2 | Pending |
| T132 | [Registry and filesystem call audit — read once, write once](T132-io-call-audit.md) | 2 | P1 | Pending |
| T117 | [Launcher discovery service](T117-launcher-discovery.md) | 3 | P1 | Pending |
| T117A | [Locators: Steam/Epic/GOG/EA/Ubisoft](T117A-locators-file-stores.md) | 3 | P1 | Pending |
| T117B | [Locators: Battle.net/Xbox/Rockstar + first-run opt-in](T117B-locators-db-stores-ui.md) | 3 | P1 | Pending |
| T118 | [Store inventory import](T118-store-inventory-import.md) | 3 | P1 | Pending |
| T118A | [Steam + GOG inventory readers](T118A-steam-gog-inventory.md) | 3 | P1 | Pending |
| T118B | [Epic + EA inventory readers](T118B-epic-ea-inventory.md) | 3 | P1 | Pending |
| T118C | [Ubisoft + Battle.net + Rockstar inventory readers](T118C-ubisoft-battlenet-rockstar-inventory.md) | 3 | P1 | Pending |
| T121 | [Incremental store sync on launch](T121-launch-sync.md) | 3 | P1 | Pending |
| T121A | [Inventory diff and reconciliation](T121A-inventory-diff.md) | 3 | P1 | Pending |
| T121B | [Startup orchestrator and time budget](T121B-sync-orchestrator.md) | 3 | P1 | Pending |
| T121C | [Store sync integration tests](T121C-sync-tests.md) | 3 | P1 | Pending |
| T122 | [Setup screen store controls](T122-setup-store-controls.md) | 3 | P1 | Pending |
| T122A | [Setup UI rows and binding](T122A-setup-ui.md) | 3 | P1 | Pending |
| T122B | [Test detection and scan now](T122B-test-and-scan.md) | 3 | P1 | Pending |
| T119 | [Inventory health + suggestions](T119-inventory-health.md) | 3 | P2 | Pending |
| T119A | [Health model + per-store probes](T119A-health-probes.md) | 3 | P2 | Pending |
| T119B | [Status/suggestion UI for all stores](T119B-health-ui.md) | 3 | P2 | Pending |

T116A–C were folded into T116; their docs remain as reference.

---

## Execution Order

```
T123/T120 complete → T124 (done: path rule) / T124B (done: persistence contract) / T124C (done: Steam create-only, Epic modify-only) / T124D
     → T124A
     → T129 (guarded repair) and T130 (shared JSON store)
     → T117 → T117A/T117B
     → T118 → T118A/T118B/T118C        (T118 requires T130)
     → T132 → (read/write audit; after T124A, T129, T130)
     → T121 → T121A/T121B/T121C        (T121 requires T130)
     → T122 → T122A/T122B
     → T119 → T119A/T119B
```

T132 (registry and filesystem call audit) sits after T129/T130 and before T121: audit the final read and
write paths, then apply the result before a redundant call is repeated on every launch. It also waits on
T124A for the call-site index. It is independent of T117/T118 and must not delay them.

T124's three companion reviews are independent of each other. T124A depends on T124. T117A/B may run
in parallel after T117. T118A/B/C may run in parallel after T117. T121A/B/C and T122A/B are
sequential. T119 runs after the inventory readers it validates.

T124 is closed for path text only. Store identity, locator, inventory reader, scan writer, policy gate
and the stat helper are decided inside the task that first needs them, against `planning/130`.

Phase N's guarded repair writes and safe local-state writes land after T124C and T124B respectively:
repair before any new repair entry point, state writes before per-launch sync (T121).
