namespace GamingCommander.App.Services;

/// <summary>Base game vs DLC from live .item fields. Not MainGame* (those are empty).</summary>
internal static class EpicItemClassifier
{
    public static bool IsAddon(EpicManifestParser.EpicItemData item)
    {
        if (item.AppCategories is null)
            return false;
        return item.AppCategories.Any(c => c.Equals("addons", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>One VFS row: not addon, has a launch exe.</summary>
    public static bool IsPlayableBase(EpicManifestParser.EpicItemData item) =>
        !item.IsIncompleteInstall
        && !IsAddon(item)
        && !string.IsNullOrWhiteSpace(item.LaunchExecutable);
}
