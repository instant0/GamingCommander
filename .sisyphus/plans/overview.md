# Project: GamingCommander

A C# Windows native UI application that replicates the look and feel of a Norton Commander-style console application, acting as a game management and launcher suite.

## Core Vision
- **UI**: Text-mode/Console UI inspired by Norton Commander.
- **Functionality**: Manage and launch games from multiple platforms (Steam, GOG, Epic, EA, Ubisoft, stand-alone).
- **Migration**: Safe migration of game folders, including symbolic linking and manifest updates.
- **Intelligence**: Gather metadata to detect games and provide deep info (registry paths, save locations, PCGamingWiki links).
- **Extensibility**: Sync metadata via online repositories (e.g., GitHub).

---

## Roadmap

### Phase 0: Foundations & Environment
- Setup project architecture (WinForms/WPF/Avalonia + Terminal UI library).
- Implement basic file system and registry access layers.
- Research game detection signatures (based on existing launchers/GameCollector).
- **Status: COMPLETE** — Avalonia 11.x chosen, Windows validation passed, solution scaffolded.

### Phase 1.0: Core UI & Infrastructure
- Implement the "Norton Commander" dual-pane UI (left: browser, right: details panel).
- F9 shortcut to jump to library-root drive listing.
- Establish the main configuration pipeline (user setup for game folders).
- Implement the core "Library" and "Launcher" interface abstractions.
- **Status: COMPLETE**

### Phase 1.1: UI Polish & User Experience
- First-run setup wizard with library root management.
- Virtual file system: navigation reads from `data/games.json`, real filesystem only scanned during Setup.
- Folder scanner: parses sub-folders, detects `.exe`, launchers, marker files, Epic manifests.
- F2 Library Root Setup: add/remove/rescan roots.
- T Configure Game: edit display name, type, executable, launcher, args, manifest path.
- Enhanced details panel with executable path and resolved type.
- Navigation polish: selection highlight, auto-scroll, status feedback.
- **Status: PARTIAL** ✅ (core features implemented, but 9 critical bugs prevent basic usability — see Phase 1.1a)

### Phase 1.1a: Setup & GUI Stabilization ✅ COMPLETE
- Full plan: [phase-1.1-stabilization.md](./phase-1.1-stabilization.md)
- Fixed: folder scanner exclusions, exe detection heuristics, game entries terminal
- Fixed: ".." entry, Backspace navigation, keyboard focus after navigation
- Fixed: command button clicks, double-click drill-in
- Wired: F3, F5, F8, F10, S key handlers
- Added: `NavigationChanged` event, `_previousRootIndex` persistence
- **Status: COMPLETE** — all navigation, mouse, and keyboard fixes implemented

### Phase 1.2: Research & Data Collection
- **Objective**: Validate parsing approaches for game store data formats (Steam ACF, Epic manifests, standalone directories) using Python helper scripts. The objective is to extract *just enough* information to identify games, generate configuration files, and support migration. Scripts are **development-environment only** and MUST NOT disclose specific game names, paths, or registry keys back to the Agent. Confirm parsing logic against one representative sample per format and document the required structural schemas generically.
- **Status: IN PROGRESS** — Steam tasks 1-3b + Task 4 complete (ACF parsing, common/ cross-ref, library discovery, cross-library validation). 2 actionable cross-library mismatches found. Epic/other tasks remaining.

### Phase 2: Steam & Stand-alone Games (Baseline)
- Implement Steam integration (parsing library folders/manifests).
- Implement detection for stand-alone games.
- Implement the first migration features for Steam games (Folder Move + Symlink/Manifest update).
- **Status: NOT STARTED**

### Phase 2.1: SyncMove — Manifest-Aware Game Migration
- F6 SyncMove dialog: Move + Symlink, Move Only, Dry Run modes.
- Steam ACF manifest backup and update.
- Epic JSON manifest backup and update.
- Directory junction creation at original location.
- Migration log and backup manifests.
- Cross-library repair: detect game folders with ACF in wrong library (e.g. folder in lib X, ACF in lib Y) and offer to move/copy the ACF to the correct library.
- **Status: NOT STARTED**

### Phase 2.2: Game Metadata Lookup
- F4 to look up enriched game data from third-party sources.
- `GamingResourcesManifest` as a surgical registry of all data sources.
- PCGamingWiki Cargo API as primary source (no API key required).
- SteamDB, Steam Store as fallback sources (no key).
- IGDB as optional rich source (API key required).
- Local game database (`data/games_db.json`) with merge logic.
- Enriched details panel: developer, publisher, genre, save locations, PCGW link.
- **Status: NOT STARTED**

### Phase 3: Multi-Launcher Support
- Add GOG Galaxy support.
- Add Epic Games Store support (with manifest patching).
- Add EA App and Ubisoft Connect support.

### Phase 3.5: KODI-Style Category Browsing & Quick Search
- Full plan: [phase-category-browse.md](./phase-category-browse.md)
- Default view: Library Roots (configured paths → games). F8 toggles to Browse by Category.
- Drill-down: Category → Value → Filtered game list (across all roots).
- Categories: Genre, Publisher, Launcher, Year of Release, Gamerankings Rating.
- Launcher category works immediately (from `GameSourceKind`). Others need Phase 2.2 metadata.
- **S key**: Quick-search overlay from any view — matches name, genre, developer, publisher, path (union).
- Wildcards (`*`, `?`), multi-term AND, real-time results, match reason badges.
- **Status: PLANNED**

### Phase 4: Advanced Features & Polish
- Implement PCGamingWiki integration.
- Implement metadata repository sync. UX (themes, animations, batch operations).
