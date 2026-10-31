using GamingCommander.App.Services;
using GamingCommander.Core;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;
using Xunit;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for GamesDatabaseService — flat games[] JSON persistence, in-memory
/// caching, and anchor (Library) scoped CRUD operations.
/// </summary>
public sealed class GamesDatabaseServiceTests : IDisposable
{
    private readonly string _tempDir;

    public GamesDatabaseServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "DbServiceTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private GamesDatabaseService CreateService()
    {
        return new GamesDatabaseService(Path.Combine(_tempDir, "games.json"));
    }

    private static GameEntry MakeGame(
        string id,
        string library,
        string folder = "GameFolder",
        string display = "Test Game") =>
        new(
            Identity: new IdentityInfo
            {
                Id = id,
                Library = library,
                DisplayName = display,
                GameSource = GameSourceKind.Standalone,
                IsSourceOverridden = false,
            },
            Location: new LocationInfo { FolderPath = folder, FolderName = folder },
            Launch: new LaunchInfo { ExecutablePath = $@"C:\Games\{folder}\game.exe" },
            Tags: [],
            UserOverrides: [],
            LastScanned: DateTimeOffset.Now,
            LastModified: DateTimeOffset.Now);

    // ════════════════════════════════════════════════════════════════
    //  Load / Save
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Load_WithNoFile_ReturnsEmptyDatabase()
    {
        var svc = CreateService();
        var db = svc.Load();

        Assert.NotNull(db);
        Assert.Empty(db.Games);
    }

    [Fact]
    public void SaveLoad_FullDto_RoundTrip()
    {
        var svc = CreateService();
        DateTimeOffset lastStoreSeen = DateTimeOffset.FromUnixTimeSeconds(1_760_000_000);
        var baseGame = MakeGame("g1", "EPIC", "MyFolder", "Epic Game");
        GameEntry game = baseGame with
        {
            Identity = baseGame.Identity with { GameSource = GameSourceKind.Epic },
            Install = new InstallInfo
            {
                State = InstallState.Moved,
                Version = "1.2",
                UpdateAvailable = true,
                StoreReportedState = StoreReportedState.UpdateRequired,
                LastStoreSeenUtc = lastStoreSeen,
            },
            Tracking = new TrackingInfo
            {
                LaunchCount = 3,
                PlayTimeSeconds = 3600,
                LastPlayed = DateTimeOffset.UnixEpoch,
            },
            UserState = new UserStateInfo
            {
                Favourite = true,
                Rating = 5,
                Notes = "great",
                Completion = CompletionState.Completed,
            },
            Tags = [Tag.User("RPG"), Tag.Derived("Unreal Engine 5", TagKind.Engine)],
            Launch = baseGame.Launch with
            {
                Profiles = [new LaunchProfile { Name = "VR", Arguments = "-vr", RunAsAdmin = true }],
            },
            Relations = new RelationsInfo { Kind = GameKind.Dlc, ParentId = "base-1" },
            Detection = new DetectionInfo
            {
                Tier = IdentificationTier.Locked,
                Origin = DetectionOrigin.StoreManifest,
                TitleSource = "SteamApp",
                ManifestKind = "Acf",
                Signals = ["SteamStructural"],
                CandidateExes = ["game.exe"],
            },
            Store = new StoreInfo
            {
                SteamAppId = "400",
                EpicCatalogItemId = "abc123",
                StoreKey = "EaApp:ea-content-1",
                EaContentId = "ea-content-1",
                UbisoftGameId = "ubi-42",
                XboxPackageFamily = "Publisher.Game_8wekyb3d8bbwe",
                RockstarTitleId = "rockstar-7",
            },
            UserOverrides = new Dictionary<string, string> { ["DisplayName"] = "2026-07-26T14:30:00Z" },
        };
        svc.SetGamesForLibrary("EPIC", [game]);
        svc.Save(svc.Load());

        Assert.True(
            File.Exists(Path.Combine(_tempDir, "games.json")),
            "Save should create the JSON file on disk");

        // Fresh service instance (cleared cache) verifies the disk read.
        GameEntry loaded = CreateService().Load().Games.Single();
        Assert.Equal("g1", loaded.Id);
        Assert.Equal("EPIC", loaded.Library);
        Assert.Equal(@"C:\Games\MyFolder\game.exe", loaded.ExecutablePath);
        Assert.Equal(GameSourceKind.Epic, loaded.GameSource);

        Assert.Equal(InstallState.Moved, loaded.Install.State);
        Assert.Equal("1.2", loaded.Install.Version);
        Assert.True(loaded.Install.UpdateAvailable);
        Assert.Equal(StoreReportedState.UpdateRequired, loaded.Install.StoreReportedState);
        Assert.Equal(lastStoreSeen, loaded.Install.LastStoreSeenUtc);

        Assert.Equal(3, loaded.Tracking.LaunchCount);
        Assert.Equal(3600, loaded.Tracking.PlayTimeSeconds);
        Assert.Equal(DateTimeOffset.UnixEpoch, loaded.Tracking.LastPlayed);

        Assert.True(loaded.UserState.Favourite);
        Assert.Equal(5, loaded.UserState.Rating);
        Assert.Equal("great", loaded.UserState.Notes);
        Assert.Equal(CompletionState.Completed, loaded.UserState.Completion);

        Assert.Equal(["RPG", "Unreal Engine 5"], loaded.Tags.Select(t => t.Name));
        Assert.Equal(TagKind.Engine, loaded.Tags[1].Kind);
        Assert.True(loaded.Tags[1].Auto);

        Assert.Single(loaded.Launch.Profiles);
        Assert.Equal("VR", loaded.Launch.Profiles[0].Name);
        Assert.True(loaded.Launch.Profiles[0].RunAsAdmin);

        Assert.Equal(GameKind.Dlc, loaded.Relations.Kind);
        Assert.Equal("base-1", loaded.Relations.ParentId);

        Assert.Equal(IdentificationTier.Locked, loaded.Detection.Tier);
        Assert.Equal(DetectionOrigin.StoreManifest, loaded.Detection.Origin);
        Assert.Equal("SteamApp", loaded.Detection.TitleSource);
        Assert.Equal("Acf", loaded.Detection.ManifestKind);
        Assert.Contains("SteamStructural", loaded.Detection.Signals);
        Assert.Contains("game.exe", loaded.Detection.CandidateExes);

        Assert.Equal("400", loaded.Store.SteamAppId);
        Assert.Equal("abc123", loaded.Store.EpicCatalogItemId);
        Assert.Equal("EaApp:ea-content-1", loaded.Store.StoreKey);
        Assert.Equal("ea-content-1", loaded.Store.EaContentId);
        Assert.Equal("ubi-42", loaded.Store.UbisoftGameId);
        Assert.Equal("Publisher.Game_8wekyb3d8bbwe", loaded.Store.XboxPackageFamily);
        Assert.Equal("rockstar-7", loaded.Store.RockstarTitleId);

        Assert.True(loaded.UserOverrides.ContainsKey("DisplayName"));
    }

