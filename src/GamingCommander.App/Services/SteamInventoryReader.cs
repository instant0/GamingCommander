using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>
/// Reads Steam's installed-game inventory from <c>appmanifest_*.acf</c> across every library.
/// The VDF is a path locator only; the ACF files and <c>steamapps/common</c> are the content
/// authority. The <c>apps</c> blocks are never treated as the installed set.
/// </summary>
internal sealed class SteamInventoryReader : IStoreInventoryReader
{
    private readonly IReadOnlyList<string> _configuredRoots;
    private StoreManifestFingerprint? _fingerprint;

    /// <summary>Creates a reader over the configured Steam library roots (anchors).</summary>
    public SteamInventoryReader(IEnumerable<string> configuredRoots)
    {
        _configuredRoots = configuredRoots
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(PathText.TrimTrailingSeparators)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <inheritdoc />
    public GameSourceKind Kind => GameSourceKind.Steam;

    /// <inheritdoc />
    public StoreManifestFingerprint? Fingerprint => _fingerprint;

    /// <inheritdoc />
    public IReadOnlyList<StoreInventoryEntry> Read()
    {
        IReadOnlyList<string> roots = DiscoverRoots();

        Dictionary<string, AcfInfo> acfs = CollectAcfs(roots);
        _fingerprint = FingerprintOfFirstVdf(roots);

        var entries = new List<StoreInventoryEntry>();
        var covered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (AcfInfo info in acfs.Values)
        {
            string installPath = Path.Combine(info.LibraryPath, "steamapps", "common", info.Installdir);
            bool folderExists = Directory.Exists(installPath);
            covered.Add(PathText.NormalizeForCompare(installPath));

            entries.Add(new StoreInventoryEntry(
                Kind,
                info.AppId,
                string.IsNullOrWhiteSpace(info.Name) ? info.Installdir : info.Name,
                installPath,
                LaunchExe: null,
                StateFromFlags(info.StateFlags),
                FolderExists: folderExists,
                ExeExists: false,
                new SteamInventoryMetadata(
                    info.AppId,
                    info.AcfFilePath,
                    info.LibraryPath,
                    info.Installdir,
                    info.LastUpdated,
                    info.BuildId,
                    ParseFlags(info.StateFlags))));
        }

        // A folder under steamapps/common with no ACF is still a Steam game (orphaned ACF).
        foreach (string root in roots)
        {
            string common = Path.Combine(root, "steamapps", "common");
            foreach (DirectoryInfo folder in FileSystemHelper.GetDirectoriesSafe(common))
            {
                string installPath = PathText.TrimTrailingSeparators(folder.FullName);
                if (covered.Contains(PathText.NormalizeForCompare(installPath)))
                    continue;

                entries.Add(new StoreInventoryEntry(
                    Kind,
                    StoreId: string.Empty,
                    folder.Name,
                    installPath,
                    LaunchExe: null,
                    StoreReportedState.Unknown,
                    FolderExists: true,
                    ExeExists: false,
                    new SteamInventoryMetadata(
                        string.Empty, null, root, folder.Name, null, null, 0)));
            }
        }

        return entries;
    }

    /// <summary>Configured roots plus every library path reachable through libraryfolders.vdf.</summary>
    private List<string> DiscoverRoots()
    {
        var roots = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<string>(_configuredRoots);

        while (pending.Count > 0)
        {
            string root = PathText.TrimTrailingSeparators(pending.Dequeue());
            if (string.IsNullOrWhiteSpace(root) || !seen.Add(root))
                continue;

            roots.Add(root);
            foreach (string discovered in SteamAcfParser.DiscoverLibraryPaths(root))
            {
                if (!seen.Contains(discovered))
                    pending.Enqueue(discovered);
            }
        }

        return roots;
    }

    /// <summary>Collects ACFs by AppID; a record whose folder exists wins over a stale one.</summary>
    private static Dictionary<string, AcfInfo> CollectAcfs(IEnumerable<string> roots)
    {
        var acfs = new Dictionary<string, AcfInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (string root in roots)
        {
            string steamapps = Path.Combine(root, "steamapps");
            foreach (string acfPath in FileSystemHelper.GetFilesSafe(
                new DirectoryInfo(steamapps), "appmanifest_*.acf"))
            {
                AcfInfo? info = SteamAcfParser.ParseAcfFile(acfPath, root);
                if (info is null || string.IsNullOrWhiteSpace(info.AppId))
                    continue;

                if (!acfs.TryGetValue(info.AppId, out AcfInfo? existing)
                    || (!HasFolder(existing) && HasFolder(info)))
                {
                    acfs[info.AppId] = info;
                }
            }
        }

        return acfs;
    }

    private static bool HasFolder(AcfInfo info) =>
        Directory.Exists(Path.Combine(info.LibraryPath, "steamapps", "common", info.Installdir));

    private static StoreManifestFingerprint? FingerprintOfFirstVdf(IEnumerable<string> roots)
    {
        foreach (string root in roots)
        {
            string vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdf))
                continue;

            try
            {
                var file = new FileInfo(vdf);
                return new StoreManifestFingerprint(vdf, file.Length, file.LastWriteTimeUtc);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        return null;
    }

    private static StoreReportedState StateFromFlags(string raw) =>
        int.TryParse(raw, out int flags)
            ? flags switch
            {
                6 => StoreReportedState.UpdateRequired,
                4 => StoreReportedState.Installed,
                _ => StoreReportedState.Unknown,
            }
            : StoreReportedState.Unknown;

    private static int ParseFlags(string raw) =>
        int.TryParse(raw, out int flags) ? flags : 0;
}
