# Plan 125 — Code Architecture Refactor: Split + Deduplicate

**Status:** **COMPLETE** (2026-09-09). Phases 0–2 + final stage all delivered: GameEntryId contract (+4 tests), `AppDataPaths` composition root, shared `ToPaneItem` projection, MainWindow partial ×8 (base 113 L), FolderScanner enrichers (526→352 L), ContainerScanner single-pass `SignalSummary`, 2a `ExecutableDiscovery.Scoring.cs` partial (749→533 L), 2b `PlatformMetadataKeys` (26 consts, 2 pin tests), 2c `CheapExeFinder`, 2d `GameSourceParser` single array + `LibrarySetupViewModel` display-name fix. Final stage part 1: ShellViewModel split by domain into 9 partials (base 1,023→178 L + Navigation/Filtering/Search/Loading/Scanning/Details/Details.Status/Details.Metadata/Tags) — deviation from plan overall criterion (ShellViewModel <400 L) resolved via domain cohesion per user direction: readability/agent-editing over line counts. Final stage part 2: App warning cleanup (8→3; only pre-existing AVLN3001 designer notices remain). Final stage part 3 (user-approved, re-scored with real occurrence counts): `TitleSourceValues` (8 consts), `SteamAcfFields` (10 consts), `EpicItemSchema` (9 consts) with 6 pin tests in Core; writer/reader key drift is now a compile error. Phase 3 `TestGameFactory` **DROPPED** as N/A (plan gate: only if still needed; audit found 3 single constructions, shared P1b fixture already covers the rest). Build: full solution 0 errors (UI project 0 warnings); `dotnet test` App 398 + Core 165 + Migration 1 = **564 pass**. All success criteria met.
**Type:** Structural refactor (behavior-preserving).
**Informed by:** Plan 124 completion review; code-reviewer architecture audit (2026-09-08).
**Predecessor:** `124-anchor-library-game-db.md` (COMPLETE, all tests green).

---

## Problem

Three files are too broad and one identity contract is ambiguous:

| File | Lines | Responsibility mix |
|------|------:|--------------------|
| `MainWindow.axaml.cs` | 1,085 | window wiring, keyboard/navigation, launching, metadata lookup, scanning, Epic/Steam repair, status |
| `ShellViewModel.cs` | 1,065 | navigation state, game-row projection (×2), details formatting, search, tags, platform status |
| `FolderScanner.cs` | 526 | detection pipeline + a 250-line entry-construction method with store/title/PE enrichment |

Dedup opportunities confirmed:

- `App.axaml.cs` and `MainWindow.axaml.cs` both construct `JsonConfigService`, `FolderScanner`, `SteamLibraryScanner`, `LibraryManager` and both repeat the `data/*.json` path helpers.
- `ShellViewModel.LoadGamesForLibrary` and `LoadFilteredGames` duplicate the full `GameEntry → ShellPaneItemViewModel` projection (~90 lines each).
- `ContainerScanner` evaluates root-exe + Unreal layout signals during its counting pass and again during promotion.
- `GameEntryId` documents *anchor name + physical folder path* but every scanner computes IDs from *physical root + folder* before `LibraryManager` stamps the anchor. Contract vs implementation are not reconciled.

## Guiding constraints

- **Behavior-preserving.** No user-visible change, no on-disk format change, no metadata-key renames (`LibraryRoot`, `ActualLibraryRoot`, `GameFolder` stay as-is).
- **Safe execution.** New files are written before code is removed from existing files. One file at a time. Small edits, build + `dotnet test` after each phase.
- **Test scope.** Only the minimal characterization tests required for safety. Most phases are protected by the existing suite (App 394 + Core 156 + Migration 1 = **551 test methods**; note: the "562" figure in `META/SESSION/CURRENT.md` was a summation error — 394+156+1 = 551). Do not add broad new suites.
- **Do not** introduce a DI framework, a detection plugin framework, or speculative abstractions.

---

## Phase order and content

Execute in this order. Each phase ends with a full solution build + `dotnet test` (all existing tests green).

### Phase 0 — Reconcile the game-entry identity contract

**Why:** `GameEntryId.ComputeId` is documented as `(anchor name, physical folder)` but every scanner passes a *physical* root (or manifest catalog ID) as the first argument:

