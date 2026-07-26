# META/SESSION/NEXT.md — Next Action

**Nature:** Scratch. **Overwritten** every session handoff.
**Audience:** Builder. Read before implementing.

---

## ACTIVE TRACK: MVP — ✅ COMPLETE

**Canonical plan:** [`planning/100-mvp-next-steps.md`](../../planning/100-mvp-next-steps.md)
**Task tracker:** [`META/TASKS/phase-h-mvp/STATUS.md`](../../META/TASKS/phase-h-mvp/STATUS.md)

| WP | Task | Priority |
|----|------|----------|
| **WP-1** | **Fix launch pipeline** — T61 ✅, T62 ✅, T63 cancelled | **P0 COMPLETE** |
| **WP-2** | **First-run defaults** — T64 ✅ | **P0 COMPLETE** |
| **WP-3** | **C# detection parity** — T65 ✅, T66 ✅, T67 ✅, T68 ✅, T68C ✅ | **P0 COMPLETE** |
| WP-4 | Launch UX polish — T69 ✅, T71 ✅ | **P1 COMPLETE** |
| WP-5 | Windows smoke gate — T70 ✅, T75 ✅, T76 ✅, T77 ✅ | **P0+P1 COMPLETE** |

**219 tests passing. Build clean.**

**Post-MVP backlog (ordered by priority):**

1. **P0 — Fix Battle.net detection** — blizzard in noise filter blocks detection → see `planning/107-battle-net-detection-fix.md`
2. **P1 — Unify setup screens** (Wizard + F2 merge) → see `planning/106-unified-setup-screen.md`
3. **P2 — Steam status messages** — actionable guidance for Orphaned/Missing/Moved → see `planning/108-steam-status-messages.md`
4. Phase G T48–T57 (tests/quality polish) — harden what shipped
5. Steam SyncMove repair (backup + ACF path fix) — from `planning/04-phase-2-syncmove.md`
6. PCGamingWiki metadata + Tags system — see `planning/102-tags-metadata-display.md`
7. Port remaining detect.py edges — see `planning/103-detect-py-port-status.md`
8. Split detect.py into modules — see `planning/104-detect-py-module-split.md`
9. Category browse / search (F8, S key) — see `planning/101-top-level-modes-and-filter.md`

---

## Recently completed (context only)

- Plan 105 (F6 Crash Fix + F5 Rescan) ✅ COMPLETE — 5 crash fixes, F6→F5 rebinding, 2 new tests, 219 tests passing
- Phases D–G partial: complexity reduction, stabilization, 99→217 tests, theme extraction, VFS display, detect.py unified, detection hardening (Python)
- T58–T60 naming/docs/noise consolidation done
- T61+T62: Launch pipeline fixed — Steam URI resolution, args passthrough, status bar with args
- T63: Cancelled (trivial logic, not worth Avalonia test infrastructure)
- T64: First-run config defaults fixed — `IsFirstRun` now true when settings.json missing; 3 new tests
- T65: GOG .info parser — title/exe/args extraction, DLC filtering; 10 new tests
- T66: UE-aware exe discovery — 4 platforms, child/bin, recursive fallback; 15 new tests
- T67: .lnk shortcut resolution — latin-1 byte parse, backup rename handling; 13 new tests
- T68: Container recursion & organization detection — UE3 fast path, all UE platforms, org folder detection; 13 new tests
- T68C: Detection robustness & module organization — extracted FallbackSignalDetector + ContainerScanner from FolderScanner; 52 new tests
- T69: Launch UX polish — F4/F9 help text, InteractionHint updated
- T70/T75: Windows smoke gate — 11/12 bugs fixed (BUG-11 deferred to ExeCandidateSelector)
- T76: Library root nesting prevention — IsChildOf check, 8 tests
- T77: Removed F7 (Add Root) — F2 subsumes it entirely
- T71: Removed F5 launch keybind — Enter handles launch
- Phase G T48–T57 still pending — **not MVP blockers**
- MVP declared **COMPLETE** — 217 tests, all gate criteria satisfied
- .NET 9 upgrade deferred indefinitely — not planned
- Plan 102 (Tags + Metadata Display) drafted — 4 phases: user tags, engine detection, metadata scraping, display
- Plan 103 (detect.py Port Status) documented — ~75% parity, gaps identified
- Plan 104 (detect.py Module Split) drafted — 8 modules, ~3.5 hours
- Plan 105 (F6 Crash Fix + F5 Rescan) ✅ — crash fixes, keybind rebinding, 2 new tests
- Plan 106 (Unified Setup Screen) drafted — merge Wizard + F2 into single LibrarySetupWindow, ~4-6 hours
- Plan 107 (BattleNet Detection Fix) drafted — noise filter cleanup, name-based fallback, ~2-3 hours
- Plan 108 (Steam Status Messages) drafted — actionable guidance for Orphaned/Missing/Moved, ~2-3 hours
- TECH_DEBT updated: Bugs 8-16 added (F6 crash, Battle.net detection, Steam Controller Config, noise gaps, duplicate setup screens, Orphaned semantics, blacklist.json placement)
- IDEAS updated: user testing feedback documented
- Detection docs updated: BattleNet skip-list regression documented in GAME-DETECTION-LOGIC.md, Plan 103, Plan 107
- Key finding: C# has RICHER BattleNet detection than Python (parent propagation, name heuristics, exe heuristics) but `"blizzard"` in skip lists blocks it entirely — this is a 10-line regression fix, not a feature gap
