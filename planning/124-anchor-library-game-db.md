# Plan 124 — Anchor-based Library + Game Database (two-file split)

**Status:** Active — planned 2026-09-08.
**Type:** Data-model refactor.
**Informed by:** `docs/DATA-FORMAT.md` (rewritten 2026-09-08 to the anchor target).

## Problem

The application database is **path-anchored**. `games.json` is one file whose
`Roots[]` array is keyed by physical `RootPath`, each root holding its own
`Games[]`. The intended VFS model is **anchor-based**:

- A **Library (Anchor)** is a unique, named catalog (e.g. `Steam`, `GOG`,
  `EPIC`, `d:\games`) with a **type** and **multiple physical folders**.
- A **Game** is linked to an **anchor by name**, not to a single physical folder.
- Several physical Steam folders collapse into ONE `Steam` anchor; a game found
  in `d:\games` that is Epic can be re-assigned to the `EPIC` anchor.

The current model cannot represent this: multiple physical Steam roots render as
multiple "Steam" rows, and a game can never belong to a catalog other than the
physical folder that contained it.

## Required change

Split the single mixed `games.json` into **two files** and make linkage by anchor:

1. **`libraries.json`** — the Library/Anchor database. Map of anchor name →
   `{ type, folders[] }`.
2. **`games.json`** — the Game database. Flat `games[]`, each entry linked to an
   anchor by `Library` (name). Each game also carries its physical `FolderPath`.

**No data migration.** Work-in-progress: existing on-disk
`settings.json`/`games.json` may be discarded. `games_metadata.json` (sidecar) is
unchanged (already keyed by game `Id`).

## Files affected

### Models (Core)
- `Models/LibraryRoot.cs` → replace with `Library` (anchor) model:
  `Name`, `GameSourceKind Type`, `IReadOnlyList<string> Folders`.
- `Models/GameRoot.cs` → remove (games are flat now).
- `Models/GamesDatabase.cs` → replace with flat `GamesDatabase` = `List<GameEntry>`.
- `Models/GameEntry.cs` → add `Library` (anchor name) and `FolderPath`; adjust
  `ComputeId` to `MD5("{Library}|{folderPath}")`.
- `Models/AppConfig.cs` → replace `LibraryRoots`/`FolderOverrides` with the
  anchor list (or move anchors fully into `libraries.json` and keep
  `AppConfig` for non-library settings only).
- `Core/ILibraryManager.cs`, `Core/IGamesDatabaseService.cs` → signatures keyed
  by anchor name (`libraryName`), not `rootPath`.

### Implementation (App)
- `Services/GamesDatabaseService.cs` → flat `games[]`; CRUD keyed by anchor.
- `Services/LibraryManager.cs` → manage anchors; `SelectScannerAndScan` still
  scans per physical folder but stores games under the anchor.
- `Services/LibrariesDatabaseService.cs` (new) → read/write `libraries.json`;
  replace the anchor role currently in `JsonConfigService`.
- `ViewModels/LibrarySetupViewModel.cs`, `ShellViewModel.cs` → iterate anchors
  (one VFS row per anchor), group games by anchor, enable re-anchoring.
- `App.axaml.cs`, `MainWindow.axaml.cs` → wire `libraries.json` + flat
  `games.json`; update path plumbing.

### Scanner changes
- `SteamLibraryScanner` — its `Scan(libraryRoot)` returns per-folder games;
  update so all discovered folders store games under the single `Steam` anchor
  while each `GameEntry.FolderPath` records its real physical folder.
- `LibraryManager.SelectScannerAndScan` / `AddSteamLibrariesAsync` — add the
  `Steam` anchor whose `folders` = all vdf-discovered libraries; pass across all
  physical folders at once.

### Documentation & tests
- `docs/DATA-FORMAT.md` — already rewritten to the anchor target (this plan's
  contract).
- Tests updated for the flat `games.json` / `libraries.json` schema; obsolete
  path-anchored `ScanAll`-style tests removed.

## Success criteria

- App builds and `dotnet test` passes.
- The VFS left pane shows **one row per anchor** (`Steam`, `GOG`, `d:\games`, …).
- Adding Steam produces **one `Steam` anchor** whose `folders` hold all vdf
  libraries; games from all Steam libraries surface under `Steam`.
- `libraries.json` and `games.json` match the `docs/DATA-FORMAT.md` schema.
- Existing on-disk data is not back-compat-guaranteed (no migration).

## Out of scope
- Data migration of old `settings.json`/`games.json`.
- `games_metadata.json` format changes.
- Online metadata / launch-command changes.
