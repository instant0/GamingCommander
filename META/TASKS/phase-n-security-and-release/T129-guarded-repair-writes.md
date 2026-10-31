# Task T129: Guarded Repair Writes

**Tier:** 3 — Logic/Behavior
**Phase:** N — Security and Release Integrity
**Risk:** High
**Status:** completed
**Prerequisites:** Task T129 depends on Task T124C (done) and Task T130 (`AppPaths` guard + `data/staging`)

## Shape (decided here; `planning/133` has the rationale)

One helper, `RepairWriteGuard` (`src/GamingCommander.App/Services/RepairWriteGuard.cs`), used by both
writers. It is not a framework: preflight, stage, copy, length-check, journal, and a dry-run preview.

```csharp
internal sealed record RepairPlan(
    string TargetPath, string? BackupPath, int StagedLength,
    string Marker, bool IsValid, string Message);

internal static bool TryPreflight(string targetDir, string targetPath, RepairWriteKind kind,
                                  out RepairPlan plan, out string error);
internal static bool TryStage(string name, string content, Func<string, bool> parses,
                              Action<string>? journal, out string stagedPath, out string error);
internal static bool TryCopyAndVerify(string stagedPath, string targetPath, string? backupPath,
                                      Action<string>? journal, out string error);
```

`RepairWriteKind` is `SteamAcfCreate` or `EpicItemModify`; the kind selects the presence rule and the undo
(Steam: delete the target it created; Epic: restore the backup). Keep it in one file; no per-store helper
class, no interface.

Append to `data/repair-journal.jsonl` with a small `AppendAllText` call (JSONL is not a JSON document;
do not route it through `JsonDocumentStore`).

## Objective

Execute the reviewed decision in `planning/133-repair-write-safety.md`: keep both repair paths with
different jobs — the Steam writer **creates** a known-format ACF, the Epic writer **modifies** an `.item`
Epic already wrote. Neither creates a directory outside the application data root.

## What Needs to Change

### 1. Shared guard (one helper, both writers)
- [x] Reject relative, UNC, and non-rooted paths before any filesystem call.
- [x] `Directory.Exists(targetDir)`; false → refuse, name the path, create nothing.
- [x] Validate the per-store marker (Steam: `libraryfolders.vdf`; Epic: the manifests dir the scan used,
      containing at least one other `*.item`).
- [x] Validate the target presence rule: Steam requires absence, Epic requires a parseable file.
- [x] **No `Directory.CreateDirectory` outside the application data root.** `data/staging/` and
      `data/backups/` go through `AppPaths.EnsureAppDataPath`.
- [x] One staging routine: write bytes under `data/staging/`, read back, parse, copy, length-check.

### 2. Steam — create only
- [x] Keep the input checks: AppID digits, non-empty library root and installdir.
- [x] Require `<root>\steamapps` and `libraryfolders.vdf`; refuse otherwise.
- [x] Refuse when `appmanifest_<appId>.acf` exists; the writer never overwrites.
- [x] Build, parse back, stage, copy, length-check.
- [x] On mismatch or `IOException`: delete the target (it did not exist before), report, journal.
- [x] No backup and no restore branch.

### 3. Epic — modify only
- [x] Refuse when the target `.item` is absent: this build cannot create one.
- [x] Refuse when the target does not parse, lacks `InstallationGuid`, `InstallLocation`,
      `CatalogNamespace` or `CatalogItemId`, or names a different installation. Never overwrite it.
- [x] Delete the `IsHollowItem` replacement path (`EpicItemWriter.cs:74-78`, `:157-171`).
- [x] Stop rewriting the whole item from the fixed schema. Load the file as a `JsonNode` and set only
      `InstallLocation`, `ManifestLocation`, and `LaunchExecutable` when it is empty. Preserve every other
      member and its order.
- [x] Assert the member count is unchanged before copy (unknown members preserved).
- [x] Back up the original to `data/backups/<utc-stamp>/<guid>.item` before the copy; refuse the write if
      the backup cannot be made.
- [x] On mismatch or `IOException`: copy the backup back over the target, report, journal, keep the backup.

### 4. Journal and dry run
- [x] Append every attempt to `data/repair-journal.jsonl`: timestamp, action, target, backup (Epic),
      staged length, final length, outcome (`ok`/`refused`/`failed`), message. Refusals included.
- [x] Dry run returns target, backup location, payload length, and validation result; writes and creates
      nothing.
- [x] The confirmation shows target path, payload size, the marker that validated the folder, and for Epic
      the backup location.
- [x] No hash, digest, or signature anywhere. Length plus a successful parse is the validation.

### 5. UI
- [x] Reword the Epic action from "write" to "repair registration (updates the existing `.item`)" in the
      dialog, the button, and the status messages.
- [x] Report refusal, success, and failure with the journal location, and for Epic the backup location.
- [x] Do not offer a repair action when preflight fails.
- [x] Keep the "quit Epic first / one officialise per launcher run" warning.

## Context

- `planning/133-repair-write-safety.md` is the decision of record.
- `App/Services/SteamAcfWriter.cs:64` creates `steamapps`; `:81` writes in place.
- `App/Services/EpicItemWriter.cs:65` creates the ProgramData manifests folder, `:74-78` replaces a
  corrupt item (via `IsHollowItem`), and `:143-148` rewrites the whole schema.
