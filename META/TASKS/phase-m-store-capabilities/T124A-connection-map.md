# Task T124A: Generate/Refresh the Connection Map

**Tier:** 1 — Documentation / Architecture
**Phase:** M — Store Capabilities
**Risk:** Minimal
**Status:** pending
**Prerequisites:** Task T124A depends on Task T124

> Documentation-only. Do not run build/test. Do not implement features.

## Objective

Produce a quick-lookup **connection map** for agents adding features: the composition root, data
flows, scan/store/launch pipelines, and the extension recipes that say where a new feature plugs in.

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
- [ ] **Extension recipes:** add a store, add a store signal, add a metadata provider, add a persisted field, add a UI action, add a detection gate.
- [ ] Each flow names the real class/file; planned arrows are labelled "planned".

### 3. Accuracy and style
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
- [ ] Read, write, and network boundaries are visible on the map.
- [ ] Extension recipes are actionable and point to the real files/classes.
- [ ] Planned vs shipped is explicit.
- [ ] `CODE_MAP.md` links to it (or contains it); no duplicate catalog.

## Verification

- [ ] The chosen file exists and is linked from `META/CODE_MAP.md`.
- [ ] Every pipeline names existing classes (or marks planned).
- [ ] No task IDs in the document.
- [ ] No build/test (docs-only).

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
