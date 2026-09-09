# 126 — Facts: The EOSH Gate (Shadowrun rejection evidence)

## Rejection log line (every run)

Log: `C:\Users\Malware\AppData\Local\EpicGamesLauncher\Saved\Logs\EpicGamesLauncher-backup-*.log`

```
LoadItemFiles: Loaded item file .../571690814DD33B38A9166E96DA8EEB76.item
Manifest Check: 805a1b31b65f4c3db221ab242f894482:fd1d31227d1d405eb68a9a3bbdd029d0:5b41454974be4d5883056ba298e53675:1.0.0
Found Installed Manifest ... (same triple)
Keeping discovered manifest
Adding product / AddProductImplNoNotify (ns 805a1b31…) / AddSocialApplicationViewModel apps 1
EoshInstallationsChanged: Received new revision '08DEBEDB31C8609B000000000000002A'
EOSH reconciliation: removing installation not reported by EOSH '.../57169081….item'
  (InstallationId 571690814DD33B38A9166E96DA8EEB76, AppId 805a1b31…:fd1d3122…:5b414549…)
UProductInfoJSBridge::ShouldBlockPrePurchaseInstall: no library data yet for 805a1b31…:fd1d3122…:5b414549…
```

Sequence = file loaded → content validated (same messages as 21 accepted files) → product added to repo → **EOSH reconciliation deletes it** → file physically removed from `Manifests\`.

## Decisive experiment (2026-09-09 01:40)

ORIGINAL working Shadowrun `.item` (from system where it worked; `AppVersionString 1.0.0`, real size/session/CDN) copied as `shadowrun.item` → **same removal** (`Manifest Check: …:1.0.0` proves it was the original file). Content is not the variable.

## The Captain control

- 4 re-link attempts, all correct-identity variants → all removed by EOSH.
- Post-reinstall (manual Epic install) `DA0CECC9….item` → appears in EOSH `get-installed-apps-response` with `"isOwnershipCacheOwned":true` → **never removed**.

## EOSH ownership cache (`get-installed-apps-response`, feature `productinfo`)

- 22 entries = exactly the 22 `.item` files in `Data\Manifests\`. All `isOwnershipCacheOwned:true`, `isInstalled:true`.
- Entry fields: `namespace`, `catalogId`, `appName`, `isInstalled`, `installedVersion`, `hasUpdate`, `supportsCloudSave`, `lastBuildUpdateTime`, `installSizeOnDisk`, `lastCloudSync`, `isOwnershipCacheOwned`, `installLocation`, ~20 status booleans.
- Shadowrun absent → only reason for removal.

## EoshRevision stamps (all items)

| suffix | count | files |
|---|---|---|
| `…000C` | 12 | Jotunnslayer, Kinglet×9 + base, Three Kingdoms |
| `…001E` | 3 | DL2 base + BloodyTies + DevTools |
| `…0020` | 1 | Dishonored |
| `…0022` | 1 | Tomb Raider |
| `…0024` | 1 | Death Stranding |
| `…0028` | 3 | Fortnite ×3 |
| `…002A` | 1 | The Captain reinstall |
| `""` | ours | Shadowrun, captain.item |

Generation: monotonically increasing server-issued counter (`…0028`→`…0029`→`…002A` observed in logs). Items carry revision at last EOSH registration/update. Not locally generatable.

## Where the state lives

- **EOSH reported-install registry (the gate):** `C:\ProgramData\Epic\EpicOnlineServicesShared\InstallHelper\InstalledItems\` — **one `.egi` JSON file per reported install**, plain local text. **22 `.egi` files = the 22 accepted games. NO `.egi` for Shadowrun.**
- EOSH service: `C:\ProgramData\Epic\EpicOnlineServices\` (MainService\startup-config.json → account-public-service-prod03.ol.epicgames.com, client `7a40f8cd…`; InstallHelper\InstalledItems\Revision.json `{"number":870}` = 0x366 = service.egi stamp; ManifestCache; overlay/service/support.egi — EOSH's own artifacts only).
- Launcher: `Data\Manifests\*.item` (EoshRevision), `Data\Catalog\catcache.bin` (catalog, not ownership), `Data\DownloadManager\`.
- Authoritative ownership = server-side per-account; no local ownership file is writable.

## Per-game `.egi` format — FIELD REFERENCE (analyzed across all 22 files)

Location: `C:\ProgramData\Epic\EpicOnlineServicesShared\InstallHelper\InstalledItems\{InstallationGuid}.egi`
One file per reported install. Plain JSON, single top-level object `{"v4":{…}}`.

### Field semantics and value ranges (from 22/22 actual files)

| Field | Type | Present | Meaning | Values seen / format |
|---|---|---|---|---|
| `installationId` | string | 22/22 | EOSH's identifier for this install | 32-hex GUID, equals `.item` filename base, equals `InstallationGuid` |
| `state` | string | 22/22 | install lifecycle state | only `"Installed"` observed (other states exist in EOSH API: Downloading/Staged/Paused/etc.) |
| `revision` | string | 22/22 | EOSH revision stamp | `08DEBEDB31C8609B` + 16 hex counter; observed `…000C, 001E, 0020, 0022, 0024, 0028, 002A`; current = `002A`; equals `.item` `EoshRevision` |
| `dir` | string | 22/22 | game install root | absolute Windows path = `.item` `InstallLocation` (case preserved) |
| `metaDir` | string | 22/22 | `.egstore` folder | `{dir}\.egstore` |
| `manifestPath` | string | 22/22 | complete binary manifest location | `{metaDir}\{InstallationGuid}.manifest` |
| `pendingManifestPath` | string | 22/22 | staged/next manifest location | `{metaDir}\Pending\{InstallationGuid}.manifest` (file need not exist yet) |
| `platform` | string | 22/22 | target platform | `"Windows"` |
| `sandboxId` | string | 22/22 | namespace | 32-hex GUID (`d8a39ea2…`, `f4a904fc…`) or short (`fn`) = `CatalogNamespace` |
| `itemId` | string | 22/22 | catalog item id | 32-hex GUID always = `CatalogItemId` |
| `artifactId` | string | 22/22 | app name | 32-hex GUID OR short slug (`Boga`, `Fortnite`, `Kinglet`, `Redstart`) = `AppName` |
| `tags` | list | 22/22 | install tags (selective installs) | `[]` for all games EXCEPT Fortnite (large chunk-tag list) and its content packs |
| `pendingTags` | list | 22/22 | staged tags | always `[]` |
| `stagedVersion` | string | 22/22 | staged build version | always `""` |
| `manifestData` | object | 22/22 | current build info | keys: `version`, `launchCommand`, `launchExe`, `prereqSHA1Hash`, `buildSize` |
| `manifestData.version` | string | — | build version | e.g. `1.1.3.91851`, `2742586.206.1`, `TRAS_15112021_…` = `AppVersionString` |
| `manifestData.launchCommand` | string | — | extra launch args | `""` everywhere observed |
| `manifestData.launchExe` | string | — | exe to launch | `Jotunnslayer.exe`, `DeathStranding.exe`, `TheCaptain.exe`, `TombRaider.exe`, … = `LaunchExecutable`; empty for DLC/content |
| `manifestData.prereqSHA1Hash` | string | — | prereq installer hash | `""` everywhere observed |
| `manifestData.buildSize` | integer | — | on-disk size in bytes | observed 233 … 127,734,757,925 = `InstallSize` |
| `pendingManifestData` | object | 22/22 | staged build info | all five keys empty/0 in all 22 |
| `manifestUris` | list-or-null | 10/22 | CDN manifest download URLs | 3 URLs when present: `egs-cloudfront-chunks.epicgamescdn.com`, `epicgames-download1.akamaized.net`, `egdownload.fastly-edge.com`; **absent (null) in 12/22** — not required for report/keep |

### Notes
- `manifestUris` is **optional** (12 of 22 lack it) — presence is tied to which install flow wrote the file, not to being kept.
- `revision` == the `.item`'s `EoshRevision` value, 1:1.
- Shared `Revision.json` in the same folder: `{"number":42}` = `0x2A` = current revision suffix; the per-item stamps are derived from this counter.
- File timestamps == the install/update event that wrote it (DS 08-23 14:31, Tomb Raider 08-23 13:07, The Captain 09-09 02:07).

### Shadowrun requirement
Shadowrun has **no `.egi`** → EOSH reports nothing → `.item` deleted. To register locally, create `57169081….egi`:
`installationId` `57169081…`, `state "Installed"`, `revision` `08DEBEDB31C8609B000000000000002A` (current), `dir` `D:\Games\Epic Games\ShadowrunDragonfall`, `metaDir` `…\.egstore`, `manifestPath` `…\57169081….manifest`, `pendingManifestPath` `…\Pending\57169081….manifest`, `platform "Windows"`, `sandboxId` `805a1b31b65f4c3db221ab242f894482`, `itemId` `fd1d31227d1d405eb68a9a3bbdd029d0`, `artifactId` `5b41454974be4d5883056ba298e53675`, `tags []`, `pendingTags []`, `stagedVersion ""`, `manifestData {version "1.0.0", launchCommand "", launchExe "Dragonfall.exe", prereqSHA1Hash "", buildSize <real>}`, `pendingManifestData {…empty}`, `manifestUris` null; bump shared `Revision.json` number to 43. **Open question: whether EOSH trusts this local file or validates against the account service (server-side) — test required.**

### TEST RESULT (2026-09-09 01:57) — the local `.egi` does NOT register the game

Created `57169081….egi` (revision `…002B`) + bumped shared `Revision.json` to 43. Result:
- Launcher **received the bumped revision** (`EoshInstallationsChanged: Received new revision '…002B'`) — the counter edit was honored.
- Shadowrun `.item` **still removed** (`EOSH reconciliation: removing installation not reported by EOSH`, same message).
- EOSH MainService log shows InstallHelper running `enumerate` against `--installationdbdir="C:\ProgramData\Epic\EpicOnlineServices\InstallHelper\InstalledItems"` — **NOT** the `EpicOnlineServicesShared` path where the game `.egi`s live.

**Conclusion:** the Shared `.egi` files are **output/mirror records written by EOSH**, not an input registry it reads. "Reported" membership comes from EOSH's own installation-API state (its DB at `C:\ProgramData\Epic\EpicOnlineServices\InstallHelper\InstalledItems`), which requires an actual install through the EOSH installation API. **A locally-planted `.egi` is ignored; the reported list cannot be forged by dropping files.** The EoshRevision counter is local-derivable, but membership is not.

### 8j. DEFINITIVE PROOF — Ruiner reinstall (2026-09-09 02:05–02:09)

User reinstalled Ruiner through the launcher. Result: complete working set written by Epic — new GUID `B6EE687984DC9E96C204A5EA81A412CF` (was `8BAACAD8…`), `EoshRevision 08DEBEDB31C8609B000000000000002D` (next counter value), matching `.item` + `.egi` both written.

**Identity triple: identical to the original working file.** Original (`/mnt/r/8BAACAD8….item`, 2024, old format) vs new (Epic-written, B6EE6879):

| Field | Original (working system) | NEW (Epic-written, this machine) |
|---|---|---|
| `AppName` | `Laridae` | `Laridae` ✓ |
| `CatalogItemId` | `682122e968804007beadd755c79db11b` | same ✓ |
| `CatalogNamespace` | `74d82b28ab424956ac24407229fe6faa` | same ✓ |
| `LaunchExecutable` | `Ruiner.exe` | `Ruiner.exe` ✓ |
| `AppVersionString` | `1.0.4` | `1.0.4` ✓ |
| `InstallSize` | 11341411523 | 11341411523 ✓ |
| `AppCategories` | public,games,applications | public,games,applications ✓ |
| `InstallationGuid` | `8BAACAD8…` | **`B6EE6879…` (new)** |
| `EoshRevision` | absent (pre-EOSH) | `…002D` (stamped) |
| `CompleteManifestPath` | absent | set → new manifest |
| `BuildLabel` | Live | `""` |
| `BaseURLs` | 5 CDN | `[]` |
| `MainGameAppName` | Laridae | `""` |
| `OwnershipTokenForNs` | `"false"` | absent |
| `StagingLocation` | `/bps` forward | `/bps` forward ✓ |

New `.egi` (`B6EE6879….egi`): `artifactId Laridae`, `itemId 682122e9…`, `sandboxId 74d82b28…`, revision `…002D`, `manifestData {version 1.0.4, launchExe Ruiner.exe, buildSize 11341411523}`, **`manifestUris` present** (real install flow supplies CDN URLs; local-planted files had null).

**Log pipeline (the path our `.item` files never take):**
```
PopulateInstallConfigFromCurrent (AppBuild.IsValid 0→1)
RequestAppInstall: 74d82b28…:682122e9…:Laridae
CreateInstallRequest: Main binary resolved to …:Laridae:1.0.4
Queued install … Queue length is 1
RequestSelectiveDownloadSigned … Live
LogEoshPatcher: StartTask: BinaryId […:Laridae:1.0.4]
HandleTaskComplete: AlertCode=[ok] … completed with manifest already committed
```

**Verdict:** identity rules fully correct (proven field-for-field by Epic's own output). Registration is achievable **only** via the EOSH installation API through an actual launcher install — which writes the stamped `.item`, the `.egi`, and makes the app "reported by EOSH". The rejected files (Shadowrun, first Ruiner attempt) were correct in content but never went through this pipeline. This closes the investigation: **no file-based workaround exists; only a real install registers the game.**

### 8k. Ruiner reinstall — OLD vs NEW file (2026-09-09)

`OLD` = `/mnt/r/8BAACAD8….item` (2024, working system). `NEW` = `B6EE6879….item` (Epic's reinstall 02:05–02:09).

| Field | OLD (8BAACAD8) | NEW (B6EE6879) |
|---|---|---|
| Identity triple (AppName/CatId/NS) | Laridae / 682122e9… / 74d82b28… | **identical** |
| LaunchExecutable / AppVersionString / InstallSize | Ruiner.exe / 1.0.4 / 11341411523 | **identical** |
| `InstallationGuid` | 8BAACAD8… | **B6EE6879… (new)** |
| `EoshRevision` | absent | **…002D (stamped)** |
| `Complete/PendingManifestPath` | absent | **set** |
| `InstallSessionId` | real (old) | **new real** |
| `InstallLocation` | O:\GAMES\…\RUINER | D:\Games\Epic Games\RUINER |
| `ManifestLocation` | mixed `…/.egstore` | backslash `…\.egstore` |
| `BaseURLs` | 5 CDN urls | `[]` |
| `BuildLabel` | Live | `""` |
| `PrereqIds` / `OwnershipTokenForNs` | `[]` / `"false"` | absent |
| `MainGameAppName` | Laridae | `""` |
| `SDMeta*` / `bSDMetaMigrated` / `bIsFab` / `SidecarDeploymentId` / `PreloadState` | absent | present-empty |

All format-era differences (old pre-EOSH vs new EOSH-era) — none affect the identity.

### 8l. UNKNOWN — why the flow worked on 2026-08-23 and is blocked now

**Established facts:**
- EOSH was **already active** on 08-23: Tomb Raider `.egi` written 08-23 13:07, DS `.egi` 08-23 14:31, and `epic_item_format.md` (committed c880b53 08-23) already documents acceptance + later cleanup: "Launcher accepted our files (title appeared, 'needs update')… After Tomb Raider's official patch, Epic deleted identification-only `.item`s… We cannot invent `EoshRevision`… survive Epic's next Verify only if Epic itself rewrites the `.item`."
- So on 08-23, identification `.item`s were **accepted and shown Update**; the cleanup was described as happening *after* the patch cycle.
- On 09-09, identification `.item`s are **removed within ~300ms** of being surfaced (`EOSH reconciliation: removing installation not reported by EOSH`), before any Update can be offered. Observed for: Shadowrun (5+ runs), Ruiner attempt #1, Bad North (`E3843FBB…`, AppName `Chives`, AppId `aa5ef9d2…:627e8071…`, 02:20 run), The Captain re-link attempts.
- Launcher now version **20.2.9**; launcher-service server build dated **2026-08-08**; Legendary v0.21.0 (2026-08-04) shipped due to Epic's **ChunksV5 encrypted manifest** CDN migration.

**What we do NOT know (honest state):**
1. **Why the timing changed** — whether the immediate reconciliation is new (20.2.x), whether it depends on account/library state, entitlement flags, or a server-side policy change between 08-23 and 09-09. Not determinable from local logs alone.
2. **Why Tomb Raider/DS got their `.egi` + stamp on 08-23** — i.e., what made those specific Update flows complete while current ones are pre-empted. The docs say the Update ran; the logs from that day are gone (30-day rotation).
3. **The exact condition for "reported by EOSH"** — ownership entitlement? EOSH install-API registration? both? The `ShouldBlockPrePurchaseInstall: no library data yet` line implies an entitlement/lookup step, but the precise trigger is unconfirmed.
4. Whether the 22 legacy items survive because they are owned **and** predate the change, or because something else keeps them in the reported set.

**Bottom line:** identity rules + write-shape are fully understood and proven correct (Ruiner reinstall matches field-for-field). The blocker is the EOSH reported-set membership, and **whether the reconciliation behavior itself changed between 08-23 and 09-09 remains an open question** — we have strong evidence it became immediate, but no proof of the trigger or the server-side driver. This is the honest limit of the local investigation.

### 8m. Tomb Raider "Update" — the decisive detail (2026-09-09 inspection)

The user's observation: TR claimed "Update" on 08-23, but **no game data was ever downloaded**. Verified:

| Item | State |
|---|---|
| `E:\Games\TombRaiderGOTYE\.egstore\181B4659….mancpn` | **STILL PRESENT, dated 2022-10-22** |
| `181B4659….manifest` | 2022-10-22, FStrings: `9917735b…` / `TRAS_15112021_838.0_…` / `TombRaider.exe` |
| `6C99E507….manifest` | **written 2026-08-23 13:07** — SAME FStrings, SAME build, slightly different binary (`29000000d3572200…` vs `2900000039542200…`) = re-encoded copy, no content change |
| game data (bigfile.*.tiger, exe, dlls) | **all dated 2022-10-22, zero files after 2023** |
| `Pending/` | **absent** (no staging happened) |
| `.item` (6C99E507, in Manifests) | `EoshRevision …0022`, `CompleteManifestPath` → 6C99E507 manifest |
| `.egi` | written 08-23 13:07, revision `…0022` |

**What the 08-23 TR Update actually did:** wrote a **new manifest GUID** (`6C99E507….manifest` — same build, re-encoded), wrote the `.item` + `.egi` + EoshRevision stamp. **Zero content download.** The old 2022 manifest AND the `.mancpn` were kept (the docs said Epic deletes mancpn only for identification-only items without CompleteManifestPath/EoshRevision).

**So "Update" = registration + manifest refresh, NOT a patch.**

### 8n. Bad North comparison — the visible difference

Bad North (`E3843FBB….item`, AppName `Chives`, ns `aa5ef9d2…`, itemId `627e8071…`, version `1.0.4`? build 2020):

| Aspect | TR (WORKED 08-23) | Bad North (REJECTED 09-09) |
|---|---|---|
| `.mancpn` present | **YES (181B4659, 2022)** | **NO** |
| `.manifest` present | YES (old 2022 + new 08-23) | YES (2020-04-27) |
| `Pending/` | absent | present (empty) |
| game data | 2022, untouched | 2020 |
| EOSH stamp/.egi | YES (…0022) | NO |
| result | kept, Update offered, registered | `EOSH reconciliation: removing installation not reported by EOSH` |

Shadowrun: `.mancpn` existed when first read (that's where we got `5b414549…`), then was gone by test time — consistent with the docs' "Epic deletes `.mancpn` for identification-only items" (`:412`). TR's mancpn survived because its Update completed.

**Hypothesis (unproven):** the `.mancpn` presence is what EOSH uses to recognize/report an install. TR kept its mancpn → recognized → registered via no-content Update. Bad North has no mancpn → never recognized → "not reported" → deleted. This would explain the difference between TR (worked) and Bad North/Shadowrun (rejected) better than a pure behavior-change theory — **but it is not yet tested, and whether the mancpn is the trigger or merely a side-effect of successful registration remains open.**

### 8o. TEST RESULT (2026-09-09 02:29) — the `.mancpn` hypothesis is FALSIFIED

Test: placed generated `.mancpn` (`E3843FBB….mancpn`: `aa5ef9d2…`/`627e8071…`/`Chives`) into Bad North's `.egstore` + copied the same rejected `.item` into `Manifests\`. Result:

```
Loaded item file .../E3843FBB….item
Manifest Check: aa5ef9d2…:627e8071…:Chives:2.00.5
Found Installed Manifest ...
Adding product / AddProductImplNoNotify / AddSocialApplicationViewModel apps 1
EOSH reconciliation: removing installation not reported by EOSH '.../E3843FBB….item'
   (InstallationId E3843FBB…, AppId aa5ef9d2…:627e8071…:Chives)
