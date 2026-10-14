# META/SESSION/CURRENT.md — Current Project State

**Nature:** Scratch. **Overwritten** every session handoff.
**Audience:** All agents. Read every session.
**Updated:** 2026-10-14 — Pending/active task audit + task review. Plans 124/125 COMPLETE. Plan 126 EOSH CLOSED — **Epic `.item` re-creation is a dead end and DEFERRED**. **Directive: detection first (read existing manifest files); SyncMove deferred.**

---

## This session (2026-10-14) — task/tracker audit

Verified by reading the latest commits (`d2da6d7`, `3027f4f`, `d6a3306`, `9b28f81`), the `126-*` fact docs, `123`, `04-phase-2-syncmove`, and the live code.

### Verified status of recent plans
- **Plan 124 (anchor library + flat game DB) — COMPLETE** (2026-09-08). Code present: `Core/Models/Library.cs`, flat `Core/Models/GamesDatabase.cs` (`List<GameEntry>`), `Core/ILibrariesService.cs`, `App/Services/LibrariesDatabaseService.cs`; `GameRoot.cs`/`LibraryRoot.cs` removed. `AppConfig` now holds only `HiddenFolders`/`IsFirstRun`/`LastSeenVersion`/`EnableOnlineMetadata`.
- **Plan 125 (stabilization refactor) — COMPLETE** (2026-09-09). 564 tests green per plan/NEXT history.
- **Plan 126 (EOSH / Epic orphan `.item`) — INVESTIGATION CLOSED** (2026-09-10). See below.
- **Plan 123 (detection tightening) — SUBSTANTIALLY COMPLETE.** The Phase 4 C# port shipped 2026-09-07 (`docs/detection/07` "ALL PORTED", `PythonParityDetectionTests`, 10 tests; code confirms `FindExactFolderMatch`/`FindParentBoundExe`/`AcronymMatchesTitle`/`IsNoiseExeName`). **Residuals now:** confidence-tier marking (`IdentificationTier` not in code) and the amended review gate. **Closed 2026-10-14:** E6a (`"tool"` added to `blacklist.json` tier 10) and the "3-level fallback" gap (was a doc error — C# matches/exceeds Python; depth now user-configurable via `AppConfig.ScanDepth`).
- **SyncMove (`04-phase-2-syncmove.md`, Phase 2.1) — PLANNED**, no tasks started. Last Phase 2 leftover per ROADMAP.

### Plan 126 outcome (why it is closed)
- The `.egi` plant experiment (2026-09-09) falsified the local-registry theory: EOSH reads its own install DB, not `EpicOnlineServicesShared\InstalledItems`.
- `.mancpn` hypothesis falsified (8o); Ruiner reinstall proved identity rules field-for-field correct but registration only occurs through a real EOSH install-API flow (8j).
- Procmon proved an external HTTPS entitlement/library call precedes the local delete (8r); Troy (Owned) still rejected (8t/8u). **No file-based workaround exists.**
- **Product consequence:** Plan 121's orphan `.item` write cannot register a game EOSH does not report. **Per user directive (2026-10-14), Epic `.item` re-creation/registration is DEFERRED** — it could not be made to work with Epic. The product focus is **detection from existing manifest files** (read-only). T78 is deferred; its AppName-precedence finding is retained as reference.
- **Non-goal now:** writing `.item`/`.egi` to register or repair Epic installs.
- Optional/non-blocking RE tail in `126-facts-eosh-re.md` (identify class of global `0x14316fed0`; confirm `vtable[0x48]` semantics) — the server-side gate is already proven, so this is archival only.

### Tracker/doc drift found and fixed this session
- `planning/README.md`: Plan 124 was absent; 123 still listed "PLANNED"; no 126 rows → updated.
- `META/CODE_MAP.md`: still described the pre-124 model (`GameRoot`, `LibraryRoot`, `GamesDatabase.Roots`, `AppConfig.LibraryRoots`, old `ILibraryManager`/`IGamesDatabaseService` signatures) → corrected.
- `META/ROADMAP.md`: no 124/125/126 milestone rows → added; current-track line refreshed.
- `META/ARCHITECTURE.md`: navigation still showed F9 (removed) and predated the anchor model → appended current-reality note.
- `docs/detection/06-deep-scan-fallbacks.md`: stale "C# gap (HIGH): IsNonGameFolder only layer 1" → corrected (all 3 layers present since 2026-09-07).
- Added `META/COMPLETED/plan-124-*.md` and `plan-125-*.md`; appended the Plan 126 correction to the Plan 121 completion record.
- Added `META/TASKS/phase-j-followups/` tracker (T78 deferred, T79 detection P0, T80 SyncMove deferred).

