using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace GamingCommander.App;

/// <summary>
/// Static accessor for theme resources defined in Themes/NortonCommander.axaml.
/// Used by code-behind files that construct UI elements programmatically.
/// Colors and brushes are resolved at first access from the Application resource dictionary.
/// To re-theme, swap the resource dictionary in App.axaml — these properties resolve dynamically.
/// Named AppTheme (not Theme) to avoid collision with Avalonia.Controls.Theme.
/// </summary>
public static class AppTheme
{
    // ── Backgrounds ──────────────────────────────────────────────
    public static SolidColorBrush WindowBg => Get("WindowBg");
    public static SolidColorBrush PaneBg => Get("PaneBg");
    public static SolidColorBrush ReadOnlyFieldBg => Get("ReadOnlyFieldBg");
    public static SolidColorBrush CommandButtonBg => Get("CommandButtonBg");
    public static SolidColorBrush ButtonBgCancel => Get("ButtonBgCancel");
    public static SolidColorBrush ButtonBgSkip => Get("ButtonBgSkip");
    public static SolidColorBrush ButtonBgSuccess => Get("ButtonBgSuccess");
    public static SolidColorBrush ButtonBgAction => Get("ButtonBgAction");
    public static SolidColorBrush ButtonBgDanger => Get("ButtonBgDanger");
    public static SolidColorBrush ButtonBgSecondary => Get("ButtonBgSecondary");

    // ── Borders ──────────────────────────────────────────────────
    public static SolidColorBrush AccentBorder => Get("AccentBorder");
    public static SolidColorBrush SeparatorBorder => Get("SeparatorBorder");
    public static SolidColorBrush EntryBorder => Get("EntryBorder");

    // ── Text ─────────────────────────────────────────────────────
    public static SolidColorBrush TextPrimary => Get("TextPrimary");
    public static SolidColorBrush TextSecondary => Get("TextSecondary");
    public static SolidColorBrush TextAccent => Get("TextAccent");
    public static SolidColorBrush TextSuccess => Get("TextSuccess");
    public static SolidColorBrush TextMuted => Get("TextMuted");
    public static SolidColorBrush TextDimmed => Get("TextDimmed");
    public static SolidColorBrush TextDisabled => Get("TextDisabled");
    public static SolidColorBrush TextHighlight => Get("TextHighlight");
    public static SolidColorBrush TextDanger => Get("TextDanger");

    // ── Status ───────────────────────────────────────────────────
    public static SolidColorBrush StatusInstalled => Get("StatusInstalled");
    public static SolidColorBrush StatusMoved => Get("StatusMoved");
    public static SolidColorBrush StatusOrphaned => Get("StatusOrphaned");

    // ── Font Sizes ───────────────────────────────────────────────
    public static double FontSizeSmall => GetDouble("FontSizeSmall");
    public static double FontSizeLabel => GetDouble("FontSizeLabel");
    public static double FontSizeBody => GetDouble("FontSizeBody");
    public static double FontSizeItem => GetDouble("FontSizeItem");
    public static double FontSizeSubHeader => GetDouble("FontSizeSubHeader");
    public static double FontSizeHeader => GetDouble("FontSizeHeader");
    public static double FontSizeTitle => GetDouble("FontSizeTitle");
    public static double FontSizeAppTitle => GetDouble("FontSizeAppTitle");

    /// <summary>
    /// Resolves a SolidColorBrush from the Application resource dictionary by semantic key.
    /// </summary>
    private static SolidColorBrush Get(string key)
    {
        if (Application.Current?.TryFindResource(key, out var value) == true)
        {
            if (value is SolidColorBrush brush) return brush;
            if (value is IImmutableSolidColorBrush immutable)
                return new SolidColorBrush(immutable.Color);
        }
        return new SolidColorBrush(Colors.Gray);
    }

    /// <summary>
    /// Resolves a double value from the Application resource dictionary by semantic key.
    /// </summary>
    private static double GetDouble(string key)
    {
        if (Application.Current?.TryFindResource(key, out var value) == true && value is double d)
            return d;
        return 12;
    }
}
