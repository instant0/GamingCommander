# META/CODE_MAP.md — Codebase Reference

**Nature:** Reference. Updated by Builder after structural changes.
**Audience:** Builder, Reviewer. Read before modifying code.

---

## Solution Structure

```
GamingCommander.sln
├── src/
│   ├── GamingCommander.Core/        Interface definitions + domain models + shared helpers
│   ├── GamingCommander.Detection/   Game discovery abstractions + design-time stub
│   ├── GamingCommander.Migration/   Migration planning abstractions + design-time stub
│   ├── GamingCommander.UI/          ViewModels (Norton Commander shell)
│   └── GamingCommander.App/         Avalonia app entry, windows, services, DI wiring
└── tests/
    ├── GamingCommander.Core.Tests/
    ├── GamingCommander.Detection.Tests/
    ├── GamingCommander.Migration.Tests/
    └── GamingCommander.App.Tests/
```

**Dependency flow:** Core ← Detection ← UI ← App (each depends on the one before it, plus Migration sits alongside Detection).

---

## Core Interfaces (GamingCommander.Core)

| Interface | File | Key Members |
|-----------|------|-------------|
| `IGame` | `IGame.cs` | `Id`, `Title`, `Source`, `InstallPath`, `ExecutablePath`, `LaunchTarget`, `LastModified` |
| `ILibraryManager` | `ILibraryManager.cs` | `LibraryRoots`, `GetGamesForRoot()`, `AddRoot()`, `RemoveRoot()`, `Refresh()`, `RescanRoot()`, `UpdateGameEntry()`, `DeleteGameEntry()`, `RetagGame()` |
| `IConfigService` | `IConfigService.cs` | `Load()` → `AppConfig`, `Save(AppConfig)` |
| `IGamesDatabaseService` | `IGamesDatabaseService.cs` | `Load()` → `GamesDatabase`, `Save()`, same CRUD as ILibraryManager |

---

## Domain Models (GamingCommander.Core/Models)

| Model | Kind | Key Fields |
|-------|------|------------|
| `GameSourceKind` | enum | `Unknown=0, Standalone=1, Steam=2, Gog=3, Epic=4, EaApp=5, UbisoftConnect=6, BattleNet=7, Xbox=8, Rockstar=9, SteamEmu=10` |
| `FileSystemEntryKind` | enum | `Directory=0, File=1, ParentDirectory=2` |
| `MigrationMode` | enum | `MoveOnly=0, MoveAndLink=1 (deprecated), ManifestRepairOnly=2` |
| `GameRecord` | record (implements IGame) | Id, Title, Source, InstallPath, LaunchTarget, ExecutablePath, LastModified, SupportsPointerInteraction, SupportsKeyboardOnlyFlow |
| `GameEntry` | record | Id, FolderName, DisplayName, GameSource, Override, ExecutablePath, LauncherPath, CmdlineArgs, ManifestPath, LastScanned, LastModified, Extra |
| `GameRoot` | record | RootPath, DefaultType, Games (List\<GameEntry\>) |
| `GamesDatabase` | record | Roots (List\<GameRoot\>) |
| `AppConfig` | record | LibraryRoots, FolderOverrides, HiddenFolders, IsFirstRun, LastSeenVersion, EnableOnlineMetadata |
| `LibraryRoot` | record | Path, DefaultType |
| `MigrationPlanSummary` | record | GameId, SourcePath, TargetPath, Mode, RequiresManifestBackup, RequiresLinkCreation (deprecated), IsDryRunOnly |
| `FileSystemEntry` | record | Name, FullPath, Kind, LastModified, Size |
| `GameSourceParser` | static class | `InferFromPath(string)`, `ParseFromString(string)`, `AvailableTypes` — shared by all ViewModels |

---

## Core Helpers (GamingCommander.Core/Services)

| Helper | File | Purpose |
|--------|------|---------|
| `VdfParser` | `VdfParser.cs` | Minimal VDF/ACF key-value parser for Steam manifest files |
| `GameEntryId` | `GameEntryId.cs` | Deterministic MD5-based ID generation for GameEntry records |

---

## UI ViewModels (GamingCommander.UI/ViewModels)

| ViewModel | File | Purpose |
|-----------|------|---------|
| `ReactiveObject` | `ReactiveObject.cs` (25 L) | Base INotifyPropertyChanged with `SetProperty<T>` |
| `ShellViewModel` | `ShellViewModel.cs` | Dual-pane shell: navigation, details, status bar, command bar |
| `ShellPaneItemViewModel` | `ShellPaneItemViewModel.cs` | Item model: Title, SourceLabel, PathSummary, Kind, IsBrowsable, GameId, PlatformId, PlatformStatus, PlatformStatusColor, PlatformStatusDetail, ItemStatusColor, HasGameSelected |
| `ShellCommandViewModel` | `ShellCommandViewModel.cs` (8 L) | Hotkey + Label for command bar |

### ShellViewModel Key Methods

