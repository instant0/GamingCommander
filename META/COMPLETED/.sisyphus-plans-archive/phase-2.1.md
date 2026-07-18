# Phase 2.1: SyncMove — Manifest-Aware Game Migration

## Goal

Implement safe, manifest-aware game relocation for Steam and Epic games. This is the only move operation in GamingCommander — it is not a general-purpose file manager.

## Background

Each launcher stores game metadata in a store-specific manifest file:
- **Steam**: `.acf` files in `steamapps/` (and `.vdf` library cache). The `installdir` field in the ACF is relative to `steamapps/common/`.
- **Epic**: `.item` JSON files in `C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests`. The game folder may contain a `.egsstore/manifests/*.json` copy, but the canonical source of truth is the `EpicGamesLauncher` path.
- **GOG**: `goggame-*.info` files in the game folder.
- **EA App**: registry entries (handled separately).
- **Ubisoft Connect**: `cache\\*.json` in the Ubisoft data folder.

The `manifestPath` field in `data/games.json` stores the **canonical manifest path** per game. Combined with the `gameSource` tag, GamingCommander knows:
- This is an Epic game → the manifest is at `manifestPath` → update `InstallLocation` in the `.item` file
- This is a Steam game → the manifest is at `manifestPath` → update `installdir` in the `.acf` file
- The `manifestPath` + `gameSource` together drive all manifest-aware migration logic.

Simply moving the game folder breaks the launcher. SyncMove relocates **both** the game files **and** the associated manifest in a coordinated, reversible operation.

## UX Model

### F6 — SyncMove

1. User selects a game in the left pane.
2. User presses F6.
3. A dialog appears:
   - **Source:** shown (read-only), e.g. `D:\SteamLibrary\steamapps\common\The Witcher 3`
   - **Destination:** folder picker or manual path input.
   - **Mode:**
     - `Move + Symlink` — move files, create a directory junction at the original location pointing to the new location. Launcher manifest is updated to new path. **Recommended.**
     - `Move Only` — move files, update manifest. No symlink. Game will not appear in original library until moved back.
     - `Dry Run` — preview what would happen without making any changes.
4. User confirms or cancels.
5. Progress is shown in the status line.
6. On completion, status line shows success or error.

### Preflight Checks

Before any mutation, SyncMove validates:
- Destination has enough disk space.
- Destination directory does not already exist.
- Source directory is accessible.
- Manifest file is readable and backup-able.

### Backup Strategy

- Before touching any manifest, copy it to `data/backups/<gameid>_manifest_backup.acf`.
- Log the operation to `data/migration_log.jsonl`.

### Reversibility

- Move + Symlink: Remove the symlink and update the manifest back to original path to "undo".
- Move Only: Move files back to original location and update manifest.
- All reversals are manual (documented in status line output).

## Manifest Update Rules

### Steam (ACF)

- File: `<SteamLibrary>/steamapps/appmanifest_<AppID>.acf`
- Field to update: `installdir` to the new folder name (relative to `steamapps/common/`).
- Example: `"installdir" "The Witcher 3"` → `"installdir" "E:\Games\The Witcher 3"` (adjust base path).
- **Note:** Steam manifest paths are relative to `steamapps/common/`. The ACF stores the folder name, not the full path. Moving across library roots requires updating the ACF `installdir` AND moving the game folder to the new library's `steamapps/common/` folder.

### Epic Games Store (JSON)

- File: `%ProgramData%\Epic\EpicGamesLauncher\Data\Manifests\*.item`
- Field to update: `InstallLocation` to the new full path.
- After updating the manifest, restart Epic Games Store or trigger a metadata refresh.

## Constraints

- **No raw file delete.** Files are moved, not deleted. The source becomes a symlink or is removed only after the destination is fully validated.
- **No cross-filesystem raw copy** without explicit user confirmation (disk space implications).
- **No manifest modification without a backup copy first.**
- Operations are resumable: if interrupted, re-running should detect and skip already-moved files.

## Scope

- **In scope:** Steam ACF-based games, Epic JSON manifest games.
- **Out of scope for Phase 2.1:** GOG, EA App, Ubisoft Connect (these come in Phase 3).
- **Out of scope:** Standalone games (no manifest to update — just move the folder).

## Tasks

### 1. SyncMove Dialog

- [ ] F6 opens SyncMove dialog with source path shown.
- [ ] Destination folder picker.
- [ ] Mode selector: Move+Symlink, Move Only, Dry Run.
- [ ] Preflight validation on confirm.
- [ ] Progress/status feedback.

### 2. Steam Manifest Update

- [ ] Read `appmanifest_<AppID>.acf`.
- [ ] Parse and update `installdir` field.
- [ ] Write updated manifest (with backup first).
- [ ] Handle cross-library-root moves (update both folder location and ACF).

### 3. Epic Manifest Update

- [ ] Read `.item` JSON manifest.
- [ ] Update `InstallLocation` field.
- [ ] Write updated manifest (with backup first).

### 4. Symlink / Junction Creation

- [ ] After moving files, create a directory junction at the original location pointing to the new location.
- [ ] Junction allows the launcher to find the game without manual manifest repair.

### 5. Backup & Logging

- [ ] Backup manifest to `data/backups/` before modification.
- [ ] Append operation record to `data/migration_log.jsonl`.

### 6. Dry Run Mode

- [ ] Simulate the entire operation without touching any files.
- [ ] Show what would be moved, what manifest would be updated, what symlink would be created.

### 7. Reversal Documentation

- [ ] After any move operation, show reversal instructions in the status line.

---

## Deliverables

- [ ] F6 SyncMove dialog with dry run support.
- [ ] Steam ACF manifest backup and update.
- [ ] Epic JSON manifest backup and update.
- [ ] Directory junction creation at original location.
- [ ] Migration log (`data/migration_log.jsonl`).
- [ ] Backup manifests in `data/backups/`.
- [ ] App builds and runs cleanly on Windows.

---

## Exit Criteria

Phase 2.1 is complete when:
- A Steam game can be moved to a new location with manifest updated and junction created.
- An Epic game can be moved with manifest updated.
- Dry run shows accurate preview without modifying any files.
- All operations are logged and backed up.
- Reversal steps are shown after each operation.
- The app builds and runs cleanly on Windows.
