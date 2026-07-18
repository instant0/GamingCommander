# Plan 97: Multi-Theme System — Runtime Theme Switching

**Date:** 2026-07-17
**Status:** PLANNED
**Priority:** P2 (polish, not blocking features)
**Depends on:** Plan 95 (theme extraction — completed)

---

## Context

Theme extraction (Plan 95) centralized all 23 colors and 8 font sizes into `App.axaml` Application.Resources with semantic names, and `AppTheme.cs` provides code-behind access. All AXAML files and code-behind files use `{DynamicResource ...}` / `AppTheme.*` instead of hardcoded values.

The user requested planning for multi-theme support with three target themes:

1. **Norton Commander Style** (current) — Blue/cyan accent on dark blue-black background
2. **Windows Commander** — White/yellow accent on dark background (closer to original WC64)
3. **GrayScale** — Monochrome, high-contrast accessibility theme

---

## Current State

### What We Have

| Component | Status | File |
|-----------|--------|------|
| 23 semantic color resources | ✅ Done | `App.axaml` L5-44 |
| 8 font size resources | ✅ Done | `App.axaml` L36-43 |
| `AppTheme.cs` static accessor | ✅ Done | `AppTheme.cs` |
| All AXAML uses `{DynamicResource}` | ✅ Done | All 4 windows |
| All code-behind uses `AppTheme.*` | ✅ Done | All 4 .cs files |
| `NortonCommander.axaml` reference file | ✅ Done | `Themes/NortonCommander.axaml` |
| Runtime theme switching | ❌ Missing | — |
| Theme selection UI | ❌ Missing | — |
| Theme persistence in AppConfig | ❌ Missing | — |

### Architecture Decision

**Why runtime switching works:** All resources use `DynamicResource` bindings (not `StaticResource`). In Avalonia 11, updating `Application.Current.Resources["key"]` at runtime causes all bound controls to re-render with the new values. This is the correct mechanism — no XAML reload or window restart needed.

**Why `AppTheme.cs` still works:** `AppTheme` properties call `TryFindResource()` each time they're accessed (they're computed properties, not cached fields). After a resource swap, the next access picks up the new value. The only edge case is `SolidColorBrush` objects that were previously returned and stored — but all current usages are property-access patterns (no caching), so this is safe.

---

## Changes

### Bundle A: Theme Definitions

Create two additional theme files alongside the existing `NortonCommander.axaml`.

#### A1. Create `Themes/WindowsCommander.axaml`

**File:** `src/GamingCommander.App/Themes/WindowsCommander.axaml`

Windows Commander (WC64 / Far Manager inspired) palette:

| Semantic Key | Color | Hex | Rationale |
|-------------|-------|-----|-----------|
| WindowBg | Very dark gray-black | `#0C0C0C` | Classic WC dark background |
| PaneBg | Pure black | `#000000` | Panel contrast |
| ReadOnlyFieldBg | Dark gray | `#1A1A1A` | Input field contrast |
| CommandButtonBg | Dark gray | `#2A2A2A` | Command bar |
| ButtonBgCancel | Dark gray | `#333333` | Neutral |
| ButtonBgSkip | Dark olive | `#2A2A1A` | Warm neutral |
| ButtonBgSuccess | Dark green | `#1A3A1A` | Success |
| ButtonBgAction | Dark blue-gray | `#1A2A3A` | Action |
| ButtonBgDanger | Dark red | `#3A1A1A` | Danger |
| ButtonBgSecondary | Dark gray | `#252525` | Secondary |
| AccentBorder | White | `#FFFFFF` | Classic WC bright accent |
| SeparatorBorder | Dark gray | `#333333` | Subtle |
| EntryBorder | Medium gray | `#444444` | Entry border |
| TextPrimary | White | `#F0F0F0` | Bright text |
| TextSecondary | Light gray | `#C0C0C0` | Secondary text |
| TextAccent | Yellow | `#FFD700` | WC signature yellow |
| TextSuccess | Light green | `#7FC97F` | Success |
| TextMuted | Medium gray | `#808080` | Muted |
| TextDimmed | Dark gray | `#606060` | Dimmed |
| TextDisabled | Darker gray | `#404040` | Disabled |
| TextHighlight | Bright yellow | `#FFFF00` | WC highlight |
| TextDanger | Bright red | `#FF4444` | Danger |
| StatusInstalled | Light green | `#7FC97F` | Installed |
| StatusMoved | Yellow | `#FFD700` | Moved |
| StatusOrphaned | Red | `#FF4444` | Orphaned |

Font sizes: Same as Norton Commander (no changes).

#### A2. Create `Themes/GrayScale.axaml`

**File:** `src/GamingCommander.App/Themes/GrayScale.axaml`

Monochrome high-contrast palette:

| Semantic Key | Color | Hex | Rationale |
|-------------|-------|-----|-----------|
| WindowBg | Near black | `#0A0A0A` | Maximum contrast |
| PaneBg | Pure black | `#000000` | Panel contrast |
| ReadOnlyFieldBg | Dark gray | `#1A1A1A` | Input contrast |
| CommandButtonBg | Dark gray | `#222222` | Command bar |
| ButtonBgCancel | Dark gray | `#2A2A2A` | Neutral |
| ButtonBgSkip | Dark gray | `#333333` | Neutral |
| ButtonBgSuccess | Medium gray | `#444444` | Success (distinguished by icon/text) |
| ButtonBgAction | Medium gray | `#3A3A3A` | Action |
| ButtonBgDanger | Medium gray | `#555555` | Danger (distinguished by icon/text) |
| ButtonBgSecondary | Dark gray | `#2A2A2A` | Secondary |
| AccentBorder | White | `#FFFFFF` | High contrast accent |
| SeparatorBorder | Dark gray | `#333333` | Subtle |
| EntryBorder | Medium gray | `#555555` | Visible border |
| TextPrimary | White | `#FFFFFF` | Maximum contrast text |
| TextSecondary | Light gray | `#CCCCCC` | Secondary |
| TextAccent | Bright white | `#FFFFFF` | Accent = bold/white |
| TextSuccess | White | `#FFFFFF` | Success = white (status text distinguishes) |
| TextMuted | Medium gray | `#888888` | Muted |
| TextDimmed | Dark gray | `#555555` | Dimmed |
| TextDisabled | Dark gray | `#444444` | Disabled |
| TextHighlight | Bright white | `#FFFFFF` | Highlight = bright |
| TextDanger | Light gray | `#CCCCCC` | Danger = lighter text |
| StatusInstalled | White | `#FFFFFF` | Installed |
| StatusMoved | Light gray | `#CCCCCC` | Moved |
| StatusOrphaned | Light gray | `#CCCCCC` | Orphaned |

**Note:** GrayScale intentionally collapses status colors into the same white/gray range. For accessibility, status distinction comes from the status text itself ("Installed" vs "Moved" vs "Orphaned") plus the `PlatformStatusDetail` field. This is a deliberate choice — the GrayScale theme prioritizes readability and high contrast over color differentiation.

Font sizes: Same as Norton Commander (no changes).

---

### Bundle B: Theme Switching Infrastructure

#### B1. Add `ThemeName` to `AppConfig`

**File:** `Core/Models/AppConfig.cs`

Add a new field to the `AppConfig` record:
```csharp
public sealed record AppConfig(
    IReadOnlyList<LibraryRoot> LibraryRoots,
    IReadOnlyList<FolderOverride> FolderOverrides,
    IReadOnlyList<string> HiddenFolders,
    bool IsFirstRun,
    string? LastSeenVersion,
    bool EnableOnlineMetadata,
    string ThemeName = "NortonCommander");  // ← NEW
```

Default is `"NortonCommander"` — existing configs without this field get the default via the positional parameter default.

#### B2. Create `ThemeManager` service

**File:** `src/GamingCommander.App/Services/ThemeManager.cs`

```csharp
public static class ThemeManager
{
    private static readonly Dictionary<string, string> ThemeResources = new()
    {
        ["NortonCommander"] = "avares://GamingCommander.App/Themes/NortonCommander.axaml",
        ["WindowsCommander"] = "avares://GamingCommander.App/Themes/WindowsCommander.axaml",
        ["GrayScale"] = "avares://GamingCommander.App/Themes/GrayScale.axaml",
    };

    private static readonly Dictionary<string, string> ThemeDisplayNames = new()
    {
        ["NortonCommander"] = "Norton Commander Style",
        ["WindowsCommander"] = "Windows Commander",
        ["GrayScale"] = "GrayScale",
    };

    public static IReadOnlyList<string> AvailableThemes => ThemeResources.Keys.ToList();
    public static IReadOnlyList<string> ThemeDisplayNames_ => ThemeDisplayNames.Values.ToList();

    public static void ApplyTheme(string themeName)
    {
        if (!ThemeResources.TryGetValue(themeName, out var avaresPath))
            return;

        // Load the ResourceDictionary from the AXAML file
        var uri = new Uri(avaresPath);
        var loader = new Avalonia.Platform.Storage UriLoader();
        // ... load and merge into Application.Current.Resources

        // Alternative approach: use Avalonia's resource loading
        // This will be refined during implementation based on
        // the actual Avalonia 11 resource loading API
    }

    public static string GetDisplayName(string themeName)
    {
        return ThemeDisplayNames.GetValueOrDefault(themeName, themeName);
    }
}
```

**Implementation note:** The exact API for loading a `ResourceDictionary` from a URI at runtime in Avalonia 11 will need to be researched during implementation. The key approach is:

1. Load the AXAML file as a `ResourceDictionary` using `AvaloniaXamlLoader.Load()` or the URI-based resource loading
2. Clear existing application resources for the theme keys
3. Merge the new dictionary

An alternative simpler approach: instead of loading AXAML at runtime, load theme values from a code-defined dictionary and apply them programmatically:

```csharp
private static readonly Dictionary<string, Dictionary<string, object>> Themes = new()
{
    ["NortonCommander"] = new()
    {
        ["WindowBg"] = new SolidColorBrush(Color.Parse("#10161C")),
        ["PaneBg"] = new SolidColorBrush(Color.Parse("#0F141A")),
        // ... all 23 colors + 8 font sizes
    },
    // ...
};
```

This avoids AXAML loading complexity but duplicates color definitions. The tradeoff should be evaluated during implementation.

#### B3. Apply theme on startup

**File:** `src/GamingCommander.App/App.axaml.cs`

In the `OnFrameworkInitializationCompleted` override, after loading config:
```csharp
var config = configService.Load();
ThemeManager.ApplyTheme(config.ThemeName);
```

This runs before any window is created, so the initial render uses the correct theme.

#### B4. Apply theme on selection change

**File:** Will be wired into the settings UI (see Bundle C).

When the user selects a different theme:
```csharp
ThemeManager.ApplyTheme(selectedThemeName);
// Persist to config
var config = configService.Load();
config = config with { ThemeName = selectedThemeName };
configService.Save(config);
```

Because all bindings use `DynamicResource`, controls update immediately without restart.

---

### Bundle C: Theme Selection UI

#### C1. Add theme selector to `LibrarySetupWindow`

**File:** `LibrarySetupWindow.axaml` + `LibrarySetupWindow.axaml.cs`

Add a "Theme" section to the F2 settings window:

```
Theme: [Norton Commander Style ▼]   [Preview]
```

A `ComboBox` populated with available theme names. On selection change, apply the theme immediately (live preview) and persist to config.

#### C2. Wire `LibrarySetupViewModel`

**File:** `LibrarySetupViewModel.cs`

Add:
```csharp
public IReadOnlyList<string> AvailableThemes => ThemeManager.AvailableThemes;
public string SelectedTheme { get; set; }

public void ApplyTheme(string themeName)
{
    ThemeManager.ApplyTheme(themeName);
    var config = _configService.Load();
    _configService.Save(config with { ThemeName = themeName });
}
```

---

### Bundle D: Persistence & Migration

#### D1. Config backward compatibility

Existing `AppConfig` records without `ThemeName` will deserialize as `null`. The `ApplyTheme()` method should treat `null` / empty as `"NortonCommander"` (the default).

#### D2. No schema migration needed

The `ThemeName` parameter has a default value in the `AppConfig` record constructor. Existing JSON configs without this key will get the default. No migration pass required.

---

## Resource Key Mapping (Reference)

All three themes must define these 31 keys:

| Key | Type | Norton Commander | Windows Commander | GrayScale |
|-----|------|-----------------|-------------------|-----------|
| WindowBg | Brush | `#10161C` | `#0C0C0C` | `#0A0A0A` |
| PaneBg | Brush | `#0F141A` | `#000000` | `#000000` |
| ReadOnlyFieldBg | Brush | `#0A0F14` | `#1A1A1A` | `#1A1A1A` |
| CommandButtonBg | Brush | `#14202A` | `#2A2A2A` | `#222222` |
| ButtonBgCancel | Brush | `#1A1A1A` | `#333333` | `#2A2A2A` |
| ButtonBgSkip | Brush | `#2A2A1A` | `#2A2A1A` | `#333333` |
| ButtonBgSuccess | Brush | `#1A3A2A` | `#1A3A1A` | `#444444` |
| ButtonBgAction | Brush | `#1A3A4A` | `#1A2A3A` | `#3A3A3A` |
| ButtonBgDanger | Brush | `#3A1A1A` | `#3A1A1A` | `#555555` |
| ButtonBgSecondary | Brush | `#1A2A3A` | `#252525` | `#2A2A2A` |
| AccentBorder | Brush | `#3BA7FF` | `#FFFFFF` | `#FFFFFF` |
| SeparatorBorder | Brush | `#243340` | `#333333` | `#333333` |
| EntryBorder | Brush | `#1A2A3A` | `#444444` | `#555555` |
| TextPrimary | Brush | `#D7E2F0` | `#F0F0F0` | `#FFFFFF` |
| TextSecondary | Brush | `#A0B4C4` | `#C0C0C0` | `#CCCCCC` |
| TextAccent | Brush | `#8CD8FF` | `#FFD700` | `#FFFFFF` |
| TextSuccess | Brush | `#7FB7A5` | `#7FC97F` | `#FFFFFF` |
| TextMuted | Brush | `#6A7E8E` | `#808080` | `#888888` |
| TextDimmed | Brush | `#4A5E6E` | `#606060` | `#555555` |
| TextDisabled | Brush | `#3A4A5A` | `#404040` | `#444444` |
| TextHighlight | Brush | `#F8E38C` | `#FFFF00` | `#FFFFFF` |
| TextDanger | Brush | `#FF6B6B` | `#FF4444` | `#CCCCCC` |
| StatusInstalled | Brush | `#7FB7A5` | `#7FC97F` | `#FFFFFF` |
| StatusMoved | Brush | `#E8C547` | `#FFD700` | `#CCCCCC` |
| StatusOrphaned | Brush | `#E87070` | `#FF4444` | `#CCCCCC` |
| FontSizeSmall | Double | `10` | `10` | `10` |
| FontSizeLabel | Double | `11` | `11` | `11` |
| FontSizeBody | Double | `12` | `12` | `12` |
| FontSizeItem | Double | `13` | `13` | `13` |
| FontSizeSubHeader | Double | `14` | `14` | `14` |
| FontSizeHeader | Double | `16` | `16` | `16` |
| FontSizeTitle | Double | `18` | `18` | `18` |
| FontSizeAppTitle | Double | `20` | `20` | `20` |

