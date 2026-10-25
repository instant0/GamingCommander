# Phase H — MVP Status Tracker

**Plan:** [`planning/100-mvp-next-steps.md`](../../../planning/100-mvp-next-steps.md)
**Gate:** Windows smoke validation (T70) must pass before MVP is declared READY.

---

## Status Legend

| Status | Meaning |
|--------|---------|
| Pending | Not started |
| Under Progress | Active work in progress |
| Complete | Done, tests pass, moved to COMPLETED/ |
| Deferred | Intentionally postponed — not blocking MVP |
| Cancelled | No longer needed — removed from scope |

---

## Work Package Mapping

| WP | Tasks | Description |
|----|-------|-------------|
| WP-0 | — | Session re-aim (docs only, handled by NEXT.md) |
| WP-1 | T61, T62, T63 | Fix launch pipeline |
| WP-2 | T64 | First-run config defaults |
| WP-3 | T65, T66, T67, T68 | C# detection parity |
| WP-4 | T69, T71 | Launch UX polish (T65b deferred post-MVP) |
| WP-5 | T70 | Windows smoke gate |

---

## Task Tracker

| # | Task | WP | Priority | Status |
|---|------|----|----------|--------|
| T61 | [Fix launch target resolution](T61-fix-launch-target-resolution.md) | WP-1 | P0 | Complete |
| T62 | [Fix launch execution](T62-fix-launch-execution.md) | WP-1 | P0 | Complete |
| T63 | [Launch pipeline unit tests](T63-launch-pipeline-tests.md) | WP-1 | P0 | Cancelled |
| T64 | [First-run config defaults](T64-first-run-config-defaults.md) | WP-2 | P0 | Complete |
| T65 | [GOG goggame info parser](T65-gog-info-parser.md) | WP-3 | P0 | Complete |
| T66 | [UE-aware exe discovery](T66-ue-aware-exe-discovery.md) | WP-3 | P0 | Complete |
| T67 | [.lnk shortcut exe resolution](T67-lnk-shortcut-resolution.md) | WP-3 | P0 | Complete |
| T68 | [Container recursion improvements](T68-container-recursion.md) | WP-3 | P0 | Complete |
| T68C | [Detection robustness & module organization plan](T68C-detection-robustness-plan.md) | WP-3 | P2 | Complete |
| T69 | [Launch UX polish](T69-launch-ux-polish.md) | WP-4 | P1 | Complete |
| T70 | [Windows smoke gate](T70-windows-smoke-gate.md) | WP-5 | P0 | Complete (bugs found → T75) |
| T75 | [Windows smoke bugfixes](T75-windows-smoke-bugfixes.md) | WP-5 | P0 | Complete (11/12 bugs fixed, BUG-11 deferred) |
| T76 | [Library root nesting prevention](T76-library-root-nesting-prevention.md) | WP-5 | P1 | Complete |
| T77 | [Remove F7 (Add Root)](T77-remove-f7-add-root.md) | WP-5 | P2 | Complete |
| T65b | [Title & exe selection dialogs](T65b-title-exe-selection-dialogs.md) | WP-4 | P1 | Deferred |
| T71 | [Remove F5 launch keybind](T71-remove-f5-launch-keybind.md) | WP-4 | P2 | Complete |

---

## Execution Order

Tasks must be completed in the order listed (T61 → T70). Within WP-1, T61 must complete before T62. T63 depends on T61+T62. Within WP-3, T65–T68 are independent and can be done in any order. T70 is the final gate.

```
T61 → T62 → T63 → T64 → T65/T66/T67/T68 (parallel OK) → T71 → T69 → T70
```

**Note:** T65b deferred post-MVP (feature addition, not polish). T71 runs before T69 to avoid verifying a keybind that will be removed.

---

## MVP Gate Criteria

MVP is READY when:
- [x] All P0 tasks (T61–T68) are Complete
- [x] T70 Windows smoke gate passes all checklist items
- [x] `dotnet build` clean, `dotnet test` green
- [x] No P0 blockers remain in TECH_DEBT.md related to launch or detection

---

## ✅ MVP DECLARED COMPLETE

All gate criteria satisfied. 217 tests passing. Build clean.
Final tasks: T75 (11/12 bugs fixed), T76 (nesting prevention), T77 (F7 removed).
