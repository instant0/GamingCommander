# Phase 2.2: Game Metadata Lookup

## Goal

Implement F4 to look up structured metadata for the currently selected game from third-party sources. Populate the local game database with enriched data: developers, publishers, genres, release date, engine, and cover art. Provide a `GamingResourcesManifest` that documents all known data sources and their field formats so lookups are surgical — requesting only the fields we need, from the best source per field.

## Key Insight

The Python research tool (`tools/lookup_metadata.py`) validated the metadata pipeline end-to-end against real game data. Documented findings:
- **Cargo API** is the PRIMARY, fastest source (returns structured JSON)
- **Parse API** is the FALLBACK for fields NOT available in Cargo (`engine`, `modes`, `perspectives`, `series`, `themes`)
- **Epic games have NO Cargo field** for their store ID — name-only lookup required
- **Company: prefix** must be stripped from developer/publisher values
- **Rate limit**: 0.6s minimum interval between calls (HTTP 429 otherwise)

See `docs/findings/metadata-lookup-verification.md` for the full verification report.
See `tools/lookup_metadata.py` for the reference Python implementation.

---

## Architecture: Store-First Identification, PCGW for Enrichment

The lookup pipeline follows a **store-first** priority for a reason: the store
that sold you the game already knows what it is.  PCGamingWiki is for
**metadata enrichment** (engine, save paths, genres, taxonomy, known issues),
not primary identification.

### Priority by Store

| Store | Primary Identification | PCGW Role |
|-------|----------------------|-----------|
| **Epic** | Epic `searchStore(namespace)` → title, developer, publisher, cover art URL, description | Enrichment (engine, genres, save paths, Steam/GOG IDs) |
| **Steam** | Steam Store API `appdetails?appids=<AppID>` → name, developer, publisher, genres, release date | Enrichment (engine, save paths, taxonomy, known issues) |
| **GOG** | GOG API `products/<gameId>` → name, developer, publisher | Enrichment (engine, save paths, Steam AppID cross-ref) |
| **Unknown** | PE executable metadata (FileDescription/ProductName) → OpenSearch → PCGW fallback | Primary + enrichment |

### Name Candidate Priority (when no store ID is available)

1. **Store-resolved name** (Epic `searchStore`, Steam Store API, etc.)
2. **PE metadata** — `FileDescription` / `ProductName` from game executable
   (e.g. `SRTTR.exe` → `"Saints Row: The Third Remastered"`)
3. **Folder name** and PascalCase split variants
4. **Executable stem names** (e.g. `SRTTR` → generic match)
5. **Detect result entry** (from `detect_folder.py`)

### Cross-Verification during Name-Based Lookup

When a store ID is unavailable and OpenSearch returns multiple PCGW pages,
the scoring system uses external signals to pick the right one:

| Signal | Points | Example |
|--------|--------|---------|
| Epic publisher matches PCGW developer | +30 | "Crystal Dynamics" → Tomb Raider (2013) ✓ |
| Epic developer matches PCGW developer | +30 | "Sperasoft" → SR3 Remastered ✓ |
| PE CompanyName matches PCGW developer | +20 | "Kojima Productions Co., Ltd." → Death Stranding ✓ |
| Store-ID match | +25 | Steam AppID / GOGcom_ID |
| Release year proximity (Epic or exe timestamp) | +10 | Epic 2021 ≈ PCGW 2013? No → skip |
| Non-game page penalty | -50 | "Soundtrack", "Digital Book", etc. |
| Year tiebreaker | higher wins | (2013) beats (1996) |

### Safety: PE Name Blacklist & Non-Game Executable Filtering

**PE metadata** can contain generic or default values (Unreal Engine defaults
to ``ProductName="AppName"``). These are blocked by ``_PE_NAME_BLACKLIST``.

**Non-game executables** are filtered at two levels:

