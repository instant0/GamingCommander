namespace GamingCommander.App.Services;

/// <summary>
/// Cheap "first usable exe" finder shared by <see cref="SteamLibraryScanner"/> and
/// <see cref="EpicItemWriter"/> (Plan 125 Phase 2c).
///
/// Rules (behavior-preserving union of the two former finders):
/// <list type="bullet">
/// <item>Folders are probed in the caller's order; an empty-string entry means the
/// game root itself (Steam probes root first, Epic probes it last).</item>
/// <item>Within a folder, enumeration order wins (first matching exe).</item>
/// <item><see cref="ExecutableDiscovery.IsForbiddenLaunchExe"/> always applies;
/// callers may add extra name skips via <paramref name="extraSkip"/>.</item>
/// <item>Never walks the whole tree — deliberate: Steam lookup is a documented
/// performance divergence from <see cref="ExecutableDiscovery"/>'s full-tree
/// scored search.</item>
/// </list>
/// </summary>
public static class CheapExeFinder
{
    /// <summary>
    /// Returns the absolute path of the first usable exe, or <see cref="string.Empty"/>.
    /// </summary>
    public static string FindFirst(
        string root,
        IReadOnlyList<string> relativeFolders,
        Func<string, bool>? extraSkip = null)
    {
        foreach (string rel in relativeFolders)
        {
            string dir = string.IsNullOrEmpty(rel)
                ? root
                : Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(dir))
                continue;

            string[] exes;
            try
            {
                exes = Directory.GetFiles(dir, "*.exe");
            }
            catch
            {
                continue;
            }

            foreach (string exe in exes)
            {
                if (ExecutableDiscovery.IsForbiddenLaunchExe(exe))
                    continue;
                if (extraSkip is not null && extraSkip(exe))
                    continue;
                return exe;
            }
        }

        return string.Empty;
    }
}