---

## Tasks

### Bundle A: Theme Definitions
- [ ] A1. Create `Themes/WindowsCommander.axaml` with WC palette
- [ ] A2. Create `Themes/GrayScale.axaml` with monochrome palette

### Bundle B: Theme Switching Infrastructure
- [ ] B1. Add `ThemeName` field to `AppConfig` record (default `"NortonCommander"`)
- [ ] B2. Create `ThemeManager.cs` with theme application logic
- [ ] B3. Wire theme application into `App.axaml.cs` startup
- [ ] B4. Ensure `DynamicResource` bindings update on theme change (verify Avalonia 11 behavior)

### Bundle C: Theme Selection UI
- [ ] C1. Add theme `ComboBox` to `LibrarySetupWindow.axaml`
- [ ] C2. Wire theme selection in `LibrarySetupViewModel.cs`
- [ ] C3. Live preview on selection change (apply immediately)
- [ ] C4. Persist selection to `AppConfig`

### Bundle D: Testing & Documentation
- [ ] D1. Verify all 3 themes render correctly (manual visual check)
- [ ] D2. Verify theme persists across app restarts
- [ ] D3. Verify backward compatibility (existing config without `ThemeName`)
- [ ] D4. Update `META/CODE_MAP.md` and session docs

---

## Execution Order

1. **Bundle A** — Create theme definition files (no runtime changes)
2. **Bundle B** — Theme switching infrastructure (core mechanism)
3. **Bundle C** — UI for theme selection
4. **Bundle D** — Testing and documentation

Build + test after each bundle.

---

## Risk Assessment

| Bundle | Risk | Rationale |
|--------|------|-----------|
| A | Zero | Static AXAML files, no runtime impact |
| B | Medium | Runtime resource swapping is an Avalonia 11 feature that needs validation; may require API research |
| C | Low | Adding a ComboBox to an existing window |
| D | Zero | Testing and documentation |

**Key risk (B2/B4):** The exact mechanism for swapping `Application.Resources` at runtime in Avalonia 11 needs to be verified. Two approaches:
1. Load AXAML `ResourceDictionary` at runtime and merge
2. Update individual resource keys programmatically via `Application.Current.Resources["key"] = newValue`

Approach 2 is simpler but verbose (31 key assignments per theme). Approach 1 is cleaner but needs AXAML loading research. The implementation should prototype approach 2 first and evaluate whether approach 1 is worth the complexity.

---

## Constraints

- **No new NuGet dependencies.** Uses only Avalonia 11 built-in resource system.
- **Font sizes shared across themes.** Only colors change. If font size customization is needed later, it's a straightforward extension.
- **No custom font loading.** All three themes use the same monospace font family (Cascadia Mono / Consolas / Monospace). Custom font loading would be a separate effort.
- **Status colors for GrayScale.** GrayScale intentionally uses white/gray for status indicators. Color-blind accessibility is handled by the status text itself, not by relying on color differentiation.

---

## Out of Scope

- User-created custom themes (loaded from user folder)
- Per-control theme overrides
- Light theme (would require significant AXAML style changes beyond color swapping)
- Custom font selection
- Theme import/export

---

## Exit Criteria

Multi-theme system is complete when:
- Three themes are selectable from the F2 Settings window
- Selecting a theme immediately updates all visible controls (live preview)
- Theme selection persists across app restarts
- Existing configs without `ThemeName` default to Norton Commander
- All 17+ tests pass, build clean
- Each theme renders without visual artifacts or missing resources