### Implemented this session (code)
- **E6a:** `"tool"` added to `data/blacklist.json` `tier_10_dev_editor_tools`.
- **Configurable scan depth:** `AppConfig.ScanDepth` (default 3, clamp 1–5) → `JsonConfigService` round-trip → `FolderScanner` → `ExecutableDiscovery.FindPrimaryExecutable`/`FindExecutablesDeep` recursive fallback. 3 new tests.
- **Detection tier (Plan 127 A3 / T79):** `IdentificationTier` enum, `DetectionInfo` + `StoreInfo` records, `DetectionTierDeriver`; wired into `FolderScanner`, `SteamLibraryScanner` (3 paths), `EpicLibraryScanner` (2 paths); persisted in `games.json` DTO.
- **Plan 127 A2 (metadata bag retired):** `PlatformMetadata` `Dictionary<string,string>` **removed** from `GameEntry` and the DTO; `PlatformMetadataKeys` + contract test deleted; replaced by typed `DetectionInfo`, `StoreInfo`, `SteamAcfInfo`, `EpicInfo`, `LocationInfo`. All writers/readers migrated (scanners, persistence, MainWindow repair/metadata, GameSetupWindow, ShellViewModel, MetadataLookupQueue). `docs/DATA-FORMAT.md` §2/§4 + `META/CODE_MAP.md` updated.
- **Plan 127 A1 (GameEntry composed):** `GameEntry` is now `Identity` + `Location` + `Launch` (positional) + `Detection`/`Store`/`Acf`/`Epic` (body), with flat names as read-only accessors. All 9 constructors, `with` expressions, DTO mapping, and tests migrated. The JSON shape is now nested.
- **Plan 127 A5 (identity reconciliation):** `IdentityKey`/`IdentityKeyKinds`, `IdentityInfo.IdentityKeys`, `LocationInfo.PreviousLocations`, `GameIdentity.WithRebuiltKeys`, and move reconciliation in `SetGamesForLibrary` (`FindRelocated`): strong store-id keys match regardless of disk; the weak folder-name key matches only when the old folder is gone; the existing `Id` + sidecar are preserved and the old path appended to `PreviousLocations`. 5 new tests. `Id` stays folder-derived (deviation — reconciliation preserves it).
- **Plan 127 A6 (file ownership):** sidecar `SteamAppId`/`GogGameId`/`Engine` renamed to `FetchedSteamAppId`/`FetchedGogGameId`/`FetchedEngine` (they are fetched PCGW/Steam values, not the detected `GameEntry.Store`/`GameEngine`); `GameMetadataRecord` XML docs + `docs/DATA-FORMAT.md` §5 now state `games.json` is authoritative and the sidecar owns fetched enrichment only.
- **Plan 127 written** (`planning/127-persistence-v2.md`) — full A/B/C spec incl. identity (A5) + file ownership (A6) + deferred `Extensions{}`/emulator (C); revised for the no-back-compat clean-cut policy.
- **Build/tests:** 0 errors; **583 pass / 0 fail** (Core 163 + Migration 1 + App 419; was 564 at session start).
- **Doc corrections:** `docs/detection/05`/`06`/`07` stale gap claims; Epic `.item` recreation marked DEFERRED.

### Back-compat policy (2026-10-14)

**Nothing has shipped → no back-compat anywhere.** Persisted schemas (`games.json`, `libraries.json`, `settings.json`, `games_metadata.json`) are disposable: no migration, no dual-write, no pinning of persisted key strings. Affected tasks revised:
- **Plan 127 A1/A2** — clean cut (no additive/dual-write/transitional steps); `PlatformMetadata` bag + `PlatformMetadataKeys` deleted outright.
- **Plan 127 A4** — versioning framework only; no v1→v2 migration to write.
- **Standing rule** "never rename persisted strings" — retired (`META/SESSION/NEXT.md`).
- **Plan 123 / T80 / T78** — unaffected.

### Open TECH_DEBT (remaining)
- Bug 12 — library-type ComboBox too narrow (`MinWidth`).
- EA format caveat — research doc based on staged install only.
- Plan 127 A4 (schema versioning), B7–B13 (tracking, install state, user state, launch profiles, relationships, collections, typed tags).
- Plan 123 amended review gate (process).
- SDK .NET 8→9 — deferred indefinitely.

## Prior session context (superseded)
Plan 125 COMPLETE (2026-09-09). EOSH RE findings live in `planning/126-facts-eosh-re.md` (corrected gate model: `vtable[0xd0]` log-only; `vtable[0x48]` on singleton `0x14316fed0` = EOSH-reported-set membership).
