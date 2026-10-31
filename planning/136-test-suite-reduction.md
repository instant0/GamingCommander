# 136 — Test Suite Reduction: Scope and Plan

**Status:** **APPLIED** (2026-10-31): 773 → 499 executed cases, 420 methods.
**Baseline:** 657 test methods, **773 executed cases** (Core 232, App 540, Migration 1).
**Companions:** the reduction task document under `META/TASKS/phase-g-code-quality-and-tests/`,
`META/CONNECTION_MAP.md`, `META/COMPLETED/backward-compatibility-removed.md`.

---

## 1. The counting trap

The task table counts **methods**; `dotnet test` counts **cases**. Theory-heavy files hide their real size:

| File | Methods | Cases |
|---|---:|---:|
| `TitleTextTests` | 25 | 44 |
| `TagNormalizerTests` | 21 | 34 |
| `FileSystemHelperTests` | 2 | 13 |
| `LibraryManagerTests` | 1 | 10 |
| `StoreSignalDetectorTests` | 57 | ~57 |

**Merging facts into a theory does not reduce the count.** Reduction must delete rows and facts, not
re-shape them. Every proposed change below is measured in methods *and* checks whether the surviving theory
still has redundant rows.

---

## 2. Target

- **Hard ceiling:** 500 executed cases.
- **Expected:** 430–470 (methods after the per-file plan: 415 + ~7 gap tests = 422; row trimming lands near 450).
- **Floor:** 400. Do not go below it without a human decision; below that, external-format markers or
  safety rows start disappearing.

The target is not a quota. A test stays if its failure would change a product decision. The final number is
the outcome of the behavior map, not the goal.

### Review corrections (2026-10-31)

- This document is the **scope**; the task is the execution. The task is not complete until every batch is
  done and the behavior map is filled.
- The per-file table is **methods**, not cases. The 415 total plus seven gap tests is ~422 methods; the
  expected case count is higher because surviving theories keep rows.
- Gap tests add: scanner routing (+1, so `LibraryManagerTests` is 1, not 0), empty launch target (+1, so
  `GameLaunchResolverTests` is 3), `LibrariesDatabaseService` (+2), FolderScanner registry pass (+1),
  Steam missing-VDF (+1), non-Steam scanner state (+1).
- Do **not** delete `IRegistryReader.ReadKeyValues` or `EnumerateSubKeyNames`; they are production surface.
  Delete only the test-only `.reg` parser tests and unused test-double members.

---

## 3. Suite ownership (one owner per behavior)

| Behavior | Owning suite | Other suites must not re-assert it |
|---|---|---|
| Store signal markers + `DetectType` precedence | `StoreSignalDetectorTests` | scanner/container/integration |
| Fallback signals (UE, root exe, lnk) | `FallbackSignalTests` | scanner/container |
| Registry fallback (exact, moved name) | `RegistryFallbackDetectorTests` | mapping only |
| Folder scan passes, containers, non-game skip | `FolderScannerContainerTests` | `ScannerFilterTests` |
| Noise/blacklist filtering, hidden folders | `ScannerFilterTests` | scanner tests |
| Steam library scan/health | `SteamLibraryScannerTests` | parser/bootstrap |
| Steam locator/vdf | `SteamBootstrapTests` | scanner |
| Steam structural membership | `SteamLibraryMembershipTests` | scanner |
| Epic edit/skip/claim | `EpicLibraryScannerTests` | parser/writer |
| Epic `.item`/`.mancpn` format | `EpicManifestParserTests` | writer/scanner |
| Executable discovery + ranking | `ExecutableDiscoveryTests`, `ExecutableScoringTests` | scanners |
| JSON store (read/write/corruption) | `JsonDocumentStoreTests` | services |
| Game catalog CRUD, merge, relocation | `GamesDatabaseServiceTests` | store/identity |
| Libraries/config/metadata/collections persistence | one suite each | store |
| Identity keys, weak relocation | `IdentityReconciliationTests`, `GameEntryIdTests` | catalog |
| Text/tag/path helpers | `TitleTextTests`, `TagNormalizerTests`, `PathTextTests` | everything else |
| External-format pin tests | `SteamAcfFieldsContractTests`, `EpicItemSchemaContractTests`, `TitleSourceValuesContractTests` | services |
| Launch composition | `GameLaunchResolverTests`, `LaunchArgumentComposerTests` | UI |
| Metadata sources | `Pcgw*`, `SteamStore*`, `Gog*`, `Ea*`; `MetadataServiceTests` end-to-end | source parsers |
| UI search | `ShellViewModelSearchTests` | helpers |
| Policy (unwired) | `StorePolicyPersistenceTests`, `StorePolicyProviderTests` | config |

---

## 4. Per-file scope

`now → after` is **methods**. “Action” names the main change; details live in the audit table for each
file (kept with the task, not duplicated here).

### Detection and scanning

| File | Now → after | Action |
|---|---:|---|
| `StoreSignalDetectorTests` | 57 → 23 | One marker theory per store predicate; delete 8 `Has*_NoSignal` false tests; keep empty/priority/weak-Steam cases |
| `FallbackSignalTests` | 16 → 10 | UE rows collapse; keep UE3 fast path, all-noise, root exe/noise, `.lnk`, priority |
| `ScannerFilterTests` | 16 → 7 | Remove corpus duplicates; keep hidden folders, one noise dir/exe, Steam location, primary-exe preference |
| `FolderScannerContainerTests` | 21 → 13 | Collapse platform rows; keep org folder (both signals), depth bound, non-game skip, store promotion, ambiguous child |
| `SteamLibraryScannerTests` | 21 → 13 | Keep health states, malformed ACF, cross-library ambiguity; delete the vacuous “vdf discovered paths” test; merge status-extra asserts |
| `SteamBootstrapTests` | 6 → 5 | `AppsBlocks…` is subsumed by the library-count test |
| `SteamLibraryMembershipTests` | 7 → 3 | 14 rows → ~8; keep `common` itself, descendant, similar segment, empty, root hit/miss |
| `EpicLibraryScannerTests` | 5 → 5 | All keep; owns addon/dedupe/orphan/path-claim |
| `EpicManifestParserTests` | 17 → 16 | Merge case/trailing-separator rows into the path-rule test |
| `EpicItemClassifierTests` | 2 → 1 | Two rows of one predicate |
| `EpicInstallPathTests` | 3 → 1 | Path compare owned by `PathTextTests`; keep “same leaf is not same folder” |
| `RegistryFallbackDetectorTests` | 19 → 6 | Per-store shape rows; moved-game name rows; drop `NormalizePath` delegates and the fake precedence test |
| `MockRegistryReaderTests` | 12 → 0 | Tests the test double, not the product; delete with the unused dword/hex/`ReadKeyValues` surface |
| `PythonParityDetectionTests` | 10 → 7 | Keep unique terminating/whole-name/backup/container cases; drop repeated flatter variants |
| `MockDataIntegrationTests` | 5 → 4 | Drop `LastScanned` init assert |
| `DetectionTierDeriverTests` | 9 → 9 | Every store→tier branch is a contract |
| `LnkParserTests` | 13 → 12 | Merge the two extraction shapes |
| `EngineDetectorTests` | 8 → 7 | Merge the two “no evidence” rows |
| `BlacklistLoaderTests` | 16 → 9 | Keep restore/no-dir/all-keys/tier mapping; drop tier-name trivia and duplicate section asserts |
| `FileSystemHelperTests` | 2 → 1 | 13 rows → 5; token list pinned by `EditionTokensTests` |
| **Subtotal** | **265 → 152** | |

