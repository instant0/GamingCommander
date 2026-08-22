# META/SESSION/CURRENT.md — Current Project State

**Nature:** Scratch. **Overwritten** every session handoff.  
**Audience:** All agents. Read every session.  
**Updated:** 2026-08-22 — documentation aligned with actual code status.

---

## Phase

**Post-MVP.** MVP complete (`planning/100-mvp-next-steps.md`).  
Phase 2 **baseline** (scan + launch) is complete. Remaining Phase 2 work is **SyncMove (2.1)** only.

**365 tests passing** (73 Core + 1 Migration + 291 App). Detection.Tests has no tests. Build clean.

**Working tree:** uncommitted mix of Plan 118 documentation + Bugs 10/13/16 implementation. Commit before starting Plan 117.

---

## What is true

- Real scanning lives in `GamingCommander.App` (`LibraryManager` → `SteamLibraryScanner` / `FolderScanner`). `GamingCommander.Detection` is a deprecated stub. `GamingCommander.Migration` is a dry-run stub.
- Store **folder/registry detection** exists for GOG, Epic, EA, Ubisoft, Battle.net, Xbox, Rockstar. That is not full store-client support (Phase 3).
- Epic **local** `.item` / `.mancpn` enrichment shipped (Plan 109). Epic API deferred.
- Rescan is **F5** (async, cancellable). F6 is not rescan. Enter launches.
- Command bar buttons are decorative (`IsHitTestVisible=False`); keyboard works.
- Nested Steam under a mixed root is silently excluded from FolderScanner. Suggest-as-root UX is still IDEAS-only.

---

## Priority Roadmap

### Done (recent)

1. ✅ Docs aligned to code (ROADMAP, CODE_MAP, README, NEXT, planning/04-phase-2, ARCHITECTURE note)
2. ✅ Bugs 10 / 13a / 13b / 16 + tests
3. ✅ Plan 118 — Doc audit
4. ✅ Plans 114–116 — B23–B33, registry fallback, BattleNet signal-file-only
5. ✅ Plan 102 Phase 4 — Tags display
6. ✅ Plan 113 — Async F5
7. ✅ Plan 109 — Epic local manifests
8. ✅ Plan 108 / 106 — Steam status copy, unified setup

### Next (product)

1. **P2 — Plan 117** — Left pane layout
2. **P2 — Steam SyncMove** — `planning/04-phase-2-syncmove.md`
3. **P2 — Plan 102 Phases 2–3** — Engine detection + PCGW
4. **P2 — Plan 103** — Remaining detect.py edges
5. **P3 — Plan 104 / 101** — detect.py split; category browse

### Deferred

- Phase G T48–T57 — no Linux-only special tests
- .NET 9 — indefinitely

Historical session detail: `META/COMPLETED/phase-post-mvp-sessions.md`

---

## Open issues (truthful TECH_DEBT)

- UI command buttons decorative (`IsHitTestVisible=False`)
- Bug 12 — Library type ComboBox width
- EA format research caveat

Bugs 10, 13a/13b, 16, 23–33, 8, 11/15, 17–19, 14, 20–22: **fixed**.

Left-pane tag/badge residual → **Plan 117**.  
Nested Steam add-as-root suggestion → `META/BACKLOG/IDEAS.md`.

---

## Test coverage gaps (known, deferred)

- `StoreSignalDetector` — limited dedicated suite
- `LibraryManager` — partial
- `GameSourceParser` / `JsonConfigService` — partial

---

## Key architecture decisions

- **Tag colors user-configurable** — `data/tag_colors.json`; `TagColorService` → `ITagColorProvider`.
- **Theme in App.axaml** — semantic brushes/fonts; `AppTheme` for code-behind.
- **Steam statuses:** Installed / Moved / Orphaned / Missing.
- **Detection chain:** Pass 1 store signals → Pass 1c registry fallback → Pass 2 fallback signals → Pass 3 container. BattleNet = signal files only.
- **ILauncher retired** — `LibraryManager` → `FolderScanner` / `SteamLibraryScanner`.
- **SyncMove philosophy:** user moves files; app repairs manifests.

---

## Docs index

- Plans: `planning/README.md`
- Detection: `docs/GAME-DETECTION-LOGIC.md`
- Epic: `docs/EPIC-MANIFEST-ENRICHMENT.md`
- Archived bootstrap docs: `META/COMPLETED/archived-docs/`

---

**Next session: Read `META/SESSION/NEXT.md` before starting.**
