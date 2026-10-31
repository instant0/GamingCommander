# Connection Map — how GamingCommander is wired

**Status:** Living reference (2026-10-30). Update when a pipeline or a store path changes.
**Audience:** Builder / agent adding a store, a reader, a writer, a metadata provider, or a UI action.
**Companions:** `CODE_MAP.md` (types and files), `ARCHITECTURE.md` (decisions),
`docs/detection/10-store-capabilities.md` (per-store capability matrix),
`planning/132-persistence-write-integrity.md` and `planning/133-repair-write-safety.md` (write contracts).

This file answers **"which path runs, and where does it fail?"**. It does not list every type;
`CODE_MAP.md` does that.

**Legend:** `shipped` = exists and is called · `planned` = designed, not built · `gap` = exists but is
unwired, or absent where it should exist · `—` = not applicable for that store.

---

## 0. Start here

| You are adding… | Read | Then change |
|---|---|---|
| A new store | §2, §3, §8 | `GameSourceKind`, `LauncherKinds`, a signal, a scanner or reader, tests |
| A store inventory reader | §4, §8 | `IStoreInventoryReader` (planned), one file per store, policy gate |
| A write to a launcher's folder | §7 | the repair contract; never a new writer |
| A persisted field | §2 | DTO + mapping + change detection, through the shared store |
| A UI action | §1, §5 | `MainWindow` partial + `ShellViewModel` method, no logic in the window |
| Online metadata | §6 | `MetadataService` + gate, never a detector |

---

## 1. Composition root

No container. `App.OnFrameworkInitializationCompleted` constructs everything by hand, in this order:

| Service | Lifetime | Notes |
|---|---|---|
| `GamesDatabaseService` | app | `data/games.json`; caches `GamesDatabase` in memory |
| `JsonConfigService` | app | `data/settings.json` |
| `LibrariesDatabaseService` | app | `data/libraries.json` |
| `BlacklistLoader` | per run | `data/blacklist.json`, embedded fallback |
| `FolderScanner` | app | hidden folders + blacklist + `IRegistryReader` + scan depth |
| `SteamLibraryScanner` | app | built from the Steam libraries in `libraries.json` |
| `LibraryManager` | app | owns the scanners; the only scan entry point |
| `TagColorService`, `MetadataStore` | app | `data/tag_colors.json`, `data/games_metadata.json` |
| `SteamStoreLookup`, `PcgwLookup`, `MetadataOnlineGate`, `MetadataService` | app | online metadata path |
| `ShellViewModel` | app | the only ViewModel the window talks to |
| `MainWindow` | app | split into partials; constructs `MetadataLookupQueue` |

`IRegistryReader` is `WindowsRegistryReader` on Windows, `null` elsewhere. That is the only OS branch in
composition. `SteamInstallPathLocator` is constructed in the Setup path (`MainWindow.LibrarySetup` →
`LibrarySetupWindow`/`LibrarySetupViewModel`), not at app start: it answers "is Steam installed, and where
are its libraries" when the user adds Steam. The scanner does not use it.

**Unwired (gap):** `StorePolicyProvider` implements `IStorePolicyProvider` but is constructed nowhere.
`StorePolicies` is persisted in `settings.json` and read by tests only. Until it is wired, no store gate is
enforced at import/scan time.

**Unwired (gap):** `GameStoreIndex` is built in tests only. It is the intended O(1) key/path lookup for
import and sync, not a live index.

---

## 2. Persistence and data flow

```
UI / MainWindow
   → ShellViewModel (display projection)
   → ILibraryManager (LibraryManager)
   → IGamesDatabaseService (GamesDatabaseService)  ← in-memory GamesDatabase cache
   → DTO mapping
   → JSON file
```

| File | Service | DTO | Read behaviour today | Write behaviour today |
|---|---|---|---|---|
| `data/games.json` | `GamesDatabaseService` | `GamesDatabaseDto` | missing/corrupt → empty catalogue | `JsonFileHelper.WriteToFile` in place |
| `data/libraries.json` | `LibrariesDatabaseService` | `LibrariesDto` | same | same |
| `data/settings.json` | `JsonConfigService` | `ConfigDto` | same; first-run flag and last version | same |
| `data/games_metadata.json` | `MetadataStore` | sidecar records | same | same |
| `data/collections.json` | `CollectionsDatabaseService` | `CollectionsDto` | same | same; service unwired (gap) |
| `data/blacklist.json` | `BlacklistLoader` | — | direct read + embedded fallback | restore only when missing |
| `data/tag_colors.json` | `TagColorService` | `TagColorConfig` | direct read, defaults on failure | never written |
| `data/startup.log` | `App`, `MainWindow` | — | — | append, best effort |

