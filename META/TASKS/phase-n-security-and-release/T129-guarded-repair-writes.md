# Task T129: Guarded Repair Writes

**Tier:** 3 — Logic/Behavior
**Phase:** N — Security and Release Integrity
**Risk:** High
**Status:** pending
**Prerequisites:** Task T129 depends on Task T124

## Objective

Execute the consolidation review's classification of the two repair writers. Either both become one
guarded contract that ADR-005 already requires, or they are removed. No write may remain unguarded.

## What Needs to Change

### 1. Follow the classification
- [ ] Read the repair-writer section of `planning/130-duplicate-feature-consolidation.md`.
- [ ] Implement the chosen outcome for `SteamAcfWriter` and `EpicItemWriter`.
- [ ] If the outcome is removal, delete the writer, its UI entry point in `MainWindow.PlatformRepair.cs`,
      and its tests. Do not leave a hidden path.

### 2. Guarded contract (if retained)
- [ ] Validate the target before writing:
      Steam root must contain `steamapps/libraryfolders.vdf`; Epic target must be the ProgramData
      `EpicGamesLauncher\Data\Manifests` folder. Refuse anything else.
- [ ] Refuse relative paths, UNC paths, and paths the application did not discover itself.
- [ ] Write the original file to `data/backups/<timestamp>/` before replacement, and record the operation
      in `data/migration_log.jsonl` (target, backup path, bytes, outcome).
- [ ] Implement a dry run that returns the planned target, backup location, and diff without writing.
- [ ] Replace the target atomically (temp file in the same directory, then move/replace).
- [ ] Provide a restore path from the backup. If restore fails, report the backup location.
- [ ] Never create a store directory that did not already exist.

### 3. UI
- [ ] Show the dry-run preview before the user confirms.
- [ ] Report success, refusal, and failure with the journal/backup location.
- [ ] Do not offer repair when target validation fails.

## Context

- `src/GamingCommander.App/Services/SteamAcfWriter.cs` writes `appmanifest_*.acf` into a Steam library.
- `src/GamingCommander.App/Services/EpicItemWriter.cs` writes `.item` files into ProgramData.
- `src/GamingCommander.App/MainWindow.PlatformRepair.cs` is the only UI entry point.
- `META/ADR/005-migration-safety.md` requires backup, logging, dry-run, and reversibility.
- Epic `.item` re-creation is a known dead end for registration; that does not make the writer safe.

## Requirements

- [ ] No repair write happens without a validated target and a backup.
- [ ] Every attempt is journalled, including refusals.
- [ ] A dry run performs no writes and no directory creation.
- [ ] A failed write leaves the original file unchanged or restores it from backup.
- [ ] Removal outcome leaves no compiled writer and no dead UI action.

## Verification

- [ ] Temp-folder tests: valid Steam root, valid Epic manifest folder, wrong folder, relative path,
      UNC path, read-only target, and interrupted write.
- [ ] Dry run leaves the file system unchanged (snapshot before/after).
- [ ] Restore test returns the original bytes.
- [ ] `dotnet build` / `dotnet test` green.
- [ ] Manual check that the UI cannot reach an unguarded writer.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
