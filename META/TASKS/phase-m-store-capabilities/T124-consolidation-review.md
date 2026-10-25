# Task T124: Duplicate-Capability Review and Consolidation Plan

**Tier:** 1 — Documentation / Architecture
**Phase:** M — Store Capabilities
**Risk:** Low (analysis only)
**Status:** pending
**Prerequisites:** Task T124 depends on Task T120 and Task T123

> Documentation-only. Do not run build/test. Do not implement the remedy here.

## Objective

Produce a review and a prioritized **plan** for consolidating duplicated detection, store, path,
identity, persistence, and gating helpers into generic methods that every gate reuses — before
T117/T118/T121 add more. The deliverable is a plan, not code.

## Scope

**In:** existing C# helpers plus the already-designed/planned store pipeline (T117/T118/T121/T122/T123).
**Out:** behavior changes, Python/C# parity, UI polish, new store support, decrypting store formats.

## Known candidates (start here; verify with `grep`, do not rediscover from zero)

### Detection (blueprint §11)
- **O1/O2** residual noise exe/dir duplication (`DetectionNoise` is only a partial consolidation; `FolderScanner`, `ContainerScanner`, `ExecutableDiscovery`, `SteamLibraryScanner`, `EpicLaunchResolver`, `EpicItemWriter` still own lists).
- **O3** Epic `.egstore`/store search in multiple places (`StoreSignalDetector`, `EpicLibraryScanner`, `EpicOrphanDiscovery`, `EpicStoreSearch`).
- **O4/O5/O6** Ubisoft/Rockstar/Blizzard signal checks repeated across `StoreSignalDetector`, `FallbackSignalDetector`, `ContainerScanner`.
- **O7** UE platform/layout tokens (`FileSystemHelper.UePlatformNames` fixed one; check remaining copies).
- **O8** path normalizers: `SteamAcfParser.NormalizePath`, `RegistryFallbackDetector.NormalizePath`, `GameStoreIndex.NormalizePath`, `SteamLibraryMembership.Normalize`, `LibraryManager.IsChildOf`/`NormalizeLibraryRoot`.
- **O10** cheap-exe finders (`SteamLibraryScanner`, `EpicItemWriter`, `EpicLaunchResolver`, `ExecutableDiscovery`).
- **O9** is done (`EditionTokens`) — record it as a successful pattern, not a candidate.

### Store pipeline (designed, not all built)
- Planned `ILauncherLocator` vs existing `SteamInstallPathLocator` and `RegistryFallbackDetector` registry probing.
- Planned `IStoreInventoryReader` vs existing `SteamLibraryScanner`/`EpicLibraryScanner` (avoid a second inventory path).
- Planned `StoreSyncService` vs `LibraryManager.RescanLibrary`/`SelectScannerAndScan`.
- Planned `StoreManifestFingerprint`/stat checks vs `FileSystemHelper.GetLastWriteTimeSafe`.
- `StoreKey`/`GameStoreIndex` (built) vs `IdentityKey`/`GameIdentity`/`GameSourceParser` key derivation.
- `IStorePolicyProvider`/`CanImport`/`CanSync` (built) vs future direct policy checks in T117/T118/T121/T122.

### Persistence and config
- Private DTO + mapping patterns in `JsonConfigService`, `GamesDatabaseService`, `LibrariesDatabaseService`, `CollectionsDatabaseService`.
- Schema-version guards repeated per service.
- Enum-name vs enum-number JSON conventions (the new policy DTO uses names).

### Identity and platform mapping
- `StoreInfo` IDs vs `IdentityKey`/`GameIdentity` keys; registry name matching; ACF/Epic ID extraction.
- `GameSourceKind` ↔ store-key prefix ↔ display label mapping.

## What Needs to Change

### 1. Inventory (evidence required)
- [ ] For each candidate: `file:line`, what it does, current callers, and whether it is a true duplicate.
- [ ] Include the planned T117/T118/T121 interfaces and where they would duplicate existing code.

### 2. Classify each candidate
- [ ] **True duplicate** → consolidate into one generic method.
- [ ] **Deliberate divergence** → document why (e.g. display substring vs tail edition; registry fallback vs store inventory are different sources).
- [ ] **Future risk** → add a contract/reuse rule before the task is built.

### 3. Produce the plan
- [ ] Propose names/signatures for the generic contracts, for example:
  - `PathText.NormalizeForCompare` / `IsUnder`
  - `StoreIdentity.Resolve` (GameSourceKind + store ID → StoreKey)
  - `IStoreInventorySource` (locator + reader + fingerprint)
  - `StoreGate.CanImport`/`CanSync`
  - `JsonDocumentStore<T>` (private DTO + schema guard)
  - `ManifestStat`/`Fingerprint`
- [ ] For each: migration order, risk, tests, and which upcoming work must consume it.
- [ ] Explicit "do not consolidate" list with reasons.
- [ ] A short **reuse checklist** for upcoming store tasks: search for the generic helper first; new readers implement the shared interface; no new path normalizer/store-key parser.

### 4. Deliverable
- [ ] Create `planning/130-duplicate-feature-consolidation.md` with the review + plan.
- [ ] Link it from `docs/detection/09-blueprint.md` §11 (overlap backlog).
- [ ] Do not put task IDs in the planning document; describe work areas.

### 5. Connection map (subtask T124A)
- [ ] Generate/refresh the how-it-connects map (see T124A): composition root, data flows, scan/store/launch pipelines, extension recipes.
- [ ] The consolidation plan links to that map instead of restating wiring.

## Context

- Existing overlap inventory: `docs/detection/09-blueprint.md` §11 (O1–O10).
- Store design: `planning/129-store-capabilities-and-launcher-discovery.md`.
- Built foundations: `DetectionNoise`, `EditionTokens`, `GameStoreIndex`, `IStorePolicyProvider`.
- This task should complete **before T117/T118** so their interfaces adopt the generic contracts.

## Requirements

- [ ] Every candidate has `file:line` evidence.
- [ ] Every candidate is classified (duplicate / deliberate / future risk).
- [ ] The plan names concrete signatures and the consumer task area (no task IDs in the plan doc).
- [ ] No behavior changes, no code, no build/test.

## Verification

- [ ] `test -f planning/130-duplicate-feature-consolidation.md`.
- [ ] Each candidate in the doc cites a file/line or a planning doc.
- [ ] A "do not consolidate" section exists.
- [ ] The reuse checklist is present.
- [ ] No build/test (docs-only).

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
