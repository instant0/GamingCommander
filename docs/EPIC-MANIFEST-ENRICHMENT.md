# Epic Manifest Enrichment — Complete Analysis

**Nature:** Reference document. Analysis of Epic Games Store detection and metadata enrichment.  
**Audience:** All agents. Read when modifying Epic detection, manifest parsing, or metadata enrichment.  
**Status:** Plan **109** (folder enrich) COMPLETE. **Not** the live Epic VFS spec.  
**Read instead for DLC/orphan/regen:** `docs/research/epic_item_format.md`. **Catalog VFS:** `planning/121-epic-manifest-vfs-investigation.md`.  
API lookup still deferred (tools only).

---

## 0. Overview

Epic Games Store games present a unique detection challenge:

1. **Detection is signal-based only** — `.egstore/` or `.egsstore/` directory at game root
2. **No in-folder manifest parsing** — unlike GOG (`.info`) or Steam (`.acf`), the authoritative metadata lives **outside** the game folder in a global manifest directory
3. **GUID-based identification** — games are identified by `CatalogNamespace` (UUID), `CatalogItemId` (UUID), and `AppName` (internal codename), none of which are human-readable
4. **Folder names are unreliable** — Epic sometimes uses codenames (e.g., `Sugar` for Fortnite, `Copper` for Unreal Engine) or abbreviated names

The enrichment task addresses all of these gaps using two complementary strategies:
- **Strategy 1:** Cross-reference `.item` manifest files from the global ProgramData directory
- **Strategy 2:** Query the Epic GraphQL `searchStore` API using namespace UUIDs extracted from local files

---

## 1. Epic Game Folder Structure

### 1.1 Detection Signals (Current)

Epic games are detected by `StoreSignalDetector.HasEpicSignal()`:

```
GameFolder/
  ├─ .egstore/           ← Primary signal (directory exists)
  │   ├─ manifests/      ← Local manifest data
  │   │   ├─ *.mancpn    ← Catalog namespace + item ID (JSON)
  │   │   └─ *.item      ← Rich metadata (JSON, if present)
  │   └─ bps/            ← Backup patches
  └─ Game.exe
```

Or the alternative path:
```
GameFolder/
  ├─ .egsstore/          ← Alternative signal directory
  │   └─ manifests/
  │       └─ *.item
```

**Detection status:** ✅ Signal-only. `.egstore/` or `.egsstore/` directory presence classified as `GameSourceKind.Epic`.

### 1.1b Current C# Implementation Status

> **Plan 109 implemented (2026-07-26).** Tables below updated 2026-08-10 (Plan 118). Historical “broken/not implemented” narrative in later sections is superseded by this status.

| Feature | Status | Location | Notes |
|---------|--------|----------|-------|
| `.egstore/` detection | ✅ Implemented | `StoreSignalDetector.HasEpicSignal()` | — |
| Local manifest file search | ✅ Implemented | `ExecutableDiscovery.FindEpicManifest()` | Prefers `*.item`, `*.mancpn`, then `*.json` |
| Local `.item`/`.mancpn` parsing | ✅ Implemented | `EpicManifestParser` | ExtractLocalIdentifiers |
| Global `.item` cross-reference | ✅ Implemented | `EpicManifestParser.CrossReferenceGlobalManifests()` | ProgramData Manifests path |
| `CatalogItemId`/`CatalogNamespace` extraction | ✅ Implemented | FolderScanner + PlatformMetadata | — |
| `LaunchExecutable` → absolute path | ✅ Implemented | Plan 109 Phase 4 | — |
| Epic GraphQL API lookup | ❌ Deferred | — | Future plan; not required for local names |
| Tests | ❌ None | — | — |

**Result:** Epic games are detected by signal but show codename/folder names. `ManifestPath` is stored but no data is extracted from it. See TECH_DEBT bugs #17, #18, #19.

### 1.2 Local Manifest Files

Epic stores two types of manifest files inside the game folder's `.egstore/manifests/` directory:

#### `.mancpn` Files (Catalog Namespace Pointer)

```json
{
  "CatalogItemId": "a]b1c2d3-e4f5-6789-abcd-ef0123456789",
  "CatalogNamespace": "caca23a0-954f-4c1a-ba1f-dd7e277b81e2",
  "AppName": "Fortnite"
}
```

