using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

public sealed class EpicLibraryScannerTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "epic_" + Guid.NewGuid().ToString("N")[..8]);

    public EpicLibraryScannerTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, true);
    }

    [Fact]
    public void Scan_SkipsAddons_KeepsBase()
    {
        File.WriteAllText(Path.Combine(_dir, "base.item"), """
            {"DisplayName":"Base Game","InstallLocation":"/tmp/does-not-exist-epic-base",
             "LaunchExecutable":"Game.exe","CatalogNamespace":"ns","CatalogItemId":"item1",
             "AppName":"app","bIsApplication":true,"AppCategories":["games","applications"]}
            """);
        File.WriteAllText(Path.Combine(_dir, "dlc.item"), """
            {"DisplayName":"DLC Pack","InstallLocation":"/tmp/does-not-exist-epic-base",
             "LaunchExecutable":"","CatalogNamespace":"ns","CatalogItemId":"item2",
             "AppName":"dlc","bIsApplication":false,"AppCategories":["addons"]}
            """);

        var games = new EpicLibraryScanner().Scan(_dir);
        Assert.Single(games);
        Assert.Equal("Base Game", games[0].DisplayName);
        Assert.Equal(GameSourceKind.Epic, games[0].GameSource);
        Assert.Equal("Missing", games[0].PlatformMetadata["EpicStatus"]);
    }

    [Fact]
    public void Scan_ListsOrphanFromOtherRoot()
    {
        string games = Path.Combine(_dir, "games");
        string orphan = Path.Combine(games, "OrphanTitle");
        Directory.CreateDirectory(Path.Combine(orphan, ".egstore"));
        File.WriteAllText(Path.Combine(_dir, "only-dlc.item"), """
            {"DisplayName":"DLC","InstallLocation":"/nope","LaunchExecutable":"",
             "AppCategories":["addons"],"bIsApplication":false}
            """);

        var list = new EpicLibraryScanner().Scan(_dir, [games]);
        Assert.Contains(list, g => g.PlatformMetadata.GetValueOrDefault("EpicStatus") == "Orphaned"
            && g.FolderName == "OrphanTitle");
    }
}
