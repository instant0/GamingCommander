# META/CODE_MAP.md — Codebase Reference

> **How it connects →** [`CONNECTION_MAP.md`](CONNECTION_MAP.md) — composition root, pipelines, per-store
> decision trees, write and egress boundaries.

**Nature:** Reference. Updated by Builder after structural changes.
**Audience:** Builder, Reviewer. Read before modifying code.

---

## Solution Structure

```
GamingCommander.sln
├── src/
│   ├── GamingCommander.Core/        Interface definitions + domain models + shared helpers
│   ├── GamingCommander.Detection/   Game discovery abstractions + design-time stub
│   ├── GamingCommander.UI/          ViewModels (Norton Commander shell)
│   └── GamingCommander.App/         Avalonia app entry, windows, services, DI wiring
└── tests/
    ├── GamingCommander.Core.Tests/
    ├── GamingCommander.Detection.Tests/
    └── GamingCommander.App.Tests/
```

**Dependency flow (declared):** Core ← Detection ← UI ← App.

**Dependency flow (actual):** Scanning, launch, and persistence live in `GamingCommander.App`. `GamingCommander.Detection` is a deprecated 3-line stub. `Detection.Tests` has no tests.

---

## Core Interfaces (GamingCommander.Core)

| Interface | File | Key Members |
|-----------|------|-------------|
| `IGame` | `IGame.cs` | `Id`, `Title`, `Source`, `InstallPath`, `ExecutablePath`, `LaunchTarget`, `LastModified` |
| `IConfigService` | `IConfigService.cs` | `Load()` → `AppConfig`, `Save(AppConfig)` |
| `ILibraryManager` | `ILibraryManager.cs` | `Libraries`, `GetGamesForLibrary()`, `UpsertLibrary()`, `RemoveLibrary()`, `ScanFolder()`, `RescanLibrary()`, `SelectScannerAndScan()`, `UpdateGameEntry()`, `DeleteGameEntry()`, `RetagGame()` |
| `IGamesDatabaseService` | `IGamesDatabaseService.cs` | `Load()` → flat `GamesDatabase`, `Save()`, `GetGamesForLibrary()`, `SetGamesForLibrary()`, `RemoveGamesForLibrary()`, `UpdateGameEntry()`, `DeleteGameEntry()`, `RetagGame()` |
| `ILibrariesService` | `ILibrariesService.cs` | `Libraries`, `Upsert(Library)`, `Remove(name)`, `Get(name)` — owns `libraries.json` (Plan 124) |
| `ICollectionsService` | `ICollectionsService.cs` | `Collections`, `Upsert(Collection)`, `Remove(name)`, `Get(name)` — named collections (Plan 127 B12) |
| `IMetadataStore` | `IMetadataStore.cs` | Get/Upsert sidecar by game id. Must not touch `games.json`. |
| `IMetadataService` | `IMetadataService.cs` | `RefreshAsync` + `SearchPagesAsync` (F3 picker) |

---

## Domain Models (GamingCommander.Core/Models)