ShouldBlockPrePurchaseInstall: no library data yet for aa5ef9d2…:627e8071…:Chives
```

**Same immediate removal.** The `.mancpn` presence did **not** change EOSH's decision. (Also note: the log does not even reference the `.mancpn` — the mancpn is read only when *Epic itself* processes an install, not by the startup reconciliation.)

**Conclusion:** `.mancpn` is a **side-effect** of successful registration, not the trigger. The reconciliation decision rests solely on EOSH's reported-set membership (server/account-driven), which a hand-placed `.mancpn` does not influence. This closes the mancpn hypothesis; the remaining open question is the exact reported-set membership condition (entitlement? EOSH install-API registration? server policy change between 08-23 and 09-09).

### 8p. Procmon observation (2026-09-09 04:33) — probe-only lookups, no new gate

Procmon trace during the Bad North run showed the launcher probing:
- `D:\…\BadNorth\.egstore\Chivesappconfig.json` → NAME NOT FOUND
- `C:\ProgramData\Epic\…\Manifests\E3843FBB….item` → NAME NOT FOUND (yet the same `.item` had been loaded moments earlier)
- then reads + **deletes** `E3843FBB….mancpn` (timestamps zeroed, disposition delete)

**Interpretation (user-corrected):** these are **standard speculative probes**, not evidence of a required file. `appconfig.json` does not exist in ANY working install's `.egstore` (verified: TR, DS, Jotunnslayer, Civ VI, 3K, The Captain, Ruiner, Dishonored, DL2 — none have it), and the launcher probing a `.item` path it already loaded proves these lookups are non-binding. The mancpn deletion matches the documented "Epic deletes mancpn it processes" behavior. No new conclusion is drawn from this trace.

### 8q. `LauncherInstalled.dat` — the launcher's own install registry (2026-09-09)

Full procmon trace showed the removal sequence is **entirely local, done by `EpicGamesLauncher.exe`**:

1. Probe `…\.egstore\Chivesappconfig.json` → NAME NOT FOUND (speculative, noise)
2. Read + **delete** `E3843FBB….mancpn` (timestamps zeroed, `FILE_DISPOSITION_DELETE`)
3. Read + **delete** `E3843FBB….item` (same pattern)
4. **Rewrite `C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat`** (7122 bytes, OverwriteIf) — removes the entry
5. CloudCache probe `Saved\Saves\d460fdcbe…\Chives\CloudCache\` → PATH NOT FOUND (normal; that dir only has registered games: `05cc0386…`, `Boga`, `Redstart`)

**`LauncherInstalled.dat` structure** (`{"InstallationList":[{InstallLocation, NamespaceId, ItemId, ArtifactId, AppVersion, AppName}]}`):

- Now contains **exactly 23 entries = the 23 surviving games** (Jotunnslayer, Kinglet×10, Dishonored, DL2×3, DS `Boga`, Fortnite×3, TR, 3K, Ruiner `Laridae`, The Captain `f00b5aca…`).
- **Bad North absent** — removed in lockstep with `.item` + `.egi`.
- This is the launcher's own installed-app registry, maintained in sync with the EOSH reported set; the reconciliation is the launcher re-deriving its list from `.item`s + EOSH and rewriting this file.

**Implication:** the gate is confirmed as the EOSH-reported membership; the local removal is just the launcher enforcing it across `.item`/`.egi`/`LauncherInstalled.dat`. No local file (`.item` content, `.egi`, `.mancpn`, or this registry) can add an app EOSH does not report.

### 8r. The server call — proof the gate is server-side (procmon, 2026-09-09 04:33:49, real IPs)

Full procmon window around the Bad North removal (local time, IPs as captured):

| Time | Event | Meaning |
|---|---|---|
| 04:33:49.2455845 | Read `Saved\Data\d460fdcbe….dat` (15 B) | per-app data lookup |
| 04:33:49.6340350 | TCP **`::1:64310 → ::1:35783`** (launcher) | **loopback to EOSH local port 35783: revision push** (IPv6 ::1) |
| 04:33:49.6340564 | TCP `::1:35783 → ::1:64310` (UserHelper) | EOSH service responds on 35783 |
| 04:33:49.6350557–.6352801 | TCP `127.0.0.1:64319 ↔ 127.0.0.1:64318` (InstallHelper) | InstallHelper self-loopback IPC (the `vortex.data.microsoft.com` name = hosts-file 127.0.0.1 pin; local-only) |
| **04:33:49.6379660** | **TCP `178.232.8.31:64277 → 184.192.116.159:443`** (launcher, 576 B + 27 B) | **EXTERNAL HTTPS to Epic's AWS EC2 endpoint (`ec2-184-192-116-159.compute-1.amazonaws.com`): the server-side library/entitlement query** |
| 04:33:49.6413661 | `Chivesappconfig.json` probe + `.egstore` scan | reconciliation begins |
| 04:33:49.6418308–.6426170 | mancpn read + delete | removal |
| 04:33:49.6430763–.6440374 | `.item` read + delete | removal |
| 04:33:49.6446387 | `LauncherInstalled.dat` overwritten | registry sync |

**Sequence decoded:** launcher gets the EOSH revision from the local service (loopback `::1:35783`) → **makes an EXTERNAL HTTPS call to `184.192.116.159:443` (Epic AWS EC2) for library/entitlement data** → receives "no library data" for Bad North → deletes `.item` + `.mancpn`, rewrites `LauncherInstalled.dat`. The local deletion is **enforcement of the server's answer**, confirming the gate cannot be influenced by any local file. (All other TCP in the window is loopback: EOSH on `::1:35783`, InstallHelper self-IPC on `127.0.0.1`.)

### 8s. Registration is NOT install-age or token driven (user-corrected, 2026-09-09)

The user's observation kills the "recent install / minted token" theory:

| Game | Installed (oldest .egstore file) | `.egi` written (= EOSH registered) | Works? |
|---|---|---|---|
| Tomb Raider | **2022-10-22** | **2026-08-23 13:07** (fix moment) | ✓ |
| Death Stranding | **2022-12-27** | **2026-08-23 14:31** (fix moment) | ✓ |
| DL2 | 2023-02-11 | 2026-08-15 | ✓ |
| Jotunnslayer | 2026-01-31 | 2026-07-02 | ✓ |
| Three Kingdoms | 2026-01-01 | 2026-07-02 | ✓ |
| Crashlands | 2025 | never | ✗ |
| Shadowrun / Bad North | 2020-2024 | never | ✗ |

- TR/DS are **2022 installs** whose `.egi` was written **on the 08-23 fix date** — registration happened when the launcher touched them, not at install.
- DS worked with **just the correct AppName** on an ancient install — no reinstall, no token.
- Crashlands (2025, newer than TR/DS) did **not** work — recency is irrelevant.

**Conclusion:** registration is not install-age or token-driven — the `.egi`/`EoshRevision` is a **side-effect of a launcher registration event**, not a per-install token, and not age-dependent. The working games were registered in batches/moments (12× `…000C` stamped 07-02; TR/DS/Dishonored stamped 08-23; DL2 08-15; Fortnite 09-08; The Captain/Ruiner 09-09 via reinstall). **Why those moments registered old installs and the current ones don't remains the open question** — pointing to a launcher/server-side behavior around those dates, not to anything in the local files.

### 8t. DIRECT LAUNCH test — Troy is OWNED yet still blocked (2026-09-09 03:12)

User copied the Troy `.item` (with both manifest references set) and launched `com.epicgames.launcher://apps/D:...\TotalWarSagaTROY\troy.exe?action=launch` directly. New log lines:

