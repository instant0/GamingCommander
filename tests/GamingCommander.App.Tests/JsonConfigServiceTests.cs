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
    public void Load_WithMissingFile_ReturnsFirstRunDefaults()
    {
        var svc = CreateService("nonexistent_settings.json");
        var config = svc.Load();

        Assert.True(config.IsFirstRun);
        Assert.Empty(config.HiddenFolders);
        Assert.Equal(3, config.ScanDepth);
    }

    [Fact]
    public void Load_AfterSave_ReturnsIsFirstRunFalseAndRoundTrips()
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
            ScanDepth = 4,
        };
        svc.Save(config);

        // Reload — file now exists and values round-trip
        var reloaded = svc.Load();
        Assert.False(reloaded.IsFirstRun);
        Assert.Equal(["Wine", "SystemVolumeInformation"], reloaded.HiddenFolders);
        Assert.Equal("0.4.0", reloaded.LastSeenVersion);
        Assert.True(reloaded.EnableOnlineMetadata);
        Assert.Equal(4, reloaded.ScanDepth);
    }

    // ════════════════════════════════════════════════════════════════
    //  Launcher discovery setting
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Load_WithMissingFile_LauncherDiscoveryIsUnset()
    {
        AppConfig config = CreateService("nonexistent_settings.json").Load();

        Assert.Equal(LauncherDiscoverySetting.Unset, config.LauncherDiscovery);
        Assert.Empty(config.SelectedLaunchers);
    }

    [Fact]
    public void Load_ExistingFileWithoutField_DefaultsToAutomatic()
    {
        File.WriteAllText(
            Path.Combine(_tempDir, "settings.json"),
            """{ "HiddenFolders": [], "IsFirstRun": false }""");

        AppConfig config = CreateService().Load();

        Assert.Equal(LauncherDiscoverySetting.Automatic, config.LauncherDiscovery);
        Assert.Empty(config.SelectedLaunchers);
    }

    [Fact]
    public void Save_RoundTripsLauncherDiscoveryAndSelection()
    {
        var svc = CreateService();
        AppConfig config = svc.Load() with
        {
            IsFirstRun = false,
            LauncherDiscovery = LauncherDiscoverySetting.Selected,
            SelectedLaunchers = [GameSourceKind.Steam, GameSourceKind.Gog],
        };

        svc.Save(config);
        AppConfig reloaded = svc.Load();

        Assert.Equal(LauncherDiscoverySetting.Selected, reloaded.LauncherDiscovery);
        Assert.Equal([GameSourceKind.Steam, GameSourceKind.Gog], reloaded.SelectedLaunchers);
    }

    // ════════════════════════════════════════════════════════════════
    //  Recovery (backup recovery itself is owned by JsonDocumentStoreTests)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Load_WithCorruptFileAndNoBackup_ReturnsDefaultsNotFirstRun()
    {
        File.WriteAllText(Path.Combine(_tempDir, "settings.json"), "not valid json {{{");

        AppConfig config = CreateService().Load();

        Assert.False(config.IsFirstRun);
        Assert.Empty(config.HiddenFolders);
    }
}
