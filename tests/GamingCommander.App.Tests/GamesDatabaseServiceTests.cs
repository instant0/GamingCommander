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
    public void Load_WithValidFile_ReturnsPersistedData()
    {
        var svc = CreateService();
        svc.SetGamesForLibrary("Steam", [MakeGame("g1", "Steam")]);
        svc.Save(svc.Load());

        // Fresh service instance (cleared cache) verifies disk read.
        var svc2 = CreateService();
        var db = svc2.Load();

        Assert.Single(db.Games);
        Assert.Equal("g1", db.Games[0].Id);
        Assert.Equal("Steam", db.Games[0].Library);
    }

    [Fact]
    public void SaveLoad_TrackingInstallUserState_RoundTrip()
    {
        var svc = CreateService();
        var baseGame = MakeGame("g1", "Steam");
        var game = baseGame with
        {
            Install = new InstallInfo { State = InstallState.Moved, Version = "1.2", UpdateAvailable = true },
            Tracking = new TrackingInfo { LaunchCount = 3, PlayTimeSeconds = 3600, LastPlayed = DateTimeOffset.UnixEpoch },
            UserState = new UserStateInfo
            {
                Favourite = true,
                Rating = 5,
                Notes = "great",
                Completion = CompletionState.Completed,
            },
        };
        svc.SetGamesForLibrary("Steam", [game]);
        svc.Save(svc.Load());

        GameEntry loaded = CreateService().Load().Games.Single();
        Assert.Equal(InstallState.Moved, loaded.Install.State);
        Assert.Equal("1.2", loaded.Install.Version);
        Assert.True(loaded.Install.UpdateAvailable);
        Assert.Equal(3, loaded.Tracking.LaunchCount);
        Assert.Equal(3600, loaded.Tracking.PlayTimeSeconds);
        Assert.True(loaded.UserState.Favourite);
        Assert.Equal(5, loaded.UserState.Rating);
        Assert.Equal("great", loaded.UserState.Notes);
        Assert.Equal(CompletionState.Completed, loaded.UserState.Completion);
    }

    [Fact]
    public void SaveLoad_ProfilesRelationsTags_RoundTrip()
    {
        var svc = CreateService();
        var baseGame = MakeGame("g1", "Steam");
        var game = baseGame with
        {
            Tags = [Tag.User("RPG"), Tag.Derived("Unreal Engine 5", TagKind.Engine)],
            Launch = baseGame.Launch with
            {
                Profiles = [new LaunchProfile { Name = "VR", Arguments = "-vr", RunAsAdmin = true }],
            },
            Relations = new RelationsInfo { Kind = GameKind.Dlc, ParentId = "base-1" },
        };
        svc.SetGamesForLibrary("Steam", [game]);
        svc.Save(svc.Load());

        GameEntry loaded = CreateService().Load().Games.Single();
        Assert.Equal(2, loaded.Tags.Count);
        Assert.Equal(TagKind.Engine, loaded.Tags[1].Kind);
        Assert.True(loaded.Tags[1].Auto);
        Assert.Single(loaded.Launch.Profiles);
        Assert.Equal("VR", loaded.Launch.Profiles[0].Name);
        Assert.True(loaded.Launch.Profiles[0].RunAsAdmin);
        Assert.Equal(GameKind.Dlc, loaded.Relations.Kind);
        Assert.Equal("base-1", loaded.Relations.ParentId);
    }

    [Fact]
    public void Load_WithFutureSchemaVersion_ReturnsEmpty()
    {
        string dbPath = Path.Combine(_tempDir, "games.json");
        File.WriteAllText(dbPath, """{ "Version": 99, "Games": [] }""");
        var svc = new GamesDatabaseService(dbPath);

        Assert.Empty(svc.Load().Games);
    }

    [Fact]
    public void Save_WritesCurrentSchemaVersion()
    {
        var svc = CreateService();
        svc.SetGamesForLibrary("Steam", []);
        svc.Save(svc.Load());

        string json = File.ReadAllText(Path.Combine(_tempDir, "games.json"));
        Assert.Contains($"\"Version\": {GamingCommander.Core.Services.SchemaVersioning.Current}", json);
    }

    [Fact]
    public void SaveLoad_DetectionAndStore_RoundTrip()
    {
        var svc = CreateService();
        GameEntry game = MakeGame("g1", "Steam") with
        {
            Detection = new DetectionInfo
            {
                Tier = IdentificationTier.Locked,
                TitleSource = "SteamApp",
                ManifestKind = "Acf",
                Signals = ["SteamStructural"],
                CandidateExes = ["game.exe"],
            },
            Store = new StoreInfo { SteamAppId = "400" },
        };
        svc.SetGamesForLibrary("Steam", [game]);
        svc.Save(svc.Load());

        GameEntry loaded = CreateService().Load().Games[0];
        Assert.Equal(IdentificationTier.Locked, loaded.Detection.Tier);
        Assert.Equal("Acf", loaded.Detection.ManifestKind);
        Assert.Contains("SteamStructural", loaded.Detection.Signals);
        Assert.Equal("400", loaded.Store.SteamAppId);
    }

    [Fact]
    public void SaveLoad_StoreIdentityProvenanceAndReportedState_RoundTrip()
    {
        var svc = CreateService();
        DateTimeOffset lastStoreSeen = DateTimeOffset.FromUnixTimeSeconds(1_760_000_000);
        GameEntry game = MakeGame("g1", "Steam") with
        {
            Detection = new DetectionInfo
            {
                Origin = DetectionOrigin.StoreManifest,
                ManifestKind = "Acf",
            },
            Store = new StoreInfo
            {
                StoreKey = "EaApp:ea-content-1",
                EaContentId = "ea-content-1",
                UbisoftGameId = "ubi-42",
                XboxPackageFamily = "Publisher.Game_8wekyb3d8bbwe",
                RockstarTitleId = "rockstar-7",
            },
            Install = new InstallInfo
            {
                State = InstallState.Installed,
                StoreReportedState = StoreReportedState.UpdateRequired,
                LastStoreSeenUtc = lastStoreSeen,
                UpdateAvailable = true,
            },
        };
        svc.SetGamesForLibrary("Steam", [game]);
        svc.Save(svc.Load());

        GameEntry loaded = CreateService().Load().Games.Single();
        Assert.Equal(DetectionOrigin.StoreManifest, loaded.Detection.Origin);
        Assert.Equal("Acf", loaded.Detection.ManifestKind);
        Assert.Equal("EaApp:ea-content-1", loaded.Store.StoreKey);
        Assert.Equal("ea-content-1", loaded.Store.EaContentId);
        Assert.Equal("ubi-42", loaded.Store.UbisoftGameId);
        Assert.Equal("Publisher.Game_8wekyb3d8bbwe", loaded.Store.XboxPackageFamily);
        Assert.Equal("rockstar-7", loaded.Store.RockstarTitleId);
        Assert.Equal(InstallState.Installed, loaded.Install.State);
        Assert.Equal(StoreReportedState.UpdateRequired, loaded.Install.StoreReportedState);
        Assert.Equal(lastStoreSeen, loaded.Install.LastStoreSeenUtc);
        Assert.True(loaded.Install.UpdateAvailable);
    }

    [Fact]
    public void Save_CreatesFile_OnDisk()
    {
        var svc = CreateService();
        svc.SetGamesForLibrary("Steam", []);
        svc.Save(svc.Load());

        string dbPath = Path.Combine(_tempDir, "games.json");
        Assert.True(File.Exists(dbPath), "Save should create the JSON file on disk");
    }

    [Fact]
    public void Save_WithCorruptFile_OverwritesCorrupt()
    {
        string dbPath = Path.Combine(_tempDir, "games.json");
        File.WriteAllText(dbPath, "not valid json {{{");

        var svc = CreateService();
        svc.Load(); // handles corrupt file gracefully (returns empty)
        svc.SetGamesForLibrary("Steam", [MakeGame("g1", "Steam")]);
        svc.Save(svc.Load());

        var svc2 = CreateService();
        var db2 = svc2.Load();
        Assert.Single(db2.Games);
        Assert.Equal("g1", db2.Games[0].Id);
    }

    [Fact]
    public void Load_WithCorruptFileAndBackup_RecoversPreviousCatalogue()
    {
        string dbPath = Path.Combine(_tempDir, "games.json");
        var svc = CreateService();
        svc.SetGamesForLibrary("Steam", [MakeGame("g1", "Steam")]);
        svc.SetGamesForLibrary("Steam", [MakeGame("g1", "Steam"), MakeGame("g2", "Steam")]);
        // .bak now holds the one-game catalogue.

        File.WriteAllText(dbPath, "not valid json {{{");

        var warnings = new List<string>();
        var db = new GamesDatabaseService(dbPath, warnings.Add).Load();

        GameEntry recovered = Assert.Single(db.Games);
        Assert.Equal("g1", recovered.Id);
        Assert.True(File.Exists(dbPath + ".corrupt"));
        Assert.Single(warnings);
    }

    [Fact]
    public void Save_ThenLoad_PreservesAllFields()
    {
        var svc = CreateService();
        var baseGame = MakeGame("g1", "EPIC", "MyFolder", "Epic Game");
        var game = baseGame with
        {
            Tags = [Tag.User("RPG"), Tag.User("Co-op")],
            Identity = baseGame.Identity with { GameSource = GameSourceKind.Epic },
            UserOverrides = new Dictionary<string, string> { ["DisplayName"] = "2026-07-26T14:30:00Z" },
            Store = new StoreInfo { EpicCatalogItemId = "abc123" },
        };
        svc.SetGamesForLibrary("EPIC", [game]);
        svc.Save(svc.Load());

        var svc2 = CreateService();
        var loaded = svc2.Load().Games.Single();

        Assert.Equal("EPIC", loaded.Library);
        Assert.Equal(@"C:\Games\MyFolder\game.exe", loaded.ExecutablePath);
        Assert.Equal(GameSourceKind.Epic, loaded.GameSource);
        Assert.Equal(["RPG", "Co-op"], loaded.Tags.Select(t => t.Name));
        Assert.Equal("abc123", loaded.Store.EpicCatalogItemId);
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
    public void GetGamesForLibrary_UnknownLibrary_ReturnsEmpty()
    {
        var svc = CreateService();
        svc.SetGamesForLibrary("Steam", [MakeGame("g1", "Steam")]);

        Assert.Empty(svc.GetGamesForLibrary(@"E:\Nonexistent"));
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
    public void RetagGame_ChangesSourceType()
    {
        var svc = CreateService();
        svc.SetGamesForLibrary(@"d:\games", [MakeGame("g1", @"d:\games")]);

        svc.RetagGame("g1", GameSourceKind.Epic);

        var games = svc.GetGamesForLibrary(@"d:\games");
        Assert.Single(games);
        Assert.Equal(GameSourceKind.Epic, games[0].GameSource);
        Assert.True(games[0].IsSourceOverridden);

        // Persisted via the same cache update.
        var svc2 = CreateService();
        Assert.Equal(GameSourceKind.Epic, svc2.Load().Games.Single(g => g.Id == "g1").GameSource);
    }

    [Fact]
    public void RetagGame_PreservesEntryId()
    {
        // P0 contract: re-anchoring (RetagGame) changes GameSource only — the
        // entry ID is identity and must never be regenerated.
        var svc = CreateService();
        string id = "g1";
        svc.SetGamesForLibrary(@"d:\games", [MakeGame(id, @"d:\games")]);

        svc.RetagGame(id, GameSourceKind.Epic);

        var games = svc.GetGamesForLibrary(@"d:\games");
        Assert.Single(games);
        Assert.Equal(id, games[0].Id);
        Assert.Equal(GameSourceKind.Epic, games[0].GameSource);
    }

    [Fact]
    public void EpicCatalogRescan_MatchedByPhysicalFolder_KeepsFolderScanId()
    {
        // P0 contract: ID = MD5("{physicalLibraryRoot|folder}"). An Epic catalog
        // rescan that matches the same physical folder emits the folder-scan ID
        // (matched by install location), so the merge keeps the entry singular
        // and sidecar/overrides stay attached — the anchor is metadata, not identity.
        var svc = CreateService();
        string physicalRoot = @"d:\games";
        string folder = "MyGame";
        string folderScanId = GameEntryId.ComputeId(physicalRoot, folder);

        svc.SetGamesForLibrary(@"d:\games", [MakeGame(folderScanId, @"d:\games", folder)]);

        var epicBase = MakeGame(folderScanId, @"d:\games", folder, "Epic Title Name");
        var epicRescan = epicBase with
        {
            Identity = epicBase.Identity with { GameSource = GameSourceKind.Epic },
        };
        svc.SetGamesForLibrary(@"d:\games", [epicRescan]);

        var entries = svc.GetGamesForLibrary(@"d:\games");
        Assert.Single(entries);
        Assert.Equal(folderScanId, entries[0].Id);
        Assert.Equal(GameSourceKind.Epic, entries[0].GameSource);
    }
}