```
AppLaunchUriHandler: Skipping entitlement refresh for AppId 53310576…:dc820156…:11e598b1… - ownership status is Owned (refresh only fires on NotOwned)
AppLaunchUriHandler: Notification - Application is not installed. AppId 53310576…:dc820156…:11e598b1…
```

**Key facts:**
1. **Troy's ownership status resolved to `Owned`** — the account OWNS it (this is the "library data" check; it succeeded for the launch flow).
2. Yet the EOSH reconciliation (same run, `03.12.53:715`) **deleted the `.item`** with the identical "not reported by EOSH" message.
3. The launch handler then reported **"Application is not installed"** ~2s later — because the `.item` was already gone.

**Conclusion:** being **`Owned` is NOT sufficient** — Troy is owned, ownership resolved to Owned, and the `.item` was still removed. The gate is the **EOSH reported-set** (the install registration), which is separate from entitlement ownership. "Library data" in the reconciliation context ≠ ownership; the account can own a game and the launcher still refuse its `.item`. This is the strongest evidence yet that the reported-set requires an actual EOSH install-API registration event, independent of ownership.

**Confirmed rejections so far (all identical mechanism):** Shadowrun, Ruiner#1, The Captain ×4, Bad North (`Chives`), Troy (`11e598b1…`, Owned).

