# META/SESSION/CURRENT.md — Current Project State

**Nature:** Scratch. **Overwritten** every session handoff.  
**Audience:** All agents. Read every session.  
**Updated:** 2026-08-23 — Plan 122 type-to-search implemented.

---

## This session

Wrote then implemented `planning/122-type-to-search.md` (marked COMPLETE). Silent keyboard capture: unmodified printable keys build a query; at 3+ chars a cross-root wildcard filter (names + folder + store label + tags) applies live via existing `GameFilterMatcher`/`ApplyFilter`. Typed text shows in the left pane header (`Search: '…'`). Backspace erases (below threshold → roots); Esc cancels; arrows/Enter work on results while the buffer persists. Bare `S` and `T` bindings removed (F8/F4 unchanged); F8 dialog and tag-badge clicks end a typed session. Files: `ShellViewModel.cs`, `MainWindow.axaml.cs`, `HelpDialogBuilder.cs`, new `ShellViewModelSearchTests.cs` (7 tests, passing). Build clean. Note: 6 pre-existing App.Tests failures (scanner/scoring) verified failing on clean tree — unrelated.

---

**Next session: Read `META/SESSION/NEXT.md`.**