| Model | Kind | Key Fields |
|-------|------|------------|
| `GameSourceKind` | enum | `Unknown=0, Standalone=1, Steam=2, Gog=3, Epic=4, EaApp=5, UbisoftConnect=6, BattleNet=7, Xbox=8, Rockstar=9, SteamEmu=10` |
| `GameEngineKind` | enum | `Unknown=0, UnrealEngine=1, Unity=2, Rage=3, Frostbite=4, Source=5, Godot=6, CryEngine=7` |
| `GameMetadataRecord` | record | Sidecar extras + optional `Details` catalog. Fetched enrichment only; `Fetched*` fields are not authoritative (Plan 127 A6). Not part of `GameEntry`. |
| `GameMetadataDetails` | record | PCGW paths, cmdline catalog, fixes, video, cloud. |
| `LaunchArgumentComposer` | static | Toggle/combine extras; Steam URI does not eat flags. |
| `GameLaunchResolver` | static | GameEntry → process target + args. |
| `MetadataSource` | enum | `Unknown, Steam, Pcgw, EpicLocal` |
| `FileSystemEntryKind` | enum | `Directory=0, File=1, ParentDirectory=2` |
| `TagType` | enum | `User=0, Store=1, Engine=2` |
| `GameRecord` | record (implements IGame) | Id, Title, Source, InstallPath, LaunchTarget, ExecutablePath, LastModified, SupportsPointerInteraction, SupportsKeyboardOnlyFlow |
| `GameEntry` | record | Composed of `Identity` (IdentityInfo), `Location` (LocationInfo), `Launch` (LaunchInfo) + `Detection`, `Store`, `Acf`, `Epic`, `Install`, `Tracking`, `UserState`, `Relations`, `Tags`, `UserOverrides`, `GameEngine`, `LastScanned`, `LastModified`. Flat names (`Id`, `DisplayName`, `ExecutablePath`, …) are read-only accessors — Plan 127 A1. Status/ACF facts live in `Acf` (`SteamAcfInfo`) and `Install` (`InstallInfo`); no `Extra` bag exists after Plan 127 A2 |
| `IdentityInfo` | record | Id, Library (anchor), DisplayName, GameSource, IsSourceOverridden — Plan 127 A1 |
| `LaunchInfo` | record | ExecutablePath, LauncherPath, CommandLineArguments, ExtraLaunchArguments, ManifestPath — Plan 127 A1 |
| `DetectionInfo` | record | Detection provenance: Tier, TitleSource, AutoDetectedTitle, PeFileDescription, ManifestKind, Origin (`DetectionOrigin?`), Signals[], CandidateExes[], IdentifiedAt — Plan 127 A3 |
| `DetectionOrigin` | enum | `StoreManifest=0, FileScan=1, User=2, Merged=3` — high-level source path (Plan 129 §3.3) |
| `StoreInfo` | record | StoreKey (`"<GameSourceKind>:<storeId>"`), SteamAppId, GogGameId, EpicCatalogNamespace, EpicCatalogItemId, EpicAppName, Studio, EaGameName, EaContentId, BlizzardProduct, UbisoftReadme, UbisoftGameId, XboxPackageFamily, RockstarTitleId — Plan 127 A2 / Plan 129 |
| `SteamAcfInfo` | record | Status, FilePath, LibraryPath, ExpectedPath, BuildId, SizeOnDisk, StateFlags — Plan 127 A2 |
| `EpicInfo` | record | Status, ItemPath — Plan 127 A2 |
| `LocationInfo` | record | FolderPath, FolderName, LibraryRoot, ActualLibraryRoot, GameFolder — Plan 127 A1/A2 |
| `InstallInfo` | record | State (`InstallState`), Version, UpdateAvailable, StoreReportedState, LastStoreSeenUtc, LastVerified — Plan 127 B8 / Plan 129 |
| `InstallState` | enum | Unknown=0, Installed=1, Owned=2, Moved=3, Missing=4, Orphaned=5, Updating=6 — Plan 127 B8 |
| `StoreReportedState` | enum | Unknown=0, Installed=1, UpdateRequired=2, Incomplete=3 — store evidence, separate from disk `InstallState` (Plan 129 §3.4) |
| `LauncherKinds` | static class | `All` (Steam, Epic, Gog, EaApp, UbisoftConnect, BattleNet, Xbox, Rockstar), `IsLauncher(GameSourceKind)` — no duplicate launcher enum (Plan 129) |
| `LauncherDiscoverySetting` | enum | `Unset=0, Automatic=1, Selected=2, FileOnly=3` — the Setup choice for launcher reads (not the display mode) |
| `StorePolicy` | record | Kind (`GameSourceKind`), IndexEnabled, CheckOnLaunch, EnrichNewGames, SkipUnchangedManifests (default true), InventoryPathOverride |
| `StoreInventoryEntry` / `SteamInventoryMetadata` / `GogInventoryMetadata` / `EpicInventoryMetadata` / `RockstarInventoryMetadata` | records | Store-native inventory row: Kind, StoreId, Name, InstallPath, LaunchExe, StoreReportedState; disk facts `FolderExists`/`ExeExists`; typed metadata (ACF path/state flags, GOG build/install date/launch args, Epic catalog ids/guid/version/size/manifest paths, Rockstar title id/rgl path/registry key). Orphan/unknown-ID rows carry an empty StoreId |
| `StorePolicySettings` | record | Stores (`IReadOnlyList<StorePolicy>`) |
| `TrackingInfo` | record | FirstSeen, LastPlayed, LaunchCount, PlayTimeSeconds, LastSessionSeconds — Plan 127 B7 |
| `UserStateInfo` | record | Favourite, Hidden, Pinned, Archived, Rating, Notes, Completion — Plan 127 B9 |
| `CompletionState` | enum | None=0, Backlog=1, Playing=2, Completed=3, Abandoned=4 — Plan 127 B9 |
| `LaunchProfile` | record | Name, ExecutablePath, Arguments, Environment, WorkingDirectory, RunAsAdmin, Engine — Plan 127 B10 |
| `RelationsInfo` | record | Kind (`GameKind`), ParentId, AddOnIds — Plan 127 B11 |
| `GameKind` | enum | Game=0, Dlc=1, Tool=2, Demo=3, Soundtrack=4, Mod=5 — Plan 127 B11 |
| `Tag` | record | Name, Kind (`TagKind`), Color, Auto — Plan 127 B13 |
| `TagKind` | enum | User=0, Store=1, Engine=2, Genre=3 — Plan 127 B13 |
| `Collection` | record | Name, GameIds[], SmartQuery — Plan 127 B12 |
| `IdentificationTier` | enum | Unknown=0, Candidate=1, Secure=2, Locked=3 — Plan 127 A3 |
| `IdentityKey` | record | Kind, Value — natural key for move/rename reconciliation (Plan 127 A5) |
| `Library` | record | Name (anchor key), Type (`GameSourceKind`), Folders (IReadOnlyList\<string\>) — Plan 124 |
| `GamesDatabase` | record | Games (flat List\<GameEntry\>); each links to a `Library` by name — Plan 124 |
| `AppConfig` | record | HiddenFolders, IsFirstRun, LastSeenVersion, EnableOnlineMetadata, ScanDepth, StorePolicies (`StorePolicySettings?`), `LauncherDiscovery` (`LauncherDiscoverySetting`, default Unset), `SelectedLaunchers` (`IReadOnlyList<GameSourceKind>`, default empty) |
| `FileSystemEntry` | record | Name, FullPath, Kind, LastModified, Size |
| `GameSourceParser` | static class | `InferFromPath(string)`, `ParseFromString(string)`, `AvailableTypes` — shared by all ViewModels |