### 8u. Troy direct launch — the REAL install was attempted and failed on PERMISSIONS (2026-09-09 03:17)

Final launch of Troy (owned) went past the reconciliation into a **real EOSH install attempt**:

```
AppLaunchUriHandler: Skipping entitlement refresh … ownership status is Owned
AppLaunchUriHandler: Notification - Application is not installed  (03.17.18 — .item already deleted by reconciliation)
PopulateInstallConfigFromCurrent: AppBuild.IsValid=1
RequestAppInstall → CreateInstallRequest: Action: Install …:16676.3427123
LogEoshPatcher: StartTask: BinaryId […:11e598b1…:16676.3427123]
LogDownloadManager: HandleTaskComplete: AlertCode=[IS-0002-DP-01]
HandlePatchComplete: The installation failed … installer will no longer try again
```

**Cause (`IS-0002-DP-01` = DP-01 "Not enough permissions"):** same-run log lines:
```
FixLauncherInstallDirectoryPermissions: SetNamedSecurityInfo failed with 5
CreateProc failed: Access is denied. (0x00000005)
Failed to start the Curl process.
```

**User clarification (2026-09-09):** the install attempt failed because the **user declined the admin/UAC permission prompt** — deliberately, to observe behavior. **More importantly: the launcher attempted to install to the DEFAULT Epic Games install path, NOT to the existing game folder** (`D:\Games\Epic Games\TotalWarSagaTROY`). The launcher had zero awareness of the orphaned install at its custom path — because the EOSH reconciliation had already deleted the `.item` at startup, so the launcher no longer knew the game existed there. A launch therefore triggered a **fresh install** (to the default location), not a repair of the existing files.

