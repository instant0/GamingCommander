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
| T124 | [Duplicate-capability review + consolidation plan](T124-consolidation-review.md) | 1 | P1 | Pending |
| T124A | [Generate/refresh the connection map](T124A-connection-map.md) | 1 | P1 | Pending |
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
T123/T120 complete → T124 → T124A → T117 → T117A/T117B
     → T118 → T118A/T118B/T118C
     → T121 → T121A/T121B/T121C
     → T122 → T122A/T122B
     → T119 → T119A/T119B
```

T117A/B may run in parallel after T117. T118A/B/C may run in parallel after T117. T121A/B/C and
T122A/B are sequential. T119 runs after the inventory readers it validates.