`JsonFileHelper` is the current single writer: `File.WriteAllText` (truncates first) and
`ReadFromFile` (returns defaults on any failure). `SchemaVersioning.IsReadable` guards a newer version but
still yields an empty value. The agreed replacement is one store with a four-state read
(`Ok`/`Missing`/`Corrupt`/`Unreadable`), a rolling `.bak`, and `.corrupt` preservation; it is **planned**,
not shipped.

`IConfigService.Load()` is called by several services; it re-reads and re-parses `settings.json` each time.
Treat it as cheap but not free — do not call it per row.

---

## 3. Scan pipeline (shipped)

```
F5 / first run
  → MainWindow.RefreshCurrentRootAsync
  → LibraryManager.RescanLibrary | SelectScannerAndScan
      ├─ LooksLikeSteamLibrary(root) or type == Steam  → SteamLibraryScanner
      ├─ type == Epic or EpicItemCatalog.LooksLikeManifestsDir(root) → EpicLibraryScanner
      └─ otherwise → FolderScanner
  → IReadOnlyList<GameEntry>
  → IGamesDatabaseService.SetGamesForLibrary / UpdateGameEntry
  → ShellViewModel.Reload → projection to ShellPaneItemViewModel
```

`LibraryManager` stamps `GameEntry.Identity.Library` with the anchor name; a scanner may re-anchor the
source kind (for example an Epic game found under a Standalone root).

### Steam scanner

`SteamInstallPathLocator` (registry) → `SteamAcfParser.DiscoverLibraryPaths` (`libraryfolders.vdf`) →
`SteamAcfParser.ParseAcfFile` per `appmanifest_*.acf` → `SteamLibraryScanner` correlates ACFs with
`steamapps/common/<installdir>`. `SteamLibraryMembership` recognises the structure when the ACF is missing.
The VDF is a path locator only; its `apps` blocks are ignored.

### Epic scanner

`EpicManifestPaths.GetManifestsDir` (ProgramData, env override) → `EpicItemCatalog` enumerates `*.item` →
`EpicManifestParser.ParseItemFile` → `EpicLibraryScanner` maps each item, resolves the exe with
`EpicLaunchResolver`, and marks known Epic entries in other libraries as Orphaned when the catalog no longer
claims them.

### Folder scanner (Standalone, GOG, EA, Ubisoft, Battle.net, Xbox, Rockstar)

`FolderScanner.Scan` runs in passes:

1. **Pass 1 — store signals** at the folder level: `StoreSignalDetector.DetectType` (Steam location first,
   then GOG → EA → Ubisoft (emulator, then standard) → Epic → Blizzard → Xbox → Rockstar → SteamEmu).
2. **Pass 1c — registry fallback** when no signal matched: `RegistryFallbackDetector` (exact install path,
   then directory-name match) for GOG, EA, Ubisoft, Rockstar.
3. **Pass 1.5 — exact folder-name match** (terminating rule) against known anchors.
4. **Pass 1.6 — parent-bound child promotion** for known child folders of a parent game.
5. **Pass 2 — standalone / fallback**: `FallbackSignalDetector`, `ExecutableDiscovery`, `CheapExeFinder`,
   `LnkParser`, blacklist noise filtering.
6. **Pass 3 — containers**: `ContainerScanner` for Unreal-style container folders with no direct signal.

Every produced entry gets `DetectionTierDeriver.Derive(...)`; `FolderScanner` sets
`Install.State = InstallState.Installed` unconditionally (gap for GOG/EA/Ubisoft/Battle.net/Xbox/Rockstar
health).

### Scan failure modes (all stores)

| Condition | Result |
|---|---|
| root missing or unreadable | scanner returns no entries; `LibraryManager` never throws |
| manifest malformed | parser returns nothing usable; detection falls back to the next pass |
| manifest points outside the root | entry is skipped or marked Missing |
| permission denied during enumeration | caught; that folder contributes nothing |
| duplicate folder in two anchors | last write wins for that anchor; identity reconciliation runs separately |

