using System.Text;

namespace GamingCommander.Core.Services;

/// <summary>Strips store marks (® ™) so names match PCGW and concatenated exe stems.</summary>
public static class TitleText
{
    /// <summary>Search/display form: drop ™/®/© and collapse spaces.</summary>
    public static string ForSearch(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var buffer = new StringBuilder(name.Length);
        foreach (char c in name.Trim())
        {
            if (c is '®' or '™' or '©' or '℠' or '\u00a0')
                continue;
            buffer.Append(c);
        }

        string text = buffer.ToString();
        while (text.Contains("  ", StringComparison.Ordinal))
            text = text.Replace("  ", " ", StringComparison.Ordinal);
        return text.Trim();
    }

    /// <summary>Letters and digits only, lowercased — <c>Dark Souls® III</c> → <c>darksoulsiii</c>.</summary>
    public static string LettersAndDigits(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var buffer = new StringBuilder(name.Length);
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c))
                buffer.Append(char.ToLowerInvariant(c));
        }

        return buffer.ToString();
    }

    /// <summary><c>DeepRock</c> → <c>Deep Rock</c>. All-lowercase packed names are unchanged.</summary>
    public static string ExpandPacked(string? name)
    {
        string text = ForSearch(name);
        if (text.Length == 0)
            return text;

        var buffer = new StringBuilder(text.Length + 4);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (i > 0 && char.IsUpper(c) && char.IsLower(text[i - 1]))
                buffer.Append(' ');
            buffer.Append(c);
        }

        return buffer.ToString();
    }

    /// <summary>Queries to try against PCGW, first wins.</summary>
    public static IReadOnlyList<string> SearchQueries(params string?[] names)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<string>();
        foreach (string? raw in names)
        {
            foreach (string variant in new[] { ForSearch(raw), ExpandPacked(raw) })
            {
                if (variant.Length == 0 || !seen.Add(variant))
                    continue;
                list.Add(variant);
            }
        }

        return list;
    }
}