- `FolderScanner.cs:297` — `ComputeId(rootPath, subDir.Name)`
- `SteamLibraryScanner.cs:189,236,269` — `ComputeId(libraryRoot, folderName | installdir)`
- `EpicLibraryScanner.cs:44` — `ComputeId(catalog.ManifestsDir, catalogItemId | folderName)`

`LibraryManager.ScanFolder` stamps `GameEntry.Library` (the anchor) *after* the ID is computed.

> **Contract revision:** Plan 124 and the current `GameEntryId` docs describe an *anchor-based* identity. This phase **formally revises that contract** to physical-root-based identity and updates the docs accordingly. This is a documentation/code-comment change only — it does not change on-disk data (IDs are already computed this way).

**Decision:** keep physical-root-based IDs. Anchor-based IDs are **unsafe** for multi-folder anchors — a single `Steam` anchor with N physical libraries would compute the same ID for two identically-named game folders in different roots (collision), and re-anchoring a game (e.g. Epic under `d:\games` → `Epic Games Store`) would rewrite its ID and detach sidecar metadata. Physical-root IDs are collision-safe and stable across rescans; re-anchoring keeps the ID (anchor is metadata, not identity).

**What changes:**
1. **Rename the first parameter** of `GameEntryId.ComputeId` from `libraryName` to `physicalLibraryRoot` (API + doc comment), stating: `ID = MD5("{physicalLibraryRoot|folder}")`, independent of anchor name.
2. Update `docs/DATA-FORMAT.md` to the physical-root contract (it currently implies anchor-based IDs).
3. Add characterization tests (below) that pin identity behavior *before* any code change.
4. No other production code change expected unless the tests expose a real inconsistency.

**Protection tests (new, minimal):**
- `tests/GamingCommander.Core.Tests/GameEntryIdTests.cs` — extend:
  - same `(physicalRoot, folder)` → same ID (stability);
  - different physical roots → different IDs (no collision);
  - re-anchoring: `ComputeId(root, folder)` unchanged regardless of any anchor-name input (prove the anchor name is NOT part of the hash inputs — the parameter rename makes this explicit);
  - **do NOT** assert `ComputeId("Steam", f) == ComputeId("GOG", f)` — the first argument *is* a hash input, so different values produce different IDs; such a test would be invalid.
- `tests/GamingCommander.App.Tests/SteamLibraryScannerTests.cs` — one assertion that a game scanned via two different physical roots yields distinct IDs (collision guard).
- `tests/GamingCommander.App.Tests/GamesDatabaseServiceTests.cs` — one test that re-anchoring (`RetagGame`) preserves the entry ID; one test that an Epic catalog rescan of a known game preserves the folder-scan ID when matched.

**Success criteria:** contract documented accurately (`physicalLibraryRoot`), tests pin it, no ID regressions in the full suite.

---

### Phase 1 — Single composition root (dedup App ↔ MainWindow)

**Why:** `App.axaml.cs` (lines 62–210) and `MainWindow.axaml.cs` (constructor, lines 36–132) each build `JsonConfigService`, `FolderScanner`, `SteamLibraryScanner`, `LibraryManager`; both repeat the `data/*.json` path helpers (`GetConfigPath`, `GetGamesDbPath`, `GetLibrariesPath`; App also has `GetMetadataDbPath`). The duplication means the two `LibraryManager` instances can diverge (e.g. blacklist/hidden-folder drift).

**What changes:**
1. Add `AppDataPaths` (new file, `src/GamingCommander.App/AppDataPaths.cs`) — static helper owning the four `data/*.json` path methods. Document that the methods may **create the data directory** (filesystem side effect). Delete the private copies from `App.axaml.cs` (212–246) and `MainWindow.axaml.cs` (134–159).
2. `MainWindow` constructor: add required `ILibraryManager` + `IConfigService` parameters. **Remove not only the `LibraryManager` construction but the `_scanner` and `_steamScanner` fields and their construction too** (lines 61–76). Delete the private null-fallback `GetLibrariesService`/`GetDbService`/`GetConfigService` methods (161–174).
3. Make `_dbService`, `_configService`, `_librariesService`, `_libraryManager` **non-nullable required** fields, and audit every direct dereference for the resulting nullable-safety improvements.
4. `OpenLibrarySetupAsync` (447–464) uses the injected `ILibraryManager`; the `SteamInstallPathLocator` stays window-local (registry reader already threaded).
5. Update the sole call site, `App.axaml.cs:141`, to pass the new arguments. Keep App's blacklist/config construction so the injected scanner is identical to today's.
6. **Verification:** confirm "exactly one place constructs each service" by repository search (`grep` for `new LibraryManager(`, `new FolderScanner(`, `new SteamLibraryScanner(`, `new JsonConfigService(`), not just by compilation.

