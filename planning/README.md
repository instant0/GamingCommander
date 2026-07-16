# Planning Documents

This folder contains implementation plans following the FILE NAMING CONTRACT.

**Project memory, session state, roadmap, and architecture have moved to `META/`.**

See `META/SESSION/CURRENT.md` for current project state.
See `META/SESSION/NEXT.md` for the next concrete action.
See `META/ROADMAP.md` for the high-level project roadmap.
See `META/ARCHITECTURE.md` for stable architectural decisions.
See `META/BACKLOG/TECH_DEBT.md` for known issues.

---

## Active Plans

| Document | Description | Status |
| :--- | :--- | :--- |
| `04-phase-2.md` | Steam & Stand-alone game implementation | ACTIVE |
| `04-phase-2-syncmove.md` | SyncMove — Manifest-aware game migration | PLANNED |
| `04-phase-2-metadata-lookup.md` | Game Metadata Lookup (F4) | PLANNED |
| `05-phase-3.md` | Multi-Launcher integration strategy | FUTURE |
| `05-phase-3-category-browse.md` | KODI-style category browsing & quick search | PLANNED |
| `06-phase-4.md` | Advanced features and polishing | FUTURE |
| `90-sdk-upgrade.md` | .NET 8 → .NET 9 SDK upgrade | PLANNED |
| `94-game-detection-overhaul.md` | Comprehensive game detection rewrite | PLANNING |

## Completed / Archived

| Document | Description |
| :--- | :--- |
| `00-overview.md` | Historical roadmap/status — superseded by `META/ROADMAP.md` |
| `01-phase-0.md` | Phase 0 complete |
| `02-phase-1.md` | Phase 1.0 complete |
| `03-phase-1-ui-polish.md` | Phase 1.1 complete |
| `03-phase-1-research.md` | Phase 1.2 complete |
| `99-stabilization.md` | Phase 1.1a complete |

## Naming Convention

```
00-overview.md             — Project overview
01-phase-0.md              — Foundations
02-phase-1.md              — Core UI & Infrastructure
03-phase-1-<feature>.md    — Phase 1 features (ui-polish, research)
04-phase-2.md              — Steam & Standalone Games
04-phase-2-<feature>.md    — Phase 2 features (syncmove, metadata-lookup)
05-phase-3.md              — Multi-Launcher Support
05-phase-3-<feature>.md    — Phase 3 features (category-browse)
06-phase-4.md              — Advanced Features & Polish
90-sdk-upgrade.md          — SDK/toolchain upgrades
99-stabilization.md        — Bug fixes and stabilization
```
