# Metadata Lookup Verification

> **Date:** 2026-07-09
> **Tool:** `tools/lookup_metadata.py`
> **Data:** `/mnt/e/games` (real game library) + `data/mock/` (synthetic test fixtures)

## Objective

Validate the end-to-end metadata lookup pipeline before committing to a C#
implementation. The pipeline is:

```
Game folder → detect store signal → extract identifiers
    → PCGW Cargo query (by store ID) → PCGW Parse API (fallback)
    → PE metadata scan → merged structured output
```

## Test Methodology

1. Run `detect_folder.py` against `/mnt/e/games` (38 real games, previously
   verified at 100% detection accuracy).
2. Run `lookup_metadata.py` in batch mode against the detection output.
3. For each game, verify: correct PCGW page found, developer/publisher/genre
   extracted, store IDs match.
4. Also test individual folders and mock data.

## Test Results

### GOG Games (3 tested — 3/3 success)

| Folder | Identified As | Method | Developer | Release | GOG ID Match? |
|--------|---------------|--------|-----------|---------|---------------|
| `Everspace2` | EVERSPACE 2 | Cargo by GOGcom_ID | ROCKFISH Games | 2023-04-06 | ✓ 1205406003 |
| `NMS` | No Man's Sky | Cargo by GOGcom_ID | Hello Games | 2016-08-12 | ✓ 1446213994 |
| `arx` | Arx Fatalis | OpenSearch → Cargo by page | Arkane Studios | 2002-06-28 | ✓ 1207658680 |

**Key finding:** GOG games are the most reliably identified because the
`goggame-<gameId>.info` file provides a numeric gameId that maps directly to
the PCGW `GOGcom_ID` Cargo field.

### Epic Games (3 tested — 1/3 success)

| Folder | Identified As | Method | Developer | Notes |
|--------|---------------|--------|-----------|-------|
| `DeathStranding` | Death Stranding | OpenSearch → Cargo | Kojima Productions,Guerrilla Games | ✓ Correct |
| `TombRaiderGOTYE` | Not found | — | — | OpenSearch found "Tomb Raider" but infobox mismatch |
| `sr3rmx` | Not found | — | — | Cryptic folder name, no identifiers |

**Key finding:** Epic games have no PCGW Cargo field for their store ID.
Identification relies on folder name matching. The `.mancpn` file's `AppName`
is an internal codename (e.g. "Boga" for Death Stranding) that should NOT be
used as a search term.

### Steam Games (not in /mnt/e/games as individual folders)

Steam games are detected at the library level (not per-folder). The standalone
detection pipeline for Steam games will need the Steam AppID from
`steam_appid.txt` or the `.acf` manifest, which maps to PCGW `Steam_AppID`.

**Verified via direct API tests:**
- Steam AppID `271590` → GTA V: Developer `Rockstar North`, Publisher
  `Rockstar Games`
- Steam AppID `1091500` → Cyberpunk 2077: Developer `CD Projekt Red`,
  Publisher `CD Projekt`
- Steam AppID `275850` → No Man's Sky: Developer `Hello Games`

### Other Stores (not tested with metadata lookup)

These stores were detected by `detect_folder.py` but have no store-specific ID
that maps to PCGW. They would rely on name-based lookup:

- Blizzard (`.battle.net/`) — 2 games
- Ubisoft (`uplay*`) — 4 games
- EA (`__Installer/`) — 3 games
- Rockstar (`title.rgl`) — 1 game
- Xbox (`default-metadata.json`) — 1 game
- Steam Emulator (steam_api64.dll / emu.ini) — 11 games
- Standalone (exe/lnk) — 5 games

## Cargo API Findings

### Validated Field Names

| Cargo Field | Output Key | Validated | Notes |
|-------------|------------|-----------|-------|
| `Developers` | `developers` | ✓ | Prefix "Company:" must be stripped |
| `Publishers` | `publishers` | ✓ | May be `null` for some games |
| `Released` | `release_date` | ✓ | Format: `YYYY-MM-DD` or `YYYY-MM-DD;YYYY-MM-DD` |
| `Genres` | `genres` | ✓ | Comma-separated, occasional trailing comma |
| `Steam_AppID` | `steam_appid` | ✓ | Space in output key: "Steam AppID" |
| `GOGcom_ID` | `gogcom_id` | ✓ | Space in output key: "GOGcom ID" |
| `Cover` | `cover_url` | ✓ | Relative wiki file path |

