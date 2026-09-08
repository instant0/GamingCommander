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
        new(id, library, folder, folder, display, GameSourceKind.Standalone, false,
            $@"C:\Games\{folder}\game.exe", "", "", "",
            DateTimeOffset.Now, DateTimeOffset.Now, [], [], []);

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
    public void Save_ThenLoad_PreservesAllFields()
    {
        var svc = CreateService();
        var game = MakeGame("g1", "EPIC", "MyFolder", "Epic Game") with
        {
            Tags = ["RPG", "Co-op"],
            GameSource = GameSourceKind.Epic,
            UserOverrides = new Dictionary<string, string> { ["DisplayName"] = "2026-07-26T14:30:00Z" },
            PlatformMetadata = new Dictionary<string, string> { ["CatalogItemId"] = "abc123" },
        };
        svc.SetGamesForLibrary("EPIC", [game]);
        svc.Save(svc.Load());

        var svc2 = CreateService();
        var loaded = svc2.Load().Games.Single();

        Assert.Equal("EPIC", loaded.Library);
        Assert.Equal(@"C:\Games\MyFolder\game.exe", loaded.ExecutablePath);
        Assert.Equal(GameSourceKind.Epic, loaded.GameSource);
        Assert.Equal(["RPG", "Co-op"], loaded.Tags);
        Assert.Equal("abc123", loaded.PlatformMetadata["CatalogItemId"]);
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
        var game = MakeGame("g1", "Steam") with
        {
            DisplayName = "My Custom Name",
            UserOverrides = new Dictionary<string, string> { ["DisplayName"] = "2026-07-26T14:30:00Z" },
            Tags = ["RPG", "Co-op"],
        };
        svc.SetGamesForLibrary("Steam", [game]);

        // Rescan supplies a fresh auto-detected entry with the same ID.
        var scannedGame = MakeGame("g1", "Steam") with { DisplayName = "Auto Name", Tags = [] };
        svc.SetGamesForLibrary("Steam", [scannedGame]);

        var games = svc.GetGamesForLibrary("Steam");
        Assert.Single(games);
        Assert.Equal("My Custom Name", games[0].DisplayName);   // user override preserved
        Assert.Equal(2, games[0].Tags.Count);                   // user tags preserved
        Assert.Equal("RPG", games[0].Tags[0]);
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

        var epicRescan = MakeGame(folderScanId, @"d:\games", folder, "Epic Title Name")
            with { GameSource = GameSourceKind.Epic };
        svc.SetGamesForLibrary(@"d:\games", [epicRescan]);

        var entries = svc.GetGamesForLibrary(@"d:\games");
        Assert.Single(entries);
        Assert.Equal(folderScanId, entries[0].Id);
        Assert.Equal(GameSourceKind.Epic, entries[0].GameSource);
    }
}