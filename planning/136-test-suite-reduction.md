# 136 — Test Suite Reduction: Scope and Plan

**Status:** investigation complete; execution pending.
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
5. **Persistence and services.** `GamesDatabaseServiceTests`, `JsonConfigServiceTests`,
   `JsonDocumentStoreTests`, `CollectionsDatabaseServiceTests`, `MetadataStoreTests`,
   `StorePolicyPersistenceTests`, `StorePolicyProviderTests`, `AppPathsTests`, writer/guard suites.
6. **Metadata and helpers.** `GogInfoParserTests`, `EaInstallLogParserTests`, `SteamStore*`, `Pcgw*`,
   `TitleTextTests`, `TagNormalizerTests`, `VdfParserTests`, `MetadataDetailsFormatterTests`,
   `ShellViewModelSearchTests`, `WindowsExplorerTests`.
7. **Final pass.** Behavior map, count, remove dead test-only seams, update `CURRENT.md`.

If a deletion exposes a real product bug, stop and fix/record it; do not delete the failing test.

---

## 10. Acceptance

- [ ] Executed cases **400–500** (hard ceiling 500; expected ~450).
- [ ] Every deleted method has a recorded reason in this plan or the task audit.
- [ ] No safety test removed: write refusals, staging/verify, backup/restore, path containment, unsafe
      explorer inputs, registry/write guards.
- [ ] No external-format pin removed: ACF/VDF, `.item`/`.mancpn`, GOG `.info`, EA log, Steam/PCGW fixtures.
- [ ] Every row in the connection map’s per-store failure table maps to a test or a recorded gap.
- [ ] No `[Skip]`; no disabled test; no test-only helper left unused.
- [ ] `dotnet build` 0 errors; full suite green.
- [ ] `CURRENT.md` records the new totals and the behavior-map result.

## 11. Stop rules

- Do not delete a test to hit a number.
- Do not weaken an assertion to make a merge possible.
- Do not merge tests that fail for different reasons.
- Do not touch production code; test-only support is allowed.
- Do not keep a test for an unwired feature just because it exists; keep a minimal contract and return the
  rest with the wiring task.
- If the target cannot be met without touching safety or external-format coverage, stop at the higher count
  and report.