### Rejected Fields

These fields cause Cargo to return an error and abort the entire query:

- `Engine` — not in `Infobox_game` table
- `Modes`, `Perspectives`, `Series` — not in `Infobox_game` table

Use the **Parse API** (`action=parse&prop=wikitext`) instead to extract these
from the `{{Infobox game}}` wiki markup.

### WHERE Clause Syntax

```
# By store ID (multi-value)
Infobox_game.Steam_AppID HOLDS "271590"

# By page name (exact)
Infobox_game._pageName="Everspace 2"
```

Note: `_pageName` uses spaces, not underscores in the value but PascalCase
with underscore in the field name.

## Rate Limiting

- PCGW returns **HTTP 429 Too Many Requests** after rapid calls (5-8 requests
  without delay)
- Minimum safe interval: **0.6 seconds** between calls
- A batch of 38 games at 0.6s/call = ~23 seconds minimum
- In C# implementation, consider: configurable delay, batch processing with
  progress reporting, and local caching to avoid re-fetching

## PE Metadata Scan

- Tested against mock `.exe` files (1-byte stubs): returns empty correctly
- Not tested against real game executables (environment is Linux; PE parsing
  requires actual Windows PE files)
- Recommended PE fields: `FileDescription`, `ProductName`, `CompanyName`,
  `FileVersion`
- Primary executable selection heuristic: score by size + name match to folder

## Recommendations for C# Implementation

### 1. Field Mapping

```csharp
public record GameMetadata
{
    // From store identifiers
    public string? SteamAppId { get; init; }
    public string? GogGameId { get; init; }
    public string? EpicCatalogItemId { get; init; }

    // From PCGW Cargo (primary)
    public string? Developer { get; init; }   // strip "Company:" prefix
    public string? Publisher { get; init; }   // strip "Company:" prefix
    public string? ReleaseDate { get; init; }
    public List<string>? Genres { get; init; }
    public string? CoverUrl { get; init; }

    // From PCGW Parse API (fallback)
    public string? Engine { get; init; }
    public List<string>? Modes { get; init; }

    // From PE metadata (local)
    public string? FileDescription { get; init; }
    public string? CompanyName { get; init; }

    // Cache metadata
    public DateTime? LastFetched { get; init; }
    public string? PcgwPageUrl { get; init; }
}
```

### 2. Lookup Priority

```
1. Store ID known → Cargo by Steam_AppID / GOGcom_ID
2. Game name known → OpenSearch → Cargo by _pageName
3. OpenSearch found page but Cargo failed → Parse API wikitext
4. No name match → scan PE metadata for FileDescription/ProductName
```

### 3. Rate Limit & Caching

- Enforce 600ms minimum delay between PCGW API calls
- Cache results locally in `%LocalAppData%\GamingCommander\cache\pcgw\`
- Expose as OPT-IN user preference (outbound connections)
- Show cached data immediately; refresh on explicit user action (F4)

### 4. Edge Cases

- **Publisher is null** in Cargo → leave unset, do not display "None"
- **Multiple comma-separated IDs** → use first ID as primary, store rest as
  secondary
- **Semicolon-separated release dates** → use earliest date as release, store
  rest as notes
- **Company: prefix** → strip from developer/publisher strings
- **Epic games** → no Cargo field exists for Epic IDs — name-only lookup
- **Disambiguation pages** → OpenSearch may find a page without infobox data;
  skip and try next candidate

## Test Assets

- **Research tool:** `tools/lookup_metadata.py`
- **Detection output (38 games):** `docs/findings/detect-folder-verification.md`
- **Subset lookup results:** `/tmp/subset_lookup2.json` (6 GOG+Epic games)
- **PCGW research notes:** `docs/research/pcgamingwiki_notes.md`
- **Mock detection fixtures:** `data/mock/`
