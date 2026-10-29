# Task T129: Guarded Repair Writes

**Tier:** 3 — Logic/Behavior
**Phase:** N — Security and Release Integrity
**Risk:** High
**Status:** pending
**Prerequisites:** Task T129 depends on Task T124C

## Objective

Execute the reviewed decision in `planning/133-repair-write-safety.md`: keep both repair paths with
different jobs — the Steam writer **creates** a known-format ACF, the Epic writer **modifies** an `.item`
Epic already wrote. Neither creates a directory outside the application data root.

## What Needs to Change

### 1. Shared guard (one helper, both writers)
- [ ] Reject relative, UNC, and non-rooted paths before any filesystem call.
- [ ] `Directory.Exists(targetDir)`; false → refuse, name the path, create nothing.
- [ ] Validate the per-store marker (Steam: `libraryfolders.vdf`; Epic: the manifests dir the scan used,
      containing at least one other `*.item`).
- [ ] Validate the target presence rule: Steam requires absence, Epic requires a parseable file.
- [ ] **No `Directory.CreateDirectory` outside the application data root.** `data/staging/` and
      `data/backups/` go through `AppPaths.EnsureAppDataPath`.
- [ ] One staging routine: write bytes under `data/staging/`, read back, parse, copy, length-check.

### 2. Steam — create only
- [ ] Keep the input checks: AppID digits, non-empty library root and installdir.
- [ ] Require `<root>\steamapps` and `libraryfolders.vdf`; refuse otherwise.
- [ ] Refuse when `appmanifest_<appId>.acf` exists; the writer never overwrites.
- [ ] Build, parse back, stage, copy, length-check.
- [ ] On mismatch or `IOException`: delete the target (it did not exist before), report, journal.
- [ ] No backup and no restore branch.

### 3. Epic — modify only
- [ ] Refuse when the target `.item` is absent: this build cannot create one.
- [ ] Refuse when the target does not parse, lacks `InstallationGuid`, `InstallLocation`,
      `CatalogNamespace` or `CatalogItemId`, or names a different installation. Never overwrite it.
- [ ] Delete the `IsHollowItem` replacement path (`EpicItemWriter.cs:74-78`, `:157-171`).
- [ ] Stop rewriting the whole item from the fixed schema. Load the file as a `JsonNode` and set only
      `InstallLocation`, `ManifestLocation`, and `LaunchExecutable` when it is empty. Preserve every other
      member and its order.
- [ ] Assert the member count is unchanged before copy (unknown members preserved).
- [ ] Back up the original to `data/backups/<utc-stamp>/<guid>.item` before the copy; refuse the write if
      the backup cannot be made.
- [ ] On mismatch or `IOException`: copy the backup back over the target, report, journal, keep the backup.

### 4. Journal and dry run
- [ ] Append every attempt to `data/repair-journal.jsonl`: timestamp, action, target, backup (Epic),
      staged length, final length, outcome (`ok`/`refused`/`failed`), message. Refusals included.
- [ ] Dry run returns target, backup location, payload length, and validation result; writes and creates
      nothing.
- [ ] The confirmation shows target path, payload size, the marker that validated the folder, and for Epic
      the backup location.
- [ ] No hash, digest, or signature anywhere. Length plus a successful parse is the validation.

### 5. UI
- [ ] Reword the Epic action from "write" to "repair registration (updates the existing `.item`)" in the
      dialog, the button, and the status messages.
- [ ] Report refusal, success, and failure with the journal location, and for Epic the backup location.
- [ ] Do not offer a repair action when preflight fails.
- [ ] Keep the "quit Epic first / one officialise per launcher run" warning.

## Context

- `planning/133-repair-write-safety.md` is the decision of record.
- `App/Services/SteamAcfWriter.cs:64` creates `steamapps`; `:81` writes in place.
- `App/Services/EpicItemWriter.cs:65` creates the ProgramData manifests folder, `:74-78` replaces a
  corrupt item (via `IsHollowItem`), and `:143-148` rewrites the whole schema.
- `MainWindow.PlatformRepair.cs` is the only UI entry point for both.
- `META/ADR/005-migration-safety.md` requires a preview and a backup for replaced files.
- `data/staging/`, `data/backups/`, and `AppPaths.EnsureAppDataPath` come from `planning/132`.

## Requirements

- [ ] No repair write without a validated target and a preflight that refuses a missing directory.
- [ ] No `Directory.CreateDirectory` outside the application data root.
- [ ] Steam never overwrites; Epic never creates and never replaces an unparseable file.
- [ ] An Epic modification preserves every unknown member.
- [ ] Every attempt is journalled, including refusals.
- [ ] A failed or short copy leaves the store folder as it was (Steam: absent; Epic: original bytes).
- [ ] Dry run performs no writes and no directory creation.

## Verification

- [ ] Temp-folder tests, Steam: valid root, missing `steamapps`, missing `libraryfolders.vdf`, existing
      target, relative path, UNC path, read-only `steamapps`, short copy, dry run.
- [ ] Temp-folder tests, Epic: absent target, corrupt target, moved game, read-only target, short copy,
      wrong manifests dir, dry run. The moved-game test asserts `InstallLocation` changed, all other
      members and their count are unchanged, and a backup exists.
- [ ] Missing-directory tests: refused, and the directory is still absent afterwards, for both stores.
- [ ] `grep -n "CreateDirectory" src/GamingCommander.App/Services/SteamAcfWriter.cs` and
      `EpicItemWriter.cs` returns nothing.
- [ ] `grep -n "IsHollowItem" src/GamingCommander.App/Services/EpicItemWriter.cs` returns nothing.
- [ ] Dry run leaves the file system unchanged (snapshot before/after).
- [ ] `dotnet build` / `dotnet test` green.
- [ ] Manual check that both UI actions pass through the shared guard.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
