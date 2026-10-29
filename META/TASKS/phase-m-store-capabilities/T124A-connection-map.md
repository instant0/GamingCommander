# Task T124A: Generate/Refresh the Connection Map

**Tier:** 1 — Documentation / Architecture
**Phase:** M — Store Capabilities
**Risk:** Minimal
**Status:** pending
**Prerequisites:** Task T124A depends on Task T124

> Documentation-only. Do not run build/test. Do not implement features.

## Objective

Produce a quick-lookup **connection map** for agents adding features: the composition root, data
flows, scan/store/launch pipelines, **the per-store decision tree from intent to code path**, and the
extension recipes that say where a new feature plugs in.

Per-store code is expected: Steam and Epic do not share a locator, a manifest format, a launch rule, or a
write target. What must not be duplicated is the *rule* — validation, no-create, staging, journal. The map
exists to show where the paths diverge, which gate selects one, and where each can fail.

`META/CODE_MAP.md` already exists but is a type/file catalog. This task decides whether to extend it
or add a companion, and fills the missing "how it connects" layer without duplicating the catalog.

## What Needs to Change

### 1. Decide the shape
- [ ] Review `META/CODE_MAP.md` (catalog) and `META/ARCHITECTURE.md` (decisions).
- [ ] If `CODE_MAP.md` can absorb a "How it connects" section while staying ≤ ~500 lines, extend it.
- [ ] Otherwise create `docs/architecture/CONNECTION_MAP.md` and link it from `META/CODE_MAP.md` and `docs/README.md`.
- [ ] Do not duplicate the type/file tables; link to `CODE_MAP.md` instead.

### 2. Required content
- [ ] **Composition root:** `App.axaml.cs` service construction/lifetimes; windows and view-models.
- [ ] **Persistence + data flow:** `settings.json`/`libraries.json`/`games.json`/`collections.json`/metadata sidecar → services → in-memory caches/index (`GameStoreIndex`, `IConfigService`).
- [ ] **Scan pipeline:** `LibraryManager` → `SteamLibraryScanner`/`EpicLibraryScanner`/`FolderScanner` → store signals/containers/exe discovery → `GameEntry` → `IGamesDatabaseService` → UI.
- [ ] **Store inventory pipeline (current + planned):** policy (`IStorePolicyProvider`) → launcher locator → inventory reader → import/sync → health → UI. Mark planned parts.
- [ ] **Launch pipeline:** UI → `GameLaunchResolver` → `steam://` or exe.
- [ ] **Metadata/online pipeline:** `MetadataService`/queue/gate → sidecar.
- [ ] **Trust boundaries and write paths:** what is read, what is written, what leaves the machine. Mark
      the two user-started repair writes, the app-owned `data/` writes, and the opt-in HTTP egress.
      External targets are checked for existence, never created; only `data/` subdirectories are created.
- [ ] **Extension recipes:** add a store, add a store signal, add a metadata provider, add a persisted field, add a UI action, add a detection gate.
- [ ] Each flow names the real class/file; planned arrows are labelled "planned".

### 3. Per-store decision tree (the part that answers "which path runs, and how do we know it is right")
- [ ] One table per store (Steam, Epic, GOG, EA, Ubisoft, Battle.net, Xbox, Rockstar, Standalone) with the
      same rows, so the differences are visible side by side:
      `intent → gate → locator → reader → writer → failure points → test`.
- [ ] Intents to cover: **scan** (folder walk), **inventory** (manifest read), **import**, **launch**,
      **repair** (create/modify), **health/status**.
- [ ] For each cell, the real class/file, or `—` when the store has no such path.
- [ ] Mark the gate for each row: `GameSourceKind`, `IStorePolicyProvider.CanImport`/`CanSync`, the user
      action, or the marker check. There must be one gate per decision, not two competing tests.
- [ ] **Failure points:** for each store, the conditions that make the row refuse or degrade (missing
      marker, absent manifest, corrupt manifest, unreadable directory, unknown format, no store id).
      These are the cases that must be tested.
- [ ] **Test column:** name the existing test that covers the row; where none exists, list it as a gap.
      This is the accuracy check for the map, not a request to write tests here.
- [ ] Record the two write rules as rows: Steam create-only (target absent), Epic modify-only (target must
      exist and parse; unknown members preserved). A store with neither is `—`.

### 4. Accuracy and style
- [ ] Verify every arrow/class against `src/` (cite class names, not line numbers that rot).
- [ ] Keep it a map, not prose; prefer short diagrams/tables.
- [ ] No task IDs in the document.
- [ ] No behavior changes.

## Context

- `META/CODE_MAP.md` is ~350 lines of type/file rows; it has no composition/data-flow section.
- `META/ARCHITECTURE.md` records decisions, not wiring.
- Store design: `planning/129-store-capabilities-and-launcher-discovery.md`.
- This map is the quick lookup referenced by T124's consolidation plan.

## Requirements

- [ ] A single entry point ("start here") for feature work.
- [ ] Composition root + the six pipelines above are covered.
- [ ] Every store has a decision-tree row per intent, with gate, failure points, and a test reference or a
      recorded gap.
- [ ] Read, write, and network boundaries are visible on the map.
- [ ] Extension recipes are actionable and point to the real files/classes.
- [ ] Planned vs shipped is explicit.
- [ ] `CODE_MAP.md` links to it (or contains it); no duplicate catalog.

## Verification

- [ ] The chosen file exists and is linked from `META/CODE_MAP.md`.
- [ ] Every pipeline names existing classes (or marks planned).
- [ ] Every store appears in the decision tree; every write rule names its precondition.
- [ ] Each failure point maps to a test name or a recorded gap.
- [ ] No task IDs in the document.
- [ ] No build/test (docs-only).

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
