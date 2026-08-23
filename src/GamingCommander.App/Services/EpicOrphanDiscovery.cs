using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>
/// Immediate children of other library roots that have .egstore but no ProgramData .item.
/// One-level only — same depth as FolderScanner.
/// </summary>
internal static class EpicOrphanDiscovery
{
    public sealed record OrphanFolder(string GameFolder, string ParentRoot);

    public static IReadOnlyList<OrphanFolder> Find(EpicItemCatalog catalog, IEnumerable<string> otherRoots)
    {
        var list = new List<OrphanFolder>();
        foreach (string root in otherRoots)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                continue;
            if (root.Equals(catalog.ManifestsDir, StringComparison.OrdinalIgnoreCase))
                continue;
            if (LibraryManager.LooksLikeSteamLibrary(root))
                continue;

            IReadOnlyList<DirectoryInfo> children;
            try
            {
                children = FileSystemHelper.GetDirectoriesSafe(root);
            }
            catch
            {
                continue;
            }

            foreach (DirectoryInfo child in children)
            {
                if (!HasEgstore(child.FullName))
                    continue;
                if (catalog.MatchesInstall(child.FullName))
                    continue;
                list.Add(new OrphanFolder(child.FullName, root));
            }
        }

        return list;
    }

    private static bool HasEgstore(string folder) =>
        Directory.Exists(Path.Combine(folder, ".egstore"))
        || Directory.Exists(Path.Combine(folder, ".egsstore"));
}
