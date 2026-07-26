# Plan 110 — User Tags, Source Tagging & Override Protection

**Status:** DRAFT — awaiting approval
**Audience:** Planner / Builder
**Priority:** P2 (post-MVP)
**Depends on:** MVP complete ✅
**Reference:** `planning/102-tags-metadata-display.md` (full 4-phase plan, 1009 lines)

---

## 0. Problem Statement

Three interconnected needs:

1. **User-generated tags** — Users want to tag games with custom labels (RPG, Co-op, Story Rich) via F4
2. **Source tagging** — User-entered data must be tagged with its source ("User") so automated enrichment can distinguish it from auto-detected data
3. **Override protection** — When automated metadata providers (PCGW, Steam, Epic) fetch new data, they must NOT overwrite fields the user has manually set

---

## 1. Current State Analysis

### 1.1 GameEntry Model (Current)

```csharp
public sealed record GameEntry(
    string Id,
    string FolderName,
    string DisplayName,          // ← Can be user-overridden
    GameSourceKind GameSource,   // ← Has IsSourceOverridden flag
    bool IsSourceOverridden,     // ← Tracks source type override ONLY
    string ExecutablePath,       // ← Can be user-overridden
    string LauncherPath,         // ← Can be user-overridden
    string CommandLineArguments, // ← Can be user-overridden
    string ManifestPath,         // ← Can be user-overridden
    DateTimeOffset LastScanned,
    DateTimeOffset LastModified,
    Dictionary<string, string> PlatformMetadata);
```

### 1.2 Current Override Tracking

The `IsSourceOverridden` flag only tracks **one field**: `GameSource`. When the user changes the source type in F4, `IsSourceOverridden` is set to `true` if the new type differs from the root's default.

**What's missing:**
- No tracking for user-overridden `DisplayName`
- No tracking for user-overridden `ExecutablePath`
- No tracking for user-overridden `LauncherPath`
- No tracking for user-overridden `CommandLineArguments`
- No `Tags` field at all
- No source attribution for any field

### 1.3 F4 Dialog Current Behavior

```csharp
// GameSetupWindow.axaml.cs SaveAndClose():
var updated = _originalGame with
{
    DisplayName = DisplayName,
    GameSource = newType,
    IsSourceOverridden = newType != rootDefault,
    ExecutablePath = ExecutablePath,
    LauncherPath = LauncherPath,
    CommandLineArguments = CommandLineArguments,
    ManifestPath = ManifestPath,
};
```

All fields are overwritten unconditionally. No distinction between "user set this" and "scan detected this".

### 1.4 Automated Enrichment Risk

When Phase 3 (Metadata Scraping) is implemented, providers like PCGW will try to:
- Set `DisplayName` from web metadata
- Set `ExecutablePath` from manifest data
- Set `LauncherPath` from store metadata

**Without override protection**, a PCGW lookup could overwrite a user's carefully curated display name with a different variant.

---

## 2. Source Tagging Design

### 2.1 Concept: UserOverrides Dictionary

Add a `UserOverrides` dictionary to `GameEntry` that tracks which fields the user has manually set:

```csharp
public sealed record GameEntry(
    // ... existing fields ...
    
    /// <summary>
    /// Fields manually set by the user via F4. Keys are field names
    /// (e.g., "DisplayName", "ExecutablePath", "Tags"). Automated
    /// enrichment skips fields present in this dictionary.
    /// Values are ISO timestamps of when the override was set.
    /// </summary>
    public Dictionary<string, string> UserOverrides { get; init; } = [];
);
```

**Example:**
```json
{
  "UserOverrides": {
    "DisplayName": "2026-07-26T14:30:00Z",
    "Tags": "2026-07-26T14:30:00Z"
  }
}
```

This tells us: the user manually set `DisplayName` and `Tags`. Automated enrichment must NOT overwrite these fields.

### 2.2 Why Dictionary<string, string>?