**Why injection and not a service-locator:** keep it explicit and minimal; no DI framework.

**Protection tests:** no direct tests for GUI wiring — the phase is internal-only; full build + full suite + existing `LibraryManagerTests`/`SteamBootstrapTests` stay green.

**Success criteria:** exactly one construction site per service (verified by grep); zero duplicated path helpers; build + suite green.

---

### Phase 1 — Deduplicate ShellViewModel game-row projection

**Why:** `LoadGamesForLibrary` (614–733) and `LoadFilteredGames` (735–821) each map `GameEntry → ShellPaneItemViewModel` with the same ~20 fields: launch target, platform ID/status, install directory, subtitle/tags, source label, store badge, tag badges. The two differ only in `LeftPath` (exe name vs library name) and `LoadGamesForLibrary`'s extra color/detail fields. Within `LoadGamesForLibrary`, `platformStatusColor` and `itemStatusColor` are the same switch evaluated twice (651–667).

**What changes:**
1. Extract a single private projection method `ToPaneItem(GameEntry game, string libraryName, string? leftPathOverride)` that returns the shared `ShellPaneItemViewModel`. Both loaders call it; the filtered loader passes the library name as `leftPathOverride`.
2. Collapse the duplicated color switch into one computation.
3. No navigation-state change.

**Protection tests:** `ShellViewModelSearchTests` already covers the filtered projection; add one test asserting a library-detail row and a filtered row for the same game produce equivalent Title/SourceLabel/Tags/StoreBadge (characterizes the projection surface so "no user-visible change" is verifiable).

**Success criteria:** the projection body exists once; the two loaders shrink to collection/navigation logic; suite green.

---

### Phase 1 — Split MainWindow by responsibility (partial classes)

**Why:** 1,085 lines across ~6 unrelated concerns; single class is hard to navigate and review.

**What changes (mechanical, behavior-preserving):** convert `MainWindow` to partial and move existing methods into focused files (new files created first, methods removed from `MainWindow.axaml.cs` afterwards). Partial methods stay discoverable to XAML event wiring (`Click="..."`) as long as signatures are unchanged.

| New file | Members moved (exact names) |
|---|---|
| `MainWindow.Status.cs` | `SetStatusWithAutoClear` + ownership of `_statusClearCts` |
| `MainWindow.Navigation.cs` | `TryMapPrintable`, keyboard/navigation commands, `LeftListBox_DoubleTapped` |
| `MainWindow.Launching.cs` | `LaunchSelectedGameAsync` + launch helpers |
| `MainWindow.LibrarySetup.cs` | `OpenLibrarySetupAsync`, `OpenFilterAsync`, `TagBadgePressed`, `OpenGameSetupAsync` |
| `MainWindow.Scanning.cs` | `RefreshCurrentRootAsync`, scan lifecycle + ownership of `_scanCts`, scanning-status badge updates |
| `MainWindow.Metadata.cs` | metadata queue/status + ownership of `_metadataCts`: `QueueSilentMetadataIfStale`, `ProbeOnlineAsync`, `SyncOnlineGateFromConfigAsync`, `UpdateLookupChip`, `EnqueueMetadataLookups`, `OnMetadataQueueProgress`, `OnMetadataQueueItemCompleted`, `LookupSelectedGameMetadataAsync`, `RefreshMetadataForGameAsync`, `IsUserPinnedTitle`, `TrySteamAppId`, `GetSelectedGame`, `FindGameById` |
| `MainWindow.PlatformRepair.cs` | `WriteEpicItem_PointerPressed`, `WriteEpicItemAsync`, `WriteSteamAcf_PointerPressed`, `WriteSteamAcfAsync` (if present), `OpenConfigPath_PointerPressed`, `OpenSavePath_PointerPressed`, `OpenGameFolder_PointerPressed`, `OpenFolderFromDisplay` |
| `MainWindow.Commands.cs` | `ChangeType_PointerPressed`, `MultipleExeWarning_PointerPressed`, `CommandButtonPressed` |