### Persistence, identity, metadata

| File | Now → after | Action |
|---|---:|---|
| `ExecutableDiscoveryTests` | 18 → 10 | Four platform probes → one; keep depth/noise/multi-site/tie-break |
| `ExecutableScoringTests` | 29 → 13 | Collapse copy/org/backup penalties into one; size bands; shipping/win64; keep safety predicates |
| `GameEntryIdTests` | 13 → 4 | Determinism, uniqueness, format, normalized formatting; drop vacuous lowercase/empty/special/duplicate re-anchor |
| `InstallIdentityTests` | 6 → 3 | Keep UNC, weak-key scope, weak-key empty; path rows move to `PathTextTests` |
| `IdentityPipelineTests` | 3 → 1 | Two argument-order tests prove nothing; keep the user-title preservation |
| `IdentityReconciliationTests` | 8 → 7 | Keep every guard (strong, weak, two-side ambiguity, store scope, alive folder, same path); drop key-builder duplication |
| `GamesDatabaseServiceTests` | 21 → 10 | One full DTO round-trip; keep first run, CRUD, override preservation, scoped replace, equivalent-path, no-op guards; drop store-level corruption dups |
| `JsonConfigServiceTests` | 6 → 3 | Merge defaults/scan-depth; keep corrupt-but-not-first-run |
| `JsonDocumentStoreTests` | 10 → 8 | Keep all three states, one `.bak`, verify failure, corrupt backup; drop the test-harness “outside app root” case |
| `CollectionsDatabaseServiceTests` | 3 → 2 | Service is unwired; keep round-trip/remove |
| `MetadataStoreTests` | 7 → 4 | Merge missing/unknown; keep isolation, corrupt→null, details round-trip |
| `MetadataServiceTests` | 9 → 8 | Keep gates/queue/merge; merge details-preservation into merge |
| `MetadataLookupQueueTests` | 3 → 3 | Three distinct gates |
| `MetadataOnlineGateTests` | 6 → 6 | Every branch is egress safety |
| `StorePolicyPersistenceTests` | 7 → 5 | Drop enum-spelling and missing-file duplicate; keep partial/unknown/disabled/defaults |
| `StorePolicyProviderTests` | 9 → 8 | Drop provider re-filter duplicate (config load owns it); reduce theories |
| `AppPathsTests` | 4 → 3 | Merge root assertion |
| `RepairWriteGuardTests` | 11 → 11 | Every safety branch stays |
| `SteamAcfWriterTests` | 11 → 7 | Keep format/create-only/no-temp/preview/read-only; drop preconditions owned by the guard |
| `EpicItemWriterTests` | 10 → 6 | Keep unknown-member preservation, non-empty launch, wrong-dir, different guid, preview, read-only; drop guard-owned preconditions |
| `SteamStoreLookupTests` | 4 → 3 | Lookup owns empty-id/HTTP failure; parse is owned below |
| `SteamStoreParserTests` | 7 → 6 | Merge failed-facts mapping |
| `PcgwInfoboxParserTests` | 10 → 10 | External format pins |
| `PcgwSectionParserTests` | 13 → 10 | Merge token rows and combined-fixture duplicates |
| `PcgwTitleFilterTests` | 7 → 6 | Merge the two year-hint outcomes into one theory |
| **Subtotal** | **228 → 151** | (excludes the `PcgwTitleFilterTests` double count) |

### Core helpers and UI

| File | Now → after | Action |
|---|---:|---|
| `TitleTextTests` | 25 → 18 | Drop duplicate edition/acronym rows; keep each matching rule once |
| `TagNormalizerTests` | 21 → 14 | Merge whitespace/equivalence rows; keep dedupe, parse, metadata, merge |
| `VdfParserTests` | 20 → 12 | Keep external ACF/VDF shapes, escapes, malformed, extraction; drop single-pair/tabs/nesting repetitions |
| `WindowsExplorerTests` | 6 → 5 | Drop quoted-args duplicate; keep all unsafe rejects |
| `GameLaunchResolverTests` | 2 → 3 | Keep Steam URI vs exe; **add empty-target refusal** (gap) |
| `LaunchArgumentComposerTests` | 5 → 5 | Composition owner |
| `GameStoreIndexTests` | 6 → 5 | Drop `NormalizePath` delegate |
| `InstallStateMapTests` | 1 → 1 | Status mapping owner; trim one row |
| `GameSourceParserTests` | 2 → 2 | Trim label rows |
| `GameFilterMatcherTests` | 4 → 4 | Keep tag/label/wildcard/options |
| `GameRecordTests` | 1 → 0 | Record getter smoke |
| `GameEntryTagsTests` | 9 → 0 | Record/language surface; preservation covered in catalog tests |
| `EditionTokensTests` | 2 → 1 | One pin for both token sets |
| `TitleSourceValuesContractTests` | 2 → 1 | Value pin; distinctness implied |
| `EpicItemSchemaContractTests` | 2 → 1 | Value pin; distinctness implied |
| `SteamAcfFieldsContractTests` | 2 → 1 | Value pin; distinctness implied |
| `PathTextTests` | 5 → 5 | Owner; absorb null/whitespace rows from `InstallIdentityTests` |
| `MetadataDetailsFormatterTests` | 7 → 5 | Keep selection/formatting; drop token expansion owned by `PcgwSectionParser` |
| `FileSystemEntryKindTests` | 4 → 0 | Enum member smoke; not persisted |
| `ShellViewModelSearchTests` | 8 → 8 | Keep threshold/filter/escape/projection; tighten tag test; drop title-string asserts during implementation |
| `LibraryManagerTests` | 1 → 1 | `IsChildOf` delegate goes; **add scanner-routing test** (gap) |
| `LibrariesDatabaseServiceTests` | 0 → 2 | New: round-trip + missing/corrupt default (gap; no suite today) |
| `MetadataTextTests` (same file) | 4 → 4 | Security contracts |
| **Subtotal** | **146 → 101** | (plus 2 new `LibrariesDatabaseServiceTests`) |

