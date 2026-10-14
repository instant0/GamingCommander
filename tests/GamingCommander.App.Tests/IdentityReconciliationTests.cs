using GamingCommander.App.Services;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for Plan 127 A5 — stable identity across external moves: strong store-id keys
/// relink regardless of disk; the weak folder-name key relinks only when the old folder is gone.
/// </summary>
public sealed class IdentityReconciliationTests : IDisposable
{
    private readonly string _tempDir;

    public IdentityReconciliationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "Identity_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private GamesDatabaseService CreateService() =>
        new(Path.Combine(_tempDir, "games.json"));

    private static GameEntry MakeGame(
        string id,
        string path,
        string folder,
        string steamAppId = "",
        GameSourceKind source = GameSourceKind.Standalone) =>
        new(
            Identity: new IdentityInfo
            {
                Id = id,
                Library = "Lib",
                DisplayName = folder,
                GameSource = source,
                IsSourceOverridden = false,
            },
            Location: new LocationInfo { FolderPath = path, FolderName = folder },
            Launch: new LaunchInfo { ExecutablePath = Path.Combine(path, "game.exe") },
            Tags: [],
            UserOverrides: [],
            LastScanned: DateTimeOffset.UtcNow,
            LastModified: DateTimeOffset.UtcNow)
        {
            Store = new StoreInfo { SteamAppId = steamAppId },
        };

    [Fact]
    public void ExternalMove_WithSteamAppId_RelinksAndPreservesId()
    {
        var svc = CreateService();
        string oldPath = Path.Combine(_tempDir, "OldFolder"); // not created → gone
        svc.SetGamesForLibrary("Lib", [MakeGame("old-id", oldPath, "OldFolder", steamAppId: "400", source: GameSourceKind.Steam)]);

        string newPath = Path.Combine(_tempDir, "NewFolder");
        svc.SetGamesForLibrary("Lib", [MakeGame("new-id", newPath, "NewFolder", steamAppId: "400", source: GameSourceKind.Steam)]);

        var games = svc.GetGamesForLibrary("Lib");
        Assert.Single(games);
        Assert.Equal("old-id", games[0].Id);
        Assert.Equal(newPath, games[0].FolderPath);
        Assert.Contains(oldPath, games[0].Location.PreviousLocations);
    }

    [Fact]
    public void ExternalMove_FolderNameOnly_WhenOldFolderGone_Relinks()
    {
        var svc = CreateService();
        string oldPath = Path.Combine(_tempDir, "GameOld"); // gone
        svc.SetGamesForLibrary("Lib", [MakeGame("old-id", oldPath, "Game")]);

        string newPath = Path.Combine(_tempDir, "GameNew");
        svc.SetGamesForLibrary("Lib", [MakeGame("new-id", newPath, "Game")]); // same folder name

        var games = svc.GetGamesForLibrary("Lib");
        Assert.Single(games);
        Assert.Equal("old-id", games[0].Id);
        Assert.Equal(newPath, games[0].FolderPath);
    }

    [Fact]
    public void FolderNameMatch_WhenOldFolderExists_DoesNotRelink()
    {
        var svc = CreateService();
        string oldPath = Path.Combine(_tempDir, "StillThere");
        Directory.CreateDirectory(oldPath);
        svc.SetGamesForLibrary("Lib", [MakeGame("old-id", oldPath, "Game")]);

        string newPath = Path.Combine(_tempDir, "Elsewhere");
        svc.SetGamesForLibrary("Lib", [MakeGame("new-id", newPath, "Game")]);

        var games = svc.GetGamesForLibrary("Lib");
        Assert.Single(games);
        Assert.Equal("new-id", games[0].Id); // ambiguous — do not guess
    }

    [Fact]
    public void SamePath_KeepsIdentity()
    {
        var svc = CreateService();
        string path = Path.Combine(_tempDir, "Same");
        Directory.CreateDirectory(path);
        svc.SetGamesForLibrary("Lib", [MakeGame("id1", path, "Same")]);
        svc.SetGamesForLibrary("Lib", [MakeGame("id1", path, "Same")]);

        var games = svc.GetGamesForLibrary("Lib");
        Assert.Single(games);
        Assert.Equal("id1", games[0].Id);
        Assert.Empty(games[0].Location.PreviousLocations);
    }

    [Fact]
    public void WithRebuiltKeys_BuildsFolderAndStoreKeys()
    {
        GameEntry keyed = GameIdentity.WithRebuiltKeys(MakeGame("id", @"D:\g", "Folder", steamAppId: "400"));

        Assert.Contains(keyed.Identity.IdentityKeys, k => k.Kind == IdentityKeyKinds.FolderName && k.Value == "Folder");
        Assert.Contains(keyed.Identity.IdentityKeys, k => k.Kind == IdentityKeyKinds.SteamAppId && k.Value == "400");
    }
}
