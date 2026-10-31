# Phase J — Post-126 Follow-ups (Task Tracker)

**Plan:** derived from the 2026-10-14 pending-task audit (see `META/SESSION/CURRENT.md`).
**Source plans:** `planning/126-epic-manifest-review-pass.md`, `planning/123-detection-bugfixes.md`, `planning/04-phase-2-syncmove.md`.

---

## Status Legend

| Status | Meaning |
|--------|---------|
| Pending | Not started |
| In Progress | Active work in progress |
| Complete | Done, tests pass, moved to COMPLETED/ |
| Deferred | Intentionally postponed |

---

## Task Tracker

| # | Task | Tier | Priority | Status |
|---|------|------|----------|--------|
| T78 | [Epic .item AppName precedence + EOSH copy](T78-epic-item-appname-and-eosh-copy.md) | 3 | P3 | **Deferred — manifest re-creation is a dead end (2026-10-14)** |
| T80 | [Registration repair after file moves](T80-syncmove-phase-2-1.md) | 3 | P1 | Pending |

---

## Execution Order

```
[T78 DEFERRED] · [T80 PENDING]
```

**Directives (2026-10-31):**
1. **Detection is the priority.** Focus on detection using **existing manifest files** (read-only).
2. **Epic `.item` re-creation is deferred** — it could not be made to register a game (Plan 126). Do not implement writer/registration work.
3. **Registration repair** is now specified: copy/update the store record, never move or delete game data, and report any stray old ACF for manual removal. The durable design is `planning/04-phase-2-syncmove.md`; the safety decision is ADR-005.

## Notes

- 2026-10-25: task/doc drift audit → `planning/128-doc-and-task-audit.md`. The Plan 123 closeout (tier marking + E6a) is complete; only its documentation refresh remains. T80's header/status and slice split is `T93`.
- The Plan 123 **C# detection port is already shipped** (2026-09-07). Do not re-port E1/E4/E5/E6/E7/E8.
- The deferred follow-ups are code/feature work and require `dotnet build` + `dotnet test` per `META/RULES.md` when started.
