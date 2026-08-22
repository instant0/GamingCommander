using System.Text.RegularExpressions;

namespace GamingCommander.Core.Services;

/// <summary>
/// Rejects PCGW pages that are not the game (soundtrack, demo, artbook).
/// Short words match as whole tokens so "Democracy" / "Bethesda" stay.
/// </summary>
public static class PcgwTitleFilter
{
    private static readonly string[] Phrases =
    [
        "soundtrack", "original soundtrack", "digital book", "art book", "artbook",
        "disambiguation", "making of", "behind the scenes", "strategy guide",
        "instruction manual", "comic book",
    ];

    private static readonly HashSet<string> Tokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "demo", "ost", "beta", "bundle", "benchmark", "prototype", "trailer",
        "teaser", "wallpaper", "avatar", "sdk", "editor", "launcher", "tool",
        "comic", "manual",
    };

    /// <summary>True when the title looks like DLC noise, not the game page.</summary>
    public static bool IsNoisy(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return true;

        string lower = title.Trim().ToLowerInvariant();
        foreach (string phrase in Phrases)
        {
            if (lower.Contains(phrase, StringComparison.Ordinal))
                return true;
        }

        foreach (string token in Regex.Split(lower, @"[^a-z0-9]+"))
        {
            if (token.Length > 0 && Tokens.Contains(token))
                return true;
        }

        return false;
    }

    /// <summary>First title that is not noise, or null.</summary>
    public static string? FirstClean(IEnumerable<string> titles)
    {
        foreach (string title in titles)
        {
            if (!IsNoisy(title))
                return title;
        }

        return null;
    }
}
