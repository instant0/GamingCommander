# Task T80: Registration Repair After User-Initiated File Moves

**Tier:** 3 — Logic/Behavior
**Phase:** J — Post-126 Follow-ups
**Risk:** High (writes launcher registration — safety-critical)
**Status:** pending
**Prerequisites:** Task T124C (repair-write safety, done); Task T129 (guarded repair writes, done); Task T119A (health model, pending)

---

## Objective

Detect that a Steam or Epic game's registration no longer matches the files on disk and offer an
explicit repair. The app never moves game data, never creates links, and never deletes launcher
registration files. When a stale ACF remains in the old library, the app reports its exact path
and tells the user how to remove it manually.

Full design: `planning/04-phase-2-syncmove.md`. Safety decision:
`META/ADR/005-registration-repair-safety.md`.

## What Needs to Change

### 1. Mismatch detection (scan time, read-only)

**Files:** `src/GamingCommander.App/Services/SteamLibraryScanner.cs`,
`src/GamingCommander.App/Services/EpicLibraryScanner.cs`,
`src/GamingCommander.Core/Models/SteamAcfInfo.cs`,
`src/GamingCommander.Core/Models/EpicInfo.cs`

**Current state:** Steam records `Acf.ExpectedPath` / `Acf.LibraryPath`; Epic records `ItemPath`.
The UI has status text but no repair flag.

**Actions:**
- [ ] Steam: compare `Acf.ExpectedPath` (or the ACF `installdir` resolved under `Acf.LibraryPath`)
  with `Location.FolderPath`; mark a mismatch when they differ or the recorded folder is absent.
- [ ] Epic: compare the `.item` `InstallLocation` with `Location.FolderPath`; mark a mismatch when
  they differ or the recorded folder is absent.
- [ ] Keep the flag in the existing typed records (`Acf` / `Epic`) or derive it in the view-model.
  Do not reintroduce `PlatformMetadata`.
- [ ] No writes, no registry reads, and no new scan pass.

### 2. Repair engine (guarded writes)

**New file:** `src/GamingCommander.App/Services/RegistrationRepairService.cs`

**Content:** one preview + one apply entry per case, reusing `RepairPlan` and
`RepairWriteGuard`. No `File.Delete`, no `Directory.CreateDirectory` outside `data/`.

**Steam — same library:**
- [ ] Update the existing ACF `installdir`; back up under `data/backups/<utc-stamp>/`; stage under
  `data/staging/`, parse it back, then copy into place.
- [ ] Journal to `data/repair-journal.jsonl`.

**Steam — different library:**
- [ ] Copy the ACF into the target library's `steamapps/` and update `installdir` if needed.
- [ ] Do **not** move, rename, or delete the old ACF.
- [ ] Return the old ACF path plus a concrete instruction, for example:
  `A stray ACF remains at E:\...\steamapps\appmanifest_<appId>.acf. GamingCommander does not delete
  launcher files. Remove it with File Explorer if Steam reports a conflict.`
- [ ] If the old library still has that install folder, report that fact and do not claim the old
  record is stale.

**Epic:**
- [ ] Update `InstallLocation` in the existing `.item` through `EpicItemWriter`; backup, member-count
  check, journal.
- [ ] Do not create, regenerate, or delete an `.item`.

**Out of scope:** GOG/EA/Ubisoft/Battle.net/Xbox writes.

### 3. UI

**Files:** `src/GamingCommander.App/MainWindow.PlatformRepair.cs`,
`src/GamingCommander.App/Services/EpicRepairDialog.cs` (or a shared repair dialog)

- [ ] A mismatch badge/details line names the store and the recorded path.
- [ ] Repair dialog shows: detected folder, recorded store path, planned write, backup path, and any
  stray old ACF with manual removal instructions.
- [ ] Actions: **Repair**, **Dry run**, **Dismiss**. Dismiss writes nothing.
- [ ] No background, startup, or launch-time probe. Rescan stays F5.

### 4. Tests

- [ ] Steam same-library `installdir` update, with backup and journal.
- [ ] Steam cross-library copy/update leaves the old ACF present and reports its path.
- [ ] Steam outside any library is refused with a clear reason.
- [ ] Epic `InstallLocation` update; missing/unreadable `.item` is refused.
- [ ] Dry run writes nothing.
- [ ] No code path deletes a registration file (grep and a test double).

## Context

- `SteamAcfWriter` is create-only; this task adds a distinct modify/copy path.
- `EpicItemWriter` already modifies an existing `.item`.
- `RepairWriteGuard` owns staging, backup, length checks, and the journal.
- Windows paths are opaque strings; do not use host-OS path rules.
- Prior `MoveOnly`/`MoveAndLink` models were removed; do not reintroduce them.

## Requirements

- [ ] No game file is moved, deleted, or linked.
- [ ] No launcher registration file is deleted; stale files are reported with manual instructions.
  Internal rollback may remove a file this code just created or staged; that is not a delete
  feature for pre-existing launcher files.
- [ ] Every write is previewed, backed up, journaled, and validated.
- [ ] No launcher directory is created outside the application data root.
- [ ] Detection adds no writes and no extra scan pass.
- [ ] Mismatch state uses the typed `Acf` / `Epic` records; no `PlatformMetadata` bag.
- [ ] Tests use temp directories and fixtures; no real registry, ProgramData, or game files.

## Verification

- [ ] `dotnet build` passes (0 errors).
- [ ] `dotnet test` passes; new detection + repair + dry-run tests green.
- [ ] Grep shows no `File.Delete`/`Delete` call in the repair path for ACF/`.item`.
- [ ] Manual (Windows): a moved Steam/Epic game shows the mismatch, repair writes the target record,
  the old ACF remains and is reported, and the launcher recognizes the game.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