### Remaining parser suites

| File | Now → after | Action |
|---|---:|---|
| `GogInfoParserTests` | 10 → 7 | Keep main/DLC precedence, malformed, no-exe/no-primary, subdir; merge relative/absolute path pair |
| `EaInstallLogParserTests` | 8 → 5 | Keep standard, last-session, missing log, name-only variants, studio; drop wrong-location duplicate |
| **Subtotal** | **18 → 12** | |

**Totals:** 657 → **422 methods** before theory-row trimming (415 + 7 gap tests); expected ~430–470 cases,
hard ceiling 500.

---

## 5. Cross-file duplicates to remove (exact pairs)

1. `GameEntryIdTests.Compute_ReAnchor…` ≡ `Compute_SameInputs…` (same body); anchor-is-not-identity also covered by `RetagGame_ChangesSourceType`.
2. `InstallIdentityTests.PathKey_NormalizesFormatting` ≡ `PathTextTests.NormalizeForCompare_UnifiesSeparators`; `PathKey_IgnoresCase…` ≡ `PathTextTests.Equals…`.
3. `LibraryManagerTests.IsChildOf_ReturnsExpected` ≡ `PathTextTests.IsUnder_IsSegmentAware` (delegate).
4. `GameStoreIndexTests.NormalizePath_…` ≡ `PathTextTests.NormalizeForCompare_…`.
5. `GamesDatabaseServiceTests.Load_WithCorruptFileAndBackup…` / `JsonConfigServiceTests.Load_WithCorruptFileAndBackup…` ≡ `JsonDocumentStoreTests.Read_CorruptWithBackup…`.
6. `GamesDatabaseServiceTests.Save_WithCorruptFile_OverwritesCorrupt` ≡ store corrupt + round-trip.
7. `GamesDatabaseServiceTests.EpicCatalogRescan…` ≡ `IdentityReconciliationTests.SamePath_KeepsIdentity`.
8. `StorePolicyProviderTests.Get_IgnoresNonLauncherRows…` ≡ `StorePolicyPersistenceTests.Load_UnknownAndNonLauncherKinds…`.
9. `SteamAcfWriterTests.TryWrite_MissingSteamapps/MissingLibraryFolders/RelativeOrUnc` ≡ the three `RepairWriteGuardTests` preflights.
10. `EpicItemWriterTests.TryWrite_AbsentTarget/CorruptTarget/MissingRequiredMember/MissingManifestsDir` ≡ guard preflights.
11. `SteamStoreLookupTests.FetchRaw_ThenParse…` ≡ `SteamStoreParserTests` mapping.
12. `MetadataDetailsFormatterTests.WindowsSaves_*` ≡ `PcgwSectionParserTests.ResolveWindows_*` token cases.
13. `SteamBootstrapTests.FindAllLibraries_AppsBlocks…` ≡ `FindAllLibraries_InstallPathPlusVdfPaths`.
14. `ScannerFilterTests` corpus cases ≡ `MockDataIntegrationTests` / `FolderScannerContainerTests` (Steam location, noise exclusion).
15. `StoreSignalDetectorTests.Has*_ReturnsFalse` (8) — one empty-dir negative owns the branch.
16. `SteamLibraryScannerTests.Scan_InstalledGame_HasSteamStatusExtra` / `Missing…Extra` ≡ their base status tests (merge one assert).
17. `SteamLibraryScannerTests.Scan_WithVdfDiscoveredPaths…` — asserts `Empty` under a “includes” name; delete.
18. Contract-pin “all distinct” tests (3) are implied by the exact-value pins.

---

## 6. Theory rows to trim

| Theory | Rows now | Keep | Why |
|---|---:|---:|---|
| `TitleTextTests.StripEdition_StripsEachTailToken` | 10 | 1 | one anchored-tail branch; the token list is pinned separately |
| `TagNormalizerTests.Normalize_TrimsAndCollapsesWhitespace` | 10 | 5 | trim/collapse, one non-space whitespace, empty, null |
| `LibraryManagerTests.IsChildOf_ReturnsExpected` | 10 | 0 | deleted with the delegate |
| `PathTextTests.IsUnder_IsSegmentAware` | 8 | 5 | formatting, prefix-not-segment, equal, reverse, empty |
| `WindowsExplorerTests.RejectsUnsafe` | 8 | 8 | each row is a different guard |
| `InstallIdentityTests.PathKey_NormalizesFormatting` | 7 | 0 | delegate; rows move to `PathTextTests` |
| `InstallStateMapTests.FromStoreStatus_Maps` | 7 | 4 | known states + empty/null |
| `FileSystemHelperTests` display-token rows | 7 | 2 | compound + generic token |
| `PcgwTitleFilterTests.RejectsNoiseTitles` | 5 | 2 | phrase + token |
| `PcgwTitleFilterTests.KeepsRealTitles` | 4 | 2 | two token-overmatch guards |
| `StoreSignalDetectorTests` marker rows | 34 facts | 6 theories | one row per predicate literal; no repeated false helpers |
| `SteamLibraryMembershipTests` rows | 14 | 8 | root itself, descendant, similar segment, empty, root hit/miss |