---

## 4. Store inventory → import → sync

| Stage | Today | Planned |
|---|---|---|
| Locator | `SteamInstallPathLocator` shipped but Setup-only; Epic fixed ProgramData path shipped; others — | `ILauncherLocator` per store (`planning/129`) |
| Inventory read | Steam ACF/VDF, Epic `.item` (inside the scanners) | `IStoreInventoryReader` returning `StoreInventoryEntry` + `StoreManifestFingerprint` |
| Import | folded into scan: `LibraryManager.SelectScannerAndScan` → `SetGamesForLibrary` | `StoreInventoryImportService.Import(kind)` through the shared store |
| Policy gate | `IStorePolicyProvider` **unwired** (gap) | `CanImport` / `CanSync` enforced before any read or write |
| Diff / sync | — | `StoreSyncService.RunOnStartupAsync`, diff by `StoreKey` via `GameStoreIndex` |
| Health | Steam/Epic states; others hardcoded `Installed` | per-store probes in the import path |

**Gate rule (planned):** every store read and write is preceded by `IStorePolicyProvider.CanImport` or
`CanSync` for that `GameSourceKind`. `LauncherKinds` defines which kinds are policy kinds (the eight store
launchers; `Standalone`, `SteamEmu`, `Unknown` are discovery sources only).

**Do not** add a second switch, a per-store bool, or a "detected means enabled" shortcut. One policy
provider, one decision per action.

---

## 5. Launch pipeline (shipped)

```
Enter in the left pane
  → ShellViewModel.NavigateInto → RequestLaunch
  → MainWindow.LaunchSelectedGameAsync
  → GameLaunchResolver.Resolve(GameEntry)
      ├─ CommandLineArguments is a steam:// URI → run the URI (Steam handles the app id)
      └─ otherwise → ExecutablePath + LaunchArgumentComposer.ForProcessStart(...)
  → Process.Start
```

Steam games launch through `steam://rungameid/{appid}`; the exe is not started directly. Epic games use the
resolved exe. No launcher client is embedded; no store API is called at launch.

Failure points: no exe and no URI → status message, no process; a moved/removed exe → the OS error is
surfaced as a status message.

---

## 6. Metadata and online egress

```
F3 / scan enrichment
  → MainWindow.MetadataLookupQueue (one queue)
  → MetadataService.RefreshAsync
      ├─ SteamStoreLookup   (store page)
      └─ PcgwLookup         (PCGamingWiki)
  → MetadataOnlineGate (one switch; Disabled / Checking / Enabled)
  → CommonMetadataParser → GameMetadataRecord
  → MetadataStore.Upsert → data/games_metadata.json
```

Offline enrichment during scanning uses local manifests only: `StoreMetadataEnricher`, `GogInfoParser`,
`EaInstallLogParser`, `UbisoftReadmeParser`, `TitleEnricher`, `EngineDetector`.

Egress is opt-in (`EnableOnlineMetadata`), one gate (`MetadataOnlineGate`), one queue, no retry storm.
`EpicStoreSearch` (Epic GraphQL) is **compiled but has no callers**. It is kept as an **identity lookup**,
not a metadata source: local `.item`/`.mancpn` manifests supply the Epic fields, and PCGW/Steam fill the
rest. It has two uses only — identify a detected game whose local records are missing or incomplete, and
resolve the canonical title before the PCGW/Steam query. It returns title, namespace, and catalog id
only. `.item` regeneration and registration are separate, deferred subjects. It must not be called from
scan, launch, sync, or health.

---

## 7. Trust boundaries and writes

| Direction | What | Where |
|---|---|---|
| Read | store registry keys | `WindowsRegistryReader` (HKLM/HKCU, store keys only) |
| Read | Steam VDF/ACF | library folders |
| Read | Epic `.item` | `%ProgramData%\Epic\EpicGamesLauncher\Data\Manifests` (or env override) |
| Read | game folders | top-level signals + bounded exe search; no recursive walk of the whole drive |
| Read | app data | `data/*.json`, `data/blacklist.json`, `data/tag_colors.json` |
| Write | app data | `data/` only: JSON through `AppDataPaths`, logs to the same folder |
| Write | Steam ACF | `<library>\steamapps\appmanifest_<appid>.acf`, user-started, create-only |
| Write | Epic `.item` | existing item only, user-started, modify-only (contract; current code can still replace) |
| Write | `startup.log` | `data/`, best effort |
| Egress | Steam store + PCGamingWiki | only when online metadata is enabled |
| Never | registry writes, services, drivers, game-file modification | — |

