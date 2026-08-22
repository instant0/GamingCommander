using GamingCommander.Core.Models;

namespace GamingCommander.Core.Services;

public enum GameFilterKind
{
    Tag,
    Label,
    Wildcard,
}

/// <summary>Active left-pane filter.</summary>
public sealed record GameFilter(GameFilterKind Kind, string Value)
{
    public string Caption => Kind == GameFilterKind.Wildcard ? $"*{Value}*" : Value;
}

/// <summary>One row in the F8 list (tags and store labels actually present).</summary>
public sealed record GameFilterOption(GameFilterKind Kind, string Value, string Group)
{
    public override string ToString() => $"{Group}: {Value}";
}

/// <summary>Match games and collect F8 options. No filesystem.</summary>
public static class GameFilterMatcher
{
    public static bool Matches(GameEntry game, GameFilter filter) =>
        Matches(game, filter, extraTags: null);

    public static bool Matches(GameEntry game, GameFilter filter, IEnumerable<string>? extraTags)
    {
        if (string.IsNullOrWhiteSpace(filter.Value))
            return true;

        string needle = filter.Value.Trim();
        IReadOnlyList<string> extra = extraTags as IReadOnlyList<string> ?? extraTags?.ToList() ?? [];
        return filter.Kind switch
        {
            GameFilterKind.Tag => HasTag(game, needle, extra),
            GameFilterKind.Label => StoreLabel(game).Equals(needle, StringComparison.OrdinalIgnoreCase),
            GameFilterKind.Wildcard => Wildcard(game, needle, extra),
            _ => true,
        };
    }

    public static IReadOnlyList<GameFilterOption> CollectOptions(IEnumerable<GameEntry> games) =>
        CollectOptions(games.Select(g => (g, (IEnumerable<string>)[])));

    public static IReadOnlyList<GameFilterOption> CollectOptions(
        IEnumerable<(GameEntry Game, IEnumerable<string> ExtraTags)> rows)
    {
        var tags = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var stores = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach ((GameEntry game, IEnumerable<string> extra) in rows)
        {
            foreach (string tag in game.Tags.Concat(extra))
            {
                if (!string.IsNullOrWhiteSpace(tag))
                    tags.Add(tag.Trim());
            }

            stores.Add(StoreLabel(game));
        }

        var list = new List<GameFilterOption>(tags.Count + stores.Count);
        foreach (string tag in tags)
            list.Add(new GameFilterOption(GameFilterKind.Tag, tag, "Tag"));
        foreach (string store in stores)
            list.Add(new GameFilterOption(GameFilterKind.Label, store, "Store"));
        return list;
    }

    public static string StoreLabel(GameEntry game) =>
        GameSourceParser.ToDisplayName(game.GameSource);

    private static bool HasTag(GameEntry game, string tag, IReadOnlyList<string> extra) =>
        game.Tags.Concat(extra).Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));

    private static bool Wildcard(GameEntry game, string needle, IReadOnlyList<string> extra)
    {
        if (game.DisplayName.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || game.FolderName.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || StoreLabel(game).Contains(needle, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return game.Tags.Concat(extra).Any(t => t.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }
}
