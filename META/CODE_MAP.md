# META/CODE_MAP.md — Codebase Reference

**Nature:** Reference. Updated by Builder after structural changes.
**Audience:** Builder, Reviewer. Read before modifying code.

---

## Solution Structure

```
GamingCommander.sln
├── src/
│   ├── GamingCommander.Core/        Interface definitions + domain models
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
| `ILauncher` | `ILauncher.cs` | `Name`, `IsAvailable`, `Detect()` → games, `Launch(IGame)` |
| `ILibraryManager` | `ILibraryManager.cs` | `LibraryRoots`, `Games`, `AddRoot()`, `RemoveRoot()`, `Refresh()`, `RescanRoot()`, `UpdateGameEntry()`, `DeleteGameEntry()`, `RetagGame()` |
| `IConfigService` | `IConfigService.cs` | `Load()` → `AppConfig`, `Save(AppConfig)` |
| `IGamesDatabaseService` | `IGamesDatabaseService.cs` | `Load()` → `GamesDatabase`, `Save()`, same CRUD as ILibraryManager |

---

## Domain Models (GamingCommander.Core/Models)

| Model | Kind | Key Fields |
|-------|------|------------|
| `GameSourceKind` | enum | `Unknown=0, Standalone=1, Steam=2, Gog=3, Epic=4, EaApp=5, UbisoftConnect=6` |
| `FileSystemEntryKind` | enum | `Directory=0, File=1, ParentDirectory=2` |
| `MigrationMode` | enum | `MoveOnly=0, MoveAndLink=1 (deprecated), ManifestRepairOnly=2` |
| `GameRecord` | record (implements IGame) | Id, Title, Source, InstallPath, LaunchTarget, ExecutablePath, LastModified, SupportsPointerInteraction, SupportsKeyboardOnlyFlow |
| `GameEntry` | record | Id, FolderName, DisplayName, GameSource, Override, ExecutablePath, LauncherPath, CmdlineArgs, ManifestPath, LastScanned, LastModified, Extra |
| `GameRoot` | record | RootPath, DefaultType, Games (List\<GameEntry\>) |
| `GamesDatabase` | record | Roots (List\<GameRoot\>) |
| `AppConfig` | record | LibraryRoots, FolderOverrides, HiddenFolders, IsFirstRun |
| `LibraryRoot` | record | Path, DefaultType |
| `MigrationPlanSummary` | record | GameId, SourcePath, TargetPath, Mode, RequiresManifestBackup, RequiresLinkCreation (deprecated), IsDryRunOnly |
| `FileSystemEntry` | record | Name, FullPath, Kind, LastModified, Size |

---

## UI ViewModels (GamingCommander.UI/ViewModels)

| ViewModel | File | Purpose |
|-----------|------|---------|
| `ReactiveObject` | `ReactiveObject.cs` (25 L) | Base INotifyPropertyChanged with `SetProperty<T>` |
| `ShellViewModel` | `ShellViewModel.cs` (257 L) | Dual-pane shell: navigation, details, status bar, command bar |
| `ShellPaneItemViewModel` | `ShellPaneItemViewModel.cs` (26 L) | Item model: Title, SourceLabel, PathSummary, Kind, IsBrowsable, GameId |
| `ShellCommandViewModel` | `ShellCommandViewModel.cs` (8 L) | Hotkey + Label for command bar |

### ShellViewModel Key Methods

- `JumpToLibraryRoots()` — populate item list from configured roots
- `LoadGamesForRoot(string rootPath)` — populate item list from a root's game entries
- `NavigateInto()` — drill into selected item (root or "..")
- `NavigateUp()` — go up one level (root list or no-op)
- `RetagSelected(GameSourceKind)` — update game source type
- `Reload()` — refresh current view

### ShellPaneItemViewModel.IsBrowsable

```csharp
IsBrowsable => Kind is FileSystemEntryKind.Directory or FileSystemEntryKind.ParentDirectory;
```

Game entries use `Kind = File` → not browsable. Library roots use `Kind = Directory` → browsable.

---

## App Services (GamingCommander.App/Services)

| Service | File (lines) | Purpose |
|---------|-------------|---------|
| `FolderScanner` | `FolderScanner.cs` (302 L) | Scans directory for games: exe heuristics, marker detection (steam_appid.txt, .egsstore, goggame.yml), type inference, primary exe selection, name-matching bonus, Epic manifest finding, hidden folder ignore list |
| `GamesDatabaseService` | `GamesDatabaseService.cs` (205 L) | JSON-file CRUD for game entries via private DTOs |
| `JsonConfigService` | `JsonConfigService.cs` (109 L) | JSON-file persistence for AppConfig |
| `DesignTimeLibraryManager` | `DesignTimeLibraryManager.cs` (67 L) | Implements ILibraryManager, delegates to IGamesDatabaseService |
| `GameSetupViewModel` | (in `.App/ViewModels/WizardViewModel.cs`) | First-run wizard dialog logic |
| `LibrarySetupViewModel` | (in `.App/ViewModels/LibrarySetupViewModel.cs`) | F2 settings dialog logic |

### FolderScanner Key Logic

- `Scan(rootPath, defaultType)` → enumerates subdirs, skips hidden + non-game, detects type, picks primary exe
- `IsNonGameExe()` — filters anti-cheat, installers, launchers (~25 patterns)
- `ExeNameMatchesFolderName()` — bidirectional substring + token match, beats size-based sort
- `HasGameMarkerFile()` — checks subtree for steam_appid.txt, .egsstore, etc.
- `FindPrimaryExecutable()` — prefers name-matching exe over largest exe

---

## Windows (GamingCommander.App)

| Window | AXAML (lines) | Code-behind (lines) | Purpose |
|--------|--------------|---------------------|---------|
| `MainWindow` | 127 L | 261 L | Dual-pane shell, keyboard handlers, command bar, details panel |
| `WizardWindow` | 43 L | 130 L | First-run wizard |
| `LibrarySetupWindow` | 31 L | 132 L | F2 settings |
| `GameSetupWindow` | 19 L | 227 L | T-key game editing |

### MainWindow Key Handlers (in `OnKeyDown`)

- `Up/Down` — navigation
- `Enter` → `NavigateInto()`
- `Backspace` → `NavigateUp()`
- `F2` → LibrarySetup dialog
- `F3` → "Not yet implemented" (placeholder)
- `F5` → "Launch not yet implemented" (placeholder)
- `F8` → "Category view not yet implemented" (placeholder)
- `F9` → JumpToLibraryRoots
- `F10` → Close()
- `S` → "Search not yet implemented" (placeholder)
- `T` → GameSetup dialog (retag)

### MainWindow Key Events

- `NavigationChanged` → `Focus()` on LeftListBox, `ScrollIntoView`
- `PropertyChanged(SelectedIndex)` → `ScrollIntoView`
- `LeftListBox_DoubleTapped` → `NavigateInto()` if browsable
- `CommandButtonPressed` → maps `Tag` hotkey to handler

---

## Shared Data Files (data/)

| File | Purpose |
|------|---------|
| `blacklist.json` | Aggregated noise patterns for C# startup loading: exe names, directory names, PE metadata defaults, PCGW page title noise. Consumed by `FolderScanner.IsNonGameExe()`, `IsNonGameDir()`, `IsGenericPeMetadata()`, metadata lookup scoring. |

## Existing Python Tools (tools/)

| Tool | Purpose | Status |
|------|---------|--------|
| `parse_steam_acf.py` | Parse Steam ACF files, extract identification + migration fields | ✅ Validated |
| `list_standalone_games.py` | Three-tier classification of standalone game folders | ✅ Exists |
| `discover_steam_libraries.py` | Find Steam libraries via registry + libraryfolders.vdf | ✅ Exists |
| `list_steam_common.py` | List Steam common folders cross-referenced with ACFs | ✅ Exists |
| `detect_folder.py` | Detect launcher type from folder contents | ✅ Exists |
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

## Test Coverage (18 tests total)

| Project | Tests | What |
|---------|-------|------|
| `Core.Tests` | 5 | GameRecord props, FileSystemEntryKind enum (4 tests) |
| `Detection.Tests` | 1 | DesignTimeGameDiscoveryService returns samples |
| `Migration.Tests` | 1 | DesignTimeMigrationPlanner dry-run plan |
| `App.Tests` | 11 | ScannerFilterTests (6), MockDataIntegrationTests (5) |

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