**Directory rule:** external directories are checked with `Directory.Exists` and refused when missing; they
are never created. Only `data/` subdirectories are created, and only through the application-data guard
(planned; today `JsonFileHelper.EnsureDirectoryExists` creates arbitrary parents, which is the defect being
replaced).

**Write rules:** Steam creates a missing ACF and refuses an existing one; Epic modifies an existing,
parseable `.item` and refuses a missing or unreadable one; both stage in `data/staging/`, copy, length-check,
journal to `data/repair-journal.jsonl`, and never write a temp file into the store folder. No hash,
signature, or digest is involved. (Current code predates this contract; see `planning/133`.)

---

## 8. Per-store decision tree

Columns: **intent → gate → path → failure points → test**. `—` means the store has no such path.
Gates today are `GameSourceKind` (routing) and the user action (repair); the policy gate is planned and
currently unwired.

### Steam

| Intent | Gate | Path | Failure points | Test |
|---|---|---|---|---|
| scan | `LibraryManager.SelectScannerAndScan`; structural check `LooksLikeSteamLibrary` | `SteamLibraryScanner` → `SteamAcfParser` + `SteamLibraryMembership` | root unreadable; vdf missing; ACF malformed; folder absent | `SteamLibraryScannerTests`, `SteamBootstrapTests`, `SteamLibraryMembershipTests` |
| inventory | configured type Steam | `SteamLibraryScanner` → `DiscoverLibraryPaths` (`libraryfolders.vdf`) → `ParseAcfFile` per ACF. `SteamInstallPathLocator` is the Setup-time locator, not the scan path | registry key absent (setup only); vdf missing → configured roots only; malformed ACF; dead library path | `SteamBootstrapTests`, `SteamLibraryScannerTests` |
| import | policy (planned) | same as scan | — | `LibraryManagerTests` |
| launch | `GameLaunchResolver` | `steam://rungameid/{appid}` | no appid → exe fallback; no exe → status only | `GameLaunchResolverTests` |
| repair | user action (Write ACF) | `SteamAcfWriter.TryWrite` (create-only) | `steamapps` missing; no `libraryfolders.vdf`; target exists | `SteamAcfWriterTests` |
| health | `SteamLibraryScanner` | `InstallStateMap` + `SteamLibraryMembership` | ACF in another library → Moved; no folder → Missing; no ACF → Orphaned | `SteamLibraryScannerTests` |

### Epic

| Intent | Gate | Path | Failure points | Test |
|---|---|---|---|---|
| scan | type Epic or `EpicItemCatalog.LooksLikeManifestsDir` | `EpicManifestPaths` → `EpicItemCatalog` → `EpicLibraryScanner` | manifests dir missing; item unparseable; install path absent | `EpicLibraryScannerTests`, `EpicManifestParserTests` |
| inventory | configured type Epic | `EpicItemCatalog.All` (ProgramData `*.item`) | no catalog ids; duplicate `CatalogItemId`; unreadable folder | `EpicItemCatalog`-covered via `EpicLibraryScannerTests` |
| import | policy (planned) | same as scan | — | gap |
| launch | `EpicLaunchResolver` → `GameLaunchResolver` | `.item` `LaunchExecutable`, else scored exe | store launcher recorded; exe missing | `EpicLibraryScannerTests` |
| repair | user action (Repair) | `EpicItemWriter.TryWrite` (modify-only per contract) | item absent; unparseable; different install id; manifests dir missing | `EpicItemWriterTests` |
| health | `EpicLibraryScanner` | Installed / Missing / Orphaned; `EpicInstallPath.Equals` | folder-name claiming across libraries is still possible (gap) | `EpicLibraryScannerTests`, `EpicInstallPathTests` |

### GOG

