# Plan 109 — Epic Manifest Enrichment

**Status:** ✅ COMPLETE
**Audience:** Builder
**Priority:** P2 (metadata enrichment)
**Depends on:** None
**Reference:** `docs/EPIC-MANIFEST-ENRICHMENT.md` (full analysis, 654 lines)

---

## 0. Problem Statement

Epic Games Store games are detected by signal (`.egstore/` directory) but no metadata is extracted from their manifest files. The current C# implementation has three bugs:

1. **`FindEpicManifest()` searches `*.json`** (Bug #17, HIGH) — Epic uses `.item` and `.mancpn` extensions, not `.json`
2. **No identifier extraction** (Bug #18, HIGH) — `CatalogNamespace`, `CatalogItemId`, `AppName` are not captured; manifest path stored but data never parsed
3. **No global `.item` cross-reference** (Bug #19, MEDIUM) — The authoritative metadata lives outside the game folder in `C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests\`

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

### Strategy 3: Epic GraphQL API (`epic_search_by_namespace`) — **DEFERRED**
- Queries `https://store.epicgames.com/graphql` with `searchStore` query filtered by namespace UUID
- Returns: `title`, `publisherDisplayName`, `developerDisplayName`, `productSlug`, `releaseDate`, `keyImages`, `description`, `customAttributes`
- Filters for `BASE_GAME` offerType only

### Resolution priority:
1. Global `.item` `DisplayName` (highest — authoritative marketing name)
2. ~~Epic GraphQL API `title`~~ **(deferred — not in this plan)**
3. Local `.item` `DisplayName` (medium — may be stale)
4. Local `.mancpn` identifiers (medium — may have dev namespace)
5. Folder name (low — may be codename)

---

## 2. What C# Currently Does

| Feature | Status | Location | Bug |
|---------|--------|----------|-----|
| `.egstore/` detection | ✅ Implemented | `StoreSignalDetector.HasEpicSignal()` | — |
| Local `.item`/`.json` lookup | ⚠️ Broken | `ExecutableDiscovery.FindEpicManifest()` | **#17** |
| Global `.item` cross-reference | ❌ Not implemented | — | **#19** |
| `CatalogItemId`/`CatalogNamespace` extraction | ❌ Not implemented | — | **#18** |
| `LaunchExecutable` → absolute path resolution | ❌ Not implemented | — | — |
| Epic GraphQL API lookup | ❌ Not implemented | — | **DEFERRED** |

### Current `FindEpicManifest()` issues (Bug #17):
1. Only searches `*.json` — misses `*.item` and `*.mancpn` files
2. Only looks inside the game folder — doesn't check global ProgramData
3. Returns file path but doesn't parse any data
4. Doesn't extract `DisplayName`, `LaunchExecutable`, or GUID identifiers

### Current `FolderScanner` integration (Bug #18):
```csharp
string manifestPath = ExecutableDiscovery.FindEpicManifest(subDir);
// ... stored as ManifestPath on GameEntry
// No metadata extracted from the manifest
```

---

## 3. Implementation Plan

### Phase 1: Fix `FindEpicManifest()` Extension Bug (#17) ✅ COMPLETE

**File:** `src/GamingCommander.App/Services/ExecutableDiscovery.cs`
**Bug:** #17 (HIGH) — searches `*.json` but Epic uses `.item` and `.mancpn`

Change `GetFiles("*.json")` to search `*.item`, `*.mancpn`, and `*.json` in preference order:
```csharp
foreach (string pattern in new[] { "*.item", "*.mancpn", "*.json" })
{
    foreach (FileInfo file in new DirectoryInfo(manifestsDir).GetFiles(pattern))
        return file.FullName;
}
```

**Result:** `FindEpicManifest()` now returns paths to actual Epic manifest files.

---

### Phase 2: `EpicManifestParser` — Local `.mancpn`/`.item` Parsing (#18 partial) ✅ COMPLETE

**File:** `src/GamingCommander.App/Services/EpicManifestParser.cs` (new)

```csharp
internal sealed class EpicManifestParser
{
    // Parse a single .item file
    internal static EpicItemData? ParseItemFile(string filePath);

    // Parse a .mancpn file (identifiers only)
    internal static EpicIdentifiers? ParseMancpnFile(string filePath);

    // Extract local identifiers from .egstore/ or .egsstore/
    internal static EpicIdentifiers? ExtractLocalIdentifiers(DirectoryInfo gameDir);

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
- `.item` preferred over `.mancpn` (richer schema)
- Skip `bIsIncompleteInstall == true` (games being downloaded)
- `ResolveLaunchExecutable` handles relative paths (e.g., `FortniteGame\Binaries\Win64\FortniteClient-Win64-Shipping.exe`)

---

### Phase 3: Global `.item` Cross-Reference (#19) ✅ COMPLETE

**File:** `src/GamingCommander.App/Services/EpicManifestParser.cs` (same file)

Add to `EpicManifestParser`:
```csharp
// Cross-reference game folder against global manifests directory
internal static EpicItemData? CrossReferenceGlobalManifests(
    DirectoryInfo gameDir, string? manifestsDir = null);
```

**Key behaviors:**
- Path normalization: case-insensitive, trailing separator stripped (matches Python)
- `InstallLocation` comparison handles Windows backslash paths
- Returns `null` when no `.item` matches or directory missing

**Path configuration:**
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

### Phase 4: Integration into `FolderScanner` (#18 full) ✅ COMPLETE

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

    // Resolve LaunchExecutable if available
    if (globalItem != null && !string.IsNullOrEmpty(globalItem.LaunchExecutable)
        && !string.IsNullOrEmpty(globalItem.InstallLocation))
    {
        var resolvedExe = EpicManifestParser.ResolveLaunchExecutable(
            globalItem.InstallLocation, globalItem.LaunchExecutable);
        if (string.IsNullOrEmpty(exePath))
            exePath = resolvedExe;
    }
}
```

**Note:** The `FindEpicManifest()` call is removed — `EpicManifestParser.ExtractLocalIdentifiers()` handles local manifest discovery directly. The `ManifestPath` field on `GameEntry` can be removed or repurposed.

---

### Phase 5: Tests (~12 test cases) ✅ COMPLETE

**File:** `tests/GamingCommander.App.Tests/EpicManifestParserTests.cs` (new)

| Test | Description |
|------|-------------|
| `ParseItemFile_BasicFields` | Parse `.item` JSON, extract DisplayName, LaunchExecutable, CatalogNamespace |
| `ParseItemFile_IncompleteInstall` | Skip games with `bIsIncompleteInstall == true` |
| `ParseMancpnFile_IdentifiersOnly` | Parse `.mancpn` JSON, extract CatalogItemId, CatalogNamespace, AppName |
| `ExtractLocalIdentifiers_PrefersItem` | `.item` takes precedence over `.mancpn` |
| `ExtractLocalIdentifiers_FallsBackToMancpn` | When no `.item`, use `.mancpn` identifiers |
| `ExtractLocalIdentifiers_MissingDir` | Handle missing `.egstore`/`.egsstore` directories |
| `CrossReferenceGlobalManifests_Match` | Match InstallLocation against game folder |
| `CrossReferenceGlobalManifests_NoMatch` | Return null when no `.item` matches |
| `CrossReferenceGlobalManifests_MissingDir` | Handle missing global manifests directory |
| `PathNormalization_CaseInsensitive` | Case-insensitive path comparison |
| `PathNormalization_TrailingSeparator` | Trailing `\` or `/` stripped |
| `ResolveLaunchExecutable` | Relative path → absolute path |

---

### DEFERRED: `EpicStoreApiClient` — GraphQL API Lookup

**Status:** Deferred to future plan. Not implemented in this phase.

**Rationale:**
- Local `.item` files from global ProgramData provide authoritative metadata for most Epic games
- API adds network dependency and complexity
- API can be added later as fallback for games without local `.item` files

**Future implementation notes:**
- Endpoint: `https://graphql.epicgames.com/graphql`
- Query: `searchStore` filtered by namespace UUID
- Filter for `BASE_GAME` offerType only
- Rate limit: 1 req/s conservative
- Timeout: 10 seconds
- Graceful degradation on network error (return null)

---

## 4. Files Changed

| File | Change |
|------|--------|
| `ExecutableDiscovery.cs` | Fix `FindEpicManifest()` extension bug (`*.json` → `*.item`/`*.mancpn`/`*.json`) ✅ |
| `EpicManifestParser.cs` | **New** — `.item`/`.mancpn` parsing, local identifier extraction, path resolution, global cross-ref ✅ |
| `FolderScanner.cs` | Replace `FindEpicManifest()` call with `EpicManifestParser` integration ✅ |
| `EpicManifestParserTests.cs` | **New** — 18 tests for parsing, cross-ref, path normalization ✅ |

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
| `ExtractLocalIdentifiers_MissingDir` | Handle missing `.egstore`/`.egsstore` directories |
| `CrossReferenceGlobalManifests_Match` | Match InstallLocation against game folder |
| `CrossReferenceGlobalManifests_NoMatch` | Return null when no `.item` matches |
| `CrossReferenceGlobalManifests_MissingDir` | Handle missing global manifests directory |
| `PathNormalization_CaseInsensitive` | Case-insensitive path comparison |
| `PathNormalization_TrailingSeparator` | Trailing `\` or `/` stripped |
| `ResolveLaunchExecutable` | Relative path → absolute path |

---

## 7. Success Criteria

- [x] Bug #17 FIXED: `FindEpicManifest()` finds `.item` and `.mancpn` files (not just `.json`)
- [x] Bug #18 FIXED: Epic manifest data extracted — `DisplayName`, `CatalogItemId`, `CatalogNamespace` stored in `PlatformMetadata`
- [x] Bug #19 FIXED: Global `.item` cross-reference works on Windows
- [x] Epic games show marketing names (e.g., "Fortnite" not "Sugar")
- [x] `EpicCatalogItemId` and `EpicCatalogNamespace` stored in `PlatformMetadata`
- [x] Build clean, all tests pass

---

## 8. Overlap with Research/Tools

| Tool | Overlap | Notes |
|------|---------|-------|
| `lookup_metadata.py` `epic_crossref_item_manifests()` | Direct reference | Python implementation to port |
| `lookup_metadata.py` `_extract_epic_identifiers()` | Direct reference | Local parsing logic to port |
| `lookup_metadata.py` `epic_resolve_metadata()` | Combined pipeline | 2-strategy resolution (API deferred) |
| `detect.py` `_check_epic()` | Detection only | Signal detection already implemented |
| `tools/epic_search.py` | Standalone tool | Manual search utility, not for porting |
| `docs/EPIC-MANIFEST-ENRICHMENT.md` | Analysis | Comprehensive reference (654 lines) |

---

## 9. Future Work (Out of Scope)

- **Epic GraphQL API lookup** — Deferred. Add as fallback when online metadata is enabled (see Phase: DEFERRED above)
- **PCGW cross-referencing** using Epic `CatalogItemId` → PCGW `Epic_Games_Store_ID`
- **Cover art download** from Epic `keyImages`
- **DLC detection** — **not** `MainGame*` (empty on live files). Use `AppCategories`/`TechnicalType` = `addons`. See `docs/research/epic_item_format.md`.
- **Epic launcher process detection** for running game detection

---

**Last updated:** 2026-07-26
**Related:** `docs/EPIC-MANIFEST-ENRICHMENT.md`, `planning/102-tags-metadata-display.md` Phase 3
