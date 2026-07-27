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

    // ── EA Detection ──────────────────────────────────────────────

    [Fact]
    public void Detect_EaGame_MatchesPerGameKey()
    {
        string eaBase = Path.Combine(_tempDir, "ea", "Games");
        string gameDir = Path.Combine(eaBase, "Dead Space 3");
        Directory.CreateDirectory(gameDir);

        string regContent = $"""
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games\Dead Space 3]
            "Install Dir"=str(2):"{eaBase.Replace("\\", "\\\\")}\\Dead Space 3"
            "DisplayName"=str(2):"Dead Space 3"
            """;

        var registry = new MockRegistryReader(regContent);
        var detector = new RegistryFallbackDetector(registry);

        var result = detector.DetectType(new DirectoryInfo(gameDir));

        Assert.Equal(GameSourceKind.EaApp, result);
    }

    [Fact]
    public void Detect_EaGame_PublisherSubfolder()
    {
        string eaBase = Path.Combine(_tempDir, "ea", "Games");
        string gameDir = Path.Combine(eaBase, "Mass Effect 3");
        Directory.CreateDirectory(gameDir);

        string regContent = $"""
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games\Mass Effect 3]
            "Install Dir"=str(2):"{eaBase.Replace("\\", "\\\\")}\\Mass Effect 3"
            "DisplayName"=str(2):"Mass Effect 3"
            """;

        var registry = new MockRegistryReader(regContent);
        var detector = new RegistryFallbackDetector(registry);

        var result = detector.DetectType(new DirectoryInfo(gameDir));

        Assert.Equal(GameSourceKind.EaApp, result);
    }

    // ── Ubisoft Detection ─────────────────────────────────────────

    [Fact]
    public void Detect_UbisoftGame_MatchesPerGameKey()
    {
        string ubiBase = Path.Combine(_tempDir, "ubi", "Games");
        string gameDir = Path.Combine(ubiBase, "Ghost Recon Breakpoint");
        Directory.CreateDirectory(gameDir);

        string regContent = $"""
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs\11903]
            "InstallDir"=str(2):"{ubiBase.Replace("\\", "\\\\")}\\Ghost Recon Breakpoint"
            "Language"=str(2):"en_US"
            """;

        var registry = new MockRegistryReader(regContent);
        var detector = new RegistryFallbackDetector(registry);

        var result = detector.DetectType(new DirectoryInfo(gameDir));

        Assert.Equal(GameSourceKind.UbisoftConnect, result);
    }

    [Fact]
    public void Detect_UbisoftGame_MultipleGameIds()
    {
        string ubiBase = Path.Combine(_tempDir, "ubi", "Games");
        string gameDir = Path.Combine(ubiBase, "The Division 2");
        Directory.CreateDirectory(gameDir);

        string regContent = $"""
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs\11903]
            "InstallDir"=str(2):"{ubiBase.Replace("\\", "\\\\")}\\Ghost Recon Breakpoint"

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs\4932]
            "InstallDir"=str(2):"{ubiBase.Replace("\\", "\\\\")}\\The Division 2"
            """;

        var registry = new MockRegistryReader(regContent);
        var detector = new RegistryFallbackDetector(registry);

        var result = detector.DetectType(new DirectoryInfo(gameDir));

        Assert.Equal(GameSourceKind.UbisoftConnect, result);
    }

    // ── GOG Detection ─────────────────────────────────────────────

    [Fact]
    public void Detect_GogGame_MatchesPerGameKey()
    {
        string gogBase = Path.Combine(_tempDir, "gog", "Games");
        string gameDir = Path.Combine(gogBase, "Blasphemous 2");
        Directory.CreateDirectory(gameDir);

        string regContent = $"""
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\Games\1201963702]
            "path"=str(2):"{gogBase.Replace("\\", "\\\\")}\\Blasphemous 2"
            "gameName"=str(2):"Blasphemous 2"
            "gameID"=str(2):"1201963702"
            """;

        var registry = new MockRegistryReader(regContent);
        var detector = new RegistryFallbackDetector(registry);

        var result = detector.DetectType(new DirectoryInfo(gameDir));

        Assert.Equal(GameSourceKind.Gog, result);
    }

    // ── Rockstar Detection ────────────────────────────────────────

    [Fact]
    public void Detect_RockstarGame_MatchesPerGameKey()
    {
        string rockstarBase = Path.Combine(_tempDir, "rockstar", "Games");
        string gameDir = Path.Combine(rockstarBase, "Grand Theft Auto V Enhanced");
        Directory.CreateDirectory(gameDir);

        string regContent = $"""
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Grand Theft Auto V Enhanced]
            "InstallFolder"=str(2):"{rockstarBase.Replace("\\", "\\\\")}\\Grand Theft Auto V Enhanced"
            """;

        var registry = new MockRegistryReader(regContent);
        var detector = new RegistryFallbackDetector(registry);

        var result = detector.DetectType(new DirectoryInfo(gameDir));

        Assert.Equal(GameSourceKind.Rockstar, result);
    }

    [Fact]
    public void Detect_RockstarGame_SkipsLauncherSubkey()
    {
        string rockstarBase = Path.Combine(_tempDir, "rockstar", "Games");
        string gameDir = Path.Combine(rockstarBase, "GTA V");
        Directory.CreateDirectory(gameDir);

        string regContent = $"""
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher]
            "InstallFolder"=str(2):"C:\\Program Files\\Rockstar Games\\Launcher\\"

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\GTA V]
            "InstallFolder"=str(2):"{rockstarBase.Replace("\\", "\\\\")}\\GTA V"
            """;

        var registry = new MockRegistryReader(regContent);
        var detector = new RegistryFallbackDetector(registry);

        var result = detector.DetectType(new DirectoryInfo(gameDir));

        Assert.Equal(GameSourceKind.Rockstar, result);
    }

    // ── Negative Cases ────────────────────────────────────────────

    [Fact]
    public void Detect_GameNotInRegistry_ReturnsUnknown()
    {
        string gameDir = Path.Combine(_tempDir, "RandomGame");
        Directory.CreateDirectory(gameDir);

        string regContent = """
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games\Dead Space 3]
            "Install Dir"=str(2):"C:\\EA Games\\Dead Space 3"
            """;

        var registry = new MockRegistryReader(regContent);
        var detector = new RegistryFallbackDetector(registry);

        var result = detector.DetectType(new DirectoryInfo(gameDir));

        Assert.Equal(GameSourceKind.Unknown, result);
    }

    [Fact]
    public void Detect_EmptyRegistry_ReturnsUnknown()
    {
        string gameDir = Path.Combine(_tempDir, "SomeGame");
        Directory.CreateDirectory(gameDir);

        var registry = new MockRegistryReader("");
        var detector = new RegistryFallbackDetector(registry);

        var result = detector.DetectType(new DirectoryInfo(gameDir));

        Assert.Equal(GameSourceKind.Unknown, result);
    }

    [Fact]
    public void Detect_CaseInsensitivePathMatch()
    {
        string eaBase = Path.Combine(_tempDir, "EA", "Games");
        string gameDir = Path.Combine(eaBase, "dead space 3");
        Directory.CreateDirectory(gameDir);

        string regContent = $"""
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games\Dead Space 3]
            "Install Dir"=str(2):"{eaBase.Replace("\\", "\\\\")}\\Dead Space 3"
            """;

        var registry = new MockRegistryReader(regContent);
        var detector = new RegistryFallbackDetector(registry);

        // Normalized paths should match case-insensitively
        var result = detector.DetectType(new DirectoryInfo(gameDir));

        Assert.Equal(GameSourceKind.EaApp, result);
    }

    [Fact]
    public void Detect_ForwardSlashPath()
    {
        string eaBase = Path.Combine(_tempDir, "ea", "Games");
        string gameDir = Path.Combine(eaBase, "Dead Space 3");
        Directory.CreateDirectory(gameDir);

        // Registry value uses forward slashes
        string forwardPath = $"{eaBase}/Dead Space 3".Replace('\\', '/');
        string regContent = $"""
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games\Dead Space 3]
            "Install Dir"=str(2):"{forwardPath}"
            """;

        var registry = new MockRegistryReader(regContent);
        var detector = new RegistryFallbackDetector(registry);

        var result = detector.DetectType(new DirectoryInfo(gameDir));

        Assert.Equal(GameSourceKind.EaApp, result);
    }

    // ── NormalizePath ─────────────────────────────────────────────

    [Fact]
    public void NormalizePath_ConvertsForwardSlashes()
    {
        string result = RegistryFallbackDetector.NormalizePath("C:/Games/My Game");
        Assert.Equal(@"C:\Games\My Game", result);
    }

    [Fact]
    public void NormalizePath_TrimsTrailingSeparator()
    {
        string result = RegistryFallbackDetector.NormalizePath(@"C:\Games\My Game\");
        Assert.Equal(@"C:\Games\My Game", result);
    }

    // ── Tier 2: Fuzzy Name Matching (moved games) ────────────────

    [Fact]
    public void Detect_EaGame_MovedToDifferentDrive_NameMatch()
    {
        // Registry says Q:\games\Dead Space 3, actual is E:\Games\Dead Space 3
        // Path doesn't match, but directory name "Dead Space 3" matches registry key name
        string actualDir = Path.Combine(_tempDir, "E", "Games", "Dead Space 3");
        Directory.CreateDirectory(actualDir);

        string regContent = """
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games\Dead Space 3]
            "Install Dir"=str(2):"Q:\\games\\Dead Space 3"
            "DisplayName"=str(2):"Dead Space 3"
            """;

        var registry = new MockRegistryReader(regContent);
        var detector = new RegistryFallbackDetector(registry);

        // Exact path match fails (Q: vs E:), but name match succeeds
        var result = detector.DetectType(new DirectoryInfo(actualDir));

        Assert.Equal(GameSourceKind.EaApp, result);
    }

    [Fact]
    public void Detect_UbisoftGame_MovedToDifferentFolder_NameMatch()
    {
        // Registry says C:\Ubisoft\games\Ghost Recon, actual is D:\MyGames\Ghost Recon
        string actualDir = Path.Combine(_tempDir, "D", "MyGames", "Ghost Recon Breakpoint");
        Directory.CreateDirectory(actualDir);

        string regContent = """
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs\11903]
            "InstallDir"=str(2):"C:\\Ubisoft\\games\\Ghost Recon Breakpoint"
            """;

        var registry = new MockRegistryReader(regContent);
        var detector = new RegistryFallbackDetector(registry);

        var result = detector.DetectType(new DirectoryInfo(actualDir));

        Assert.Equal(GameSourceKind.UbisoftConnect, result);
    }

    [Fact]
    public void Detect_GogGame_MovedToDifferentDrive_NameMatch()
    {
        string actualDir = Path.Combine(_tempDir, "F", "Games", "Celeste");
        Directory.CreateDirectory(actualDir);

        string regContent = """
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\Games\1423041000]
            "path"=str(2):"C:\\GOG Games\\Celeste"
            "gameName"=str(2):"Celeste"
            """;

        var registry = new MockRegistryReader(regContent);
        var detector = new RegistryFallbackDetector(registry);

        var result = detector.DetectType(new DirectoryInfo(actualDir));

        Assert.Equal(GameSourceKind.Gog, result);
    }

    [Fact]
    public void Detect_RockstarGame_MovedToDifferentPath_NameMatch()
    {
        string actualDir = Path.Combine(_tempDir, "D", "Games", "Grand Theft Auto V Enhanced");
        Directory.CreateDirectory(actualDir);

        string regContent = """
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Grand Theft Auto V Enhanced]
            "InstallFolder"=str(2):"E:\\Games\\Grand Theft Auto V Enhanced"
            """;

        var registry = new MockRegistryReader(regContent);
        var detector = new RegistryFallbackDetector(registry);

        var result = detector.DetectType(new DirectoryInfo(actualDir));

        Assert.Equal(GameSourceKind.Rockstar, result);
    }

    [Fact]
    public void Detect_NameMatch_DoesNotMatchUnrelatedGame()
    {
        // Registry has "Dead Space 3", scanning "Need for Speed"
        string actualDir = Path.Combine(_tempDir, "E", "Games", "Need for Speed");
        Directory.CreateDirectory(actualDir);

        string regContent = """
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games\Dead Space 3]
            "Install Dir"=str(2):"Q:\\games\\Dead Space 3"
            """;

        var registry = new MockRegistryReader(regContent);
        var detector = new RegistryFallbackDetector(registry);

        var result = detector.DetectType(new DirectoryInfo(actualDir));

        Assert.Equal(GameSourceKind.Unknown, result);
    }

    [Fact]
    public void Detect_ExactPathMatch_TakesPrecedenceOverNameMatch()
    {
        // Both exact path and name match should return the same result
        // But exact match is checked first (performance — early return)
        string gameDir = Path.Combine(_tempDir, "ea", "Games", "Dead Space 3");
        Directory.CreateDirectory(gameDir);

        string regContent = $"""
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games\Dead Space 3]
            "Install Dir"=str(2):"{gameDir.Replace("\\", "\\\\")}"
            """;

        var registry = new MockRegistryReader(regContent);
        var detector = new RegistryFallbackDetector(registry);

        var result = detector.DetectType(new DirectoryInfo(gameDir));

        Assert.Equal(GameSourceKind.EaApp, result);
    }
}
