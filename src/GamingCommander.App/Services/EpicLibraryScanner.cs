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
        IReadOnlyList<(string RootPath, GameEntry Game)>? knownFromOtherRoots = null)
    {
        var catalog = new EpicItemCatalog(catalogRoot);
        var known = knownFromOtherRoots ?? [];
        var list = new List<GameEntry>();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var claimedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (EpicManifestParser.EpicItemData item in catalog.Playable)
        {
            string folderName = FolderName(item.InstallLocation);
            (string RootPath, GameEntry Game)? match = FindKnown(known, item.InstallLocation);
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
                && !EpicLaunchResolver.IsStoreLauncher(m1.Game.ExecutablePath))
            {
                exe = m1.Game.ExecutablePath;
            }
            string id = match is { } m2
                ? m2.Game.Id
                : GameEntryId.ComputeId(catalog.ManifestsDir,
                    string.IsNullOrEmpty(item.CatalogItemId) ? folderName : item.CatalogItemId);

            if (!string.IsNullOrWhiteSpace(item.InstallLocation))
            {
                claimedFolders.Add(EpicInstallPath.Normalize(item.InstallLocation));
                claimedFolders.Add(EpicInstallPath.FolderName(item.InstallLocation));
            }
            if (match is { } m3)
            {
                string knownFolder = Path.Combine(m3.RootPath, m3.Game.FolderName);
                claimedFolders.Add(EpicInstallPath.Normalize(knownFolder));
                claimedFolders.Add(EpicInstallPath.FolderName(knownFolder));
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

            DateTimeOffset modified = match?.Game.LastModified ?? now;
            list.Add(new GameEntry(
                Id: id,
                FolderName: folderName,
                DisplayName: string.IsNullOrWhiteSpace(item.DisplayName) ? folderName : item.DisplayName,
                GameSource: GameSourceKind.Epic,
                IsSourceOverridden: false,
                ExecutablePath: exe,
                LauncherPath: string.IsNullOrEmpty(launcher) ? match?.Game.LauncherPath ?? "" : launcher,
                CommandLineArguments: match?.Game.CommandLineArguments ?? "",
                ManifestPath: item.ItemFilePath,
                LastScanned: now,
                LastModified: modified,
                PlatformMetadata: extra,
                Tags: match?.Game.Tags ?? [],
                UserOverrides: match?.Game.UserOverrides ?? []));
        }

        foreach ((string rootPath, GameEntry game) in known)
        {
            if (game.GameSource != GameSourceKind.Epic)
                continue;
            string folder = Path.Combine(rootPath, game.FolderName);
            if (claimedFolders.Contains(EpicInstallPath.Normalize(folder))
                || claimedFolders.Contains(EpicInstallPath.FolderName(folder))
                || catalog.MatchesInstall(folder))
            {
                continue;
            }

            var extra = new Dictionary<string, string>(game.PlatformMetadata)
            {
                ["EpicStatus"] = "Orphaned",
                ["GameFolder"] = folder,
                ["LibraryRoot"] = rootPath,
            };

            list.Add(game with
            {
                PlatformMetadata = extra,
            });
        }

        return list;
    }

    private static (string RootPath, GameEntry Game)? FindKnown(
        IReadOnlyList<(string RootPath, GameEntry Game)> known,
        string? installLocation)
    {
        if (string.IsNullOrWhiteSpace(installLocation))
            return null;
        foreach ((string root, GameEntry game) in known)
        {
            if (EpicInstallPath.Same(Path.Combine(root, game.FolderName), installLocation))
                return (root, game);
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
