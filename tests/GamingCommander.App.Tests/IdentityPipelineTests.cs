using GamingCommander.App.Services;
using GamingCommander.App.Services.Metadata;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Tests;

/// <summary>
/// E7/E8 tests (2026-09-07).
///   E7: identity query pipeline — exe stem + PE title come BEFORE folder name;
///       folder name is LAST (acronym folders must never be queried verbatim first).
///   E8: title persistence — user-picked titles are authoritative and survive rescan.
/// </summary>
public sealed class IdentityPipelineTests
{
    // ════════════════════════════════════════════════════════════════
    //  E7 — query ordering
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void SearchQueries_PeTitleBeforeFolderName()
    {
        // E7: PE title → exe stem → display → folder (LAST). The acronym folder
        // "jag2" must appear after the PE title "Jagged Alliance 2 Gold".
        IReadOnlyList<string> q = TitleText.SearchQueries(
            "Jagged Alliance 2 Gold",   // PE title (candidate 1)
            "ja2",                      // exe stem (candidate 2)
            "jag2",                     // display name (candidate 3)
            "jag2");                    // folder name (candidate 4, LAST)

        int peIdx = q.ToList().FindIndex(s => s.Contains("Jagged", StringComparison.OrdinalIgnoreCase));
        int folderIdx = q.ToList().FindIndex(s => s.Equals("jag2", StringComparison.OrdinalIgnoreCase));
        Assert.True(peIdx >= 0, "PE title must be a query candidate");
        Assert.True(folderIdx > peIdx, "folder name must come AFTER the PE title");
    }

    [Fact]
    public void SearchQueries_ExeStemBeforeFolderName()
    {
        // S8 root cause: dungeon of the endless.exe stem must be queried before
        // the folder acronym; exact exe-stem match resolves the right title.
        IReadOnlyList<string> q = TitleText.SearchQueries(
            null,                       // no PE title
            "dungeonoftheendless",      // exe stem
            "endless",                  // display name
            "endless");                 // folder name
        int stemIdx = q.ToList().FindIndex(s => s.Contains("dungeonoftheendless", StringComparison.OrdinalIgnoreCase));
        Assert.True(stemIdx >= 0, "exe stem must be a query candidate");
        Assert.True(stemIdx == 0, "exe stem must be the FIRST query when no PE title");
    }

    // ════════════════════════════════════════════════════════════════
    //  E8 — title persistence through rescan merge
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void MergeGameEntries_PreservesPickedTitle()
    {
        // E8: a user-picked title (TitleSource=PcgwPick) must survive a rescan
        // merge even when the scan re-detects the folder-derived name.
        var existing = new GameEntry(
            Id: "root|jag2",
            Library: "root",
            FolderPath: "/d/jag2",
            FolderName: "jag2",
            DisplayName: "Jagged Alliance 2",
            GameSource: GameSourceKind.Standalone,
            IsSourceOverridden: false,
            ExecutablePath: "/d/jag2/ja2.exe",
            LauncherPath: string.Empty,
            CommandLineArguments: string.Empty,
            ManifestPath: string.Empty,
            LastScanned: DateTimeOffset.UtcNow,
            LastModified: DateTimeOffset.UtcNow,
            PlatformMetadata: new Dictionary<string, string> { ["TitleSource"] = "PcgwPick" },
            Tags: [],
            UserOverrides: []);

        var scanned = existing with
        {
            DisplayName = "jag2",
            PlatformMetadata = new Dictionary<string, string> { ["TitleSource"] = "FolderExeMatch" },
        };

        // GamesDatabaseService.MergeGameEntries is private; verify the public
        // SetGamesForLibrary (rescan path) preserves the pick via a real service.
        string dbPath = Path.Combine(Path.GetTempPath(), "E8Merge_" + Guid.NewGuid().ToString("N")[..8] + ".json");
        try
        {
            var svc = new GamesDatabaseService(dbPath);
            svc.SetGamesForLibrary("root", [existing]);
            svc.SetGamesForLibrary("root", [scanned]);

            var merged = svc.GetGamesForLibrary("root").Single();
            Assert.Equal("Jagged Alliance 2", merged.DisplayName);   // picked title survives
            Assert.Equal("PcgwPick", merged.PlatformMetadata["TitleSource"]);
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }
}