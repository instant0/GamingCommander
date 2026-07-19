using GamingCommander.App.Services;
using GamingCommander.Core;
using GamingCommander.Core.Models;
using Xunit;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for GamesDatabaseService — JSON persistence, in-memory caching, and CRUD operations.
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

    private static GameEntry MakeGame(string id, string folder = "GameFolder", string display = "Test Game") =>
        new(id, folder, display, GameSourceKind.Standalone, false,
            $@"C:\Games\{folder}\game.exe", "", "", "",
            DateTimeOffset.Now, DateTimeOffset.Now, []);

    // ════════════════════════════════════════════════════════════════
    //  Load/Save
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Load_WithNoFile_ReturnsEmptyDatabase()
    {
        var svc = CreateService();
        var db = svc.Load();

        Assert.NotNull(db);
        Assert.Empty(db.Roots);
    }

    [Fact]
    public void Load_WithValidFile_ReturnsPersistedData()
    {
        var svc = CreateService();
        svc.AddRoot(@"D:\Games", GameSourceKind.Standalone, [MakeGame("g1")]);
        svc.Save(svc.Load());

        // Create new service instance (fresh cache) to test disk read
        var svc2 = CreateService();
        var db = svc2.Load();

        Assert.Single(db.Roots);
        Assert.Single(db.Roots[0].Games);
        Assert.Equal("g1", db.Roots[0].Games[0].Id);
    }

    [Fact]
    public void Save_CreatesFile_OnDisk()
    {
        var svc = CreateService();
        svc.AddRoot(@"D:\Games", GameSourceKind.Standalone, []);
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
        var db = svc.Load(); // Should handle corrupt file gracefully
        svc.AddRoot(@"D:\Games", GameSourceKind.Standalone, [MakeGame("g1")]);

        // Re-read: should have valid data now
        var svc2 = CreateService();
        var db2 = svc2.Load();
        Assert.Single(db2.Roots);
    }

    // ════════════════════════════════════════════════════════════════
    //  CRUD Operations
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void AddRoot_AddsToDatabase()
    {
        var svc = CreateService();
        svc.AddRoot(@"D:\Games", GameSourceKind.Standalone,
            [MakeGame("g1", "Game1", "Game One"), MakeGame("g2", "Game2", "Game Two")]);

        var games = svc.GetGamesForRoot(@"D:\Games");
        Assert.Equal(2, games.Count);
    }

    [Fact]
    public void AddRoot_DuplicateRoot_Ignored()
    {
        var svc = CreateService();
        svc.AddRoot(@"D:\Games", GameSourceKind.Standalone, [MakeGame("g1")]);
        svc.AddRoot(@"D:\Games", GameSourceKind.Standalone, [MakeGame("g2")]); // duplicate

        var games = svc.GetGamesForRoot(@"D:\Games");
        Assert.Single(games); // Only first game present
    }

    [Fact]
    public void RemoveRoot_RemovesFromDatabase()
    {
        var svc = CreateService();
        svc.AddRoot(@"D:\Games", GameSourceKind.Standalone, [MakeGame("g1")]);
        svc.RemoveRoot(@"D:\Games");

        var db = svc.Load();
        Assert.Empty(db.Roots);
    }

    [Fact]
    public void GetGamesForRoot_ReturnsCorrectEntries()
    {
        var svc = CreateService();
        svc.AddRoot(@"D:\Games", GameSourceKind.Standalone,
            [MakeGame("g1"), MakeGame("g2"), MakeGame("g3")]);

        var games = svc.GetGamesForRoot(@"D:\Games");
        Assert.Equal(3, games.Count);
    }

    [Fact]
    public void GetGamesForRoot_UnknownRoot_ReturnsEmpty()
    {
        var svc = CreateService();
        svc.AddRoot(@"D:\Games", GameSourceKind.Standalone, [MakeGame("g1")]);

        var games = svc.GetGamesForRoot(@"E:\Nonexistent");
        Assert.Empty(games);
    }

    [Fact]
    public void UpdateGameEntry_UpdatesFields()
    {
        var svc = CreateService();
        svc.AddRoot(@"D:\Games", GameSourceKind.Standalone, [MakeGame("g1")]);

        var updated = MakeGame("g1", "Game1", "Renamed Game");
        svc.UpdateGameEntry(@"D:\Games", updated);

        var games = svc.GetGamesForRoot(@"D:\Games");
        Assert.Equal("Renamed Game", games[0].DisplayName);
    }

    [Fact]
    public void DeleteGameEntry_RemovesEntry()
    {
        var svc = CreateService();
        svc.AddRoot(@"D:\Games", GameSourceKind.Standalone, [MakeGame("g1"), MakeGame("g2")]);

        svc.DeleteGameEntry(@"D:\Games", "g1");

        var games = svc.GetGamesForRoot(@"D:\Games");
        Assert.Single(games);
        Assert.Equal("g2", games[0].Id);
    }

    [Fact]
    public void RetagGame_ChangesSourceKind()
    {
        var svc = CreateService();
        svc.AddRoot(@"D:\Games", GameSourceKind.Standalone, [MakeGame("g1")]);

        svc.RetagGame(@"D:\Games", "g1", GameSourceKind.Steam);

        var games = svc.GetGamesForRoot(@"D:\Games");
        Assert.Equal(GameSourceKind.Steam, games[0].GameSource);
        Assert.True(games[0].IsSourceOverridden); // Steam != Standalone default
    }

    // ════════════════════════════════════════════════════════════════
    //  Caching
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Load_CachesResult()
    {
        var svc = CreateService();
        svc.AddRoot(@"D:\Games", GameSourceKind.Standalone, [MakeGame("g1")]);

        var db1 = svc.Load();
        var db2 = svc.Load();

        Assert.Same(db1, db2);
    }

    [Fact]
    public void Save_UpdatesCache()
    {
        var svc = CreateService();
        var db = svc.Load();
        Assert.Empty(db.Roots);

        svc.AddRoot(@"D:\Games", GameSourceKind.Standalone, [MakeGame("g1")]);
        var dbAfter = svc.Load();

        Assert.Single(dbAfter.Roots);
        // After AddRoot→Save, cache is updated (new instance with data)
        Assert.NotSame(db, dbAfter);
        Assert.Empty(db.Roots); // original reference unchanged
    }

    // ════════════════════════════════════════════════════════════════
    //  Edge Cases
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void RescanRoot_ReplacesAllGames()
    {
        var svc = CreateService();
        svc.AddRoot(@"D:\Games", GameSourceKind.Standalone,
            [MakeGame("g1"), MakeGame("g2"), MakeGame("g3"), MakeGame("g4"), MakeGame("g5")]);

        svc.RescanRoot(@"D:\Games", [MakeGame("new1"), MakeGame("new2")]);

        var games = svc.GetGamesForRoot(@"D:\Games");
        Assert.Equal(2, games.Count);
    }

    [Fact]
    public void MultipleRoots_IndependentCRUD()
    {
        var svc = CreateService();
        svc.AddRoot(@"D:\Games", GameSourceKind.Standalone, [MakeGame("g1")]);
        svc.AddRoot(@"E:\Steam", GameSourceKind.Steam, [MakeGame("g2")]);

        svc.DeleteGameEntry(@"D:\Games", "g1");

        // E:\Steam should be unaffected
        var steamGames = svc.GetGamesForRoot(@"E:\Steam");
        Assert.Single(steamGames);
        Assert.Equal("g2", steamGames[0].Id);

        // D:\Games should be empty
        var dGames = svc.GetGamesForRoot(@"D:\Games");
        Assert.Empty(dGames);
    }
}
