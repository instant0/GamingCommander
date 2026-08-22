# Plan 117 — Left Pane Layout Redesign: Badge Left, Tags Right

**Created:** 2026-07-27
**Priority:** P2
**Status:** ✅ COMPLETE (2026-08-22)
**Source:** User feedback on tag/badge layout from live testing

---

## 1. Problem Statement

The current left-pane layout has several issues:
- **Library Roots** lost their path and source badge after the tag refactor (regression)
- Store badge on the right side pushes the path off-screen or misaligns
- User tags as separate badge columns are visually cluttered
- No consistent column alignment across root-level and game-level views

## 2. Target Layout

### Library Roots level
```
[Steam]  STEAMLIBRARY (95 games)              d:\steamlibrary
[GOG]    GOG Library (12 games)               d:\games\gog
         My Games (5 games)                   d:\mygames
```

### Games level (inside a root)
```
[Steam]  Cyberpunk 2077 (RPG, UE5)            d:\...\cyberpunk2042.exe
[Steam]  Elden Ring (Souls-like)              d:\...\eldenring.exe
[Epic]   Fortnite (Battle Royale)             d:\...\fortnite\FortniteClient.exe
         Hollow Knight (Metroidvania)          d:\...\hollowknight.exe
[Steam]  ⏳ Scanning...                       d:\steamlibrary
```

### Column Structure

| Col | Content | Width | Alignment |
|-----|---------|-------|-----------|
| 0 | Store badge pill | `Auto` (sized to longest badge: "Battle.net" = 10 chars) | Center — all badges stack vertically aligned |
| 1 | Title + parenthetical (tags or count) | `1*` (flex) | Left, ellipsis from right |
| 2 | Path (full root path at roots, exe filename at games) | `Auto` (sized by font + max path length, right-aligned) | Right |
| 3 | Scanning badge | Auto | Right |

### Rules
- **Badge on LEFT** — fixed width, all badges align vertically. Standalone/Unknown = empty space (no badge).
- **Title** — bold, includes parenthetical: `(95 games)` at root level, `(TAG1, TAG2)` at game level. Empty parens = not shown.
- **Path** — right-aligned. Full root path at library roots level. Exe filename at games level (not full path).
- **No tags-as-separate-badges** in left pane. Tags shown as plain text inside parens: `(RPG, Open World)`. Tag colors appear only in the right-pane details.
- Standard `TextTrimming="CharacterEllipsis"` on path (right-trimming, not left). Left-trimming deferred to a future plan.
- Window too narrow: Path column is fixed — title column absorbs squeeze. If total width < 600px, path column hides entirely.

## 3. Files to Modify

| File | Change |
|------|--------|
| `src/GamingCommander.App/MainWindow.axaml` | Rewrite left-pane ItemTemplate: 4-column grid (Badge \| Title \| Path \| Scanning) |
| `src/GamingCommander.UI/ViewModels/ShellPaneItemViewModel.cs` | Remove `TagBadges`, `Tags` from left-pane use (keep for right pane). Remove `PathDisplay`. Simplify `StoreBadge` to single `TagBadgeViewModel?`. Add `Subtitle` property (parens text: game count or tags). |
| `src/GamingCommander.UI/ViewModels/ShellViewModel.cs` | Update `LoadGamesForRoot()` — populate `Subtitle` from tags, `PathDisplay` → use `PathSummary` for roots, exe-only for games. Update `JumpToLibraryRoots()` — populate `StoreBadge` + `Subtitle` from game count. Update `UpdateScanningBadges()` to preserve new fields. |

## 4. Detailed Changes

### 4.1 ShellPaneItemViewModel

Remove from left-pane use (keep `TagBadges` and `Tags` for right-pane details only):
- `PathDisplay` → replaced by smarter `PathSummary` usage
- Renamed/repurposed properties:

```csharp
// NEW: parenthetical text — "(95 games)" or "(RPG, Open World)" or empty
public string Subtitle { get; init; } = string.Empty;

// CHANGED: single nullable badge (not a list)
public TagBadgeViewModel? StoreBadge { get; init; }

// REMOVED from left-pane use:
// - Tags, TagBadges → still used by right pane (DetailsTags, DetailsTagBadges)
// - PathDisplay → replaced by PathSummary at roots, new LeftPath at games
// - SourceLabel → still used by right pane (DetailsType)

// NEW: short path for left pane
// Roots: full root path (d:\steamlibrary)
// Games: exe filename only (eldenring.exe)
public string LeftPath { get; init; } = string.Empty;
```

### 4.2 MainWindow.axaml Left Pane

```xml
<Grid ColumnDefinitions="Auto,1*,Auto,Auto">
  <!-- Col 0: Store badge (Auto-width, sized to longest badge "Battle.net" = 10 chars at FontSize 10) -->
  <ItemsControl Grid.Column="0" ... HorizontalAlignment="Center" />
  <!-- Col 1: Title + Subtitle (flex, left, ellipsis) -->
  <StackPanel Grid.Column="1" Orientation="Horizontal">
    <TextBlock Text="{Binding Title}" FontWeight="Bold" ... />
    <TextBlock Text="{Binding Subtitle}" Foreground="TextMuted" ... />
  </StackPanel>
  <!-- Col 2: Path (Auto-width, right-aligned, MaxWidth 280) -->
  <TextBlock Grid.Column="2" Text="{Binding LeftPath}" TextAlignment="Right" MaxWidth="280" ... />
  <!-- Col 3: Scanning badge -->
  <TextBlock Grid.Column="3" Text="{Binding ScanningBadge}" ... />
</Grid>
```

**Path handling:** Column is `Auto` so it sizes to content (grows right-to-left as path lengthens). `MaxWidth="280"` on the TextBlock prevents it from swallowing the title on very long paths. `TextTrimming="CharacterEllipsis"` handles overflow. If the window shrinks below ~600px total, the path TextBlock can be hidden via a binding on `MinWidth` or a converter.

### 4.3 ShellViewModel — PathDisplay Logic

```csharp
// Library roots:
LeftPath = root.RootPath,  // full path: d:\steamlibrary

// Games:
LeftPath = Path.GetFileName(game.ExecutablePath),  // exe only: eldenring.exe
```

### 4.4 ShellViewModel — Subtitle Logic

```csharp
// Library roots:
Subtitle = $"({games.Count} game{(games.Count != 1 ? "s" : "")})",

// Games:
Subtitle = game.Tags.Count > 0 ? $"({string.Join(", ", game.Tags)})" : string.Empty,
```

### 4.5 ShellViewModel — StoreBadge for Roots

```csharp
// JumpToLibraryRoots:
StoreBadge = BuildStoreBadge(root.DefaultType),
```

## 5. What NOT to change

- Right pane details panel — still uses `TagBadges`, `Tags`, `SourceLabel` for colored tag badges and type display
- `BuildTagBadges()` method — still needed for right pane
- `BuildStoreBadge()` method — change return type to `TagBadgeViewModel?` instead of `List<TagBadgeViewModel>`
- `tag_colors.json` — no changes needed
- `TagColorService` — no changes needed

## 6. Verification

- Build: `dotnet build` — zero errors
- Tests: `dotnet test` — all passing
- Manual: Library Roots level shows badge + name + game count + path
- Manual: Games level shows badge + name + tags + exe filename
- Manual: Standalone games show no badge, just empty left space
- Manual: Scanning badge still appears during F5 rescan
- Manual: Window resize — path column stays fixed, title absorbs
