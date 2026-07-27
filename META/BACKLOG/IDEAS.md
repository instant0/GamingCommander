# META/BACKLOG/IDEAS.md — Future Ideas

**Nature:** Append-only. Never pruned. No priority implied.

---

## Feature Ideas

- **ExeCandidateSelector** — Replace filesystem "Browse..." in F4 with a dropdown of detected candidate exes. During scan, store all non-noise exe paths in `GameEntry.Extra["CandidateExes"]` (semicolon-separated). F4 dialog shows a combo box of candidates instead of a file picker. User selects which exe is the game launcher. Keeps the entire experience self-contained — only browse for library roots, never for individual files. Eliminates the need for `SuggestedStartLocation` API and avoids the "wrong folder" UX issue.
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

---

## User Testing Feedback (2026-07-26)

### blackist.json should not ship with user data
- **Observation:** `blacklist.json` is a shipped reference file, not user data. When the user deleted their `data/` folder to reset, they had to re-copy `blacklist.json` back. Consider shipping `blacklist.json` alongside the exe (in the publish output) and loading from `AppContext.BaseDirectory` rather than the user's data directory.
- **Impact:** MEDIUM — confusing for users who manage their data folder.

### Orphaned vs Missing status distinction
- **Observation:** "Orphaned" means physical folder exists but no ACF references it. "Missing" means ACF exists but game files not found. The distinction is not explained in the UI.
- **Impact:** LOW — UX confusion. Consider adding tooltip or status detail text.

### Library type ComboBox too narrow
- **Observation:** ComboBox for library type (Standalone, Battle.net, etc.) is too small to show the full text.
- **Impact:** LOW — cosmetic. Set `MinWidth` on ComboBox.

### Steam Controller Configs should be noise-filtered
- **Observation:** `Steam Controller Configs` folder in Steam library appears as an "Orphaned" game entry. This is a Steam internal folder, not a game.
- **Impact:** LOW — noise entry. Add to skip list.

### Two setup screens is confusing
- **Observation:** "Why do we have two different setup screens that are supposed to do the same thing?" — Wizard vs F2. See Plan 106 for unification.
- **Status:** ✅ Resolved — Plan 106 implemented. Single LibrarySetupWindow handles both.

### F5 should be refresh/rescan
- **Observation:** Every application uses F5 for refresh. GamingCommander should too.
- **Impact:** MEDIUM — UX convention mismatch. See Plan 105.

### Version-aware startup flow (not wizard on every version bump)
- **Observation:** `App.axaml.cs` line 107 triggers `needsWizard` on any version change (`isNewerVersion`). This reopens the full LibrarySetupWindow on every version bump, even when the user already has a working config.
- **Desired behavior:**
  - **First run / no config:** Full LibrarySetupWindow (wizard)
  - **Version bump with breaking changes:** LibrarySetupWindow with migration-focused message (flagged in code per version)
  - **Version bump, no breaking changes:** Brief "What's New" dialog, then straight to library
  - **Same version:** Straight to library, no dialog
- **Implementation:** Add a `Dictionary<string, string[]>` mapping version numbers to breaking changes. On startup, check if any version between `LastSeenVersion` and `currentVersion` has entries. If yes → migration wizard. If no → "What's New" dialog. If none → skip.
- **Impact:** LOW for internal testing, needed before public release.
- **Where:** `App.axaml.cs` lines 104-110
