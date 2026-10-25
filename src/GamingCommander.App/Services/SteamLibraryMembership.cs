namespace GamingCommander.App.Services;

/// <summary>
/// Structural check for Steam library membership: a path under a library's
/// <c>steamapps/common</c> folder is a Steam game, even when the ACF is missing.
/// Windows paths are treated as opaque strings; the host OS path rules are not used.
/// </summary>
internal static class SteamLibraryMembership
{
    /// <summary>
    /// Returns true when <paramref name="path"/> is the <c>steamapps/common</c>
    /// folder itself or any descendant of it. The match is segment-based and
    /// case-insensitive, so <c>steamapps/commonfiles</c> does not match.
    /// </summary>
    internal static bool IsUnderSteamLibrary(string path)
    {
        string normalized = Normalize(path);
        if (normalized.Length == 0)
            return false;

        string[] segments = normalized.Split('/');
        for (int i = 0; i + 1 < segments.Length; i++)
        {
            if (segments[i].Equals("steamapps", StringComparison.OrdinalIgnoreCase)
                && segments[i + 1].Equals("common", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true when <paramref name="path"/> is under <c>steamapps/common</c>
    /// for any of the supplied Steam library roots, in addition to the structural check.
    /// </summary>
    internal static bool IsUnderSteamLibrary(string path, IEnumerable<string> libraryRoots)
    {
        if (IsUnderSteamLibrary(path))
            return true;

        string normalized = Normalize(path);
        if (normalized.Length == 0)
            return false;

        foreach (string root in libraryRoots)
        {
            string normalizedRoot = Normalize(root);
            if (normalizedRoot.Length == 0)
                continue;

            string commonPrefix = normalizedRoot + "/steamapps/common";
            if (normalized.Equals(commonPrefix, StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith(commonPrefix + "/", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>Normalizes separators and trims trailing separators without resolving the path.</summary>
    private static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return path.Replace('\\', '/').TrimEnd('/');
    }
}
