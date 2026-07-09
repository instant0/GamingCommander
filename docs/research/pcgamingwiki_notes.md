# PCGamingWiki Integration — Notes

> **Last updated:** 2026-07-09
> **Status:** Updated with Cargo API field verification and rate-limit findings

## Purpose

Preliminary notes on how PCGamingWiki (PCGW) metadata could be integrated into
GamingCommander for enhanced game information and launch arguments.

## API Endpoints

| Endpoint | URL | Use |
|----------|-----|-----|
| MediaWiki API | `https://www.pcgamingwiki.com/w/api.php` | All queries |
| REST API | `https://www.pcgamingwiki.com/api/rest_v1/` | Not tested (404 on summary endpoint) |

## Data Access Methods (Tested & Working)

### 1. Cargo Query API (Primary — Structured)

Query the `Infobox_game` table by store ID or page name.

**Base query:**

```
https://www.pcgamingwiki.com/w/api.php?action=cargoquery
  &tables=Infobox_game
  &fields=Developers,Publishers,Released,Genres,Steam_AppID,GOGcom_ID,Cover
  &where=Infobox_game.Steam_AppID HOLDS "271590"
  &format=json&limit=1
```

**Validated Cargo fields for `Infobox_game`:**

| Cargo Field | Output Key | Example Value | Notes |
|-------------|------------|---------------|-------|
| `Developers` | `developers` | `Company:ROCKFISH Games` | Prefix `Company:` should be stripped |
| `Publishers` | `publishers` | `Company:505 Games` | Prefix `Company:` should be stripped |
| `Released` | `release_date` | `2020-07-14;2023-06-01` | Semicolons separate base + DLC dates |
| `Genres` | `genres` | `Action,Shooter,Vehicle combat,` | Trailing comma in some entries |
| `Steam_AppID` | `steam_appid` | `1190460,1258500,1258510` | Comma-separated (main + depots/DLC) |
| `GOGcom_ID` | `gogcom_id` | `1205406003,1073861653,...` | Comma-separated (main + DLCs) |
| `Cover` | `cover_url` | `Death Stranding cover.jpg` | Relative wiki file name |

**NOT available in Cargo** (the API rejects unknown fields):

- `Engine` — use Parse API fallback
- `Modes`, `Perspectives`, `Series` — use Parse API fallback
- `Epic_AppID` — this field does **not** exist in the Cargo table
- `_pageName` — cannot be used in field list (underscore prefix rejected);
  can be used in `where` clause

**WHERE clause operators:**

- `=` — exact match (for `_pageName`)
- `HOLDS` — multi-value field match (for `Steam_AppID`, `GOGcom_ID`)
- Field names in `where` are PascalCase with underscores: `Steam_AppID`,
  `GOGcom_ID`, `_pageName`

**Known quirks:**
- "Company:" prefix on developer/publisher values must be stripped client-side
- `Publishers` may be `null` for some games
- Multi-value fields return comma-separated strings
- Release date may have a `Released__precision` companion field

### 2. OpenSearch API (Name Discovery)

```
https://www.pcgamingwiki.com/w/api.php?action=opensearch
  &search=Death+Stranding&limit=3&namespace=0&format=json
```

Returns `[query, [titles...], [urls...], [urls...]]`.

**Limitations:**
- Search terms must closely match the PCGW page title
- Internal codenames (e.g. Epic `AppName: "Boga"`) should NOT be used as
  search terms — they can match the wrong game
- CamelCase splitting (e.g. `DeathStranding` → `Death Stranding`) is
  essential as a preprocessing step for folder-name-only lookups

### 3. Parse API (Raw Wikitext Fallback)

```
https://www.pcgamingwiki.com/w/api.php?action=parse
  &page=Cyberpunk_2077&prop=wikitext&format=json
```

Returns the raw wiki markup. Parse the `{{Infobox game}}` block for
fields not available in Cargo:

- `engine` (via `{{Infobox game/row/engine|...}}`)
- `modes` (via `{{Infobox game/row/taxonomy/modes|...}}`)
- `perspectives`, `themes`, `series`
- `release_date` for Windows (via `{{Infobox game/row/date|Windows|...}}`)
- `steam appid`, `gogcom id`, `official site`, etc.

