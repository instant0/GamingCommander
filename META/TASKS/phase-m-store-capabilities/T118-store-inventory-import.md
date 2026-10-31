# Task T118: Read-Only Store Inventory Import

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** High
**Status:** pending
**Prerequisites:** Task T118 depends on Task T117, Task T120, Task T123, and Task T130 (it persists through the shared store)

> **Test policy (2026-10-31):** new tests follow `planning/136-test-suite-reduction.md` — use
> `TestSupport/TempDirectory` and `TestGame`, one test per behavior/failure, no app-version back-compat
> tests. Sub-reader tests extend the owning suite; do not create a duplicate fixture per store.

## Objective

Read each launcher's own installed-game inventory (read-only), map it into normal `GameEntry`
records, cross-check the disk, and persist through the existing game database. This is the core of
the "populate the inventory like a normal file scanner" proposal.

## What Needs to Change

### 1. Models
- [ ] `StoreInventoryEntry`: `StoreId`, `Name`, `InstallPath`, `LaunchExe?`, `StoreState` (store-reported), `FolderExists`, `ExeExists`, `Metadata` (typed per store).
- [ ] `IStoreInventoryReader`: `GameSourceKind Kind`, `StoreManifestFingerprint? Fingerprint`, `IReadOnlyList<StoreInventoryEntry> Read()`.

### 2. Import service
- [ ] `StoreInventoryImportService.Import(kind)` is gated by `IStorePolicyProvider.CanImport(kind)`: a store
      the user did not enable is never read. No second enable flag.
- [ ] `StoreInventoryImportService.Import(kind)`: read the manifest once → map to `GameEntry` → check only the folder/exe states the record needs (no enumeration, no recursion) → set `DetectionOrigin.StoreManifest` → persist through the shared JSON store (`planning/132`). If that store has not landed yet, the import does not ship: no direct `File.WriteAllText`, no `JsonFileHelper` fallback.
- [ ] Imported `LaunchExe` must resolve to an existing file under the reported install root; otherwise store `null` and mark `ExeMissing`.
- [ ] Do **not** call `FolderScanner` in this path. Enrichment is a separate opt-in step for new games only.
- [ ] Use `DetectionTierDeriver` for confidence; do not set `IdentificationTier` by hand. It already derives
      Steam/Epic-global as `Locked`, Epic-local and GOG as `Secure`, and heuristic discoveries as
      `Candidate`. A store record identifies the game; `Installed` still requires the folder on disk.
- [ ] Keep import **idempotent**: re-running must not duplicate entries; match by `StoreKey`, then by normalized install path (`PathText`). A name-only match is allowed only inside the same store and only when exactly one existing entry has that name.
- [ ] Expose the same import entry point to first-run (T117B2) and startup sync (T121).
- [ ] Build the `GameStoreIndex` in the import path (it is currently built only in tests) so duplicate checks are O(1), not a scan.
- [ ] Never write to store files.

**What can actually be imported today:** only Steam (ACF + `common`) and Epic (ProgramData `*.item`) have a
usable launcher inventory. GOG and Rockstar have registry-based inventory; Ubisoft has a registry list
(`Installs\{gameId}\InstallDir`) marked `NeedsVerification`; EA and Battle.net are detection-only; Xbox has
none. The import service must accept a reader that returns an empty or partial list without inventing
entries, and must not infer "removed" from a store that has no inventory.

### 3. Identity
- [ ] Add/confirm stable IDs per store (`StoreInfo`/`GameIdentity`): GOG, Rockstar title ID. EA content ID, Ubisoft game ID, and Battle.net product code only if T136 confirms their sources. Xbox has no stable id in this version.
- [ ] Add contract/pin tests for any persisted ID.

### 4. Subtasks
- [ ] T118A Steam + GOG.
- [ ] T118B Epic.
- [ ] T118B2 EA (blocked by T136; detection-only unless a source is confirmed).
- [ ] T118C Rockstar (registry + `title.rgl`; no `Settings.json` yet).
- [ ] T118C2 Ubisoft (blocked by T136).
- [ ] T118C3 Battle.net (blocked by T136; no SQLite reader by default).
- [ ] Xbox has **no inventory reader** in this version: marker-only detection, no package API, no package
      family id.
- [ ] Arc: no reader until T136 decides whether it is a distinct store.

## Context

- Existing readers to reuse: `SteamLibraryScanner`, `SteamAcfParser`, `EpicLibraryScanner`, `EpicItemCatalog`.
- Matrix: `docs/detection/10-store-capabilities.md`; proposal: `planning/129`.
- Disk wins over the launcher record for `Installed`.

## Requirements

- [ ] Each reader is read-only and returns records even when the folder is missing.
- [ ] Each imported entry has the correct source, install path, and state.
- [ ] Import performs no recursive enumeration. Existence checks are used only where existence is the
      answer (`FolderExists`, `ExeExists`) or where the path must be validated; never as a probe before a
      read (`planning/135` read rule).
- [ ] Import uses `GameStoreIndex` for O(1) duplicate checks.
- [ ] Enrichment is the existing pipeline, not a parallel one.
- [ ] No store-file writes, no directory creation, and no network access.
- [ ] Imported paths are validated before persistence; a record cannot point outside its reported install root.
- [ ] Persistence uses the shared JSON store: copy to `.bak` → write → read back and parse (`planning/132`).
      No direct `File.WriteAllText`, no temp file, no rename.

## Verification

- [ ] Fixture per store; missing-folder case yields `Missing`.
- [ ] Round-trip: import → `games.json` → reload preserves source/id/state.
- [ ] `dotnet build` / `dotnet test` green; detection matrix unaffected.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
