# Plan 109 — Epic Manifest Enrichment

**Status:** DRAFT — awaiting approval
**Audience:** Builder
**Priority:** P2 (metadata enrichment)
**Depends on:** None
**Reference:** `docs/EPIC-MANIFEST-ENRICHMENT.md` (full analysis, 639 lines)

---

## 0. Problem Statement

Epic Games Store games are detected by signal (`.egstore/` directory) but no metadata is extracted from their manifest files. The current C# implementation has three gaps:

1. **`FindEpicManifest()` searches `*.json`** — Epic uses `.item` and `.mancpn` extensions, not `.json`
2. **No global `.item` cross-reference** — The authoritative metadata lives outside the game folder in `C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests\`
3. **No identifier extraction** — `CatalogNamespace`, `CatalogItemId`, `AppName` are not captured

**Result:** Epic games show with codename/folder names instead of marketing names, and no store IDs are available for cross-referencing.

---

## 1. What the Python Tool Does

`lookup_metadata.py` implements a 3-strategy pipeline for Epic metadata:

### Strategy 1: Local identifier extraction (`_extract_epic_identifiers`)
- Searches `.egstore/manifests/` and `.egsstore/manifests/` for `.item` files first (richer schema)
- Falls back to `.mancpn` files (only has CatalogItemId, CatalogNamespace, AppName)
- Extracts: `DisplayName`, `CatalogItemId`, `CatalogNamespace`, `AppName`, `LaunchExecutable`

### Strategy 2: Global `.item` cross-reference (`epic_crossref_item_manifests`)
- Scans all `.item` files in `C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests\`
- Matches `InstallLocation` against the game folder path (case-insensitive, trailing separator stripped)
- Returns the matching `.item` dict with `DisplayName`, `LaunchExecutable`, correct `CatalogNamespace`

### Strategy 3: Epic GraphQL API (`epic_search_by_namespace`)
- Queries `https://store.epicgames.com/graphql` with `searchStore` query filtered by namespace UUID
- Returns: `title`, `publisherDisplayName`, `developerDisplayName`, `productSlug`, `releaseDate`, `keyImages`, `description`, `customAttributes`
- Filters for `BASE_GAME` offerType only

### Resolution priority:
1. Global `.item` `DisplayName` (highest — authoritative marketing name)
2. Epic GraphQL API `title` (high — but may hit dev namespace)
3. Local `.item` `DisplayName` (medium — may be stale)
4. Local `.mancpn` → API lookup (medium — risk of dev namespace)
5. Folder name (low — may be codename)

---

## 2. What C# Currently Does

| Feature | Status | Location |
|---------|--------|----------|
| `.egstore/` detection | ✅ Implemented | `StoreSignalDetector.HasEpicSignal()` |
| Local `.item`/`.json` lookup | ⚠️ Broken | `ExecutableDiscovery.FindEpicManifest()` — searches `*.json` only |
| Global `.item` cross-reference | ❌ Not implemented | — |
| Epic GraphQL API lookup | ❌ Not implemented | — |
| `CatalogItemId`/`CatalogNamespace` extraction | ❌ Not implemented | — |
| `LaunchExecutable` → absolute path resolution | ❌ Not implemented | — |

### Current `FindEpicManifest()` issues:
1. Only searches `*.json` — misses `*.item` and `*.mancpn` files
2. Only looks inside the game folder — doesn't check global ProgramData
3. Returns file path but doesn't parse any data
4. Doesn't extract `DisplayName`, `LaunchExecutable`, or GUID identifiers

### Current `FolderScanner` integration:
```csharp
string manifestPath = ExecutableDiscovery.FindEpicManifest(subDir);
// ... stored as ManifestPath on GameEntry
// No metadata extracted from the manifest
```

---

## 3. Implementation Plan

### Phase 1: `EpicManifestParser` — Local + Global `.item` Parsing

**File:** `src/GamingCommander.App/Services/EpicManifestParser.cs`