**Rule:** keep one row per distinct branch, plus each boundary (empty, null, separator, max/min). Rows that
differ only in data are deleted, not merged into a longer theory.

---

## 7. Test infrastructure to consolidate

| Item | Current | Change |
|---|---|---|
| Temp-dir fixture | 24 hand-rolled copies, 27 `IDisposable` classes | one `TempDirectory` helper in `TestSupport/` |
| `GameEntry` builder | 5 `MakeGame` variants + inline entries | one `TestGame` builder with named options |
| Config fake | 3 `StubConfig` + 2 other fakes | one `TestConfigService` |
| HTTP fake | `RoutingHandler` in one file | one shared `RoutingHandler` |
| `MockRegistryReader` | 144-line `.reg` parser + 12 tests | move to `TestSupport/`; delete its tests and unused dword/hex/`ReadKeyValues` surface |
| Fixtures | 3 suites climb to repo `testdata/` (104 files, real-machine samples) | move to `Fixtures/`; replace machine samples with synthetic ACF/VDF |
| `FailingReadConverter` | local to store tests | keep; only proof of write-verify failure |
| Empty `Detection.Tests` project | deprecated, 0 tests | delete at the phase boundary (not counted) |

Test-only production seam to remove: `RegistryFallbackDetector.NormalizePath` (pure delegate; two tests
deleted). Keep `InternalsVisibleTo`; keep internal members used by real internal callers.

---

## 8. Gaps to close before deleting coverage

These are untested today; add one focused test each **before** the deletion batch so the reduction cannot
hide them:

1. `LibraryManager.SelectScannerAndScan` routing: Steam structure → Steam scanner; manifests dir → Epic;
   plain root → `FolderScanner`.
2. `LibrariesDatabaseService` round-trip + missing/corrupt default (zero tests today).
3. `FolderScanner` pass 1c: an `IRegistryReader` supplied to the scanner actually classifies a folder.
4. `GameLaunchResolver` empty target: no exe + non-URI cmd → empty target, no process.
5. `SteamLibraryScanner`: missing VDF → configured roots only (not “vdf discovery”).
6. Scanner-level `Install.State` for one non-Steam store (currently hardcoded `Installed`; pin it before
   health work changes it).
7. `ShellViewModelSearchTests.TagSubstring_Matches` uses a tag-only needle (currently passes via title).
8. Optional: `StoreMetadataEnricher`/`TitleEnricher` one representative each (zero tests today).

Gaps not fixed here (recorded): E7 query ordering lives in `MainWindow.Metadata` (needs a UI seam);
`PcgwTitleFilter.FormatChoice`; `GameSourceParser.InferFromPath`; `VdfParser` null path; `GameRecord` after
deletion.

---

## 9. Execution order

Each batch: change → `dotnet build` → `dotnet test` → record count. No production code.

1. [x] **Test support + gap tests.** `TempDirectory`, `TestGame`, `TestConfigService`, `RoutingHandler`; add
   §8 tests 1–7. Suite green before deletions. **Done:** `TempDirectory` and `TestGame` added under
   `tests/GamingCommander.App.Tests/TestSupport/`; §8 tests 1–7 added (`TestConfigService`/`RoutingHandler`
   consolidation left for later batches). Suite green at **780 cases** (Core 233, App 546, Migration 1).
2. [x] **Trivial/obsolete removals.** `MockRegistryReaderTests`, `GameEntryTagsTests`,
   `FileSystemEntryKindTests`, `GameRecordTests`, `LibraryManagerTests.IsChildOf`, contract “distinct”
   tests, vacuous scoring/id tests. Expected: −30 methods. **Done:** 39 methods / 48 executed cases removed
   (`MockRegistryReaderTests` 12, `GameEntryTagsTests` 9, `FileSystemEntryKindTests` 4,
   `GameRecordTests` 1, contract distinctness 3, `LibraryManagerTests.IsChildOf` 1 method / 10 cases,
   `GameEntryIdTests` 9). Suite green at **732 cases** (Core 207, App 524, Migration 1).
3. [x] **Detection and scanning.** `StoreSignalDetectorTests`, `FallbackSignalTests`, `ScannerFilterTests`,
   `FolderScannerContainerTests`, `SteamLibraryScannerTests`, `SteamBootstrapTests`,
   `SteamLibraryMembershipTests`, `RegistryFallbackDetectorTests`, `PythonParityDetectionTests`,
   `BlacklistLoaderTests`, `FileSystemHelperTests`. **Done:** 104 methods / 57 executed cases removed
   (`StoreSignalDetectorTests` 57→20, `FallbackSignalTests` 16→10, `ScannerFilterTests` 16→7,
   `FolderScannerContainerTests` 21→13, `SteamLibraryScannerTests` 22→14, `SteamBootstrapTests` 6→5,
   `SteamLibraryMembershipTests` 7→3, `EpicManifestParserTests` 17→16, `EpicItemClassifierTests` 2→1,
   `EpicInstallPathTests` 3→1, `RegistryFallbackDetectorTests` 19→6, `PythonParityDetectionTests` 10→7,
   `MockDataIntegrationTests` 5→4, `LnkParserTests` 13→12, `EngineDetectorTests` 8→7,
   `BlacklistLoaderTests` 16→9, `FileSystemHelperTests` 2→1; `EpicLibraryScannerTests` 5 unchanged).
   Suite green at **675 cases** (Core 207, App 467, Migration 1).
4. [x] **Executables and identity.** `ExecutableDiscoveryTests`, `ExecutableScoringTests`, `GameEntryIdTests`,
   `InstallIdentityTests`, `IdentityPipelineTests`, `IdentityReconciliationTests`. **Done:** 30 methods /
   16 executed cases removed (`ExecutableDiscoveryTests` 18→10, `ExecutableScoringTests` 29→12,
   `InstallIdentityTests` 6→3 with path rows absorbed by `PathTextTests`, `IdentityPipelineTests` 3→2
   — one ordering test kept because the exe-stem-before-folder rule would otherwise have no coverage,
   `IdentityReconciliationTests` 8→7; `GameEntryIdTests` stays at 4). Suite green at **659 cases**
   (Core 201, App 457, Migration 1).
