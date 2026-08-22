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

---

## Nice-to-have / lowest priority (2026-08-10)

> Not on the post-MVP execute list. Capture only — do not schedule ahead of Plan 117, SyncMove, Plan 102, or open TECH_DEBT bugs.

### Launcher-vs-game signal discrimination (name collisions)

- **Problem:** Plan 114 filtered `"arc"` as noise (ARC Game Store/launcher). There is also a **game** called ARC (and similar short-name collisions). Pure folder/exe **name** blacklists cannot safely distinguish store client vs title.
- **Desired approach:** Research and document **signal profiles**:
  - Typical **game-store / launcher** folder contents (installer remnants, update clients, shared runtimes, store-branded DLLs/manifests, no primary game payload layout).
  - Typical **game** folder contents (store game signals we already use, engines, large primary exe, content dirs).
- **Outcome:** A detection task/plan that classifies ambiguous names via **in-folder signals** (and scoring), not by banning the string `"arc"` alone. Same pattern should generalize to other launcher/name collisions.
- **Priority:** Nice-to-have — lowest. Related short-term: Bug 24 name filter is a blunt fix; this idea is the durable replacement.
- **Touches (when eventually planned):** `StoreSignalDetector` / `FallbackSignalDetector` / noise lists / possibly `data/signals.json` (see also “Configurable detection signals” above).

### User-toggleable noise / backup filters in setup

- **Problem:** Auto-filters hide backup-style folders such as `"x64 - Copy"`, `" - copy"`, `" - Copy of"`, and similar known patterns. Some users **want** those entries visible (deliberate backups, side-by-side builds).
- **Desired approach:** In Library Setup (F2 / first-run), add a **sub-screen or options section** where the user can **enable/disable groups of known filter patterns** (e.g. “Hide backup/copy folders”, “Hide store launcher dirs”, runtime redistributable skips). Defaults stay conservative (filters on).
- **Constraints:** Shipped defaults remain safe; toggles are user preference persisted in settings (not rewriting shipped `blacklist.json` unless we design overlay rules).
- **Priority:** Nice-to-have — lowest. Complements “Configurable detection signals” but is **UX/settings**, not only moving patterns to JSON.
- **Touches (when eventually planned):** `LibrarySetupWindow` / settings model / `FileSystemHelper` + container noise application path.

### Suggest nested Steam (and similar) as a separate library root (`??` marker)

- **Related debt:** Bug 13 — FolderScanner **excludes** nested Steam trees (no auto-wire of `SteamLibraryScanner` into FolderScanner). Until the user adds a second root, those Steam games are invisible.
- **Problem:** Mixed layout is common:

  ```
  d:\games\                 ← root A (FolderScanner): standalones
  d:\games\steam\           ← Steam client and/or library (steamapps\common\...)
  ```

  GamingCommander uses a **VFS of library roots**, not a full filesystem browser. Showing everything under one root by secretly calling Steam from FolderScanner is rejected (coupling / bug risk).

- **Desired UX — “Suggest to user”:**
  1. While scanning a mixed root, **detect** nested Steam library structure (`steamapps/common` on a child; same idea as `LooksLikeSteamLibrary`).
  2. Do **not** import those games into the mixed root’s game list via Steam scanner.
  3. Surface a **suggestion row** in the VFS/UI (e.g. status/badge `??` or “Suggested library”) for the detected path — typically the Steam **library root** (folder that **contains** `steamapps/`, e.g. `d:\games\steam`), not every game under `common`.
  4. If the user **marks / accepts** the suggestion: prompt to **add this path as its own library root** (type Steam). After accept, config has two roots, e.g.:
     - `d:\games\` — Standalone/mixed (FolderScanner)
     - `d:\games\steam` — Steam (`SteamLibraryScanner`)
  5. VFS then lists both roots independently; no need to navigate a real nested FS tree inside one root.

- **Copy sketch:** “Suggested: Steam library at `d:\games\steam`. Add as library root?” → Yes adds root and rescans that root only.

- **Extensions (later):** Same pattern for other nested store trees if we ever exclude them from FolderScanner the same way.

- **Priority:** Nice-to-have — after Bug 13 exclusion exists; do not block Plan 117.
- **Touches (when planned):** FolderScanner (emit suggestion metadata only), setup/F2 add-root flow, Shell VFS roots list, config persistence.
- **Non-goals:** Wiring `SteamLibraryScanner` into `FolderScanner`; auto-adding roots without user consent.

---

## Resolved / shipped (append 2026-08-22)

- **PCGW save/config paths in the right pane** — shipped (Plan 120 `Details` + token resolve). Clickable only under install / profile / AppData allowlist.
- **F3 metadata** — shipped as force lookup + page picker (not a separate “view” window).
- **Cargo / SteamDB / IGDB** — still ideas only; product path is PCGW Parse + Steam Store. Do not treat `04-phase-2-metadata-lookup.md` as current.
