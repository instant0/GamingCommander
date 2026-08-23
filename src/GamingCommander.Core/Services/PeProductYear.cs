using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GamingCommander.Core.Services;

/// <summary>Year hint for PCGW disambiguation from PE version or exe timestamp.</summary>
public static class PeProductYear
{
    /// <summary>
    /// Product/File version year if present, else last-write year (1995–2035).
    /// </summary>
    public static int? Guess(string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
            return null;

        try
        {
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(executablePath);
            foreach (string? raw in new[] { info.ProductVersion, info.FileVersion })
            {
                if (TryYear(raw, out int year))
                    return year;
            }
        }
        catch
        {
        }

        try
        {
            DateTime stamp = File.GetLastWriteTimeUtc(executablePath);
            if (stamp.Year is >= 1995 and <= 2035)
                return stamp.Year;
        }
        catch
        {
        }

        return null;
    }

    internal static bool TryYear(string? text, out int year)
    {
        year = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        foreach (Match m in Regex.Matches(text, @"(19|20)\d{2}"))
        {
            if (int.TryParse(m.Value, out int y) && y is >= 1995 and <= 2035)
            {
                year = y;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// PE ProductName if it looks like a real title (not <c>FSD</c> / module codes).
    /// </summary>
    public static string? TitleHint(string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
            return null;

        try
        {
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(executablePath);
            foreach (string? raw in new[] { info.ProductName, info.FileDescription })
            {
                if (IsUsefulTitle(raw))
                    return raw!.Trim();
            }
        }
        catch
        {
        }

        return null;
    }

    public static bool IsUsefulTitle(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        string t = text.Trim();
        if (t.Length <= 4)
            return false;
        if (t.All(c => char.IsUpper(c) || !char.IsLetter(c)))
            return false;
        return true;
    }
}
