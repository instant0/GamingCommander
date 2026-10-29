# Task T129: Guarded Repair Writes

**Tier:** 3 — Logic/Behavior
**Phase:** N — Security and Release Integrity
**Risk:** High
**Status:** pending
**Prerequisites:** Task T129 depends on Task T124C

## Objective

Execute the repair-write review. Either both writers become one guarded contract, or the reviewed
writers are removed. No repair write may create a directory outside the application folder.

## What Needs to Change

### 1. Follow the review
- [ ] Read `planning/133-repair-write-safety.md`.
- [ ] Implement the chosen outcome for `SteamAcfWriter` and `EpicItemWriter`.
- [ ] If the outcome is removal, delete the writer, its UI entry point in `MainWindow.PlatformRepair.cs`,
      and its tests. Do not leave a hidden path.

### 2. Preflight — no directory creation
- [ ] `Directory.Exists(targetDir)`; if false, refuse with a message naming the expected location.
- [ ] `File.Exists(targetFile)`; if true, refuse, except the reviewed hollow-`.item` replacement case.
- [ ] Validate the target marker:
      Steam — `targetDir\libraryfolders.vdf` exists.
      Epic — `targetDir` is the known ProgramData `EpicGamesLauncher\Data\Manifests` folder.
- [ ] Refuse relative paths, UNC paths, and paths the application did not discover itself.
- [ ] **Never call `Directory.CreateDirectory` on any path outside the application data root.**
      `CreateDirectory` is allowed only under `data/`, `data/backups/`, and `data/staging/`.

### 3. Generate, validate, copy
- [ ] Build the payload in memory (ACF/`.item` are a few KB).
- [ ] Serialize, re-read, and re-parse it; check required fields and byte length.
- [ ] Write it to `data/staging/<name>` and read that file back, so the exact bytes to be copied are
      proven to parse.
- [ ] `File.Copy(staged, target)` — one call, from a validated source.
- [ ] Compare the target length with the staged length. On mismatch, delete the target and report.
- [ ] On `IOException`, delete a zero-length or oversized target if one exists, then report.
- [ ] Delete the staged file after success; journal the outcome either way.
- [ ] Do not add a hash, digest, or signature. Length plus a successful re-parse is the validation.
- [ ] Nothing else is written to the target directory: no temp file, no rename step, no second pass.

### 4. Backup, journal, dry run, restore
- [ ] Back up a replaced hollow `.item` to `data/backups/<timestamp>/` before the copy.
- [ ] Append every attempt to `data/repair-journal.jsonl` (target, backup, staged length, final
      length, outcome), including refusals.
- [ ] Dry run returns target, backup location, payload length, and validation result without writing.
- [ ] Restore copies the backup back with the same preflight and no directory creation.
- [ ] If restore fails, report the backup location.

### 5. UI
- [ ] Show the dry-run preview before the user confirms.
- [ ] Report success, refusal, and failure with the journal/backup location.
- [ ] Do not offer repair when target validation fails.

## Context

- `src/GamingCommander.App/Services/SteamAcfWriter.cs` currently calls
  `Directory.CreateDirectory(libraryRoot\steamapps)` before writing.
- `src/GamingCommander.App/Services/EpicItemWriter.cs` currently calls
  `Directory.CreateDirectory(manifestsDir)` before writing.
- `src/GamingCommander.App/MainWindow.PlatformRepair.cs` is the only UI entry point.
- `META/ADR/005-migration-safety.md` requires backup, logging, dry-run, and reversibility.
- Epic `.item` re-creation is a known dead end for registration; that does not make the writer safe.

## Requirements

- [ ] No repair write happens without a validated target and a preflight that refuses missing directories.
- [ ] No `Directory.CreateDirectory` call exists outside the application data root.
- [ ] Every attempt is journalled, including refusals.
- [ ] A dry run performs no writes and no directory creation.
- [ ] A failed copy leaves the original file unchanged, restores it from backup, or deletes the
      wrong-length result it just created.
- [ ] A short or empty target is never left in place silently; it is deleted and journalled.
- [ ] Removal outcome leaves no compiled writer and no dead UI action.

## Verification

- [ ] Temp-folder tests: valid Steam root, valid Epic manifest folder, wrong folder, relative path,
      UNC path, read-only target, short copy result.
- [ ] Missing-directory test: the writer refuses and the directory is still absent afterwards.
- [ ] `grep -n "CreateDirectory" src/GamingCommander.App/Services/SteamAcfWriter.cs` and
      `EpicItemWriter.cs` returns nothing.
- [ ] `grep -rn "CreateDirectory" src/GamingCommander.App/Services/JsonFileHelper.cs` shows only the
      app-data-root guard path (once T130 lands).
- [ ] Dry run leaves the file system unchanged (snapshot before/after).
- [ ] Restore test returns the original bytes.
- [ ] Length-mismatch test: a truncated copy is deleted and reported, not accepted.
- [ ] `dotnet build` / `dotnet test` green.
- [ ] Manual check that the UI cannot reach an unguarded writer.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
