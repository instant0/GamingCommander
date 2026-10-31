# ADR-005: Registration Repair Safety

## Status
Accepted

## Date
2025-01-20 (revised 2026-10-31)

## Context
A user may relocate an installed game with OS tools. The store's registration (manifest, ACF,
registry) can then point at the old location, so the launcher no longer recognizes the game.

GamingCommander does **not** move game files and does **not** create symlinks/junctions. It only
repairs the store registration so the launcher finds the files where they already are.

## Decision
Registration repair is safety-critical:

- Validate that the source and target paths are rooted and present.
- Never create launcher directories outside the application data root.
- Refuse when the store's expected structure is absent.
- Preview the planned change (dry run) before writing.
- Back up any file that is modified, and journal the operation.
- Never move or delete game data.
- Ask before removing an old registration file.

Per-store actions:

- **Steam:** create the ACF in the game's actual library. When an ACF for the same AppID already
  exists in another library, copy it to the target library and update `installdir` if needed. Do
  **not** delete or move the old ACF: report its exact path and tell the user how to remove it
  manually.
- **Epic:** modify an `.item` Epic already wrote; do not invent one.

## Consequences
- Repair code paths are separate from detection.
- Backups live under `data/backups/`.
- Operations are logged to `data/repair-journal.jsonl`.
- Changes are reversible from the backup.
- The app has no move/symlink mode and never relocates game data.
- The app has **no delete functionality** for launcher registration files. Stray ACFs/items are
  reported with their exact path and manual removal instructions. Internal rollback may remove a
  file the app just created or staged; that is not a delete feature for pre-existing files.
