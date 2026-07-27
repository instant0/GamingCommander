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

**358 tests passing. Build clean.**

**Post-MVP backlog (ordered by priority):**

1. **P1 — Detection Bug Fixes from Live Testing** — Plan 114: ✅ **COMPLETE** — all 11 bugs fixed (B23-B33). See `planning/114-detection-bugfixes.md`
2. **P2 — EA/Ubisoft Registry Fallback** — Plan 115: ✅ **COMPLETE** — `IRegistryReader` + `RegistryFallbackDetector` + Pass 1c. EA/Ubisoft/GOG/Rockstar per-game registry keys. 353 tests. See `planning/115-ea-ubisoft-registry-fallback.md`
3. **P2 — BattleNet Signal-File Detection** — Plan 116: ✅ **COMPLETE** — Removed path-based "blizzard" checks from ContainerScanner and FolderScanner. Detection based on signal files only. 358 tests. See `planning/116-battlenet-detection-enhancement.md`
4. **P2 — Phase G Quality (T48–T57)** — Test coverage gaps for StoreSignalDetector, LibraryManager, GameSourceParser, JsonConfigService
5. **P2 — Tags Display in UI** — Render Tags field in left lister and details pane (Bug 33)
6. **P2 — Steam SyncMove Repair** — Backup + ACF path fix (from `planning/04-phase-2-syncmove.md`)
7. **P2 — PCGamingWiki Metadata + Tags System (Phase 3)** — See `planning/102-tags-metadata-display.md`
8. **P2 — Port Remaining detect.py Edges** — See `planning/103-detect-py-port-status.md`
9. **P3 — detect.py Module Split** — See `planning/104-detect-py-module-split.md`
10. **P3 — Category Browse / Search (F8, S key)** — See `planning/101-top-level-modes-and-filter.md`

---

## Recently completed (context only)

- **Plan 116 (BattleNet Signal-File Detection) ✅ COMPLETE** — Removed path-based "blizzard" checks from ContainerScanner and FolderScanner. Detection based on signal files only. 358 tests passing.
- **Plan 108 Status Message Refinement ✅ COMPLETE** — Removed "delete ACF" guidance, added ACF re-linking future work references.
- **Documentation Review — P2 Task Analysis ✅ COMPLETE** — Created Plan 109 (Epic Manifest Enrichment), Plan 110 (User Tags + Override Protection), updated Plan 108.
- **Plan 106 (Unified Setup Screen) ✅ COMPLETE** — Wizard + F2 merged into single `LibrarySetupWindow`; 4 Wizard files deleted; `App.axaml.cs` auto-opens with `isFirstRun` flag; 227 tests passing.
- **Plan 108 (Steam Status Messages) ✅ COMPLETE** — Orphaned/Missing/Moved detail messages now explain what each state means, why it happened, and how to fix it. Added `FolderName`, `LibraryRoot`, `AcfExpectedPath` to PlatformMetadata. 227 tests passing.
- **Battle.net Detection Fix (P0) ✅ COMPLETE** — Removed `"blizzard"` and `"battle.net"` from `NoiseSubDirNames` and `s_nonGameFolderNames`. Build clean, 219 tests passing.
- **PE Metadata Analysis ✅ COMPLETE** — Analyzed 276 executables with PE metadata. Key findings: InternalName more reliable than Description, 93% divergence, file size thresholds. Updated docs and planning.
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