- **Keys** are field names — matches the pattern of `PlatformMetadata`
- **Values** are ISO timestamps — useful for debugging and UI ("Last user edit: 2026-07-26")
- **JSON-serializable** — works with existing `JsonFileHelper` infrastructure
- **Backward-compatible** — empty dictionary defaults to `[]`, existing games load correctly

### 2.3 Field Name Registry

```csharp
public static class GameEntryFields
{
    public const string DisplayName = "DisplayName";
    public const string ExecutablePath = "ExecutablePath";
    public const string LauncherPath = "LauncherPath";
    public const string CommandLineArguments = "CommandLineArguments";
    public const string ManifestPath = "ManifestPath";
    public const string GameSource = "GameSource";
    public const string Tags = "Tags";
}
```

---

## 3. Override Protection Design

### 3.1 Rule: User Overrides Take Precedence

When automated enrichment (PCGW, Steam, Epic) wants to update a field:

```
IF field is in UserOverrides:
    SKIP — do not overwrite
ELSE:
    Apply enrichment value
```

### 3.2 Implementation in MetadataService

```csharp
// In MetadataService.LookupAndCacheAsync():
private static GameEntry ApplyMetadata(
    GameEntry game, GameMetadataRecord metadata, UserOverrides userOverrides)
{
    var updated = game;
    
    // DisplayName: only update if user hasn't overridden
    if (!userOverrides.ContainsKey(GameEntryFields.DisplayName)
        && !string.IsNullOrEmpty(metadata.Developer))
    {
        // Don't overwrite — developer is metadata, not display name
    }
    
    // Tags: merge, don't replace (user tags + metadata tags)
    if (metadata.Genre != null)
    {
        var genreTags = metadata.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var mergedTags = updated.Tags.ToList();
        foreach (var genre in genreTags)
        {
            mergedTags = TagNormalizer.AddDistinct(mergedTags, genre.Trim());
        }
        updated = updated with { Tags = mergedTags };
    }
    
    // ExecutablePath: only update if user hasn't overridden
    if (!userOverrides.ContainsKey(GameEntryFields.ExecutablePath)
        && metadata.Engine != null)
    {
        // Engine metadata doesn't set exe path — skip
    }
    
    return updated;
}
```

### 3.3 Merge Strategy for Tags

Tags are **additive**, not replacement:

```
User tags:     ["RPG", "Co-op"]
Metadata tags: ["RPG", "Open World", "Story Rich"]
Merged result: ["RPG", "Co-op", "Open World", "Story Rich"]
```

- User tags are never removed by metadata
- Metadata tags are added (not replacing)
- Dedup via `TagNormalizer.AreEquivalent`

### 3.4 F4 Dialog: Show Override Status

When the user opens F4, show which fields are user-overridden:

```
Display Name: [The Witcher 3: Wild Hunt  ]  ← User override active
Game Type:    [Steam                   ▼]
Executable:   [C:\...\witcher3.exe     ] [..]  ← User override active
Launch Args:  [--launcher-mode          ]
Tags:         [RPG, Open World, Co-op   ]
```

This gives the user visibility into what they've manually set.

---

## 4. Tags Data Model

### 4.1 Add Tags to GameEntry

```csharp
public sealed record GameEntry(
    // ... existing fields ...
    
    /// <summary>User-defined tags: ["RPG", "Open World", "Co-op"]</summary>
    public List<string> Tags { get; init; } = [];
    
    /// <summary>Fields manually set by the user via F4.</summary>
    public Dictionary<string, string> UserOverrides { get; init; } = [];
);
```

### 4.2 Tags in PlatformMetadata (Alternative)

**Rejected approach:** Storing tags in `PlatformMetadata["Tags"]` as comma-separated string.

**Reasons:**
- `PlatformMetadata` is for platform-specific data (Steam status, ACF paths)
- Tags are game-level, not platform-level
- Comma-separated parsing is error-prone (commas in tag names)
- `List<string>` is type-safe and JSON-serializable