1. **Executable name patterns** (``NOISE_EXE_PARTS``) — substring match against
   the filename:
   - Universal noise: ``cleanup, touchup, crash, installer, unins, setup, redist, vcredist, dxsetup, oalinst, dotnet, directx, physx, eos, msi, msiexec, xna, ndp, dotnetfx``
   - Launcher stubs: ``launcher, updater, patcher, startup, bootstrapper``
   - Store bootstraps: ``galaxy, gog, epic, steam, origin, uplay, ubisoft``
   - Anti-cheat/DRM: ``easyanticheat, battleye, beclient, beservice, equ8, punkbuster, nprotect, xigncode, denuvo, vmprotect``
   - Unreal build/debug: ``crashreportclient, unrealcefsubprocess, symboldump, ubiquitous``

2. **Directory name patterns** (``_NOISE_DIR_PARTS`` / ``NOISE_DIR_PARTS``) —
   entire directories matching these are skipped during EXE scanning:
   ``__redist, _CommonRedist, redist, directx, vcredist, dotnet, physx, support, _installer, install, installer``

Applied in both ``tools/lookup_metadata.py`` and ``tools/detect_folder.py``.

### 1. PCGamingWiki — Primary Source (No API Key Required)

PCGamingWiki is a MediaWiki-based wiki with a structured **Cargo** database backend. The Cargo API allows SQL-style queries against typed tables.

**Base URL:** `https://pcgamingwiki.com/w/api.php`

**WARNING — Verified Cargo fields only:**
The Cargo API rejects unknown fields with an error. Only the fields listed below
are valid in the `Infobox_game` table. Fields like `Engine`, `Modes`,
`Perspectives`, `Series`, and `Epic_AppID` do NOT exist in Cargo and
will cause the entire query to fail. Use the **Parse API** for those.

**Validated Cargo fields for `Infobox_game`:**

| Cargo Field | Output Key | Type | Notes |
|---|---|---|---|
| `Developers` | `developers` | string | Prefix `Company:` must be stripped |
| `Publishers` | `publishers` | string (nullable) | May be `null` for some games |
| `Released` | `release_date` | string | `YYYY-MM-DD` or `YYYY-MM-DD;YYYY-MM-DD` (semicolons for multiple dates) |
| `Genres` | `genres` | string | Comma-separated, occasional trailing comma |
| `Steam_AppID` | `steam_appid` | string | Comma-separated (main + depots/DLC) |
| `GOGcom_ID` | `gogcom_id` | string | Comma-separated (main + DLCs); field name is `GOGcom_ID` not `GOG_ID` |
| `Cover` | `cover_url` | string | Relative wiki file path |

**Cargo lookup strategies:**

| When you have | WHERE clause |
|---|---|
| Steam AppID | `Infobox_game.Steam_AppID HOLDS "271590"` |
| GOG gameId | `Infobox_game.GOGcom_ID HOLDS "1205406003"` |
| PCGW page title | `Infobox_game._pageName="Everspace 2"` |

**NOT available in Cargo — use Parse API instead:**
- `Engine` — extract from `{{Infobox game/row/engine|...}}` in wikitext
- `Modes` — extract from `{{Infobox game/row/taxonomy/modes|...}}`
- `Perspectives`, `Themes`, `Series` — same pattern
- `Epic_AppID` — this field does **not exist** in PCGW at all;
  Epic games have no store-specific ID lookup path

**Rate limiting:**
- PCGW returns **HTTP 429 Too Many Requests** after 5-8 rapid calls
- Minimum safe interval: **600 milliseconds** between calls
- In C#, use `Task.Delay(600)` between sequential lookups
- Batch lookups must show progress during the delay

**Example queries:**

