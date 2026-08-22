namespace GamingCommander.Core.Services;

/// <summary>
/// Builds a safe Explorer launch. FileName is always explorer.exe.
/// Folder must be a local drive path (C:\…). UNC and URLs are rejected.
/// </summary>
public static class WindowsExplorer
{
    private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".bat", ".cmd", ".com", ".scr", ".ps1", ".msi", ".vbs", ".js", ".lnk", ".url",
    };

    /// <summary>
    /// True when <paramref name="displayPath"/> is a local Windows folder.
    /// <paramref name="fileName"/> is always <c>explorer.exe</c>.
    /// </summary>
    public static bool TryBuildOpenFolder(
        string? displayPath,
        string? gameDirectory,
        out string fileName,
        out string arguments)
    {
        fileName = "explorer.exe";
        arguments = string.Empty;

        string? expanded = PcgwPathTokens.ForExplorer(displayPath);
        if (expanded is null || !TryNormalizeFolder(expanded, out string folder))
            return false;

        if (!IsClickableFolder(folder, gameDirectory))
            return false;

        arguments = "\"" + folder + "\"";
        return true;
    }

    /// <summary>
    /// Clickable only under the game install dir, %USERPROFILE%, %APPDATA%, or %LOCALAPPDATA%.
    /// Other sanitized strings may be shown, not opened.
    /// </summary>
    public static bool IsClickableFolder(string? displayOrExpanded, string? gameDirectory)
    {
        string? expanded = displayOrExpanded is not null
            && (displayOrExpanded.Contains('%') || displayOrExpanded.Contains('<'))
            ? PcgwPathTokens.ForExplorer(displayOrExpanded)
            : displayOrExpanded;
        if (expanded is null || !TryNormalizeFolder(expanded, out string folder))
            return false;

        if (IsUnder(folder, gameDirectory))
            return true;

        foreach (string name in new[] { "USERPROFILE", "APPDATA", "LOCALAPPDATA" })
        {
            string? root = Environment.GetEnvironmentVariable(name);
            if (IsUnder(folder, root))
                return true;
        }

        return IsKnownStoreDataFolder(folder);
    }

    /// <summary>Ubisoft Connect savegames / Steam userdata — not arbitrary Program Files.</summary>
    private static bool IsKnownStoreDataFolder(string folder)
    {
        string a = folder.Replace('/', '\\').TrimEnd('\\') + "\\";
        return a.Contains(@"\Ubisoft\Ubisoft Game Launcher\", StringComparison.OrdinalIgnoreCase)
            || a.Contains(@"\Steam\userdata\", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Parent of a Windows exe path. Does not use <see cref="Path"/> (Linux-safe).</summary>
    public static string? ParentDirectory(string? windowsPath)
    {
        if (string.IsNullOrWhiteSpace(windowsPath))
            return null;

        string text = windowsPath.Trim().Replace('/', '\\').TrimEnd('\\');
        int slash = text.LastIndexOf('\\');
        return slash >= 2 ? text[..slash] : null;
    }

    private static bool IsUnder(string folder, string? root)
    {
        if (string.IsNullOrWhiteSpace(root))
            return false;

        string a = folder.TrimEnd('\\') + "\\";
        string b = root.Replace('/', '\\').TrimEnd('\\') + "\\";
        return a.StartsWith(b, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Local <c>X:\…</c> folder only. No UNC, no <c>..</c>, no extra colons.</summary>
    public static bool TryNormalizeFolder(string path, out string folder)
    {
        folder = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        string text = path.Trim().Replace('/', '\\');
        if (text.Any(c => char.IsControl(c) || char.IsSurrogate(c)))
            return false;
        if (text.IndexOfAny(['"', '<', '>', '|', '*', '?', ';', '&', '^']) >= 0)
            return false;
        if (text.Contains("..", StringComparison.Ordinal))
            return false;
        if (text.Contains("://", StringComparison.Ordinal))
            return false;
        if (text.StartsWith(@"\\", StringComparison.Ordinal))
            return false;

        bool driveRoot = text.Length == 3 && IsDrivePrefix(text);
        if (!driveRoot)
            text = text.TrimEnd('\\');

        if (text.Length == 2 && char.IsLetter(text[0]) && text[1] == ':')
            text += "\\";

        if (text.Length < 3 || !IsDrivePrefix(text))
            return false;

        if (text.IndexOf(':', 2) >= 0)
            return false;

        int dot = text.LastIndexOf('.');
        int slash = text.LastIndexOf('\\');
        if (dot > slash && slash >= 0 && BlockedExtensions.Contains(text[dot..]))
            return false;

        folder = text;
        return true;
    }

    private static bool IsDrivePrefix(string text) =>
        text.Length >= 3
        && char.IsLetter(text[0])
        && text[1] == ':'
        && text[2] == '\\';
}
