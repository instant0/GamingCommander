# META/ROADMAP.md — Project Roadmap

**Nature:** Reference. Updated by Planner after milestones.
**Audience:** All agents. Read when determining what to work on next.

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
| **Phase 2: Steam & Standalone Games** | **🔵 ACTIVE** | Steam integration, standalone detection, migration, launching |
| Phase 2.1: SyncMove Migration | ⏳ PLANNED | Manifest-aware game relocation for Steam+Epic |
| Phase 2.2: Game Metadata Lookup | ⏳ PLANNED | F4 metadata lookup (PCGW, SteamDB, IGDB), local game DB |
| Phase 3: Multi-Launcher Support | 🔮 FUTURE | GOG, Epic, EA App, Ubisoft Connect integration |
| Phase 3.5: Category Browsing & Search | 🔮 FUTURE | F8 category view, S key quick search, cross-root aggregation |
| Phase 4: Advanced Features & Polish | 🔮 FUTURE | PCGamingWiki integration, metadata sync, UX polish |

---

## Active Phase Detail: Phase 2 — Steam & Standalone Games

**Goal:** Implement the baseline game management features.

**Tasks:**
- [x] Steam Integration — library detection (`libraryfolders.vdf`), manifest parsing (`appmanifest_*.acf`), ACF cross-referencing, orphan/moved detection
- [x] Stand-alone Detection — generic signal-based scanning for GOG, EA, Ubisoft, Epic, Blizzard, Xbox, Rockstar, Steam Emu; deep executable discovery + scoring; container detection; 320 noise exe patterns
- [ ] Migration (Steam) — manifest repair for user-relocated games, Steam manifest patching
- [x] Launcher Logic — game launching via `steam://rungameid/{appid}` for Steam, direct `.exe` for others

**Acceptance:**
- ✅ Steam games appear in the UI (Installed/Moved/Orphaned status tracked)
- ✅ Stand-alone games detected via signal-based folder scan for all major platforms
- [ ] Steam games can be migrated safely (planned as Phase 2.1)
- [ ] Epic .item manifest support for metadata enrichment (planned as Phase 3)

---

## Milestone History

| Date | Milestone | Summary |
|------|-----------|---------|
| 2025-Q1 | Phase 0 | Avalonia 11.x chosen, solution scaffolded, Windows validation pass |
| 2025-Q2 | Phase 1.0 | Dual-pane UI, config engine, IGame/ILibraryManager interfaces (ILauncher later retired) |
| 2025-Q3 | Phase 1.1 | First-run wizard, virtual FS navigation, F2/T keys, details panel enhanced |
| 2025-Q4 | Phase 1.1a | Navigation/mouse fixes, 17 tests, mock data, Python validation tools |
| 2026-Q1 | Phase 1.2 | Research docs for Steam ACF/VDF, Epic, standalone, GOG, EA, Ubisoft |
| 2026-Q2 | Phases D+E | Code quality: complexity reduction (T16–T29), stabilization + test coverage (T31–T40): 99 tests |
| 2026-Q2 | **MVP (Phase H)** | Launch pipeline fixed, GOG/UE/lnk/container detection, 217 tests, Windows smoke gate passed |
