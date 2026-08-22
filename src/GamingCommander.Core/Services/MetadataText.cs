using System.Text;
using System.Text.RegularExpressions;

namespace GamingCommander.Core.Services;

/// <summary>Strips wiki/network strings down to values safe to store and show.</summary>
public static class MetadataText
{
    private static readonly Regex ArgPattern = new(
        @"^--?[A-Za-z][\w-]*(?: [A-Za-z0-9]+)?$", RegexOptions.CultureInvariant);

    /// <summary>PCGW path template only. Null if it is not a folder template.</summary>
    public static string? SafePathTemplate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        string text = StripControls(raw).Trim();
        if (text.Length is 0 or > 240)
            return null;
        if (text.Contains("://", StringComparison.Ordinal)
            || text.Contains("http", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        foreach (char c in text)
        {
            if (char.IsLetterOrDigit(c))
                continue;
            if (c is '\\' or '/' or ':' or '.' or '_' or '-' or ' ' or '%' or '{' or '}' or '|' or '\'')
                continue;
            return null;
        }

        return text;
    }

    /// <summary>Launch flag from the catalog. Null if it is not a flag.</summary>
    public static string? SafeArgument(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        string text = StripControls(raw).Trim();
        if (text.Length is 0 or > 64)
            return null;
        return ArgPattern.IsMatch(text) ? text : null;
    }

    /// <summary>One-line note. Empty if nothing usable remains.</summary>
    public static string SafeNote(string? raw, int max = 160)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var buffer = new StringBuilder(Math.Min(raw.Length, max));
        foreach (char c in raw)
        {
            if (c is '\r' or '\n' or '\t')
            {
                if (buffer.Length > 0 && buffer[^1] != ' ')
                    buffer.Append(' ');
                continue;
            }

            if (char.IsControl(c))
                continue;

            buffer.Append(c);
            if (buffer.Length >= max)
                break;
        }

        return buffer.ToString().Trim();
    }

    /// <summary>Steam AppID digits only.</summary>
    public static string? SafeSteamAppId(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        string text = raw.Trim();
        if (text.Length is 0 or > 12 || !text.All(char.IsDigit))
            return null;
        return text;
    }

    /// <summary>Store slug (GOG/Epic). Letters, digits, dash, underscore.</summary>
    public static string? SafeStoreSlug(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        string text = raw.Trim();
        if (text.Length is 0 or > 80)
            return null;
        return text.All(c => char.IsLetterOrDigit(c) || c is '-' or '_') ? text : null;
    }

    public static string StripControls(string text)
    {
        var buffer = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (!char.IsControl(c))
                buffer.Append(c);
        }

        return buffer.ToString();
    }
}
