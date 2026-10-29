## Status

**2026-10-25 — audit follow-ups, title canonicalization, and the Steam location rule are complete.** Detection remains the priority. Plan 123's review gate is **CLOSED** (agent; human sign-off pending). Plan 127 A/B is complete. Epic `.item` re-creation and SyncMove remain deferred; persisted schemas are disposable pre-ship. **2026-10-29 — security-review findings split into T124B/T124C/T124D and Phase N. The Epic store client stays as a deferred opt-in metadata feature (T131), not a removal. T125 may start immediately. T124 is done: the shared path rule landed as `PathText`; store identity, locator, inventory reader, scan writer, policy gate and stat stay open and are decided with their consumers.**

## Active queue (ordered)

1. **T124B** — persistence write review: one JSON store, backup → write → verify, rolling `.bak`, app-folder-only `CreateDirectory`.
2. **T124C** — repair-write safety review: guard or remove, no external directory creation, generate in `data/staging/` → copy → length check.
3. **T124D** — Epic online metadata client disposition: keep deferred, define the opt-in gate.
4. **T124A** — generate/refresh the connection map (composition, data flows, extension recipes).
5. **T125** — release path cleanup (one workflow, high/critical scan gate, SHA-pinned actions, checksum; independent).
6. **T126** — reconcile the data contract and Readme access inventory (after T124D; before T117).
7. **T129** — guarded repair writes, or removal (after T124C; before T117).
8. **T130** — safe local-state writes with visible failure (after T124B; required before T121).
9. **T117** — launcher discovery + first-run opt-in (subtasks T117A–B).
10. **T118** — read-only store inventory import (subtasks T118A–C).
11. **T121** — incremental store sync on every launch (subtasks T121A–C).
12. **T122** — Setup screen per-store controls (subtasks T122A–B).
13. **T119** — launcher-vs-disk health + suggestions (subtasks T119A–B).
14. **T127** — security contact + download verification docs (after T125).
15. **T128** — supply-chain hygiene (Dependabot, lock files, warnings-as-errors; phase boundary).
16. **Epic engine detection (P2)** — author its task doc after the store work.
17. **Remaining detector/docs backlog** — O3 is covered by T124D; O4/O5/O8/O10 dedup, deep 4-level walk, `DetectionLogger`, doc splits.

## Deferred (do NOT start)

- **T131 — Epic online metadata opt-in gate** — deferred until online Epic metadata is scheduled. The client is kept, not deleted, and must not be called by default.
- **Epic `.item` re-creation / registration** — dead end; detection reads existing manifests only.
- **SyncMove Phase 2.1** — blocked until detection is working.
- **T72/T73** — P2; not next.
- **Plan 127 C** (`Extensions{}` / emulator) — needs a user decision.
- **SDK .NET 8→9** — deferred indefinitely.

## Standing rules

One file at a time; new files before removal; detection behavior-preserving; **persisted schemas are disposable pre-ship — no back-compat**; Windows paths opaque; full build + suite after each code step. Documentation-only sessions do not build/test. Do not reference task IDs in non-task documents.

## Not next (backlog)

Plan 101 / T72 mode switcher, 104 detect.py split, 97 themes, 111 logging toggle, 110 user tags, 91 blacklist editor, 05-phase-3 category browse, §8.3 Program Files (x86) layout plan, 90 SDK upgrade. EA format caveat remains blocked on a complete install.
