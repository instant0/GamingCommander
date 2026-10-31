## Status

**2026-10-25 — audit follow-ups, title canonicalization, and the Steam location rule are complete.** Detection remains the priority. Plan 123's review gate is **CLOSED** (agent; human sign-off pending). Plan 127 A/B is complete. Epic `.item` re-creation and registration repair remain deferred; persisted schemas are disposable pre-ship. **2026-10-29 — security-review findings split into T124B/T124C/T124D and Phase N. T124 is done (shared path rule). T124B is done (`planning/132`). T124C is done (`planning/133`): both repair paths stay, with different jobs. T124D/T126/T133 and the store chain remained. 2026-10-31 — T130 (safe JSON writes), T129 (guarded repair writes), and T132 (I/O call audit) are done. T124D was closed and merged into the contract pass (T126), which is done. T133 (install identity by path) is done. T135 (remove app-version back-compat) is done. T134 (test suite reduction) is done: 773 → 499 cases. T117 (launcher discovery service) is done: 505 cases. T117A1 (Steam + Epic locators) is done: 515 cases. T136 (unknown store data sources) is done: EA/Battle.net detection-only, Ubisoft registry locator + reader, Arc unsupported, Rockstar verified. T117A2 (GOG locator) is done. T117A3 (Ubisoft registry locator) is done. T117B2 (launcher preview + policy wiring) is done: 528 cases. T137 (launcher discovery setup choice) is done: 557 cases. T117B1 (Rockstar + Xbox locators) is done: 562 cases. Removed the obsolete move/symlink Migration project, models, and test: 561 cases (Core 157, App 404). Added T138 (completed removal record) and rewrote T80 as the executable registration-repair task: copy/update the store record, never delete, and report a stray old ACF with manual removal steps. Reordered the active queue: readers → health → repair → sync → setup → release. T118A (Steam + GOG inventory readers) is done: 572 cases (Core 157, App 415). T118B (Epic inventory reader) is done: 580 cases (Core 157, App 423). T118C (Rockstar inventory reader) is done: 587 cases (Core 157, App 430). Next T118C2 (Ubisoft registry inventory reader).**

## Active queue (ordered)

**This file is the canonical execution order.** Phase `STATUS.md` files hold status and per-phase order;
if they disagree with this list about what runs next, this list wins.

1. **T118C2** — Ubisoft registry inventory reader. Completes the readable-store matrix.
2. **T119A** — launcher-vs-disk health model + per-store probes (read-only).
3. **T119B** — health status/suggestion UI.
4. **T80** — registration repair after user-initiated file moves: guarded copy/update, no delete, stray-ACF advisory.
5. **T121A** — inventory diff and reconciliation.
6. **T121B** — startup sync orchestrator and time budget.
7. **T121C** — store sync integration tests.
8. **T122A** — Setup store rows and binding.
9. **T122B** — Test detection and Scan now actions.
10. **T125** — release path cleanup.
11. **T127** — security contact + download verification docs.
12. **T128** — supply-chain hygiene (Dependabot, lock files, warnings-as-errors; phase boundary).
13. **Epic engine detection (P2)** — author its task doc after the store work.
14. **Remaining detector/docs backlog** — O4/O5/O8/O10 dedup, deep 4-level walk, `DetectionLogger`, doc splits.

**Why this order.** The reader tasks are independent and can run in parallel; they come first because
health, setup scan-now, and sync all consume the same inventory. Health comes before repair so the
mismatch model is defined once and T80 consumes it instead of duplicating detection. Repair comes before
sync because it is the explicit user fix and only needs detection. Sync then automates the same data,
and Setup controls follow with the finished import/diff services. Release work is last because it does
not advance product capability.

T136 decided: EA and Battle.net are detection-only, Ubisoft uses the registry inventory, Arc is not a store,
Rockstar is locator + registry reader (Settings.json deferred). The cancelled EA/Battle.net tasks must not be
recreated. T118A/B/C/C2 are independent of each other. T137 (the three-choice launcher discovery setting
and the FileOnly scan gate) is done and precedes T118.

## Parallel UI track (P2)

- **T72** — view-mode state machine (Library / Stores / Engines), F9. Spec rewritten 2026-10-30.
- **T73** — Flat mode as the home view plus one-key scope to a store or anchor; depends on T72.

Neither blocks the store-capability chain. Both are specified and can start whenever the P1 path is idle.

## Deferred (do NOT start)

- **T131 — Epic online metadata opt-in gate** — deferred until online Epic metadata is scheduled. The client is kept, not deleted, and must not be called by default.
- **Epic `.item` re-creation / registration** — dead end; detection reads existing manifests only.
- **Plan 127 C** (`Extensions{}` / emulator) — needs a user decision.
- **SDK .NET 8→9** — deferred indefinitely.

## Standing rules

One file at a time; new files before removal; detection behavior-preserving; **persisted schemas are disposable pre-ship — no back-compat**; Windows paths opaque; full build + suite after each code step. Documentation-only sessions do not build/test. Do not reference task IDs in non-task documents.

## Not next (backlog)

Plan 101 / T72 mode switcher, 104 detect.py split, 97 themes, 111 logging toggle, 110 user tags, 91 blacklist editor, 05-phase-3 category browse, §8.3 Program Files (x86) layout plan, 90 SDK upgrade. EA format caveat remains blocked on a complete install.
