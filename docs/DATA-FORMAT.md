# GamingCommander — Internal Save/Data Format (Fixed Schema)

This document is the **fixed standard** for the JSON files the application writes
under its `data/` folder (next to the exe). It complements `ONLINE-AND-DATA.md`
(where files live, when written); this document defines the **exact structure**.

> Reference code: `JsonFileHelper.WriteToFile` / `ReadFromFile` (indented JSON,
> case-insensitive property matching). All files live in `{app-base}/data/`.
>
> **Status note:** This documents the **target / intended** schema (anchor-based,
> two-file layout), decided 2026-09-08. The previous implementation is
> path-anchored and work-in-progress; existing on-disk `games.json`/`settings.json`
> data is **not migrated** — it may be discarded.

---

## Core concept: ANCHOR (Library) vs GAME

- **Library / Anchor** — a unique, named top-level catalog (e.g. `Steam`, `GOG`,
  `EPIC`, `d:\games`). An anchor has a **type** (Steam, Epic, Standalone, …) and
  **multiple physical folders**. Several physical Steam folders collapse into ONE
  `Steam` anchor; two Ubisoft catalog folders collapse into ONE `UbisoftConnect`
  anchor.
- **Game** — a discovered game. A game is **linked to an anchor** by the anchor's
  name, not to any single physical folder. This lets games detected inside one
  folder be re-assigned to another anchor of the correct type (e.g. an Epic game
  found in `d:\games` is relinked to the `EPIC` anchor).

Layout (user-specified, 2026-09-08):

```json
{
  "Steam":  { "type": "Steam",       "folders": ["d:\\steamlibrary", "e:\\steamlibrary", "p:\\program files (x86)\\Steam"] },
  "d:\\games": { "type": "Standalone", "folders": ["d:\\games"] },
  "GOG":    { "type": "Gog",         "folders": ["p:\\program files (x86)\\Gog Galaxy\\Games"] },
  "EPIC":   { "type": "Epic",        "folders": [] }
}
```

A game is anchored to one of these names. **Separate files:** the library/anchor
database and the game database are two distinct files (previously mixed in one
`games.json`).

---

## 1. `libraries.json` — Library (Anchor) database

The set of anchors. Map of **anchor name** → **anchor object**.

Top-level shape:

| Property | Type | Meaning |
|----------|------|---------|
| `version` | int | Schema version (currently `1`) |
| `libraries` | object (name → **anchor object**) | All configured anchors, keyed by unique name |

### Anchor object (`libraries.<name>`)
| Property | Type | Meaning |
|----------|------|---------|
| `type` | `GameSourceKind` (int) | Source type for this anchor (Steam, Epic, Standalone, …) |
| `folders` | array of string | Physical library root paths that feed this anchor |

Rules:
- The anchor **name is unique** and is the linkage key for game entries.
- An anchor's name is its display title in the VFS left pane (e.g. `Steam`,
  `GOG`, `EPIC`, or a plain folder name like `d:\games`).
- An anchor owns **zero or more** physical folders. When the scanner resolves
  `folders`, it scans each physical folder and stores discovered games under the
  anchor.
- Assigning a game to an anchor is by name; a game may be moved/reassigned to a
  different anchor of matching `type` (Epic game found under `d:\games` → `EPIC`).

### Anchor ↔ physical path relationship

- **Standalone anchors are 1:1 with a physical path.** The anchor name literally
  IS the library path (e.g. anchor `d:\games`, type Standalone, folders
  `["d:\games"]`). A game's anchor equals the folder it physically lives under.
- **Platform anchors may be virtual or span many physical paths.** E.g. one
  `Steam` anchor owns `["d:\steamlibrary", "e:\steamlibrary",
  "p:\program files (x86)\Steam"]`; one `EPIC` anchor may own several
  `Manifests` folders. A game's **anchor is the displayed top-level library**,
  which is NOT necessarily the physical path it resides in.
- Every game entry therefore records **two** things: its `Library` (the anchor
  it belongs to / is displayed under) AND its `FolderPath` (the physical
  folder where it actually lives). These are equal only for Standalone anchors.

#### `GameSourceKind` values (stored as int)
`Unknown=0`, `Standalone=1`, `Steam=2`, `Gog=3`, `Epic=4`, `EaApp=5`,
`UbisoftConnect=6`, `BattleNet=7`, `Xbox=8`, `Rockstar=9`, `SteamEmu=10`
(other values per enum).