**Limitations:**
- Rate-limit heavy (one call per game name)
- Wikitext parsing is fragile (nested templates, ref tags, wiki markup)
- Infobox may not exist on disambiguation/list pages

## Data Mapping

PCGW "Infobox game" fields → GamingCommander GameMetadata model:

| Cargo / Parse Field | GameMetadata Property | Source |
|---------------------|-----------------------|--------|
| `Developers` | `Developer` | Cargo (strip "Company:") |
| `Publishers` | `Publisher` | Cargo (strip "Company:") |
| `Released` | `ReleaseDate` | Cargo (first date) |
| `Genres` | `Genres` | Cargo (split commas) |
| `Steam_AppID` | `SteamAppId` | Cargo (first ID) |
| `GOGcom_ID` | `GogGameId` | Cargo (first ID) |
| `Cover` | `CoverUrl` | Cargo (relative path) |
| `engine` (parse) | `Engine` | Parse API |
| `modes` (parse) | `Modes` | Parse API |
| `epic games launcher` | `EpicAppId` | Not available as numeric ID |

## Lookup Pipeline (Confirmed Working Order)

```
1. GOG Steam AppID known
   → Cargo by Steam_AppID HOLDS "12345"
   ✓ Verified (Cyberpunk 2077, No Man's Sky, Everspace 2, GTA V)

2. GOG gameId known
   → Cargo by GOGcom_ID HOLDS "12345"
   ✓ Verified (No Man's Sky, Everspace 2)

3. Only folder name / game name
   → OpenSearch → Cargo by page name → Parse API fallback
   ✓ Verified (Arx Fatalis via "arx", Death Stranding via "DeathStranding")

4. No name match found
   → PE metadata scan for FileDescription/ProductName
   (Verified: mock exes return empty; needs real Windows test)
```

## Rate Limiting

- During testing, PCGW returns **HTTP 429 Too Many Requests** after
  approximately 5-8 rapid calls without delay
- Minimum safe interval: **0.6 seconds** between calls (tested stable)
- Recommended implementation in C#: `Task.Delay(600)` between sequential lookups
- Batch mode processes one game at a time with delays — 6 games takes ~30-40s

## Lookup by Store ID (Strategy)

| Store | Available Identifier | Cargo Field | Works? |
|-------|---------------------|-------------|--------|
| Steam | Steam AppID | `Steam_AppID HOLDS` | ✓ Yes |
| GOG | gameId / rootGameId | `GOGcom_ID HOLDS` | ✓ Yes |
| Epic | CatalogItemId / AppName | No Cargo field for Epic | ✗ Name-only |
| Rockstar | RGL title ID | No Cargo field | ✗ Name-only |
| EA | None | N/A | ✗ Name-only |
| Ubisoft | None | N/A | ✗ Name-only |
| Blizzard | None | N/A | ✗ Name-only |
| Xbox | None | N/A | ✗ Name-only |

## Caching Strategy

PCGW data should be cached locally to avoid:
- Excessive API calls
- Network dependency for basic display
- Rate-limit errors

Suggested cache format:

```json
{
  "gameId": "gog_1205406003",
  "pcgwTitle": "Everspace 2",
  "lastFetched": "2026-07-09T10:30:00Z",
  "developer": "ROCKFISH Games",
  "publisher": "",
  "releaseDate": "2023-04-06",
  "genres": ["Action", "Shooter", "Vehicle combat"]
}
```

Store cache in `%LocalAppData%\GamingCommander\cache\pcgw\`.

## Out of Scope (Current Phase)

- Live PCGW API calls from the application — use local cache, opt-in only
- IGDB integration (requires API key)
- Cover art download caching
- Save data backup/restore using PCGW paths

## References

- PCGamingWiki API: https://www.pcgamingwiki.com/w/api.php
- MediaWiki API docs: https://www.mediawiki.org/wiki/API:Main_page
- Cargo extension docs: https://www.mediawiki.org/wiki/Extension:Cargo
- Research tool: `tools/lookup_metadata.py`
- Verification findings: `docs/findings/metadata-lookup-verification.md`