- `MainWindow.PlatformRepair.cs` is the only UI entry point for both.
- `META/ADR/005-migration-safety.md` requires a preview and a backup for replaced files.
- `data/staging/`, `data/backups/`, and `AppPaths.EnsureAppDataPath` come from `planning/132`.

## Requirements

- [x] No repair write without a validated target and a preflight that refuses a missing directory.
- [x] No `Directory.CreateDirectory` outside the application data root.
- [x] Steam never overwrites; Epic never creates and never replaces an unparseable file.
- [x] An Epic modification preserves every unknown member.
- [x] Every attempt is journalled, including refusals.
- [x] A failed or short copy leaves the store folder as it was (Steam: absent; Epic: original bytes).
- [x] Dry run performs no writes and no directory creation.

## Verification

- [x] Temp-folder tests, Steam: valid root, missing `steamapps`, missing `libraryfolders.vdf`, existing
      target, relative path, UNC path, read-only `steamapps`, short copy, dry run.
- [x] Temp-folder tests, Epic: absent target, corrupt target, moved game, read-only target, short copy,
      wrong manifests dir, dry run. The moved-game test asserts `InstallLocation` changed, all other
      members and their count are unchanged, and a backup exists.
- [x] Missing-directory tests: refused, and the directory is still absent afterwards, for both stores.
- [x] `grep -n "CreateDirectory" src/GamingCommander.App/Services/SteamAcfWriter.cs` and
      `EpicItemWriter.cs` returns nothing.
- [x] `grep -n "IsHollowItem" src/GamingCommander.App/Services/EpicItemWriter.cs` returns nothing.
- [x] Dry run leaves the file system unchanged (snapshot before/after).
- [x] `dotnet build` / `dotnet test` green.
- [x] Manual check that both UI actions pass through the shared guard.

## Stop rules

- **Two writers only.** Do not add a generic repair service, an interface, a plugin point, or a third
  store operation.
- **No `Directory.CreateDirectory` outside `data/`.** Use `AppPaths.EnsureAppDataPath` for staging/backups.
- **No hash, digest, signature, or temp file in the target folder.**
- **Do not change scan, detection, or launch behaviour.**
- **Do not rewrite `GameEntry` identity or the Epic catalog.** This task only changes what the two writers
  write and how they validate it.
- **Tests use temp directories and synthetic files**; never touch the real Steam library or ProgramData.

## Files expected to change

`RepairWriteGuard.cs` (new), `SteamAcfWriter.cs`, `EpicItemWriter.cs`, `MainWindow.PlatformRepair.cs`,
`EpicRepairDialog.cs` (wording), `MainWindow.axaml` only if a label changes, plus
`SteamAcfWriterTests.cs`, `EpicItemWriterTests.cs`, and a new `RepairWriteGuardTests.cs`.

## Completion Notes

- **Completed:** 2026-10-31
- **What was done:**
  - Added `RepairWriteGuard` with `RepairPlan`, path/marker/presence preflight, one staging routine under
    `data/staging/`, one `File.Copy` plus length and parse verification, kind-selected undo, Epic backup
    creation under `data/backups/`, and JSONL journaling in `data/repair-journal.jsonl`.
  - Steam now creates only: it requires `steamapps` and `libraryfolders.vdf`, refuses an existing ACF,
    stages and verifies the generated payload, and deletes the file it created on any failure.
  - Epic now modifies only: it refuses a missing, corrupt, incomplete, or mismatched-guid `.item`, loads the
    file as `JsonObject`, updates `InstallLocation`, `ManifestLocation` (when present), and an empty
    `LaunchExecutable`, preserves every other member and its order, asserts the member count, and backs up
    before the copy. The `IsHollowItem` replacement branch and the fixed-schema payload are gone.
  - `Preview` on both writers returns target, backup, payload length, marker, validity, and message without
    writing. The shell calls it before either write; Epic shows the details in the confirmation dialog and
    refuses before offering it.
  - UI wording changed from "Write Epic .item" to "Repair Epic registration (updates the existing .item)";
    success/failure/refusal messages name the journal and, for Epic, the backup.
- **Verification:**
  - `dotnet build GamingCommander.sln -v q --nologo`: 0 errors, 0 warnings.
  - `dotnet test GamingCommander.sln --no-build -v q --nologo`: 755 passing (Core 219, App 535,
    Migration 1); no failures.
  - `grep -n "CreateDirectory"` in both writers returns nothing; `grep -n "IsHollowItem"` in
    `EpicItemWriter.cs` returns nothing.
  - New `RepairWriteGuardTests` cover relative/UNC paths, missing directories, each store marker and
    presence rule, staging, short copies with both undos, and journal JSON. Writer tests cover the Steam
    create/refuse/read-only/dry-run cases and the Epic moved-game preservation/backup case plus absent,
    corrupt, wrong-folder, wrong-guid, missing-member, read-only, and dry-run cases.
- **Issues encountered:**
  - `AppPaths` is the only allowed creator and has no test override, so guard tests use the test host's
    `data/staging`, `data/backups`, and journal under the test output folder; store targets stay in temp
    directories.
  - `ManifestLocation` is updated only when the member already exists. Adding it to an item that lacks it
    would change the member count that proves unknown members were preserved; all real items carry it.
  - The Steam read-only case is asserted by outcome (failed, no ACF left) rather than by a specific
    exception, because the test host may run with elevated filesystem capabilities.
  - `TryReadOvt` in `EpicItemWriter` was removed: it could only return a guid that `FindManifestGuid`
    already supplied, so it had become dead code once item creation was deleted.
