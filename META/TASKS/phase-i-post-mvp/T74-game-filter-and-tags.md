# Task T74: Game Filter (F5) + User-Editable Tags

**Tier:** 1 — Feature
**Phase:** I — Post-MVP Navigation
**Effort:** ~2–3 hours
**Risk:** Low
**Status:** Pending
**Prerequisites:** T72 (Mode Switcher — for engine data), T73 (Flatten — optional)
**WP:** Post-MVP (Plan 101, Phase 3)

---

## Objective

Add a user-editable `Tags` field to game entries and implement F5 as a filter button that filters the game list by store, engine, and tags. Tags are the foundation for genre/category filtering — user-curated now, PCGW auto-populate later.

## What Needs to Change

### 1. `src/GamingCommander.Core/Models/GameEntry.cs`

Add field:
```csharp
List<string> Tags = []
```

### 2. `src/GamingCommander.App/Services/GamesDatabaseService.cs`

Add `Tags` to `GameEntryDto`:
```csharp
public List<string>? Tags { get; set; }
```

Update Load/Save mapping to include `Tags`. Backward-compatible: missing field defaults to `[]`.

### 3. `src/GamingCommander.App/GameSetupWindow.axaml`

Add tags input field after existing fields:
```xml
<StackPanel Spacing="4">
    <TextBlock Text="Tags" Foreground="..." FontSize="..." />
    <TextBox Text="{Binding TagsInput}" ... />
    <TextBlock Text="Comma-separated: RPG, Open World, Co-op" Foreground="..." FontSize="..." />
</StackPanel>
```

### 4. `src/GamingCommander.App/GameSetupWindow.axaml.cs`

Add tag handling:
```csharp
public string TagsInput { get; set; }  // comma-separated display

// In constructor:
TagsInput = string.Join(", ", game.Tags);

// In SaveAndClose():
var tags = TagsInput
    .Split(',', StringSplitOptions.RemoveEmptyEntries)
    .Select(t => NormalizeTag(t))  // lowercase + trim + collapse whitespace
    .Where(t => t.Length > 0)
    .Distinct()
    .ToList();
```

**Tag normalization (critical for dedup):**
```csharp
private static string NormalizeTag(string tag)
{
    return Regex.Replace(tag.Trim().ToLowerInvariant(), @"\s+", " ");
}
```

This ensures "RPG", "rpg", " Rpg ", "roleplaying game" (after alias resolution in Phase 4) are handled correctly.

### 5. New File: `src/GamingCommander.App/Services/FilterService.cs`

Filter logic:
```csharp
public static class FilterService
{
    public static IReadOnlyList<GameEntry> ApplyFilter(
        IReadOnlyList<GameEntry> games,
        IReadOnlySet<GameSourceKind>? storeFilter,    // null = no filter
        IReadOnlySet<GameEngineKind>? engineFilter,   // null = no filter
        IReadOnlySet<string>? tagFilter)              // null = no filter
    {
        return games.Where(g =>
            (storeFilter == null || storeFilter.Contains(g.GameSource)) &&
            (engineFilter == null || engineFilter.Contains(g.GameEngine)) &&
            (tagFilter == null || g.Tags.Any(t => tagFilter.Contains(t)))
        ).ToList();
    }

    public static IReadOnlyList<string> GetAllTags(IReadOnlyList<GameEntry> games)
    {
        return games.SelectMany(g => g.Tags).Distinct().Order().ToList();
    }
}
```

### 6. New File: `src/GamingCommander.App/FilterWindow.axaml`

Filter dialog UI:
- Store checkboxes (one per GameSourceKind with games)
- Engine checkboxes (one per GameEngineKind with games)
- Tags multi-select (one per unique tag across all games)
- Apply / Clear / Cancel buttons

### 7. New File: `src/GamingCommander.App/FilterWindow.axaml.cs`

Filter dialog code-behind:
- Populate checkboxes from game data
- Return selected filters on Apply
- Store state for re-opening

### 8. `src/GamingCommander.App/MainWindow.axaml.cs`

F5 handler:
```csharp
case Key.F5:
    await OpenFilterAsync();
    e.Handled = true;
    break;
```

Command dispatcher:
```csharp
case "F5":
    _ = OpenFilterAsync();
    break;
```

### 9. `src/GamingCommander.UI/ViewModels/ShellViewModel.cs`

Add filter state and methods:
```csharp
private IReadOnlyList<GameEntry>? _filteredGames;
private IReadOnlyList<ShellPaneItemViewModel>? _preFilterItems;

public void ApplyFilter(IReadOnlyList<GameEntry> filteredGames)
{
    // Cache current items for restore
    _preFilterItems = Items.ToList();
    _filteredGames = filteredGames;
    
    // Rebuild Items from filtered games
    LoadGamesFromEntries(filteredGames);
    StatusText = $"Filter: {filteredGames.Count} game(s) matched";
}

public void ClearFilter()
{
    if (_preFilterItems != null)
    {
        Items.Clear();
        foreach (var item in _preFilterItems) Items.Add(item);
        _preFilterItems = null;
        _filteredGames = null;
        StatusText = "Filter cleared";
    }
}
```

### 10. `src/GamingCommander.App/Services/HelpDialogBuilder.cs`

Add F5 entry:
```csharp
("F5", "Filter games by store, engine, or tags"),
```

### 11. `src/GamingCommander.UI/ViewModels/ShellViewModel.cs`

Add F5 to Commands and InteractionHint:
```csharp
new ShellCommandViewModel { Hotkey = "F5", Label = "Filter" },
```

Hint: `"...F4: configure  |  F5: filter  |  F9: mode"`

## Context

- **Tags are user-curated** — no external dependency. Users add tags via F4 editor.
- **PCGW is a future enhancement** — can bulk-add tags later, but never overwrites user tags.
- **Filter is additive** — store + engine filters work immediately; tags light up as users add them.
- **Existing data is unaffected** — `Tags` defaults to empty list; no migration needed.

## Requirements

- [ ] `GameEntry.Tags` field added (List<string>)
- [ ] `GameEntryDto.Tags` added with backward-compatible default
- [ ] `NormalizeTag()` helper: lowercase + trim + collapse whitespace
- [ ] F4 editor has tags input field (comma-separated)
- [ ] Tags normalized and deduplicated on save
- [ ] `FilterService.ApplyFilter()` implemented
- [ ] `FilterService.GetAllTags()` implemented
- [ ] `FilterWindow` dialog created with store/engine/tag checkboxes
- [ ] F5 keyboard handler opens filter dialog
- [ ] F5 command bar entry added
- [ ] `ShellViewModel.ApplyFilter()` and `ClearFilter()` implemented
- [ ] Filter updates left pane in real-time
- [ ] Clear filter restores previous view
- [ ] Status bar shows filter summary
- [ ] HelpDialogBuilder F5 description added
- [ ] Build clean, existing tests pass

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (no regressions)
- [ ] Manual: F4 → add tags → save → tags persist
- [ ] Manual: F5 → filter by store → list updates
- [ ] Manual: F5 → filter by engine → list updates
- [ ] Manual: F5 → filter by tags → list updates
- [ ] Manual: F5 → clear filter → previous view restored
- [ ] Manual: status bar shows filter summary

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
