namespace GamingCommander.Core.Models;

/// <summary>
/// JSON member names of the Epic Games Store .item manifest format, shared by
/// EpicManifestParser (read) and EpicItemWriter (write). The writer must emit
/// exactly the spellings the launcher and the parser read, so the names are
/// pinned here as constants — writer/reader drift becomes a compile error
/// instead of silent data corruption.
/// Write-only members of the format (FormatVersion, ChunkDbs, ManifestHash, ...)
/// remain literals in EpicItemWriter: no reader exists, so there is no drift
/// surface.
/// </summary>
public static class EpicItemSchema
{
    /// <summary>Human-readable game title.</summary>
    public const string DisplayName = "DisplayName";

    /// <summary>Absolute install directory of the game.</summary>
    public const string InstallLocation = "InstallLocation";

    /// <summary>Path to the launch executable, relative to InstallLocation.</summary>
    public const string LaunchExecutable = "LaunchExecutable";

    /// <summary>Epic catalog namespace (e.g. "oceano").</summary>
    public const string CatalogNamespace = "CatalogNamespace";

    /// <summary>Epic catalog item id.</summary>
    public const string CatalogItemId = "CatalogItemId";

    /// <summary>Epic app name (artifact id).</summary>
    public const string AppName = "AppName";

    /// <summary>True while the install is incomplete (download/update in progress).</summary>
    public const string BIsIncompleteInstall = "bIsIncompleteInstall";

    /// <summary>True when the item is a launchable application.</summary>
    public const string BIsApplication = "bIsApplication";

    /// <summary>Array of category strings (e.g. "games", "applications").</summary>
    public const string AppCategories = "AppCategories";
}