| Intent | Gate | Path | Failure points | Test |
|---|---|---|---|---|
| scan | `StoreSignalDetector.HasGogSignal` | `FolderScanner` pass 1 (goggame*, gog_*, `gog.ico`, `Launch *.lnk`) | signal absent; signal file unreadable | `StoreSignalDetectorTests`, `PythonParityDetectionTests` |
| inventory | — | — (Galaxy SQLite unread) | — | gap |
| import | policy (planned) | — | — | gap |
| launch | `GameLaunchResolver` | exe; `.info` provides name/gameId/exe/args | `.info` malformed; exe missing | `GogInfoParserTests`, `ExecutableDiscoveryTests` |
| repair | — | — | — | — |
| health | `FolderScanner` | hardcoded `Installed` | no Moved/Missing/Orphaned | gap |

### EA

| Intent | Gate | Path | Failure points | Test |
|---|---|---|---|---|
| scan | `StoreSignalDetector.HasEaSignal`, else `RegistryFallbackDetector` | `FolderScanner` pass 1 / pass 1c | no `__Installer`; registry key missing or stale | `StoreSignalDetectorTests`, `RegistryFallbackDetectorTests` |
| inventory | — | — (encrypted `IS`; legacy `.mfst` unread) | — | gap |
| import | policy (planned) | — | — | gap |
| launch | `GameLaunchResolver` | scored exe | exe missing | `ExecutableScoringTests` |
| repair | — | — | — | — |
| health | `FolderScanner` | hardcoded `Installed` | no health states | gap |

### Ubisoft

| Intent | Gate | Path | Failure points | Test |
|---|---|---|---|---|
| scan | `StoreSignalDetector.HasUbisoftSignal` / `HasUbisoftEmulatorSignal`, else registry | `FolderScanner` passes 1/1c | signal absent; registry key missing | `StoreSignalDetectorTests`, `RegistryFallbackDetectorTests` |
| inventory | — | — (registry classifies, id discarded) | — | gap |
| import | policy (planned) | — | — | gap |
| launch | `GameLaunchResolver` | scored exe | exe missing | `ExecutableDiscoveryTests` |
| repair | — | — | — | — |
| health | `FolderScanner` | hardcoded `Installed` | no health states | gap |

### Battle.net

| Intent | Gate | Path | Failure points | Test |
|---|---|---|---|---|
| scan | `HasBlizzardSignal` (`.battle.net`, `.build.info`, or `.product.db`/`.patch.result`) | `FolderScanner` pass 1 | marker absent | `StoreSignalDetectorTests`, `FallbackSignalTests` |
| inventory | — | — (`product.db` SQLite unread) | — | gap |
| import | policy (planned) | — | — | gap |
| launch | `GameLaunchResolver` | scored exe | exe missing | `ExecutableDiscoveryTests` |
| repair | — | — | — | — |
| health | `FolderScanner` | hardcoded `Installed` | no health states | gap |

### Xbox

| Intent | Gate | Path | Failure points | Test |
|---|---|---|---|---|
| scan | `HasXboxSignal` (`default-metadata.json`) | `FolderScanner` pass 1 | marker absent; WindowsApps ACL | `StoreSignalDetectorTests` |
| inventory | — | — (no AppX package API) | — | gap |
| import | policy (planned) | — | — | gap |
| launch | `GameLaunchResolver` | scored exe | package identity unavailable | gap |
| repair | — | — | — | — |
| health | `FolderScanner` | hardcoded `Installed` | no health states | gap |

### Rockstar

| Intent | Gate | Path | Failure points | Test |
|---|---|---|---|---|
| scan | `HasRockstarSignal` (`title.rgl`), else registry | `FolderScanner` passes 1/1c | launcher key skipped; game key missing | `StoreSignalDetectorTests`, `RegistryFallbackDetectorTests` |
| inventory | — | — (`Settings.json`/registry unread for inventory) | — | gap |
| import | policy (planned) | — | — | gap |
| launch | `GameLaunchResolver` | scored exe | exe missing | `ExecutableDiscoveryTests` |
| repair | — | — | — | — |
| health | `FolderScanner` | hardcoded `Installed` | no health states | gap |

### Standalone

