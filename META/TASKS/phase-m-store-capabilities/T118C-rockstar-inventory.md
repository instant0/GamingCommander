# Task T118C: Rockstar Inventory Reader

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Low
**Status:** completed (2026-10-31)
**Prerequisites:** None (consumes the shared `StoreInventoryEntry`/`IStoreInventoryReader` contract delivered by T118A); T130 done

> **Review revision (2026-10-31):**
> - `StoreReportedState` has no `Missing` value. “State Missing” means `FolderExists = false`; the
>   import service maps that to `InstallState.Missing`.
> - Rockstar identity is the `title.rgl` text. When the file is absent, empty, unreadable, or contains
>   control characters, omit the title ID and leave `StoreId` empty; the import service matches by path.
>   The registry subkey name stays in metadata as `RegistryGameName`.
> - Do not guess a launch exe from registry value names (`GTAV`, `GTAVLauncher`, …). They vary per title;
>   the folder scan/enrichment owns exe discovery.
> - The registry is best-available, not a complete catalogue. Absence of a registry key must not mark a
>   persisted entry `Missing`; only a present record with a missing folder does.

> **Test policy:** follow `planning/136-test-suite-reduction.md` — one test per behavior/failure.

## Objective

Read Rockstar's readable inventory: per-game registry keys already used by `RegistryFallbackDetector`, plus
`title.rgl` as an identity marker. `Settings.json` is **not** parsed until T136 confirms its schema; the
registry path is the source for this task.

## What Needs to Change

- [x] Enumerate per-game Rockstar registry keys and read the install path, skipping the Launcher key.
- [x] Persist the title ID from `title.rgl` only when it can be read safely; otherwise omit it and leave
      `StoreId` empty.
- [x] Return records for missing folders with `FolderExists = false` (mapped to `Missing` by the import
      service).
- [x] Do not parse `Settings.json`; do not add a YAML/JSON schema dependency for it here.
- [x] Register through the shared import service; no new write path.

## Tests

- [x] Registry key with path → record.
- [x] Missing folder → record retained with `FolderExists = false`.
- [x] `title.rgl` present → title ID set; absent/empty → omitted and `StoreId` empty.
- [x] Launcher key is skipped.
- [x] No writes/network.

## Verification

- [x] `dotnet build` 0 errors; `dotnet test` green.

## Stop rules

- Do not parse `Settings.json` here (T136 may open that separately).
- Do not add Ubisoft/Battle.net here (T118C2/C3).
- Do not treat a missing registry entry as removal.

## Completion Notes

- **Completed:** 2026-10-31
- **What was done:**
  - Added `RockstarInventoryMetadata` (registry game name, title ID, `title.rgl` path, registry key).
  - Added `RockstarInventoryReader`: enumerates `HKLM\SOFTWARE\WOW6432Node\Rockstar Games`, skips the
    `Launcher` key, reads `InstallFolder`, and returns every record including those whose folder is
    gone (`FolderExists = false`).
  - `title.rgl` is read as short printable text. A non-empty, control-free value becomes `StoreId` and
    `TitleId`; absent, empty, over-long, or control-containing files omit the ID and leave `StoreId`
    empty. The registry subkey name is kept in metadata.
  - No launch exe is guessed from registry value names; folder scan/enrichment owns exe discovery.
  - Read-only: no writes, `Settings.json` parse, network, or new write path.
- **Verification:** `dotnet build` 0 errors (only the five pre-existing warnings); `dotnet test`
  **587 green** (Core 157, App 430). New tests cover the registry record with title, missing folder,
  missing/empty/control title, Launcher-key exclusion, and an empty registry.
  `python3 tools/validate_probe_matrix.py` passes all 22 scenarios.
- **Issues encountered:** the registry is best-available, not a complete catalogue. The future diff must
  not mark a persisted Rockstar entry `Missing` merely because its registry key is absent; only a present
  record whose folder is gone produces `Missing`.
