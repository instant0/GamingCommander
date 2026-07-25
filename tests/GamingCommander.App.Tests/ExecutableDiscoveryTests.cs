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
    //  Win64 (existing behavior)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void FindExecutablesDeep_Win64_FindsGame()
    {
        // GameName/Binaries/Win64/GameName.exe
        string gameDir = CreateDir("MyGame");
        string win64 = CreateDir("MyGame", "Binaries", "Win64");
        CreateExe(win64, "MyGame.exe");

        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, EmptyNoiseDirs);

        Assert.Single(results);
        Assert.Contains("MyGame.exe", results[0]);
    }

    // ════════════════════════════════════════════════════════════════
    //  Win32 (new in T66)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void FindExecutablesDeep_Win32_FindsGame()
    {
        string gameDir = CreateDir("ClassicGame");
        string win32 = CreateDir("ClassicGame", "Binaries", "Win32");
        CreateExe(win32, "ClassicGame.exe");

        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, EmptyNoiseDirs);

        Assert.Single(results);
        Assert.Contains("ClassicGame.exe", results[0]);
    }

    [Fact]
    public void FindExecutablesDeep_Win32_DeduplicatesAcrossPlatforms()
    {
        // If the same exe is somehow in both Win32 and Win64, only one result
        string gameDir = CreateDir("Game");
        string win32 = CreateDir("Game", "Binaries", "Win32");
        string win64 = CreateDir("Game", "Binaries", "Win64");
        CreateExe(win32, "Game.exe");
        CreateExe(win64, "Game.exe");

        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, EmptyNoiseDirs);

        // Two distinct paths, both kept (different absolute paths)
        Assert.Equal(2, results.Count);
    }

    // ════════════════════════════════════════════════════════════════
    //  WinGDK (new in T66)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void FindExecutablesDeep_WinGDK_FindsGame()
    {
        string gameDir = CreateDir("GdkGame");
        string wingdk = CreateDir("GdkGame", "Binaries", "WinGDK");
        CreateExe(wingdk, "GdkGame.exe");

        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, EmptyNoiseDirs);

        Assert.Single(results);
        Assert.Contains("GdkGame.exe", results[0]);
    }

    // ════════════════════════════════════════════════════════════════
    //  Steam (new in T66)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void FindExecutablesDeep_Steam_FindsGame()
    {
        string gameDir = CreateDir("SteamGame");
        string steam = CreateDir("SteamGame", "Binaries", "Steam");
        CreateExe(steam, "SteamGame.exe");

        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, EmptyNoiseDirs);

        Assert.Single(results);
        Assert.Contains("SteamGame.exe", results[0]);
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

        Assert.Single(results);
        Assert.Contains("Gothic.exe", results[0]);
    }

    [Fact]
    public void FindExecutablesDeep_ChildBin_InheritsNoiseExeFilter()
    {
        string gameDir = CreateDir("Game");
        string binDir = CreateDir("Game", "bin");
        CreateExe(binDir, "Game.exe");
        CreateExe(binDir, "setup.exe"); // noise

        IReadOnlyList<string> noiseExes = new List<string> { "setup" };
        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), noiseExes, EmptyNoiseDirs);

        Assert.Single(results);
        Assert.Contains("Game.exe", results[0]);
    }

    // ════════════════════════════════════════════════════════════════
    //  Recursive fallback (BioShock pattern)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void FindExecutablesDeep_EmptyRoot_RecursesIntoSubdirs()
    {
        // Game/ has no root exes, but Game/Build/Shipping/Win64/Game.exe exists
        string gameDir = CreateDir("BioShock");
        string deepDir = CreateDir("BioShock", "Build", "Shipping", "Win64");
        CreateExe(deepDir, "BioShock.exe");

        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, EmptyNoiseDirs);

        Assert.Single(results);
        Assert.Contains("BioShock.exe", results[0]);
    }

    [Fact]
    public void FindExecutablesDeep_EmptyRoot_RespectsMaxDepth2()
    {
        // Game/a/b/c/d/Game.exe — depth 3 from root's children, should NOT be found
        // Recursion: depth 0 = root children, depth 1 = grandchildren, depth 2 = great-grandchildren
        // At depth 2 we enumerate children of c but don't recurse into d
        string gameDir = CreateDir("DeepGame");
        string deepDir = CreateDir("DeepGame", "a", "b", "c", "d");
        CreateExe(deepDir, "DeepGame.exe");

        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, EmptyNoiseDirs);

        Assert.Empty(results);
    }

    [Fact]
    public void FindExecutablesDeep_EmptyRoot_FindsAtDepth2()
    {
        // Game/a/b/c/Game.exe — depth 2 boundary (we enumerate c's children at depth 2)
        string gameDir = CreateDir("Depth2Game");
        string depth2 = CreateDir("Depth2Game", "a", "b", "c");
        CreateExe(depth2, "Depth2Game.exe");

        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, EmptyNoiseDirs);

        Assert.Single(results);
        Assert.Contains("Depth2Game.exe", results[0]);
    }

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

    // ════════════════════════════════════════════════════════════════
    //  Noise filtering
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void FindExecutablesDeep_NoiseDirSkipped()
    {
        string gameDir = CreateDir("Game");
        string binDir = CreateDir("Game", "Binaries", "Win64");
        CreateExe(binDir, "Game.exe");
        // Redist is a noise subdir
        string redistDir = CreateDir("Game", "redist");
        CreateExe(redistDir, "redist_installer.exe");

        IReadOnlySet<string> noiseDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "redist" };
        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, noiseDirs);

        Assert.Single(results);
        Assert.Contains("Game.exe", results[0]);
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

        Assert.Single(results);
        Assert.Contains("Game.exe", results[0]);
    }

    // ════════════════════════════════════════════════════════════════
    //  Multiple platforms (real-world scenario)
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
        // Gothic 2 style: Game/bin/Game.exe (root-level child exe) + Game/Engine/Binaries/Win64/Game.exe (UE probe)
        // The UE probe checks child/Binaries/{platform}, so "Engine" as a child resolves correctly.
        string gameDir = CreateDir("Gothic2");
        string binDir = CreateDir("Gothic2", "bin");
        string engine = CreateDir("Gothic2", "Engine", "Binaries", "Win64");
        CreateExe(binDir, "Gothic2.exe");
        CreateExe(engine, "Gothic2Engine.exe");

        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, EmptyNoiseDirs);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void FindExecutablesDeep_BinariesAsDirectChild_UEProbeMissesButRecursiveFinds()
    {
        // Edge case: Binaries/ is a direct child of game root.
        // UE probe checks child/Binaries/{platform} → Binaries/Binaries/{platform} (miss).
        // But recursive fallback finds it since no other exes were found.
        string gameDir = CreateDir("FlatGame");
        string win64 = CreateDir("FlatGame", "Binaries", "Win64");
        CreateExe(win64, "FlatGame.exe");

        var results = ExecutableDiscovery.FindExecutablesDeep(
            new DirectoryInfo(gameDir), EmptyNoiseExes, EmptyNoiseDirs);

        // Found via 2-level recursive fallback (depth 2 = Binaries/Win64/FlatGame.exe)
        Assert.Single(results);
        Assert.Contains("FlatGame.exe", results[0]);
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