**Rule:** a partial file touches only members already private to `MainWindow`; no signature changes, no behavior changes, no new services. **Do NOT** extract non-UI workflows into new coordinator classes in this phase — deferred pending partial-split outcome.

**Protection tests:** none exist for MainWindow; the phase is pure member relocation — rely on compile + full suite.

**Success criteria:** `MainWindow.axaml.cs` drops to constructor + wiring; every moved method compiles in its new file; suite green.

---

### Phase 2 — Split FolderScanner entry construction

**Why:** `AddGameEntry` (271–524, ~250 lines) mixes executable discovery, store enrichment (GOG/EA/Epic/BattleNet/Ubisoft), PE/title enrichment, and final `GameEntry` assembly — all inside one method of a scanner whose `Scan` pipeline is otherwise a clean 10-signal chain.

**What changes (extract, do not reorder):**
1. `Scan` (113–218) stays the pipeline with the exact current pass order: Pass 1 (store) → 1c (registry) → 1.5 (exact match) → 1.6 (parent-bound) → Pass 2 (fallback) → Pass 3 (container).
2. Extract `StoreMetadataEnricher` (new file) — the GOG/EA/Epic/BattleNet enrichment blocks from `AddGameEntry` (320–438), operating on the same mutable locals (`displayName`, `exePath`, `commandLineArgs`, `platformMetadata`) via a small result object. **Preserve the exact enrichment ordering and all metadata keys** — store title enrichment runs BEFORE PE/title enrichment, and each store block writes its specific keys (`GogGameId`, `Studio`, `EaGameName`, `EpicCatalogItemId`, `BlizzardProduct`, `TitleSource`, …).
3. Extract `TitleEnricher` (new file) — the `TitleSource` logic (FolderExeMatch, PE FileDescription with guards, Ubisoft readme; 440–498).
4. Keep executable/launcher candidate resolution where it lives (`ExecutableDiscovery`, `LnkParser`); `AddGameEntry` becomes a thin assembler calling the two enrichers + `EngineDetector` and constructing the `GameEntry`.
5. **Read-only contract:** enrichers only read files (existing parsers already do); add a review assertion (or one test) that enrichment performs no manifest/config writes.

**Protection tests:** the detection suite already covers these branches (`GogInfoParserTests`, `EaInstallLogParserTests`, `EpicManifestParserTests`, `StoreSignalDetectorTests`, `PythonParityDetectionTests`, `MockDataIntegrationTests`, `FolderScannerContainerTests`, `RegistryFallbackDetectorTests`). No new tests required unless a branch loses coverage — verify by running the suite. Watch for nullable/default-value regressions in the new result objects (existing fixtures pin the outputs).

**Success criteria:** `FolderScanner.cs` drops below ~300 lines; enrichers are single-responsibility; detection order, signal priority, and metadata keys unchanged; suite green.

---

### Phase 2 — Deduplicate container/fallback signal evaluation

**Why:** `ContainerScanner.ScanContainerChildren` evaluates `StoreSignalDetector.DetectType` + `HasRootExecutableSignal` + `HasUnrealLayoutSignal` in the counting pass (63–74) and again in the promotion pass (85–124).

**What changes:** introduce a narrow internal per-child summary `ContainerScanner.SignalSummary(child, noiseExePatterns)` computed **once per child per call**, carrying: store type (or Unknown), hasRootExe, hasUnreal, and **non-game classification** — because `IsNonGameFolder` itself can call `StoreSignalDetector.DetectType` (line 197), the summary must include the layer-1/2/3 non-game result so `DetectType` is not re-evaluated. Keep traversal/promotion policy in `ContainerScanner`; keep `FallbackSignalDetector` and `ExecutableDiscovery` as-is.