---

## Core Helpers (GamingCommander.Core/Services)

| Helper | File | Purpose |
|--------|------|---------|
| `VdfParser` | `VdfParser.cs` | Minimal VDF/ACF key-value parser for Steam manifest files |
| `GameEntryId` | `GameEntryId.cs` | Deterministic MD5-based ID generation for GameEntry records |
| `GameIdentity` | Core `Services/GameIdentity.cs` | Plan 127 A5: rebuilds `IdentityKeys` from folder name + store ids |
| `InstallStateMap` | Core `Services/InstallStateMap.cs` | Plan 127 B8: store status string → `InstallState` |
| `GameStoreIndex` | `Core/Services/GameStoreIndex.cs` | Plan 129 §3.3: read-only in-memory index (`ByStoreKey`, normalized `ByPath`, `ByStore`, duplicate keys, missing manifest keys); no I/O |
| `PathText` | `Core/Services/PathText.cs` | Plan 130: the single path rule — `TrimTrailingSeparators` (real paths), `NormalizeForCompare`, `Equals`, `IsUnder`, `Leaf`. Opaque Windows strings, no I/O, no case rewriting of stored values |
| `InstallIdentity` | `Core/Services/InstallIdentity.cs` | Identity seams: `PathKey` (full path is the only folder identity) and `WeakFolderKey` (store-scoped folder name for the unique relocation hint only) |
| `LauncherDiscoveryService` / `ILauncherLocator` | Core `Services/LauncherDiscoveryService.cs`, `ILauncherLocator.cs` | Read-only launcher probes; one row per `LauncherKinds.All`, missing/throwing locator is `Unsupported`; `Supports(kind)` reports registered locators; `DiscoverForPolicy(policy)` probes only allowed kinds (none in FileOnly/Unset) |
| `LauncherDiscoveryPolicy` | Core `Services/LauncherDiscoveryPolicy.cs` | Pure setting matrix: `AllowsDiscovery(kind)`, `AllowsRegistryFallback`, `AllowsEpicCatalog`, `AllowsStorePolicies`; no I/O, no cache |
| `SteamLauncherLocator` / `EpicLauncherLocator` / GOG / Ubisoft | `Services/LauncherLocators.cs` | Steam: valid library first (configured roots; `steamapps` + vdf/common/ACF), registry fallback only when none; Epic: ProgramData manifests with at least one `.item`; GOG: Galaxy path, `NeedsVerification`; Ubisoft: `Installs` registry list, `NeedsVerification`. Registered in `App.LauncherDiscovery` |
| `IStorePolicyProvider` | Core `Services/IStorePolicyProvider.cs` | Per-store policy reads/writes, `EnabledStores`, `CheckOnLaunchStores`, `CanImport`, `CanSync` |
| `IStoreInventoryReader` / `StoreManifestFingerprint` | Core `Services/IStoreInventoryReader.cs` | Read-only store inventory contract: `Kind`, last-read `Fingerprint`, one `Read()` returning `StoreInventoryEntry` rows; no writes and no recursive scan |
| `PeProductYear` | `PeProductYear.cs` | Year hint from PE FileVersion / LastWriteTime |

---

## UI ViewModels (GamingCommander.UI/ViewModels)

