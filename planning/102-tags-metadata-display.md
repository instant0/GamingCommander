# Plan 102 — Tags, Metadata, and Display System

**Status:** DRAFT — awaiting approval  
**Audience:** Planner / Builder  
**Priority:** P2 (post-MVP)  
**Effort:** ~6–10 sessions across 4 phases  
**Depends on:** MVP complete ✅  

---

## 0. Problem Statement

Users want to:
1. **Tag games** with genres, categories, and custom labels — accessible via F4
2. **Auto-populate tags** from web metadata (PCGamingWiki, Steam, etc.)
3. **See metadata** in the right-pane details view (review scores, developer, engine, etc.)
4. **Treat Engine and Store as tags** — for display and filtering

The existing Plan 101 (top-level modes + filter) proposed a minimal tag system. This plan expands it into a full metadata + tagging + display system.

---

## 1. Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│                  Data Sources                        │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────┐  │
│  │ User     │  │ Manifest │  │ Web Metadata     │  │
│  │ (F4)     │  │ Files    │  │ (PCGW/Steam/etc) │  │
│  └────┬─────┘  └────┬─────┘  └────┬─────────────┘  │
│       │              │              │                 │
│       └──────────────┼──────────────┘                 │
│                      ▼                                │
│          ┌───────────────────────┐                    │
│          │   GameEntry.Tags      │  ← List<string>    │
│          │   GameEntry.Metadata  │  ← MetadataRecord  │
│          └───────────┬───────────┘                    │
│                      │                                │
│         ┌────────────┼────────────┐                   │
│         ▼            ▼            ▼                   │
│   ┌──────────┐ ┌──────────┐ ┌──────────────┐        │
│   │ F4 Edit  │ │ Filter   │ │ Details Pane │        │
│   │ Tags     │ │ System   │ │ Display      │        │
│   └──────────┘ └──────────┘ └──────────────┘        │
└─────────────────────────────────────────────────────┘
```

### Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Tags field type | `List<string>` | Type-safe, easy to query, JSON-serializable |
| Metadata field type | Separate `MetadataRecord` | Avoids bloating `GameEntry` with 20+ nullable fields |
| Engine as tag? | Yes — `GameEngine` field + auto-tag | Engine is a first-class dimension, not just a tag |
| Store as tag? | Yes — `GameSource` field + auto-label | Store already exists; display it as a tag |
| Genre source | User first, PCGW second | User knows their games; PCGW is convenience |
| Display format | `[Tag]` style with colored badges | Matches NC aesthetic; compact |

---

## 2. Data Model Changes

### 2.1 Add Tags to GameEntry

```csharp
// GamingCommander.Core/Models/GameEntry.cs
public record GameEntry
{
    // ... existing fields ...
    
    /// <summary>User-defined tags: ["RPG", "Open World", "Co-op", "Story Rich"]</summary>
    public List<string> Tags { get; init; } = [];
    
    /// <summary>Game engine detected during scan or from metadata.</summary>
    public GameEngineKind GameEngine { get; init; } = GameEngineKind.Unknown;
}
```

### 2.2 New GameEngine Enum

```csharp
// GamingCommander.Core/Models/GameEngineKind.cs (NEW)
public enum GameEngineKind
{
    Unknown = 0,
    UnrealEngine = 1,
    Unity = 2,
    Rage = 3,         // Rockstar Advanced Game Engine
    Frostbite = 4,    // EA DICE
    Source = 5,       // Valve
    Godot = 6,
    CryEngine = 7,
    // Extensible — values > 100 for custom/unregistered
}
```

### 2.3 New MetadataRecord

```csharp
// GamingCommander.Core/Models/GameMetadataRecord.cs (NEW)
/// <summary>
/// Enriched metadata from web sources (PCGW, Steam, IGDB).
/// Separate from GameEntry to avoid bloating the core model.
/// Persisted in data/games_metadata.json.
/// </summary>
public record GameMetadataRecord
{
    /// <summary>Matches GameEntry.Id for lookup.</summary>
    public string GameEntryId { get; init; } = string.Empty;
    
    // Core metadata
    public string? Developer { get; init; }
    public string? Publisher { get; init; }
    public string? ReleaseDate { get; init; }
    public string? Genre { get; init; }          // "Action, RPG, Open World"
    public string? Description { get; init; }
    
    // Technical metadata
    public string? Engine { get; init; }         // "Unreal Engine 4"
    public string? SaveLocation { get; init; }
    public string? ConfigLocation { get; init; }
    
    // Store IDs (cross-reference)
    public string? SteamAppId { get; init; }
    public string? GogGameId { get; init; }
    
