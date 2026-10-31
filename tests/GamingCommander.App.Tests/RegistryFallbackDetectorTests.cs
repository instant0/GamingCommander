using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

public sealed class RegistryFallbackDetectorTests : IDisposable
{
    private readonly string _tempDir;

    public RegistryFallbackDetectorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "gc_registry_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private const string EaBase = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games";
    private const string UbisoftBase = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs";
    private const string GogBase = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\Games";
    private const string RockstarBase = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games";

    // ════════════════════════════════════════════════════════════════
    //  Per-store registry key shapes (exact path match)
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(EaBase, "Dead Space 3", "Install Dir", "Dead Space 3", GameSourceKind.EaApp)]
    [InlineData(UbisoftBase, "11903", "InstallDir", "Ghost Recon Breakpoint", GameSourceKind.UbisoftConnect)]
    [InlineData(GogBase, "1201963702", "path", "Blasphemous 2", GameSourceKind.Gog)]
    [InlineData(RockstarBase, "Grand Theft Auto V Enhanced", "InstallFolder", "Grand Theft Auto V Enhanced", GameSourceKind.Rockstar)]
    public void Detect_PerStoreKey_ReturnsStore(
        string keyBase, string keyName, string valueName, string folderName, GameSourceKind expected)
    {
        string gameDir = Path.Combine(_tempDir, "Games", folderName);
        Directory.CreateDirectory(gameDir);
        string registry = BuildKey(keyBase + @"\" + keyName, valueName, gameDir);

        var result = new RegistryFallbackDetector(new MockRegistryReader(registry))
            .DetectType(new DirectoryInfo(gameDir));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Detect_RockstarGame_SkipsLauncherSubkey()
    {
        string gameDir = Path.Combine(_tempDir, "rockstar", "Games", "GTA V");
        Directory.CreateDirectory(gameDir);

        string registry = BuildKey(RockstarBase + @"\Launcher", "InstallFolder",
                @"C:\Program Files\Rockstar Games\Launcher\")
            + BuildKey(RockstarBase + @"\GTA V", "InstallFolder", gameDir);

        var result = new RegistryFallbackDetector(new MockRegistryReader(registry))
            .DetectType(new DirectoryInfo(gameDir));

        Assert.Equal(GameSourceKind.Rockstar, result);
    }

    [Fact]
    public void Detect_UbisoftGame_MultipleGameIds_MatchesOneOfThem()
    {
        string gameDir = Path.Combine(_tempDir, "ubi", "Games", "The Division 2");
        Directory.CreateDirectory(gameDir);

        string registry =
            BuildKey(UbisoftBase + @"\11903", "InstallDir",
                Path.Combine(_tempDir, "ubi", "Games", "Ghost Recon Breakpoint"))
            + BuildKey(UbisoftBase + @"\4932", "InstallDir", gameDir);

        var result = new RegistryFallbackDetector(new MockRegistryReader(registry))
            .DetectType(new DirectoryInfo(gameDir));

        Assert.Equal(GameSourceKind.UbisoftConnect, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Tier 2: moved games (name match per store)
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(EaBase, "Dead Space 3", "Install Dir", "Dead Space 3", GameSourceKind.EaApp)]
    [InlineData(GogBase, "1423041000", "path", "Celeste", GameSourceKind.Gog)]
    public void Detect_MovedGame_NameMatch(
        string keyBase, string keyName, string valueName, string folderName, GameSourceKind expected)
    {
        // Registry points at a stale path; the actual folder lives elsewhere with the same name.
        string actualDir = Path.Combine(_tempDir, "Moved", folderName);
        Directory.CreateDirectory(actualDir);

        string registry = BuildKey(keyBase + @"\" + keyName, valueName,
            Path.Combine(@"Q:\Stale", folderName));

        var result = new RegistryFallbackDetector(new MockRegistryReader(registry))
            .DetectType(new DirectoryInfo(actualDir));

        Assert.Equal(expected, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Negative cases
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(false)]
    public void Detect_NoMatch_ReturnsUnknown(bool emptyRegistry)
    {
        string gameDir = Path.Combine(_tempDir, "Unrelated Game");
        Directory.CreateDirectory(gameDir);

        string registry = emptyRegistry
            ? ""
            : BuildKey(EaBase + @"\Dead Space 3", "Install Dir", @"C:\EA Games\Dead Space 3");

        var result = new RegistryFallbackDetector(new MockRegistryReader(registry))
            .DetectType(new DirectoryInfo(gameDir));

        Assert.Equal(GameSourceKind.Unknown, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Path formatting (via the public detector, not the normalize delegate)
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(false)]
    public void Detect_PathFormatting_MatchesCaseAndSeparators(bool useForwardSlashes)
    {
        string gameDir = Path.Combine(_tempDir, "EA", "Games", "Dead Space 3");
        Directory.CreateDirectory(gameDir);

        string registryPath = useForwardSlashes
            ? gameDir.Replace('\\', '/')
            : gameDir.ToLowerInvariant();
        string registry = BuildKey(EaBase + @"\Dead Space 3", "Install Dir", registryPath);

        var result = new RegistryFallbackDetector(new MockRegistryReader(registry))
            .DetectType(new DirectoryInfo(gameDir));

        Assert.Equal(GameSourceKind.EaApp, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════

    /// <summary>Builds one .reg key block with a single string value.</summary>
    private static string BuildKey(string keyPath, string valueName, string value) =>
        $"""
         Windows Registry Editor Version 5.00

         [{keyPath}]
         "{valueName}"=str(2):"{value.Replace("\\", "\\\\")}"

         """;
}
