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

        return AppTheme.TextSecondary;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