### 4.3 Tags Persistence

Tags are stored directly on `GameEntry` in `games.json`:

```json
{
  "Id": "abc123",
  "DisplayName": "The Witcher 3",
  "Tags": ["RPG", "Open World", "Co-op", "Story Rich"],
  "UserOverrides": {
    "DisplayName": "2026-07-26T14:30:00Z",
    "Tags": "2026-07-26T14:30:00Z"
  },
  "PlatformMetadata": { ... }
}
```

---

## 5. Tag Sources & Attribution

### 5.1 Tag Source Tracking

Each tag can have a source attribution:

```csharp
public record TagEntry(
    string Name,
    string Source,     // "User", "Engine", "Store", "PCGW", "Steam", "Epic"
    DateTime AddedAt);
```

**Rejected:** This is overengineered for Phase 1. Keep tags as `List<string>` and track source only at the field level via `UserOverrides`.

### 5.2 Simplified Approach

Tags are a flat `List<string>`. Source attribution is implicit:

| Tag Source | How Added | Override Protection |
|------------|-----------|---------------------|
| User | F4 dialog | ✅ Protected via `UserOverrides["Tags"]` |
| Engine | Auto-tag on scan | ⚠️ Can be removed by user |
| Store | Auto-tag on scan | ⚠️ Can be removed by user |
| Metadata | PCGW/Steam/Epic | ⚠️ Additive only, never removes user tags |

---

## 6. Implementation Plan (Phase 1 Only)

### 6.1 Files Changed

| File | Change |
|------|--------|
| `Core/Models/GameEntry.cs` | Add `Tags` (`List<string>`), `UserOverrides` (`Dictionary<string, string>`) |
| `Core/Models/GameEntryFields.cs` | **New** — Field name constants |
| `Core/Services/TagNormalizer.cs` | **New** — Normalize, AreEquivalent, AddDistinct |
| `App/Services/GamesDatabaseService.cs` | Add `Tags`, `UserOverrides` to `GameEntryDto` with backward-compat defaults |
| `App/GameSetupWindow.axaml` | Add tag editing UI (comma-separated input + existing tags display) |
| `App/GameSetupWindow.axaml.cs` | Handle tag input, set `UserOverrides["Tags"]` on save |

### 6.2 Tests

| Test | Description |
|------|-------------|
| `TagNormalizerTests.Normalize` | Trim, collapse whitespace, preserve case |
| `TagNormalizerTests.AreEquivalent` | Case-insensitive comparison |
| `TagNormalizerTests.AddDistinct` | Dedup with case-insensitive matching |
| `GameEntryTagsTests.DefaultEmpty` | New GameEntry has empty tags |
| `GameEntryTagsTests.BackwardCompat` | Old games.json loads with empty tags |
| `GameEntryUserOverridesTests.DefaultEmpty` | New GameEntry has empty overrides |
| `GameEntryUserOverridesTests.SetOnSave` | F4 save sets override timestamp |
| `GamesDatabaseServiceTests.TagsPersist` | Tags survive save/load cycle |

### 6.3 Success Criteria

- [ ] F4 dialog shows tag input field (comma-separated)
- [ ] Existing tags displayed below input for reference
- [ ] Tags persist to `games.json` as `List<string>` array
- [ ] `UserOverrides` tracks which fields user has manually set
- [ ] Existing games load with empty tags and empty overrides (backward-compatible)
- [ ] Tag normalization works (trim, collapse, dedup)
- [ ] Build clean, all tests pass

---

## 7. Override Protection — Detailed Analysis

### 7.1 Current Fields That Can Be User-Overridden