    // ════════════════════════════════════════════════════════════════
    //  Anchor (Library) scoped operations
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void SetGamesForLibrary_EntriesAreScopedToLibrary()
    {
        var svc = CreateService();
        svc.SetGamesForLibrary("Steam", [MakeGame("s1", "Steam")]);
        svc.SetGamesForLibrary(@"d:\games", [MakeGame("d1", @"d:\games")]);

        Assert.Single(svc.GetGamesForLibrary("Steam"));
        Assert.Single(svc.GetGamesForLibrary(@"d:\games"));
        Assert.Equal(2, svc.Load().Games.Count);
        Assert.Empty(svc.GetGamesForLibrary(@"E:\Nonexistent"));
    }

    [Fact]
    public void SetGamesForLibrary_PreservesUserOverridesOnRescan()
    {
        var svc = CreateService();
        var baseGame = MakeGame("g1", "Steam");
        var game = baseGame with
        {
            Identity = baseGame.Identity with { DisplayName = "My Custom Name" },
            UserOverrides = new Dictionary<string, string> { ["DisplayName"] = "2026-07-26T14:30:00Z" },
            Tags = [Tag.User("RPG"), Tag.User("Co-op")],
        };
        svc.SetGamesForLibrary("Steam", [game]);

        // Rescan supplies a fresh auto-detected entry with the same ID.
        var scannedGame = baseGame with
        {
            Identity = baseGame.Identity with { DisplayName = "Auto Name" },
            Tags = [],
        };
        svc.SetGamesForLibrary("Steam", [scannedGame]);

        var games = svc.GetGamesForLibrary("Steam");
        Assert.Single(games);
        Assert.Equal("My Custom Name", games[0].DisplayName);   // user override preserved
        Assert.Equal(2, games[0].Tags.Count);                   // user tags preserved
        Assert.Equal("RPG", games[0].Tags[0].Name);
    }

