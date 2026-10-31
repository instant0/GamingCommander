# What GamingCommander reads, writes, and contacts

**This is the live contract.** If code and this file disagree, fix the code or this file — do not leave both.

Default: **offline.** If F2 **Enable online metadata** is off, there is **no HTTP at all** (no probe, no PCGW, no Steam Store). If it is on, HTTP runs only while the chip is **Online**.

We do **not** use SteamDB, PCGW Cargo, or IGDB. The Epic storefront client exists but is **not called in v1**; when it is wired it is an identity lookup only (§1.4).

---

## 1. Network (CONNECT / DOWNLOAD)

Only these hosts, and only when lookup is enabled.

### 1.1 One connectivity probe (startup, once per process)

| | |
|--|--|
| **When** | App opens (or F2 turns lookup on). Once. Never again that session. |
| **How** | `HEAD https://www.pcgamingwiki.com/favicon.ico` (15s timeout) |
| **Send** | User-Agent. No query, no body. |
| **Receive** | Headers only (0 bytes). **Any HTTP status = Online.** Timeout / no connection = Offline. |
| **Why** | Set the chip **Online** / **Offline**. Offline is **sticky** — no more HTTP this run. |
| **Chip** | **Online** green · **Offline** red · **Lookup Disabled** yellow · **Checking…** while the probe runs |

Failed later PCGW/Steam HTTP (timeout, 5xx, connection error) also flips **Offline** and stops the queue.

### 1.2 PCGamingWiki — extras for one game

| | |
|--|--|
| **When** | Chip is **Online**, and (a) **F3** on the selected game, or (b) F5 finished a rescan and queued that game, or (c) you open **F4** (background, dialog does not wait), or (d) you **launch** it and its sidecar is older than **60 days**. **Not** when you only highlight a row. Failed HTTP or a *different* wiki/AppID does not replace a good sidecar. F3 page pick can. Same-URL page edits still update. |
| **How** | 1. If we have a Steam AppID (Steam library ACF): `GET https://www.pcgamingwiki.com/api/appid.php?appid={id}` — one page, no name picker. Works **without** an exe. 2. Else if F3 chose a page, fetch that title. 3. Else name search: display name, then folder name, then PE ProductName; ®/™ stripped; `DeepRock` → `Deep Rock`. `GET .../w/api.php?action=opensearch&limit=8&search={query}`. If OpenSearch is empty, `list=search`. Soundtrack/demo titles and case-only duplicates are skipped. Year from the exe (or folder date) prefers e.g. Dead Space (2023) over 2008. F3 with several *different* titles: you pick. 4. `GET .../w/api.php?action=parse&prop=wikitext&redirects=1&page={title}`. |
| **Send** | AppID or a title string. No account, no install paths, no files. |
| **Receive** | Wiki wikitext. We parse Infobox (dev/engine/date), Availability (Steam/GOG/Epic ids), Game data paths, command-line table, Fixbox args, Video caps. |
| **Why** | Right-pane paths/args/video; F4 argument catalog; Steam AppID when the folder scan did not have one. |
| **Rate** | ≥ 0.6s between PCGW calls. Queue is **one game at a time**. |

### 1.3 Steam Store — optional pane fields

| | |
|--|--|
| **When** | After PCGW for that game, if we have a Steam AppID (from ACF **or** parsed from PCGW Availability). Same triggers as 1.2. |
| **How** | `GET https://store.steampowered.com/api/appdetails?appids={id}` |
| **Send** | AppID only. |
| **Receive** | Name, developers, publishers, genres, release date (locale string), Metacritic score, short description, header image URL, website. **No** engine, saves, or launch args. |
| **Why** | Fill holes PCGW does not have (score, cover URL). Does not change launch. |
| **Rate** | ≥ 10s between Store calls. |

### 1.4 What we do not contact in v1

SteamDB, PCGW Cargo, IGDB, telemetry, update servers, crash reporters.

Epic's storefront GraphQL (`store.epicgames.com/graphql`) is **implemented but not called in v1**. When it
is wired it is an identity lookup only: resolve the canonical title, namespace, and catalog id for a game
local files cannot identify, and resolve the title before the PCGW/Steam query. It is never called from
scan, launch, sync, or health, and it is not a metadata source or an `.item` generator.

### 1.5 Cache (not a delete timer)

Results live in `data/games_metadata.json` with `LastUpdated`. If younger than **60 days**, we do not fetch again. Old rows are **kept** until a successful fetch overwrites them. A failed fetch does not wipe the row.

---

## 2. Local files we WRITE

App-owned state lives under `data/` (next to the exe). Two **user-started repair actions** write outside
it: a Steam ACF into the user's library, and an existing Epic `.item` in `%ProgramData%`. Nothing is
written into game installs.

