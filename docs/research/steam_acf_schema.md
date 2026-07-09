# Steam ACF Schema — Required Fields Only

## Purpose
This document describes the subset of the Steam ACF (Application Configuration File)
format that is required for game **identification** and **migration support**.

The ACF format is Valve's VDF variant — `"key" "value"` pairs and `"key" { ... }`
nested blocks. Files are named `appmanifest_<appid>.acf` and stored in a library's
`steamapps/` directory (alongside `libraryfolders.vdf`).

## Required Fields

Only the following fields under the root `"AppState"` block are extracted:

| Field | Type | Purpose | Used For |
|-------|------|---------|----------|
| `appid` | string (numeric) | Unique Steam App ID | Identification — primary key for all Steam operations |
| `name` | string | Human-readable game name | Identification — display name |
| `installdir` | string | Directory name under `steamapps/common/` | Identification — maps game to its folder |
| `StateFlags` | string (numeric bitmask) | Installation state (4 = fully installed) | Migration — validate game is ready |
| `LastUpdated` | string (Unix timestamp) | Last content update time | Migration — detect stale/stale-after-move |
| `SizeOnDisk` | string (bytes) | Total size on disk | Migration — verify move completed/space |
| `buildid` | string (numeric) | Current installed build ID | Migration — track version, detect drift |

## Format Notes

- All values are quoted strings even when they represent numbers or timestamps.
- Windows path separators (`\`) appear in paths like `installdir` and `LauncherPath`.
- Nested blocks (e.g., `"InstalledDepots"`, `"UserConfig"`, `"MountedConfig"`) are
  **not required** and are skipped during extraction. They contain depot manifests,
  user settings, and installed scripts that are irrelevant for basic identification
  and migration.
- The `StateFlags` field uses a bitmask. The most common values are:
  - `4` = fully installed and valid
  - `6` = installed but needs update
- Timestamps in `LastUpdated` are Unix epoch seconds (UTC).

## Usage in Identification

Given an `appmanifest_<appid>.acf` file, the three identification fields
(`appid`, `name`, `installdir`) uniquely determine:
1. Which game this is (by `appid` and `name`)
2. Where its files live on disk (`steamapps/common/<installdir>/`)

## Usage in Migration Support

When moving a game to a new library location, the migration fields enable:
1. **Pre-flight check**: `StateFlags` confirms the game is fully installed (value `4`).
2. **Disk space validation**: `SizeOnDisk` provides the expected transfer size.
3. **Post-move verification**: `LastUpdated` timestamps and `buildid` can be compared
   to confirm the game state is intact.
4. **Manifest generation**: When creating a new ACF for the relocated game, these
   fields can be preserved (or updated) to maintain a valid game entry for Steam.

## Out of Scope

The following ACF features are explicitly NOT required for this phase:
- `InstalledDepots` block (depot manifests and sizes)
- `InstallScripts` block (redistributable installer paths)
- `UserConfig` / `MountedConfig` blocks
- `LastOwner`, `BytesToDownload`, `BytesDownloaded`, `BytesToStage`, `BytesStaged`,
  `AutoUpdateBehavior`, `ScheduledAutoUpdate`, and other runtime/status fields
- The `universe` field