---

## 2. `games.json` — Game database

All game entries, each linked to an anchor by name.

Top-level shape:

| Property | Type | Meaning |
|----------|------|---------|
| `version` | int | Schema version (currently `1`) |
| `games` | array of **game-entry objects** | All discovered games |

### Game-entry object (`games[]`)
| Property | Type | Meaning |
|----------|------|---------|
| `Id` | string (16 hex) | Deterministic ID; `first 16 hex of MD5("{Library}\|{folderPath}")`, lowercase |
| `Library` | string | **Anchor name** the game belongs to / is displayed under (e.g. `Steam`, `GOG`, `d:\games`) |
| `FolderName` | string | Installation folder name |
| `FolderPath` | string | Absolute **physical** path of the game folder. Equals the anchor's sole folder ONLY for 1:1 Standalone anchors; for virtual/multi-folder anchors it is the real game location, not the anchor |
| `DisplayName` | string | Human-readable title shown in the UI |
| `GameSource` | `GameSourceKind` (int) | Detected/overridden source (normally matches anchor type) |
| `Override` | bool | True if the user manually changed `GameSource` |
| `ExecutablePath` | string | Primary executable path |
| `LauncherPath` | string | Launcher executable path (if any) |
| `CmdlineArgs` | string | Command-line args passed on launch |
| `ManifestPath` | string | Launcher manifest path (e.g. Steam ACF) |
| `LastScanned` | ISO-8601 `DateTimeOffset` | When this entry was last produced by a scan |
| `LastModified` | ISO-8601 `DateTimeOffset` | Game folder's last modification time |
| `Extra` | object (string→string) | Platform-specific metadata (see §4) |
| `Tags` | array of string | User tags (F4). Additive merge with metadata tags |
| `UserOverrides` | object (string→string) | Fields the user manually set (F4); keys are field names (§3), values are ISO timestamps |
| `GameEngine` | `GameEngineKind` (int) | Detected engine; `Unknown` when no signal |
| `ExtraLaunchArguments` | string | F4 PCGW toggles / free text; used on exe launch only |

#### `GameEngineKind` values (stored as int)
`Unknown=0`, `UnrealEngine=1`, `Unity=2`, `Rage=3`, `Frostbite=4`, `Source=5`,
`Godot=6`, `CryEngine=7`.

---

## 3. `GameEntry.UserOverrides` — field-override keys

Keys are constants from `GameEntryFields`. When a key is present, the
corresponding field is user-set and **automated enrichment skips it** on rescan.
Values are ISO timestamps of when the override was set.

| Key | Field it signs |
|-----|----------------|
| `DisplayName` | Title is user-set / was auto-picked (merged title pin) |
| `ExecutablePath` | Exe is user-set; `ExeCandidates`/`ExeCandidateCount` are dropped |
| `LauncherPath` | Launcher path is user-set |
| `CommandLineArguments` | Launch args are user-set |
| `ExtraLaunchArguments` | F4 extras are user-set |
| `ManifestPath` | Manifest path is user-set |
| `GameSource` | Source type is user-set |
| `Tags` | Tags are user-set |

---

## 4. `GameEntry.Extra` (PlatformMetadata) — common keys

Platform metadata is a free-form string→string map. Keys are not part of the
fixed `GameEntry` schema, but several are treated specially for display/merge.
The authoritative list grows per-scanner; the shared/standard keys are:

| Key | Meaning | Notes |
|-----|---------|-------|
| `TitleSource` | Where the current title came from | Values: `GogInfo`, `EaInstallLog`, `EpicItemManifest`, `PeFileDescription`, `UbisoftReadme`, `FolderExeMatch`, `PcgwPick`, `UserOverride`. **Preserved through rescan** for `PcgwPick`/`UserOverride`. |
| `AutoDetectedTitle` | Title auto-detected from a store signal | Written alongside a `TitleSource` |
| `PeFileDescription` | Version-info description of the primary exe (from PE metadata) | Title candidate (E7) |
| `ExeCandidates` | Pipe-separated (`\|`) list of candidate exes | |
| `ExeCandidateCount` | Number of exe candidates | Dropped when exe user-set |
| `Studio` | Detected developer/publisher signal | |
| `SteamStatus` | Steam scan status | `Installed`, `Moved`, `Orphaned`, `Missing` |
| `SteamAppId` | Steam numeric AppID | |
| `AcfLibraryPath` | Physical library owning the ACF | |
| `AcfExpectedPath` | Where the game folder is expected (moved/missing context) | |
| `AcfFilePath`, `AcfSizeOnDisk`, `AcfBuildId`, `AcfStateFlags` | ACF metadata passthrough | |
| `FolderName` | Steam installdir | steam scanner |
| `ActualLibraryRoot` / `LibraryRoot` | Physical library the folder lives under | |
| `EpicStatus`, `EpicAppName`, `EpicCatalogItemId`, `EpicCatalogNamespace`, `EpicItemPath` | Epic scanner | |
| `GogGameId`, `EaGameName`, `BlizzardProduct`, `UbisoftReadme` | Store signals | |

