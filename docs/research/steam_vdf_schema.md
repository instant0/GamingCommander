# Steam `libraryfolders.vdf` — Required Structure

## Purpose
This document describes the subset of the `libraryfolders.vdf` format required for
discovering Steam library paths for game migration.

The file lives at `{library_root}/steamapps/libraryfolders.vdf` and enumerates all
Steam library folders known to the installation. It is a discovery file only —
migration works by moving/copying ACFs between library `steamapps/` folders, and
Steam rescans + updates this file on restart.

## Required Structure

Only the following fields under each numbered library entry are extracted:

| Field | Type | Purpose |
|-------|------|---------|
| `path` | string (Windows path) | Library root directory |
| `label` | string | Optional user-assigned label |
| `totalsize` | string (numeric bytes) | Total capacity (0 = auto/managed) |
| `apps` | block of `{appid: size}` | Map of installed app IDs to their size on disk |

## Format Notes

- The root key is `"libraryfolders"` containing numbered entries `"0"`, `"1"`, `"2"`, etc.
- Paths use Windows backslash separators. The VDF format may serialize with
  doubled backslashes (`P:\\...`) or single (`P:\...`).
- The `apps` block within each library entry lists `"appid" "size_in_bytes"`
  pairs. This provides a quick registry of which games live in which library.
- Other fields (`contentid`, `update_clean_bytes_tally`,
  `time_last_update_verified`) are not required for discovery or migration.

## Usage in Discovery

Given the `libraryfolders.vdf` from the default Steam installation, enumeration
provides:
1. All library root paths (potential source and target locations)
2. Which app IDs reside in each library
3. Available capacity for migration planning (via `totalsize`)

## Usage in Migration

The migration flow does NOT modify `libraryfolders.vdf` directly:

1. **Pre-move**: Use the library list to select source and target locations.
2. **Move**: Transfer the game folder to `{target_library}/steamapps/common/` and
   the ACF to `{target_library}/steamapps/appmanifest_<appid>.acf`.
3. **Post-move**: Remove the old ACF from the source library's `steamapps/`.
4. **Restart**: Steam rescans all known `steamapps/` directories on restart and
   updates `libraryfolders.vdf` automatically.

## Limitations
- Only the default Steam installation's `libraryfolders.vdf` is guaranteed to
  exist. Other libraries may lack this file.
- Registry fallback (`HKCU\Software\Valve\Steam\SteamPath`) can locate the
  default Steam installation if the VDF file is not available.
- The `apps` block provides a snapshot — it may be slightly stale if Steam is
  running or was recently closed.
