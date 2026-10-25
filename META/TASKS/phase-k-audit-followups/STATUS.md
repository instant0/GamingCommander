# Phase K — Audit Follow-ups (Task Tracker)

**Plan:** [`planning/128-doc-and-task-audit.md`](../../../planning/128-doc-and-task-audit.md)
**Scope:** documentation/state hygiene only. No code, no build/test.

---

## Status Legend

| Status | Meaning |
|--------|---------|
| Pending | Not started |
| In Progress | Active work in progress |
| Complete | Done; docs reviewed |
| Deferred | Intentionally postponed |

---

## Task Tracker

| # | Task | Tier | Priority | Status |
|---|------|------|----------|--------|
| T81 | [Reconcile task statuses + template](T81-reconcile-task-statuses.md) | 1 | P0 | Complete |
| T82 | [Archive closed phase dirs](T82-archive-closed-phases.md) | 2 | P0 | Pending |
| T83 | [Refresh CODE_MAP](T83-refresh-code-map.md) | 1 | P0 | Pending |
| T84 | [ROADMAP + Plan 127 completion record](T84-roadmap-plan127-record.md) | 1 | P0 | Pending |
| T85 | [Refresh TECH_DEBT + IDEAS](T85-refresh-tech-debt-ideas.md) | 1 | P1 | Pending |
| T86 | [planning/README + tracker table](T86-planning-readme-trackers.md) | 1 | P1 | Pending |
| T87 | [Plan 103 parity + Plan 123 residual text](T87-detection-plan-status.md) | 1 | P1 | Pending |
| T88 | [Superseded/partial plan status pass](T88-superseded-plan-status.md) | 1 | P1 | Pending |
| T115 | [Redefine CURRENT.md; remove task references](T115-current-md-purpose.md) | 1 | P1 | Pending |

---

## Execution Order

```
T81 → T82 → T83/T84/T85 → T87/T88 → T86 → T115
```

T83, T84, T85, T87, and T88 touch separate files and may run in parallel after T81. T86 runs last so it reflects the final statuses.