```http
# Query by Steam AppID — returns developers, publishers, release date, cover URL
https://pcgamingwiki.com/w/api.php?action=cargoquery
  &tables=Infobox_game
  &fields=Developers,Publishers,Released,Cover,Genres,Steam_AppID,GOGcom_ID
  &where=Infobox_game.Steam_AppID HOLDS "292030"
  &format=json

# Query by page name (exact)
https://pcgamingwiki.com/w/api.php?action=cargoquery
  &tables=Infobox_game
  &fields=Developers,Publishers,Released,Steam_AppID
  &where=Infobox_game._pageName="The Witcher 3 Wild Hunt"
  &format=json

# Query save file locations for a known page
https://pcgamingwiki.com/w/api.php?action=cargoquery
  &tables=Game_data/saves
  &fields=Game_data/saves.Path,Game_data/saves.Platform,
          Game_data/saves.Mandatory,Game_data/saves.Notes
  &where=Game_data/saves._pageName="The_Witcher_3:_Wild_Hunt"
  &format=json
```

**NOTES:**
- `_pageName` is NOT a valid field in the `fields` parameter (underscore
  prefix rejected). It works only in the `where` clause.
- The fields `Engine`, `Modes`, `Perspectives`, `Series` do NOT exist in the
  `Infobox_game` Cargo table. Include them and the API returns an error.
- `GOGcom_ID` (not `GOG_ID`) is the correct field name.
- `Epic_AppID` does not exist — Epic games have no store ID in PCGW Cargo.

**Redirect API** (Steam → PCGW page):
```
https://pcgamingwiki.com/api/appid.php?appid=292030
→ Returns redirect to PCGW page for The Witcher 3
```

**Key strengths:** No API key required. Fast structured queries by store ID.
Community-maintained fixes and compatibility notes.

**Key weaknesses:** Limited fields (engine, modes, save paths require Parse API).
Data quality varies. Not all games have complete entries. Cover images require
separate fetch. Rate limit at ~8 calls/minute without delay.

### PCGW Parse API Fallback (for Engine, Modes, Perspectives)

Fields not available in Cargo can be extracted from the raw wiki markup:

```
https://www.pcgamingwiki.com/w/api.php?action=parse
  &page=Cyberpunk_2077&prop=wikitext&format=json
```

Parse the `{{Infobox game}}` block for:
- `engine` — `{{Infobox game/row/engine|Unreal Engine 4}}`
- `developers` — `{{Infobox game/row/developer|CD Projekt Red}}`
- `publishers` — `{{Infobox game/row/publisher|CD Projekt}}`
- `release_dates` — `{{Infobox game/row/date|Windows|December 10, 2020}}`
- `modes` — `{{Infobox game/row/taxonomy/modes|Singleplayer}}`
- `perspectives`, `themes`, `genres`, `series`

**Strengths:** Complete infobox access. No API key. No rate limit beyond the
shared PCGW limit.

**Weaknesses:** One API call per game per lookup (slower than Cargo). Wikitext
parsing is fragile (nested templates, ref tags, wiki markup). Extra latency
for the rate limit delay.

---

### 2. SteamDB — Secondary Source (No API Key Required)

SteamDB publishes a skimmed JSON data dump at `https://nebukam.github.io/steam-db/app/<AppID>/infos.json` (public GitHub Pages mirror).

**Available fields:** `name`, `appid`, `parentappid`, `flags`, `tags`, `cooptimus` (multiplayer player counts).

**Example:**
```json
{
  "appid": 292030,
  "name": "The Witcher 3: Wild Hunt",
  "parentappid": "",
  "flags": [2, 36, 38, 29, 35, 18, 41, 42, 1, 9, 20],
  "tags": ["RPG", "Open World", "Story Rich", ...],
  "cooptimus": { "players": "2-8", ... }
}
```

**Key strengths:** No API key. Fast JSON. Good for tag/signal data.

**Key weaknesses:** Limited metadata (no developers, publishers, release date directly). Tag data is noisy.

### 3. IGDB — Richest Source (API Key Required)


IGDB is the industry-standard game database. It requires Twitch developer credentials (`Client-ID` + `Client-Secret`) to obtain an OAuth bearer token.

**Query method:** POST to `https://api.igdb.com/v4/games` with an APICalypse query.

**Key fields for GamingCommander:**