    // Ratings
    public int? MetacriticScore { get; init; }
    public int? IgdbScore { get; init; }
    
    // Links
    public string? PcGamingWikiUrl { get; init; }
    public string? OfficialWebsite { get; init; }
    public string? CoverArtUrl { get; init; }
    
    // Source tracking
    public string? LastMetadataSource { get; init; }  // "PCGW", "Steam", "IGDB"
    public DateTime? LastUpdated { get; init; }
}
```

### 2.4 AppConfig Changes

```csharp
// GamingCommander.Core/Models/AppConfig.cs
public record AppConfig
{
    // ... existing fields ...
    
    /// <summary>Enable online metadata lookups (PCGW, Steam, IGDB).</summary>
    public bool EnableOnlineMetadata { get; init; } = true;
    
    /// <summary>Auto-tag games with detected engine on scan.</summary>
    public bool AutoTagEngine { get; init; } = true;
    
    /// <summary>Auto-tag games with store source on scan.</summary>
    public bool AutoTagStore { get; init; } = true;
}
```

---

## 3. Phase 1: User-Editable Tags (F4)

### 3.1 Goal

Users can add/remove tags per game via the F4 dialog. Tags persist to `games.json`.

### 3.2 Changes

| File | Change |
|------|--------|
| `Core/Models/GameEntry.cs` | Add `Tags` field (`List<string>`) |
| `App/Services/GamesDatabaseService.cs` | Add `Tags` to `GameEntryDto`, backward-compat default `[]` |
| `App/GameSetupWindow.axaml` | Add tag editing UI below existing fields |
| `App/GameSetupWindow.axaml.cs` | Handle tag input, parse comma-separated, normalize |

### 3.3 F4 Tag UI

```
┌─────────────────────────────────────────────┐
│  Configure Game: The Witcher 3              │
│                                             │
│  Display Name: [The Witcher 3          ]    │
│  Game Type:    [Steam              ▼  ]     │
│  Executable:   [C:\...\witcher3.exe  ] [..]│
│  Launch Args:  [--launcher-mode     ]       │
│                                             │
│  Tags:         [RPG, Open World, Co-op  ]   │
│                ─────────────────────────    │
│                Existing: RPG · Open World · │
│                          Co-op · Story Rich  │
│                                             │
│  [OK]  [Cancel]                             │
└─────────────────────────────────────────────┘
```

- Input field accepts comma-separated tags
- Below the input, show existing tags as clickable chips (for quick removal)
- Tag normalization: trim, collapse whitespace, preserve case
- Dedup: check normalized version before adding

### 3.4 Tag Normalization

```csharp
// GamingCommander.Core/Services/TagNormalizer.cs (NEW)
public static class TagNormalizer
{
    /// <summary>
    /// Normalize a tag string: trim, collapse whitespace, preserve case.
    /// Returns null if the tag is empty after normalization.
    /// </summary>
    public static string? Normalize(string tag)
    {
        var trimmed = tag.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        // Collapse multiple spaces
        return Regex.Replace(trimmed, @"\s+", " ");
    }
    
    /// <summary>
    /// Check if two tags are equivalent (case-insensitive, whitespace-collapsed).
    /// </summary>
    public static bool AreEquivalent(string a, string b)
    {
        return string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Add a tag to a list, avoiding duplicates.
    /// Returns the updated list.
    /// </summary>
    public static List<string> AddDistinct(List<string> existing, string newTag)
    {
        var normalized = Normalize(newTag);
        if (normalized == null) return existing;
        
        if (existing.Any(t => AreEquivalent(t, normalized)))
            return existing;
        
        return [.. existing, normalized];
    }
}
```

### 3.5 Tests

- `TagNormalizerTests.cs`: Normalize, AreEquivalent, AddDistinct edge cases
- `GameSetupWindowTagTests.cs`: Tag parsing from comma-separated input
- `GamesDatabaseServiceTagTests.cs`: Tags persist/load correctly

### 3.6 Success Criteria

- [ ] F4 dialog shows tag input field
- [ ] Tags persist to `games.json` as `List<string>` array
- [ ] Existing games load with empty tags (backward-compatible)
- [ ] Tag normalization works (trim, collapse, dedup)
- [ ] Build clean, all tests pass

---

## 4. Phase 2: Engine Detection + Auto-Tagging

### 4.1 Goal

Detect game engine during scan. Store in `GameEntry.GameEngine`. Optionally auto-tag.

### 4.2 EngineDetector

```csharp
// GamingCommander.App/Services/EngineDetector.cs (NEW)
/// <summary>
/// Detects game engine from local filesystem signals.
/// Ported from tools/detect.py _detect_engine().
/// </summary>
public static class EngineDetector
{
    /// <summary>
    /// Detect engine from game folder. Returns GameEngineKind.
    /// </summary>
    public static GameEngineKind Detect(string gamePath)
    {
        if (HasUnrealEngine(gamePath)) return GameEngineKind.UnrealEngine;
        if (HasUnity(gamePath)) return GameEngineKind.Unity;
        if (HasRage(gamePath)) return GameEngineKind.Rage;
        if (HasFrostbite(gamePath)) return GameEngineKind.Frostbite;
        if (HasSource(gamePath)) return GameEngineKind.Source;
        if (HasGodot(gamePath)) return GameEngineKind.Godot;
        if (HasCryEngine(gamePath)) return GameEngineKind.CryEngine;
        return GameEngineKind.Unknown;
    }
    