| ViewModel | File | Purpose |
|-----------|------|---------|
| `ReactiveObject` | `ReactiveObject.cs` (25 L) | Base INotifyPropertyChanged with `SetProperty<T>` |
| `ShellViewModel` | `ShellViewModel.cs` (178 L) + 9 domain partials | Dual-pane shell: navigation, details, status bar, command bar. Partials (Plan 125 final stage): `ShellViewModel.Navigation.cs` (roots/drill/reload/retag), `.Filtering.cs` (F8 filter + option list), `.Search.cs` (Plan 122 type-to-search), `.Loading.cs` (library/filter loaders + shared `ToPaneItem` projection), `.Scanning.cs` (F5 scan badges), `.Details.cs` (right-pane surface + refresh batch), `.Details.Status.cs` (Orphaned/Missing/Moved detail text), `.Details.Metadata.cs` (sidecar extras + config/save/args/video), `.Tags.cs` (tag badges + store badge map) |
| `ShellPaneItemViewModel` | `ShellPaneItemViewModel.cs` | Item model: Title, SourceLabel, PathSummary, Kind, IsBrowsable, GameId, PlatformId, PlatformStatus, PlatformStatusColor, PlatformStatusDetail, ItemStatusColor, HasGameSelected, Tags, TagBadges |
| `ShellCommandViewModel` | `ShellCommandViewModel.cs` (8 L) | Hotkey + Label for command bar |
| `TagBadgeViewModel` | `TagBadgeViewModel.cs` | Tag badge with configurable colors (Name, Background, Foreground) |
| `ITagColorProvider` | `ITagColorProvider.cs` | Interface for tag color lookup (decouples UI from App) |

### ShellViewModel Key Methods

- `JumpToLibraryRoots()` — populate item list from configured roots (Navigation partial)
- `LoadGamesForLibrary(string)` — populate item list from a root's game entries; maps SteamStatus to PlatformStatusColor, PlatformStatusDetail, and ItemStatusColor; filter rows share the `ToPaneItem` projection (Loading partial)
- `NavigateInto()` — drill into selected item (root or "..") or launch game
- `NavigateUp()` — go up one level (root list or no-op)
- `RetagSelected(GameSourceKind)` — update game source type
- `AppendSearchChar(char)` / `SearchBackspace()` / `CancelSearch()` — Plan 122 type-to-search buffer; threshold `ShellViewModel.SearchThreshold` (3); live query shown in `LeftPaneTitle`
- `ApplyFilter(GameFilter)` / `ClearFilter()` / `CollectFilterOptions()` — F8 cross-library filter (Filtering partial)
- `Reload()` — refresh current view
- `SetScanning` / `ClearScanning` — F5 background-scan state + root badges (Scanning partial)
- `HasGameSelected` — true when a game file (not directory) is selected

### ShellPaneItemViewModel.IsBrowsable

```csharp
IsBrowsable => Kind is FileSystemEntryKind.Directory or FileSystemEntryKind.ParentDirectory;
```

Game entries use `Kind = File` → not browsable. Library roots use `Kind = Directory` → browsable.

---

## App Services (GamingCommander.App/Services)

