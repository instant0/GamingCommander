using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>
/// Epic catalog VFS: ProgramData <c>*.item</c> plus Epic rows already in other library
/// roots (no second folder walk). Same <see cref="GameEntry.Id"/> as the folder scan
/// so sidecar extras stay attached.
/// </summary>
internal sealed class EpicLibraryScanner
{
    public IReadOnlyList<GameEntry> Scan(
        string catalogRoot,
        IReadOnlyList<GameEntry>? knownFromOtherLibraries = null)
    {
        var catalog = new EpicItemCatalog(catalogRoot);
        var known = knownFromOtherLibraries ?? [];
        var list = new List<GameEntry>();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var claimedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (EpicManifestParser.EpicItemData item in catalog.Playable)
        {
            string folderName = FolderName(item.InstallLocation);
            GameEntry? match = FindKnown(known, item.InstallLocation);
            bool folderOk = !string.IsNullOrWhiteSpace(item.InstallLocation)
                && Directory.Exists(item.InstallLocation);
            string exe = "";
            string launcher = "";
            IReadOnlyList<string> candidates = [];
            if (folderOk)
            {
                (exe, launcher, candidates) = EpicLaunchResolver.Resolve(
                    item.InstallLocation, item.LaunchExecutable, folderName);
            }
            if (string.IsNullOrEmpty(exe) && match is { } m1
                && !EpicLaunchResolver.IsStoreLauncher(m1.ExecutablePath))
            {
                exe = m1.ExecutablePath;
            }
            string id = match is { } m2
                ? m2.Id
                : GameEntryId.ComputeId(catalog.ManifestsDir,
                    string.IsNullOrEmpty(item.CatalogItemId) ? folderName : item.CatalogItemId);

            if (!string.IsNullOrWhiteSpace(item.InstallLocation))
            {
                claimedFolders.Add(EpicInstallPath.Normalize(item.InstallLocation));
                claimedFolders.Add(EpicInstallPath.FolderName(item.InstallLocation));
            }
            if (match is { } m3)
            {
                claimedFolders.Add(EpicInstallPath.Normalize(m3.FolderPath));
                claimedFolders.Add(EpicInstallPath.FolderName(m3.FolderPath));
            }

            var extra = new Dictionary<string, string>
            {
                ["EpicStatus"] = folderOk ? "Installed" : "Missing",
                ["EpicItemPath"] = item.ItemFilePath,
                ["EpicCatalogItemId"] = item.CatalogItemId,
                ["EpicCatalogNamespace"] = item.CatalogNamespace,
                ["EpicAppName"] = item.AppName,
                ["LibraryRoot"] = catalog.ManifestsDir,
                ["GameFolder"] = item.InstallLocation,
            };
            if (candidates.Count > 1)
            {
                extra["ExeCandidateCount"] = candidates.Count.ToString();
                extra["ExeCandidates"] = string.Join('|',
                    candidates.Select(Path.GetFileName).Where(n => !string.IsNullOrEmpty(n))!);
            }

            DateTimeOffset modified = match?.LastModified ?? now;
            list.Add(new GameEntry(
                Id: id,
                Library: string.Empty,
                FolderPath: item.InstallLocation ?? string.Empty,
                FolderName: folderName,
                DisplayName: string.IsNullOrWhiteSpace(item.DisplayName) ? folderName : item.DisplayName,
                GameSource: GameSourceKind.Epic,
                IsSourceOverridden: false,
                ExecutablePath: exe,
                LauncherPath: string.IsNullOrEmpty(launcher) ? match?.LauncherPath ?? "" : launcher,
                CommandLineArguments: match?.CommandLineArguments ?? "",
                ManifestPath: item.ItemFilePath,
                LastScanned: now,
                LastModified: modified,
                PlatformMetadata: extra,
                Tags: match?.Tags ?? [],
                UserOverrides: match?.UserOverrides ?? []));
        }

        foreach (GameEntry game in known)
        {
            if (game.GameSource != GameSourceKind.Epic)
                continue;
            string folder = game.FolderPath;
            if (string.IsNullOrWhiteSpace(folder)
                || claimedFolders.Contains(EpicInstallPath.Normalize(folder))
                || claimedFolders.Contains(EpicInstallPath.FolderName(folder))
                || catalog.MatchesInstall(folder))
            {
                continue;
            }

            var extra = new Dictionary<string, string>(game.PlatformMetadata)
            {
                ["EpicStatus"] = "Orphaned",
                ["GameFolder"] = folder,
                ["LibraryRoot"] = Path.GetDirectoryName(folder) ?? "",
            };

            list.Add(game with
            {
                PlatformMetadata = extra,
            });
        }

        return list;
    }

    private static GameEntry? FindKnown(
        IReadOnlyList<GameEntry> known,
        string? installLocation)
    {
        if (string.IsNullOrWhiteSpace(installLocation))
            return null;
        foreach (GameEntry game in known)
        {
            if (EpicInstallPath.Same(game.FolderPath, installLocation))
                return game;
        }

        return null;
    }

    private static string FolderName(string installLocation)
    {
        string t = installLocation.TrimEnd('\\', '/');
        int slash = Math.Max(t.LastIndexOf('\\'), t.LastIndexOf('/'));
        return slash >= 0 ? t[(slash + 1)..] : t;
    }

}
