namespace GamingCommander.Core.Services;

/// <summary>
/// Normalizes and compares tags. Handles trimming, whitespace collapse,
/// and case-insensitive deduplication.
/// </summary>
public static class TagNormalizer
{
    /// <summary>
    /// Normalizes a tag: trims whitespace, collapses internal whitespace.
    /// Preserves original casing.
    /// </summary>
    public static string Normalize(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return string.Empty;

        // Trim leading/trailing whitespace, collapse internal whitespace
        char[] separators = [' ', '\t', '\n', '\r'];
        return string.Join(' ', tag.Split(separators, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Returns true if two tags are equivalent (case-insensitive, normalized).
    /// </summary>
    public static bool AreEquivalent(string tag1, string tag2)
    {
        return string.Equals(Normalize(tag1), Normalize(tag2), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Adds a tag to the list if it doesn't already exist (case-insensitive).
    /// Returns the updated list.
    /// </summary>
    public static List<string> AddDistinct(List<string> tags, string newTag)
    {
        string normalized = Normalize(newTag);
        if (string.IsNullOrEmpty(normalized))
            return tags;

        if (tags.Any(t => AreEquivalent(t, normalized)))
            return tags;

        tags.Add(normalized);
        return tags;
    }

    /// <summary>
    /// Parses a comma-separated string into a list of normalized, deduplicated tags.
    /// </summary>
    public static List<string> ParseFromCommaSeparated(string input)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(input))
            return result;

        foreach (string part in input.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            AddDistinct(result, part);
        }

        return result;
    }

    /// <summary>
    /// Converts a list of tags to a comma-separated string.
    /// </summary>
    public static string ToCommaSeparated(List<string> tags)
    {
        return string.Join(", ", tags);
    }

    /// <summary>Split a comma list (PCGW genre or engine). Does not write <c>games.json</c>.</summary>
    public static List<string> SplitList(string? raw)
    {
        var list = new List<string>();
        if (string.IsNullOrWhiteSpace(raw))
            return list;
        foreach (string part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!IsLatinDisplayTag(part))
                continue;
            AddDistinct(list, part);
        }
        return list;
    }

    /// <summary>Genre tokens then engine tokens. Engine names stay tags of type Engine in the UI.</summary>
    public static List<string> FromMetadata(string? genre, string? engine) =>
        Merge(SplitList(genre), SplitList(engine));

    /// <summary>User tags first, then metadata tokens that are not already present.</summary>
    public static List<string> Merge(IEnumerable<string> userTags, IEnumerable<string> metadataTags)
    {
        var list = new List<string>();
        foreach (string tag in userTags)
            AddDistinct(list, tag);
        foreach (string tag in metadataTags)
            AddDistinct(list, tag);
        return list;
    }

    /// <summary>Genre/engine tags from PCGW/Steam: skip Arabic/CJK store-locale strings.</summary>
    public static bool IsLatinDisplayTag(string? tag)
    {
        string n = Normalize(tag ?? "");
        if (n.Length == 0)
            return false;
        int letters = 0, latin = 0;
        foreach (char c in n)
        {
            if (!char.IsLetter(c))
                continue;
            letters++;
            if (c <= 0x024F)
                latin++;
        }

        return letters == 0 || latin * 2 >= letters;
    }
}
