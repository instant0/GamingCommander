# Epic Games Store .item File Format

## Overview
The `.item` file is a JSON manifest that tells the Epic Games Launcher about an installed game. It lives in `%ProgramData%\Epic\EpicGamesLauncher\Data\Manifests\`.

## Working identification `.item` (Death Stranding → Epic **Update**, 2026-08-23)

User-confirmed file. Epic showed **Update**. Diff vs our earlier Python output:

| Must be | Value |
|---------|--------|
| `AppName` | **`Boga`** (`.manifest` `BogaStaging` stripped) — **not** `.ovt` `sub` `d460fdcb…` |
| `CatalogNamespace` | `.ovt` `f4a904…` |
| `CatalogItemId` | `.ovt` `761fe092…` — **not** GraphQL `offerId` / Director’s Cut |
| `DisplayName` | `Death Stranding` |
| `LaunchExecutable` | `DeathStranding.exe` (no leading `/`) |
| `MandatoryAppFolderName` | `DeathStranding` (leaf) |
| `AppVersionString` | **omit the key** (build string caused a GET ghost) |
| `MainGame*` / `PrereqIds` / `ExpectingDLCInstalled` | **omit** |
| Epic extras | present, empty: `EoshRevision`, `CompleteManifestPath`, `PendingManifestPath`, `SDMeta*`, `SidecarDeploymentId` |
| `InstallSessionId` | zeros |
| `OwnershipToken` | `"true"` |
| `AppCategories` | `public`, `games`, `applications` |
| `InstallSize` | `0` |

`tools/decode_manifest.py` `generate_item` now emits this shape.

---

## Generating .item from .manifest

### Scenario
User has: `Y:\Games\GameName\.egstore\*manifest`
User needs: `.item` file to register game in Epic Launcher

### Process
1. Parse `.manifest` file to get: AppName, BuildVersion, LaunchExecutable, InstallationGuid
2. Query Epic GraphQL API to get: CatalogNamespace, CatalogItemId, DisplayName
3. Generate `.item` with correct path format

**Verify with the Python tool before changing C#:**

```
python3 tools/decode_manifest.py "<game>\.egstore\<guid>.manifest" --item "e:\Games\Folder" --game "Death Stranding"
```

That is the recipe that produced a working identification `.item`. Do not invent a different field set in C# until this output is confirmed in the launcher.

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

## Seven games Epic lists as **Installed** (2026-08-23)

Base `.item` files only: Jotunnslayer, Dishonored DE, Dying Light 2 Reloaded, Tomb Raider GOTY, Civ VI, Three Kingdoms, Fortnite.  
(DLC rows exist too; they are `addons` and may have an empty `LaunchExecutable`.)

**Always set** on those installed base games — this is what “registered” looks like:

| Field | Notes |
|-------|--------|
| `DisplayName`, `AppName`, `CatalogNamespace`, `CatalogItemId` | This install’s ids |
| `InstallationGuid` | Matches `.egstore\{guid}.manifest` |
| `InstallLocation`, `ManifestLocation`, `StagingLocation` | Real folders |
| `MandatoryAppFolderName` | **Leaf only** (`DishonoredDE`, not a full path) |
| `CompleteManifestPath` | Points at that `.manifest` |
| `PendingManifestPath` | Present (file may or may not exist) |
| `EoshRevision` | Non-empty — **Epic writes this** |
| `InstallSize` | **> 0** |
| `AppVersionString` | Whatever Epic wrote (can be ugly) |
| `LaunchExecutable` | Relative exe (Civ = `2KLauncher/LauncherPatcher.exe`) |
| `AppCategories` / `TechnicalType` | `games` + `applications` (`public` **optional** — Civ/DL2 have it, DH/TR do not) |
| `OwnershipToken` | `"false"` |

**Blank on some installed games — not required:**

| Field | Empty on |
|-------|----------|
| `BaseURLs` | Dishonored, Tomb Raider |
| `ManifestHash` | Dishonored, Tomb Raider |
| `BuildLabel` | Dishonored, Tomb Raider |
| `HostInstallationGuid` | Dishonored, Tomb Raider |
| `InstallSessionId` | Dishonored = all zeros (still Installed) |
| `MainGame*` | **all seven** |
| `VaultThumbnailUrl` | all seven |
| `bCanRunOffline` | true or false |
| Most other bools / process lists | false / `[]` |

Identification `.item` can omit the “not required” column. It **cannot** become Installed until Epic sets `EoshRevision` + `InstallSize` + `CompleteManifestPath` (Update).

---

## Tomb Raider after Epic **Update** vs Three Kingdoms (both official, both 58 keys)

User generated an identification `.item`, then used **Update** in the launcher. Epic wrote `6C99E507….item`. That file **works**. It is **not** a clone of Three Kingdoms.

| Field | TR after Update (works) | Three Kingdoms (works) | Required to *be official*? |
|-------|-------------------------|------------------------|----------------------------|
| `EoshRevision` | set | set | **Yes — Epic writes this** |
| `CompleteManifestPath` | set, new guid `.manifest` | set | **Yes — Epic writes this** |
| `InstallSize` | real (~27 GB) | real | Epic writes this |
| `InstallSessionId` | real guid | real guid | Epic writes this |
| `AppVersionString` | long `TRAS_15112021_…` | `1.7.8` | Whatever Epic writes; can look ugly |
| `LaunchExecutable` | `TombRaider.exe` | `Launcher.exe` | Game-specific |
| `MandatoryAppFolderName` | `TombRaiderGOTYE` (leaf only) | `TotalWarTHREEKINGDOMS` | Leaf folder name |
| `BaseURLs` | **`[]`** | CDN list | **No** |
| `ManifestHash` | **`""`** | SHA1 | **No** |
| `BuildLabel` | **`""`** | `Live` | **No** |
| `HostInstallationGuid` | **`""`** | zeros | **No** |
| `bCanRunOffline` | `true` | `false` | Either works |
| Catalog ids | this product | that product | Must match **this** install |

So Three Kingdoms is **one** official shape, not the minimum. Update does **not** need CDN URLs, hash, or `Live`. It **does** need Epic to assign `EoshRevision` + complete manifest + size + session. We cannot mint those; Update does.

Our job: identification `.item` with **this install’s** `.ovt`/`.mancpn` ids + correct leaf folder + exe from `.manifest`. Then **one** Update. Do not overwrite those ids with GraphQL.

---

## Epic rewrite of our Python `.item` (Death Stranding, 2026-08-23)

**R:** `decode_manifest.py --item --game "Death Stranding"` output (2-space indent), **not** copied by us into ProgramData for this compare.  
**ProgramData:** same filename after the user dropped it in and Epic opened the library. Still **not registered as installed**.

Epic does **not** change catalog ids, exe, paths, display name, version, or flags. It **normalizes the schema** to its on-disk shape.

### Format / keys Epic adds (all empty except session id)

| Key | Our Python | After Epic | Notes |
|-----|------------|------------|--------|
| indent | 2 spaces | **tabs** | Cosmetic |
| `EoshRevision` | absent | `""` | Still empty — official installs have a value |
| `CompleteManifestPath` | absent | `""` | Official fills this; Epic did **not** fill it for us |
| `PendingManifestPath` | absent | `""` | Same |
| `SDMetaHash` | absent | `""` | |
| `SDMetaLocation` | absent | `""` | |
| `bSDMetaMigrated` | absent | `false` | |
| `SidecarDeploymentId` | absent | `""` | |
| `InstallSessionId` | `""` | `00000000…0000` | Epic writes zeros if we leave empty |

### Keys Epic **removes**

| Key | Our Python |
|-----|------------|
| `PrereqIds` | `[]` |
| `ExpectingDLCInstalled` | `{}` |

### Values Epic **does not touch**

`LaunchExecutable`, `InstallLocation`, `ManifestLocation`, `StagingLocation`, `DisplayName`, `CatalogNamespace`, `CatalogItemId`, `AppName`, `MainGame*`, `AppVersionString`, `AppCategories` (`public` kept), `TechnicalType`, `BuildLabel`, `OwnershipToken`, `VaultThumbnailUrl`, `bRequiresAuth`, `bCanRunOffline`, `InstallSize` (stays `0`).

### What this means

Epic’s **minimum on-disk schema** = our Python field set **plus** those eight extra keys (empty), **minus** `PrereqIds` / `ExpectingDLCInstalled`, tab indent, `InstallSessionId` = zeros.

That is **still not enough to register as installed**. Epic did **not** set `CompleteManifestPath` or `EoshRevision` even though `.egstore\0C1A9FF2….manifest` exists. Those two stay empty unless Epic itself owns the install (Update/Verify).

`MandatoryAppFolderName` is wrong in **both** files (`e:\Games\DeathStranding` instead of `DeathStranding`) — Linux `os.path.basename` on a `\` path. Fix in `decode_manifest.py` before the next Python regen. Unrelated to Epic’s rewrite.

### `.egstore` scraps vs the Python `.item` we wrote (R:)

| | On disk in `.egstore` | In our `.item` |
|--|----------------------|----------------|
| `.manifest` filename | `0C1A9FF2…` | `InstallationGuid` **same** |
| `.manifest` launch | `DeathStranding.exe` | **same** |
| `.manifest` app | `BogaStaging` → strip → `Boga` | `AppName` / `MainGameAppName` = **`Boga`** |
| `.manifest` build | `2742586.206.1` | `AppVersionString` **same** |
| `.ovt` JWT `namespace` | `f4a904fcef2447439c35c4e6457f3027` (**dev**) | **not used** |
| `.ovt` JWT `catalogItemId` | `761fe09295aa422e8199cebaacf51675` | **not used** |
| `.ovt` JWT `sub` / folder | `d460fdcbec4e42f295473e94e96fda11` | **not used** |
| GraphQL `searchStore("Death Stranding")` first hit | — | **Director’s Cut** public ns `0a9e3c5a…` / id `7253c099…` |

The `.ovt` filename is `namespace`+`catalogItemId` with no separator. Those leftovers match the **dev** product. **Do not replace them with GraphQL.**

GraphQL usage:

| Query | What happens |
|-------|----------------|
| `searchStore(keywords: "Death Stranding")` | See hit list below — **no** `BASE_GAME` named exactly the 2019/2020 original |
| `searchStore(namespace: "<ovt namespace>")` | For DS `f4a904…` returns **BogaDevAudience** — not a store title |

Keyword search **does** contain the string “Death Stranding” (2026-08-23, `count: 15`):

| offerType | title | namespace |
|-----------|--------|-----------|
| BASE_GAME | DEATH STRANDING DIRECTOR'S CUT | `0a9e3c5a…` |
| BASE_GAME | DEATH STRANDING 2: ON THE BEACH | `3dd83ff5…` |
| EDITION / ADD_ON / DLC | DS2 deluxe / upgrade / preorder | same as DS2 |
| **OTHERS** | **Death Stranding** | **`epic`** (not a game namespace) |

The launcher library tile is an **account entitlement**, not this `OTHERS`/`epic` row. There is **no** store `BASE_GAME` left for the original SKU. `count: 1` took Director’s Cut because that is the first real base game.

### Full GraphQL record for title `Death Stranding` / namespace `epic` (not two lines)

`searchStore` returns a **storefront pointer**, not a game install catalog:

```
title: Death Stranding
id: 9a071cfb3827440ba36cbef6a6042186
namespace: epic
offerType: OTHERS
status: ACTIVE
description: Death Stranding
productSlug: death-stranding/home
releaseDate: 2020-12-31T16:00:00.000Z
publisherDisplayName: null
developerDisplayName: null
items: [ { id: 83564e9426d44ce2bbbab6037b80354c, namespace: epic } ]
customAttributes:
  com.epicgames.app.offerNs  = f4a904fcef2447439c35c4e6457f3027   ← same as .ovt namespace
  com.epicgames.app.offerId  = 6093deee510e4adca8ec48b64528d344   ← store offer, ≠ .ovt catalogItemId
  com.epicgames.app.productSlug = death-stranding/home
  com.epicgames.app.weight = 5000
