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
| T125 | [Release path cleanup](T125-release-integrity-gate.md) | 3 | P1 | Pending |
| T126 | [Reconcile the data contract](T126-contract-accuracy.md) | 1 | P1 | Pending |
| T129 | [Guarded repair writes](T129-guarded-repair-writes.md) | 3 | P1 | Done |
| T130 | [Safe local-state writes](T130-safe-local-state-writes.md) | 3 | P1 | Done |
| T127 | [Security contact and download verification](T127-security-contact-and-verification.md) | 1 | P2 | Pending |
| T131 | [Epic store lookup gate](T131-epic-online-metadata-gate.md) | 3 | P3 | Deferred |
| T128 | [Supply-chain hygiene](T128-supply-chain-hygiene.md) | 3 | P2 | Pending |

---

## Execution Order

```
T125 independent (release path)

T126 after T124D (contract wording depends on the Epic client disposition)
T129 after T124C (done)
T130 after T124B (done)

T127 after T125 (checksums must exist before the verification instructions)
T131 deferred until online Epic metadata is scheduled
T128 at a phase boundary (lock files and warnings-as-errors)
```

T125 may start immediately. T129 and T130 have landed (2026-10-31): both repair writers go through the
shared guard (Steam creates a missing ACF, Epic modifies an existing `.item`; `planning/133`), and the
shared JSON store replaces `JsonFileHelper`, so import (T118) and per-launch sync (T121) can proceed
against it. No task may add a second writer or a parallel JSON path. T131 is kept so the deferred Epic
client is not deleted and cannot be called by default.