**Protection tests:** `FallbackSignalTests`, `FolderScannerContainerTests`, `PythonParityDetectionTests` cover the signals; run suite after.

**Success criteria:** each child is classified once per call (including the non-game check); suite green.

---

### Phase 2 (round-2 additions) — Deduplicate remaining hotspots found in re-evaluation

**2a. `ExecutableDiscovery.cs` split (749 lines, NOT in the original scope).**
The class mixes search/parsing (`FindExecutablesDeep`, `FindParentBoundExe`, `FindExactFolderMatch`, `FindPrimaryExecutable`, `FindLauncherExecutable`) with a large scoring algorithm (`ScoreExecutable` 340–505 incl. PE-internal-name checks, `RomanNumeralBonus`, `AbbreviationBonus`, `ExeScoreResult`). Split scoring into `ExecutableScoring.cs`; keep discovery/parsing in `ExecutableDiscovery.cs`. Pure relocation; `ExecutableDiscoveryTests`/`ExecutableScoringTests` already exist and pin behavior.

**2b. `PlatformMetadataKeys` constants class.**
The ~20 metadata keys (`"SteamAppId"`, `"SteamStatus"`, `"EpicStatus"`, `"EpicCatalogItemId"`, `"GameFolder"`, `"LibraryRoot"`, `"ActualLibraryRoot"`, `"FolderName"`, `"TitleSource"`, `"BlizzardProduct"`, `"AcfLibraryPath"`, `"AcfSizeOnDisk"`, `"AcfBuildId"`, `"AcfStateFlags"`, `"AcfExpectedPath"`, `"AcfFilePath"`, `"ExeCandidateCount"`, `"ExeCandidates"`, `"PeFileDescription"`, `"AutoDetectedTitle"`, `"EpicCatalogNamespace"`, `"EpicAppName"`, `"GogGameId"`, `"Studio"`, `"EaGameName"`, …) are string literals across 6+ files (56+ occurrences). Add `PlatformMetadataKeys` (Core) with `const string` values. **Rename-safe only because values are unchanged** — these keys are persisted data; the constants must equal the current literals exactly.

**2c. Unify the three cheap-exe finders.**
`SteamLibraryScanner.FindPrimaryExe` (325–342), `EpicItemWriter.FindLaunchExe` (342–373), and EpicLaunchResolver share the same skeleton: root-exe first, then a few relative folders (`Binaries/Win64`, `Binaries/Win32`, …), forbidden-exe filtering via `ExecutableDiscovery.IsForbiddenLaunchExe`. Extract one shared `CheapExeFinder.FindFirst(dir, relativeFolders, extraSkips)`; keep SteamLibraryScanner's intentionally-cheap behavior (documented) and EpicItemWriter's crashpad/crashreporter skips as per-caller parameters. **Do not** upgrade Steam to the full-tree finder — that is a deliberate performance divergence.

**2d. `GameSourceParser` manual parallel lists.**
`SourceDisplayNames` (array) + `ToDisplayName` (switch) + `ParseFromString` (switch) are manually kept in sync (`/// Must match SourceDisplayNames`). Replace with one ordered `(GameSourceKind, string)[]` + two derived mappings. Fix the one consistency bug it enables: `LibrarySetupViewModel` stores `DefaultType = type.ToString()` (e.g. `"EaApp"`) while the combo/parse path uses display names (`"EA App"`) — works only because only Steam/Epic/Standalone anchors are ever created; switch to `GameSourceParser.ToDisplayName(type)`.

**Protection tests:** existing `ExecutableScoringTests`, `ExecutableDiscoveryTests`, `GameSourceParserTests`, `EpicLibraryScannerTests`, `SteamLibraryScannerTests`, `EpicItemWriterTests`, `PythonParityDetectionTests` pin all of 2a–2d; no new tests required unless a branch loses coverage.

---

### Phase 3 — Test fixture deduplication (only if still needed)

**Why:** `GamesDatabaseServiceTests`, `ShellViewModelSearchTests`, `MetadataLookupQueueTests`, `EpicLibraryScannerTests` each construct full `GameEntry` records with overlapping defaults.

