using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>One Epic catalog root: ProgramData Manifests → VFS rows. No tree walk.</summary>
internal sealed class EpicLibraryScanner
{
    public IReadOnlyList<GameEntry> Scan(string catalogRoot, IEnumerable<string>? otherLibraryRoots = null)
    {
        var catalog = new EpicItemCatalog(catalogRoot);
        var list = new List<GameEntry>(catalog.Playable.Count);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        foreach (EpicManifestParser.EpicItemData item in catalog.Playable)
        {
            bool folderOk = !string.IsNullOrWhiteSpace(item.InstallLocation)
                && Directory.Exists(item.InstallLocation);
            string exe = folderOk
                ? EpicManifestParser.ResolveLaunchExecutable(item.InstallLocation, item.LaunchExecutable)
                : string.Empty;
            if (!string.IsNullOrEmpty(exe) && !File.Exists(exe))
                exe = string.Empty;

            string folderName = FolderName(item.InstallLocation);
            string id = GameEntryId.ComputeId(catalog.ManifestsDir,
                string.IsNullOrEmpty(item.CatalogItemId) ? folderName : item.CatalogItemId);

            var extra = new Dictionary<string, string>
            {
                ["EpicStatus"] = folderOk ? "Installed" : "Missing",
                ["EpicItemPath"] = item.ItemFilePath,
                ["EpicCatalogItemId"] = item.CatalogItemId,
                ["EpicCatalogNamespace"] = item.CatalogNamespace,
                ["EpicAppName"] = item.AppName,
                ["LibraryRoot"] = catalog.ManifestsDir,
            };

            DateTimeOffset modified = now;
            try
            {
                if (folderOk)
                    modified = new DateTimeOffset(Directory.GetLastWriteTimeUtc(item.InstallLocation));
            }
            catch
            {
            }

            list.Add(new GameEntry(
                Id: id,
                FolderName: folderName,
                DisplayName: string.IsNullOrWhiteSpace(item.DisplayName) ? folderName : item.DisplayName,
                GameSource: GameSourceKind.Epic,
                IsSourceOverridden: false,
                ExecutablePath: exe,
                LauncherPath: "",
                CommandLineArguments: "",
                ManifestPath: item.ItemFilePath,
                LastScanned: now,
                LastModified: modified,
                PlatformMetadata: extra,
                Tags: [],
                UserOverrides: []));
        }

        if (otherLibraryRoots is not null)
        {
            foreach (EpicOrphanDiscovery.OrphanFolder orphan in EpicOrphanDiscovery.Find(catalog, otherLibraryRoots))
            {
                string folderName = Path.GetFileName(orphan.GameFolder.TrimEnd('\\', '/'));
                string id = GameEntryId.ComputeId(catalog.ManifestsDir, "orphan:" + folderName);
                list.Add(new GameEntry(
                    Id: id,
                    FolderName: folderName,
                    DisplayName: TitleText.FromFolderName(folderName),
                    GameSource: GameSourceKind.Epic,
                    IsSourceOverridden: false,
                    ExecutablePath: "",
                    LauncherPath: "",
                    CommandLineArguments: "",
                    ManifestPath: "",
                    LastScanned: now,
                    LastModified: now,
                    PlatformMetadata: new Dictionary<string, string>
                    {
                        ["EpicStatus"] = "Orphaned",
                        ["GameFolder"] = orphan.GameFolder,
                        ["LibraryRoot"] = orphan.ParentRoot,
                    },
                    Tags: [],
                    UserOverrides: []));
            }
        }

        return list;
    }

    private static string FolderName(string installLocation)
    {
        string t = installLocation.TrimEnd('\\', '/');
        int slash = Math.Max(t.LastIndexOf('\\'), t.LastIndexOf('/'));
        return slash >= 0 ? t[(slash + 1)..] : t;
    }
}
