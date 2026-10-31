using GamingCommander.App.Services;
using GamingCommander.Core.Models;
using Xunit;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for JsonConfigService — first-run detection, hidden folders,
/// and general config persistence.
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
        Assert.Empty(config.HiddenFolders);
    }

    [Fact]
    public void Load_AfterSave_ReturnsIsFirstRunFalse()
    {
        var svc = CreateService();

        // First load — file doesn't exist yet
        var config = svc.Load();
        Assert.True(config.IsFirstRun);

        // Save with settings and mark first run complete
        config = config with
        {
            HiddenFolders = ["Wine", "SystemVolumeInformation"],
            IsFirstRun = false,
            LastSeenVersion = "0.4.0",
            EnableOnlineMetadata = true,
        };
        svc.Save(config);

        // Reload — file now exists and values round-trip
        var reloaded = svc.Load();
        Assert.False(reloaded.IsFirstRun);
        Assert.Equal(["Wine", "SystemVolumeInformation"], reloaded.HiddenFolders);
        Assert.Equal("0.4.0", reloaded.LastSeenVersion);
        Assert.True(reloaded.EnableOnlineMetadata);
    }

    [Fact]
    public void Load_WithMissingFile_DefaultsScanDepthTo3()
    {
        var svc = CreateService("nonexistent_settings.json");
        Assert.Equal(3, svc.Load().ScanDepth);
    }

    [Fact]
    public void Save_ScanDepth_RoundTrips()
    {
        var svc = CreateService();
        var config = svc.Load() with { ScanDepth = 4, IsFirstRun = false };
        svc.Save(config);

        Assert.Equal(4, svc.Load().ScanDepth);
    }

    // ════════════════════════════════════════════════════════════════
    //  Recovery (shared JSON store)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Load_WithCorruptFileAndBackup_RecoversPreviousSettings_AndReports()
    {
        var svc = CreateService();
        svc.Save(new AppConfig(["Wine"], IsFirstRun: false));
        svc.Save(new AppConfig(["Temp"], IsFirstRun: false)); // .bak holds the first save

        string path = Path.Combine(_tempDir, "settings.json");
        File.WriteAllText(path, "not valid json {{{");

        var warnings = new List<string>();
        var recovered = new JsonConfigService(path, warnings.Add).Load();

        Assert.Equal(["Wine"], recovered.HiddenFolders);
        Assert.False(recovered.IsFirstRun);
        Assert.True(File.Exists(path + ".corrupt"));
        Assert.Single(warnings);
    }

    [Fact]
    public void Load_WithCorruptFileAndNoBackup_ReturnsDefaultsNotFirstRun()
    {
        File.WriteAllText(Path.Combine(_tempDir, "settings.json"), "not valid json {{{");

        AppConfig config = CreateService().Load();

        Assert.False(config.IsFirstRun);
        Assert.Empty(config.HiddenFolders);
    }
}