5. [x] **Persistence and services.** `GamesDatabaseServiceTests`, `JsonConfigServiceTests`,
   `JsonDocumentStoreTests`, `CollectionsDatabaseServiceTests`, `MetadataStoreTests`,
   `StorePolicyPersistenceTests`, `StorePolicyProviderTests`, `AppPathsTests`, writer/guard suites.
   **Done:** 15 files reduced by 39 methods / 47 executed cases:
   `GamesDatabaseServiceTests` 21→10, `JsonConfigServiceTests` 6→3, `JsonDocumentStoreTests` 10→8,
   `CollectionsDatabaseServiceTests` 3→2, `MetadataStoreTests` 7→4, `MetadataServiceTests` 9→8,
   `StorePolicyPersistenceTests` 7→5, `StorePolicyProviderTests` 9→8, `AppPathsTests` 4→3,
   `SteamAcfWriterTests` 11→7, `EpicItemWriterTests` 10→6, `SteamStoreLookupTests` 4→3,
   `SteamStoreParserTests` 7→6, `PcgwSectionParserTests` 13→10, `PcgwTitleFilterTests` 7→6.
   `MetadataLookupQueueTests` (3), `MetadataOnlineGateTests` (6), `PcgwInfoboxParserTests` (10) and
   `RepairWriteGuardTests` (11) are unchanged. The intermittent journal test was fixed without touching
   production code: the three journal-writing suites share one non-parallel collection and the journal
   line is read through a shared handle with a short retry (the line is still identified by its unique
   marker and parsed as JSON).
6. [x] **Metadata and helpers.** `GogInfoParserTests`, `EaInstallLogParserTests`, `SteamStore*`, `Pcgw*`,
   `TitleTextTests`, `TagNormalizerTests`, `VdfParserTests`, `MetadataDetailsFormatterTests`,
   `ShellViewModelSearchTests`, `WindowsExplorerTests`. **Done:** 10 files reduced by 33 methods / 45 cases
   (`GogInfoParserTests` 10→7, `EaInstallLogParserTests` 8→5, `TitleTextTests` 25→18,
   `TagNormalizerTests` 21→14, `VdfParserTests` 20→12, `WindowsExplorerTests` 6→5 with `MetadataTextTests`
   4 unchanged, `GameStoreIndexTests` 6→5, `EditionTokensTests` 2→1, `MetadataDetailsFormatterTests` 7→5;
   `ShellViewModelSearchTests` 8→8 with only the non-contract `LeftPaneTitle` string asserts dropped).
   Suite green at **566 cases** (Core 155, App 410, Migration 1), 420 methods.
7. [x] **Final pass.** Behavior map, count, remove dead test-only seams. **Done:** theory-row trim
   (never safety or external-format rows) removed **67 cases**: `StoreSignalDetectorTests` −9,
   `ExecutableScoringTests` −9, `FolderScannerContainerTests` −6, `FallbackSignalTests` −6,
   `SteamLibraryMembershipTests` −6, `RegistryFallbackDetectorTests` −4, `PathTextTests` −4,
   `ExecutableDiscoveryTests` −3, `FileSystemHelperTests` −3, `BlacklistLoaderTests` −2,
   `DetectionTierDeriverTests` −2, `SteamLibraryScannerTests` −2, `TagNormalizerTests` −2,
   `GameSourceParserTests` −2, `TitleTextTests` −5, `LnkParserTests` −1, `EpicManifestParserTests` −1.
   Final suite: **499 cases** (Core 142, App 356, Migration 1), 420 methods, 0 skipped.
   Behavior map filled below in §12. `TempDirectory`, `TestGame` and `MockRegistryReader` remain in use;
   no dead test-only helper was left behind.

If a deletion exposes a real product bug, stop and fix/record it; do not delete the failing test.

---

## 10. Acceptance

- [x] Executed cases **400–500** (hard ceiling 500; expected ~450). Final: **499**.
- [x] Every deleted method has a recorded reason in this plan or the task audit.
- [x] No safety test removed: write refusals, staging/verify, backup/restore, path containment, unsafe
      explorer inputs, registry/write guards.
- [x] No external-format pin removed: ACF/VDF, `.item`/`.mancpn`, GOG `.info`, EA log, Steam/PCGW fixtures.
- [x] Every row in the connection map’s per-store failure table maps to a test or a recorded gap (§12).
- [x] No `[Skip]`; no disabled test; no test-only helper left unused.
- [x] `dotnet build` 0 errors; full suite green.
- [ ] `CURRENT.md` records the new totals and the behavior-map result. (Deferred to the integrating
      session: the reduction pass was told not to edit `CURRENT.md`.)

## 11. Stop rules

- Do not delete a test to hit a number.
- Do not weaken an assertion to make a merge possible.
- Do not merge tests that fail for different reasons.
- Do not touch production code; test-only support is allowed.
- Do not keep a test for an unwired feature just because it exists; keep a minimal contract and return the
  rest with the wiring task.
- If the target cannot be met without touching safety or external-format coverage, stop at the higher count
  and report.

## 12. Final behavior map (batches 5–7)

### 12.1 Per-file methods (before → after) and executed cases after

