namespace GamingCommander.App.Services;

/// <summary>All .item files in one Manifests directory. No folder walk.</summary>
internal sealed class EpicItemCatalog
{
    public EpicItemCatalog(string? manifestsDir = null)
    {
        ManifestsDir = EpicManifestPaths.GetManifestsDir(manifestsDir);
        var all = new List<EpicManifestParser.EpicItemData>();
        if (Directory.Exists(ManifestsDir))
        {
            try
            {
                foreach (string file in Directory.GetFiles(ManifestsDir, "*.item"))
                {
                    EpicManifestParser.EpicItemData? item = EpicManifestParser.ParseItemFile(file);
                    if (item is not null)
                        all.Add(item);
                }
            }
            catch (IOException)
            {
            }
        }

        All = all;
        Playable = all.Where(EpicItemClassifier.IsPlayableBase).ToList();
    }

    public string ManifestsDir { get; }
    public IReadOnlyList<EpicManifestParser.EpicItemData> All { get; }
    public IReadOnlyList<EpicManifestParser.EpicItemData> Playable { get; }

    public bool MatchesInstall(string? installPath)
    {
        if (string.IsNullOrWhiteSpace(installPath))
            return false;
        string target = Normalize(installPath);
        return All.Any(i => Normalize(i.InstallLocation) == target);
    }

    public static bool LooksLikeManifestsDir(string path)
    {
        if (!Directory.Exists(path))
            return false;
        try
        {
            return Directory.EnumerateFiles(path, "*.item").Any();
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static string Normalize(string path) =>
        path.ToLowerInvariant().TrimEnd('\\', '/');
}
