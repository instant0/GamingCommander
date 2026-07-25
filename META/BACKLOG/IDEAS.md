# META/BACKLOG/IDEAS.md — Future Ideas

**Nature:** Append-only. Never pruned. No priority implied.

---

## Feature Ideas

- KODI-style category browsing (F8) — fully designed in planning/05-phase-3-category-browse.md
- Quick-search overlay (S key) — designed in planning/05-phase-3-category-browse.md
- GOG Galaxy detection and integration
- Epic Games Store manifest patching
- EA App and Ubisoft Connect support
- GamingResourcesManifest as registry of all metadata sources
- PCGamingWiki Cargo API integration for rich metadata
- IGDB integration (requires API key)
- SteamDB as secondary metadata source
- Local game database with merge logic (data/games_db.json)
- Cover art display in details panel
- Save location display from PCGamingWiki
- Theme support (Norton Commander color schemes)
- Batch operations (multi-game migration, retagging)
- GitHub metadata repository sync
- Game launch counter and play time tracking
- Category value normalization (publisher name merging)
- Rating bucketing for gamerankings display
- Cheat Engine table linking — F4 editor option to associate a `.CT` file with a game; on launch, optionally start Cheat Engine with the table loaded
- Library root display: show game count + top used tags per root (e.g., "D:\SteamLibrary (123 games) [RPG, Action, Co-op]")
- SteamDB metadata: tags, user ratings, active users for Steam games — populate GameEntry.Tags automatically
- Library root details page: aggregate info (total games, top 5 rated, last played)
- F9 interaction hint shows current mode: `"F9: Mode [Library]"`

## Technical Ideas

- Migrate from JSON to SQLite for larger game libraries (thousands of games)
- Build-time code generation for GamingResourcesManifest from YAML/JSON source
- Windows registry abstraction layer for testability
- Integration test framework using mock data in data/mock/
- **Configurable detection signals** — Move UE platform names, non-game folder patterns, and noise exe patterns to `data/blacklist.json` (or new `data/signals.json`). This allows updating detection logic without recompiling. Currently hardcoded in: `ExecutableDiscovery.s_uePlatformNames`, `FolderScanner.s_nonGameFolderNames`, `FileSystemHelper.NoiseSubDirNames`, `FolderScanner.DefaultNoiseExePatterns`.
