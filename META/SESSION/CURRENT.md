# META/SESSION/CURRENT.md — Current Project State

**Nature:** Scratch. **Overwritten** every session handoff.
**Audience:** All agents. Read every session.
**Updated:** 2026-09-09 — Plan 125 COMPLETE (Phases 0–2 + final stage: ShellViewModel split, App warning cleanup, constants round).

---

## This session

**Plan 125 — Code Architecture Refactor: COMPLETE. Phases 0, 1a, 1b, 1c, 2, final stage (ShellViewModel split, App warnings, constants round) ALL DONE; Phase 3 dropped as N/A.**

### Phases 0 / 1a / 1b / 1c / Phase 2 — complete (see 2026-09-08/09 handoffs)
Identity contract (`ComputeId` → `physicalLibraryRoot`); `AppDataPaths` composition root; shared `ToPaneItem` projection; MainWindow partial ×8 (base 113 L); FolderScanner enrichers (`StoreMetadataEnricher` + `TitleEnricher` + `GameEntryBuildContext`, 526→352 L); ContainerScanner `SignalSummary` once-per-child classification; 2a `ExecutableDiscovery.Scoring.cs` partial (749→533 L); 2b `PlatformMetadataKeys` (26 consts + 2 pin tests); 2c `CheapExeFinder`; 2d `GameSourceParser` single array + display-name fix.

### Final stage — ShellViewModel domain split — complete (user-approved direction)
User direction: *"Logically split in the best manner possible; line number itself shouldn't be a goal, rather ease of read / parsing / agent editing which necessitates smaller files. … many domain specific source files is fine."*

`ShellViewModel` → `public sealed partial class`; **base 1,023 → 178 L**, plus 9 domain partials (all in `src/GamingCommander.UI/ViewModels/`):

| Partial | Lines | Domain |
|---|---|---|
| `ShellViewModel.cs` (base) | 178 | deps/fields/ctor/events, Items/SelectedIndex/SelectedItem, IsAtRootLevel, ActiveFilter, status+lookup props, Commands, CurrentLibraryName/ItemCount, LeftPaneTitle, GetSelectedGameId, TruncatePath |
| `ShellViewModel.Navigation.cs` | 142 | JumpToLibraryRoots, NavigateInto, NavigateUp, GetCurrentLibraryName, RetagSelected, ApplyRescannedGames, Reload (+`_previousRootIndex`) |
| `ShellViewModel.Filtering.cs` | 49 | ApplyFilter, ClearFilter, CollectFilterOptions, EnumerateGamesWithExtraTags |
| `ShellViewModel.Search.cs` | 78 | _searchBuffer, IsSearching/SearchText, AppendSearchChar, SearchBackspace, CancelSearch, EndSearch, SetSearchBuffer |
| `ShellViewModel.Loading.cs` | 179 | LoadGamesForLibrary, LoadFilteredGames, ToPaneItem (shared projection) |
| `ShellViewModel.Scanning.cs` | 78 | SetScanning, ClearScanning, UpdateScanningBadges |
| `ShellViewModel.Details.cs` | 242 | Selection-derived details surface, IsSteamOrphaned/IsEpicOrphaned/SteamAppIdForAcf, UpdateDetailsForSelection + ApplySidecarMetadata batches, date/time formatting (+`_selectedMetadata`) |
| `ShellViewModel.Details.Status.cs` | 51 | FormatOrphanedDetail, FormatMissingDetail, FormatMovedDetail |
| `ShellViewModel.Details.Metadata.cs` | 45 | Sidecar extras (Developer..PcgwUrl), config/save path clickable/registry/display, CommandLine/Video |
| `ShellViewModel.Tags.cs` | 78 | DetailsTags/HasTags/DetailsTagBadges, SelectedMergedTags, BuildTagBadges, s_storeBadgeMap, BuildStoreBadge |

Methodology: 8 partials written first (new files before removal), then base rewritten trimmed. Verification: full build **0 errors**; **GamingCommander.UI rebuilds 0 warnings / 0 errors**; all 558 tests pass; member grep shows every moved member declared exactly once (duplicates would be CS0111).

### App warning cleanup — complete (user-approved)
Full rebuild **8 → 3 warnings / 0 errors** (558 tests unchanged):
- `LibrarySetupViewModel.cs` — removed dead `_librariesService` field (CS0169 + CS8618; zero usages, ctor never assigned it).
- `MainWindow.LibrarySetup.cs` — F4 dialog now passes `game.FolderPath` as `rootPath` instead of the library anchor name (CS8604 + latent logic bug: the file-picker start folder was an anchor *label*, not a filesystem path, when the exe had no directory).
- `MainWindow.Scanning.cs:126-131` — `finally` captures `_viewModel` into a local; both `ClearScanning()` and `IsScanning = false` are null-safe (CS8602).
- `EpicLibraryScanner.cs` — captured the InstallLocation invariant in a local `string installLocation = item.InstallLocation ?? string.Empty` used at all four sites (FolderName, FindKnown, Resolve, GameFolder key) — cleared CS8604 ×2 + CS8601.
- Remaining **3 × AVLN3001** — pre-existing Avalonia designer notices ("no public constructor") on MainWindow.axaml / LibrarySetupWindow.axaml / GameSetupWindow.axaml; unactionable without breaking the composition-root design (windows are constructed in code with injected services, never via the runtime XAML loader). Accepted.

### Final stage — persisted-value constants (user-approved after occurrence-count re-scoring) — complete
Three constants classes in `src/GamingCommander.Core/Models/`, values EXACTLY the persisted literals (on-disk contract unchanged), each with 2 pin tests in `tests/GamingCommander.Core.Tests/` (equal-values + distinctness, mirroring the 2b `PlatformMetadataKeysContractTests` pattern):

| Class | Consts | Used by |
|---|---|---|
| `TitleSourceValues` | 8 (GogInfo…UserOverride) | StoreMetadataEnricher ×3, TitleEnricher ×3 (incl. the decoupled `TitleSourceValues.PeFileDescription` — no longer reuses the metadata-KEY constant as a VALUE), MainWindow.Metadata ×2, GamesDatabaseService ×1, IdentityPipelineTests fixtures |
| `SteamAcfFields` | 10 (appid…path) | SteamAcfParser (RequiredAcfFields array, 6 GetValueOrDefault, libraryfolders "path") + SteamAcfWriter (AppState header + 8 Append keys) |
| `EpicItemSchema` | 9 (DisplayName…AppCategories) | EpicManifestParser (.item + .mancpn reads) + EpicItemWriter (9 payload keys + .mancpn reads) — the writer/reader OVERLAP set; 40 write-only .item members (FormatVersion, ChunkDbs, …) stay literals (no reader → no drift surface, documented in class) |

Phase 3 `TestGameFactory` — **DROPPED as N/A**: audit found only 3 single `GameEntry` constructions (ShellViewModelSearchTests / IdentityPipelineTests / EpicLibraryScannerTests, one each); GamesDatabaseServiceTests + MetadataLookupQueueTests never construct `GameEntry`; P1b `CreateViewModel` fixture already centralizes ShellViewModelSearchTests setup. Plan gate ("only if still needed") → not needed.

Verification: full solution rebuild **0 errors** (3 × AVLN3001 unchanged); `dotnet test` **App 398 + Core 165 + Migration 1 = 564 pass / 0 fail** (558 + 6 new pin tests).

## Next session: see `META/SESSION/NEXT.md` — Plan 123 detection-bugfixes (PLANNED).