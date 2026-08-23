# META/SESSION/CURRENT.md — Current Project State

**Nature:** Scratch. **Overwritten** every session handoff.  
**Audience:** All agents. Read every session.  
**Updated:** 2026-08-23 — Plan 122 shipped; test-suite drift repaired.

---

## This session

1. Implemented Plan 122 (type-to-search) — see `planning/122-type-to-search.md` (COMPLETE). Live query in left pane header, 3-char threshold, cross-root wildcard filter, S/T freed.
2. Repaired pre-existing test-suite failures (not caused by Plan 122):
   - `ScannerFilterTests` + `MockDataIntegrationTests` pointed at moved fixture (`data/mock` → `testdata/mock`, commit bc1fdb8). Path fixed; 5 tests recovered.
   - `ScoreExecutable_Win64Binary_AddsBonus`: scoring drift — root exact-name exe tied/beat platform-suffixed binaries. Fixed per ADR-012: +40 exact-match bonus also granted when name key equals folder key after stripping `win64`/`win32`/`wingdk`/`shipping`. `TitleText.MatchesFolderAndExe` unchanged (identity/search only).
3. Full suite green: 539 tests, 0 failures.

---

**Next session: Read `META/SESSION/NEXT.md`.**