    [Fact]
    public void RemoveGamesForLibrary_RemovesOnlyThatLibrary()
    {
        var svc = CreateService();
        svc.SetGamesForLibrary("Steam", [MakeGame("s1", "Steam")]);
        svc.SetGamesForLibrary(@"d:\games", [MakeGame("d1", @"d:\games")]);

        svc.RemoveGamesForLibrary("Steam");

        Assert.Empty(svc.GetGamesForLibrary("Steam"));
        Assert.Single(svc.GetGamesForLibrary(@"d:\games"));
        Assert.Single(svc.Load().Games);
    }

    // ════════════════════════════════════════════════════════════════
    //  Single-entry CRUD
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void UpdateGameEntry_UpdatesFields()
    {
        var svc = CreateService();
        svc.SetGamesForLibrary("Steam", [MakeGame("g1", "Steam")]);

        var updated = MakeGame("g1", "Steam", display: "Renamed Game");
        svc.UpdateGameEntry(updated);

        var games = svc.GetGamesForLibrary("Steam");
        Assert.Single(games);
        Assert.Equal("Renamed Game", games[0].DisplayName);
    }

    [Fact]
    public void UpdateGameEntry_UnknownId_IsNoOp()
    {
        var svc = CreateService();
        svc.SetGamesForLibrary("Steam", [MakeGame("g1", "Steam")]);

        svc.UpdateGameEntry(MakeGame("nope", "Steam"));

        Assert.Single(svc.GetGamesForLibrary("Steam"));
    }

    [Fact]
    public void DeleteGameEntry_RemovesEntry()
    {
        var svc = CreateService();
        svc.SetGamesForLibrary("Steam", [MakeGame("g1", "Steam"), MakeGame("g2", "Steam")]);

        svc.DeleteGameEntry("g1");

        var games = svc.GetGamesForLibrary("Steam");
        Assert.Single(games);
        Assert.Equal("g2", games[0].Id);
    }

    [Fact]
    public void RetagGame_ChangesSourceTypeAndKeepsEntryId()
    {
        // P0 contract: re-anchoring (RetagGame) changes GameSource and marks the
        // override, but the entry ID is identity and must never be regenerated.
        var svc = CreateService();
        string id = "g1";
        svc.SetGamesForLibrary(@"d:\games", [MakeGame(id, @"d:\games")]);

        svc.RetagGame(id, GameSourceKind.Epic);

        var games = svc.GetGamesForLibrary(@"d:\games");
        Assert.Single(games);
        Assert.Equal(id, games[0].Id);
        Assert.Equal(GameSourceKind.Epic, games[0].GameSource);
        Assert.True(games[0].IsSourceOverridden);

        // Persisted via the same cache update.
        var svc2 = CreateService();
        Assert.Equal(GameSourceKind.Epic, svc2.Load().Games.Single(g => g.Id == id).GameSource);
    }

    [Theory]
    [InlineData(@"C:\Games\Foo", "c:/games/foo/")]
    public void SetGamesForLibrary_EquivalentPaths_AreNotARelocation(
        string existingPath, string scannedPath)
    {
        // Path equality is PathText.Equals: separators, trailing separators and case
        // do not make two paths different, so the scanned entry is not a move from
        // the old one (and never adopts its id through the weak name hint).
        var svc = CreateService();
        GameEntry existing = MakeGame("old-id", "Lib") with
        {
            Location = new LocationInfo { FolderPath = existingPath, FolderName = "Foo" },
        };
        svc.SetGamesForLibrary("Lib", [existing]);

        GameEntry scanned = MakeGame("new-id", "Lib") with
        {
            Location = new LocationInfo { FolderPath = scannedPath, FolderName = "Foo" },
        };
        svc.SetGamesForLibrary("Lib", [scanned]);

        GameEntry kept = Assert.Single(svc.GetGamesForLibrary("Lib"));
        Assert.Equal("new-id", kept.Id);
        Assert.Empty(kept.Location.PreviousLocations);
    }
}