**What changes:** add a single test-only `TestGameFactory` (in the App.Tests project) only if the Phase 1–2 production refactor leaves the duplication standing. Do not introduce production abstractions for test setup.

**Success criteria:** (optional) fixture construction is shared; suite green.

**Decision (2026-09-09): NOT NEEDED — DROPPED.** Audit after Phase 1–2: only 3 single constructions remain (ShellViewModelSearchTests, IdentityPipelineTests, EpicLibraryScannerTests, one each); `ShellViewModelSearchTests` already shares the P1b `CreateViewModel` fixture; `GamesDatabaseServiceTests`/`MetadataLookupQueueTests` never construct `GameEntry` directly. Plan gate "only if still needed" → not needed. Closed as N/A.

---

## Success criteria (overall)

- Full solution build: 0 errors.
- `dotnet test`: all existing tests pass unchanged (App 394 + Core 156 + Migration 1 = 551), plus the small Phase 0/1 characterization additions.
- `MainWindow.axaml.cs`, `ShellViewModel.cs`, `FolderScanner.cs` each below ~400 lines (from 1,085 / 1,065 / 526).
- Exactly one construction site per service (verified by repository search); zero duplicated data-path helpers.
- `GameEntryId` first parameter renamed `physicalLibraryRoot`; contract documented in `docs/DATA-FORMAT.md`; IDs pinned by tests; no ID/on-disk format change.
- No metadata-key renames; no user-visible behavior change (characterized for the projection surface by the Phase 1 test).

## Out of scope

- Data migration, on-disk schema changes, metadata-key renames.
- DI framework, plugin/abstraction frameworks.
- New features (filters, views, detection rules).
- Rewriting detection order or store-signal priority (Plan 123 is closed).
- Extracting MainWindow non-UI workflows into coordinator services (deferred pending partial-split outcome).

## Files affected

- **New:** `src/GamingCommander.App/AppDataPaths.cs`, `MainWindow.Status.cs`, `MainWindow.Navigation.cs`, `MainWindow.Launching.cs`, `MainWindow.LibrarySetup.cs`, `MainWindow.Scanning.cs`, `MainWindow.Metadata.cs`, `MainWindow.PlatformRepair.cs`, `MainWindow.Commands.cs`, `src/GamingCommander.App/Services/StoreMetadataEnricher.cs`, `src/GamingCommander.App/Services/TitleEnricher.cs`, `src/GamingCommander.App/Services/ExecutableScoring.cs`, `src/GamingCommander.Core/Models/PlatformMetadataKeys.cs`, `src/GamingCommander.App/Services/CheapExeFinder.cs`.
- **Modified:** `App.axaml.cs`, `MainWindow.axaml.cs`, `ShellViewModel.cs`, `FolderScanner.cs`, `ContainerScanner.cs`, `GameEntryId.cs`, `GameSourceParser.cs`, `ExecutableDiscovery.cs`, `SteamLibraryScanner.cs`, `EpicItemWriter.cs`, `LibrarySetupViewModel.cs`, `docs/DATA-FORMAT.md`, `planning/README.md`, `META/SESSION/NEXT.md`.
- **Tests (minimal):** `GameEntryIdTests.cs`, `SteamLibraryScannerTests.cs`, `GamesDatabaseServiceTests.cs`, `ShellViewModelSearchTests.cs`.

---

## Evaluation (2026-09-08, code-reviewer)

**Verdict: sound-with-changes.** All corrections from the review are folded into the sections above. Summary:

- **Accurate:** file/line references, phase ordering (P0 → composition → projection/UI splits → FolderScanner → container dedup → fixtures), feasibility of constructor injection with a single call site, and the collision risk of anchor-based IDs.
- **Corrected:** test-count figure (551, not 562); the invalid `ComputeId("Steam", f) == ComputeId("GOG", f)` assertion (first arg is a hash input — such a test would fail); missing MainWindow methods in the partial mapping (`QueueSilentMetadataIfStale`, `OnMetadataQueueProgress/ItemCompleted`, `IsUserPinnedTitle`, `TrySteamAppId`, `GetSelectedGame`, `FindGameById`, `RefreshMetadataForGameAsync`, `RefreshCurrentRootAsync`, both `*_PointerPressed` wrappers) and the exact method names (`OpenConfigPath_PointerPressed` etc.); CTS field ownership; `IsNonGameFolder`'s internal `DetectType` call in the container-dedup summary; enricher read-only + ordering guarantees; verifiability of "single construction site" via grep.
- **Residual risks (accepted, mitigated):** FolderScanner enricher result objects could introduce nullable regressions — pinned by existing fixtures; partial-class relocation is mechanical but large — done one file at a time with a build after each; "no user-visible change" is characterized for the projection surface only.