| Intent | Gate | Path | Failure points | Test |
|---|---|---|---|---|
| scan | no store signal → `FolderScanner` pass 2 | `FallbackSignalDetector` + `ExecutableDiscovery` + `CheapExeFinder` + `LnkParser` + blacklist | no plausible exe; all candidates are noise; deep tree beyond scan depth | `ExecutableDiscoveryTests`, `ScannerFilterTests`, `BlacklistLoaderTests` |
| inventory | — | — | — | — |
| import | policy (planned) | — | — | gap |
| launch | `GameLaunchResolver` | exe + args | exe removed | `GameLaunchResolverTests` |
| repair | — | — | — | — |
| health | `FolderScanner` | hardcoded `Installed` | no health states | gap |

---

## 9. Extension recipes

### Add a store kind
1. `GameSourceKind` (Core/Models) — new value, never reorder existing ones.
2. `GameSourceParser` — display name and parse-from-string.
3. `LauncherKinds` — add to the launcher set if it is a policy store.
4. `StoreSignalDetector` — one signal method, one line in `DetectType` priority order.
5. `RegistryFallbackDetector` — only if the store has usable per-game keys.
6. A scanner or inventory reader (§4), with a fixture.
7. `StorePolicyProvider` defaults — disabled.
8. Tests: signal, detection priority, reader, and the capability matrix row.

### Add a store signal
- One method on `StoreSignalDetector`, named for the marker, tested with a temp folder.
- One line in `DetectType`; document the position because order decides ties.
- Do not add a second detector class for a single marker.

### Add an inventory reader
- Implement `IStoreInventoryReader` (planned) with `Kind`, `Fingerprint`, `Read()`.
- Read the manifest once; return records even when the folder is missing.
- No recursion, no `FolderScanner` call, no writes.
- Wire it into `StoreInventoryImportService`; do not call it from a detector.

### Add a repair writer
- Do not. Extend the existing contract (`planning/133`) with a new operation only if a store has a
  format that can be modified safely; the writer supplies target derivation and preconditions only.
- Reuse the shared preflight, staging, length check, backup/restore, and journal.

### Add a persisted field
1. Add it to the DTO, not to `GameEntry` first.
2. Map it in load and save; default it for old files.
3. Decide whether a change to it affects identity or detection; if yes, stop and use the identity path.
4. Write through the shared store; never `File.WriteAllText` directly.

### Add a UI action
- `MainWindow` partial for input routing; the decision and data work live in `ShellViewModel` or a service.
- Add the command-bar entry only if it is a top-level action; labels are init-only.
- Update `HelpDialogBuilder` and the interaction hint in the same change.

### Add a metadata provider
- Implement the lookup against `MetadataOnlineGate`; no direct `HttpClient` in a detector.
- Route through `MetadataService` and the queue; one request per game, no retry storm.
- Persist only through `MetadataStore`.

---

## 10. Known gaps (verified against the current code)

| Gap | Consequence | Where it belongs |
|---|---|---|
| `StorePolicyProvider` unwired | store enable/disable is persisted but not enforced | launcher discovery / import |
| `GameStoreIndex` built only in tests | no live O(1) key/path index for import or sync | inventory import |
| `JsonFileHelper` truncates then writes; read returns defaults | a damaged save can become an empty catalogue | persistence contract |
| `EpicItemWriter` can still create and replace a corrupt item | contradicts the modify-only contract | repair contract |
| `SteamAcfWriter` creates `steamapps` | writes a launcher-owned directory | repair contract |
| `EpicOrphanDiscovery` has no callers | orphan detection is done inside `EpicLibraryScanner` only | Epic scanner |
| `CollectionsDatabaseService` has no caller | collections are persisted only in tests | collections feature |
| non-Steam health is hardcoded `Installed` | GOG/EA/Ubisoft/Battle.net/Xbox/Rockstar cannot report Moved/Missing | health work |
| `EpicLibraryScanner` claims folders by name as well as path | two libraries with the same folder name can collide | Epic identity |
| `EpicStoreSearch` compiled, no callers | deferred opt-in metadata leg | online metadata |

A gap is recorded here; it is not fixed here. Do not add an abstraction to paper over one.

---

## 11. Maintenance rules

- Cite classes, not line numbers.
- When a store's path changes, update its row in §8 and the capability matrix only if the capability
  changed.
- When a pipeline gains an arrow, update §2–§6; a new arrow that cannot be drawn means the feature spans
  too many layers.
- If a row's test column is empty in a change you are making, either add the test or mark the gap.
- Keep this file a map: tables and short arrows, not prose.
