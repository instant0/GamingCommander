namespace GamingCommander.Core.Services;

/// <summary>
/// The single path-comparison rule. Paths are treated as opaque strings: no filesystem
/// I/O, no <c>Path.GetFullPath</c>, no culture-sensitive comparison, and no case rewriting
/// of stored values.
/// </summary>
/// <remarks>
/// NTFS is case-insensitive, so the filesystem does not care about case. String comparison
/// does: the same folder can arrive from the registry with backslashes, from an ACF or Epic
/// JSON with forward slashes, and with different casing. Normalise separators once and compare
/// with <see cref="StringComparison.OrdinalIgnoreCase"/>; keep the original string for display,
/// storage and launch.
/// </remarks>
public static class PathText
{
    /// <summary>
    /// Trims surrounding whitespace and trailing separators without changing the
    /// separators themselves. Safe to use on a real path handed to the filesystem;
    /// a root (<c>C:\</c>, <c>/</c>) is left intact.
    /// </summary>
    public static string TrimTrailingSeparators(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        string trimmed = path.Trim();
        if (trimmed.Length == 1 && (trimmed[0] == '\\' || trimmed[0] == '/'))
            return trimmed;
        if (trimmed.Length == 3 && trimmed[1] == ':' && (trimmed[2] == '\\' || trimmed[2] == '/'))
            return trimmed;

        return trimmed.TrimEnd('\\', '/');
    }

    /// <summary>
    /// Backslashes, no trailing separator, trimmed. A drive root keeps its separator
    /// (<c>C:</c> becomes <c>C:\</c>). Case is preserved. Comparison only — never
    /// pass the result to the filesystem, a shell, or storage.
    /// </summary>
    public static string NormalizeForCompare(string? path)
    {
        string normalized = TrimTrailingSeparators(path);
        if (normalized.Length == 0)
            return string.Empty;

        normalized = normalized.Replace('/', '\\');
        if (normalized.Length == 2 && normalized[1] == ':')
            normalized += '\\';
        return normalized;
    }

    /// <summary>True when both strings name the same folder.</summary>
    public static bool Equals(string? a, string? b) =>
        string.Equals(
            NormalizeForCompare(a),
            NormalizeForCompare(b),
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True when <paramref name="child"/> is strictly inside <paramref name="parent"/>.
    /// Segment-aware: <c>d:\games2</c> is not under <c>d:\games</c>, and an exact match is not a child.
    /// </summary>
    public static bool IsUnder(string? child, string? parent)
    {
        string c = NormalizeForCompare(child);
        string p = NormalizeForCompare(parent);
        if (c.Length <= p.Length || p.Length == 0)
            return false;

        if (!c.StartsWith(p, StringComparison.OrdinalIgnoreCase))
            return false;

        return p.EndsWith('\\') || c[p.Length] == '\\';
    }

    /// <summary>Last folder name, or the empty string for an empty path.</summary>
    public static string Leaf(string? path)
    {
        string normalized = NormalizeForCompare(path);
        if (normalized.Length == 0)
            return string.Empty;

        int slash = normalized.LastIndexOf('\\');
        return slash >= 0 ? normalized[(slash + 1)..] : normalized;
    }
}