```csharp
internal sealed class EpicManifestParser
{
    // Parse a single .item file
    internal static EpicItemData? ParseItemFile(string filePath);

    // Parse a .mancpn file (identifiers only)
    internal static EpicIdentifiers? ParseMancpnFile(string filePath);

    // Extract local identifiers from .egstore/ or .egsstore/
    internal static EpicIdentifiers? ExtractLocalIdentifiers(DirectoryInfo gameDir);

    // Cross-reference game folder against global manifests directory
    internal static EpicItemData? CrossReferenceGlobalManifests(
        DirectoryInfo gameDir, string? manifestsDir = null);

    // Resolve LaunchExecutable relative path to absolute
    internal static string ResolveLaunchExecutable(
        string installLocation, string launchExecutable);
}

internal sealed record EpicItemData(
    string DisplayName,
    string InstallLocation,
    string LaunchExecutable,
    string CatalogNamespace,
    string CatalogItemId,
    string AppName,
    bool IsIncompleteInstall);

internal sealed record EpicIdentifiers(
    string CatalogNamespace,
    string CatalogItemId,
    string AppName,
    string DisplayName = "",
    string LaunchExecutable = "");
```

**Key behaviors:**
- Path normalization: case-insensitive, trailing separator stripped (matches Python)
- `.item` preferred over `.mancpn` (richer schema)
- `InstallLocation` comparison handles Windows backslash paths
- Skip `bIsIncompleteInstall == true` (games being downloaded)

### Phase 2: `EpicStoreApiClient` — GraphQL API Lookup

**File:** `src/GamingCommander.App/Services/EpicStoreApiClient.cs`

```csharp
internal sealed class EpicStoreApiClient
{
    internal static async Task<EpicApiResult?> LookupByNamespaceAsync(
        string namespaceId, CancellationToken ct = default);
}

internal sealed record EpicApiResult(
    string Title,
    string Developer,
    string Publisher,
    string Slug,
    int ReleaseYear,
    string Description,
    string CoverUrl);
```

**Key behaviors:**
- Conservative rate limiting (1 req/s)
- Filter for `BASE_GAME` offerType only
- Graceful degradation on network error (return null)
- Timeout: 10 seconds

### Phase 3: Integration into `FolderScanner`

**File:** `src/GamingCommander.App/Services/FolderScanner.cs`

Replace the current `FindEpicManifest()` call with:

```csharp
if (resolvedType == GameSourceKind.Epic)
{
    // Strategy 1: Local identifier extraction
    var localIds = EpicManifestParser.ExtractLocalIdentifiers(subDir);

    // Strategy 2: Global .item cross-reference
    var globalItem = EpicManifestParser.CrossReferenceGlobalManifests(subDir);
    if (globalItem != null && !string.IsNullOrEmpty(globalItem.DisplayName))
    {
        platformMetadata["AutoDetectedTitle"] = displayName;
        displayName = globalItem.DisplayName;
        platformMetadata["TitleSource"] = "EpicItemManifest";
        // Override local namespace with correct public namespace
        localIds = localIds with
        {
            CatalogNamespace = globalItem.CatalogNamespace,
            CatalogItemId = globalItem.CatalogItemId
        };
    }

    // Strategy 3: API lookup (if online metadata enabled)
    if (_config.EnableOnlineMetadata && localIds?.CatalogNamespace is { Length: > 10 })
    {
        var apiResult = await EpicStoreApiClient.LookupByNamespaceAsync(
            localIds.CatalogNamespace);
        if (apiResult?.Title is { Length: > 0 } && globalItem == null)
        {
            platformMetadata["AutoDetectedTitle"] = displayName;
            displayName = apiResult.Title;
            platformMetadata["TitleSource"] = "EpicSearchStore";
        }
    }

    // Store identifiers
    if (localIds != null)
    {
        if (localIds.CatalogItemId.Length > 0)
            platformMetadata["EpicCatalogItemId"] = localIds.CatalogItemId;
        if (localIds.CatalogNamespace.Length > 0)
            platformMetadata["EpicCatalogNamespace"] = localIds.CatalogNamespace;
        if (localIds.AppName.Length > 0)
            platformMetadata["EpicAppName"] = localIds.AppName;
    }
}
```

### Phase 4: Fix `FindEpicManifest()` Extension Bug

**File:** `src/GamingCommander.App/Services/ExecutableDiscovery.cs`

Change `GetFiles("*.json")` to search `*.item`, `*.mancpn`, and `*.json`:
```csharp
foreach (string pattern in new[] { "*.item", "*.mancpn", "*.json" })
{
    foreach (FileInfo file in new DirectoryInfo(manifestsDir).GetFiles(pattern))
        return file.FullName;
}
```

---

## 4. Files Changed