| File | Methods | Cases after | Note |
|---|---:|---:|---|
| `GamesDatabaseServiceTests` | 21 → 10 | 10 | full DTO round-trip; store corruption recovery left to the store |
| `JsonConfigServiceTests` | 6 → 3 | 3 | defaults + scan depth merged; backup recovery owned by the store |
| `JsonDocumentStoreTests` | 10 → 8 | 8 | three read states, one `.bak`, verify failure, corrupt backup; outside-root harness case deleted |
| `CollectionsDatabaseServiceTests` | 3 → 2 | 2 | smart-collection assertion merged into the round-trip |
| `MetadataStoreTests` | 7 → 4 | 4 | missing/unknown merged; details round-trip absorbs field round-trip |
| `MetadataServiceTests` | 9 → 8 | 8 | details preservation merged into the merge test |
| `MetadataLookupQueueTests` | 3 → 3 | 3 | unchanged |
| `MetadataOnlineGateTests` | 6 → 6 | 6 | unchanged (egress safety) |
| `StorePolicyPersistenceTests` | 7 → 5 | 5 | enum-spelling and missing-file duplicates dropped |
| `StorePolicyProviderTests` | 9 → 8 | 10 | stored-row re-filter dropped (config load owns it); non-launcher theories 3→2 rows |
| `AppPathsTests` | 4 → 3 | 3 | root/path assertions merged; both containment tests kept |
| `RepairWriteGuardTests` | 11 → 11 | 12 | every safety branch kept; journal test made isolation-safe |
| `SteamAcfWriterTests` | 11 → 7 | 7 | guard-owned preconditions dropped; format/create-only/no-temp/preview/read-only kept |
| `EpicItemWriterTests` | 10 → 6 | 6 | guard-owned preconditions dropped; unknown members, launch, wrong dir, guid, preview, read-only kept |
| `SteamStoreLookupTests` | 4 → 3 | 3 | fetch-then-parse duplicate dropped (parser owns mapping) |
| `SteamStoreParserTests` | 7 → 6 | 6 | failed-facts mapping merged into the unsuccessful-facts test |
| `PcgwInfoboxParserTests` | 10 → 10 | 10 | unchanged (external format pins) |
| `PcgwSectionParserTests` | 13 → 10 | 10 | token rows and combined-fixture fixbox asserts merged |
| `PcgwTitleFilterTests` | 7 → 6 | 9 | year-hint outcomes merged into one theory; noise/keep rows trimmed |
| `GogInfoParserTests` | 10 → 7 | 8 | relative/absolute path pair merged; no-exe/no-primary and empty/malformed merged |
| `EaInstallLogParserTests` | 8 → 5 | 5 | wrong-location duplicate dropped (standard log has a foreign path); name-only variants merged |
| `TitleTextTests` | 25 → 18 | 23 | duplicate edition/acronym rows and duplicate helpers removed |
| `TagNormalizerTests` | 21 → 14 | 24 | whitespace/equivalence rows merged; dedupe/parse/metadata/merge kept |
| `VdfParserTests` | 20 → 12 | 12 | single-pair/tabs/nesting repetitions dropped; ACF/VDF shapes, escapes, malformed, extraction kept |
| `WindowsExplorerTests` | 6 → 5 | 12 | quoted-args duplicate merged; all 8 unsafe rejects kept (`MetadataTextTests` 4 unchanged) |
| `GameStoreIndexTests` | 6 → 5 | 5 | `NormalizePath` delegate dropped (owned by `PathTextTests`) |
| `EditionTokensTests` | 2 → 1 | 1 | both token sets pinned in one test |
| `MetadataDetailsFormatterTests` | 7 → 5 | 5 | token-expansion duplicates dropped (owned by `PcgwSectionParserTests`) |
| `ShellViewModelSearchTests` | 8 → 8 | 8 | only non-contract title-string asserts removed |
| `LaunchArgumentComposerTests` | 5 → 5 | 5 | unchanged |
| `InstallStateMapTests` | 1 → 1 | 6 | dropped only the empty-string row that duplicates the null default (**deviation** from 7→4: each remaining row is a distinct store-status literal) |
| `GameSourceParserTests` | 2 → 2 | 4 | label rows trimmed |
| `GameFilterMatcherTests` | 4 → 4 | 4 | unchanged |
| `GameLaunchResolverTests` | 3 → 3 | 3 | includes the empty-target gap test |
| `PathTextTests` | 5 → 5 | 15 | `IsUnder` rows 8→5 per §6; null/whitespace rows absorbed earlier |
| Contract pins (`SteamAcfFields`, `EpicItemSchema`, `TitleSourceValues`) | 1 each | 1 each | verified, one value pin each |
| `StoreSignalDetectorTests` | 20 | 33 | marker rows trimmed to one per predicate family |
| `ExecutableScoringTests` | 12 | 16 | backup/org penalties collapsed to three variants |
| `FolderScannerContainerTests` | 13 | 15 | UE/platform/non-game/blizzard rows trimmed |
| `FallbackSignalTests` | 10 | 10 | duplicate deep-dir/noise rows trimmed |
| `RegistryFallbackDetectorTests` | 6 | 10 | moved-name and path-formatting rows trimmed; per-store shapes kept |
| `ExecutableDiscoveryTests` | 10 | 11 | platform probes 4→1 |
| `LnkParserTests` | 12 | 12 | bare-name row dropped (path row remains) |
| `BlacklistLoaderTests` | 9 | 10 | tier-key rows 4→2 |
| `DetectionTierDeriverTests` | 9 | 10 | other-store rows 4→2 |
| `SteamLibraryMembershipTests` | 3 | 8 | rows 14→8 per §6 (root itself, descendant, similar segment, empty, root hit/miss) |
| `SteamLibraryScannerTests` | 14 | 15 | two duplicate row pairs trimmed |
| `EpicManifestParserTests` | 16 | 16 | path-formatting row 2→1 |
| `FileSystemHelperTests` | 1 | 2 | display-token rows 5→2 |

### 12.2 Connection-map §8 failure points → owning test

