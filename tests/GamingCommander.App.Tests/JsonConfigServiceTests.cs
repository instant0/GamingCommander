using GamingCommander.App.Services;
using GamingCommander.Core.Models;
using Xunit;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for JsonConfigService — first-run detection and config persistence.
/// </summary>
public sealed class JsonConfigServiceTests : IDisposable
{
    private readonly string _tempDir;

    public JsonConfigServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ConfigTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private JsonConfigService CreateService(string? fileName = null)
    {
        return new JsonConfigService(Path.Combine(_tempDir, fileName ?? "settings.json"));
    }

    // ════════════════════════════════════════════════════════════════
    //  First-run detection
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Load_WithMissingFile_ReturnsIsFirstRunTrue()
    {
        var svc = CreateService("nonexistent_settings.json");
        var config = svc.Load();

        Assert.True(config.IsFirstRun);
        Assert.Empty(config.LibraryRoots);
    }

    [Fact]
    public void Load_AfterSave_ReturnsIsFirstRunFalse()
    {
        var svc = CreateService();

        // First load — file doesn't exist yet
        var config = svc.Load();
        Assert.True(config.IsFirstRun);

        // Save with a root
        config = config with
        {
            LibraryRoots = new List<LibraryRoot> { new(RootPath: @"D:\Games", DefaultType: GameSourceKind.Standalone) },
        };
        svc.Save(config);

        // Reload — file now exists
        var reloaded = svc.Load();
        Assert.False(reloaded.IsFirstRun);
        Assert.Single(reloaded.LibraryRoots);
        Assert.Equal(@"D:\Games", reloaded.LibraryRoots[0].RootPath);
    }

    // ════════════════════════════════════════════════════════════════
    //  Missing games database
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void GamesDatabase_Load_WithMissingFile_ReturnsEmptyDatabase()
    {
        var svc = new GamesDatabaseService(Path.Combine(_tempDir, "nonexistent_games.json"));
        var db = svc.Load();

        Assert.NotNull(db);
        Assert.Empty(db.Roots);
    }
}
