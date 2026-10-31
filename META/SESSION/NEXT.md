## Status

**2026-10-25 — audit follow-ups, title canonicalization, and the Steam location rule are complete.** Detection remains the priority. Plan 123's review gate is **CLOSED** (agent; human sign-off pending). Plan 127 A/B is complete. Epic `.item` re-creation and SyncMove remain deferred; persisted schemas are disposable pre-ship. **2026-10-29 — security-review findings split into T124B/T124C/T124D and Phase N. T124 is done (shared path rule). T124B is done (`planning/132`). T124C is done (`planning/133`): both repair paths stay, with different jobs. T124D/T126/T133 and the store chain remain. 2026-10-31 — T130 (safe JSON writes), T129 (guarded repair writes), and T132 (I/O call audit) are done: 755 tests, build 0 errors/0 warnings, 23 redundant filesystem probes removed. T130 and T129 unblock the import/sync chain.**

## Active queue (ordered)

**This file is the canonical execution order.** Phase `STATUS.md` files hold status and per-phase order;
if they disagree with this list about what runs next, this list wins.

1. **T124D** — Epic store lookup client disposition: identity only (two uses: identify, pre-PCGW title), gate, docs, test definitions.
2. **T126** — reconcile the data contract, Readme access inventory, and the stale capability-matrix wording (after T124D).
3. **T133** — Epic claimed-folder identity: path-only claims, no folder-name matching (before T118B).
4. **T117** — launcher discovery + first-run opt-in (subtasks T117A–B); T117B wires `StorePolicyProvider`.
5. **T118** — read-only store inventory import (subtasks T118A–C; only Steam/Epic have usable inventories).
6. **T119** — launcher-vs-disk health + suggestions (subtasks T119A–B; after T118, before T121).
7. **T121** — incremental store sync on every launch (subtasks T121A–C; requires T118, T119).
8. **T122** — Setup screen per-store controls (subtasks T122A–B; requires T117, T118).
9. **T125** — release path cleanup (independent; may run at any point).
10. **T127** — security contact + download verification docs (after T125).
11. **T128** — supply-chain hygiene (Dependabot, lock files, warnings-as-errors; phase boundary).
12. **Epic engine detection (P2)** — author its task doc after the store work.
13. **Remaining detector/docs backlog** — O3 is covered by T124D; O4/O5/O8/O10 dedup, deep 4-level walk, `DetectionLogger`, doc splits.

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
