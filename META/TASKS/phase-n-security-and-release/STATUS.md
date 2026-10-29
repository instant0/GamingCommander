# Phase N — Security and Release Integrity (Task Tracker)

**Plan:** [`planning/131-security-and-release-integrity.md`](../../../planning/131-security-and-release-integrity.md)
**Scope:** one fail-closed release path, an accurate live contract, durable local state, guarded repair writes, and supply-chain hygiene.

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
| T125 | [Release integrity gate](T125-release-integrity-gate.md) | 3 | P1 | Pending |
| T126 | [Reconcile the data contract](T126-contract-accuracy.md) | 1 | P1 | Pending |
| T129 | [Guarded repair writes](T129-guarded-repair-writes.md) | 3 | P1 | Pending |
| T130 | [Atomic local-state writes](T130-atomic-local-state-writes.md) | 3 | P1 | Pending |
| T127 | [Security contact and download verification](T127-security-contact-and-verification.md) | 1 | P2 | Pending |
| T131 | [Remove the unreferenced Epic store client](T131-remove-epic-store-search.md) | 4 | P2 | Pending |
| T128 | [Supply-chain hygiene](T128-supply-chain-hygiene.md) | 3 | P2 | Pending |

---

## Execution Order

```
T125 independent (release path)
T126 independent (contract must be accurate before new store readers)

Phase M: T124 → T129 / T130 / T131
T129 and T130 must land before the launcher-discovery and inventory work consumes them.

T127 after T125 (checksums must exist before the verification instructions)
T128 at a phase boundary (lock files and warnings-as-errors)
```

T125 and T126 may start immediately and do not block Phase M. T129, T130, and T131 require the
consolidation plan from T124; none of them may add a second writer or a parallel JSON path.