| Field | Use |
|---|---|
| `name` | Display name |
| `summary` | Short description |
| `involved_companies` (with developer/publisher filter) | Developer and publisher names |
| `genres` | Genre names |
| `cover.url` | Cover art URL |
| `screenshots` | Screenshot URLs |
| `first_release_date` | Release date |
| `total_rating` | IGDB score |
| `platforms` | Supported platforms |
| `game_modes` | Single/multiplayer |
| `websites.url` where `category = 1` | Official website |

**Authentication flow:**
1. POST `https://id.twitch.tv/oauth2/token` with `client_id`, `client_secret`, `grant_type=client_credentials`
2. Receive `access_token`
3. Use `access_token` in `Authorization: Bearer <token>` header on IGDB requests
4. Token expires — refresh as needed

**Key strengths:** Most comprehensive metadata. Structured. High data quality.

**Key weaknesses:** Requires API key registration. Rate limits apply. Requires OAuth flow.

### 4. Steam Store Web API — Free, No Key

Valve's public Steam Web API provides basic metadata for games via `https://store.steampowered.com/api/appdetails?appids=<AppID>`. Returns: `type`, `name`, `required_age`, `is_free`, `detailed_description`, `about_the_game`, `short_description`, `header_image`, `website`, `developers`, `publishers`, `genres`, `release_date`, `metacritic`.

**Key strengths:** Free, no key, reliable.

**Key weaknesses:** No structured schema. HTML-formatted descriptions. No save locations.

### 5. Other Sources (Future)

- **MobyGames:** Good for older titles, save locations. No public API — scraping required.
- **HowLongToBeat (HLTB):** Play time data. Unofficial API or scraping.
- **Wikidata:** SPARQL endpoint. Cross-reference for developer/publisher authority data.

---

## UX Model

### F4 — Game Metadata Lookup

1. User selects a game in the left pane.
2. User presses F4.
3. If outbound connections are disabled (user preference), show "Network
   lookup disabled — enable in Settings."
4. Status line shows "Looking up: <Game Name>...".
5. The app queries data sources in priority order:
   - **Store ID known (Steam AppID / GOG gameId)** → PCGW Cargo by store ID
   - **Store ID unknown** → PCGW OpenSearch by game name → Cargo by page name
   - **Cargo misses fields** → PCGW Parse API for engine/modes/perspectives
   - **PCGW fails** → SteamDB JSON (if Steam AppID known)
   - **Still missing** → Steam Store API (if Steam AppID known)
   - **Optional** → IGDB (if credentials configured)
6. **Rate limiting:** 600ms delay enforced between each PCGW API call.
   Batch lookups show a progress bar during the wait.
7. Results populate the details panel with enriched fields.
8. Data is cached to the local game database (`data/games_db.json`).
9. Status line shows "Found: <Game Name> — X fields retrieved" or "No data found".

### Details Panel — Enriched Fields (after lookup)

| Field | Source Priority |
|---|---|
| Developer | PCGW > IGDB |
| Publisher | PCGW > IGDB |
| Genre(s) | PCGW > IGDB |
| Release Date | PCGW > Steam Store > IGDB |
| Cover Art URL | PCGW > IGDB |
| Official Website | IGDB > Steam Store |
| Steam App ID | Derived from ACF / manifest |
| Save Location(s) | PCGW `Game_data/saves` table |
| Config Location(s) | PCGW `Game_data/config` table |
| IGDB Score | IGDB `total_rating` |
| System Requirements | Steam Store > IGDB |
| Compatible Launchers | PCGW infobox (available_on fields) |

### Local Game Database

The enriched data is persisted to `data/games_db.json`:

```json
{
  "games": {
    "gog-1205406003": {
      "primaryKey": "gog-1205406003",
      "steamAppId": "1128920",
      "gogGameId": "1205406003",
      "epicCatalogItemId": null,
      "title": "EVERSPACE 2",
      "developer": "ROCKFISH Games",
      "publisher": null,
      "genres": ["Action", "Shooter", "Vehicle combat"],
      "releaseDate": "2023-04-06",
      "engine": "Unreal Engine 4",
      "coverUrl": "Everspace 2 cover.png",
      "pcgwUrl": "https://pcgamingwiki.com/wiki/Everspace_2",
      "saveLocations": [],
      "lastUpdated": "2026-07-09T12:00:00Z"
    }
  }
}
```