    /// <summary>Engine/Binaries/ exists, or child/Binaries/Win64/ exists.</summary>
    public static bool HasUnrealEngine(string path)
    {
        var dir = new DirectoryInfo(path);
        var engineDir = new DirectoryInfo(Path.Combine(path, "Engine"));
        if (!engineDir.Exists) return false;
        if (Directory.Exists(Path.Combine(engineDir.FullName, "Binaries"))) return true;
        foreach (var child in dir.GetDirectories())
        {
            if (Directory.Exists(Path.Combine(child.FullName, "Binaries", "Win64")))
                return true;
        }
        return false;
    }
    
    /// <summary>UnityPlayer.dll + *_Data/ directory.</summary>
    public static bool HasUnity(string path)
    {
        if (!File.Exists(Path.Combine(path, "UnityPlayer.dll"))) return false;
        return Directory.GetDirectories(path).Any(d => d.EndsWith("_Data"));
    }
    
    /// <summary>title.rgl + common.rpf.</summary>
    public static bool HasRage(string path)
    {
        return File.Exists(Path.Combine(path, "title.rgl"))
            && File.Exists(Path.Combine(path, "common.rpf"));
    }
    
    /// <summary>Engine.BuildInfo_Win64_retail.dll.</summary>
    public static bool HasFrostbite(string path)
    {
        return File.Exists(Path.Combine(path, "Engine.BuildInfo_Win64_retail.dll"));
    }
    
    /// <summary>bin/ directory with source engine DLLs.</summary>
    public static bool HasSource(string path)
    {
        var binDir = Path.Combine(path, "bin");
        return Directory.Exists(binDir)
            && Directory.GetFiles(binDir, "client.dll").Length > 0;
    }
    
    /// <summary>export_presets.cfg + project.godot.</summary>
    public static bool HasGodot(string path)
    {
        return File.Exists(Path.Combine(path, "export_presets.cfg"))
            || File.Exists(Path.Combine(path, "project.godot"));
    }
    
