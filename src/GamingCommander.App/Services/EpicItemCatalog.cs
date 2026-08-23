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
        Playable = DedupPlayable(all.Where(EpicItemClassifier.IsPlayableBase));
    }

    public string ManifestsDir { get; }
    public IReadOnlyList<EpicManifestParser.EpicItemData> All { get; }
    public IReadOnlyList<EpicManifestParser.EpicItemData> Playable { get; }

    public bool MatchesInstall(string? installPath)
    {
        if (string.IsNullOrWhiteSpace(installPath))
            return false;
        return All.Any(i => EpicInstallPath.Same(i.InstallLocation, installPath));
    }

    /// <summary>One row per CatalogItemId (two .item files for the same Dishonored install).</summary>
    private static IReadOnlyList<EpicManifestParser.EpicItemData> DedupPlayable(
        IEnumerable<EpicManifestParser.EpicItemData> playable)
    {
        var byId = new Dictionary<string, EpicManifestParser.EpicItemData>(StringComparer.OrdinalIgnoreCase);
        foreach (EpicManifestParser.EpicItemData item in playable)
        {
            string key = string.IsNullOrWhiteSpace(item.CatalogItemId)
                ? EpicInstallPath.Normalize(item.InstallLocation) + "|" + item.DisplayName
                : item.CatalogItemId;
            if (!byId.TryGetValue(key, out EpicManifestParser.EpicItemData? existing)
                || Prefer(item, existing))
            {
                byId[key] = item;
            }
        }

        return byId.Values.ToList();
    }

    private static bool Prefer(
        EpicManifestParser.EpicItemData candidate,
        EpicManifestParser.EpicItemData existing)
    {
        bool cOk = !candidate.LaunchExecutable.StartsWith('/');
        bool eOk = !existing.LaunchExecutable.StartsWith('/');
        return cOk && !eOk;
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

}
