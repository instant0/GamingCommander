using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for FallbackSignalDetector — the 5 fallback signals checked when no store signal is found.
/// Priority: Steam Emu deep → Ubisoft legacy → UE layout → root exe → root .lnk.
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

    // ════════════════════════════════════════════════════════════════
    //  Signal 1: Steam Emulator Deep
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_SteamEmuIniAtRoot_ReturnsSteamEmu()
    {
        CreateFile("steam_emu.ini");

        var result = FallbackSignalDetector.DetectFallbackType(AsDir(_tempDir), DefaultNoise);

        Assert.Equal(GameSourceKind.SteamEmu, result);
    }

    [Fact]
    public void DetectType_SteamEmuIniInChild_ReturnsSteamEmu()
    {
        CreateDir("GameData");
        CreateFile("GameData", "steam_emu.ini");

        var result = FallbackSignalDetector.DetectFallbackType(AsDir(_tempDir), DefaultNoise);

        Assert.Equal(GameSourceKind.SteamEmu, result);
    }

    [Fact]
    public void DetectType_SteamEmuIniInUeSteamworksPath_ReturnsSteamEmu()
    {
        CreateDir("Engine", "Binaries", "ThirdParty", "Steamworks", "Steamv142", "Win64");
        CreateFile("Engine", "Binaries", "ThirdParty", "Steamworks", "Steamv142", "Win64", "steam_emu.ini");

        var result = FallbackSignalDetector.DetectFallbackType(AsDir(_tempDir), DefaultNoise);

        Assert.Equal(GameSourceKind.SteamEmu, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Signal 2: Ubisoft Legacy
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_UbiStatsDllAtRoot_ReturnsUbisoftConnect()
    {
        CreateFile("UbiStats.dll");

        var result = FallbackSignalDetector.DetectFallbackType(AsDir(_tempDir), DefaultNoise);

        Assert.Equal(GameSourceKind.UbisoftConnect, result);
    }

    [Fact]
    public void DetectType_UbiStatsDllInChild_ReturnsUbisoftConnect()
    {
        CreateDir("Support");
        CreateFile("Support", "UbiStats.dll");

        var result = FallbackSignalDetector.DetectFallbackType(AsDir(_tempDir), DefaultNoise);

        Assert.Equal(GameSourceKind.UbisoftConnect, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Signal 3: Unreal Engine Layout
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_Ue4Layout_ReturnsStandalone()
    {
        // Engine/ + GameName/Binaries/Win64/GameName.exe
        CreateDir("Engine");
        CreateDir("GameName", "Binaries", "Win64");
        CreateFile("GameName", "Binaries", "Win64", "GameName.exe");

        var result = FallbackSignalDetector.DetectFallbackType(AsDir(_tempDir), DefaultNoise);

        Assert.Equal(GameSourceKind.Standalone, result);
    }

    [Fact]
    public void DetectType_Ue3BinariesAtRoot_ReturnsStandalone()
    {
        // Binaries/Win32/GameName.exe (no Engine/)
        CreateDir("Binaries", "Win32");
        CreateFile("Binaries", "Win32", "GameName.exe");

        var result = FallbackSignalDetector.DetectFallbackType(AsDir(_tempDir), DefaultNoise);

        Assert.Equal(GameSourceKind.Standalone, result);
    }

    [Fact]
    public void DetectType_UeLayoutWinGdk_ReturnsStandalone()
    {
        CreateDir("Engine");
        CreateDir("GameName", "Binaries", "WinGDK");
        CreateFile("GameName", "Binaries", "WinGDK", "GameName.exe");

        var result = FallbackSignalDetector.DetectFallbackType(AsDir(_tempDir), DefaultNoise);

        Assert.Equal(GameSourceKind.Standalone, result);
    }

    [Fact]
    public void DetectType_UeLayoutAllNoiseExes_ReturnsUnknown()
    {
        // UE layout but all exes are noise
        CreateDir("Engine");
        CreateDir("GameName", "Binaries", "Win64");
        CreateFile("GameName", "Binaries", "Win64", "unins000.exe");

        var result = FallbackSignalDetector.DetectFallbackType(AsDir(_tempDir), DefaultNoise);

        Assert.Equal(GameSourceKind.Unknown, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Signal 4: Root Executable
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_RootExe_ReturnsStandalone()
    {
        CreateFile("GameName.exe");

        var result = FallbackSignalDetector.DetectFallbackType(AsDir(_tempDir), DefaultNoise);

        Assert.Equal(GameSourceKind.Standalone, result);
    }

    [Fact]
    public void DetectType_RootExeAllNoise_ReturnsUnknown()
    {
        CreateFile("unins000.exe");
        CreateFile("setup.exe");

        var result = FallbackSignalDetector.DetectFallbackType(AsDir(_tempDir), DefaultNoise);

        Assert.Equal(GameSourceKind.Unknown, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Signal 5: Root .lnk Shortcut
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_RootLnk_ReturnsStandalone()
    {
        CreateFile("Game.lnk");

        var result = FallbackSignalDetector.DetectFallbackType(AsDir(_tempDir), DefaultNoise);

        Assert.Equal(GameSourceKind.Standalone, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  No Signal
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_EmptyDir_ReturnsUnknown()
    {
        var result = FallbackSignalDetector.DetectFallbackType(AsDir(_tempDir), DefaultNoise);

        Assert.Equal(GameSourceKind.Unknown, result);
    }

    [Fact]
    public void DetectType_OnlyNonExeFiles_ReturnsUnknown()
    {
        CreateFile("readme.txt");
        CreateFile("data.bin");

        var result = FallbackSignalDetector.DetectFallbackType(AsDir(_tempDir), DefaultNoise);

        Assert.Equal(GameSourceKind.Unknown, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Priority Ordering
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_SteamEmuAndRootExe_ReturnsSteamEmu()
    {
        // Steam Emu (priority 1) should win over root exe (priority 4)
        CreateFile("steam_emu.ini");
        CreateFile("GameName.exe");

        var result = FallbackSignalDetector.DetectFallbackType(AsDir(_tempDir), DefaultNoise);

        Assert.Equal(GameSourceKind.SteamEmu, result);
    }

    [Fact]
    public void DetectType_UbiStatsAndRootExe_ReturnsUbisoftConnect()
    {
        // Ubisoft legacy (priority 2) should win over root exe (priority 4)
        CreateFile("UbiStats.dll");
        CreateFile("GameName.exe");

        var result = FallbackSignalDetector.DetectFallbackType(AsDir(_tempDir), DefaultNoise);

        Assert.Equal(GameSourceKind.UbisoftConnect, result);
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

    private void CreateFile(params string[] parts)
    {
        string path = Path.Combine(new[] { _tempDir }.Concat(parts).ToArray());
        File.WriteAllBytes(path, new byte[1024]);
    }

    private DirectoryInfo AsDir(string path) => new(path);
}
