# META/SESSION/NEXT.md — Next Action

**Nature:** Scratch. **Overwritten** every session handoff.  
**Audience:** Builder. Read before implementing.  
**Updated:** 2026-08-22 — docs aligned with code; bugs 10/13/16 done.

---

## Active track: Post-MVP

**MVP:** ✅ COMPLETE — `planning/100-mvp-next-steps.md`  
**Phase 2 baseline:** ✅ COMPLETE (scan + launch). Leftover is SyncMove (2.1).  
**Tests:** 365 passing (73 Core + 1 Migration + 291 App). Detection.Tests empty.  
**Working tree:** uncommitted Plan 118 docs + Bugs 10/13/16 code. Commit before the next feature.

**Do not re-implement:** Bugs 10, 13a/13b, 16, 23–33, 8, 11/15, 14, 17–22.

---

## Do next (product)

### 1. Plan 117 — Left pane layout (recommended)

**Plan:** [`planning/117-left-pane-layout.md`](../../planning/117-left-pane-layout.md)  
**Why:** Direct follow-on to tags UI; roots lost path/badge after tag refactor.  
**Scope:** Badge column left, title + parenthetical tags/count, path right; no colored tag pills in left pane.

### 2. After 117 (or if reprioritized)

| Priority | Item | Plan |
|----------|------|------|
| P2 | Steam SyncMove repair | `planning/04-phase-2-syncmove.md` |
| P2 | Engine + PCGW metadata | `planning/102-tags-metadata-display.md` (Phases 2–3) |
| P2 | detect.py remaining edges | `planning/103-detect-py-port-status.md` |
| P3 | detect.py module split | `planning/104-detect-py-module-split.md` |
| P3 | Category browse / search | `planning/101-top-level-modes-and-filter.md` |
| Low | Command bar clickable | TECH_DEBT “UI Command Buttons Are Decorative” |
| Low | Library type ComboBox width | TECH_DEBT Bug 12 |
| Low | `tag_colors.json` embed/restore | same pattern as Bug 16 |

### Deferred

- Phase G T48–T57 test gaps  
- .NET 9 (`planning/90-sdk-upgrade.md`)

---

## Recently completed (context only)

- Bugs 10 / 13a / 13b / 16 + tests  
- Plan 118 — Doc audit  
- Plans 114–116 — detection bugfixes, registry fallback, BattleNet signals  
- Plan 113 — Async F5 scan  
- Plan 109 — Epic local manifests  
- Plan 108 — Steam status messages  
- Plan 106 — Unified setup  
- Plan 102 Phase 4 — Tag color badges  
- MVP T61–T77  

Full narrative: `META/COMPLETED/phase-post-mvp-sessions.md`