| Service | File | Purpose |
|---------|------|---------|
| `LibraryManager` | `LibraryManager.cs` | Routes scanning to appropriate scanner, manages roots, delegates to IGamesDatabaseService; takes `LauncherDiscoveryContext` and skips the Epic ProgramData catalog when the policy forbids it (FileOnly falls through to `FolderScanner`) |
| `LauncherDiscoveryContext` | `Services/LauncherDiscoveryContext.cs` | The one runtime launcher discovery setting + selected launchers; built from config at startup, updated by Setup, exposes a policy snapshot per scan. No startup probe |
| `MetadataStore` | `Metadata/MetadataStore.cs` | Plan 119 sidecar `data/games_metadata.json` |
| `MetadataService` | `Metadata/MetadataService.cs` | PCGW then Steam Store; `EnableOnlineMetadata`; 60-day cache; identity guard; never writes `games.json` |
| `SteamStoreLookup` | `Metadata/SteamStoreLookup.cs` | Raw `appdetails` GET (injectable HttpClient) |
| `SteamStoreParser` | `Metadata/SteamStoreParser.cs` | Envelope → `SteamStoreFacts` |
| `PcgwLookup` | `Metadata/PcgwLookup.cs` | appid.php / OpenSearch / Parse / `pageTitleOverride` — **not Cargo** |
| `PcgwInfoboxParser` | `Metadata/PcgwInfoboxParser.cs` | HTML title, OpenSearch, Infobox wikitext |
| `PcgwSectionParser` | `Metadata/PcgwSectionParser.cs` | Plan 120: paths, cmdline, Fixbox, Video, cloud |
| `PcgwLookup.LookupAsync` | `Metadata/PcgwLookup.cs` | Infobox + sections from one Parse payload |
| `MetadataLookupQueue` | `Metadata/MetadataLookupQueue.cs` | F5 enqueues; one game at a time; sidecar only |
| `PcgwPathTokens` | Core `PcgwPathTokens.cs` | `{{P|…}}` → Windows env tokens (display only) |
| `TitleSourceValues` | Core `Models/TitleSourceValues.cs` | `const string` values for `DetectionInfo.TitleSource` (GogInfo/EaInstallLog/EpicItemManifest/FolderExeMatch/PeFileDescription/UbisoftReadme/PcgwPick/UserOverride) |
| `SteamAcfFields` | Core `Models/SteamAcfFields.cs` | Plan 125 final stage: `const string` names of the Steam ACF / libraryfolders.vdf key=value format, shared by `SteamAcfParser` + `SteamAcfWriter`, pin-tested |
| `EpicItemSchema` | Core `Models/EpicItemSchema.cs` | Plan 125 final stage: `const string` JSON member names of the Epic .item/.mancpn format, shared by `EpicManifestParser` + `EpicItemWriter` (writer/reader drift = compile error), pin-tested. Write-only .item members (FormatVersion, ChunkDbs, …) stay literals in `EpicItemWriter` — no reader, no drift surface |
| `PcgwTitleFilter` | Core | Reject soundtrack/demo/artbook; `PickBest` + year |
| `GameFilter` / `GameFilterMatcher` | Core | F8 tag / store / wildcard match |
| `TitleText` | Core | Strip ®/™; camelCase split; letters+digits exe match |
| `MetadataDetailsFormatter` | Core | Right-pane path/arg/video strings |
| `CommonMetadataParser` | `Metadata/CommonMetadataParser.cs` | Source facts → `GameMetadataRecord` |
| `EngineDetector` | `EngineDetector.cs` | Local engine probe (Unreal / Unity / RAGE / Frostbite) — Plan 102 Phase 2 |
| `FolderScanner` | `FolderScanner.cs` | Generic folder scanner: detection chain, exe noise filtering, container detection, nested Steam exclusion (`IsNestedSteamTree`). Entry construction delegated to `StoreMetadataEnricher` / `TitleEnricher` (Plan 125 Ph 2) — `AddGameEntry` is a thin assembler. Optional `allowRegistryFallback` gate; Pass 1c builds the detector lazily, so a false gate performs zero registry reads |
| `ContainerScanner` | `ContainerScanner.cs` | Container/publisher folder scan: bounded depth-2 recursion, org detection, `IsNonGameFolder` 3-layer suppression. Children classified once per call via `SignalSummary` (Plan 125 Ph 2) |
| `StoreMetadataEnricher` | `StoreMetadataEnricher.cs` | Plan 125 Ph 2: GOG→EA→Epic→BattleNet enrichment blocks extracted from `FolderScanner.AddGameEntry`; operates on `GameEntryBuildContext`; read-only |
| `DetectionTierDeriver` | `DetectionTierDeriver.cs` | Plan 127 A3: derives `DetectionInfo` (tier + signals + manifest kind) from a scanner's collected signals |
| `TitleEnricher` | `TitleEnricher.cs` | Plan 125 Ph 2: FolderExeMatch → PE FileDescription (with guards) → Ubisoft readme title rules; read-only |
| `StoreSignalDetector` | `StoreSignalDetector.cs` | DetectType + 10 store/platform signal checks (GOG, EA, Ubisoft, Epic, Blizzard, Xbox, Rockstar, Steam) |
| `ExecutableDiscovery` | `ExecutableDiscovery.cs` (+ `ExecutableDiscovery.Scoring.cs` partial) | Deep exe search, primary exe selection, launcher detection, Epic manifest discovery. Scoring half (`ScoreExecutable`, `RomanNumeralBonus`, `AbbreviationBonus`, `ExeScoreResult`) in the `.Scoring.cs` partial (Plan 125 Ph 2a) |
| `SteamLibraryScanner` | `SteamLibraryScanner.cs` | Dedicated Steam scanner: ACF cross-ref, library path discovery, Installed/Moved/Orphaned/Missing; skips Steam-internal `common/` folders (Bug 10); cheap exe picking via `CheapExeFinder` |
| `CheapExeFinder` | `CheapExeFinder.cs` | Plan 125 Ph 2c: root-exe-first + relative-folder cheap finder shared by Steam scanner and Epic `.item` writer (per-caller forbidden-exe skips) |
| `EpicLibraryScanner` | `EpicLibraryScanner.cs` | ProgramData `*.item` → VFS (base games only); claims and matches by full install path |
| `EpicItemCatalog` / `EpicItemClassifier` | same folder | Catalog and DLC skip; read-only |
| `SteamAcfWriter` / `EpicItemWriter` / `RepairWriteGuard` | same folder | Repair writes: Steam creates an absent ACF, Epic modifies an existing `.item` (never creates/replaces); shared preflight, staging, copy, length check, backup/restore, journal |
| `EpicStoreSearch` | `EpicStoreSearch.cs` | Deferred **identity lookup** (title/namespace/catalog id), no callers in v1. Two uses: identify a game with missing/incomplete local records; resolve the title before PCGW/Steam. Not a metadata source and not an `.item` generator. |
| `SteamAcfParser` | `SteamAcfParser.cs` | Parses Steam ACF files and libraryfolders.vdf; AcfInfo record |
| `SteamInventoryReader` | `SteamInventoryReader.cs` | ACFs across configured roots + VDF-discovered libraries, keyed by AppID; ACF-present/absent and orphan `common` folders; `StateFlags` 4/6; `apps` blocks ignored |
| `GogInventoryReader` | `GogInventoryReader.cs` | `GOG.com\Games` registry inventory plus merged `goggame-*.info`; registry ID kept without `.info`; `.info` exe preferred when present; missing folders still returned |
| `EpicInventoryReader` | `EpicInventoryReader.cs` | ProgramData `*.item` catalog (playable base games) with optional guid/version/size/manifest fields; local `.mancpn` + binary `.manifest` fallback for unclaimed folders; missing folders retained; duplicate installs collapsed |
| `RockstarInventoryReader` | `RockstarInventoryReader.cs` | Per-game `Rockstar Games` registry keys (Launcher skipped) plus safe `title.rgl` title ID; missing folders retained; no `Settings.json`, no exe guessing |
| `GamesDatabaseService` | `GamesDatabaseService.cs` | JSON-file CRUD for the flat game list (`games.json`) via private DTOs, in-memory cache, anchor-keyed access (Plan 124) |
| `LibrariesDatabaseService` | `LibrariesDatabaseService.cs` | Reads/writes `libraries.json` (named `Library` anchors) implementing `ILibrariesService` (Plan 124) |
| `CollectionsDatabaseService` | `CollectionsDatabaseService.cs` | Reads/writes `collections.json` implementing `ICollectionsService` (Plan 127 B12) |
| `JsonConfigService` | `JsonConfigService.cs` | JSON-file persistence for AppConfig, including per-store `StorePolicies` and the launcher discovery setting/selection (enum names as strings; existing files without the field default to Automatic) |
| `LauncherPolicyViewModel` / `LauncherPolicyRow` / `LauncherDiscoveryOption` / `LauncherSelectionOption` | `ViewModels/LauncherPolicy*.cs`, `LauncherDiscoveryOptions.cs` | Launcher discovery setting + selected checklist + preview/policy rows; one probe per explicit Detect, no I/O on construction; setting/selection persist immediately through `IConfigService` and update `LauncherDiscoveryContext`; FileOnly disables rows without clearing policies; Index requires an inventory reader |
| `StorePolicyProvider` | `StorePolicyProvider.cs` | Now constructed in `App`; single provider shared by first-run and F2 setup |
| `BlacklistLoader` | `BlacklistLoader.cs` | Loads noise patterns from `data/blacklist.json`; missing/corrupt file falls back to embedded resource and restores disk copy (Bug 16) |
| `DetectionNoise` | `DetectionNoise.cs` | Small detection noise sets (`NonGameExe`, `LnkSkip`, `SteamCommonNoise`) shared by `FolderScanner`, `ContainerScanner`, `ExecutableDiscovery`, `LnkParser`, and `SteamLibraryScanner`; separate from the tiered `data/blacklist.json`, which drives exe scoring |
| `FileSystemHelper` | `FileSystemHelper.cs` | Shared filesystem utilities: GetDirectoriesSafe, GetFilesSafe, GetLastWriteTimeSafe, NormalizeDisplayName, NoiseSubDirNames |
| `JsonDocumentStore` / `AppPaths` | `Services/JsonDocumentStore.cs`, `AppPaths.cs` | Shared JSON store: four-state read, rolling `.bak`, `.corrupt` preservation, verified single write; app-data root + parent-directory guard |
| `HelpDialogBuilder` | `HelpDialogBuilder.cs` | Builds and shows the help dialog with keyboard shortcuts |
| `PickPcgwPageWindow` | `PickPcgwPageWindow.cs` | F3: choose among several PCGW titles |
| `FilterWindow` | `FilterWindow.cs` | F8 / S: tag, store, wildcard |
| `TagColorService` | `TagColorService.cs` | Reads tag_colors.json, maps tag→color by type (User/Store/Engine), implements ITagColorProvider |
| `LibrarySetupViewModel` | `.App/ViewModels/LibrarySetupViewModel.cs` | Library root setup dialog logic (F2 + first-run); launcher discovery card/checklist visibility, Full rescan action |

