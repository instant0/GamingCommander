using GamingCommander.Core.Models;

namespace GamingCommander.Core.Services;

/// <summary>Right-pane strings from sidecar <see cref="GameMetadataDetails"/>.</summary>
public static class MetadataDetailsFormatter
{
    /// <summary>First Windows config path, tokens expanded.</summary>
    public static string WindowsConfig(GameMetadataDetails? details, string? gameDirectory = null) =>
        FirstWindows(details?.ConfigPaths, gameDirectory);

    /// <summary>First Windows save path, tokens expanded.</summary>
    public static string WindowsSaves(GameMetadataDetails? details, string? gameDirectory = null) =>
        FirstWindows(details?.SavePaths, gameDirectory);

    /// <summary>Up to <paramref name="max"/> catalog rows as <c>arg — notes</c>.</summary>
    public static string CommandLineSummary(GameMetadataDetails? details, int max = 8)
    {
        if (details is null || details.CommandLine.Count == 0)
            return string.Empty;

        return string.Join(
            Environment.NewLine,
            details.CommandLine.Take(max).Select(r => r.Argument));
    }

    /// <summary>One video cap per line. Skips false/unknown and URL/note blobs.</summary>
    public static string VideoSummary(GameMetadataDetails? details)
    {
        if (details is null || details.Video.Count == 0)
            return string.Empty;

        string[] order = ["widescreen", "ultrawide", "fov", "4k", "60fps", "120fps", "hdr", "raytracing", "borderless", "vsync"];
        var parts = new List<string>();
        foreach (string key in order)
        {
            if (!details.Video.TryGetValue(key, out string? value) || !IsShortCap(value))
                continue;
            if (value.Equals("false", StringComparison.OrdinalIgnoreCase)
                || value.Equals("unknown", StringComparison.OrdinalIgnoreCase)
                || value.Equals("n/a", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string shown = value.Equals("true", StringComparison.OrdinalIgnoreCase) ? "yes" : value.Trim();
            parts.Add($"{key}: {shown}");
        }

        return string.Join(Environment.NewLine, parts);
    }

    public static bool IsShortCap(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (value.Contains("http", StringComparison.OrdinalIgnoreCase)
            || value.Contains('[')
            || value.Contains("{{", StringComparison.Ordinal))
        {
            return false;
        }

        return value.Trim().Length <= 24;
    }

    private static string FirstWindows(IReadOnlyList<GameMetadataPath>? paths, string? gameDirectory)
    {
        if (paths is null)
            return string.Empty;

        GameMetadataPath? path = paths.FirstOrDefault(p =>
            p.Os.Equals("Windows", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(p.Template));
        return path is null ? string.Empty : PcgwPathTokens.ResolveWindows(path.Template, gameDirectory);
    }

    private static string Trim(string text, int max)
    {
        if (text.Length <= max)
            return text;
        return text[..(max - 1)].TrimEnd() + "…";
    }
}
