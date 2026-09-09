# 126 — Facts: Shadowrun Dragonfall recovery record

Game: Shadowrun: Dragonfall - Director's Cut. Folder `D:\Games\Epic Games\ShadowrunDragonfall`. Install GUID `571690814DD33B38A9166E96DA8EEB76`.

## Local scraps (`D:\Games\Epic Games\ShadowrunDragonfall\.egstore`)

| Scrap | Value |
|---|---|
| `.mancpn` | AppName `5b41454974be4d5883056ba298e53675`, CatalogItemId `fd1d31227d1d405eb68a9a3bbdd029d0`, CatalogNamespace `805a1b31b65f4c3db221ab242f894482` |
| `.manifest` | FString[0] `810fd6e26e5c4c768ea1b707f31ce007` (NOT AppName), FString[1] `1.0.0`, FString[2] `Dragonfall.exe` |
| `Pending/` | empty |
| exes | `Dragonfall.exe` (launch); `ShadowrunEditor.exe` (editor, never) |

## GraphQL (documented query)

- keywords `Shadowrun Dragonfall` → 1 element: title "Shadowrun: Dragonfall - Director's Cut", store offer `id` `71489603554f452f9d71e93627115939` (≠ install CatalogItemId), ns `805a1b31…` ✓, slug `shadowrun-dragonfall/home`.
- ns query → same single element.

## catcache.bin cross-check

`id` `fd1d31227d1d405eb68a9a3bbdd029d0` = "Shadowrun: Dragonfall - Director's Cut" (exact CatalogItemId ✓), `releaseInfo[].appId` `5b41454974be4d5883056ba298e53675` (exact AppName ✓), `FolderName` `ShadowrunDragonfall`.

## Write-shape `.item` (regeneration reference)

All non-identity fields are the standard write-shape constants: empty strings for `EoshRevision`/`CompleteManifestPath`/`PendingManifestPath`/`ManifestHash`/`SDMeta*`/`SidecarDeploymentId`/`HostInstallationGuid`/`PrereqSHA1Hash`/`LastPrereqSucceededSHA1Hash`/`MainGame*`/`MainWindowProcessName`/`VaultThumbnailUrl`/`VaultTitleText`; zeros for `InstallSessionId`/`InstallSize`/`SidecarConfigRevision`/`PreloadState`; `[]` for BaseURLs/ChunkDbs/CompatibleApps/InstallTags/InstallComponents/ProcessNames/BackgroundProcessNames/IgnoredProcessNames/DlcProcessNames/AllowedUriEnvVars; booleans `false` for bIsIncompleteInstall/bIsManaged/bIsFab/bNeedsValidation/bSDMetaMigrated/bAllowMultipleInstances/bAllowUriCmdArgs/bLaunchElevated, `true` for bIsApplication/bIsExecutable/bRequiresAuth/bCanRunOffline; `BuildLabel Live`; `AppCategories ["games","applications"]`; `TechnicalType "games,applications"`; `OwnershipToken "false"`; `DisplayName "Shadowrun: Dragonfall - Director's Cut"`; `LaunchExecutable "Dragonfall.exe"`; `MandatoryAppFolderName "ShadowrunDragonfall"`; `ManifestLocation "d:\Games\Epic Games\ShadowrunDragonfall/.egstore"`; `StagingLocation "d:\Games\Epic Games\ShadowrunDragonfall\.egstore\bps"`; `InstallLocation "d:\Games\Epic Games\ShadowrunDragonfall"`.

Generated file: `/mnt/r/571690814DD33B38A9166E96DA8EEB76.item`.

## Source-per-field (regeneration if `.mancpn` gone)

| Field | Value | Recoverable from |
|---|---|---|
| AppName | `5b41454974be4d5883056ba298e53675` | this doc / catcache `releaseInfo.appId` / NEVER manifest FString[0] |
| CatalogItemId | `fd1d31227d1d405eb68a9a3bbdd029d0` | this doc / catcache `id` / NOT GraphQL `71489603…` |
| CatalogNamespace | `805a1b31b65f4c3db221ab242f894482` | this doc / GraphQL ns |
| LaunchExecutable | `Dragonfall.exe` | manifest FString[2] |
| AppVersionString | `1.0.0` | manifest FString[1] (Epic adds on Update) |
| InstallationGuid | `571690814DD33B38A9166E96DA8EEB76` | manifest/mancpn filename |
| DisplayName | Shadowrun: Dragonfall - Director's Cut | GraphQL |
| MandatoryAppFolderName | `ShadowrunDragonfall` | folder leaf / catcache FolderName |

## ORIGINAL (working, from other system) vs GENERATED (ours)

Identity identical: AppName, CatalogItemId, CatalogNamespace, InstallationGuid, LaunchExecutable, MandatoryAppFolderName.

Deltas (original → generated):
- `AppCategories`/`TechnicalType`: `public,games,applications` → `games,applications`
- `AppVersionString`: `1.0.0` → missing
- `MainGameAppName`: = AppName → `""`
- `BaseURLs`: 5 CDN urls → `[]`
- `InstallSize`: 7236472495 → 0
- `InstallSessionId`: real → zeros
- `PrereqIds`: `[]` → missing; `OwnershipTokenForNs` `"false"` → missing
- `bAllowMultipleInstances`: true → false
- `HostInstallationGuid`: zeros → `""`
- `StagingLocation`: forward `/bps` → backslash `\bps`
- EOSH/Complete/Pending: absent (pre-EOSH era) → `""` present

Proven not to matter: original was removed identically by EOSH (2026-09-09 01:40 test).

## Status

Registration on this account: **impossible via `.item`** — EOSH does not report Shadowrun for this account. File survives only where the account owns the game and an Epic-side install registered it.