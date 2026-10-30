# Phase I — Post-MVP Navigation Status Tracker

**Plan:** [`planning/101-top-level-modes-and-filter.md`](../../../planning/101-top-level-modes-and-filter.md)

> The plan is background. The live specs are the two task docs below; both were rewritten on 2026-10-30
> against the current code.

---

## Status Legend

| Status | Meaning |
|--------|---------|
| Pending | Not started |
| Ready | Spec complete, safe to start |
| Under Progress | Active work in progress |
| Complete | Done, tests pass |
| Deferred | Intentionally postponed |
| Cancelled | No longer needed |

---

## Task Tracker

| # | Task | Phase | Priority | Status |
|---|------|-------|----------|--------|
| T72 | [Top-level view modes (F9): Library / Stores / Engines](T72-top-level-mode-switcher.md) | 1 | P2 | Ready |
| T73 | [Flat view + quick scope (All Games home)](T73-flatten-library-view.md) | 2 | P2 | Ready |

What already shipped and is **not** part of either task: engine detection and `GameEntry.GameEngine`
(Plan 102/103), the F8 tag/store filter, and the existing library browser.

---

## Execution Order

```
T72 (view-mode state machine) → T73 (Flat mode, home view, quick scope)
```

T73 extends the seams T72 leaves: it adds `Flat` to the enum, re-points the reset callers to Flat, makes
Flat the default, and moves the top of the Backspace chain from Library to Flat. Both documents state that
seam explicitly.

Neither task blocks the store-capability track (T117/T118/T121). Run them after the P1 chain or in
parallel with a docs phase.