**Conclusion:** for an Owned game whose `.item` was deleted by reconciliation, the launcher treats it as *not installed anywhere* and attempts a fresh install to the default path. There is **no repair/relink path** back to the existing custom-path install — the orphan is invisible to the launcher once EOSH doesn't report it. This fully explains why no local file manipulation can register such installs.

## Correction (docs/research/epic_item_format.md:188,394,412)

- Unstamped identification `.item` **is accepted** (title appears, "needs update"). Tomb Raider precedent.
- `EoshRevision`/`CompleteManifestPath` written **by Epic during Update** ("We cannot mint those; Update does", `:206`).
- Identification-only `.item`s without `CompleteManifestPath`/`EoshRevision` are later **deleted** (`:412`) = the observed `EOSH reconciliation` line.

## Death Stranding control — accepted EMPTY, stamped after Update

- DS breakthrough file (2026-08-23) had **`EoshRevision` empty at write time** and was accepted (Update shown).
- DS's current `EoshRevision` = `08DEBEDB31C8609B0000000000000024`; `lastCloudSync 2026.08.23-13.30.47` = the Update date (git history of `epic_item_format.md` matches). Epic stamped it **during that Update**.
- Current logs (09-08 onward) show DS loaded → manifest found → added → in `get-installed-apps-response` with `isOwnershipCacheOwned:true`, **never removed** (owned + stamped).
- Launcher logs only retain ~30 days; the 08-23 DS event predates all logs we have — the docs/`plan-121` are its record.

## Conclusion

`.item` content correct + catalog contains the ids (catcache.bin) is not enough. The account's EOSH registry must report the app (requires an actual Epic-side install/ownership). No `.item` can add an unreported title.