    /// <summary>engine3/ or engine2/ directory with bin/ subdirectory.</summary>
    public static bool HasCryEngine(string path)
    {
        return Directory.Exists(Path.Combine(path, "engine3"))
            || Directory.Exists(Path.Combine(path, "engine2"));
    }
}
```

### 4.3 Integration into FolderScanner

```csharp
// In FolderScanner.Scan() or AddGameEntry():
var engine = EngineDetector.Detect(gameDir);
// Set on GameEntry:
entry = entry with { GameEngine = engine };
// Auto-tag if enabled:
if (_config.AutoTagEngine && engine != GameEngineKind.Unknown)
{
    var engineTag = EngineToTagName(engine);  // "Unreal Engine", "Unity", etc.
    entry = entry with { Tags = TagNormalizer.AddDistinct(entry.Tags, engineTag) };
}
```

### 4.4 Store Auto-Tagging

```csharp
// In FolderScanner.Scan() after detecting GameSource:
if (_config.AutoTagStore && gameSource != GameSourceKind.Unknown)
{
    var storeTag = GameSourceParser.ParseToString(gameSource);  // "Steam", "GOG", etc.
    entry = entry with { Tags = TagNormalizer.AddDistinct(entry.Tags, storeTag) };
}
```

### 4.5 Tests

- `EngineDetectorTests.cs`: Test each engine signal (Unreal, Unity, RAGE, Frostbite, Source, Godot, CryEngine, Unknown)
- `AutoTagTests.cs`: Auto-tag on scan, dedup, config toggle

### 4.6 Success Criteria

- [ ] `EngineDetector` correctly identifies all 7 engine types
- [ ] `GameEntry.GameEngine` persisted to `games.json`
- [ ] Auto-tagging adds engine name as tag
- [ ] Auto-tagging adds store name as tag
- [ ] Existing games load with `GameEngine = Unknown` (backward-compatible)
- [ ] Build clean, all tests pass

---

## 5. Phase 3: Metadata Scraping (PCGW + Steam + IGDB)

### 5.1 Goal

Fetch metadata from web sources. Cache locally. Display in details pane.

### 5.1a Game Name Enrichment via PCGW

**Key Finding:** PCGamingWiki can resolve abbreviated game names (e.g., `lotrbfme2` → "The Lord of the Rings: The Battle for Middle-earth II").

**Use Case:** When PE metadata is insufficient (old games pre-2010 often have empty Description/InternalName), PCGW lookup can provide the full game name.

**Implementation Strategy:**
1. **Extract exe name** (without extension) as search key: `lotrbfme2.exe` → `lotrbfme2`
2. **Rate limit aggressively**: Only search for games that need enrichment (empty PE metadata)
3. **Cache results**: Store resolved names in `games.json` to avoid repeated lookups
4. **Graceful fallback**: If PCGW lookup fails, keep the folder name as display name

**Example Resolution Chain:**
```
Folder: "BFME2" (display name from folder)
  ↓
PE Metadata: Description="" (empty, old game)
  ↓
PCGW Search: "lotrbfme2" (exe name without extension)
  ↓
Result: "The Lord of the Rings: The Battle for Middle-earth II"
  ↓
Update: DisplayName = "The Lord of the Rings: The Battle for Middle-earth II"
```

**Rate Limiting Strategy:**
- **Batch processing**: Process enrichment in background after initial scan
- **Throttle**: 0.6s between PCGW calls (their rate limit)
- **Priority queue**: Enrich games with empty PE metadata first
- **User opt-in**: Configurable setting `EnableOnlineMetadata` (default: true)

### 5.2 Source Priority

| Priority | Source | Fields | API Key | Rate Limit |
|----------|--------|--------|---------|------------|
| 1 | Steam Store API | name, developers, publishers, genres, release_date, metacritic | No | 1 req/10s |
| 2 | PCGW Cargo API | developers, publishers, genres, release_date, cover, steam_appid, gogcom_id | No | 0.6s between calls |
| 3 | PCGW Parse API | engine, modes, perspectives, themes | No | Same as above |
| 4 | SteamDB JSON | name, tags, cooptimus | No | None |
| 5 | IGDB | All fields | Yes (Twitch) | 4 req/s |

### 5.3 Local Metadata Database

```
data/games_metadata.json
```

```json
{
  "metadata": {
    "game-entry-id-1": {
      "gameEntryId": "game-entry-id-1",
      "developer": "CD Projekt Red",
      "publisher": "CD Projekt",
      "releaseDate": "2015-05-18",
      "genre": "Action, RPG, Open World",
      "engine": "REDengine 3",
      "metacriticScore": 93,
      "steamAppId": "292030",
      "pcgwUrl": "https://pcgamingwiki.com/wiki/The_Witcher_3",
      "coverArtUrl": "...",
      "lastMetadataSource": "Steam",
      "lastUpdated": "2026-07-26T00:00:00Z"
    }
  }
}
```

### 5.4 MetadataProvider Interface

```csharp
// GamingCommander.Core/Services/IMetadataProvider.cs (NEW)
public interface IMetadataProvider
{
    string Name { get; }
    int Priority { get; }
    bool RequiresApiKey { get; }
    
    /// <summary>
    /// Look up metadata for a game entry.
    /// Returns null if no data found.
    /// </summary>
    Task<GameMetadataRecord?> LookupAsync(
        string gameTitle,
        string? steamAppId = null,
        string? gogGameId = null,
        CancellationToken ct = default);
}
```

### 5.5 SteamStoreProvider

```csharp
// GamingCommander.App/Services/Providers/SteamStoreProvider.cs (NEW)
/// <summary>
/// Fetches metadata from Steam Store API (free, no key).
/// https://store.steampowered.com/api/appdetails?appids={appid}
/// </summary>
public class SteamStoreProvider : IMetadataProvider
{
    public string Name => "Steam Store";
    public int Priority => 1;
    public bool RequiresApiKey => false;
    
    public async Task<GameMetadataRecord?> LookupAsync(
        string gameTitle, string? steamAppId = null, string? gogGameId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(steamAppId)) return null;
        
