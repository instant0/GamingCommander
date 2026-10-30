# Task T80: SyncMove Phase 2.1 — Registration Repair After File Moves

**Tier:** 3 — Logic/Behavior
**Phase:** J — Post-126 Follow-ups
**Effort:** ~3–4 hours (split into slices)
**Risk:** High (writes launcher manifests — safety-critical)
**Status:** Deferred — **must be rewritten before it can start** (see note)
**Prerequisites:** Plan 124 anchor model (done); Plan 123 §8.3 `libraryfolders.vdf` bootstrap (for cross-library ACF, can be deferred)

> **2026-10-30 — rewrite required.** The guarded repair contract (`planning/133`) makes the current UI
> repair create-only for Steam: it refuses when the ACF exists. Updating an existing ACF (this task's
> Slice 2) is therefore a **different modify path**: it needs its own preconditions, a backup, and a
> restore rule. Before starting, reconcile this task with `planning/132` and `planning/133`:
> - backup location must be `data/backups/<utc-stamp>/`, not `data/backups/<gameid>.<ext>.bak`;
> - the operation log is `data/repair-journal.jsonl`, not a second `migration_log.jsonl`;
> - no `Directory.CreateDirectory` outside the app data root; cross-library copy must refuse a missing
>   `steamapps` rather than create it;
> - the "keep original as backup" step is a copy, not a rename.
> Do not start until detection is settled (unchanged).

---

## Objective

Implement scan-time detection + one-click repair of launcher registration after a user manually moves
game files. The app never moves game files; it only repairs the store's manifest/registration.
Full design and per-store detail: `planning/04-phase-2-syncmove.md`.

## What Needs to Change (slices)

### Slice 1 — Mismatch detection (scan time)
- [ ] Add mismatch state to `GameEntry` (preferred: `PlatformMetadata` keys `ManifestMismatch`/`DetectedPath`; do **not** reorder `GameEntry` params).
- [ ] `SteamLibraryScanner`: compare ACF `installdir` + library path vs actual `steamapps/common/<folder>`; set mismatch + detected path.
- [ ] `EpicLibraryScanner`/`EpicManifestParser`: compare `.item` `InstallLocation` vs actual `FolderPath`.
- [ ] Persist mismatch through `GamesDatabaseService` (survives restart).

### Slice 2 — Repair engine (with backup + log)
- [ ] New `src/GamingCommander.App/Services/ManifestRepairService.cs`:
  - Steam same-library: update `installdir` in the existing ACF (backup first).
  - Steam cross-library: copy ACF to target library `steamapps/`, update `installdir`, keep original as backup.
  - Epic: rewrite `.item` `InstallLocation` (backup first).
- [ ] New backup writer → `data/backups/<gameid>.<ext>.bak`.
- [ ] New append-only log → `data/migration_log.jsonl` (timestamp, gameId, source, old/new path, manifestPath, action, backupPath).
- [ ] Dry-run mode returns the planned changes without writing.
- [ ] Reuse `SteamAcfWriter.Build` + `SteamAcfParser.ParseAcfFile`; note `TryWrite` refuses existing files — the repair path needs explicit backup+overwrite.

### Slice 3 — UI (F6) + indicators
- [ ] F6 opens the SyncMove repair dialog for a mismatched selected game (F6 is free; rescan is F5).
- [ ] Dialog shows store, game, old manifest path, detected new path, planned action, `Fix Registration` / `Dry Run` / `Dismiss`.
- [ ] `⚠️ moved` badge in the list + explanatory details-panel text; clear on success/dismiss.
- [ ] Out of Phase 2.1 scope: GOG/EA/Ubisoft registry repair; launch-time mismatch detection.

## Context

- **Constraints verified 2026-10-14** — see `planning/04-phase-2-syncmove.md` §"Findings & constraints": anchor model, reusable `SteamAcfWriter`/`SteamAcfParser`, no `GameEntry.ManifestMismatch`, F6 free, `PlatformMetadataKeys` to reuse, no backup/log infra yet, registry stores out of scope.
- **Safety (META/RULES.md):** validate paths, back up originals, log operations, ensure recovery; distinguish move-only / move+link / metadata-repair. Phase 2.1 is metadata-repair only.
- Cross-library Steam resolution should reuse Plan 123 §8.3 `libraryfolders.vdf` bootstrap once available.

## Requirements

- [ ] Scan flags moved Steam + Epic games without writing anything.
- [ ] Dry run previews changes; repair applies them with a backup.
- [ ] `data/backups/` + `data/migration_log.jsonl` created and written.
- [ ] F6 dialog only enabled for mismatched games.
- [ ] No game files are moved, deleted, or linked.
- [ ] Windows paths treated as opaque strings; cross-platform-safe APIs only.

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes; new mismatch + repair + dry-run tests green (temp dirs, fixtures)
- [ ] Manual (Windows): moved Epic/Steam game flags mismatch; F6 repairs; launcher recognizes it
- [ ] Backup exists after repair; log entry appended

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
