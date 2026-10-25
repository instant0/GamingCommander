## Status

**2026-10-25 — audit follow-ups, title canonicalization, and the Steam location rule are complete.** Detection remains the priority. Plan 123's review gate is **CLOSED** (agent; human sign-off pending). Plan 127 A/B is complete. Epic `.item` re-creation and SyncMove remain deferred; persisted schemas are disposable pre-ship.

## Active queue (ordered)

1. **T124** — duplicate-capability review + consolidation plan (must precede the new store readers/locators).
2. **T124A** — generate/refresh the connection map (composition, data flows, extension recipes).
3. **T117** — launcher discovery + first-run opt-in (subtasks T117A–B).
4. **T118** — read-only store inventory import (subtasks T118A–C).
5. **T121** — incremental store sync on every launch (subtasks T121A–C).
6. **T122** — Setup screen per-store controls (subtasks T122A–B).
7. **T119** — launcher-vs-disk health + suggestions (subtasks T119A–B).
8. **Epic engine detection (P2)** — author its task doc after the store work.
9. **Remaining detector/docs backlog** — O3/O4/O5/O8/O10 dedup, deep 4-level walk, `DetectionLogger`, doc splits.

## Deferred (do NOT start)

- **Epic `.item` re-creation / registration** — dead end; detection reads existing manifests only.
- **SyncMove Phase 2.1** — blocked until detection is working.
- **T72/T73** — P2; not next.
- **Plan 127 C** (`Extensions{}` / emulator) — needs a user decision.
- **SDK .NET 8→9** — deferred indefinitely.

## Standing rules

One file at a time; new files before removal; detection behavior-preserving; **persisted schemas are disposable pre-ship — no back-compat**; Windows paths opaque; full build + suite after each code step. Documentation-only sessions do not build/test. Do not reference task IDs in non-task documents.

## Not next (backlog)

Plan 101 / T72 mode switcher, 104 detect.py split, 97 themes, 111 logging toggle, 110 user tags, 91 blacklist editor, 05-phase-3 category browse, §8.3 Program Files (x86) layout plan, 90 SDK upgrade. EA format caveat remains blocked on a complete install.