        var url = $"https://store.steampowered.com/api/appdetails?appids={steamAppId}";
        // ... HTTP GET, parse JSON ...
        // Returns: name, developers, publishers, genres, release_date, metacritic, header_image
    }
}
```

### 5.6 PCGW Provider

```csharp
// GamingCommander.App/Services/Providers/PcgwProvider.cs (NEW)
/// <summary>
/// Fetches metadata from PCGamingWiki Cargo API (free, no key).
/// Primary: Cargo API for structured fields.
/// Fallback: Parse API for engine, modes, perspectives.
/// Also: Game name enrichment for old games with empty PE metadata.
/// </summary>
public class PcgwProvider : IMetadataProvider
{
    public string Name => "PCGamingWiki";
    public int Priority => 2;
    public bool RequiresApiKey => false;
    
    private DateTime _lastCall = DateTime.MinValue;
    private const int RateLimitMs = 600;
    
    public async Task<GameMetadataRecord?> LookupAsync(
        string gameTitle, string? steamAppId = null, string? gogGameId = null,
        string? exeName = null,  // NEW: for game name enrichment
        CancellationToken ct = default)
    {
        await EnforceRateLimit(ct);
        
        // Step 1: Try Cargo by store ID
        if (!string.IsNullOrEmpty(steamAppId))
        {
            var result = await LookupByCargoField("Steam_AppID", steamAppId, ct);
            if (result != null) return result;
        }
        if (!string.IsNullOrEmpty(gogGameId))
        {
            var result = await LookupByCargoField("GOGcom_ID", gogGameId, ct);
            if (result != null) return result;
        }
        
        // Step 2: OpenSearch by name
        var pageName = await OpenSearch(gameTitle, ct);
        if (pageName != null)
        {
            var result = await LookupByPageName(pageName, ct);
            if (result != null) return result;
        }
        
        // Step 3: Game name enrichment via exe name (for old games)
        if (!string.IsNullOrEmpty(exeName))
        {
            var enriched = await EnrichGameName(exeName, ct);
            if (enriched != null)
            {
                return new GameMetadataRecord
                {
                    GameEntryId = "",  // Will be set by caller
                    Developer = enriched.Developer,
                    Publisher = enriched.Publisher,
                    Description = enriched.Description,
                    PcGamingWikiUrl = enriched.PcGamingWikiUrl,
                    LastMetadataSource = "PCGW (enrichment)",
                    LastUpdated = DateTime.UtcNow,
                };
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Enrich game name using exe name (without extension).
    /// Example: "lotrbfme2" → "The Lord of the Rings: The Battle for Middle-earth II"
    /// </summary>
    private async Task<EnrichmentResult?> EnrichGameName(string exeName, CancellationToken ct)
    {
        // Try OpenSearch with exe name
        var pageName = await OpenSearch(exeName, ct);
        if (pageName == null) return null;
        
        // Fetch page content for full name
        var pageData = await FetchPageData(pageName, ct);
        if (pageData == null) return null;
        
        return new EnrichmentResult
        {
            FullName = pageData.Title,
            Developer = pageData.Developer,
            Publisher = pageData.Publisher,
            Description = pageData.Description,
            PcGamingWikiUrl = $"https://pcgamingwiki.com/wiki/{pageName}",
        };
    }
    
    private async Task EnforceRateLimit(CancellationToken ct)
    {
        var elapsed = (DateTime.UtcNow - _lastCall).TotalMilliseconds;
        if (elapsed < RateLimitMs)
            await Task.Delay(RateLimitMs - (int)elapsed, ct);
        _lastCall = DateTime.UtcNow;
    }
}

internal record EnrichmentResult
{
    public string FullName { get; init; } = string.Empty;
    public string? Developer { get; init; }
    public string? Publisher { get; init; }
    public string? Description { get; init; }
    public string? PcGamingWikiUrl { get; init; }
}
```

### 5.7 IGDB Provider (Optional)

```csharp
// GamingCommander.App/Services/Providers/IgdbProvider.cs (NEW)
/// <summary>
/// Fetches metadata from IGDB (requires Twitch API key).
/// Richest source: genres, themes, perspectives, cover art, ratings.
/// </summary>
public class IgdbProvider : IMetadataProvider
{
    public string Name => "IGDB";
    public int Priority => 5;
    public bool RequiresApiKey => true;
    
    // OAuth2 flow: POST twitch.tv/oauth2/token → bearer token
    // POST api.igdb.com/v4/games with APICalypse query
}
```

### 5.8 MetadataService Orchestrator

```csharp
// GamingCommander.App/Services/MetadataService.cs (NEW)
/// <summary>
/// Orchestrates metadata lookup across providers.
/// Merges results into local cache.
/// </summary>
public class MetadataService
{
    private readonly IReadOnlyList<IMetadataProvider> _providers;
    private readonly GameMetadataDatabase _database;
    
    public async Task<GameMetadataRecord> LookupAndCacheAsync(
        GameEntry game, CancellationToken ct = default)
    {
        var existing = _database.Get(game.Id);
        var merged = existing ?? new GameMetadataRecord { GameEntryId = game.Id };
        
        foreach (var provider in _providers.OrderBy(p => p.Priority))
        {
            if (provider.RequiresApiKey && !HasApiKey(provider.Name))
                continue;
            
            var result = await provider.LookupAsync(
                game.DisplayName,
                GetSteamAppId(game),
                GetGogGameId(game),
                ct);
            
            if (result != null)
                merged = MergeMetadata(merged, result);
        }
        
        _database.Upsert(game.Id, merged);
        return merged;
    }
    
    private static GameMetadataRecord MergeMetadata(
        GameMetadataRecord existing, GameMetadataRecord incoming)
    {
        // Merge: only overwrite fields that are non-null in incoming
        return existing with
        {
            Developer = incoming.Developer ?? existing.Developer,
            Publisher = incoming.Publisher ?? existing.Publisher,
            ReleaseDate = incoming.ReleaseDate ?? existing.ReleaseDate,
            Genre = incoming.Genre ?? existing.Genre,
            Engine = incoming.Engine ?? existing.Engine,
            MetacriticScore = incoming.MetacriticScore ?? existing.MetacriticScore,
            IgdbScore = incoming.IgdbScore ?? existing.IgdbScore,
            CoverArtUrl = incoming.CoverArtUrl ?? existing.CoverArtUrl,
            PcGamingWikiUrl = incoming.PcGamingWikiUrl ?? existing.PcGamingWikiUrl,
            LastMetadataSource = incoming.LastMetadataSource ?? existing.LastMetadataSource,
            LastUpdated = DateTime.UtcNow,
        };
    }
}
```

### 5.9 Metadata Tag Auto-Population

When metadata is fetched, auto-populate tags from genres:

```csharp
// In MetadataService.LookupAndCacheAsync, after merge:
if (_config.AutoTagFromMetadata && merged.Genre != null)
{
    var genreTags = merged.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries);
    var updatedTags = game.Tags.ToList();
    foreach (var genre in genreTags)
    {
        updatedTags = TagNormalizer.AddDistinct(updatedTags, genre.Trim());
    }
    // Update GameEntry tags (via LibraryManager.UpdateGameEntry)
}
```

### 5.10 Tests

- `SteamStoreProviderTests.cs`: Mock HTTP, parse response
- `PcgwProviderTests.cs`: Mock Cargo/Parse responses, rate limiting
- `MetadataServiceTests.cs`: Provider priority, merge logic, caching
- `TagAutoPopulateTests.cs`: Genre → tag conversion, dedup

### 5.11 Success Criteria

- [ ] Steam Store metadata fetch works for known AppIDs
- [ ] PCGW metadata fetch works (Cargo + Parse fallback)
- [ ] Metadata cached locally in `data/games_metadata.json`
- [ ] Merge logic preserves earlier successful lookups
- [ ] Genre tags auto-populated from metadata
- [ ] Rate limiting enforced (0.6s between PCGW calls)
- [ ] Build clean, all tests pass

---

## 6. Phase 4: Display System (Right Pane)

### 6.1 Goal

Show tags, metadata, and scores in the right-pane details view.

### 6.2 Display Format

```
┌─────────────────────────────────────────────────────┐
│  The Witcher 3: Wild Hunt                           │
│  ─────────────────────────────────────────────────  │
│                                                     │
│  Store:    Steam          [blue badge]              │
│  Engine:   Unreal Engine 4                          │
│  Tags:     [RPG] [Open World] [Co-op] [Story Rich] │
│                                                     │
│  Developer:    CD Projekt Red                       │
│  Publisher:    CD Projekt                           │
│  Released:     2015-05-18                           │
│  Metacritic:   93/100                               │
│                                                     │
│  PCGW: https://pcgamingwiki.com/wiki/...            │
│                                                     │
│  Path: D:\SteamLibrary\steamapps\common\...\        │
│  Status: Installed                                  │
│  Exe:    witcher3.exe                               │
│  Args:   --launcher-mode                            │
└─────────────────────────────────────────────────────┘
```

### 6.3 Tag Badge Rendering

```xml
<!-- MainWindow.axaml — right pane details -->
<ItemsControl ItemsSource="{Binding SelectedGame.Tags}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <WrapPanel />
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Border Background="{DynamicResource TagBadgeBg}"
                    CornerRadius="4" Padding="6,2" Margin="2">
                <TextBlock Text="{Binding}"
                           Foreground="{DynamicResource TagBadgeText}"
                           FontSize="11" />
            </Border>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

### 6.4 Metadata Display

Show metadata fields only when available (not null/empty):

```xml
<!-- Conditional visibility for metadata fields -->
<StackPanel IsVisible="{Binding SelectedGame.Metadata.Developer, Converter={StaticResource NullToBoolConverter}}">
    <TextBlock Text="Developer:" FontWeight="Bold" />
    <TextBlock Text="{Binding SelectedGame.Metadata.Developer}" />
</StackPanel>

<StackPanel IsVisible="{Binding SelectedGame.Metadata.MetacriticScore, Converter={StaticResource NullToBoolConverter}}">
    <TextBlock Text="Metacritic:" FontWeight="Bold" />
    <TextBlock Text="{Binding SelectedGame.Metadata.MetacriticScore, StringFormat='{}{0}/100'}" />
</StackPanel>
```

### 6.5 ShellPaneItemViewModel Extensions

```csharp
// GamingCommander.UI/ViewModels/ShellPaneItemViewModel.cs
public record ShellPaneItemViewModel
{
    // ... existing fields ...
    
    /// <summary>User-defined and auto-populated tags.</summary>
    public List<string> Tags { get; init; } = [];
    
    /// <summary>Detected engine name for display.</summary>
    public string EngineLabel { get; init; } = string.Empty;
    
    /// <summary>Store source as display tag.</summary>
    public string StoreLabel { get; init; } = string.Empty;
    
    /// <summary>Enriched metadata (null if not fetched).</summary>
    public GameMetadataRecord? Metadata { get; init; }
}
```

### 6.6 Theme Resources for Tags

```xml
<!-- App.axaml — tag badge theme resources -->
<SolidColorBrush x:Key="TagBadgeBg" Color="#2A3A4A" />
<SolidColorBrush x:Key="TagBadgeText" Color="#B8C8D8" />
<SolidColorBrush x:Key="TagBadgeBorder" Color="#4A5A6A" />

<!-- Engine badge (distinct color) -->
<SolidColorBrush x:Key="EngineBadgeBg" Color="#1A3A2A" />
<SolidColorBrush x:Key="EngineBadgeText" Color="#88CC88" />

<!-- Store badge (distinct color) -->
<SolidColorBrush x:Key="StoreBadgeBg" Color="#3A2A1A" />
<SolidColorBrush x:Key="StoreBadgeText" Color="#CCAA88" />

<!-- Metacritic colors -->
<SolidColorBrush x:Key="MetacriticGreen" Color="#66CC66" />  <!-- 75+ -->
<SolidColorBrush x:Key="MetacriticYellow" Color="#CCCC66" /> <!-- 50-74 -->
<SolidColorBrush x:Key="MetacriticRed" Color="#CC6666" />    <!-- <50 -->
```

### 6.7 Tests

- `ShellPaneItemTagDisplayTests.cs`: Tags render correctly
- `MetadataDisplayTests.cs`: Null fields hidden, populated fields shown
- `MetacriticColorTests.cs`: Score → color mapping

### 6.8 Success Criteria

- [ ] Tags display as colored badges in right pane
- [ ] Engine displayed as badge (colored distinctly from tags)
- [ ] Store displayed as badge
- [ ] Metadata fields shown only when available
- [ ] Metacritic score color-coded (green/yellow/red)
- [ ] Empty state shows "No metadata" gracefully
- [ ] Build clean, all tests pass

---

## 7. Engine + Store as Tags (Secondary Feature)

### 7.1 Concept

Engine and Store are **first-class dimensions** but displayed as tags for compactness:

```
Call of Duty [Battle.net] (blue) [FPS] [COOP] [Competitive] — 89% MC
```

- `[Battle.net]` — Store tag (blue badge, from `GameSource`)
- `[FPS] [COOP] [Competitive]` — User/metadata tags
- `89% MC` — Metacritic score (from metadata)

### 7.2 Implementation

The Store and Engine badges are rendered **separately** from user tags in the UI:

```xml
<!-- Left pane: game title line -->
<StackPanel Orientation="Horizontal">
    <TextBlock Text="{Binding Title}" />
    <Border Background="{DynamicResource StoreBadgeBg}" Margin="4,0"
            IsVisible="{Binding StoreLabel, Converter={StaticResource StringToBoolConverter}}">
        <TextBlock Text="{Binding StoreLabel}"
                   Foreground="{DynamicResource StoreBadgeText}" FontSize="10" />
    </Border>
</StackPanel>

<!-- Right pane: tags row -->
<ItemsControl ItemsSource="{Binding Tags}">
    <!-- ... tag badges ... -->
</ItemsControl>

<!-- Right pane: metadata row -->
<StackPanel Orientation="Horizontal"
            IsVisible="{Binding Metadata.MetacriticScore, Converter={StaticResource NullToBoolConverter}}">
    <TextBlock Text="{Binding Metadata.MetacriticScore, StringFormat='{}{0}% MC'}"
               Foreground="{Binding Metadata.MetacriticScore, Converter={StaticResource MetacriticColorConverter}}" />
</StackPanel>
```

### 7.3 Tag Display Hierarchy

```
Level 1: Store badge (colored, always shown if game has a store source)
Level 2: Engine badge (colored, shown if engine detected)
Level 3: User tags (neutral color, shown if any exist)
Level 4: Metadata scores (inline text, shown if available)
```

---

## 8. File Changes Summary

### New Files

| File | Purpose |
|------|---------|
| `Core/Models/GameEngineKind.cs` | Engine enum |
| `Core/Models/GameMetadataRecord.cs` | Enriched metadata record |
| `Core/Services/IMetadataProvider.cs` | Provider interface |
| `Core/Services/TagNormalizer.cs` | Tag normalization logic |
| `App/Services/EngineDetector.cs` | Engine detection from filesystem |
| `App/Services/MetadataService.cs` | Orchestrator for metadata lookup |
| `App/Services/GameMetadataDatabase.cs` | JSON persistence for metadata |
| `App/Services/Providers/SteamStoreProvider.cs` | Steam Store API |
| `App/Services/Providers/PcgwProvider.cs` | PCGamingWiki API |
| `App/Services/Providers/IgdbProvider.cs` | IGDB API (optional) |
| `App/FilterWindow.axaml` | Filter dialog (from Plan 101) |
| `App/FilterWindow.axaml.cs` | Filter dialog code-behind |
| `App/Services/FilterService.cs` | Filter logic (from Plan 101) |

### Modified Files

| File | Change |
|------|--------|
| `Core/Models/GameEntry.cs` | Add `Tags`, `GameEngine` |
| `Core/Models/AppConfig.cs` | Add `AutoTagEngine`, `AutoTagStore`, `EnableOnlineMetadata` |
| `App/GameSetupWindow.axaml` | Add tag editing UI |
| `App/GameSetupWindow.axaml.cs` | Handle tag input |
| `App/Services/GamesDatabaseService.cs` | Add `Tags`, `GameEngine` to DTO |
| `UI/ViewModels/ShellPaneItemViewModel.cs` | Add `Tags`, `EngineLabel`, `StoreLabel`, `Metadata` |
| `UI/ViewModels/ShellViewModel.cs` | Map tags/metadata to view model |
| `App/MainWindow.axaml` | Tag badges, metadata display in right pane |
| `App/Services/FolderScanner.cs` | Call `EngineDetector`, auto-tag |
| `App/Services/HelpDialogBuilder.cs` | Update F5 description |

---

## 9. Backward Compatibility

| Change | Compatibility |
|--------|--------------|
| `GameEntry.Tags` defaults to `[]` | ✅ Existing games load with empty tags |
| `GameEntry.GameEngine` defaults to `Unknown` | ✅ Existing games load with Unknown engine |
| `GameMetadataRecord` in separate file | ✅ No change to `games.json` schema |
| `AppConfig` new fields have defaults | ✅ Existing settings.json loads correctly |
| `ShellPaneItemViewModel` new fields have defaults | ✅ UI unaffected until populated |

---

## 10. Out of Scope

- Tag categories/groups (user-defined tag taxonomy)
- Tag auto-suggestion in F4 (future enhancement)
- Tag alias resolution for PCGW import (Phase 5)
- Persistent filter presets (future extension)
- HowLongToBeat play time data
- Cover art download/display (URL only, no download)
- Multi-language metadata
- Batch metadata lookup (per-root scan)

---

## 11. Execution Order

```
Phase 1: User Tags (F4 editing)        ← Self-contained, no external deps
    ↓
Phase 2: Engine Detection + Auto-Tag    ← Needs EngineDetector, auto-tag on scan
    ↓
Phase 3: Metadata Scraping              ← Needs IMetadataProvider, MetadataService
    ↓
Phase 4: Display System                 ← Needs all above for full display
```

Phase 1 is the foundation. Phase 2 and 3 are independent of each other but both depend on Phase 1. Phase 4 depends on all three.

---

**Planner note:** This plan extends Plan 101 (top-level modes + filter) by adding the metadata and display layers. Plan 101's Phase 1 (mode switcher) and Phase 2 (flatten) should be completed first. This plan's Phase 1 (user tags) is the foundation that both systems share.
