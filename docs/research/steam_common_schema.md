# Steam `common/` Folder Cross-Reference — Approach

## Purpose
This document describes the approach for cross-referencing Steam ACF `installdir`
fields with actual folder names in `steamapps/common/`. This is the mechanism
that maps a logical game entry (ACF) to its physical files on disk.

## The Mapping Rule

ACF files contain an `installdir` field which is a **folder name** (not a full path).
The actual game files reside at:

```
{library_root}/steamapps/common/{installdir}/
```

The cross-reference is a straight string comparison: does a directory named
`{installdir}` exist under `steamapps/common/`?

## Cross-Reference Outcomes

| Outcome | Meaning | Handling |
|---------|---------|----------|
| **Matched** | ACF `installdir` has a corresponding folder in `common/` | Normal state. Game is fully present. |
| **Missing folder** | ACF references an `installdir` that does not exist in `common/` | Game files are absent. ACF is stale or the folder was moved/deleted manually. |
| **Orphaned folder** | Folder exists in `common/` but no ACF has this `installdir` | Non-game content (Steam system folders, manually placed folders) or a game whose ACF was removed. |

## Identifying a Game from Its Folder

Given a folder path `{library_root}/steamapps/common/{folder_name}/`, the
corresponding ACF is found by:

1. Scanning `{library_root}/steamapps/appmanifest_*.acf` files.
2. For each ACF, reading the `installdir` field.
3. Matching when `installdir == folder_name`.

This is the reverse mapping: from folder → ACF → appid → launch URI.

## Usage in Migration

During migration (moving a game to a new library):

1. **Before move**: Read the ACF's `installdir` to know which folder to move.
2. **After move**: The ACF is updated (or regenerated) with the new library path
   context. The `installdir` field itself does not change — only the containing
   library root changes.
3. **Validation**: After migration, re-run the cross-reference at the new library
   to confirm the match succeeded.

## Usage in Identification

The identification flow is:
```
appmanifest_<appid>.acf → appid + name → (display)
                       → installdir → common/{installdir}/ → game files
```

This enables:
- Displaying game name and executable from the `common/` folder
- Launching via `steam://rungameid/<appid>`
- Locating save data, configs, or mod directories within the game folder

## Limitations
- This cross-reference is purely name-based. It does not validate file contents
  or integrity.
- Non-ASCII characters in folder names (e.g., "™", "®") may differ between the
  ACF `name` field and the actual filesystem folder name. The `installdir` field
  should always match the filesystem folder name exactly.
