# Epic Games Store .item File Format

## Overview
The `.item` file is a JSON manifest that tells the Epic Games Launcher about an installed game. It lives in `%ProgramData%\Epic\EpicGamesLauncher\Data\Manifests\`.

## Generating .item from .manifest

### Scenario
User has: `Y:\Games\GameName\.egstore\*manifest`
User needs: `.item` file to register game in Epic Launcher

### Process
1. Parse `.manifest` file to get: AppName, BuildVersion, LaunchExecutable, InstallationGuid
2. Query Epic GraphQL API to get: CatalogNamespace, CatalogItemId, DisplayName
3. Generate `.item` with correct path format

### Path Format (Critical!)
```
InstallLocation:     y:\games\GameName
ManifestLocation:  y:\games\GameName/.egstore
StagingLocation:    y:\games\GameName/.egstore/bps
```
- Drive letter: lowercase
- Path separators: backslash for drive, forward slash for subfolders
- Case: preserve original

## Required Fields

| Field | Source | Notes |
|-------|--------|-------|
| AppName | .manifest | Strip "Staging" suffix |
| AppVersionString | .manifest | Build version |
| LaunchExecutable | .manifest | Relative path |
| InstallationGuid | .manifest filename | |
| CatalogNamespace | Epic API | e.g., `87b7846d2eba4bc49eead0854323aba8` |
| CatalogItemId | Epic API | e.g., `ec549727d7084801a2ff7f63eb0e5459` |
| DisplayName | Epic API | |
| MainGameCatalogNamespace | Same as CatalogNamespace | |
| MainGameCatalogItemId | Same as CatalogItemId | |

## Epic GraphQL API

Endpoint: `https://store.epicgames.com/graphql`

```python
import requests
headers = {"Content-Type": "application/json"}
query = """{ Catalog { searchStore(start: 0, count: 1, keywords: "GAME NAME") { elements { title id namespace } } } }"""
resp = requests.post(endpoint, json={"query": query}, headers=headers)
```

## Test .item File

Location: `testdata/samples/DyingLight2.item`

```json
{
  "FormatVersion": 0,
  "bIsIncompleteInstall": false,
  "LaunchCommand": "",
  "LaunchExecutable": "ph/work/bin/x64/DyingLightGame_x64_rwdi.exe",
  "ManifestLocation": "y:\\games\\DyingLight2StayHuman/.egstore",
  "ManifestHash": "",
  "bIsApplication": true,
  "bIsExecutable": true,
  "bIsManaged": false,
  "bNeedsValidation": false,
  "bRequiresAuth": true,
  "bAllowMultipleInstances": false,
  "bCanRunOffline": true,
  "bAllowUriCmdArgs": false,
  "bLaunchElevated": false,
  "BaseURLs": [],
  "BuildLabel": "Live",
  "AppCategories": ["public", "games", "applications"],
  "ChunkDbs": [],
  "CompatibleApps": [],
  "DisplayName": "Dying Light 2 Stay Human - Reloaded Edition",
  "InstallationGuid": "5A18D2F542FB88CFADEB65A438C006A8",
  "InstallLocation": "y:\\games\\DyingLight2StayHuman",
  "InstallSessionId": "",
  "InstallTags": [],
  "InstallComponents": [],
  "HostInstallationGuid": "00000000000000000000000000000000",
  "PrereqIds": [],
  "PrereqSHA1Hash": "",
  "LastPrereqSucceededSHA1Hash": "",
  "StagingLocation": "y:\\games\\DyingLight2StayHuman/.egstore/bps",
  "TechnicalType": "public,games,applications",
  "VaultThumbnailUrl": "",
  "VaultTitleText": "",
  "InstallSize": 0,
  "MainWindowProcessName": "",
  "ProcessNames": [],
  "BackgroundProcessNames": [],
  "IgnoredProcessNames": [],
  "DlcProcessNames": [],
  "ExpectingDLCInstalled": {},
  "MandatoryAppFolderName": "DyingLight2StayHuman",
  "OwnershipToken": "true",
  "SidecarConfigRevision": 0,
  "PreloadState": 0,
  "CatalogNamespace": "87b7846d2eba4bc49eead0854323aba8",
  "CatalogItemId": "ec549727d7084801a2ff7f63eb0e5459",
  "AppName": "Redstart",
  "AppVersionString": "1.25.2_5972288_cert",
  "MainGameCatalogNamespace": "87b7846d2eba4bc49eead0854323aba8",
  "MainGameCatalogItemId": "ec549727d7084801a2ff7f63eb0e5459",
  "MainGameAppName": "Redstart",
  "AllowedUriEnvVars": []
}
```

## Base game vs DLC (live ProgramData, 2026-08)

Sample `.item` files in the research dump set `MainGameAppName` / `MainGameCatalogItemId` to the base game. **Live launcher files on this machine left those three `MainGame*` fields empty** — including DLC. Do **not** use `MainGame*` to find the base `.item`.

| | Base / playable | DLC / addon |
|---|---|---|
| `AppCategories` / `TechnicalType` | contains `games` (often `public`) | **`addons`** |
| `bIsApplication` / `bIsExecutable` | true | **false** |
| `LaunchExecutable` | set | **empty** |
| `CatalogNamespace` | product family | **same as base** |
| `CatalogItemId` / `AppName` | base product | **this addon** |
| `ExpectingDLCInstalled` | on the **base** `.item` only (`namespace:itemId:appName`) | empty |

**VFS:** one row per **base** `.item` (`games` + launch exe). Skip `addons` and empty `LaunchExecutable`. Exception: some extras (e.g. editor/tools) are `games` + an exe — not the main title; do not treat as the only catalog row for that `InstallLocation` if a `public,games` sibling exists.

**DLC does not reconstruct a missing base `.item`.** Same namespace only. The map of DLC → base is on the **base** file (`ExpectingDLCInstalled`), which is the file that is gone.

## Orphan folder (`.egstore`, no ProgramData `.item`)

Detected as Epic by `.egstore` / `.egsstore` (Plan 109), not by Manifests.

Typical local files:

| File | Use |
|------|-----|
| `.egstore/*.mancpn` | `CatalogNamespace`, `CatalogItemId`, `AppName` — enough for an **identification** `.item`. Namespace may be **dev** (§7.4 in `docs/EPIC-MANIFEST-ENRICHMENT.md`). |
| `.egstore/*.manifest` | Binary. Filename stem = `InstallationGuid`. `tools/decode_manifest.py` can fail on some builds. |
| Local `.item` | Often **absent** on orphans. |

**Handle:**

1. Status **Orphaned** (folder exists, no matching ProgramData `InstallLocation`).
2. Identification `.item` (our VFS / optional write): ids from `.mancpn`, path = folder, exe from normal scan, display name from folder / PE / PCGW.
3. Launcher `.item`: `.mancpn` ids + folder + scanned exe was **accepted by Epic Launcher** (2026-08, no GraphQL). Still never invent UUIDs. Re-probe `searchStore` only when `.mancpn` is missing.
4. Do not wait for a DLC `.item` to appear — orphans often have **no** Manifests rows at all.

## Testing

Copy to Windows:
```
data\DyingLight2.item → C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests\5A18D2F542FB88CFADEB65A438C006A8.item
```

Restart Epic Games Launcher.