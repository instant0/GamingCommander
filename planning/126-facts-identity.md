# 126 — Facts: Item Identity (The Captain + Death Stranding + Jotunnslayer)

## Working The Captain `.item` (`DA0CECC9….item`, Epic post-reinstall)

| Field | Value | Source |
|---|---|---|
| `AppName` | `f00b5acabb0f49a7b49e8350d1cd0de0` | original `.mancpn` |
| `CatalogItemId` | `450d325cc72e4fcbb5999aaf0f672e10` | original `.mancpn` |
| `CatalogNamespace` | `301980d060324bb29202677f468c6c44` | `.mancpn`, GraphQL ✓ |
| `InstallationGuid` | `DA0CECC94D1365835DE331DC43B441DE` | new manifest filename (changed after reinstall) |
| `LaunchExecutable` | `TheCaptain.exe` | manifest FString[2] |
| `DisplayName` | The Captain | GraphQL |
| `MandatoryAppFolderName` | TheCaptain | folder leaf |
| `AppVersionString` | 1.1.4_win | Epic-added (from manifest FString[1]) |
| `EoshRevision` | `08DEBEDB31C8609B000000000000002A` | Epic-added |
| `AppCategories`/`TechnicalType` | `games,applications` | no `public` |
| `OwnershipToken` | `false` | |
| `BuildLabel` | `""` | Epic blanked |

New manifest `DA0CECC9….manifest` FStrings identical to original: `[0] 17aff4bb5b5841ac9cc2ec7b23daf58b`, `[1] 1.1.4_win`, `[2] TheCaptain.exe`. Only the GUID filename changed.

## The Captain — attempt history (all removed by EOSH)

| Attempt | AppName | CatalogItemId | Result |
|---|---|---|---|
| 1 | `17aff4bb…` (FString) | `450d325c…` (.mancpn) ✓ | removed |
| 2 | `f00b5aca…` (.mancpn) ✓ | `450d325c…` ✓ | **removed** |
| 3 | `f00b5aca…` ✓ | `450d325c…` ✓ | removed |
| 4 | `17aff4bb…` | `80fb2028…` (GraphQL) ✗ | removed |

Attempts 2–3 = exact correct identity = still deleted.

## AppName source per format era

| Title | manifest FString[0] | accepted AppName | rule that worked |
|---|---|---|---|
| Death Stranding | `BogaStaging` | `Boga` | FString[0] strip `Staging` (old `.ovt` install, no `.mancpn`) |
| Jotunnslayer | `05cc0386…` | `05cc0386…` | FString[0] == `.mancpn` AppName (coincidence) |
| The Captain | `17aff4bb…` | `f00b5aca…` | `.mancpn` AppName; FString[0] is a different id |
| Shadowrun | `810fd6e2…` | `5b414549…` | `.mancpn` AppName; FString[0] is a different id |

## `.mancpn` field redundancy (if `.mancpn` deleted)

| Field | `.manifest` | filename | GraphQL | verdict |
|---|---|---|---|---|
| `CatalogNamespace` | no | no | **yes** (keyword search → element namespace) | substitutable |
| `CatalogItemId` | no | no | no (store offer id ≠ install id) | irreplaceable (`.ovt` filename is only other carrier, old format) |
| `AppName` | only old `.ovt` installs (DS) | no | no (store has no AppName) | irreplaceable |

## What GraphQL gives vs local scraps

- Gives: `DisplayName` (title), `namespace` (corroborates `.mancpn`), metadata (dev/publisher/date/slug/images).
- Never gives: install `CatalogItemId` (store offer `id` is a different id in every title examined), `AppName` (no concept).
- catcache.bin (launcher catalog cache, 1043 entries, base64 JSON) **does** contain install-level ids: `id` = CatalogItemId, `releaseInfo[].appId` = AppName, `customAttributes.FolderName` = MandatoryAppFolderName.