Data is merged on subsequent lookups — new fields overwrite old ones without nuking fields not returned by the source.

---

## GamingResourcesManifest

A C# static class or JSON file that serves as the authoritative registry of all known data sources, their query formats, and field schemas. This makes adding new sources surgical — you update the manifest, not scattered code.

**Structure:**
```csharp
public static class GamingResourcesManifest
{
    public static readonly IReadOnlyList<MetadataSource> Sources = [
        new MetadataSource
        {
            Name = "PCGamingWiki",
            Priority = 1,
            IsFree = true,
            RequiresApiKey = false,
            BaseUrl = "https://pcgamingwiki.com/w/api.php",
            LookupMethods = [
                new LookupMethod
                {
                    Key = "SteamAppID",
                    QueryTemplate = "action=cargoquery&tables=Infobox_game&fields=...&where=Infobox_game.Steam_AppID+HOLDS+\"{0}\"&format=json",
                    ResponseParser = "ParseCargoJson"
                }
            ],
            KnownFields = ["Developers", "Publishers", "Released", "Cover_URL", "Genres", "Steam_AppID", "GOG_ID"]
        },
        new MetadataSource
        {
            Name = "SteamDB",
            Priority = 2,
            IsFree = true,
            RequiresApiKey = false,
            BaseUrl = "https://nebukam.github.io/steam-db/app/{0}/infos.json",
            LookupMethods = [
                new LookupMethod { Key = "SteamAppID", QueryTemplate = "...", ResponseParser = "ParseSteamDbJson" }
            ],
            KnownFields = ["name", "tags", "cooptimus"]
        },
        // ...
    ];
}
```

The `IGameMetadataProvider` iterates through `Sources` in priority order, tries each lookup method, and merges results into the local game database.

---

## Tasks

### 1. GamingResourcesManifest

- [ ] Define `MetadataSource` and `LookupMethod` records in `GamingCommander.Core`.
- [ ] Populate manifest with PCGamingWiki, SteamDB, Steam Store, IGDB sources.
- [ ] Document query templates and field schemas for each source.
- [ ] Make manifest extensible — new sources added by updating the static list, not scattered code.

### 2. IGameMetadataProvider Interface

- [ ] Define `IGameMetadataProvider.Lookup(IGame game)` returning `GameMetadata`.
- [ ] Define `GameMetadata` record with all enrichable fields (nullable).
- [ ] Define `Merge(GameMetadata existing, GameMetadata incoming)` — new values overwrite old, old values with no new value are preserved.

### 3. PCGamingWiki Provider (Primary)

- [ ] Implement `PCGWMetadataProvider`:
  - **Priority 1:** Resolve by store ID → Cargo API
    - Steam AppID → `Infobox_game.Steam_AppID HOLDS "<appid>"`
    - GOG gameId → `Infobox_game.GOGcom_ID HOLDS "<gameId>"`
  - **Priority 2:** Resolve by game name → OpenSearch → Cargo by `_pageName`
  - **Priority 3:** Cargo miss on engine/modes → Parse API wikitext extraction
  - Strip `Company:` prefix from developer/publisher values
  - Split comma-separated IDs; use first as primary
  - Handle `null` publishers gracefully
- [ ] Rate limit: enforce **600ms minimum interval** between PCGW API calls
- [ ] Cache results to local game database on success
- [ ] No API key required.
- [ ] Handle gracefully when no PCGW entry exists.
- [ ] **Known limitation:** Epic games have no Cargo-field ID mapping; name-only lookup only
- [ ] **IMPORTANT:** For Epic games, use `searchStore` by `CatalogNamespace` to get publisher name
   and release year — then cross-check against PCGW developers to disambiguate when multiple
   pages match the same base name (e.g. "Tomb Raider (1996)" vs "Tomb Raider (2013)").
   The Epic `publisherDisplayName` field reliably identifies the correct game.
