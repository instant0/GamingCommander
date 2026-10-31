# Task T134: Test Suite Reduction — One Test per Behavior

**Tier:** 2 — Code Quality
**Phase:** G — Code Quality and Tests
**Risk:** Low (tests only)
**Status:** completed (2026-10-31) — batches 1–7 done; 499 cases / 420 methods
**Prerequisites:** Task T135 (done: version/back-compat tests removed)

## Investigation result

Full scope and the per-file deletion/merge plan are in
[`planning/136-test-suite-reduction.md`](../../../planning/136-test-suite-reduction.md).

Baseline is **657 methods / 773 executed cases** (Core 232, App 540, Migration 1). The task table below
counts methods; executed cases are higher because of theories. Merging facts into a theory does not reduce
the count — rows must be deleted.

Target shape: **400–500 executed cases, hard ceiling 500, expected ~450**. Per-file method plan sums to 422
(415 + seven gap tests); theory-row trimming and row deletion land near 450. Safety and external-format
tests are excluded from reduction.

Execution is in seven batches: test support + gap tests, trivial removals, detection/scanning,
executables/identity, persistence/services, metadata/helpers, final behavior map.

## Objective

Reduce the suite to the smallest set that still proves every product behavior and failure mode. A test
earns its place only if its failure would change a decision. Count is an outcome, not the target.

Baseline after T135: **657 methods / 773 cases** (Core 232, App 540, Migration 1). The concentration is
visible:

| File | Test methods |
|---|---|
| `StoreSignalDetectorTests` | 57 |
| `ExecutableScoringTests` | 29 |
| `TitleTextTests` | 25 |
| `GamesDatabaseServiceTests` | 21 |
| `TagNormalizerTests` | 21 |
| `SteamLibraryScannerTests` | 21 |
| `FolderScannerContainerTests` | 21 |
| `VdfParserTests` | 20 |
| `RegistryFallbackDetectorTests` | 19 |
| `ExecutableDiscoveryTests` | 18 |
| `EpicManifestParserTests` | 17 |
| `ScannerFilterTests` / `FallbackSignalTests` / `BlacklistLoaderTests` | 16 each |

These numbers are a starting point, not a verdict.

## What counts as a redundant test

1. **Same behavior, two owners.** A service test re-asserts what the store test already proves; a scanner
   test re-asserts an ACF/parser case; `SteamBootstrapTests` and `SteamLibraryScannerTests` cover the same
   locator/parse path. Keep the test at the lowest level that owns the behavior.
2. **Same branch, more data.** Theory rows that all take the same `if` branch. Keep one representative plus
   each boundary (empty, one, separator, max, malformed).
3. **Implementation detail.** Private method names, call order, DI wiring, string formatting that is not a
   contract, exact exception messages that no user sees.
4. **Trivial surface.** Record get/set, constants compared to themselves, constructor smoke tests. Exempt:
   pin tests for external formats (ACF, `.item`, `.mancpn`, VDF, GOG/EA log shapes).
5. **Obsolete behavior.** Tests for removed features, old normalisers, removed writers, and anything the
   back-compat task deletes.
6. **Parity without a contract.** `PythonParityDetectionTests` / `MockDataIntegrationTests` rows that repeat
   a detection case already covered and do not pin an external fixture or a known divergence.

## What must stay

- Safety and preconditions: write refusal, no external directory creation, path containment, staging,
  length/parse verification, `.bak`/`.corrupt` recovery.
- External format contracts: Steam ACF/VDF, Epic `.item`/`.mancpn`, GOG `.info`, EA logs, registry shapes.
- One test per failure mode named in `META/CONNECTION_MAP.md` §8 (missing marker, corrupt manifest, absent
  folder, wrong id, ambiguous match).
- One test per detection branch that can change a verdict (store priority, container promotion, parent-bound
  child, Steam vs SteamEmu, non-game filtering).
- The UI/identity tests that protect user data (relocation, tags/overrides preservation, filter/search).

## Method

1. **Inventory.** For each test file, list each test and the behavior it proves in one phrase. Group by
   behavior, not by file.
