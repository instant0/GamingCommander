# Phase I — Post-MVP Navigation Status Tracker

**Plan:** [`planning/101-top-level-modes-and-filter.md`](../../../planning/101-top-level-modes-and-filter.md)

---

## Status Legend

| Status | Meaning |
|--------|---------|
| Pending | Not started |
| Under Progress | Active work in progress |
| Complete | Done, tests pass |
| Deferred | Intentionally postponed |
| Cancelled | No longer needed |

---

## Task Tracker

| # | Task | Phase | Priority | Status |
|---|------|-------|----------|--------|
| T72 | [Top-level mode switcher](T72-top-level-mode-switcher.md) | 1 | P2 | Pending |
| T73 | Flatten library view | 2 | P2 | Pending (not yet written) |
| T74 | [Game filter (F5) + user-editable tags](T74-game-filter-and-tags.md) | 3 | P2 | **Superseded in part** — F8/S filter shipped 2026-08-22 (not this task’s F5 design) |

---

## Execution Order

```
T72 (Mode Switcher) → T73 (Flatten) → T74 (Filter)
```

T73 and T74 are independent of each other but both depend on T72.