Platform-specific full key sets are defined by their scanners
(`EpicLibraryScanner`, `SteamLibraryScanner`, `FolderScanner`).

---

## 5. `games_metadata.json` — offline metadata sidecar

Written by `MetadataStore` (`IMetadataStore`), keyed by `GameEntryId`. Never
touches `games.json`. Top-level shape:

| Property | Type | Meaning |
|----------|------|---------|
| `version` | int | Sidecar file version (currently `1`) |
| `entries` | object (gameId → **sidecar-entry object**) | One entry per game id |

Sidecar-entry object (`SidecarEntry`):

| Property | Type | Meaning |
|----------|------|---------|
| `merged` | **metadata-record object** | The merged record for the game |
| `sources` | object / null | Per-source raw records (not displayed) |

The `merged` record corresponds to `GameMetadataRecord`:

| Property | Type | Meaning |
|----------|------|---------|
| `GameEntryId` | string | Matches `GameEntry.Id` |
| `Developer` | string/null | |
| `Publisher` | string/null | |
| `ReleaseDate` | string/null | |
| `Genre` | string/null | |
| `Description` | string/null | |
| `Engine` | string/null | |
| `MetacriticScore` | int/null | |
| `SteamAppId` | string/null | |
| `GogGameId` | string/null | |
| `CoverArtUrl` | string/null | |
| `OfficialWebsite` | string/null | |
| `PcGamingWikiUrl` | string/null | |
| `LastMetadataSource` | string/null | |
| `LastUpdated` | ISO-8601 / null | Throttle key: within 60 days → no refetch |
| `Details` | object/null | PCGW operator extras (§4 below) |

### `Details` object (`GameMetadataDetails`)
| Property | Type | Meaning |
|----------|------|---------|
| `ConfigPaths` | array of **path objects** | Config-file/subdir templates |
| `SavePaths` | array of **path objects** | Save-data templates |
| `CommandLine` | array of **cmdline objects** | F4 toggle catalog (not user state) |
| `Fixes` | array of **fix objects** | Essential-improvements hints |
| `Video` | object (string→string) | Video links |
| `CloudSync` | object (string→string) | Cloud-sync/save locations |

**Path object** (`GameMetadataPath`): `Kind`, `Os`, `Template` (strings).
**Cmdline object** (`GameMetadataCommandLine`): `Argument`, `Notes`, `NeedsValue`, `Source`.
**Fix object** (`GameMetadataFix`): `Title`, `SuggestedArgs`, `SuggestedExecutable`.

---

## 6. Persistence invariants

- **Location:** only `{app-base}/data/`. Never write to game installs, the
  registry, Start Menu, or `%APPDATA%` outside the app folder.
- **Two-file split:** `libraries.json` holds anchors; `games.json` holds game
  entries (each linked to an anchor by `Library` name). They are **not** mixed.
- **JSON style:** indented (`WriteIndented`), property names case-insensitive on
  read. JSON property names use the DTO names shown (e.g. `CmdlineArgs`,
  `Override`, `Extra`), not `GameEntry` record parameter names.
- **Idempotent reads:** a missing/corrupt file returns defaults (empty), never throws.
- **Skip migration:** existing on-disk data from earlier versions is not migrated
  (work-in-progress).
- **Merge rule on rescan:** user overrides survive; scanned values fill the rest;
  `TitleSource` pinned to `PcgwPick`/`UserOverride` is preserved.
- **Steam aggregation:** one `Steam` anchor owns many physical folders; games are
  linked to the `Steam` anchor, not to any single folder.
