# What GamingCommander reads, writes, and contacts

**This is the live contract.** If code and this file disagree, fix the code or this file — do not leave both.

Default: **offline.** If F2 **Enable online metadata** is off, there is **no HTTP at all** (no probe, no PCGW, no Steam Store). If it is on, HTTP runs only while the chip is **Online**.

We do **not** use SteamDB, PCGW Cargo, Epic GraphQL, or IGDB.

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
| **When** | Chip is **Online**, and (a) F5 finished a rescan and queued that game, or (b) you open **F4** for that game, or (c) you **launch** it and its sidecar is older than 30 days. **Not** when you only highlight a row. |
| **How** | 1. If we have a Steam AppID: `GET https://www.pcgamingwiki.com/api/appid.php?appid={id}` → page title from HTML `<title>`. 2. Else: `GET .../w/api.php?action=opensearch&search={display name}`. Soundtrack/demo titles are skipped. 3. `GET .../w/api.php?action=parse&prop=wikitext&page={title}`. |
| **Send** | AppID or the game’s display name. No account, no paths, no files. |
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

### 1.4 What we never contact

SteamDB, PCGW Cargo, Epic store GraphQL, IGDB, telemetry, update servers, crash reporters.

### 1.5 Cache (not a delete timer)

Results live in `data/games_metadata.json` with `LastUpdated`. If younger than **30 days**, we do not fetch again. Old rows are **kept** until a successful fetch overwrites them. A failed fetch does not wipe the row.

---

## 2. Local files we WRITE

Only under the app’s `data/` folder (next to the exe). Never in game installs.

| File | What | Why | When |
|------|------|-----|------|
| `settings.json` | Library roots, hidden folders, `EnableOnlineMetadata`, first-run / version | Remember setup | F2 close, first run |
| `games.json` | Offline VFS: name, source, exe, Steam URI or args, **ExtraLaunchArguments**, tags, user overrides | Launch and list | Scan / F4 save |
| `games_metadata.json` | Sidecar extras only (developer, engine, wiki URL, paths, cmdline catalog, video, scores) | Pane + F4 catalog | After a successful lookup |
| `startup.log` | Startup diagnostics | Support | Launch (unless `GC_STARTUP_LOGGING=0`) |
| `tag_colors.json` | Tag colours | UI | If the app updates colours |

**Never written:** game files, registry, Steam/GOG/Epic client data, Start Menu, `%APPDATA%` outside our folder.

`games.json` does **not** get wiki essays, covers, or scores. Those stay in the sidecar.

---

## 3. Local files / registry we READ

Only folders you add in F2, plus the well-known store keys already listed in `GamingCommander.Readme.txt` (Steam ACF, GOG `.info`, Epic `.item`, EA logs, Ubisoft, Battle.net, Rockstar, Xbox signals, selected HKLM install-path keys). All **read-only**.

Shipped read: `data/blacklist.json` (also embedded in the exe).

---

## 4. What we start (LAUNCH)

| Game | How | Why |
|------|-----|-----|
| Steam row with `steam://rungameid/{id}` | `Process.Start` that URI | Documented Steam launch. Overlay/cloud stay with Steam. Extra PCGW flags are **not** passed. |
| Everything else | `ExecutablePath` + `CommandLineArguments` + `ExtraLaunchArguments` | Direct exe. F4 checkboxes build extras. |
| Click config/save in the pane | `Process.Start` the resolved Windows folder | Open Explorer. Tokens like `{{p|userprofile\Documents}}` become `%USERPROFILE%\Documents\…`. |

We do not start browsers for PCGW except if you click a path that is not a folder (we refuse unresolved `{{…}}` tokens).

---

## 5. When lookup runs (if Online)

| Trigger | HTTP? |
|---------|--------|
| Highlight a game | No — cache only |
| F5 rescan done | Yes — enqueue every scanned game, one at a time |
| F4 | Yes — that game immediately (if stale) |
| Enter / launch | Game starts first; if sidecar stale, enqueue silently |
| Chip Offline or Lookup Disabled | Never |

---

## 6. Keys

| Key | Meaning |
|-----|---------|
| Esc / Backspace | Game list → catalogue (two levels). No F9. |
| F4 | Edit that game; may fetch extras |
| F5 | Rescan folders; then metadata queue if Online |
| F2 | Roots + **Enable online metadata** |
