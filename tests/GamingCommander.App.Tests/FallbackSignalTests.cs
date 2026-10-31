using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for FallbackSignalDetector — the fallback signals checked when no store signal
/// is found. Priority: Steam Emu deep → Ubisoft legacy → UE layout → root exe → root .lnk.
/// </summary>
public sealed class FallbackSignalTests : IDisposable
{
    private readonly string _tempDir;
    private static readonly IReadOnlyList<string> DefaultNoise = FolderScanner.DefaultNoiseExePatterns;

    public FallbackSignalTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "FallbackTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Theory]
    [InlineData("")]
    public void DetectFallback_SteamEmuIni_ReturnsSteamEmu(string relativeDir)
    {
        string dir = relativeDir.Length == 0
            ? _tempDir
            : CreateDir(relativeDir.Split('/'));
        CreateFile(dir, "steam_emu.ini");

        Assert.Equal(GameSourceKind.SteamEmu, Detect());
    }

    [Theory]
    [InlineData("")]
    public void DetectFallback_UbiStatsDll_ReturnsUbisoftConnect(string relativeDir)
    {
        string dir = relativeDir.Length == 0
            ? _tempDir
            : CreateDir(relativeDir);
        CreateFile(dir, "UbiStats.dll");

        Assert.Equal(GameSourceKind.UbisoftConnect, Detect());
    }

    [Theory]
    [InlineData("Win64")]
    public void DetectFallback_UeLayout_ReturnsStandalone(string platform)
    {
        // Engine/ + GameName/Binaries/{platform}/GameName.exe
        CreateDir("Engine");
        string binaries = CreateDir("GameName", "Binaries", platform);
        CreateFile(binaries, "GameName.exe");

        Assert.Equal(GameSourceKind.Standalone, Detect());
    }

    [Fact]
    public void DetectFallback_Ue3BinariesAtRoot_ReturnsStandalone()
    {
        // Binaries/Win32/GameName.exe (no Engine/)
        string win32 = CreateDir("Binaries", "Win32");
        CreateFile(win32, "GameName.exe");

        Assert.Equal(GameSourceKind.Standalone, Detect());
    }

    [Fact]
    public void DetectFallback_UeLayoutAllNoiseExes_ReturnsUnknown()
    {
        CreateDir("Engine");
        string win64 = CreateDir("GameName", "Binaries", "Win64");
        CreateFile(win64, "unins000.exe");

        Assert.Equal(GameSourceKind.Unknown, Detect());
    }

    [Fact]
    public void DetectFallback_RootExe_ReturnsStandalone()
    {
        CreateFile(_tempDir, "GameName.exe");

        Assert.Equal(GameSourceKind.Standalone, Detect());
    }

    [Fact]
    public void DetectFallback_RootExeAllNoise_ReturnsUnknown()
    {
        CreateFile(_tempDir, "unins000.exe");
        CreateFile(_tempDir, "setup.exe");

        Assert.Equal(GameSourceKind.Unknown, Detect());
    }

    [Fact]
    public void DetectFallback_RootLnk_ReturnsStandalone()
    {
        CreateFile(_tempDir, "Game.lnk");

        Assert.Equal(GameSourceKind.Standalone, Detect());
    }

    [Theory]
    [InlineData(false)]
    public void DetectFallback_NoSignal_ReturnsUnknown(bool addNonExeFiles)
    {
        if (addNonExeFiles)
        {
            CreateFile(_tempDir, "readme.txt");
            CreateFile(_tempDir, "data.bin");
        }

        Assert.Equal(GameSourceKind.Unknown, Detect());
    }

    [Theory]
    [InlineData("steam_emu.ini", GameSourceKind.SteamEmu)]
    public void DetectFallback_DeepSignalWinsOverRootExe(string signalFile, GameSourceKind expected)
    {
        CreateFile(_tempDir, signalFile);
        CreateFile(_tempDir, "GameName.exe");

        Assert.Equal(expected, Detect());
    }

    // ════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════

    private GameSourceKind Detect() =>
        FallbackSignalDetector.DetectFallbackType(new DirectoryInfo(_tempDir), DefaultNoise);

    private string CreateDir(params string[] parts)
    {
        string path = Path.Combine(new[] { _tempDir }.Concat(parts).ToArray());
        Directory.CreateDirectory(path);
        return path;
    }

    private static void CreateFile(string dir, string fileName)
    {
        File.WriteAllBytes(Path.Combine(dir, fileName), new byte[1024]);
    }
}