**Recommended execution posture:** implement Phase 0 fully (tests + rename + docs) and stop for a checkpoint; then Phase 1 in three independent commits (composition, projection, MainWindow split); then Phase 2 in two commits (original items, then the 2a–2d round-2 additions); Phase 3 only if fixture duplication remains.

---

## Round-2 review (2026-09-08, second pass over the full source tree)

Follow-up review beyond the original audit. Verdict: original plan stands; the following were **missed** and are now folded in as Phase 2a–2d.

### New findings

- **Missed large file:** `ExecutableDiscovery.cs` is **749 lines** (second-largest in the repo after ShellViewModel/MainWindow) — entirely absent from the original plan. Split scoring from search/parsing (2a).
- **Biggest dedup miss:** metadata keys are raw string literals — **56+ occurrences of ~25 keys across 6+ files** (`ShellViewModel`, `SteamLibraryScanner`, `FolderScanner`, `EpicLibraryScanner`, `EpicItemWriter`, `MainWindow`, `GamesDatabaseService`, `MetadataLookupQueue`). Add `PlatformMetadataKeys` constants (2b). Values must stay byte-identical (persisted data).
- **Triplicated cheap-exe-finder skeleton:** `SteamLibraryScanner.FindPrimaryExe`, `EpicItemWriter.FindLaunchExe`, and `EpicLaunchResolver` each re-implement "root exe + a few relative folders + forbidden-exe filter" (2c).
- **Manually-synced parallel lists in `GameSourceParser`:** `SourceDisplayNames` + `ToDisplayName` + `ParseFromString` must be kept in sync by hand; one latent inconsistency enabled (`LibrarySetupViewModel` stores `type.ToString()` = `"EaApp"`/`"BattleNet"` while the rest of the UI uses display names `"EA App"`/`"Battle.net"`) — currently latent only because only Steam/Epic/Standalone anchors are ever created (2d).

### Checked and intentionally NOT refactored

- `WindowsExplorer.ParentDirectory` vs `Path.GetDirectoryName` — the former is the cross-platform-safe Windows-path helper; the 4 call sites are correct. Not duplication.
- `SteamLibraryScanner.FindPrimaryExe` vs `ExecutableDiscovery.FindPrimaryExecutable` — a documented performance divergence (Steam never lists the whole tree); unify only the shared skeleton (2c), not the strategy.
- `StoreSignalDetector` vs `FallbackSignalDetector` — clean separation (store signals vs low-confidence fallback); no overlap.
- `EpicBinaryManifest` vs `EpicManifestParser` — different formats (binary .manifest vs .item/.mancpn JSON); not duplication.
- `MetadataOnlineGate`/`MetadataLookupQueue` — justified by Plans 119/120; not overengineering.
- No dead `ShellPaneItemViewModel`/`LibraryEntry` properties found (all referenced).

### Local-error sweep results

- No active logic bugs found: `GamesDatabaseService` cache is updated by `Save`; `NavigateInto` refreshes details; `WriteEpicItemAsync`'s `GameFolder` fallback correctly reconstructs the folder; Epic orphan `LibraryRoot` is now physical (previous session fix).
- Minor consistency nit (normalize during projection refactor): `LoadFilteredGames` calls `UpdateDetailsForSelection()` internally, `LoadGamesForLibrary` relies on the caller.
- `MainWindow.OnKeyDown` is `async void` — standard for the Avalonia override; keep as-is; it moves verbatim with the keyboard region.
- `AcfExpectedPath` may contain mixed path separators when computed on a Linux dev box (`Path.Combine` uses `/`); Windows-target runtime is unaffected. Noted, not a defect for the shipped target.