keyImages: DieselStoreFrontWide / Tall (Kojima / Death Stranding art)
```

So Graph **does** know this is Death Stranding, and it **points at** namespace `f4a904…` (the scraps). Public `searchStore(namespace: f4a904…)` still only lists **BogaDevAudience**. The playable offer `6093deee…` is **not** in that public list.

**Legendary / Heroic** (`derrod/legendary`) do **not** use this anonymous `searchStore` for library metadata. They use **logged-in** Epic account APIs (`legendary list` / entitlements). That is why they can see “Death Stranding” as an owned game. We have not called those APIs (need the user’s Epic login).

Identification `.item` must keep `.ovt` ids (`f4a904…` / `761fe092…` / `d460fdcb…`). GraphQL stub ids (`epic` / `9a071cfb…`) are the **store page**, not the install.

So when we already have `.ovt`/`.mancpn` ids, GraphQL must **not** change `CatalogNamespace` / `CatalogItemId` / `AppName`. It may only fill `DisplayName` / thumbnail if the returned title actually matches `--game`. Otherwise keep `--game` (here: “Death Stranding”).

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
| `.egstore/*.manifest` | Binary. Filename stem = `InstallationGuid`. `decode_manifest.py` works on some (Death Stranding → `DeathStranding.exe` + build). Fails on others. |
| `.egstore/*/*.ovt` | Epic ownership JWT. Payload `ent[0].namespace` + `catalogItemId`, `sub` = AppName. **May be a dev namespace** (Death Stranding `f4a904…`). Enough to regen an identification `.item` after `.mancpn` is deleted. |

If Write says “already exists”, check `{guid}.item` — a **hollow** leftover (no `CompleteManifestPath`, `InstallSize` 0, `bRequiresAuth`/`bCanRunOffline` false) is not “official”. Replace it. Set `CompleteManifestPath` to the remaining `.egstore\{guid}.manifest` and `bRequiresAuth`/`bCanRunOffline` **true** (first working regen had those true).

### Death Stranding (our regen, 2-space then tab) vs Three Kingdoms (official)

Same **58 keys**. Empty arrays / empty strings that TK also leaves empty are **not** why Epic ignores the file.

| Field | Ours (DS) | Official (Three Kingdoms) | Means |
|-------|-----------|---------------------------|--------|
| `AppCategories` / `TechnicalType` | includes **`public`** | `games, applications` only | `public` → store **GET** ghost |
| `AppVersionString` | EGL build `2742586.206.1` | store version `1.7.8` | That build string **is** the GET version tag |
| `CatalogNamespace` / `CatalogItemId` | `.ovt` **dev** `f4a904…` | public store ids | Not the same product as the INSTALL row |
| `EoshRevision` | empty | set | Launcher-only; we cannot invent |
| `InstallSize` | `0` | real bytes | Optional; 0 ≠ missing key |
| `ManifestHash` | empty | SHA1 of `.manifest` | **We can compute** |
| `BaseURLs` | `[]` | CDN list | Cannot invent |
| `BuildLabel` | empty | `Live` | Set `Live` |
| `PendingManifestPath` | set though **file missing** | set (Epic’s pending) | Only write if the Pending file exists |
| `LaunchExecutable` | `DeathStranding.exe` | `Launcher.exe` | Root exe, **no** leading `/` |

GraphQL `searchStore` would replace **dev** ids + marketing title. It still cannot fill `EoshRevision` / `BaseURLs` / real size. Do **not** put `public` or the raw manifest build in the `.item`.
| Local `.item` | Often **absent** on orphans. |

**Handle:**

1. Status **Orphaned** (folder exists, no matching ProgramData `InstallLocation`).
2. Identification `.item` (our VFS / optional write): ids from `.mancpn`, path = folder, exe from normal scan, display name from folder / PE / PCGW.
3. Launcher `.item`: `.mancpn` ids + folder + scanned exe was **accepted by Epic Launcher** (2026-08, no GraphQL). Still never invent UUIDs. Re-probe `searchStore` only when `.mancpn` is missing.
4. Do not wait for a DLC `.item` to appear — orphans often have **no** Manifests rows at all.

## Ours vs Epic after a launcher patch (2026-08)

Same title / `CatalogItemId` / install folder. Epic then wrote a **second** `.item` + new `.egstore\*.manifest` (new `InstallationGuid`). Catalog VFS must **dedupe by CatalogItemId**.

| Field | Our identification `.item` | Epic after patch | Action |
|-------|----------------------------|------------------|--------|
| `CatalogNamespace` / `CatalogItemId` / `AppName` | From `.mancpn` | Same | Keep |
| `DisplayName` / `InstallLocation` / `MandatoryAppFolderName` | Folder + `.mancpn` | Same | Keep |
| `LaunchExecutable` | `Binaries\Win64\Game.exe` | `/Binaries/Win64/Game.exe` | Prefer Epic style: **leading `/`, forward slashes** |
| `InstallationGuid` | Stem of **old** `.mancpn` / `.manifest` | **New** guid (new `.manifest`) | Do not treat ours as permanent |
| `CompleteManifestPath` / `PendingManifestPath` | empty | Points at `.egstore\{guid}.manifest` | Epic-only; we cannot invent a new manifest |
| `EoshRevision` | empty | Set | Launcher-internal; leave empty |
| `InstallSize` | `0` | Real byte size | Optional; `0` is fine for identification |
| `AppVersionString` | empty | e.g. `1.6` | From patch; leave empty unless we parse `.manifest` |
| `MainGame*` | We copied catalog ids | **Empty** on a base game | **Leave empty** (matches live files) |
| `BuildLabel` | `Live` | empty | Leave empty |
| `HostInstallationGuid` | zeros | empty | Leave empty |
| `OwnershipToken` | we used `"true"` | `"false"` | Use `"false"` unless we know otherwise |

Same pattern seen again on **Tomb Raider GOTY**: our `.item` used `crashpad_handler.exe` and size 0; Epic’s patch file uses `TombRaider.exe`, real `InstallSize`, `AppVersionString`, and a new guid. Never pick crashpad/crashreporter as `LaunchExecutable`.

**Enough for launcher to see the game:** catalog ids + install path + launch exe + categories.  
**Not enough to own updates:** Epic replaces guid / manifest paths / size / version on patch. Dedup; do not fight the new file.

## GraphQL vs “valid enough to survive Epic”

`tools/decode_manifest.py` + `searchStore` GraphQL **does** build a working **identification** `.item` (name, catalog ids, thumbnail). That is what we used before. It is **not** what Epic writes after Verify/Update.

| | GraphQL / `.mancpn` regen | Epic after Update/Verify |
|--|---------------------------|---------------------------|
| Catalog ids + display name | Yes | Yes |
| `LaunchExecutable` | From `.manifest` or folder scan | Official path |
| `CompleteManifestPath` / `PendingManifestPath` | empty | Required for *their* install record |
| `EoshRevision` | empty | Set (launcher EOS host) |
| `InstallSize` / `AppVersionString` | 0 / empty | From the download |
| Entitlement / “managed” install | No | Yes — Epic’s own database |

Launcher **accepted** our files (title appeared, “needs update”). After Tomb Raider’s official patch, Epic **deleted** identification-only `.item`s (and `.mancpn`) that had no `CompleteManifestPath` / `EoshRevision`. Official Dishonored / Tomb Raider files stayed.

We **cannot** invent `EoshRevision` or a new official `.manifest`. GraphQL does not provide those. Regen remains: show the game in **our** VFS; survive Epic’s next Verify only if Epic itself rewrites the `.item`.

## Testing

Copy to Windows:
```
data\DyingLight2.item → C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests\5A18D2F542FB88CFADEB65A438C006A8.item
```

Restart Epic Games Launcher.