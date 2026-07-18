# Phase 2.2: Game Metadata Lookup

## Goal

Implement F4 to look up structured metadata for the currently selected game from third-party sources. Populate the local game database with enriched data: developers, publishers, genres, save locations, system requirements, and cover art. Provide a `GamingResourcesManifest` that documents all known data sources and their field formats so lookups are surgical — requesting only the fields we need, from the best source per field.

## Key Insight

The Python helper scripts in Phase 1.2 are **development-environment-only** tools for validating parsing approaches. They are never disclosed to the Agent and never write concrete data back to the project. This phase is the **runtime C# feature** — the actual `IGameMetadataProvider` that users interact with in the app. Both must be understood as separate concerns.

---

## Data Sources

### 1. PCGamingWiki — Primary Source (No API Key Required)

PCGamingWiki is a MediaWiki-based wiki with a structured **Cargo** database backend. The Cargo API allows SQL-style queries against typed tables.

**Base URL:** `https://pcgamingwiki.com/w/api.php`

**Relevant tables and fields:**

| Table | Key Fields | Lookup Key |
|---|---|---|
| `Infobox_game` | `Name`, `Developers`, `Publishers`, `Released`, `Cover_URL`, `Genres`, `Themes`, `Steam_AppID`, `GOG_ID`, `Epic_AppID`, `Uplay_ID`, `Modes`, `Perspectives`, `Engine`, `Series` | `Steam_AppID`, `GOG_ID`, `Epic_AppID`, or page name |
| `Game_data/saves` | `Path`, `Platform`, `Mandatory`, `Notes` | Page name |
| `Game_data/config` | `Path`, `Platform`, `Mandatory`, `Notes` | Page name |
| `Infobox_game` | `Steam_AppID` | Cargo query by `Steam_AppID HOLDS "<AppID>"` |
| `Infobox_game` | `GOG_ID` | Cargo query by `GOG_ID HOLDS "<ProductID>"` |

**Example queries:**

```http
# Query by Steam AppID — returns page name, developers, publishers, release date, cover URL
https://pcgamingwiki.com/w/api.php?action=cargoquery
  &tables=Infobox_game
  &fields=Infobox_game._pageName=Page,Infobox_game.Developers,
          Infobox_game.Publishers,Infobox_game.Released,
          Infobox_game.Cover_URL,Infobox_game.Genres
  &where=Infobox_game.Steam_AppID HOLDS "292030"
  &format=json

# Query save file locations for a known page
https://pcgamingwiki.com/w/api.php?action=cargoquery
  &tables=Game_data/saves
  &fields=Game_data/saves.Path,Game_data/saves.Platform,
          Game_data/saves.Mandatory,Game_data/saves.Notes
  &where=Game_data/saves._pageName="The_Witcher_3:_Wild_Hunt"
  &format=json
```

**Redirect API** (Steam → PCGW page):
```
https://pcgamingwiki.com/api/appid.php?appid=292030
→ Returns redirect to PCGW page for The Witcher 3
```

**Key strengths:** No API key required. Comprehensive save/config file locations. Community-maintained fixes and compatibility notes.

**Key weaknesses:** Data quality varies. Not all games have complete entries. Cover images require separate fetch.

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
3. Status line shows "Looking up: <Game Name>...".
4. The app queries data sources in priority order:
   - **Steam AppID known** → PCGW Cargo (by AppID) → SteamDB JSON → IGDB (by AppID)
   - **Steam AppID unknown** → PCGW Cargo (by game name search) → Steam Store API → IGDB (by name search)
5. Results populate the details panel with enriched fields.
6. Data is cached to the local game database (`data/games_db.json`).
7. Status line shows "Found: <Game Name> — X fields retrieved" or "No data found".

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
    "steam-292030": {
      "steamAppId": "292030",
      "title": "The Witcher 3: Wild Hunt",
      "developer": "CD Projekt RED",
      "publisher": "CD Projekt",
      "genres": ["RPG", "Open World"],
      "releaseDate": "2015-05-18",
      "coverUrl": "https://pcgamingwiki.com/.../cover.jpg",
      "pcgwUrl": "https://pcgamingwiki.com/wiki/The_Witcher_3:_Wild_Hunt",
      "saveLocations": [
        { "platform": "Windows", "path": "%USERPROFILE%\\Documents\\The Witcher 3\\gamesaves" }
      ],
      "igdbId": null,
      "lastUpdated": "2026-04-17T12:00:00Z"
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
  - Resolve Steam AppID to PCGW page via `https://pcgamingwiki.com/api/appid.php?appid=<AppID>`.
  - Query `Infobox_game` table via Cargo API by `Steam_AppID`.
  - Query `Game_data/saves` table by page name.
  - Map PCGW field names to `GameMetadata` fields.
- [ ] No API key required.
- [ ] Handle gracefully when no PCGW entry exists.

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
