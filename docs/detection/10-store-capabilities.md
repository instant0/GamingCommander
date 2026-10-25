# Store Capabilities — Launcher, Inventory, and Health Matrix

**Status:** Living reference (2026-10-25).
**Audience:** Anyone adding launcher discovery, inventory import, health probes, or startup sync.

This document records what the shipped app actually does for each store, what the launcher
itself trusts as inventory, and what remains unverified. Use it before building a store
feature: a cell marked `unsupported` is not implemented, and `needs verification` means the
format or behavior is documented but not confirmed in code or against a complete real install.

---

## 1. Legend and how to use

| Token | Meaning |
|-------|---------|
| `verified` | The behavior exists in shipped code and is cited to `file:line`, or the format is confirmed by a real corpus/research doc. |
| `needs verification` | The format or source is documented or partially handled, but the app does not fully implement it, or its shape/schema has not been confirmed against a complete install. |
| `unsupported` | Not implemented in the shipped app. The launcher may do it; we do not. |

**How to use:** read the row first to see whether a capability exists, then read the
per-store section and gap register before planning work. Treat launcher inventory as a
hint, not truth: disk existence is the final `Installed` signal.

---

## 2. Capability matrix

| Store | Launcher located | Inventory source (launcher trusts) | Inventory read by app | Per-game metadata read | Exe discovery | Health states | Startup incremental sync | Read-only safety | Evidence |
|---|---|---|---|---|---|---|---|---|---|
| **Steam** | `verified` — HKLM `Valve\Steam\InstallPath` only (`SteamInstallPathLocator.cs:25,35`) | `verified` — `libraryfolders.vdf` (path locator) + `appmanifest_*.acf` + `steamapps/common/` (`SteamAcfParser.cs:62`; `SteamLibraryScanner.cs:134`) | `verified` — ACF/VDF + `common/` (`SteamLibraryScanner.cs:39-97`) | `verified` — appid/name/installdir/state/size/build; `LastUpdated` is not persisted and `StateFlags` is stored raw, never interpreted (`SteamAcfParser.cs:35-44`; `SteamLibraryScanner.cs:178-186`) | `verified` — cheap root/known-folder heuristic; launch via `steam://` (`SteamLibraryScanner.cs:212-214,331-336`; `GameLaunchResolver.cs:14`) | `verified` — Installed/Moved/Orphaned/Missing (`SteamLibraryScanner.cs:64-66,72,298`) | `unsupported` | `verified` — reads registry/VDF/ACF only | [03-store-signals.md](03-store-signals.md), [steam_acf_schema.md](../research/steam_acf_schema.md), [steam_vdf_schema.md](../research/steam_vdf_schema.md) |
| **Epic** | `unsupported` — no locator; fixed ProgramData manifests path only (`EpicManifestParser.cs:270-276`) | `needs verification` — ProgramData `*.item`; `LauncherInstalled.dat` documented but unread | `verified` — ProgramData `*.item` only (`EpicItemCatalog.cs:14-28`) | `verified` — display/install/exe/catalog ids (`EpicManifestParser.cs:24-88`); `InstallSize`/`AppVersionString`/`InstallationGuid`/`CompleteManifestPath` unread | `verified` — `.item` `LaunchExecutable` else scored heuristic (`EpicLaunchResolver.cs:21-56`) | `verified` — Installed/Missing/Orphaned, fuzzy path match (`EpicLibraryScanner.cs:57,151-155`; `EpicInstallPath.cs:16-27`) | `unsupported` | `verified` — catalog scan is read-only; a separate user-started repair writes an identification `.item` (`EpicItemWriter.cs:145`) but does not register | [epic_item_format.md](../research/epic_item_format.md), [epic_manifest_schema.md](../research/epic_manifest_schema.md) |
| **GOG** | `unsupported` | `needs verification` — per-game registry + `goggame-*.info`; Galaxy SQLite schema unverified | `unsupported` — registry classifies only (`RegistryFallbackDetector.cs:183-197`) | `verified` — `.info` name/gameId/exe/args, partial (`GogInfoParser.cs:82-118`) | `verified` — heuristic first, `.info` fallback (`StoreMetadataEnricher.cs:84-88`) | `unsupported` — `FolderScanner` hardcodes `Installed` (`FolderScanner.cs:383`) | `unsupported` | `needs verification` — registry/`.info` safe; Galaxy DB unverified | [gog_format.md](../research/gog_format.md), [09-blueprint.md](09-blueprint.md) |
| **EA** | `unsupported` | `needs verification` — encrypted EA App `IS`; legacy Origin `.mfst`; registry | `unsupported` | `verified` — `InstallLog.txt` name/display/studio (`EaInstallLogParser.cs:51-65`) | `verified` — heuristic (`FolderScanner.cs:285-305`) | `unsupported` | `unsupported` | `needs verification` — encrypted `IS`; legacy `.mfst` safe but unparsed | [ea_format.md](../research/ea_format.md), [GAME-DETECTION-LOGIC.md](../GAME-DETECTION-LOGIC.md) |
| **Ubisoft** | `unsupported` | `needs verification` — `Installs\{gameId}\InstallDir` + `uplay_install.state`; manifest encrypted | `unsupported` — registry classifies only, game ID discarded (`RegistryFallbackDetector.cs:163-177`) | `verified` — readme title only (`UbisoftReadmeParser.cs:87-88`; `TitleEnricher.cs:76-87`) | `verified` — heuristic (`FolderScanner.cs:285-305`) | `unsupported` | `unsupported` | `needs verification` — registry safe; manifest encrypted | [ubisoft_format.md](../research/ubisoft_format.md) |
| **Battle.net** | `unsupported` | `needs verification` — Agent `product.db` SQLite schema unverified; `Battle.net.config` | `unsupported` | `verified` — `.build.info` product code (`StoreSignalDetector.cs:214-241`) | `verified` — heuristic (`FolderScanner.cs:285-305`) | `unsupported` | `unsupported` | `needs verification` — SQLite schema unverified | [blizzard_format.md](../research/blizzard_format.md), [03-store-signals.md](03-store-signals.md) |
| **Xbox** | `unsupported` | `needs verification` — AppX package registration via `Get-AppxPackage`; no package API in app | `unsupported` | `unsupported` — `default-metadata.json` is a marker only, no fields parsed | `needs verification` — heuristic only; no package identity/exe | `unsupported` | `unsupported` | `needs verification` — WindowsApps ACL/package API not used | [xbox_format.md](../research/xbox_format.md) |
| **Rockstar** | `unsupported` — Rockstar launcher key is skipped (`RegistryFallbackDetector.cs:210-212`) | `needs verification` — `Settings.json` + per-game registry; app reads neither | `unsupported` | `unsupported` — `title.rgl` unparsed; RAGE engine only (`EngineDetector.cs:65-69`) | `verified` — heuristic (`FolderScanner.cs:285-305`) | `unsupported` | `unsupported` | `verified` — `Settings.json`/registry safe; app reads neither for inventory | [rockstar_format.md](../research/rockstar_format.md) |

