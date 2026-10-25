## Status

**2026-10-25 — documentation/task audit complete** (`planning/128-doc-and-task-audit.md`). Detection remains the priority. Plan 123 is substantively complete; its amended review gate is **CLOSED** (agent, 2026-10-14; human sign-off pending). Plan 127 A/B is complete. Epic `.item` re-creation (T78) and SyncMove (T80 / Phase 2.1) remain deferred.

## Active queue (ordered)

1. **T81** — reconcile task statuses + fix template (docs-only).
2. **T82** — archive closed phase dirs (depends T81).
3. **T83** — refresh `META/CODE_MAP.md`.
4. **T84** — ROADMAP + Plan 127 completion record.
5. **T85** — refresh `TECH_DEBT` + `IDEAS`.
6. **T87** — Plan 103 parity + Plan 123 residual text.
7. **T88** — superseded/partial plan status pass.
8. **T89** — Plan 124/126/121 status alignment.
9. **T86** — `planning/README.md` + tracker table.
10. **T115** — redefine `CURRENT.md`; remove task references from non-task docs (depends T86).
11. **T102** — **O9 title canonicalization** (first product/code task).
12. **T103** — SteamEmu ADR — **blocked on user decision**.

Remaining backlog (`T90`–`T101`, `T104`–`T114`) is in `planning/128-doc-and-task-audit.md` §2.

## Deferred (do NOT start)

- **Epic `.item` re-creation / registration (`T78`)** — dead end; detection reads existing manifests only.
- **SyncMove Phase 2.1 (`T80`)** — blocked until detection is working.
- **T72/T73** — P2; not next; decision tracked in `T99`.
- **Plan 127 C** (`Extensions{}` / emulator) — decision tracked in `T110`.
- **SDK .NET 8→9** — deferred indefinitely.

## Standing rules

One file at a time; new files before removal; detection behavior-preserving; **persisted schemas are disposable pre-ship — no back-compat**; Windows paths opaque; full build + suite after each code step. Documentation-only sessions do not build/test.

## Not next (backlog)

Plan 101 / T72 mode switcher, 104 detect.py split, 97 themes, 111 logging toggle, 110 user tags, 91 blacklist editor, 05-phase-3 category browse, §8.3 Program Files (x86) layout plan, 90 SDK upgrade (deferred). Open TECH_DEBT: see `META/BACKLOG/TECH_DEBT.md` (refreshed by `T85`); EA format caveat remains.
