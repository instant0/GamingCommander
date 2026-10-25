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
The entry is composed of nested typed objects (Plan 127 A1). Each nested object is a
record; the flat field names are exposed as read-only accessors in code.

| Property | Type | Meaning |
|----------|------|---------|
| `Identity` | **IdentityInfo** | `Id`, `Library` (anchor), `DisplayName`, `GameSource`, `IsSourceOverridden`, `IdentityKeys[]` |
| `Location` | **LocationInfo** | `FolderPath`, `FolderName`, `LibraryRoot`, `ActualLibraryRoot`, `GameFolder`, `PreviousLocations[]` |
| `Launch` | **LaunchInfo** | `ExecutablePath`, `LauncherPath`, `CommandLineArguments`, `ExtraLaunchArguments`, `ManifestPath`, `Profiles[]` (`LaunchProfile`: Name, ExecutablePath, Arguments, Environment, WorkingDirectory, RunAsAdmin, Engine — Plan 127 B10) |
| `Tags` | **Tag[]** | Typed tags `{ Name, Kind (User/Store/Engine/Genre), Color, Auto }` — Plan 127 B13 |
| `UserOverrides` | object (string→string) | Fields the user manually set (F4); keys are field names (§3), values are ISO timestamps |
| `GameEngine` | `GameEngineKind` (int) | Detected engine; `Unknown` when no signal |
| `LastScanned` | ISO-8601 `DateTimeOffset` | When this entry was last produced by a scan |
| `LastModified` | ISO-8601 `DateTimeOffset` | Game folder's last modification time |
| `Detection` | **DetectionInfo** | Detection provenance — tier, title source, signals, candidate exes (§4) |
| `Store` | **StoreInfo** | Typed store identifiers — Steam/GOG/Epic ids (§4) |
| `Acf` | **SteamAcfInfo** | Steam ACF status/paths/build (§4) |
| `Epic` | **EpicInfo** | Epic status + `.item` path (§4) |
| `Install` | **InstallInfo** | Unified `State` (`InstallState` int), `Version`, `UpdateAvailable`, `LastVerified` (Plan 127 B8) |
| `Tracking` | **TrackingInfo** | `FirstSeen`, `LastPlayed`, `LaunchCount`, `PlayTimeSeconds`, `LastSessionSeconds` (Plan 127 B7) |
| `UserState` | **UserStateInfo** | `Favourite`, `Hidden`, `Pinned`, `Archived`, `Rating`, `Notes`, `Completion` (`CompletionState` int) (Plan 127 B9) |
| `Relations` | **RelationsInfo** | `Kind` (`GameKind` int: Game/Dlc/Tool/Demo/Soundtrack/Mod), `ParentId`, `AddOnIds[]` (Plan 127 B11) |

`Identity.Id` is the deterministic ID: `first 16 hex of MD5("{physicalLibraryRoot\|folderName}")`,
lowercase. `physicalLibraryRoot` is the **physical library folder being scanned** (e.g.
`d:\steamlibrary` — NOT the anchor name); `folderName` is the game folder name (Epic catalog scans
key on the Manifests folder + catalog item id/folder). Anchor name plays no part — see §6.
`Location.FolderPath` equals the anchor's sole folder ONLY for 1:1 Standalone anchors; for
virtual/multi-folder anchors it is the real game location, not the anchor.

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

## 4. Typed metadata records (Detection / Store / Acf / Epic / Location)

The former free-form `Extra` (`PlatformMetadata`) string bag was **removed** in Plan 127 A2.
Store/detection data is now stored in typed records on the game entry.

### `Detection` (`DetectionInfo`)
| Property | Type | Meaning |
|----------|------|---------|
| `Tier` | `IdentificationTier` (int) | `Unknown=0`, `Candidate=1`, `Secure=2`, `Locked=3` |
| `TitleSource` | string | Where the title came from (see `TitleSourceValues`); preserved through rescan for `PcgwPick`/`UserOverride` |
| `AutoDetectedTitle` | string | The title replaced when enrichment set `TitleSource` |
| `PeFileDescription` | string | PE version-info description of the primary exe (title candidate, E7) |
| `ManifestKind` | string | `Acf`, `EpicItem`, `GogInfo`, or empty |
| `Signals` | string[] | Signal ids that produced the entry |
| `CandidateExes` | string[] | Candidate exe file names (dropped when exe user-set) |
| `IdentifiedAt` | ISO-8601 / null | When detection ran |

