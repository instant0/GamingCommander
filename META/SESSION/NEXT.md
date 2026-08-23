# META/SESSION/NEXT.md — Next Action

**Nature:** Scratch. **Overwritten** every session handoff.  
**Audience:** Builder. Read before implementing.  
**Updated:** 2026-08-23

---

## Status

Plan 122 (type-to-search) implemented and marked COMPLETE: `ShellViewModel` search buffer + live `LeftPaneTitle`, `MainWindow.OnKeyDown` printable routing (bare `S`/`T` freed; F8/F4 unchanged), help dialog updated, 7 new tests in `App.Tests/ShellViewModelSearchTests.cs` all passing. Build clean.

Known: 6 pre-existing test failures in App.Tests (ScannerFilter / MockDataIntegration / ExecutableScoring) — confirmed failing on a clean tree before this work; unrelated to Plan 122. Candidate TECH_DEBT entry if user wants it tracked.

## Next task

User live-smoke-test on Windows (type "assa" with several libraries; verify cross-root matches, Backspace/Esc, Enter launch from results). No code task queued.

## Candidate next work (user decides)

- SyncMove (Phase 2.1) — top product item per `planning/README.md`.
- Plan 111 F2 "Enable startup logging" toggle (PLANNED; not in code).
- Investigate the 6 pre-existing scanner/scoring test failures (see above).

## Standing hard rule

Documented format/research wins. Ask before changing a documented field source or `.item` shape. Verify with `tools/decode_manifest.py` before more C# Epic writer changes.
