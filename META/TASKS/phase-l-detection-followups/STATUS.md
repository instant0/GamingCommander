# Phase L — Detection Follow-ups (Task Tracker)

**Plan:** [`planning/128-doc-and-task-audit.md`](../../../planning/128-doc-and-task-audit.md) §2 and `docs/detection/09-blueprint.md` §14
**Scope:** detector P1/P2 work. Code tasks require build + tests; documentation tasks say so.

---

## Status Legend

| Status | Meaning |
|--------|---------|
| Pending | Not started |
| In Progress | Active work in progress |
| Complete | Done, tests pass |
| Blocked | Needs a user decision |
| Deferred | Intentionally postponed |

---

## Task Tracker

| # | Task | Tier | Priority | Status |
|---|------|------|----------|--------|
| T102 | [O9 — unify title edition stripping](T102-o9-title-canonicalization.md) | 3 | P1 | Pending |
| T103 | [SteamEmu classification decision (ADR)](T103-steam-emu-adr.md) | 1 | P1 | **Blocked — user decision** |

**Not yet authored** (specified in `planning/128-doc-and-task-audit.md` §2): T104 Epic engine detection, T105 O3 Epic signal + dead-code removal, T106 O4/O5 store-signal dedup, T107 O8 path normalizer, T108 O10 Epic cheap-exe, T109 structured `DetectionLogger`.

---

## Execution Order

```
T102 (O9) ──────────────────────────────► next code task
T103 (SteamEmu ADR) — blocked on user; do not start code
T104 (Epic engine) after T102
```
