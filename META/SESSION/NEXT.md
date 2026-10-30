## Status

**2026-10-25 — audit follow-ups, title canonicalization, and the Steam location rule are complete.** Detection remains the priority. Plan 123's review gate is **CLOSED** (agent; human sign-off pending). Plan 127 A/B is complete. Epic `.item` re-creation and SyncMove remain deferred; persisted schemas are disposable pre-ship. **2026-10-29 — security-review findings split into T124B/T124C/T124D and Phase N. T124 is done (shared path rule). T124B is done (`planning/132`, JSON store contract; T130 unblocked). T124C is done (`planning/133`): both repair paths stay, with different jobs — Steam creates a missing ACF, Epic modifies an existing `.item` and refuses to create one. T129 revised to match. The per-store decision tree is a required deliverable of T124A (the connection map). The Epic store client stays as a deferred opt-in metadata feature (T131), not a removal. T132 added: registry and filesystem call audit — reads take their answer from the operation already performed, writes make one state change; no probe-then-act. T125 may start immediately.**

## Active queue (ordered)

1. **T124A** — connection map at `META/CONNECTION_MAP.md` (Proposal B, adopted), including the per-store decision tree (intent → gate → locator → reader → writer → failure → test).
2. **T124D** — Epic online metadata client disposition: keep deferred, define the opt-in gate.
3. **T125** — release path cleanup (one workflow, high/critical scan gate, SHA-pinned actions, checksum; independent).
4. **T126** — reconcile the data contract and Readme access inventory (after T124D; before T117).
5. **T129** — guarded repair writes: Steam create-only, Epic modify-only (after T124C; before T117).
6. **T130** — safe local-state writes with visible failure (contract in `planning/132`; required before T121).
7. **T117** — launcher discovery + first-run opt-in (subtasks T117A–B).
8. **T118** — read-only store inventory import (subtasks T118A–C).
9. **T132** — registry and filesystem call audit: reads take their answer from the operation already performed, writes make one state change; no probe-then-act (after T129/T130; before T121).
10. **T121** — incremental store sync on every launch (subtasks T121A–C).
11. **T122** — Setup screen per-store controls (subtasks T122A–B).
12. **T119** — launcher-vs-disk health + suggestions (subtasks T119A–B).
13. **T127** — security contact + download verification docs (after T125).
14. **T128** — supply-chain hygiene (Dependabot, lock files, warnings-as-errors; phase boundary).
15. **Epic engine detection (P2)** — author its task doc after the store work.
16. **Remaining detector/docs backlog** — O3 is covered by T124D; O4/O5/O8/O10 dedup, deep 4-level walk, `DetectionLogger`, doc splits.

## Parallel UI track (P2)

- **T72** — view-mode state machine (Library / Stores / Engines), F9. Spec rewritten 2026-10-30.
- **T73** — Flat mode as the home view plus one-key scope to a store or anchor; depends on T72.

Neither blocks the store-capability chain. Both are specified and can start whenever the P1 path is idle.

## Deferred (do NOT start)

- **T131 — Epic online metadata opt-in gate** — deferred until online Epic metadata is scheduled. The client is kept, not deleted, and must not be called by default.
- **Epic `.item` re-creation / registration** — dead end; detection reads existing manifests only.
- **SyncMove Phase 2.1** — blocked until detection is working.
- **Plan 127 C** (`Extensions{}` / emulator) — needs a user decision.
- **SDK .NET 8→9** — deferred indefinitely.

## Standing rules

One file at a time; new files before removal; detection behavior-preserving; **persisted schemas are disposable pre-ship — no back-compat**; Windows paths opaque; full build + suite after each code step. Documentation-only sessions do not build/test. Do not reference task IDs in non-task documents.

## Not next (backlog)

Plan 101 / T72 mode switcher, 104 detect.py split, 97 themes, 111 logging toggle, 110 user tags, 91 blacklist editor, 05-phase-3 category browse, §8.3 Program Files (x86) layout plan, 90 SDK upgrade. EA format caveat remains blocked on a complete install.