### `Store` (`StoreInfo`)
`SteamAppId`, `GogGameId`, `EpicCatalogNamespace`, `EpicCatalogItemId`, `EpicAppName`, `Studio`, `EaGameName`, `BlizzardProduct`, `UbisoftReadme`.

### `Acf` (`SteamAcfInfo`)
`Status` (`Installed`/`Moved`/`Missing`/`Orphaned`), `FilePath`, `LibraryPath`, `ExpectedPath`, `BuildId`, `SizeOnDisk`, `StateFlags`.

### `Epic` (`EpicInfo`)
`Status` (`Installed`/`Missing`/`Orphaned`), `ItemPath`.

### `Location` (`LocationInfo`)
`LibraryRoot`, `ActualLibraryRoot`, `GameFolder`, `FolderName` (Steam installdir).

---

## 5. `games_metadata.json` — offline metadata sidecar

Written by `MetadataStore` (`IMetadataStore`), keyed by `GameEntryId`. Never
touches `games.json`.

**Ownership (Plan 127 A6):** `games.json` owns identity, store ids, engine, launch, and user state
and is **authoritative**. This sidecar owns **fetched enrichment only** — it is never authoritative
for identity/store-id/engine. The `Fetched*` fields below are hints discovered from PCGW/Steam and
may differ from (or exist without) the detected values on `GameEntry` (`Store` / `GameEngine`).

Top-level shape:

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
| `FetchedEngine` | string/null | Fetched (PCGW). Not authoritative — detected engine is `GameEntry.GameEngine` |
| `MetacriticScore` | int/null | |
| `FetchedSteamAppId` | string/null | Fetched (PCGW/Steam). Not authoritative — detected id is `GameEntry.Store.SteamAppId` |
| `FetchedGogGameId` | string/null | Fetched (PCGW). Not authoritative — detected id is `GameEntry.Store.GogGameId` |
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

## 5b. `collections.json` — named collections (Plan 127 B12)

Written by `CollectionsDatabaseService` (`ICollectionsService`). Shape:

| Property | Type | Meaning |
|----------|------|---------|
| `version` | int | Schema version (`SchemaVersioning.Current`) |
| `collections` | object (name → **collection object**) | One entry per collection |

Collection object:

| Property | Type | Meaning |
|----------|------|---------|
| `gameIds` | string[] | Explicitly included game ids (manual playlists) |
| `smartQuery` | string/null | Filter query (F8 syntax); when set the collection is dynamic |

---

## 6. Persistence invariants

- **Location:** only `{app-base}/data/`. Never write to game installs, the
  registry, Start Menu, or `%APPDATA%` outside the app folder.
- **Two-file split:** `libraries.json` holds anchors; `games.json` holds game
  entries (each linked to an anchor by `Library` name). They are **not** mixed.
- **JSON style:** indented (`WriteIndented`), property names case-insensitive on
  read. JSON property names match the record/DTO property names shown in this document
  (`Identity`, `Location`, `Launch`, `Detection`, `Store`, `Acf`, `Epic`).
- **Idempotent reads:** a missing/corrupt file returns defaults (empty), never throws.
- **Schema versioning (Plan 127 A4):** every file carries a `version` field written as
  `SchemaVersioning.Current` (Core). On read, a file from a **newer** build is rejected (returned as
  defaults) rather than silently misread. No migrations exist yet (nothing shipped); a future
  breaking change bumps `Current` and adds an upgrade step in that file's load path.
- **Merge rule on rescan:** user overrides survive; scanned values fill the rest;
  `TitleSource` pinned to `PcgwPick`/`UserOverride` is preserved.
- **Steam aggregation:** one `Steam` anchor owns many physical folders; games are
  linked to the `Steam` anchor, not to any single folder.
- **Identity is physical, not anchor-based:** `GameEntry.Id` derives from
  `MD5("{physicalLibraryRoot|folderName}")` — the anchor (`Library`) name is
  **metadata, not identity**. IDs never change when a game is re-anchored
  (e.g. an Epic game relinked from `d:\games` to `EPIC`), which keeps
  `games_metadata.json` sidecars attached. Two physically identical folder
  names under two different library roots therefore have distinct IDs.
- **Identity reconciliation on external move (Plan 127 A5):** when a scanned game no longer
  matches an existing id, it is re-linked to an existing entry that shares an `IdentityKeys`
  entry — a store id (strong key; matches regardless of disk) or a folder name (weak key; matches
  only when the old folder is gone). The existing `Id` and sidecar are preserved, the new path
  becomes `FolderPath`, and the old path is appended to `Location.PreviousLocations`. Ambiguous
  folder-name matches are never guessed. (Cross-library moves are not yet reconciled.)
