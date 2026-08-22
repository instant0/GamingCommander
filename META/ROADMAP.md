# META/ROADMAP.md — Project Roadmap

**Nature:** Reference. Updated by Planner after milestones.  
**Audience:** All agents. Read when determining what to work on next.  
**Updated:** 2026-08-22 — Plans 119/120 metadata sidecar shipped.

---

## Project Vision

GamingCommander is a C# Windows-native game management and launcher application with a Norton Commander-inspired dual-pane UI. It discovers installed games, collects technical metadata, launches games safely, and supports migration between locations across multiple game platforms.

---

## Phase Summary

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 0: Foundations & Environment | ✅ COMPLETE | Project scaffold, UI framework selection, solution structure |
| Phase 1.0: Core UI & Infrastructure | ✅ COMPLETE | Dual-pane UI, configuration engine, core abstractions |
| Phase 1.1: UI Polish & UX | ✅ COMPLETE | First-run wizard, virtual FS, F2/T key workflows, details panel |
| Phase 1.1a: Stabilization | ✅ COMPLETE | Navigation/mouse fixes, mock data, scanner tests, Python tools |
| Phase 1.2: Research & Data Collection | ✅ COMPLETE | Format research for Steam ACF, Epic, standalone; Python validation |
| **Phase 2: Steam & Standalone Games** | **✅ BASELINE COMPLETE** | Scan + launch shipped. Leftover: SyncMove (2.1) |
| Phase 2.1: SyncMove Migration | ⏳ PLANNED | Manifest repair after user-relocated files (Steam first) |
| Phase 2.2: Game Metadata Lookup | ✅ SHIPPED (narrowed) | Sidecar extras via PCGW + Steam Store. No SteamDB / IGDB / Cargo. |
| Phase 3: Multi-Launcher Support | 🔵 DETECTION SHIPPED / CLIENTS FUTURE | Folder/registry detection for GOG, Epic, EA, Ubisoft, Battle.net, Rockstar exists. Full store APIs / repair do not. |
| Phase 3.5: Category Browsing & Search | 🔮 FUTURE | F8 category view, S key quick search, cross-root aggregation |
| Phase 4: Advanced Features & Polish | 🔮 FUTURE | PCGamingWiki integration, metadata sync, UX polish |

**Post-MVP product track (current):** Plans 117 + 119 + 120 shipped. Next product plan: SyncMove (2.1). F8/S category browse remains future.

---

## Phase 2 — Steam & Standalone (baseline)

**Goal:** Baseline game management: discover, list, launch.

**Tasks:**
- [x] Steam Integration — `libraryfolders.vdf`, `appmanifest_*.acf`, ACF cross-ref, Installed/Moved/Orphaned/Missing; Steam-internal `common/` folders skipped (Bug 10)
- [x] Stand-alone Detection — signal scan for GOG, EA, Ubisoft, Epic, Blizzard, Xbox, Rockstar, Steam Emu; deep exe discovery + scoring; container detection; nested Steam trees excluded from FolderScanner (Bug 13b)
- [ ] Migration (Steam) — manifest repair for user-relocated games → **Phase 2.1**
- [x] Launcher Logic — `steam://rungameid/{appid}` for Steam, direct `.exe` for others

**Acceptance:**
- ✅ Steam games appear in the UI (Installed/Moved/Orphaned/Missing)
- ✅ Stand-alone games detected via signal-based folder scan
- [ ] Steam games can be migrated safely (Phase 2.1 SyncMove)
- ✅ Epic local `.item` / `.mancpn` enrichment (Plan 109 — not Phase 3)

---

## What is shipped vs not

| Shipped | Not shipped |
|---------|-------------|
| Dual-pane VFS over `data/games.json` | SyncMove manifest repair |
| F2 / first-run `LibrarySetupWindow` | SteamDB / IGDB / Cargo / Epic GraphQL |
| F5 async rescan + cancel | Category browse / search (F8, S) |
| Steam ACF + multi-library | Full GOG/Epic/EA/Ubisoft *clients* |
| FolderScanner + registry fallback | Nested Steam “add as root” UX |
| Epic local manifests + global cross-ref | Cover-art image UI |
| Right-pane colored tags + Plan 117 left pane | `tag_colors.json` embed/restore |
| Embedded `blacklist.json` restore (Bug 16) | Writing extras into `GameEntry.Tags` |
| Sidecar extras (`games_metadata.json`, flag off) | Auto-applying PCGW launch args |
| F3 lookup + F4 arg catalog; Steam URI launch | Inverting Steam → raw exe |

---

## Milestone History

| Date | Milestone | Summary |
|------|-----------|---------|
| 2025-Q1 | Phase 0 | Avalonia 11.x chosen, solution scaffolded, Windows validation pass |
| 2025-Q2 | Phase 1.0 | Dual-pane UI, config engine, IGame/ILibraryManager (ILauncher later retired) |
| 2025-Q3 | Phase 1.1 | First-run wizard, virtual FS navigation, F2/T keys, details panel |
| 2025-Q4 | Phase 1.1a | Navigation/mouse fixes, mock data, Python validation tools |
| 2026-Q1 | Phase 1.2 | Research docs for Steam ACF/VDF, Epic, standalone, GOG, EA, Ubisoft |
| 2026-Q2 | Phases D+E | Complexity reduction + test coverage |
| 2026-Q2 | **MVP (Phase H)** | Launch pipeline, GOG/UE/lnk/container detection, Windows smoke gate |
| 2026-07 | Post-MVP 105–113 | F5 rescan, unified setup, Steam status copy, async scan, tags Phase 4 |
| 2026-07 | Post-MVP 109/114–116 | Epic manifests, detection bugfixes B23–B33, registry fallback, BattleNet signals |
| 2026-08 | Plan 118 + Bugs 10/13/16 | Doc audit; Steam internals skip; nested Steam exclusion; embedded blacklist |
| 2026-08 | Plans 119 + 120 | Sidecar extras; PCGW details; F3 picker; no SteamDB |