---

## 3. Steam

**Launcher mechanism.** The app reads one machine registry value:
`HKLM\SOFTWARE\Valve\Steam\InstallPath` (`SteamInstallPathLocator.cs:25,35-39`). It does not
probe HKCU `SteamPath`, `WOW6432Node`, default paths, or running processes. The install path
plus every `"path"` in `libraryfolders.vdf` becomes the library set
(`SteamInstallPathLocator.cs:61-74`).

**Inventory source.** Steam's inventory is structural: `libraryfolders.vdf` locates libraries,
`appmanifest_*.acf` identifies apps, and `steamapps/common/<installdir>` proves the files exist.
The VDF is explicitly a path locator only; its `apps` blocks are ignored
(`SteamAcfParser.cs:52-59`; `docs/detection/03-store-signals.md:54-58`).

**What we read today.** `SteamLibraryScanner` collects ACFs from every discovered library and
scans `common/` in the requested root (`SteamLibraryScanner.cs:39-97`). The ACF map is keyed by
`installdir` and is first-wins (`SteamLibraryScanner.cs:150-152`). Required ACF fields are
appid, name, installdir, StateFlags, LastUpdated, SizeOnDisk, buildid
(`SteamAcfParser.cs:13-16`).

**Metadata.** `appid` is persisted as `StoreInfo.SteamAppId`; name becomes the display title;
installdir drives folder matching; size and buildid are stored
(`SteamLibraryScanner.cs:178-186,223`). `LastUpdated` is parsed but not put into
`SteamAcfInfo`, and `StateFlags` is stored as a raw string with no bitmask interpretation.

