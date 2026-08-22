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
}
