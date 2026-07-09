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
- **Status: COMPLETE** ✅ (all features implemented)

### Phase 1.1a: Setup & GUI Stabilization ✅ COMPLETE
- Full plan: [phase-1.1-stabilization.md](./phase-1.1-stabilization.md)
- All 22 steps across 5 layers done. 18 tests pass (up from 3). Mock data + registry files created. Python validation tools for ACF, registry .reg files, and Epic manifests confirmed working.
- Next: Phase 1.2 — Research & Data Collection

### Phase 1.2: Research & Data Collection ✅ COMPLETE
- Explore and document data formats from Steam ACF files, Epic manifests, and standalone directories using Python helper scripts.
- Scripts are dev-only — never disclose output or findings to the Agent.
- Validate parsing approach against one representative sample per format.
- Produce schema docs in `docs/research/` for Phase 2 implementation.
- **Status: COMPLETE** — All 10 research docs exist. Real-world format validation done against real game files at `/mnt/p/Program Files (x86)/`. Python tools validated/fixed for accurate format handling. Bugs found in C# `FolderScanner.DetectType()` (see below).
- **C# Bugs Found (fix in Phase 2.0):**
  1. **GOG**: Checks `goggame.info` (exact name) — real files are `goggame-<id>.info` (prefix match needed)
  2. **EA**: Checks `eaapp_` prefix / `.ea.web` / folder name — real detection needs `__Installer/` directory (validated even against staged install)
  3. **Ubisoft**: Checks `ubisoft game launcher url` / folder name — real detection needs `uplay_install.manifest` or `uplay_r*_loader*.dll`
  4. **Performance**: `Directory.GetFiles("*", AllDirectories)` in `DetectType()` is recursive — use root-level scan
- **Format Caveats**:
  - EA format doc based on staged install only — needs verification against a complete EA game install
  - Steam `libraryfolders.vdf` has `contentid`, `update_clean_bytes_tally`, `time_last_update_verified` — documented but not required for GC
- **Detection Research Extension:** Python helper detection now targets near-100% local folder classification before C# implementation. It separates store detection from local game-engine detection. Engine tags are signal-only (`Unreal Engine`, `Unity`, `RAGE`, `Frostbite`, or `Unknown`) and can later be enriched by PCGamingWiki metadata.
- **Executable Metadata Note:** Python helpers may use `pefile` during research to extract Windows PE version resources such as `FileDescription` and `ProductName`. The actual C# Windows application must implement this separately using Windows/.NET-compatible PE version APIs (for example `FileVersionInfo`) rather than depending on Python tooling.

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
- **Status: NOT STARTED**

### Phase 2.2: Game Metadata Lookup
- F4 to look up enriched game data from third-party sources.
- `GamingResourcesManifest` as a surgical registry of all data sources.
- PCGamingWiki Cargo API as primary source (no API key required).
- SteamDB, Steam Store as fallback sources (no key).
- IGDB as optional rich source (API key required).
- Local game database (`data/games_db.json`) with merge logic.
- Enriched details panel: developer, publisher, genre, save locations, PCGW link.
- Include `GameEngine` as searchable metadata. Initial value comes from local
  signal detection; PCGamingWiki/metadata sync may enrich or override it later.
- Include executable metadata (`FileDescription`, `ProductName`, company,
  original filename) as a local-name signal when folder/executable names are
  abbreviated. Python research uses `pefile`; C# implementation must use native
  .NET/Windows PE version resource reading.
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
