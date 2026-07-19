using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace GamingCommander.App.Services;

/// <summary>
/// Converts a hex color string (e.g. "#7FB7A5") to a SolidColorBrush.
/// Used by MainWindow.axaml to bind PlatformStatusColor to Foreground.
/// Returns a default gray brush if the input is null/empty or cannot be parsed.
/// </summary>
public sealed class HexToBrushConverter : IValueConverter
{
    /// <summary>Converts a hex color string (e.g., '#FF0000') to a SolidColorBrush. Empty string returns the default text brush.</summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrEmpty(hex))
        {
            try
            {
                return new SolidColorBrush(Color.Parse(hex));
            }
            catch
            {
                // Fall through to default
            }
        }

        // Empty/null string → use primary text color (for non-game items in the list)
        return AppTheme.TextPrimary;
    }

    /// <summary>Not supported. Returns null.</summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
