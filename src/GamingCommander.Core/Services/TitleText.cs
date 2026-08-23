using System.Text;
using System.Text.RegularExpressions;
using GamingCommander.Core.Models;

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
            foreach (string variant in new[]
                     {
                         ForSearch(raw), ExpandPacked(raw), FromFolderName(raw), StripEdition(raw),
                     })
            {
                if (variant.Length == 0 || IsGenericLabel(variant) || !seen.Add(variant))
                    continue;
                list.Add(variant);
            }
        }

        return list;
    }

    /// <summary>
    /// PCGW search string: Steam ACF / Epic .item display name first, never a launcher PE title.
    /// </summary>
    public static string LookupName(string displayName, string folderName, GameSourceKind source)
    {
        if (IsStoreNamed(source)
            && !string.IsNullOrWhiteSpace(displayName)
            && !IsGenericLabel(displayName))
        {
            return displayName.Trim();
        }

        string fromFolder = FromFolderName(folderName);
        if (!string.IsNullOrWhiteSpace(fromFolder) && !IsGenericLabel(fromFolder))
            return fromFolder;
        if (!string.IsNullOrWhiteSpace(displayName) && !IsGenericLabel(displayName))
            return displayName.Trim();
        return displayName?.Trim() ?? folderName ?? string.Empty;
    }

    private static bool IsStoreNamed(GameSourceKind source) =>
        source is GameSourceKind.Steam
            or GameSourceKind.Epic
            or GameSourceKind.Gog
            or GameSourceKind.EaApp
            or GameSourceKind.UbisoftConnect
            or GameSourceKind.BattleNet
            or GameSourceKind.Xbox
            or GameSourceKind.Rockstar;

    private static readonly HashSet<string> GenericWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "system", "binaries", "win32", "win64", "wingdk", "shipping", "engine",
        "common", "redist", "bin", "game", "data", "content",
        "launcher", "patcher", "2klauncher", "launcherpatcher",
    };

    /// <summary>PE / parent-folder words that must not become the game title (ELEX <c>system\</c>).</summary>
    public static bool IsGenericLabel(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;
        string key = LettersAndDigits(text);
        return key.Length == 0 || GenericWords.Contains(key);
    }

    /// <summary>
    /// Drop store SKU tails PCGW does not use
    /// (<c>Dying Light 2 Stay Human - Reloaded Edition</c> → <c>Dying Light 2 Stay Human</c>).
    /// </summary>
    public static string StripEdition(string? name)
    {
        string text = ForSearch(name);
        if (text.Length == 0)
            return text;
        return EditionTail.Replace(text, "").Trim();
    }

    private static readonly Regex EditionTail = new(
        @"\s*[-:]\s*(Reloaded|Definitive|Complete|Enhanced|Ultimate|Deluxe|Gold|Anniversary|Game of the Year|GOTY)(\s+Edition)?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Folder <c>elexII</c> → <c>elex II</c> for display and PCGW.</summary>
    public static string FromFolderName(string? folderName)
    {
        string packed = ExpandPacked(ForSearch(folderName));
        return packed.Length > 0 ? packed : (folderName ?? string.Empty).Trim();
    }

    /// <summary>
    /// Folder vs exe stem: <c>elex</c>/<c>elex.exe</c>, <c>elexII</c>/<c>ELEX2.exe</c>
    /// (trailing II/III/IV ≡ 2/3/4).
    /// </summary>
    public static bool MatchesFolderAndExe(string? folderName, string? exeStem)
    {
        string a = CanonicalKey(folderName);
        string b = CanonicalKey(exeStem);
        return a.Length >= 3 && a == b;
    }

    public static string CanonicalKey(string? name)
    {
        string text = ForSearch(name);
        text = TrailingRoman.Replace(text, m => m.Groups[1].Value.ToUpperInvariant() switch
        {
            "IV" => "4",
            "III" => "3",
            "II" => "2",
            _ => m.Value,
        });
        return LettersAndDigits(text);
    }

    private static readonly Regex TrailingRoman = new(
        @"\s*(IV|III|II)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>True when PE title and folder share a letter-run (reject <c>System</c> for <c>elexII</c>).</summary>
    public static bool SharesNameToken(string? title, string? folderName)
    {
        string a = LettersAndDigits(title);
        string b = LettersAndDigits(folderName);
        if (a.Length < 3 || b.Length < 3)
            return false;
        return a.Contains(b) || b.Contains(a);
    }
}
