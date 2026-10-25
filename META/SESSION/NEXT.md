## Status

**Pending-task audit complete (2026-10-14).** Plan 124 + 125 COMPLETE. Plan 126 EOSH investigation **CLOSED** — Epic `.item` re-creation is a **dead end and deferred**. **Detection is the priority**, using existing manifest files (read-only).

## Next (directive 2026-10-14: DETECTION FIRST)

1. **Plan 123 — detection closeout (essentially done).** E6a ✅, ScanDepth ✅, confidence-tier marking ✅ (Plan 127 A3). **Remaining (process only):** run the amended review gate over the final Python scanner; refresh `planning/103` notes.
2. **Plan 127 — persistence v2: A + B COMPLETE** (`planning/127-persistence-v2.md`). Done: A1–A6 and B7–B13. Remaining: **C** (`Extensions{}` / emulator/ROM model) — deferred; and follow-ups (launch-profile picker UI, `Relations.Kind` scanner wiring, collections UI, per-tag colour, play-time tracking).
   - With A/B done, the next product priorities are: **Plan 123 amended review gate** (process) and, once detection is confirmed working, **SyncMove Phase 2.1** (currently deferred).
3. **Detection from manifest files (read-only)** — ensure Epic/Steam/GOG detection identifies games from existing `.item`/`.mancpn`/`.manifest`/ACF files. No manifest writing.

## Deferred (do NOT start)

- **Epic `.item` re-creation / registration (T78)** — could not be made to work with Epic; writer work deferred. `docs/research/epic_item_format.md` generation recipes are reference-only.
- **SyncMove Phase 2.1 (T80)** — blocked until detection is working.
- Optional RE tail (`126-facts-eosh-re.md`: global `0x14316fed0` class, `vtable[0x48]` semantics) — archival/non-blocking.

## Standing rules

One file at a time; new files before removal; detection behavior-preserving; **persisted schemas are disposable pre-ship — no back-compat, so persisted names/shapes may be renamed or restructured freely (no migration, no dual-write)**; Windows paths opaque; full build + suite after each code step. Documentation-only sessions do not build/test.

## Not next (backlog)

Plan 101 / T72 mode switcher, 104 detect.py split, 97 themes, 111 logging toggle, 110 user tags, 91 blacklist editor, 05-phase-3 category browse, §8.3 Program Files (x86) layout plan, 90 SDK upgrade (deferred). Open TECH_DEBT: Bug 12 (ComboBox width), EA format caveat.
