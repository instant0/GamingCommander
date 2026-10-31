# Phase 2.1 — Registration Repair After User-Initiated File Relocation

**Status:** planned. The app never moves, deletes, or links game data.

## Design rule

GamingCommander does **not** move game files and does **not** create symlinks/junctions. The user
relocates files with OS tools. The app detects that a store's registration no longer matches the
files and offers to repair that registration so the launcher recognizes the game again.

The app also has **no delete functionality** for launcher registration files. When an obsolete
registration file remains at the old location, the app reports its exact path and tells the user
how to remove it manually. The user decides and performs any deletion.

## Goal

After a user moves a game's files (for example `D:\games\Game` → `E:\games\Game`), detect the
mismatch at scan time and offer a one-click repair of the store record: ACF, `.item`, or registry.
No re-download and no launcher reconfiguration.

## Detection at scan time

Each `GameEntry` already carries the store facts it needs:

- Steam: `Acf.FilePath`, `Acf.LibraryPath`, `Location.FolderPath`, `Location.LibraryRoot`.
- Epic: `Epic.ItemPath`, `Location.FolderPath`.
- Registry stores: the reader's recorded install path.

A mismatch exists when the recorded store location and the scanned folder disagree, or when the
recorded folder no longer exists. Detection writes nothing; it only sets the mismatch state used
by the repair UI.

Do not add a second path model. Compare real paths through `PathText` and keep Windows paths as
opaque strings.

## Per-store repair

### Steam — same library, new folder name

- ACF stays in `<library>\steamapps\appmanifest_<appId>.acf`.
- Update the `installdir` member to the new folder name.
- Backup first; stage and validate before replacing.

### Steam — different library

- The game folder is under a different library's `steamapps\common\`.
- Copy the existing ACF into the target library's `steamapps\`, then update `installdir` if needed.
- Do **not** move, delete, or rename the old ACF. After a successful write, report the old path with
  a concrete manual step:
  `A stray ACF remains at E:\...\steamapps\appmanifest_<appId>.acf. GamingCommander does not delete
  launcher files. Remove it with File Explorer if Steam reports a conflict.`
- If the old library still contains that install folder, report it and do not claim the old record
  is stale.

### Steam — outside every library

Steam only recognizes games under `steamapps\common\`. The app cannot create a valid library
registration; report that and offer the existing standalone classification instead.

### Epic

- Update `InstallLocation` in the existing `.item` that Epic wrote.
- Do not create an `.item`, do not regenerate one, and do not delete one.
- Backup and validate the member count before replacing.

### Out of scope

GOG, EA, Ubisoft, Battle.net, and Xbox registry/package repair. Their detection stays read-only.
A future task may add a registry repair path; it is not part of this phase.

## Safety contract

- Preview every change (dry run) and ask before writing.
- Back up the target file under `data/backups/<utc-stamp>/`.
- Journal the operation to `data/repair-journal.jsonl`.
- Stage generated content under `data/staging/`, read it back, then copy into place.
- Never create a launcher directory outside the application data root.
- Never delete a launcher registration file. Report it instead. Internal rollback may remove a
  file the app just created or staged; that is not a delete feature for pre-existing files.
- Never write into the game installation folder.
- Treat paths as opaque strings; use cross-platform-safe .NET APIs.

## UI

- A mismatch badge and details text explain which store record is stale.
- The repair dialog shows: store, game, detected folder, recorded store path, planned write,
  backup path, and any stray old registration file with manual removal instructions.
- Actions: **Repair**, **Dry run**, **Dismiss**. Dismiss leaves the registration untouched.
- Rescan stays F5; the repair action is explicit. No background or launch-time probing.

## Success criteria

- A moved Steam game in another library can be registered in the target library without deleting
  the old ACF; the old path and manual removal step are shown.
- A moved Epic game can have its `.item` `InstallLocation` updated with a backup and a journal.
- A moved game outside a store layout is reported, not falsely repaired.
- No code path moves or deletes game data or registration files.
- All writes are previewed, backed up, journaled, and validated.

## References

- `META/ADR/005-registration-repair-safety.md` — the repair safety decision.
- `planning/133-repair-write-safety.md` — the guarded write contract.