**Fields:**
| Field | Description | Example |
|-------|-------------|---------|
| `CatalogItemId` | Unique item UUID in Epic's catalog | `"abc123d4-e5f6-7890-abcd-ef0123456789"` |
| `CatalogNamespace` | Namespace UUID (public game namespace) | `"caca23a0-954f-4c1a-ba1f-dd7e277b81e2"` |
| `AppName` | Internal codename (NOT the display name) | `"Fortnite"`, `"Sugar"` (codename) |

**Key insight:** The `CatalogNamespace` is the critical identifier. It resolves to the public game namespace that maps to the correct Epic Store listing via the GraphQL API.

**WARNING:** The `CatalogNamespace` in `.mancpn` may sometimes be a **dev/testing** namespace (not the public game namespace). For example, Death Stranding's `.mancpn` has namespace `f4a904...` which resolves to `BogaDevAudience`, an internal testing tool. The `.item` file (from the global manifests directory) always has the **correct** public namespace.

#### `.item` Files (Rich Metadata)

```json
{
  "FormatVersion": 0,
  "bIsIncompleteInstall": false,
  "LaunchExecutable": "FortniteGame\\Binaries\\Win64\\FortniteClient-Win64-Shipping.exe",
  "DisplayName": "Fortnite",
  "InstallLocation": "D:\\Epic Games\\Fortnite",
  "CatalogNamespace": "caca23a0-954f-4c1a-ba1f-dd7e277b81e2",
  "CatalogItemId": "abc123d4-e5f6-7890-abcd-ef0123456789",
  "AppName": "Fortnite",
  "AppVersionString": "1.0.0.0",
  "MainGameCatalogNamespace": "caca23a0-954f-4c1a-ba1f-dd7e277b81e2",
  "MainGameCatalogItemId": "abc123d4-e5f6-7890-abcd-ef0123456789",
  "MainGameAppName": "Fortnite",
  "InstallSize": 0,
  "bIsApplication": true,
  "bIsExecutable": true,
  "MandatoryAppFolderName": "Fortnite"
}
```

**Fields available for enrichment:**
| Field | Description | Use |
|-------|-------------|-----|
| `DisplayName` | Human-readable game name | **Authoritative display name** |
| `InstallLocation` | Absolute path to game install | Cross-reference matching |
| `LaunchExecutable` | Relative path to game exe | Exe resolution |
| `CatalogNamespace` | Public namespace UUID | Epic API lookup |
| `CatalogItemId` | Catalog item UUID | Cross-platform ID |
| `AppName` | Internal app name | Internal reference |
| `InstallSize` | Install size in bytes | Size metadata |
| `MainGameCatalogNamespace` | Parent game namespace | DLC identification |

**Key insight:** The `.item` file contains the **Display Name** — this is the authoritative game name from Epic's servers, not the folder name. This alone resolves the primary metadata gap.

---

## 2. Global Manifest Directory

### 2.1 Location

The Epic Games Launcher writes `.item` files to a **global** manifest directory, separate from individual game folders:

```
C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests\
```

On Windows, `C:\ProgramData` is typically `C:\ProgramData` (not `%AppData%`).

### 2.2 Cross-Reference Strategy

The key insight is that each `.item` file contains an `InstallLocation` field that points to the game's actual install path. By reading all `.item` files and matching `InstallLocation` against the scanned game folder, we can resolve:

```
Game folder: D:\Epic Games\Fortnite\
    ↓
Scan all .item files in C:\ProgramData\Epic\...\Manifests\
    ↓
Match: "InstallLocation" = "D:\Epic Games\Fortnite"
    ↓
Extract: DisplayName = "Fortnite", LaunchExecutable = "...", CatalogNamespace = "..."
```

### 2.3 Python Reference Implementation

The Python tool `lookup_metadata.py` implements this in `epic_crossref_item_manifests()`:

