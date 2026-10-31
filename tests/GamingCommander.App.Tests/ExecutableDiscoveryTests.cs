using GamingCommander.App.Services;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for ExecutableDiscovery.FindExecutablesDeep — verifies deep search
/// across root, children, UE Binaries/{platform} paths, child/bin/, and recursive fallback.
/// </summary>
public sealed class ExecutableDiscoveryTests : IDisposable
{
    private readonly string _tempDir;
    private static readonly IReadOnlySet<string> EmptyNoiseDirs = new HashSet<string>();
    private static readonly IReadOnlyList<string> EmptyNoiseExes = Array.Empty<string>();

    public ExecutableDiscoveryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ExeDiscTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    // ════════════════════════════════════════════════════════════════
    //  UE platform probes (Win64, Win32, WinGDK, Steam)
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("Binaries/Win64")]
    public void FindExecutablesDeep_PlatformProbe_FindsGame(string relativeDir)
    {
        // Game/Binaries/{platform}/Game.exe
        string gameDir = CreateDir("MyGame");
        string exeDir = CreateDir(("MyGame/" + relativeDir).Split('/'));
        CreateExe(exeDir, "MyGame.exe");

        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, EmptyNoiseDirs);

        var result = Assert.Single(results);
        Assert.Contains("MyGame.exe", result);
    }

    // ════════════════════════════════════════════════════════════════
    //  child/bin/ (old UE games — Gothic, Jagged Alliance)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void FindExecutablesDeep_ChildBin_FindsGame()
    {
        string gameDir = CreateDir("Gothic");
        string binDir = CreateDir("Gothic", "bin");
        CreateExe(binDir, "Gothic.exe");

        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, EmptyNoiseDirs);

        var result = Assert.Single(results);
        Assert.Contains("Gothic.exe", result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Recursive fallback
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void FindExecutablesDeep_EmptyRoot_RecursesIntoSubdirs()
    {
        // Game/ has no root exes, but Game/Build/Shipping/Win64/Game.exe exists.
        string gameDir = CreateDir("BioShock");
        string deepDir = CreateDir("BioShock", "Build", "Shipping", "Win64");
        CreateExe(deepDir, "BioShock.exe");

        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, EmptyNoiseDirs);

        var result = Assert.Single(results);
        Assert.Contains("BioShock.exe", result);
    }

    [Theory]
    [InlineData("a/b/c", true)]   // depth-2 boundary is found
    [InlineData("a/b/c/d", false)] // one level deeper is not
    public void FindExecutablesDeep_DepthBoundary(string relativeDir, bool expected)
    {
        string gameDir = CreateDir("DepthGame");
        string deepDir = CreateDir(("DepthGame/" + relativeDir).Split('/'));
        CreateExe(deepDir, "DepthGame.exe");

        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, EmptyNoiseDirs);

        if (expected)
        {
            var result = Assert.Single(results);
            Assert.Contains("DepthGame.exe", result);
        }
        else
        {
            Assert.Empty(results);
        }
    }

    [Fact]
    public void FindExecutablesDeep_ScanDepth4_FindsRel4Exe()
    {
        // Game/a/b/c/d/Game.exe — default scan depth 3 does not reach it;
        // scan depth 4 does. Corpus: ~15% of real exes sit at rel 4.
        string gameDir = CreateDir("DeepRel4");
        string deepDir = CreateDir("DeepRel4", "a", "b", "c", "d");
        CreateExe(deepDir, "DeepRel4.exe");

        var atDefault = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, EmptyNoiseDirs);
        Assert.Empty(atDefault);

        var atFour = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, EmptyNoiseDirs, maxRelativeDepth: 4);
        var result = Assert.Single(atFour);
        Assert.Contains("DeepRel4.exe", result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Noise filtering
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void FindExecutablesDeep_EmptyRoot_SkipsNoiseDirsInRecursion()
    {
        string gameDir = CreateDir("Game");
        string savesDir = CreateDir("Game", "saves"); // noise
        CreateExe(savesDir, "sneaky.exe");

        IReadOnlySet<string> noiseDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "saves" };
        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, noiseDirs);

        Assert.Empty(results);
    }

    [Fact]
    public void FindExecutablesDeep_NoiseExeFiltered()
    {
        string gameDir = CreateDir("Game");
        string binDir = CreateDir("Game", "Binaries", "Win64");
        CreateExe(binDir, "Game.exe");
        CreateExe(binDir, "crashreporter.exe");

        IReadOnlyList<string> noiseExes = new List<string> { "crashreporter" };
        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), noiseExes, EmptyNoiseDirs);

        var result = Assert.Single(results);
        Assert.Contains("Game.exe", result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Multiple sites
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void FindExecutablesDeep_MultiplePlatforms_CollectsAll()
    {
        string gameDir = CreateDir("MultiPlatformGame");
        string win64 = CreateDir("MultiPlatformGame", "Binaries", "Win64");
        string wingdk = CreateDir("MultiPlatformGame", "Binaries", "WinGDK");
        CreateExe(win64, "Game.exe");
        CreateExe(wingdk, "GameGdk.exe");

        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, EmptyNoiseDirs);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Contains("Game.exe"));
        Assert.Contains(results, r => r.Contains("GameGdk.exe"));
    }

    [Fact]
    public void FindExecutablesDeep_ChildWithBinariesAndRootExe_CollectsBoth()
    {
        // Gothic 2 style: Game/bin/Game.exe + Game/Engine/Binaries/Win64/Game.exe (UE probe)
        string gameDir = CreateDir("Gothic2");
        string binDir = CreateDir("Gothic2", "bin");
        string engine = CreateDir("Gothic2", "Engine", "Binaries", "Win64");
        CreateExe(binDir, "Gothic2.exe");
        CreateExe(engine, "Gothic2Engine.exe");

        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, EmptyNoiseDirs);

        Assert.Equal(2, results.Count);
    }

    // ════════════════════════════════════════════════════════════════
    //  Deterministic tie-break
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void FindPrimaryExecutable_EqualScore_TieBreaksOnBaseName()
    {
        // Two root exes with no folder-name relationship and equal size → equal scores.
        string gameDir = CreateDir("TieGame");
        CreateExe(gameDir, "bbb.exe");
        CreateExe(gameDir, "aaa.exe");

        var result = ExecutableDiscovery.FindPrimaryExecutable(
            new DirectoryInfo(gameDir),
            Directory.GetFiles(gameDir, "*.exe"),
            EmptyNoiseExes, EmptyNoiseDirs, []);

        Assert.NotNull(result.ExePath);
        Assert.EndsWith("aaa.exe", result.ExePath);
    }

    // ════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════

    private string CreateDir(params string[] parts)
    {
        string path = Path.Combine(new[] { _tempDir }.Concat(parts).ToArray());
        Directory.CreateDirectory(path);
        return path;
    }

    private void CreateExe(string dir, string fileName)
    {
        File.WriteAllBytes(Path.Combine(dir, fileName), new byte[1024]);
    }
}
