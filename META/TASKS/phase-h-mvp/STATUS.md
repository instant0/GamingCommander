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
| WP-4 | T69 | Launch UX polish |
| WP-5 | T70 | Windows smoke gate |

---

## Task Tracker

| # | Task | WP | Priority | Status |
|---|------|----|----------|--------|
| T61 | [Fix launch target resolution](T61-fix-launch-target-resolution.md) | WP-1 | P0 | Complete |
| T62 | [Fix launch execution](T62-fix-launch-execution.md) | WP-1 | P0 | Pending |
| T63 | [Launch pipeline unit tests](T63-launch-pipeline-tests.md) | WP-1 | P0 | Pending |
| T64 | [First-run config defaults](T64-first-run-config-defaults.md) | WP-2 | P0 | Pending |
| T65 | [GOG goggame info parser](T65-gog-info-parser.md) | WP-3 | P0 | Pending |
| T66 | [UE-aware exe discovery](T66-ue-aware-exe-discovery.md) | WP-3 | P0 | Pending |
| T67 | [.lnk shortcut exe resolution](T67-lnk-shortcut-resolution.md) | WP-3 | P0 | Pending |
| T68 | [Container recursion improvements](T68-container-recursion.md) | WP-3 | P0 | Pending |
| T69 | [Launch UX polish](T69-launch-ux-polish.md) | WP-4 | P1 | Pending |
| T70 | [Windows smoke gate](T70-windows-smoke-gate.md) | WP-5 | P0 | Pending |

---

## Execution Order

Tasks must be completed in the order listed (T61 → T70). Within WP-1, T61 must complete before T62. T63 depends on T61+T62. Within WP-3, T65–T68 are independent and can be done in any order. T70 is the final gate.

```
T61 → T62 → T63 → T64 → T65/T66/T67/T68 (parallel OK) → T69 → T70
```

---

## MVP Gate Criteria

MVP is READY when:
- [ ] All P0 tasks (T61–T68) are Complete
- [ ] T70 Windows smoke gate passes all checklist items
- [ ] `dotnet build` clean, `dotnet test` green
- [ ] No P0 blockers remain in TECH_DEBT.md related to launch or detection