| Store | Failure point | Owning test |
|---|---|---|
| Steam | root unreadable | `ScannerFilterTests` / `SteamLibraryScannerTests.Scan_WithoutLibraryPath_ReturnsEmpty` |
| Steam | vdf missing → configured roots only | `SteamBootstrapTests` (missing-VDF gap test) |
| Steam | ACF malformed | `SteamLibraryScannerTests` (malformed ACF case) |
| Steam | folder absent / moved / orphaned | `SteamLibraryScannerTests` health tests, `SteamLibraryMembershipTests` |
| Steam | repair: `steamapps` missing, no `libraryfolders.vdf`, target exists | `RepairWriteGuardTests.Preflight_MissingDirectory_RefusedAndNotCreated`, `Preflight_Steam_RequiresMarkerAndAbsentTarget`; `SteamAcfWriterTests.TryWrite_CreatesFileOnce` |
| Steam | launch: no appid → exe fallback, no exe → status only | `GameLaunchResolverTests.SteamUri_AlwaysSteam_IgnoresExtras`, `EmptyTarget_NoExeAndNoUri_ReturnsEmptyTargetAndArguments` |
| Epic | manifests dir missing | `EpicLibraryScannerTests` + `RepairWriteGuardTests.Preflight_Epic_RequiresExistingParsableItem` |
| Epic | item unparseable | `EpicManifestParserTests`, `RepairWriteGuardTests.Preflight_Epic_RequiresExistingParsableItem` |
| Epic | install path absent | `EpicLibraryScannerTests` (orphan case) |
| Epic | repair: different install id | `EpicItemWriterTests.TryWrite_DifferentInstallationGuid_Refused` |
| Epic | repair: wrong manifests dir | `EpicItemWriterTests.TryWrite_NotTheScanManifestsDir_Refused` |
| Epic | repair: unknown members preserved | `EpicItemWriterTests.TryWrite_MovedGame_UpdatesOwnedMembersAndPreservesTheRest` |
| GOG | `.info` malformed / no exe / no primary | `GogInfoParserTests.TryParse_NoUsableInfo_ReturnsFalse`, `TryParse_ExeFallback_NoTasksThenNoPrimary` |
| GOG | signal absent | `StoreSignalDetectorTests.DetectType_EmptyDir_ReturnsUnknown`, `FallbackSignalTests` |
| EA | no `__Installer` / registry missing or stale | `StoreSignalDetectorTests.DetectType_EaMarkers_ReturnsEaApp`, `RegistryFallbackDetectorTests.Detect_NoMatch_ReturnsUnknown` |
| EA | install log stale path / no config lines | `EaInstallLogParserTests.TryParse_StandardLog_ExtractsAllFields`, `TryParse_NoLogOrNoConfigLines_ReturnsFalse` |
| Ubisoft | signal absent / registry missing | `StoreSignalDetectorTests` Ubisoft markers, `RegistryFallbackDetectorTests` |
| Ubisoft | emulator INI absent/present | `StoreSignalDetectorTests.DetectType_UbisoftEmulatorWithoutIni_ReturnsUnknown`, `…WithIni_ReturnsUbisoftConnect` |
| Battle.net | marker absent / product codename | `StoreSignalDetectorTests.DetectType_BlizzardMarkers_ReturnsBattleNet`, `ExtractBlizzardProduct_*` |
| Xbox | marker absent | `StoreSignalDetectorTests.DetectType_XboxMetadata_ReturnsXbox` |
| Rockstar | launcher key skipped / game key missing | `RegistryFallbackDetectorTests.Detect_RockstarGame_SkipsLauncherSubkey`, `Detect_NoMatch_ReturnsUnknown` |
| Standalone | no plausible exe | `ExecutableDiscoveryTests`, `FallbackSignalTests.DetectFallback_NoSignal_ReturnsUnknown` |
| Standalone | all candidates are noise | `FallbackSignalTests.DetectFallback_RootExeAllNoise_ReturnsUnknown`, `ExecutableScoringTests.ScoreExecutable_BackupVariant_Penalized` |
| Standalone | deep tree beyond scan depth | `ExecutableDiscoveryTests.FindExecutablesDeep_DepthBoundary`, `FolderScannerContainerTests.Scan_ContainerRecursion_BoundedAtDepth2` |
| All | manifest points outside root | `FolderScannerContainerTests` (parent-bound child), `SteamLibraryMembershipTests.IsUnderSteamLibrary_NotUnderCommon_ReturnsFalse` |
| All | duplicate folder in two anchors | `GamesDatabaseServiceTests.SetGamesForLibrary_EntriesAreScopedToLibrary`, `IdentityReconciliationTests` |
| Persistence | corrupt store / backup recovery | `JsonDocumentStoreTests.Read_CorruptWithBackup_RecoversBackupAndPreservesDamagedBytes`, `…CorruptWithoutBackup…`, `…CorruptWithCorruptBackup…` |
| Persistence | write verification failure | `JsonDocumentStoreTests.Write_WhenVerificationFails_RestoresBackupAndThrows` |
| Writes | staging + length verify + undo | `RepairWriteGuardTests.Stage_*`, `CopyAndVerify_*` |
| Writes | external directory never created | `AppPathsTests.EnsureAppDataPath_OutsideRoot_ThrowsAndCreatesNothing`, `RepairWriteGuardTests.Preflight_MissingDirectory_RefusedAndNotCreated` |
| Metadata | egress gate states | `MetadataOnlineGateTests` (6) |
| Metadata | queue dedupe/gate order | `MetadataLookupQueueTests` (3) |
| Metadata | stale/fresh cache, merge, source filter | `MetadataServiceTests` |

Recorded gaps (unchanged): E7 query ordering in `MainWindow.Metadata` (needs a UI seam);
`PcgwTitleFilter.FormatChoice`; `GameSourceParser.InferFromPath`; `VdfParser` null path; GOG/EA/Ubisoft/
Battle.net/Xbox/Rockstar health states (hardcoded `Installed`); store policy wiring (unwired contract only).

### 12.4 Removed methods (batches 5–7) and why

Categories: **dup** = same behavior owned elsewhere; **merge** = assertion moved into the surviving test;
**row** = theory row deleted (same branch, data-only difference); **impl** = non-contract detail.