### FolderScanner Key Logic

- `Scan(rootPath, defaultType)` → 10-signal priority-ordered detection for GOG, EA, Ubisoft, Epic, Blizzard, Xbox, Rockstar, Steam, Steam Emu
- Deep executable discovery (root → child → Binaries/Win64/ → Binaries/WinGDK/)
- Executable scoring (folder-token match +10, launcher -20, `game.exe` +18, exact/suffix name match +40 — suffix match via platform-token strip, see ADR-012; `*-Win64-Shipping.exe` +28 + `\Binaries\Win64\` +12, org-copy -25, filesize)
- Container detection (parent with no signals, child has signals → promote child)
- Nested Steam trees (`steamapps/common` or folder name `steam`) are skipped — not scanned via `SteamLibraryScanner`
- Uses `GameEntryId.Compute()` and `GameSourceParser` from Core

### SteamLibraryScanner Key Logic

- `Scan(rootPath)` / `ScanAll()` → scans steamapps/common/, cross-references ACFs from all libraries
- ACF map is keyed by full install path (`InstallIdentity.PathKey`), never by the leaf `installdir`; two libraries can hold different games in same-named folders
- `DiscoverLibraryPaths()` — parses libraryfolders.vdf for additional Steam library paths
- Detects Installed/Moved/Orphaned/Missing status via ACF cross-referencing; a single ACF in another library whose folder is gone yields Moved (two candidates are ambiguous → Orphaned)
- "Missing" detection: iterates all ACFs, checks if each installdir exists in any library's common/
- "Moved" games set `Acf.ExpectedPath` and `Location.ActualLibraryRoot` for cross-library context
- Stores status/ACF facts in `Acf` (`SteamAcfInfo`) and install state in `Install` (`InstallInfo`); no `Extra` dict exists
- Skips known non-game `common/` children (`Steam Controller Configs`, `Steamworks Shared`); never skips a game named `steam`
- Exe: root `*.exe`, else only named folders (`Shipping`, `Binaries\Win64`, `bin`…). No tree walk. Lookup uses ACF name/AppID if exe is empty.
- Uses `GameEntryId.Compute()` from Core

---

## Windows (GamingCommander.App)

| Window | AXAML (lines) | Code-behind (lines) | Purpose |
|--------|--------------|---------------------|---------|
| `MainWindow` | — | — | Dual-pane shell, keyboard handlers, command bar, details panel. Split into partials (Plan 125 Ph 1c): `MainWindow.Status.cs` (status bar + auto-clear), `MainWindow.Navigation.cs` (keys/type-to-search/double-click), `MainWindow.Launching.cs`, `MainWindow.LibrarySetup.cs` (F2/F4/F8 dialogs), `MainWindow.Scanning.cs` (F5 rescan + `_scanCts`), `MainWindow.Metadata.cs` (F3 lookup queue + `_metadataCts`), `MainWindow.PlatformRepair.cs` (Epic .item / Steam ACF writes, folder-open), `MainWindow.Commands.cs` (command bar). Base file = ctor + wiring only. |
| `LibrarySetupWindow` | 31 L | 140 L | Library root setup (F2 + first-run onboarding) |
| `GameSetupWindow` | 19 L | ~225 L | F4 game editing |

### MainWindow Key Handlers (in `OnKeyDown`)

- `Up/Down` — navigation
- `Enter` → `NavigateInto()` (launches games, drills into directories)
- `Backspace` / `Esc` → `NavigateUp()`
- `F1` → Help dialog
- `F2` → LibrarySetup dialog
- `F3` → force online extras lookup; picker if several PCGW pages
- `F4` → GameSetup dialog (name, type, exe, PCGW arg catalog). Does not wait for HTTP.
- `F5` → Rescan current root or all roots (async; press again to cancel). Then enqueue metadata if Online.
- `F8` → filter (tags, store labels, wildcard). Backspace clears.
- Type-to-search (Plan 122): unmodified printable keys append to a live query; at 3+ chars applies cross-root wildcard filter over names + tags. Backspace erases (below threshold returns to roots), Esc cancels. Bare `S`/`T` bindings removed; opening the F8 dialog or clicking a tag badge ends the typed session.
- `F10` → Close()
- No F9 (Esc / Backspace goes up to roots)

### MainWindow Key Events

- `NavigationChanged` → `Focus()` on LeftListBox, `ScrollIntoView`
- `PropertyChanged(SelectedIndex)` → `ScrollIntoView`
- `LeftListBox_DoubleTapped` → launches games, drills into directories
- `CommandButtonPressed` → maps `Tag` hotkey to handler

---

## Shared Data Files (data/)

| File | Purpose |
|------|---------|
| `blacklist.json` | Aggregated noise patterns for exe names, directory names, PE metadata defaults, PCGW page title noise. Consumed by FolderScanner and SteamLibraryScanner. |
| `tag_colors.json` | Configurable tag badge colors: default, store-specific (Steam, GOG, Epic, etc.), engine-specific (Unreal, Unity, etc.). Consumed by TagColorService. |
| `collections.json` | Named collections (game id lists + smart-query definitions). Read/written by `CollectionsDatabaseService`. |

## Existing Python Tools (tools/)

> **Note:** `detect.py` is the unified replacement for `detect_folder.py` and `list_standalone_games.py`. Deprecated tools are retained for reference only.

| Tool | Purpose | Status |
|------|---------|--------|
| `detect.py` | Unified game detection tool: 4-phase scan, 9 store signals, engine detection, GOG metadata | ✅ Primary |
| `parse_steam_acf.py` | Parse Steam ACF files, extract identification + migration fields | ✅ Validated |
| `list_standalone_games.py` | Three-tier classification of standalone game folders | ⚠️ Deprecated — use detect.py |
| `discover_steam_libraries.py` | Find Steam libraries via registry + libraryfolders.vdf | ✅ Exists |
| `list_steam_common.py` | List Steam common folders cross-referenced with ACFs | ✅ Exists |
| `detect_folder.py` | Detect launcher type from folder contents | ⚠️ Deprecated — use detect.py |
| `validate_steam_libraries.py` | Validate Steam library structure | ✅ Exists |
| `decode_manifest.py` | Parse Epic .manifest binary + generate .item files | ✅ Exists |
| `parse_manifest.py` | Simpler Epic .manifest parser | ✅ Exists |
| `epic_search.py` | Query Epic API for namespace/catalog metadata | ✅ Exists |
| `setup_mock_data.py` | Generate mock Windows game folder tree | ✅ Exists |
| `generate_mock_registry.py` | Generate mock .reg files for 5 launchers | ✅ Exists |
| `parse_registry.py` | Parse .reg files, extract launcher paths | ✅ Exists |
| `lookup_metadata.py` | Multi-source metadata lookup (store ID → PCGW Cargo/Parse → PE scan) | ✅ Exists |
| `probe_steam_store.py` | Live Steam Store `appdetails` probe (Plan 102 pri 1). Public AppIDs only | ✅ Exists |
| `probe_pcgw.py` | Live PCGW probe: Cargo denied; OpenSearch + Parse work | ✅ Exists |

---

## Test fixtures (`testdata/`) — not shipped

```
testdata/mock/
├── steam/steamapps/
│   ├── appmanifest_12345.acf          (Mock Game Alpha)
│   ├── appmanifest_67890.acf          (Mock Game Beta)
│   ├── common/MockGameAlpha/          GameAlpha.exe + steam_appid.txt
│   ├── common/MockGameBeta/           GameBeta.exe + GameBetaLauncher.exe + steam_appid.txt
│   └── libraryfolders.vdf
├── epic/EpicGameGamma/
│   ├── GameGamma.exe
│   └── .egsstore/manifests/abc123.item
├── standalone/
│   ├── StandaloneGameDelta/           GameDelta.exe + launcher exe
│   ├── SteamEmuEpsilon/               GameEpsilon.exe + steam_api64.dll
│   ├── AntiCheatZeta/                 GameZeta.exe + easyanticheat_setup.exe
│   ├── _installer/                    setup.exe + vcredist_x64.exe (non-game)
│   ├── redist/                        dxwebsetup.exe + oalinst.exe (non-game)
│   ├── documentation/                 readme.txt only (no exe, excluded)
│   └── PublisherCollection/SubGameEta/ GameEta.exe + steam_appid.txt
└── registry/
    ├── steam.reg / ea.reg / epic.reg / gog.reg / ubisoft.reg
    └── *.reg.txt (UTF-8 copies for Linux)
```

---

## Test Coverage

**Current baseline:** 587 pass / 0 fail. `META/SESSION/CURRENT.md` is the source for the live count; this table is a pointer, not a running test log.

| Project | Tests |
|---------|-------|
| `Core.Tests` | 157 |
| `App.Tests` | 430 |
| **Total** | **587** |

---

## Research Docs (docs/research/)

| Doc | Content | Status |
|-----|---------|--------|
| `steam_acf_schema.md` | Steam ACF required fields for identification + migration | ✅ Done |
| `steam_vdf_schema.md` | libraryfolders.vdf structure for discovery | ✅ Done |
| `steam_common_schema.md` | common/ folder cross-reference approach | ✅ Done |
| `standalone_schema.md` | Three-tier classification for folder detection | ✅ Done |
| `ea_format.md` | EA App format documentation | ✅ Done |
| `gog_format.md` | GOG format documentation | ✅ Done |
| `epic_item_format.md` | Epic .item JSON format + GraphQL API | ✅ Done |
| `ubisoft_format.md` | Ubisoft format documentation | ✅ Done |
| `pcgamingwiki_notes.md` | PCGamingWiki research notes | ✅ Done |
| `launcher_discovery.md` | Launcher registry discovery notes | ✅ Done |

## Detection Logic Docs (docs/detection/) — primary reference (2026-09-07)

Fresh, code-accurate detection documentation. Start at `docs/detection/01-overview.md`.

| Doc | Content |
|-----|---------|
| `README.md` | Index of the detection docs |
| `01-overview.md` | Decision pipeline (gates T1/T1.5/T2/C1/C2/N1/D1), working-set reduction, confidence tiers |
| `02-noise-filtering.md` | Noise exe/dir tiers, `epicgames` special case, non-game names, parent-bound dirs |
| `03-store-signals.md` | 9-store priority chain, BattleNet `.build.info`, tier preservation |
| `04-containers.md` | Container/collection/publisher-wrapper logic + fixes + dropped P3c-2 |
| `05-executable-discovery.md` | Discovery order, terminating rule, scoring, parent-bound promotion |
| `06-deep-scan-fallbacks.md` | Deep scan, engines, .lnk, GOG metadata, non-game layers, UE-aware filter |
| `07-csharp-parity.md` | C# gaps, fixes to port, port order |
| `08-validation.md` | Verification tools, green results, fixtures, corpora, regression rules |