| File | What | Why | When |
|------|------|-----|------|
| `settings.json` | Library roots, hidden folders, `EnableOnlineMetadata`, first-run / version | Remember setup | F2 close, first run |
| `libraries.json` | Library anchors and their root folders | Remember roots | F2 / first run |
| `games.json` | Offline VFS: name, source, exe, Steam URI or args, **ExtraLaunchArguments**, tags, user overrides | Launch and list | Scan / F4 save |
| `games_metadata.json` | Sidecar extras only (developer, engine, wiki URL, paths, cmdline catalog, video, scores) | Pane + F4 catalog | After a successful lookup |
| `collections.json` | Collections | Collections feature | Not written today (service unwired) |
| `startup.log` | Startup diagnostics | Support | Launch (unless `GC_STARTUP_LOGGING=0`) |
| `tag_colors.json` | Tag colours | UI | If the app updates colours |
| `blacklist.json` | Noise filter | Detection | Restored only when missing |
| `<name>.json.bak` | Previous copy of each JSON state file | Recovery | On every save |
| `<name>.json.corrupt` | Damaged bytes kept for inspection | Recovery | When a read fails to parse |
| `data/staging/` | Staged repair payload before copy | Verify bytes before touching a store | User-started repair |
| `data/backups/<utc-stamp>/` | Original Epic `.item` before modification | Restore on failure | User-started Epic repair |
| `data/repair-journal.jsonl` | One line per repair attempt, including refusals | Audit / support | User-started repair |

Field-by-field schemas for `settings.json`, `games.json`, and
`games_metadata.json` are defined in [DATA-FORMAT.md](DATA-FORMAT.md).

**Never written:** game files, registry, Start Menu, anything under `%APPDATA%` other than our own folder.
`%ProgramData%` and the Steam library are touched only by the two user-started repair actions below.

**JSON recovery:** each state file is written as `.bak` (previous copy) → write → read back and parse. On
read, a damaged file is kept as `.corrupt` and the `.bak` is restored. The app does not silently reset to an
empty catalogue. There is no cross-version schema guarantee: persisted state is disposable pre-release.

**User-started Steam ACF:** if a Steam folder is Orphaned and we have a numeric AppID (usually after F3), **Write Steam ACF** creates `{library}\steamapps\appmanifest_{id}.acf` with the identification fields only (`appid`, `name`, `installdir`, `StateFlags=4`, …). It refuses when the file already exists, when `libraryfolders.vdf` is missing, and when the folder is not a Steam library. We do not invent an AppID. We do not write depot blocks.

**User-started Epic `.item` repair:** updates `InstallLocation`, `ManifestLocation`, and an empty
`LaunchExecutable` in an `.item` Epic already wrote, under
`%ProgramData%\Epic\EpicGamesLauncher\Data\Manifests`. It never creates an `.item` and never replaces an
unreadable one; it backs the original up under `data/backups/` and restores it if the copy fails.
Registration — making Epic accept an app EOS does not report — is a separate, deferred subject.

**Repair safety:** both actions stage the payload in `data/staging/`, copy it to the target, compare
lengths, and append to `data/repair-journal.jsonl`. No directory outside `data/` is created; a missing
target folder is a refusal, not a creation.

`games.json` does **not** get wiki essays, covers, or scores. Those stay in the sidecar.

---

## 3. Local files / registry we READ

Only folders you add in F2, plus the well-known store keys already listed in `GamingCommander.Readme.txt` (Steam ACF, GOG `.info`, Epic `.item`, EA logs, Ubisoft, Battle.net, Rockstar, Xbox signals, selected HKLM install-path keys). All **read-only**.

Shipped read: `data/blacklist.json` and `data/tag_colors.json` (blacklist also embedded).  
Repo `testdata/` is tests/fixtures only — not copied to publish.

---

## 4. What we start (LAUNCH)

| Game | How | Why |
|------|-----|-----|
| Steam row with `steam://rungameid/{id}` | `Process.Start` that URI | Documented Steam launch. Overlay/cloud stay with Steam. Extra PCGW flags are **not** passed. |
| Everything else | `ExecutablePath` + `CommandLineArguments` + `ExtraLaunchArguments` | Direct exe. F4 checkboxes build extras. |
| Click config/save/game folder in the pane | `explorer.exe "X:\folder"` only | Clickable **only** under the game install dir, `%USERPROFILE%`, `%APPDATA%`, or `%LOCALAPPDATA%` (plus Ubisoft `savegames` / Steam `userdata` prefixes). Registry (`HKCU\…`) displays, not clickable. Not clickable: UNC, URLs, `%WINDIR%`, `..`, `.exe`. |

---

## 5. When lookup runs (if Online)

| Trigger | HTTP? |
|---------|--------|
| Highlight a game | No — cache only |
| F3 Lookup | Yes — force fetch; picker if several pages |
| F5 rescan done | Yes — enqueue every scanned game, one at a time |
| F4 | Background only if stale; dialog does not wait |
| Enter / launch | Game starts first; if sidecar stale, enqueue silently |
| Chip Offline or Lookup Disabled | Never |

---

## 6. Keys

| Key | Meaning |
|-----|---------|
| Esc / Backspace | Game list or **filter** → catalogue. No F9. |
| F3 | Fetch extras. Steam AppID → one PCGW page (no picker). Else name variants + pick if several pages. |
| F4 | Edit that game; may start a background fetch |
| F5 | Rescan folders; then metadata queue if Online |
| F8 / S | Filter: tags (user + PCGW genre/engine), store labels, wildcard. Clear / .. / Backspace restore the list. |
| F2 | Roots + **Enable online metadata** |