| Field | F4 Editable | Currently Tracked | Needs Tracking |
|-------|-------------|-------------------|----------------|
| `DisplayName` | ✅ Yes | ❌ No | ✅ Add to `UserOverrides` |
| `GameSource` | ✅ Yes | ✅ `IsSourceOverridden` | ✅ Migrate to `UserOverrides` |
| `ExecutablePath` | ✅ Yes | ❌ No | ✅ Add to `UserOverrides` |
| `LauncherPath` | ✅ Yes | ❌ No | ✅ Add to `UserOverrides` |
| `CommandLineArguments` | ✅ Yes | ❌ No | ✅ Add to `UserOverrides` |
| `ManifestPath` | ✅ Yes (Epic only) | ❌ No | ✅ Add to `UserOverrides` |
| `Tags` | ✅ Yes (Phase 1) | N/A | ✅ Add to `UserOverrides` |

### 7.2 Migration: IsSourceOverridden → UserOverrides

When adding `UserOverrides`, migrate existing `IsSourceOverridden`:

```csharp
// In GamesDatabaseService.Load():
if (game.IsSourceOverridden && !game.UserOverrides.ContainsKey("GameSource"))
{
    game = game with
    {
        UserOverrides = new Dictionary<string, string>(game.UserOverrides)
        {
            ["GameSource"] = game.LastScanned.ToString("O")
        }
    };
}
```

### 7.3 Automated Enrichment Skip List

When a metadata provider wants to update a field:

```csharp
public static bool ShouldApplyEnrichment(
    GameEntry game, string fieldName, out string? reason)
{
    if (game.UserOverrides.ContainsKey(fieldName))
    {
        reason = $"User override active for {fieldName}";
        return false;
    }
    reason = null;
    return true;
}
```

### 7.4 Merge Rules by Field

| Field | Enrichment Strategy | User Override Behavior |
|-------|--------------------|-----------------------|
| `DisplayName` | Replace if better | Skip if user-overridden |
| `ExecutablePath` | Replace if found | Skip if user-overridden |
| `LauncherPath` | Replace if found | Skip if user-overridden |
| `CommandLineArguments` | Replace if found | Skip if user-overridden |
| `Tags` | **Additive** (never replace) | Skip if user-overridden |
| `GameSource` | Never auto-change | Already tracked via `IsSourceOverridden` |
| `PlatformMetadata` | Merge keys | Never override user-set keys |

---

## 8. Future Phases (Out of Scope for Phase 1)

| Phase | Feature | Depends On |
|-------|---------|------------|
| Phase 2 | Engine detection + auto-tagging | Phase 1 (Tags field) |
| Phase 3 | Metadata scraping (PCGW, Steam, Epic) | Phase 1 + Plan 109 (Epic manifests) |
| Phase 4 | Display system (right pane badges) | Phase 1 + 2 + 3 |
| Phase 5 | Filter system (by tag, engine, store) | Phase 1 + 4 |

---

## 9. Overlap with Other Plans

| Plan | Overlap | Notes |
|------|---------|-------|
| Plan 102 (Tags + Metadata Display) | Parent plan | This is Phase 1 of Plan 102 |
| Plan 109 (Epic Manifest Enrichment) | Phase 3 prerequisite | Epic enrichment feeds metadata into tags |
| Plan 108 (Steam Status Messages) | PlatformMetadata | Tags are separate from platform status |
| Plan 106 (Unified Setup Screen) | Config toggle | `EnableOnlineMetadata` checkbox already implemented |

---

## 10. Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Tags field type | `List<string>` | Type-safe, JSON-serializable, easy to query |
| Override tracking | `Dictionary<string, string>` | Flexible, matches `PlatformMetadata` pattern |
| Override value | ISO timestamp | Useful for debugging, UI display |
| Tag merge strategy | Additive only | User tags are never removed by automation |
| `IsSourceOverridden` | Keep + migrate | Backward-compatible, migrate to `UserOverrides` |
| Tag source tracking | Implicit (Phase 1) | Overengineered for initial implementation |

---

**Last updated:** 2026-07-26
**Related:** `planning/102-tags-metadata-display.md`, `planning/109-epic-manifest-enrichment.md`