2. **Classify.** `keep` / `merge into <test>` / `delete (duplicate)` / `delete (implementation detail)` /
   `delete (obsolete)` / `move to back-compat task`. Record the reason.
3. **Delete or merge.** One behavior = one focused test plus its boundary rows. Delete tests, do not
   `[Fact(Skip=...)]` them; no disabled tests remain.
4. **Re-run.** Full suite green. If a deletion exposes an untested behavior, keep one test for it.
5. **Record.** Write `planning/136-test-suite-reduction.md` with the inventory, decisions, before/after
   counts per file, and the behavior-to-test map for the connection map’s rows.

## Deliverable

`planning/136-test-suite-reduction.md`:

- [x] Before/after counts per file and total.
- [x] Every removed test with a reason (duplicate / same branch / implementation detail / obsolete /
      back-compat).
- [x] A behavior → test map: every row in the connection map’s decision tree has at least one test.
- [x] A short “do not add” note: new tests must name the behavior/failure they protect; no test per method,
      no test per constructor, no duplicate of an external-format pin.
- [x] No task IDs in the plan.

## Requirements

- [x] No production code change. If a test needs a seam, record it; do not add a seam in this task.
- [x] Every deleted test has a recorded reason.
- [x] No safety/failure test is removed.
- [x] No disabled/skipped tests are left behind.
- [x] Suite is green; the count is materially lower.
- [x] The behavior → test map covers the connection map’s per-store failure points.

## Verification

- [x] `dotnet build` 0 errors; `dotnet test` green.
- [x] `grep -rn "Skip =" tests` shows no new skips.
- [x] `planning/136-test-suite-reduction.md` exists and lists every deletion.
- [ ] The new total is recorded in `META/SESSION/CURRENT.md`. (Left to the integrating session; this pass
      was told not to edit `CURRENT.md`.)
- [x] No test asserts a private method, call order, or non-contract string.

## Stop rules

- **Do not delete a test to hit a number.** If it protects a distinct behavior, it stays.
- **Do not touch production code or fixtures** beyond removing unused test-only helpers.
- **Do not merge tests that fail for different reasons.**
- **Do not weaken assertions** to make a merge easier.
- **One area per change** (detection, scanner, persistence, metadata, UI) so a regression is attributable.

## Completion Notes

- **Completed:** 2026-10-31 (batches 1–7).
- **What was done:** reduced the suite from 773 cases (baseline) / 659 at batches 5-start to **499 cases**
  (Core 142, App 356, Migration 1) and **420 methods**. Batches 5–6: persistence/services/metadata/helper
  files reduced per plan §4–§5. Batch 7: theory-row trim per §6 (67 cases), connection-map behavior map
  (§12 of the plan), and a flakiness fix for the repair-journal test. No production code touched.
- **Verification:** `dotnet build GamingCommander.sln` 0 warnings / 0 errors; `dotnet test` green
  (499 passed, 0 failed, 0 skipped), run three times. No `[Skip]`/disabled tests. `TempDirectory`,
  `TestGame` and `MockRegistryReader` remain in use.
- **Issues encountered:**
  - Journal test: `repair-journal.jsonl` is one shared append-only file under `data/`; concurrent appends
    could swallow a line. Fixed test-side by putting `RepairWriteGuardTests`, `SteamAcfWriterTests` and
    `EpicItemWriterTests` in one non-parallel collection and reading through a shared handle with a short
    retry; the line is still found by its unique marker and parsed as JSON. No production seam added.
  - Deviations: `InstallStateMapTests` keeps 6 rows (each remaining row is a distinct store-status
    literal; only the empty-string row was dropped instead of trimming to 4); `GamesDatabaseServiceTests`
    keeps one equivalent-path row instead of two (same branch); `SteamLibraryScannerTests` keeps both
    "missing path" / "no common folder" rows (two distinct empty-result branches).
  - A first attempt at swapping a platform-subdir theory row to `WinGDK` failed: `WinGDK` sibling folders
    are treated as games. Not introduced as coverage; recorded here rather than changing production code.

