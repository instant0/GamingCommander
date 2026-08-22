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
    public static string? FirstClean(IEnumerable<string> titles) =>
        PickBest(titles, yearHint: null);

    /// <summary>
    /// Prefer a title whose <c>(YYYY)</c> is closest to <paramref name="yearHint"/>
    /// (exe ProductVersion / last-write). Without a hint, first clean title wins.
    /// </summary>
    public static string? PickBest(IEnumerable<string> titles, int? yearHint)
    {
        var clean = titles.Where(t => !IsNoisy(t)).ToList();
        if (clean.Count == 0)
            return null;
        if (yearHint is null)
            return clean[0];

        string? best = null;
        int bestDelta = int.MaxValue;
        foreach (string title in clean)
        {
            int? year = YearFromTitle(title);
            int delta;
            if (year is int y)
                delta = Math.Abs(y - yearHint.Value);
            else if (title.Contains("remake", StringComparison.OrdinalIgnoreCase) && yearHint >= 2018)
                delta = 3;
            else
                delta = yearHint >= 2018 ? 20 : Math.Abs(2008 - yearHint.Value);

            if (delta < bestDelta)
            {
                best = title;
                bestDelta = delta;
            }
        }

        return best ?? clean[0];
    }

    /// <summary>Four-digit year in parentheses, e.g. Dead Space (2023).</summary>
    public static int? YearFromTitle(string title)
    {
        Match m = Regex.Match(title, @"\((\d{4})\)");
        if (!m.Success)
            return null;
        return int.TryParse(m.Groups[1].Value, out int year) ? year : null;
    }
}