- [ ] **Tiebreaker strategy:** When multiple PCGW pages have the same base name and no
   store ID can verify:
   1. Epic publisher → PCGW developer match (+30)
   2. PE CompanyName → PCGW developer match (+20)
   3. Release year proximity between PCGW page and Epic/exe timestamp (+10)
   4. Higher release year wins (likely the modern re-release)
   5. PCGW OpenSearch ranking as final tiebreaker
- [ ] **File timestamps:** Extract the modification year from game executables in the folder.
   Use as year-proximity tiebreaker when PCGW pages have parenthetical years.
- [ ] **PE metadata:** Extract `CompanyName` from the game's main executable.
   Cross-check against PCGW developer field for verification.

### 4. SteamDB Provider (Secondary)

- [ ] Implement `SteamDBMetadataProvider`:
  - Fetch `https://nebukam.github.io/steam-db/app/<AppID>/infos.json`.
  - Extract `name`, `tags`, `cooptimus`.
- [ ] No API key required.

### 5. IGDB Provider (Rich, Optional)

- [ ] Implement `IGDBMetadataProvider`:
  - OAuth2 client credentials flow to obtain bearer token.
  - Search by `Steam App ID` or game name.
  - Extract: developer, publisher, genres, cover URL, summary, release date, rating.
- [ ] Requires `Client-ID` and `Client-Secret` — document setup in `docs/igdb-setup.md`.
- [ ] Graceful degradation when no IGDB key is configured.

### 6. Steam Store Provider (Free, No Key)

- [ ] Implement `SteamStoreMetadataProvider`:
  - Fetch `https://store.steampowered.com/api/appdetails?appids=<AppID>`.
  - Extract: developers, publishers, genres, release date, header image.
- [ ] No API key required.

### 7. Local Game Database

- [ ] Define `games_db.json` schema.
- [ ] Implement `LocalGameDatabase` service:
  - `Load()` — read from `data/games_db.json`.
  - `Save()` — write to `data/games_db.json`.
  - `Get(gameId)` — retrieve cached metadata.
  - `Upsert(gameId, GameMetadata)` — merge new data into existing.
- [ ] Merge strategy: only overwrite fields that are non-null in the incoming data.

### 8. F4 Integration in ShellViewModel

- [ ] Wire F4 key to call `metadataProvider.Lookup(selectedGame)`.
- [ ] Update details panel with enriched fields on completion.
- [ ] Show status line progress and result summary.
- [ ] Cache results to local game database.

### 9. Details Panel — Enriched Display

- [ ] Add enriched fields to the right pane bindings:
  - Developer, Publisher, Genre.
  - Save Locations (expandable list).
  - PCGW link (clickable).
  - Cover art (if URL available).
- [ ] Show "No metadata" state when lookup returns nothing.

---

## Deliverables

- [ ] `GamingCommander.Core/GamingResourcesManifest.cs` — registry of all data sources
- [ ] `IGameMetadataProvider` interface and `GameMetadata` record
- [ ] `PCGWMetadataProvider` (primary, no key)
- [ ] `SteamDBMetadataProvider` (secondary, no key)
- [ ] `SteamStoreMetadataProvider` (tertiary, no key)
- [ ] `IGDBMetadataProvider` (optional, key required)
- [ ] `LocalGameDatabase` service with merge logic
- [ ] F4 wired in `ShellViewModel`
- [ ] `data/games_db.json` caching layer
- [ ] Enriched details panel display
- [ ] `docs/igdb-setup.md` for IGDB key configuration
- [ ] App builds and runs cleanly on Windows

---

## Exit Criteria

Phase 2.2 is complete when:
- F4 triggers a metadata lookup for the selected game.
- PCGW is queried first (no key needed), with graceful fallback to SteamDB and Steam Store.
- IGDB is wired if credentials are configured.
- Results are displayed in the details panel and cached to `data/games_db.json`.
- Merge logic preserves fields from earlier successful lookups.
- The app builds and runs cleanly on Windows.
