# Task T124A: Generate/Refresh the Connection Map

**Tier:** 1 — Documentation / Architecture
**Phase:** M — Store Capabilities
**Risk:** Minimal
**Status:** ready (Proposal B adopted 2026-10-30)
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

### What this is for

- **Prevent the next duplicated rule.** Five path normalisers, nine noise-exe lists, four Epic
  `.egstore` checks: each was a small local decision nobody could see the whole of. The map is the place
  that makes the existing rule findable before a seventh is written.
- **Make failure modes testable.** For each store and each intent, it names the condition that refuses or
  degrades (missing marker, corrupt manifest, absent folder, different install id) and the test that
  covers it — or records the gap.
- **Answer "where do I add X?"** in one page: a store, a locator, an inventory reader, a UI action, a
  persisted field.
- **Show the trust boundaries once:** what is read, what is written, what leaves the machine.

### What this is not

- Not an optimisation; it has no runtime effect.
- Not a refactor; it changes no code and creates no interface.
- Not a second catalog; the type/file tables stay in `CODE_MAP.md`.
- Not a test plan; it points at existing tests and lists gaps, it does not write tests.

## Evaluation and two proposals (2026-10-30)

### What it buys

The decision tree is the only artifact that answers "which path runs for this intent, where does it
refuse, and which test proves it". Without it, T117/T118/T121 each pick the nearest existing helper —
the mechanism that produced five path normalisers, nine noise-exe lists, and four Epic `.egstore`
checks. The map is also the call-site index the I/O audit needs.

### What already exists

| Artifact | What it is | What it is not |
|---|---|---|
| `META/CODE_MAP.md` (~350 lines) | type/file catalog | no flows, no gates, no failure points |
| `META/ARCHITECTURE.md` | decisions | wiring, not invocations |
| `planning/129` | store design | current code, no test mapping |
| `docs/detection/10-store-capabilities.md` | per-store matrix | no code paths, no failure points |

The missing layer is: *intent → gate → locator → reader → writer → failure → test*.

### Drift risk

Class names change; line numbers rot. The map cites classes and test names only, never line numbers, and
the decision-tree rows are checked against `src/` at write time. A row whose test column is empty is a
recorded gap, not a promise.

### Proposal A — extend `META/CODE_MAP.md`

Add a "How it connects" section and the decision-tree tables to the catalog file; cap the file at ~500
lines by linking to `docs/detection/10` instead of repeating the store matrix.

- **Pros:** one file, already in the mandatory reading order; no second document to keep in sync; agents
  cannot miss it.
- **Cons:** mixes "what types exist" with "how work flows"; the decision tree is ~9 stores × 6 intents
  plus six pipelines and will dominate the file; every structural edit touches the same file, so catalog
  churn and flow churn collide; harder to review a flow change on its own.

### Proposal B — companion `META/CONNECTION_MAP.md`

Keep `CODE_MAP.md` as the catalog. Put a three-line "Start here → how it connects" link at the top of
`CODE_MAP.md` and add the map to the `docs/README.md` live table.

**Why `META/`, not `docs/architecture/`:** the audience is the builder/agent, the same audience as
`CODE_MAP.md` and `ARCHITECTURE.md`; putting it beside them avoids a new directory, and `docs/README.md`
already lists the `META/` documents.

- **Pros:** separation of concerns (catalog vs wiring); no size cap on the map; flows can be short
  diagrams; a flow change is a reviewable diff in its own file; `CODE_MAP.md` stays a fast lookup;
  sibling of the catalog, so the link is one line and the directory is already in the reading path.
- **Cons:** a second file to maintain; two documents can disagree if only one is updated.

### Comparison

| Criterion | A — extend `CODE_MAP.md` | B — companion in `META/` |
|---|---|---|
| Discovery by an agent | High — already in the reading order | High — sibling of `CODE_MAP.md`, linked in its first lines |
| Separation of catalog and flows | Low | High |
| Readability at the required size (9 stores × 6 intents + 6 pipelines) | Low — the catalog stops being a lookup | High |
| Diff/review quality | Low — every flow edit touches the catalog | High — one file per concern |
| Maintenance after refactors | Medium — catalog and flows age differently but share a file | Medium — two files, but each has one owner concern |
| Cost to create | Slightly lower (no link) | Slightly higher (one link, one live-table row) |

### Recommendation

**Proposal B.** The decision tree is the part that will change most often, and it is not a catalog. B's
only real advantage for A — discovery — is covered by the link in the first lines of `CODE_MAP.md`, which
is already mandatory reading, and by the `docs/README.md` live table.

**Adopted (human decision, 2026-10-30): Proposal B, at `META/CONNECTION_MAP.md`.**

## What Needs to Change

### 1. Decide the shape
- [x] Evaluation written above (2026-10-30): Proposal A (extend `CODE_MAP.md`) vs Proposal B (companion).
      **Adopted: B, `META/CONNECTION_MAP.md`.**
- [ ] Create `META/CONNECTION_MAP.md`; put the link in the first three lines of `META/CODE_MAP.md`; add it
      to the `docs/README.md` live table.
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

Row shape — this is what the document must contain, for every store and intent:

| Store | Intent | Gate | Path | Failure points | Test |
|---|---|---|---|---|---|
| Steam | inventory | `IStorePolicyProvider.CanImport` | `SteamInstallPathLocator` → `SteamAcfParser` / `DiscoverLibraryPaths` | vdf missing, ACF unreadable, root gone | `SteamLibraryScannerTests`, `SteamBootstrapTests` |
| Steam | repair | user action (Write ACF) | `SteamAcfWriter.TryWrite` | `steamapps` missing, no `libraryfolders.vdf`, target exists | `SteamAcfWriterTests` |
| Epic | inventory | `CanImport` | `EpicManifestPaths` → `EpicItemCatalog` | manifests dir missing, `.item` unparseable, no catalog ids | `EpicManifestParserTests`, `EpicLibraryScannerTests` |
| Epic | repair | user action (Repair) | `EpicItemWriter.TryWrite` (modify) | item absent, unparseable, different install id, dir missing | `EpicItemWriterTests` |

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

- [ ] `META/CONNECTION_MAP.md` exists and is linked from the first three lines of `META/CODE_MAP.md` and
      the `docs/README.md` live table.
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
