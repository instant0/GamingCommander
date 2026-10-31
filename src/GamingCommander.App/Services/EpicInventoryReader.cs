using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>
/// Reads Epic's known inventory from ProgramData <c>*.item</c> manifests and, for folders the
/// catalog does not claim, from local <c>.mancpn</c> and binary <c>.manifest</c> files. Epic
/// identity is the catalog item. Read-only: no writes, no registration, no GraphQL.
/// </summary>
internal sealed class EpicInventoryReader : IStoreInventoryReader
{
    private readonly string _manifestsDir;
    private readonly IReadOnlyList<string> _otherRoots;
    private StoreManifestFingerprint? _fingerprint;

    /// <summary>
    /// Creates a reader. <paramref name="manifestsDir"/> defaults to the known ProgramData path;
    /// <paramref name="otherRoots"/> are library roots searched one level deep for orphan folders.
    /// </summary>
    public EpicInventoryReader(string? manifestsDir = null, IEnumerable<string>? otherRoots = null)
    {
        _manifestsDir = EpicManifestPaths.GetManifestsDir(manifestsDir);
        _otherRoots = otherRoots?
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(PathText.TrimTrailingSeparators)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
    }

    /// <inheritdoc />
    public GameSourceKind Kind => GameSourceKind.Epic;

    /// <inheritdoc />
    public StoreManifestFingerprint? Fingerprint => _fingerprint;

    /// <inheritdoc />
    public IReadOnlyList<StoreInventoryEntry> Read()
    {
        var catalog = new EpicItemCatalog(_manifestsDir);
        _fingerprint = FingerprintOf(_manifestsDir);

        var entries = new List<StoreInventoryEntry>();
        var indexByInstall = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (EpicManifestParser.EpicItemData item in catalog.Playable)
        {
            StoreInventoryEntry entry = CreateCatalogEntry(item);
            string key = PathText.NormalizeForCompare(entry.InstallPath);
            if (key.Length > 0 && indexByInstall.TryGetValue(key, out int existing))
            {
                // Two catalog items can name the same physical install; keep one row and
                // prefer the record that resolves a real exe.
                if (!entries[existing].ExeExists && entry.ExeExists)
                    entries[existing] = entry;
                continue;
            }

            if (key.Length > 0)
                indexByInstall[key] = entries.Count;
            entries.Add(entry);
        }

        foreach (EpicOrphanDiscovery.OrphanFolder orphan in EpicOrphanDiscovery.Find(catalog, _otherRoots))
        {
            string key = PathText.NormalizeForCompare(orphan.GameFolder);
            if (key.Length > 0 && indexByInstall.ContainsKey(key))
                continue;

            if (key.Length > 0)
                indexByInstall[key] = entries.Count;
            entries.Add(CreateLocalEntry(orphan.GameFolder));
        }

        return entries;
    }

    private static StoreInventoryEntry CreateCatalogEntry(EpicManifestParser.EpicItemData item)
    {
        string install = item.InstallLocation ?? string.Empty;
        bool folderExists = !string.IsNullOrWhiteSpace(install) && Directory.Exists(install);
        string? exe = ResolveExe(install, item.LaunchExecutable);
        string name = string.IsNullOrWhiteSpace(item.DisplayName)
            ? EpicInstallPath.FolderName(install)
            : item.DisplayName;

        return new StoreInventoryEntry(
            GameSourceKind.Epic,
            item.CatalogItemId ?? string.Empty,
            name,
            install,
            exe,
            folderExists ? StoreReportedState.Installed : StoreReportedState.Unknown,
            FolderExists: folderExists,
            ExeExists: exe is not null,
            new EpicInventoryMetadata(
                item.CatalogNamespace,
                item.CatalogItemId,
                item.AppName,
                item.InstallationGuid,
                item.AppVersionString,
                item.InstallSize,
                item.CompleteManifestPath,
                item.ItemFilePath,
                MancpnPath: null,
                IsCatalogItem: true));
    }

    private static StoreInventoryEntry CreateLocalEntry(string folder)
    {
        var gameDir = new DirectoryInfo(folder);
        EpicManifestParser.EpicIdentifiers? ids = EpicManifestParser.ExtractLocalIdentifiers(gameDir);
        (EpicBinaryManifest.Header? header, string? manifestPath) = ReadLocalManifest(folder);
        string? itemPath = FindLocalFile(folder, "*.item");
        string? mancpnPath = FindLocalFile(folder, "*.mancpn");

        string launch = ids?.LaunchExecutable ?? string.Empty;
        if (string.IsNullOrWhiteSpace(launch) && header is not null)
            launch = header.LaunchExe;
        string? exe = ResolveExe(folder, launch);

        string name = ids?.DisplayName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            name = EpicInstallPath.FolderName(folder);

        return new StoreInventoryEntry(
            GameSourceKind.Epic,
            ids?.CatalogItemId ?? string.Empty,
            name,
            folder,
            exe,
            StoreReportedState.Installed,
            FolderExists: true,
            ExeExists: exe is not null,
            new EpicInventoryMetadata(
                ids?.CatalogNamespace,
                ids?.CatalogItemId,
                string.IsNullOrWhiteSpace(ids?.AppName) ? header?.AppName : ids!.AppName,
                manifestPath is null ? null : Path.GetFileNameWithoutExtension(manifestPath),
                header?.BuildVersion,
                InstallSize: null,
                manifestPath,
                itemPath,
                mancpnPath,
                IsCatalogItem: false));
    }

    /// <summary>Resolves the manifest launch exe relative to the install folder and checks it once.</summary>
    private static string? ResolveExe(string installLocation, string? launchExecutable)
    {
        if (string.IsNullOrWhiteSpace(launchExecutable))
            return null;
        if (!Path.IsPathRooted(launchExecutable) && string.IsNullOrWhiteSpace(installLocation))
            return null;

        string resolved = EpicManifestParser.ResolveLaunchExecutable(installLocation, launchExecutable);
        return resolved.Length > 0 && File.Exists(resolved) ? resolved : null;
    }

    private static (EpicBinaryManifest.Header? Header, string? Path) ReadLocalManifest(string gameFolder)
    {
        string? path = FindLocalFile(gameFolder, "*.manifest");
        if (path is not null && EpicBinaryManifest.TryRead(path, out EpicBinaryManifest.Header? header))
            return (header, path);
        return (null, path);
    }

    private static string? FindLocalFile(string gameFolder, string pattern)
    {
        foreach (string store in new[] { ".egstore", ".egsstore" })
        {
            string storeDir = Path.Combine(gameFolder, store);
            foreach (string candidate in new[] { Path.Combine(storeDir, "manifests"), storeDir })
            {
                string[] files = FileSystemHelper.GetFilesSafe(new DirectoryInfo(candidate), pattern);
                if (files.Length > 0)
                    return files[0];
            }
        }

        return null;
    }

    private static StoreManifestFingerprint? FingerprintOf(string manifestsDir)
    {
        try
        {
            var dir = new DirectoryInfo(manifestsDir);
            if (!dir.Exists)
                return null;

            FileInfo[] files = dir.GetFiles("*.item", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
                return null;

            return new StoreManifestFingerprint(
                manifestsDir,
                files.Sum(file => file.Length),
                files.Max(file => file.LastWriteTimeUtc));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
