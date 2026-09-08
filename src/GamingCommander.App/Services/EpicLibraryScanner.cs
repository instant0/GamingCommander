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
            // EpicItemData.InstallLocation is a non-nullable string; the Parser
            // substitutes "" for a missing JSON member, so this never flows null.
            // Capture the invariant once so every usage below is null-flow clean.
            string installLocation = item.InstallLocation ?? string.Empty;
            string folderName = FolderName(installLocation);
            GameEntry? match = FindKnown(known, installLocation);
            bool folderOk = !string.IsNullOrWhiteSpace(installLocation)
                && Directory.Exists(installLocation);
            string exe = "";
            string launcher = "";
            IReadOnlyList<string> candidates = [];
            if (folderOk)
            {
                (exe, launcher, candidates) = EpicLaunchResolver.Resolve(
                    installLocation, item.LaunchExecutable, folderName);
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

            if (!string.IsNullOrWhiteSpace(installLocation))
            {
                claimedFolders.Add(EpicInstallPath.Normalize(installLocation));
                claimedFolders.Add(EpicInstallPath.FolderName(installLocation));
            }
            if (match is { } m3)
            {
                claimedFolders.Add(EpicInstallPath.Normalize(m3.FolderPath));
                claimedFolders.Add(EpicInstallPath.FolderName(m3.FolderPath));
            }

            var extra = new Dictionary<string, string>
            {
                [PlatformMetadataKeys.EpicStatus] = folderOk ? "Installed" : "Missing",
                [PlatformMetadataKeys.EpicItemPath] = item.ItemFilePath,
                [PlatformMetadataKeys.EpicCatalogItemId] = item.CatalogItemId,
                [PlatformMetadataKeys.EpicCatalogNamespace] = item.CatalogNamespace,
                [PlatformMetadataKeys.EpicAppName] = item.AppName,
                [PlatformMetadataKeys.LibraryRoot] = catalog.ManifestsDir,
                [PlatformMetadataKeys.GameFolder] = installLocation,
            };
            if (candidates.Count > 1)
            {
                extra[PlatformMetadataKeys.ExeCandidateCount] = candidates.Count.ToString();
                extra[PlatformMetadataKeys.ExeCandidates] = string.Join('|',
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
                [PlatformMetadataKeys.EpicStatus] = "Orphaned",
                [PlatformMetadataKeys.GameFolder] = folder,
                [PlatformMetadataKeys.LibraryRoot] = Path.GetDirectoryName(folder) ?? "",
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
