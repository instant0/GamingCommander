# Task T118B: Epic Inventory Reader

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** completed (2026-10-31)
**Prerequisites:** None (consumes the shared `StoreInventoryEntry`/`IStoreInventoryReader` contract delivered by T118A); T130 done

> **Review revision (2026-10-31):**
> - `StoreReportedState` has no `Missing` value. “State Missing” means the entry is returned with
>   `FolderExists = false`; the import service maps that to `InstallState.Missing`. Do not add a
>   store-reported `Missing` variant.
> - Resolve the launch exe directly from `.item` `LaunchExecutable` / binary `.manifest` and check
>   existence once. Do not call the recursive `EpicLaunchResolver`/`ExecutableDiscovery` from this
>   read-only inventory path.
> - Extend `EpicManifestParser.EpicItemData` with optional `InstallSize`, `AppVersionString`,
>   `InstallationGuid`, `CompleteManifestPath` at the end; existing callers use named args.
> - Orphan discovery is `EpicOrphanDiscovery.Find(catalog, otherRoots)`: folders with `.egstore`
>   that the catalog does not claim. The import service dedupes by path; do not add a second claim
>   model here.

> **Test policy:** follow `planning/136-test-suite-reduction.md` — one test per behavior/failure, no
> back-compat tests.

## Objective

Read Epic's known inventory: ProgramData `*.item` manifests. Epic identity is the catalog item; local
`.mancpn`/binary `.manifest` fill identity gaps when an `.item` is absent. No LauncherInstalled primary, no
GraphQL, no writes.

## What Needs to Change

- [x] Primary inventory: `EpicItemCatalog` over `EpicManifestPaths.GetManifestsDir()`.
- [x] When an `.item` is absent, use local `.mancpn` and binary `.manifest` for catalog ids/install/exe.
- [x] Use `EpicOrphanDiscovery` only for folders the catalog does not claim, without duplicating the
      scanner's claim logic.
- [x] Read `InstallSize`, `AppVersionString`, `InstallationGuid`, `CompleteManifestPath` when present; never
      require them. Extend the `.item` parser/schema with these optional fields.
- [x] Return records for missing install folders with `FolderExists = false` (mapped to `Missing` by the
      import service), not just existing ones.
- [x] No `.item` writes, no registration, no `.item` regeneration.

## Tests

- [x] `.item` catalog → records with ids/path/exe/state.
- [x] Catalog metadata carries `InstallSize`, `AppVersionString`, `InstallationGuid`, `CompleteManifestPath`.
- [x] `.mancpn` fallback when no `.item`.
- [x] Binary `.manifest` fallback supplies the launch exe/version/guid.
- [x] Corrupt `.item` skipped, not fatal.
- [x] Missing folder → `FolderExists = false`, record retained.
- [x] Orphan folder surfaced without duplicating an already-claimed item.
- [x] Temp dirs/fixtures; no ProgramData.

## Verification

- [x] `dotnet build` 0 errors; `dotnet test` green.
- [x] No writes, network, or GraphQL.
- [x] Existing Epic catalog tests green.

## Stop rules

- Do not use `LauncherInstalled.dat` as primary; it is a cross-check only if verified.
- Do not add EA here (T118B2).
- Do not recreate/register `.item` files.
- Do not call `EpicLaunchResolver`/`ExecutableDiscovery` from the reader; use the manifest exe only.

## Completion Notes

- **Completed:** 2026-10-31
- **What was done:**
  - Extended the `.item` schema/parser with optional `InstallSize`, `AppVersionString`,
    `InstallationGuid`, and `CompleteManifestPath`; existing callers are unchanged.
  - Added `EpicInventoryMetadata` and `EpicInventoryReader`. It reads `EpicItemCatalog.Playable`,
    returns every playable base game including entries whose install folder is gone
    (`FolderExists = false`), and carries the optional manifest fields.
  - The launch exe is resolved directly from `.item` `LaunchExecutable` and checked once; no
    `EpicLaunchResolver`/`ExecutableDiscovery` recursion from this read path.
  - For folders the catalog does not claim, the reader uses `EpicOrphanDiscovery.Find` plus local
    `.mancpn` (`ExtractLocalIdentifiers`) and binary `.manifest` (`EpicBinaryManifest`) for catalog
    ids, app name, build version, installation GUID, and launch exe.
  - Duplicate catalog items that name the same physical install collapse to one row, preferring the
    record that resolves a real exe.
  - Read-only: no `.item` writes, registration, `.item` regeneration, GraphQL, or network.
- **Verification:** `dotnet build` 0 errors (only the five pre-existing warnings); `dotnet test`
  **580 green** (Core 157, App 423). New tests cover catalog metadata, missing folder, corrupt item,
  `.mancpn` fallback, binary `.manifest` fallback, catalog-claim exclusion, duplicate installs, and
  the empty case. `python3 tools/validate_probe_matrix.py` passes all 22 scenarios.
- **Issues encountered:** `StoreReportedState` has no `Missing` value; the reader reports
  `FolderExists = false` and `StoreState = Unknown`, and the import service maps that to
  `InstallState.Missing` (as noted in the review revision). The reader is not yet wired into the
  import service (T118) or composition.