**Executable.** A cheap lookup checks the game root and a small set of known relative folders
(`Shipping`, `Binaries/Win64`, `bin/x64`, …) and skips installer noise
(`SteamLibraryScanner.cs:320-349`). Launch does not use the exe: it uses
`steam://rungameid/{appid}` (`SteamLibraryScanner.cs:214`; `GameLaunchResolver.cs:14-15`).

**Health.** Real states: `Installed` (ACF and folder in this root), `Moved` (ACF belongs to a
different library), `Orphaned` (folder with no ACF anywhere), `Missing` (ACF with no folder in
any known library) (`SteamLibraryScanner.cs:60-97,232-307`).

**Verification status.** Locator, VDF parsing, ACF metadata, inventory, and health are shipped
and covered by the detection corpus. Launcher-locator variants and ACF state semantics are
not.

**Gap register**
- Add HKCU/`WOW6432Node`/default-path locator fallbacks.
- Parse the VDF as VDF rather than splitting quoted lines.
- Interpret `StateFlags` and persist `LastUpdated`.
- Key the ACF map by appid instead of first-wins `installdir`.
- Detect and offer cross-library ACF remediation.
- Add file-level integrity and startup incremental sync.

---

## 4. Epic Games Store

**Launcher mechanism.** No launcher locator. The app uses the fixed ProgramData manifests
path (`%ProgramData%\Epic\EpicGamesLauncher\Data\Manifests\`) with an environment/override
escape hatch (`EpicManifestParser.cs:264-288`).

**Inventory source.** The launcher's display records are `*.item` JSON files in that folder;
`LauncherInstalled.dat` is also documented as a launcher record but the app never reads it
(`docs/research/epic_item_format.md`). A game folder with `.egstore/` or `.egsstore/` is a
local signal (`StoreSignalDetector.cs:172-177`).

**What we read today.** `EpicItemCatalog` enumerates `*.item` and keeps playable base games,
deduplicating by `CatalogItemId` (`EpicItemCatalog.cs:6-59`). `EpicLibraryScanner` turns those
records into entries and does not walk the disk except to resolve the recorded install path
(`EpicLibraryScanner.cs:11-145`).

**Metadata.** Display name, install location, launch executable, catalog namespace, catalog
item ID, app name, incomplete-install flag, application flag, and categories
(`EpicManifestParser.cs:24-88`). `InstallSize`, `AppVersionString`, `InstallationGuid`, and
`CompleteManifestPath` are not read.

**Executable.** The `.item` `LaunchExecutable` is resolved against `InstallLocation`; if it is
missing or is a store launcher, a scored heuristic picks a real game exe
(`EpicLaunchResolver.cs:21-56`).

**Health.** `Installed` when the recorded folder exists, `Missing` when it does not, and
`Orphaned` for a known Epic folder with no matching ProgramData `.item`
(`EpicLibraryScanner.cs:57,128-159`). Matching is path- or folder-name-based, so it is fuzzy
(`EpicInstallPath.cs:16-27`).

**Verification status.** Catalog reading, metadata, exe resolution, and health are shipped.
`EpicOrphanDiscovery` exists but has no callers; `LauncherInstalled.dat` is unread.

**Gap register**
- Add an Epic launcher locator and read `LauncherInstalled.dat`.
- Wire orphan discovery into the catalog pipeline.
- Read `InstallSize`, `AppVersionString`, `InstallationGuid`, `CompleteManifestPath`.
- Honor a configurable manifests root consistently and harden JSON parsing.
- Add optional `com.epicgames.launcher://` launch.
- Do not treat a written identification `.item` as registration (see §10).

---

## 5. GOG

**Launcher mechanism.** None. The app has no GOG launcher locator.

**Inventory source.** GOG's launcher trusts per-game registry keys under
`GOG.com\Games\{gameId}` (path, exe, game name, build, install date) and a per-game
`goggame-*.info` file inside the game folder (`docs/research/gog_format.md`;
`planning/115-ea-ubisoft-registry-fallback.md:71-89`). Galaxy also keeps a SQLite database;
its schema is unverified and the app does not read it.

**What we read today.** The registry fallback enumerates `GOG.com\Games\{gameId}` and reads
only `path` (`RegistryFallbackDetector.cs:183-197`). It uses that path to classify folders; it
does not import the registry as inventory. `GogInfoParser` reads `goggame-*.info` at the game
root and one non-noise subdirectory level (`GogInfoParser.cs:42-134`).

**Metadata.** `.info` title, game ID, primary play-task exe, and launch arguments
(`GogInfoParser.cs:82-118`). Registry `gameName`, `exe`, `BUILDID`, `INSTALLDATE`, and `ver`
are not read.

**Executable.** The normal heuristic runs first; `.info` fills the exe only when discovery
found nothing, and `.info` arguments are applied (`StoreMetadataEnricher.cs:70-98`).

**Health.** None. GOG folders classified by `FolderScanner` get `Installed` unconditionally
(`FolderScanner.cs:383`).

**Verification status.** Detection and partial `.info` enrichment are shipped. Inventory
import, registry metadata, and Galaxy SQLite are not.

**Gap register**
- Add a GOG launcher locator.
- Enumerate `GOG.com\Games` as inventory, independent of a disk scan.
- Read full registry metadata and merge all `.info` files.
- Prefer the `.info` exe over the heuristic.
- Add health states and verify the Galaxy SQLite schema before coding.

---

## 6. EA App / Origin

**Launcher mechanism.** None. `__Installer/` at the game root is the detection signal
(`StoreSignalDetector.cs:98-108`).

**Inventory source.** EA App records installs in the encrypted `IS` store; legacy Origin used
`.mfst` files and per-game registry keys. The app reads neither as inventory
(`docs/research/ea_format.md`; `docs/research/launcher_discovery.md`).

**What we read today.** `EaInstallLogParser` reads `__Installer/InstallLog.txt` (UTF-16),
taking the last session's game name, display name, and studio (`EaInstallLogParser.cs:28-88`).
The registry fallback can read EA per-game `Install Dir` values for classification
(`RegistryFallbackDetector.cs:136-157`).

**Metadata.** Display name, canonical game name, and studio from `InstallLog.txt`
(`StoreMetadataEnricher.cs:100-126`). The staged/unstaged `installerdata.xml` is not parsed.

**Executable.** The normal heuristic runs; there is no EA content-ID or launcher-path
resolution (`FolderScanner.cs:285-305`).

**Health.** None; `Installed` is hardcoded (`FolderScanner.cs:383`).

**Verification status.** `InstallLog.txt` enrichment is shipped. Launcher location, inventory,
`installerdata.xml`, legacy `.mfst`, and a complete modern install are unverified.

**Gap register**
- Add an EA launcher locator.
- Parse `installerdata.xml` (content ID, title, launcher path, version).
- Parse legacy Origin `.mfst` files.
- Enumerate registry inventory and store the EA content ID.
- Verify the format against a complete modern install.
- Add health states and startup sync.

---

## 7. Ubisoft Connect

**Launcher mechanism.** None. Detection is `uplay_install.manifest`/`.state`,
`uplay_r*_loader*.dll`, `uplay_download/`, `*_UPP*.exe`, or the emulator signature
(`StoreSignalDetector.cs:110-170`).

**Inventory source.** Ubisoft records per-game installs under
`HKLM\...\Ubisoft\Launcher\Installs\{gameId}\InstallDir` with `uplay_install.state` in the
folder (`docs/research/ubisoft_format.md`;
`planning/115-ea-ubisoft-registry-fallback.md:55-66`). `uplay_install.manifest` is encrypted
and unparsed.

**What we read today.** The registry fallback enumerates `Installs\{gameId}` and reads
`InstallDir` to classify a folder, then discards the game ID
(`RegistryFallbackDetector.cs:163-177,95-101`). `UbisoftReadmeParser` reads the first two
lines of `Support/Readme/*.txt` for publisher and title (`UbisoftReadmeParser.cs:43-111`).

**Metadata.** A readme title only; publisher is parsed but only the title reaches the entry
(`TitleEnricher.cs:76-87`). No game ID, install state, or manifest metadata is persisted.

**Executable.** The normal heuristic runs (`FolderScanner.cs:285-305`).

**Health.** None; `Installed` is hardcoded (`FolderScanner.cs:383`).

**Verification status.** Detection and readme title enrichment are shipped. Launcher location,
inventory import, game-ID persistence, `uplay_install.state` parsing, and health are not.

**Gap register**
- Add a Ubisoft launcher locator.
- Enumerate `Installs\{gameId}` as inventory.
- Store the Ubisoft game ID and publisher.
- Parse `uplay_install.state` and distinguish emulator folders.
- Add health states and startup sync; do not decrypt the manifest before verification.

---

## 8. Battle.net

**Launcher mechanism.** None. Battle.net games are typed by markers at the game root:
`.battle.net/`, `.build.info`, `.product.db`, or `.patch.result`
(`StoreSignalDetector.cs:187-206`).

**Inventory source.** The Battle.net Agent keeps `%ProgramData%\Battle.net\Agent\product.db`
(SQLite) and `Battle.net.config`; the app reads neither. The Agent database and the game-root
`.product.db` are different files (see §10) (`docs/research/blizzard_format.md:41-51`).

**What we read today.** Only the marker files. `ExtractBlizzardProduct` reads the CDN Path
field (index 6) of `.build.info` and extracts the product codename
(`StoreSignalDetector.cs:214-241`).

**Metadata.** Product code only, stored as `StoreInfo.BlizzardProduct`
(`StoreMetadataEnricher.cs:180-188`). No title, install path, version, or language from the
Agent database.

**Executable.** The normal heuristic runs (`FolderScanner.cs:285-305`).

**Health.** None; `Installed` is hardcoded (`FolderScanner.cs:383`).

**Verification status.** Marker detection and product-code extraction are shipped. Agent
inventory, schema, and metadata are unverified.

**Gap register**
- Add a Battle.net launcher locator.
- Verify the Agent `product.db` schema, then read it read-only.
- Distinguish the Agent database from the game-root `.product.db`.
- Read metadata beyond the product code.
- Add health states and startup sync.

---

## 9. Xbox / Microsoft Store

**Launcher mechanism.** None. `default-metadata.json` at the folder root is the only signal
(`StoreSignalDetector.cs:243-247`).

**Inventory source.** The authoritative source is AppX package registration via
`Get-AppxPackage` / the Windows package API, not registry (`docs/research/xbox_format.md`).
The app has no package-API call.

**What we read today.** Only the marker file's existence. `default-metadata.json` contents,
`appxmanifest.xml`, `resources.pri`, and package identity are not read.

**Metadata.** None parsed. The folder is typed as Xbox, then normal folder-scan title/exe
enrichment runs.

**Executable.** The normal heuristic runs, but there is no package identity or declared
`Application Executable` from `appxmanifest.xml`
(`docs/research/xbox_format.md`).

**Health.** None; `Installed` is hardcoded (`FolderScanner.cs:383`).

**Verification status.** Detection is shipped. Package location, inventory, metadata, exe
resolution, launch (`shell:appsFolder`), and health are not.

**Gap register**
- Add a package locator (`Get-AppxPackage` / package API).
- Read `default-metadata.json` and `appxmanifest.xml`.
- Persist package identity and resolve the declared executable.
- Support `shell:appsFolder` launch.
- Add package-presence health states and startup sync.

---

## 10. Rockstar Games Launcher

**Launcher mechanism.** None. The registry fallback deliberately skips the `Launcher` subkey
because it is the launcher, not a game (`RegistryFallbackDetector.cs:203-221`).

**Inventory source.** Rockstar records games in
`%LOCALAPPDATA%\Rockstar Games\Launcher\Settings.json` plus per-game registry keys
(`docs/research/rockstar_format.md`). The app reads neither for inventory.

**What we read today.** Only `title.rgl` as a detection marker
(`StoreSignalDetector.cs:249-253`). `title.rgl` contents are not parsed.

**Metadata.** None. The RAGE engine is detected when both `title.rgl` and `common.rpf` exist
(`EngineDetector.cs:65-69`), but that is an engine tag, not store metadata.

**Executable.** The normal heuristic runs (`FolderScanner.cs:285-305`).

**Health.** None; `Installed` is hardcoded (`FolderScanner.cs:383`).

**Verification status.** Detection and RAGE engine tagging are shipped. Launcher location,
`Settings.json`, title ID, inventory, health, and startup sync are not.

**Gap register**
- Add a Rockstar launcher-key locator.
- Read `Settings.json` and the per-game registry inventory.
- Parse `title.rgl` for the title ID.
- Add health states and startup sync.

---

## 11. Documentation contradictions reconciled

This section records the disagreements found across the detection docs and research, and which
behavior is authoritative.

### 11.1 Stale "signal only" wording for Epic, EA, and Ubisoft

`docs/GAME-DETECTION-LOGIC.md` calls Epic "signal only" (lines 80, 127, 141), EA "signal only"
(line 167), and Ubisoft "signal only" (lines 86, 188); `docs/EPIC-MANIFEST-ENRICHMENT.md:50`
also says "Signal-only". Shipped code does more: Epic parses local `.mancpn`/`.item` and
cross-references ProgramData manifests (`StoreMetadataEnricher.cs:128-177`), EA parses
`InstallLog.txt` (`StoreMetadataEnricher.cs:100-126`), and Ubisoft reads `Support/Readme`
titles (`TitleEnricher.cs:76-87`). Resolution: **classification is signal-based; metadata is
parsed where a parser exists.** The matrix and per-store sections above describe the actual
split, and the old wording is historical, not current.

### 11.2 Epic `.item` write versus registration

`docs/research/epic_item_format.md:6` explicitly marks `.item` re-creation/registration as a
dead end because Epic's installed set is server/account-driven. The app still has
`EpicItemWriter`, which writes an **identification** `.item` in the user-started repair flow
(`EpicItemWriter.cs:6-9,145`). Resolution: **writing an identification `.item` is not
registration and is not inventory import.** Catalog scanning stays read-only; the writer is a
separate, explicit user action and does not create an installed state in Epic.

### 11.3 Battle.net `.battle.net/` versus `.build.info` / `.product.db`

Older wording and `docs/research/blizzard_format.md` treat `.battle.net/` as the primary
signal. The corpus fix in `docs/detection/03-store-signals.md:87-104` showed real games carry
`.build.info` / `.product.db` / `.patch.result` instead, often without `.battle.net/`. The
shipped detector accepts all four (`StoreSignalDetector.cs:187-206`). Resolution: **markers are
alternatives, not a hierarchy that requires `.battle.net/`;** `.build.info` supplies the
product code.

### 11.4 The two `product.db` files

There are two unrelated files with the same name:

- `%ProgramData%\Battle.net\Agent\product.db` — the Agent's SQLite inventory
  (`docs/research/blizzard_format.md:41-51`).
- `<game>\.product.db` — a game-root binary/protobuf marker used for detection
  (`StoreSignalDetector.cs:198-200`; `docs/GAME-DETECTION-LOGIC.md:335`).

Resolution: **never conflate them.** The app reads neither as inventory today; only the
game-root file's existence is used as a marker. The Agent database remains unverified.

### 11.5 Steam registry-key variants

`docs/research/launcher_discovery.md` lists HKCU `Software\Valve\Steam\SteamPath` and HKLM
`SOFTWARE\WOW6432Node\Valve\Steam\InstallPath`, while `docs/detection/03-store-signals.md:42-50`
documents HKLM `SOFTWARE\Valve\Steam\InstallPath` plus an HKCU equivalent. The shipped locator
reads only `HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam\InstallPath`
(`SteamInstallPathLocator.cs:25,35-39`). Resolution: **the extra keys are documented
variants, not implemented fallbacks.** HKCU, `WOW6432Node`, and default-path probing are
gaps, not shipped behavior.

### 11.6 GOG default-path disagreement

Three different values appear:

- `docs/research/gog_format.md:8` and `docs/research/launcher_discovery.md:31` say the default
  game directory is `C:\Program Files (x86)\GOG Galaxy\Games\`.
- The real registry sample in `planning/115-ea-ubisoft-registry-fallback.md:87-89` gives
  `GalaxyClient\paths\client = C:\Program Files (x86)\GOG Galaxy` and
  `DefaultPackPath = C:\GOG Games`.
- The real corpus layout in `planning/123-detection-bugfixes.md:828` shows
  `GOG Galaxy\Games\<Game>\`.

Resolution: **these describe different things** — the client install directory, the optional
`Games\` subfolder, and the registry-configured default pack path. There is no single
"GOG default"; a GOG install can be anywhere. The app uses none of them because it has no GOG
locator.

### 11.7 Store-count drift and the omitted Xbox row

`docs/detection/README.md` says "all 9 stores"; `docs/detection/03-store-signals.md` lists nine
signal rules because it counts emulator buckets (Ubisoft Emulator and Steam Emulator) alongside
store clients. `META/ROADMAP.md:27` lists Phase 3 stores as "GOG, Epic, EA, Ubisoft,
Battle.net, Rockstar" — seven rows, omitting Xbox — while line 41 records Xbox detection as
shipped. Resolution: **this matrix covers eight launcher stores** (Steam, Epic, GOG, EA,
Ubisoft, Battle.net, Xbox, Rockstar). The detection docs' nine are signal categories, not
launcher clients. Xbox detection is shipped (`StoreSignalDetector.cs:243-247`) and belongs in
the eight-store list; the roadmap phase summary should read eight when next updated.

---

## 12. Cross-store observations

- **Only Steam and Epic have real health states.** Every other store goes through
  `FolderScanner`, which writes `InstallState.Installed` unconditionally
  (`FolderScanner.cs:383`).
- **No store has startup incremental sync.** No startup inventory reader or diff exists.
- **Inventory is never imported for six stores.** GOG, EA, Ubisoft, Battle.net, Xbox, and
  Rockstar are detection-and-enrichment only; their launcher inventories remain unread.
- **Read-only is the rule, with one explicit exception.** Catalog/inventory scanning reads
  stores; the Epic identification `.item` writer is a separate user-started repair action and
  does not register a game.