| File | Change |
|------|--------|
| `EpicManifestParser.cs` | **New** — `.item`/`.mancpn` parsing, global cross-ref, path resolution |
| `EpicStoreApiClient.cs` | **New** — GraphQL API client for namespace lookup |
| `FolderScanner.cs` | Replace `FindEpicManifest()` call with 3-strategy enrichment pipeline |
| `ExecutableDiscovery.cs` | Fix `FindEpicManifest()` extension bug (`*.json` → `*.item`/`*.mancpn`) |
| `EpicManifestParserTests.cs` | **New** — 10+ tests for parsing, cross-ref, path normalization |
| `EpicStoreApiClientTests.cs` | **New** — 4+ tests for API lookup, error handling |

---

## 5. Path Configuration

```csharp
internal static class EpicManifestPaths
{
    /// <summary>Default: %ProgramData%\Epic\EpicGamesLauncher\Data\Manifests\</summary>
    internal static string DefaultManifestsDir =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Epic", "EpicGamesLauncher", "Data", "Manifests");

    internal static string GetManifestsDir(string? overridePath = null) =>
        overridePath
        ?? Environment.GetEnvironmentVariable("EPIC_MANIFESTS_DIR")
        ?? DefaultManifestsDir;
}
```

---

## 6. Tests

| Test | Description |
|------|-------------|
| `ParseItemFile_BasicFields` | Parse `.item` JSON, extract DisplayName, LaunchExecutable, CatalogNamespace |
| `ParseItemFile_IncompleteInstall` | Skip games with `bIsIncompleteInstall == true` |
| `ParseMancpnFile_IdentifiersOnly` | Parse `.mancpn` JSON, extract CatalogItemId, CatalogNamespace, AppName |
| `ExtractLocalIdentifiers_PrefersItem` | `.item` takes precedence over `.mancpn` |
| `ExtractLocalIdentifiers_FallsBackToMancpn` | When no `.item`, use `.mancpn` identifiers |
| `CrossReferenceGlobalManifests_Match` | Match InstallLocation against game folder |
| `CrossReferenceGlobalManifests_NoMatch` | Return null when no `.item` matches |
| `CrossReferenceGlobalManifests_MissingDir` | Handle missing global manifests directory |
| `PathNormalization_CaseInsensitive` | Case-insensitive path comparison |
| `PathNormalization_TrailingSeparator` | Trailing `\` or `/` stripped |
| `ResolveLaunchExecutable` | Relative path → absolute path |
| `LookupByNamespace_MockResponse` | Mock HTTP, parse GraphQL result |
| `LookupByNamespace_FilterBaseGame` | Only return BASE_GAME offers |
| `LookupByNamespace_NetworkError` | Return null on failure |

---

## 7. Success Criteria

- [ ] Epic games show marketing names (e.g., "Fortnite" not "Sugar")
- [ ] `EpicCatalogItemId` and `EpicCatalogNamespace` stored in PlatformMetadata
- [ ] `FindEpicManifest()` finds `.item` and `.mancpn` files (not just `.json`)
- [ ] Global `.item` cross-reference works on Windows
- [ ] API fallback works when online metadata is enabled
- [ ] Build clean, all tests pass

---

## 8. Overlap with Research/Tools

| Tool | Overlap | Notes |
|------|---------|-------|
| `lookup_metadata.py` `epic_crossref_item_manifests()` | Direct reference | Python implementation to port |
| `lookup_metadata.py` `epic_search_by_namespace()` | Direct reference | GraphQL query structure to port |
| `lookup_metadata.py` `_extract_epic_identifiers()` | Direct reference | Local parsing logic to port |
| `lookup_metadata.py` `epic_resolve_metadata()` | Combined pipeline | 3-strategy resolution to port |
| `detect.py` `_check_epic()` | Detection only | Signal detection already implemented |
| `tools/epic_search.py` | Standalone tool | Manual search utility, not for porting |
| `docs/EPIC-MANIFEST-ENRICHMENT.md` | Analysis | Comprehensive reference (639 lines) |

---

## 9. Future Work (Out of Scope)

- **PCGW cross-referencing** using Epic `CatalogItemId` → PCGW `Epic_Games_Store_ID`
- **Cover art download** from Epic `keyImages`
- **DLC detection** via `MainGameCatalogNamespace` mismatch
- **Epic launcher process detection** for running game detection

---

**Last updated:** 2026-07-26
**Related:** `docs/EPIC-MANIFEST-ENRICHMENT.md`, `planning/102-tags-metadata-display.md` Phase 3