- `JumpToLibraryRoots()` — populate item list from configured roots
- `LoadGamesForRoot(string rootPath)` — populate item list from a root's game entries; maps SteamStatus to PlatformStatusColor, PlatformStatusDetail, and ItemStatusColor
- `NavigateInto()` — drill into selected item (root or "..") or launch game
- `NavigateUp()` — go up one level (root list or no-op)
- `RetagSelected(GameSourceKind)` — update game source type
- `Reload()` — refresh current view
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
| `LibraryManager` | `LibraryManager.cs` | Routes scanning to appropriate scanner, manages roots, delegates to IGamesDatabaseService |
| `FolderScanner` | `FolderScanner.cs` | Generic folder scanner: fallback detection chain, exe noise filtering, container detection |
| `StoreSignalDetector` | `StoreSignalDetector.cs` | DetectType + 10 store/platform signal checks (GOG, EA, Ubisoft, Epic, Blizzard, Xbox, Rockstar, Steam) |
| `ExecutableDiscovery` | `ExecutableDiscovery.cs` | Deep exe search, primary exe selection, launcher detection, Epic manifest discovery |
| `SteamLibraryScanner` | `SteamLibraryScanner.cs` | Dedicated Steam scanner: ACF cross-referencing, library path discovery, Installed/Moved/Orphaned/Missing detection |
| `SteamAcfParser` | `SteamAcfParser.cs` | Parses Steam ACF files and libraryfolders.vdf; AcfInfo record |
| `GamesDatabaseService` | `GamesDatabaseService.cs` | JSON-file CRUD for game entries via private DTOs, in-memory cache |
| `JsonConfigService` | `JsonConfigService.cs` | JSON-file persistence for AppConfig |
| `BlacklistLoader` | `BlacklistLoader.cs` | Loads noise patterns from data/blacklist.json |
| `FileSystemHelper` | `FileSystemHelper.cs` | Shared filesystem utilities: GetDirectoriesSafe, GetFilesSafe, GetLastWriteTimeSafe, NormalizeDisplayName, NoiseSubDirNames |
| `JsonFileHelper` | `JsonFileHelper.cs` | Shared JSON read/write: ReadFromFile\<T\>, WriteToFile\<T\>, DefaultOptions |
| `HelpDialogBuilder` | `HelpDialogBuilder.cs` | Builds and shows the help dialog with keyboard shortcuts |
| `WizardViewModel` | `.App/ViewModels/WizardViewModel.cs` | First-run wizard dialog logic |
| `LibrarySetupViewModel` | `.App/ViewModels/LibrarySetupViewModel.cs` | F2 settings dialog logic |

### FolderScanner Key Logic

- `Scan(rootPath, defaultType)` → 10-signal priority-ordered detection for GOG, EA, Ubisoft, Epic, Blizzard, Xbox, Rockstar, Steam, Steam Emu
- Deep executable discovery (root → child → Binaries/Win64/ → Binaries/WinGDK/)
- Executable scoring (folder-token match +10, launcher penalty -20, shipping bonus +5, filesize bonus)
- Container detection (parent with no signals, child has signals → promote child)
- Uses `GameEntryId.Compute()` and `GameSourceParser` from Core

### SteamLibraryScanner Key Logic

- `Scan(rootPath)` / `ScanAll()` → scans steamapps/common/, cross-references ACFs from all libraries
- `DiscoverLibraryPaths()` — parses libraryfolders.vdf for additional Steam library paths
- Detects Installed/Moved/Orphaned/Missing status via ACF cross-referencing
- "Missing" detection: iterates all ACFs, checks if each installdir exists in any library's common/
- "Moved" games store `AcfExpectedPath` in Extra for cross-library context
- Stores status in `GameEntry.Extra` dict (SteamStatus, SteamAppId, AcfLibraryPath, AcfExpectedPath, etc.)
- Uses `GameEntryId.Compute()` from Core

---

## Windows (GamingCommander.App)

| Window | AXAML (lines) | Code-behind (lines) | Purpose |
|--------|--------------|---------------------|---------|
| `MainWindow` | 135 L | ~550 L | Dual-pane shell, keyboard handlers, command bar, details panel |
| `WizardWindow` | 43 L | 132 L | First-run wizard |
| `LibrarySetupWindow` | 31 L | 140 L | F2 settings |
| `GameSetupWindow` | 19 L | ~225 L | F4 game editing |

### MainWindow Key Handlers (in `OnKeyDown`)

- `Up/Down` — navigation
- `Enter` → `NavigateInto()` (launches games, drills into directories)
- `Backspace` / `Esc` → `NavigateUp()`
- `F1` → Help dialog
- `F2` → LibrarySetup dialog
- `F3` → "Not yet implemented" (placeholder)
- `F4` → GameSetup dialog (configure name, type, exe, args)
- `F6` → Rescan current root or all roots
- `F8` → "Category view not yet implemented" (placeholder)
- `F9` → JumpToLibraryRoots
- `F10` → Close()

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

---

## Mock Data (data/mock/)

```
data/mock/
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

## Test Coverage (217 tests total)

| Project | Tests | What |
|---------|-------|------|
| `Core.Tests` | 33 | VdfParser (20), GameEntryId (8), GameRecord (1), FileSystemEntryKind (4) |
| `Migration.Tests` | 1 | DesignTimeMigrationPlanner dry-run plan |
| `App.Tests` | 183 | GamesDatabaseService (20), SteamLibraryScanner (14), BlacklistLoader (11), ExecutableScoring (10), ScannerFilter (12), ExecutableDiscovery (15), GogInfoParser (10), LnkParser (13), FolderScannerContainer (13), LibraryManager (8), JsonConfigService (3), MockDataIntegration (5), RescanMerge (4), GameSetupWindow/HelpDialog/Theme (65) |

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
