# PCGamingWiki Integration — Notes

## Purpose
Preliminary notes on how PCGamingWiki (PCGW) metadata could be integrated into GamingCommander for enhanced game information and launch arguments.

## Background

PCGamingWiki (pcgamingwiki.com) is a community-maintained wiki covering technical details for thousands of PC games, including:
- Configuration file locations
- Save game paths
- Graphics/performance tweaks
- Known issues and fixes
- Launch arguments
- Store/DRM information

## Potential Integration Points

### 1. Game Technical Metadata
PCGW could provide per-game data that GamingCommander displays alongside launcher metadata:

| Field | PCGW Source | Usage |
|-------|-------------|-------|
| Save file location | `Save game cloud syncing` section | Quick access to save files |
| Config file location | `Config file location` section | Quick access to config editing |
| Executable name | Page title + infobox | Launch target verification |
| Windowed/mode args | `Launch arguments` section | Auto-configure launch parameters |
| Known issues | Page content | Warnings before launch |

### 2. API Approach

PCGW uses MediaWiki. Potential data access methods:

- **REST API**: `https://www.pcgamingwiki.com/api/rest_v1/`
- **Parse API**: `https://www.pcgamingwiki.com/w/api.php?action=parse&page={title}`
- **Page scraping**: Direct page fetch (fragile, last resort)

MediaWiki API endpoint: `https://www.pcgamingwiki.com/w/api.php`

### 3. Data Mapping

```
PCGW "Infobox game" template fields → GamingCommander GameMetadata model:

  | PCGW Field            | GameMetadata Property     |
  |-----------------------|---------------------------|
  | title                 | DisplayName               |
  | developer             | Developer                 |
  | publisher             | Publisher                 |
  | release date          | ReleaseDate               |
  | steam appid           | SteamAppId                |
  | epic games appid      | EpicAppId                 |
  | gogcom id             | GogGameId                 |
  | drm                   | Protection (list)         |
  | store                 | Store                     |
  | executable note       | ExecutableNote            |
  | save game location    | SaveLocation              |
  | config location       | ConfigLocation            |
```

### 4. Caching Strategy

PCGW data should be cached locally to avoid:
- Excessive API calls
- Network dependency for basic display
- Slow load times

Suggested cache format:
```json
{
  "gameId": "steam_12345",
  "pcgwTitle": "Game Title",
  "lastFetched": "2026-01-15T10:30:00Z",
  "etag": "abc123",
  "metadata": { ... }
}
```

Store cache in `%LocalAppData%\GamingCommander\cache\pcgw\`.

### 5. Future GitHub Integration

The plan mentions eventual sync from a GitHub-hosted source (see `planning/overview.md`). This could:
1. Serve as a curated metadata source (game → ID mappings)
2. Potentially replace or supplement live PCGW queries
3. Provide pre-built metadata for known games

## Out of Scope (Current Phase)

- Live PCGW API calls from the application
- Automatic wiki scraping
- Launch argument modification from PCGW data
- Save data backup/restore using PCGW paths

## References

- PCGamingWiki API: https://www.pcgamingwiki.com/w/api.php
- MediaWiki REST API docs: https://www.mediawiki.org/wiki/API:REST_API
- GitHub-hosted metadata sync: see `planning/overview.md`