| File | Removed methods | Category |
|---|---|---|
| `GamesDatabaseServiceTests` | `Load_WithValidFile…`, `SaveLoad_TrackingInstallUserState…`, `SaveLoad_ProfilesRelationsTags…`, `SaveLoad_DetectionAndStore…`, `SaveLoad_StoreIdentityProvenance…`, `Save_CreatesFile_OnDisk`, `Save_ThenLoad_PreservesAllFields` | merge into `SaveLoad_FullDto_RoundTrip` |
| | `RetagGame_PreservesEntryId` | merge into `RetagGame_ChangesSourceTypeAndKeepsEntryId` |
| | `GetGamesForLibrary_UnknownLibrary_ReturnsEmpty` | merge into scoped-replace test |
| | `Save_WithCorruptFile_OverwritesCorrupt`, `Load_WithCorruptFileAndBackup…`, `EpicCatalogRescan…` | dup (store / identity owner) |
| | `SetGamesForLibrary_EquivalentPaths…` second row | row |
| `JsonConfigServiceTests` | `Load_WithMissingFile_DefaultsScanDepthTo3`, `Save_ScanDepth_RoundTrips` | merge |
| | `Load_WithCorruptFileAndBackup…` | dup (store) |
| `JsonDocumentStoreTests` | `Read_TruncatedFileWithBackup…` | row/branch dup of corrupt-with-backup |
| | `Write_ToExistingParentOutsideAppRoot_Succeeds` | impl (harness) |
| `CollectionsDatabaseServiceTests` | `SmartCollection_IsSmart` | merge |
| `MetadataStoreTests` | `Get_UnknownId_ReturnsNull`, `Upsert_ThenGet_RoundTripsMergedFields`, `HasDisplayableExtras_EmptyRecord_IsFalse` | merge |
| `MetadataServiceTests` | `Merge_KeepsDetailsWhenSteamHasNone` | merge |
| `StorePolicyPersistenceTests` | `Load_MissingFile_ReturnsEmptyPolicies` | dup |
| | `Save_WritesKindAsEnumName` | impl |
| `StorePolicyProviderTests` | `Get_IgnoresNonLauncherRowsInStoredSettings` | dup (config load filters) |
| | non-launcher rows (`Unknown`) | row |
| `AppPathsTests` | `AppDataPaths_ReturnPathsUnderAppDataRoot` | merge |
| `RepairWriteGuardTests` | — (journal read made isolation-safe) | — |
| `SteamAcfWriterTests` | `TryWrite_MissingSteamapps…`, `TryWrite_MissingLibraryFolders…`, `TryWrite_RelativeOrUnc…` | dup (guard preflights) |
| | `IsAppId_RejectsJunk` | merge |
| `EpicItemWriterTests` | `TryWrite_AbsentTarget…`, `TryWrite_MissingManifestsDir…`, `TryWrite_CorruptTarget…`, `TryWrite_MissingRequiredMember…` | dup (guard preflights) |
| `SteamStoreLookupTests` | `FetchRaw_ThenParse_ProducesRecord` | dup (parser) |
| `SteamStoreParserTests` | `CommonParser_UnsuccessfulFacts_ReturnsNull` | merge |
| `PcgwSectionParserTests` | `ParseGameDataPaths_UserProfileDocuments`, `ResolveWindows_UplayUidSavePath`, `ParseFixboxes_DirectExeAndBypassArgs` | merge |
| `PcgwTitleFilterTests` | `PickBest_YearHint2008_KeepsOriginal` | merge (one year-hint theory) |
| | noise/keep rows (7) | row |
| `GogInfoParserTests` | relative/absolute path pair, empty/malformed pair, no-exe/no-primary pair | merge |
| `EaInstallLogParserTests` | `TryParse_NoConfigLines_ReturnsFalse` | merge |
| | name-only pair | merge |
| | `TryParse_WrongInstallLocation_StillExtractsMetadata` | dup (standard log already logs a foreign path) |
| `TitleTextTests` | `StripEdition_Reloaded`, `LookupName_EpicUsesItemDisplayName`, `AcronymMatchesTitle_Eve…`, `AcronymMatchesTitle_Toee…` | dup (same branch/rule) |
| | `IsGenericLabel_System`, `SharesNameToken_RejectsSystemForElex` | merge |
| | acronym false-positive pair | merge |
| | `StripEdition_StripsEachTailToken` rows 10→1, other row trims (5) | row |
| `TagNormalizerTests` | `AreEquivalent_NormalizesWhitespace`, `AddDistinct_AddsNewTag`, `AddDistinct_NormalizesAddedTag`, `ParseFromCommaSeparated_SingleTag` | merge |
| | empty/whitespace pairs (2) | merge |
| | `ToCommaSeparated_EmptyList` + `_SingleTag` | merge into `_Shapes` |
| | whitespace/equivalence/empty rows (5) | row |
| `VdfParserTests` | `Parse_SingleKeyValue`, `Parse_MultipleNestedBlocks`, `Parse_WhitespaceOnly`, `Parse_QuotedValuesWithSpaces`, `Parse_TabsAndSpacesBoth`, `Parse_QuoteInMiddleOfValue`, `ExtractFields_MissingKey`, `ExtractFields_MalformedInput` | merge/row (shapes kept) |
| `WindowsExplorerTests` | `Build_QuotesPathsWithSpaces` | merge |
| `GameStoreIndexTests` | `NormalizePath_UnifiesSeparatorsAndTrailingSeparators` | dup (PathText) |
| `EditionTokensTests` | one pin method | merge |
| `MetadataDetailsFormatterTests` | `WindowsSaves_ResolvesUplayAndUid`, `WindowsSaves_UserProfileDocumentsToken` | dup (`PcgwSectionParserTests`) |
| `ShellViewModelSearchTests` | — (only `LeftPaneTitle` asserts dropped) | impl |
| `GameSourceParserTests` | two label rows | row |
| `InstallStateMapTests` | empty-string row | row (null covers the default arm) |
| `PathTextTests` | `NormalizeForCompare` 3 rows, `IsUnder` 3 rows, `Leaf` 1 row | row (boundaries kept) |
| `StoreSignalDetectorTests` | marker rows: GOG 4→2, Ubisoft 8→4, SteamEmu 3→2, readme publisher/title pair + line-2 pair | row |
| `ExecutableScoringTests` | backup 9→3, folder-name 3→2, exact-token 2→1, shipping 2→1 | row |
| `FolderScannerContainerTests` | UE 5→3, non-game 3→1, platform 2→1, Blizzard 2→1 | row |
| `FallbackSignalTests` | deep-dir rows 3→1, UbiStats 2→1, UE platform 2→1, no-signal 2→1, deep-signal 2→1 | row |
| `RegistryFallbackDetectorTests` | moved-name 4→2, no-match 2→1, path-formatting 2→1 | row (per-store shapes kept) |
| `ExecutableDiscoveryTests` | platform probes 4→1 | row |
| `LnkParserTests` | bare-name row | row |
| `BlacklistLoaderTests` | tier-key rows 4→2 | row |
| `DetectionTierDeriverTests` | other-store rows 4→2 | row |
| `SteamLibraryMembershipTests` | rows 14→8 | row |
| `SteamLibraryScannerTests` | known-subfolder 2→1, internal folder 2→1 | row |
| `EpicManifestParserTests` | path-formatting row 2→1 | row |
| `FileSystemHelperTests` | display-token rows 5→2 | row |

No safety test, write refusal, staging/verify, path-containment, unsafe-explorer, registry-shape or
external-format pin was deleted or weakened.

### 12.5 Do not add

New tests must name the behavior or failure they protect: no test per method, no constructor smoke, no
duplicate of an external-format pin, no second owner for a behavior listed in §3.