```python
def epic_crossref_item_manifests(folder_path: Path) -> dict | None:
    """Cross-reference an Epic game folder against the global
    .item manifest directory."""
    global EPIC_MANIFESTS_DIR
    manifests_path = Path(EPIC_MANIFESTS_DIR) if EPIC_MANIFESTS_DIR else None
    if not manifests_path or not manifests_path.is_dir():
        return None

    target = str(folder_path.resolve()).lower()
    for item_file in manifests_path.glob("*.item"):
        try:
            data = json.loads(item_file.read_text(encoding="utf-8", errors="ignore"))
        except (json.JSONDecodeError, OSError):
            continue
        install_loc = data.get("InstallLocation", "")
        if install_loc and install_loc.lower().rstrip("\\/") == target.rstrip("\\/"):
            return data
    return None
```

**Algorithm:**
1. Resolve the global manifests directory path
2. For each `.item` file in the directory
3. Parse JSON
4. Compare `InstallLocation` against the game folder path (case-insensitive, trailing separator stripped)
5. Return the matching `.item` data dict

**Path normalization:** Both sides have trailing `\` or `/` stripped and comparison is case-insensitive. This handles path separator differences and casing inconsistencies.

---

## 3. Epic GraphQL API (searchStore)

### 3.1 Purpose

When `.item` files are not available (Linux, missing ProgramData, or the `.mancpn` has a dev namespace), the Epic GraphQL API can resolve namespace UUIDs to human-readable game metadata.

### 3.2 API Endpoint

```
https://graphql.epicgames.com/graphql
```

### 3.3 Query

```graphql
{
  Catalog {
    searchStore(
      start: 0,
      count: 5,
      namespace: "<catalog-namespace-uuid>"
    ) {
      elements {
        title
        id
        namespace
        productSlug
        publisherDisplayName
        developerDisplayName
        releaseDate
        offerType
        status
        description
        keyImages { type url }
        customAttributes { key value }
      }
    }
  }
}
```

### 3.4 Response Fields

The response `elements` array contains store offers (base game + editions). Key fields:

| Field | Description | Example |
|-------|-------------|---------|
| `title` | Display name | `"Fortnite"` |
| `offerType` | `"BASE_GAME"` vs `"DLC"` / `"EDITION"` | Filter to BASE_GAME |
| `publisherDisplayName` | Publisher | `"Epic Games"` |
| `developerDisplayName` | Developer | `"Epic Games"` |
| `productSlug` | Store URL path | `"fortnite"` |
| `releaseDate` | ISO date string | `"2017-07-25T00:00:00.000Z"` |
| `description` | Short description | `"Drop in, gear up, and compete..."` |
| `keyImages` | Array of `{type, url}` | Cover art URLs |
| `customAttributes` | Structured key/value pairs | `developerName`, `productSlug` |

### 3.5 Custom Attributes

```json
[
  {"key": "developerName", "value": "Epic Games"},
  {"key": "publisherName", "value": "Epic Games"},
  {"key": "com.epicgames.app.productSlug", "value": "fortnite"}
]
```

### 3.6 Namespace UUID WARNING

**Critical:** The `CatalogNamespace` in `.mancpn` files (inside the game folder) may be a **dev/testing** namespace, not the public game namespace. For example:

- Death Stranding `.mancpn` → namespace `f4a904...` → resolves to `BogaDevAudience` (internal tool)
- Death Stranding `.item` (global manifests) → correct public namespace

**Resolution priority:**
1. `.item` file from global manifests directory (most reliable)
2. `.mancpn` from game folder (may be dev namespace — use with caution)
3. `.egstore/manifests/*.item` from game folder (if present)

---

## 4. Identifier Resolution Pipeline

### 4.1 Complete Resolution Chain

```
Epic game detected (.egstore/ signal)
  │
  ├─ Step 1: Extract local identifiers
  │   ├─ Read .egstore/manifests/*.mancpn → CatalogNamespace, CatalogItemId, AppName
  │   └─ Read .egstore/manifests/*.item (if present) → DisplayName, LaunchExecutable, etc.
  │
  ├─ Step 2: Global .item cross-reference
  │   ├─ Scan C:\ProgramData\Epic\...\Manifests\*.item
  │   ├─ Match InstallLocation against game folder
  │   └─ Extract: DisplayName, LaunchExecutable, CatalogNamespace (correct)
  │
  ├─ Step 3: Epic API lookup (fallback)
  │   ├─ Use CatalogNamespace from Step 2 (preferred) or Step 1
  │   ├─ Query searchStore GraphQL API
  │   ├─ Filter for BASE_GAME offerType
  │   └─ Extract: title, developer, publisher, slug, releaseDate, coverArt
  │
  └─ Step 4: Enrich GameEntry
      ├─ DisplayName = .item DisplayName (Step 2) OR API title (Step 3)
      ├─ ExecutablePath = .item LaunchExecutable (resolved to absolute)
      ├─ PlatformMetadata["EpicCatalogItemId"] = CatalogItemId
      ├─ PlatformMetadata["EpicCatalogNamespace"] = CatalogNamespace
      ├─ PlatformMetadata["EpicAppName"] = AppName
      ├─ PlatformMetadata["TitleSource"] = "EpicItemManifest" or "EpicSearchStore"
      └─ PlatformMetadata["AutoDetectedTitle"] = original folder name
```

### 4.2 Name Resolution Priority

| Priority | Source | Reliability | Data |
|----------|--------|-------------|------|
| 1 | Global `.item` `DisplayName` | ✅ Highest | Authoritative marketing name |
| 2 | Epic GraphQL API `title` | ✅ High | May include editions/DLC if namespace is wrong |
| 3 | Local `.item` `DisplayName` | ⚠️ Medium | May be stale or missing |
| 4 | Local `.mancpn` → API lookup | ⚠️ Medium | Risk of dev namespace |
| 5 | Folder name (fallback) | ⚠️ Low | May be codename or abbreviated |

---

## 5. C# Implementation Status

### 5.1 Current C# Capabilities

| Feature | Status | Location | Plan |
|---------|--------|----------|------|
| `.egstore/` detection | ✅ Implemented | `StoreSignalDetector.HasEpicSignal()` | — |
| Local `.item`/`.mancpn`/`.json` lookup | ✅ Fixed (Bug #17) | `ExecutableDiscovery.FindEpicManifest()` | Plan 109 Phase 1 |
| Global `.item` cross-reference | ✅ Implemented (Bug #19) | `EpicManifestParser` | Plan 109 Phase 3 |
| Epic GraphQL API lookup | ❌ Deferred | — | **DEFERRED** |
| `CatalogItemId`/`CatalogNamespace` extraction | ✅ Implemented (Bug #18) | FolderScanner + parser | Plan 109 Phases 2+4 |
| `LaunchExecutable` → absolute path resolution | ✅ Implemented | Plan 109 Phase 4 | Plan 109 Phase 4 |

### 5.2 FindEpicManifest — Current Implementation

```csharp
// ExecutableDiscovery.cs — searches LOCAL .egstore/.egsstore for manifests
internal static string FindEpicManifest(DirectoryInfo dir)
{
    string[] egsPaths = [
        Path.Combine(dir.FullName, ".egsstore", "manifests"),
        Path.Combine(dir.FullName, ".egstore", "manifests"),
        Path.Combine(dir.FullName, "manifests"),
    ];

    foreach (string manifestsDir in egsPaths)
    {
        if (!Directory.Exists(manifestsDir)) continue;
        try
        {
            foreach (FileInfo jsonFile in new DirectoryInfo(manifestsDir).GetFiles("*.json"))
                return jsonFile.FullName;
        }
        catch { }
    }
    return string.Empty;
}
```

**Issues:**
1. Only searches for `*.json` — misses `*.item` and `*.mancpn` files (Epic uses `.item` and `.mancpn` extensions, not `.json`)
2. Only looks inside the game folder — doesn't check the global ProgramData manifests directory
3. Returns the first found file path but doesn't parse any data from it
4. Doesn't extract `DisplayName`, `LaunchExecutable`, or GUID identifiers

### 5.3 FolderScanner Epic Integration — Current

```csharp
// FolderScanner.cs line 201
string manifestPath = ExecutableDiscovery.FindEpicManifest(subDir);
// ... later ...
ManifestPath: manifestPath,
```

The `ManifestPath` is stored on `GameEntry` but **no metadata is extracted from it**. The F4 dialog shows it as "Epic Manifest" field (BUG-7 fix), but the data is not used for enrichment.

### 5.4 Gap Summary

| Python Feature | C# Status | Gap | Plan |
|----------------|-----------|-----|------|
| `epic_crossref_item_manifests()` — global `.item` cross-ref | ✅ Implemented | — | Plan 109 Phase 3 |
| `epic_resolve_metadata()` — combined pipeline | ✅ Local strategies | API strategy deferred | Plan 109 Phase 4 |
| `_extract_epic_identifiers()` — local `.mancpn`/`.item` | ✅ Implemented | — | Plan 109 Phase 2 |
| `epic_search_by_namespace()` — GraphQL API | ❌ Not implemented | **DEFERRED** | Future plan |
| GUID-based identification | ✅ Implemented | — | Plan 109 Phase 2 |
| Display name resolution from manifests | ✅ Implemented | — | Plan 109 Phase 4 |
| `.mancpn` parsing | ✅ Implemented | — | Plan 109 Phase 2 |
| `.item` parsing (local + global) | ✅ Implemented | — | Plan 109 Phases 2+3 |

> **Note:** §§5.2–5.3 code snippets below are **historical** (pre–Plan 109). Prefer §1.1b and live sources `EpicManifestParser.cs` / `ExecutableDiscovery.FindEpicManifest`.

---

## 6. Proposed C# Implementation

### 6.1 New Class: `EpicManifestParser`

```
src/GamingCommander.App/Services/EpicManifestParser.cs
```

Responsibilities:
1. Parse `.item` files (both local `.egstore/manifests/` and global ProgramData)
2. Parse `.mancpn` files (local `.egstore/manifests/`)
3. Cross-reference game folders against global manifests directory
4. Extract GUID identifiers (CatalogNamespace, CatalogItemId, AppName)
5. Resolve `LaunchExecutable` relative paths to absolute paths

### 6.2 New Class: `EpicStoreApiClient`

```
src/GamingCommander.App/Services/EpicStoreApiClient.cs
```

Responsibilities:
1. Query `searchStore` GraphQL API by namespace UUID
2. Parse response for BASE_GAME offers
3. Extract title, developer, publisher, slug, releaseDate, coverArt
4. Rate limiting (no official rate limit documented, but conservative 1 req/s)

### 6.3 Integration into FolderScanner

```csharp
// In FolderScanner.AddGameEntry(), after Epic detection:
if (resolvedType == GameSourceKind.Epic)
{
    // Strategy 1: Local identifier extraction
    var localIds = EpicManifestParser.ExtractLocalIdentifiers(subDir);
    
    // Strategy 2: Global .item cross-reference
    var globalItem = EpicManifestParser.CrossReferenceGlobalManifests(subDir);
    if (globalItem != null)
    {
        // Authoritative display name from global .item
        if (!string.IsNullOrEmpty(globalItem.DisplayName))
        {
            platformMetadata["AutoDetectedTitle"] = displayName;
            displayName = globalItem.DisplayName;
            platformMetadata["TitleSource"] = "EpicItemManifest";
        }
        // Override local namespace with correct public namespace
        localIds ??= new EpicIdentifiers();
        localIds.CatalogNamespace = globalItem.CatalogNamespace;
        localIds.CatalogItemId = globalItem.CatalogItemId;
    }
    
    // Strategy 3: API lookup (if namespace available and online metadata enabled)
    if (_config.EnableOnlineMetadata && !string.IsNullOrEmpty(localIds?.CatalogNamespace))
    {
        var apiResult = await EpicStoreApiClient.LookupByNamespaceAsync(localIds.CatalogNamespace);
        if (apiResult != null && !string.IsNullOrEmpty(apiResult.Title))
        {
            if (string.IsNullOrEmpty(globalItem?.DisplayName))
            {
                platformMetadata["AutoDetectedTitle"] = displayName;
                displayName = apiResult.Title;
                platformMetadata["TitleSource"] = "EpicSearchStore";
            }
        }
    }
    
    // Store GUID identifiers
    if (localIds != null)
    {
        if (!string.IsNullOrEmpty(localIds.CatalogItemId))
            platformMetadata["EpicCatalogItemId"] = localIds.CatalogItemId;
        if (!string.IsNullOrEmpty(localIds.CatalogNamespace))
            platformMetadata["EpicCatalogNamespace"] = localIds.CatalogNamespace;
        if (!string.IsNullOrEmpty(localIds.AppName))
            platformMetadata["EpicAppName"] = localIds.AppName;
    }
}
```

### 6.4 Path Configuration

The global manifests directory path needs to be configurable:

```csharp
public static class EpicManifestPaths
{
    /// <summary>
    /// Default global manifests directory on Windows.
    /// %ProgramData%\Epic\EpicGamesLauncher\Data\Manifests\
    /// </summary>
    public static string DefaultManifestsDir =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Epic", "EpicGamesLauncher", "Data", "Manifests");
    
    /// <summary>
    /// Can be overridden via environment variable or config.
    /// </summary>
    public static string GetManifestsDir(string? overridePath = null)
    {
        return overridePath
            ?? Environment.GetEnvironmentVariable("EPIC_MANIFESTS_DIR")
            ?? DefaultManifestsDir;
    }
}
```

---

## 7. Testing and Validation

### 7.1 Mock Data (Existing)

`tools/setup_mock_data.py` creates a mock Epic game structure:

```python
epic_root = MOCK_ROOT / "epic"
epic_game = epic_root / "EpicGameGamma"
epic_manifest_dir = epic_game / ".egsstore" / "manifests"

write_binary_file(epic_game / "GameGamma.exe")
write_file(epic_manifest_dir / "abc123.item",
           make_epic_item(
               display_name="Mock Epic Game Gamma",
               app_name="MockEpicGamma",
               install_location=str(epic_game),
               launch_executable="GameGamma.exe",
               catalog_namespace="ns_gamma_1234",
               catalog_item_id="item_gamma_5678",
               installation_guid="ABCDEF1234567890ABCDEF1234567890",
           ))
```

The `make_epic_item()` helper generates a complete `.item` JSON with all standard fields, matching the real Epic launcher format.

### 7.2 GUID Testing

The GUID format used by Epic follows standard UUID v4:
- `CatalogNamespace`: e.g., `caca23a0-954f-4c1a-ba1f-dd7e277b81e2`
- `CatalogItemId`: e.g., `abc123d4-e5f6-7890-abcd-ef0123456789`
- `InstallationGuid`: e.g., `ABCDEF1234567890ABCDEF1234567890` (32 hex chars, no dashes)

**Testing with GUIDs:** The Python tool was tested by:
1. Creating mock `.item` files with known GUIDs
2. Matching `InstallLocation` against mock game folder paths
3. Verifying that `DisplayName` is correctly extracted from `.item`
4. Verifying that `CatalogNamespace` from `.item` is the correct public namespace (vs `.mancpn` dev namespace)

### 7.3 Test Plan for C# Implementation

| Test | Description |
|------|-------------|
| `EpicManifestParserTests.ParseItemFile` | Parse `.item` JSON, extract all fields |
| `EpicManifestParserTests.ParseMancpnFile` | Parse `.mancpn` JSON, extract identifiers |
| `EpicManifestParserTests.CrossReferenceMatch` | Match `InstallLocation` against game folder |
| `EpicManifestParserTests.CrossReferenceNoMatch` | Return null when no `.item` matches |
| `EpicManifestParserTests.CrossReferenceMissingDir` | Handle missing global manifests directory |
| `EpicManifestParserTests.PathNormalization` | Case-insensitive, trailing separator handling |
| `EpicManifestParserTests.LocalIdentifiersFromEgstore` | Extract from `.egstore/manifests/` |
| `EpicManifestParserTests.LocalIdentifiersFromEgsstore` | Extract from `.egsstore/manifests/` |
| `EpicManifestParserTests.LaunchExecutableResolution` | Relative path → absolute path |
| `EpicStoreApiClientTests.LookupByNamespace` | Mock HTTP response, parse GraphQL result |
| `EpicStoreApiClientTests.FilterBaseGameOffer` | Only return BASE_GAME offers |
| `EpicStoreApiClientTests.HandleApiError` | Graceful degradation on network error |
| `EpicStoreApiClientTests.HandleDevNamespace` | Detect and handle dev namespace |

### 7.4 Real-World Validation

Testing on actual hardware (Windows) with the Python tool revealed:

| Finding | Detail |
|---------|--------|
| `.mancpn` namespace may be dev namespace | Death Stranding: `f4a904...` → `BogaDevAudience` |
| `.item` namespace is always correct | Global manifests have the public namespace |
| `InstallLocation` uses Windows paths | `D:\Epic Games\Fortnite` (backslash) |
| `LaunchExecutable` is relative | `FortniteGame\Binaries\Win64\FortniteClient-Win64-Shipping.exe` |
| Some games have multiple `.item` files | DLC/editions have separate `.item` files |
| `bIsIncompleteInstall` can be true | Games being downloaded/updating |
| `InstallSize` is often 0 | Not reliably populated |

---

## 8. Data Flow Diagram

```
                    ┌─────────────────────┐
                    │   Library Root Scan  │
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │ StoreSignalDetector  │
                    │ .egstore/ → Epic     │
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │ FolderScanner        │
                    │ AddGameEntry()       │
                    └──────────┬──────────┘
                               │
              ┌────────────────┼────────────────┐
              │                │                │
    ┌─────────▼─────────┐ ┌───▼────────────┐ ┌─▼──────────────────┐
    │ Local .mancpn     │ │ Global .item   │ │ Epic GraphQL API   │
    │ .egstore/manifests │ │ ProgramData/   │ │ searchStore        │
    │ (identifiers)     │ │ (cross-ref)    │ │ (metadata)         │
    └─────────┬─────────┘ └───┬────────────┘ └─┬──────────────────┘
              │                │                │
              └────────────────┼────────────────┘
                               │
                    ┌──────────▼──────────┐
                    │ GameEntry enriched   │
                    │ • DisplayName        │
                    │ • ExecutablePath     │
                    │ • CatalogItemId      │
                    │ • CatalogNamespace   │
                    └─────────────────────┘
```

---

## 9. Relationship to Plan 102 (Tags + Metadata Display)

The Epic Manifest Enrichment is a **prerequisite** for Plan 102's Phase 3 (Metadata Scraping):

1. **Epic manifest enrichment** provides the correct game name and store IDs locally
2. **Plan 102 Phase 3** uses those IDs for PCGW/Steam cross-referencing
3. **Epic CatalogItemId** can map to PCGW's `Epic_Games_Store_ID` field
4. **Epic CatalogNamespace** enables the GraphQL API lookup for additional metadata

The enrichment should be implemented **before** Plan 102 Phase 3 to ensure Epic games have correct names for PCGW lookups.

---

## 10. Implementation Priority

**Plan 109 Status:** ✅ COMPLETE (Phases 1–6). Phase 7 API still deferred.

| Phase | Feature | Priority | Status |
|-------|---------|----------|--------|
| 1 | Fix `FindEpicManifest()` extension bug (#17) | High | ✅ Done |
| 2 | Local `.mancpn`/`.item` parsing (#18 partial) | High | ✅ Done |
| 3 | Global `.item` cross-reference (#19) | High | ✅ Done |
| 4 | `LaunchExecutable` → absolute path resolution | Medium | ✅ Done |
| 5 | Integration into `FolderScanner` (#18 full) | High | ✅ Done |
| 6 | Tests | High | ✅ Done |
| 7 | Epic GraphQL API client | Medium | **DEFERRED** |

**Deferred:** Epic GraphQL API client — fallback when online metadata is needed.

---

## 11. Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Parse `.item` not `.json` | Match file extension | `FindEpicManifest` currently searches `*.json` — wrong extension |
| Global manifests dir configurable | Env var + config | Different machines may have different paths; Linux may not have ProgramData |
| `.mancpn` as identifier source only | Don't use for display name | May have dev namespace; use only for API lookup |
| `.item` as primary display name source | Authoritative | Written by Epic launcher with marketing name |
| API as fallback only | Graceful degradation | Network dependency undesirable; `.item` is sufficient for most cases |
| `InstallLocation` comparison case-insensitive | Windows paths | File system is case-insensitive; paths may differ in casing |
| `bIsIncompleteInstall` check | Skip incomplete | Don't enrich games being downloaded |

---

**Last updated:** 2026-08-10 (Plan 118 — body status aligned with Plan 109 complete; API still deferred)  
**Related documents:** `docs/GAME-DETECTION-LOGIC.md`, `planning/102-tags-metadata-display.md` (Phase 3), `planning/109-epic-manifest-enrichment.md`
