using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for StoreSignalDetector — verifies all 10 store/platform signals
/// and priority ordering (first match wins).
/// </summary>
public sealed class StoreSignalDetectorTests : IDisposable
{
    private readonly string _tempDir;

    public StoreSignalDetectorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "StoreSignalTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    // ════════════════════════════════════════════════════════════════
    //  GOG Signal (Priority 1)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_GogGameDll_ReturnsGog()
    {
        CreateFile("goggame.dll");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.Gog, result);
    }

    [Fact]
    public void DetectType_GogGameInfo_ReturnsGog()
    {
        CreateFile("goggame-12345.info");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.Gog, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  EA Signal (Priority 2)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_EaInstallerDir_ReturnsEaApp()
    {
        CreateDir("__Installer");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.EaApp, result);
    }

    [Fact]
    public void DetectType_EaTouchupExe_ReturnsEaApp()
    {
        CreateFile("Touchup.exe");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.EaApp, result);
    }

    [Fact]
    public void DetectType_EaActivationUiExe_ReturnsEaApp()
    {
        CreateFile("ActivationUI.exe");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.EaApp, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Ubisoft Emulator Signal (Priority 3)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_UbisoftEmulatorWithIni_ReturnsUbisoftConnect()
    {
        CreateFile("uplay_loader.exe");
        File.WriteAllText(Path.Combine(_tempDir, "settings.ini"),
            "Username=testuser\nAccountId=12345\n");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.UbisoftConnect, result);
    }

    [Fact]
    public void DetectType_UbisoftEmulatorWithoutIni_ReturnsUnknown()
    {
        CreateFile("uplay_loader.exe");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        // No INI with Username/AccountId → not emulator signal
        Assert.Equal(GameSourceKind.Unknown, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Ubisoft Signal (Priority 4)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_UplayInstallManifest_ReturnsUbisoftConnect()
    {
        CreateFile("uplay_install.manifest");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.UbisoftConnect, result);
    }

    [Fact]
    public void DetectType_UplayInstallState_ReturnsUbisoftConnect()
    {
        CreateFile("uplay_install.state");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.UbisoftConnect, result);
    }

    [Fact]
    public void DetectType_UplayR1Loader64_ReturnsUbisoftConnect()
    {
        CreateFile("uplay_r1_loader64.dll");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.UbisoftConnect, result);
    }

    [Fact]
    public void DetectType_UplayR2Loader64_ReturnsUbisoftConnect()
    {
        CreateFile("uplay_r2_loader64.dll");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.UbisoftConnect, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Epic Signal (Priority 5)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_EgstoreDir_ReturnsEpic()
    {
        CreateDir(".egstore");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.Epic, result);
    }

    [Fact]
    public void DetectType_EgsstoreDir_ReturnsEpic()
    {
        CreateDir(".egsstore");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.Epic, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Blizzard Signal (Priority 6)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_BattleNetDir_ReturnsBattleNet()
    {
        CreateDir(".battle.net");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.BattleNet, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Xbox Signal (Priority 7)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_XboxMetadata_ReturnsXbox()
    {
        CreateFile("default-metadata.json");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.Xbox, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Rockstar Signal (Priority 8)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_TitleRgl_ReturnsRockstar()
    {
        CreateFile("title.rgl");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.Rockstar, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Steam Emulator Signal (Priority 9 — strong)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_SteamApi64Dll_ReturnsSteamEmu()
    {
        CreateFile("steam_api64.dll");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.SteamEmu, result);
    }

    [Fact]
    public void DetectType_SteamApiDll_ReturnsSteamEmu()
    {
        CreateFile("steam_api.dll");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.SteamEmu, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Steam Signal (Priority 10 — weak)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_SteamAppIdTxt_ReturnsSteamEmu()
    {
        CreateFile("steam_appid.txt");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.SteamEmu, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  No Signal
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_EmptyDir_ReturnsUnknown()
    {
        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.Unknown, result);
    }

    [Fact]
    public void DetectType_RandomFiles_ReturnsUnknown()
    {
        CreateFile("readme.txt");
        CreateFile("data.bin");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.Unknown, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Priority Ordering — GOG wins over EA when both present
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_GogAndEaPresent_ReturnsGog()
    {
        CreateFile("goggame.dll");
        CreateDir("__Installer");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        // GOG (priority 1) > EA (priority 2)
        Assert.Equal(GameSourceKind.Gog, result);
    }

    [Fact]
    public void DetectType_EaAndSteamEmuPresent_ReturnsEaApp()
    {
        CreateDir("__Installer");
        CreateFile("steam_api64.dll");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        // EA (priority 2) > Steam Emu (priority 9)
        Assert.Equal(GameSourceKind.EaApp, result);
    }

    // ════════════════════════════════════════════════════════════════
    //  Individual Signal Methods
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void HasGogSignal_NoGogFiles_ReturnsFalse()
    {
        Assert.False(StoreSignalDetector.HasGogSignal(AsDirInfo(_tempDir)));
    }

    [Fact]
    public void HasEaSignal_NoInstallerDir_ReturnsFalse()
    {
        Assert.False(StoreSignalDetector.HasEaSignal(AsDirInfo(_tempDir)));
    }

    [Fact]
    public void HasEpicSignal_NoStoreDir_ReturnsFalse()
    {
        Assert.False(StoreSignalDetector.HasEpicSignal(AsDirInfo(_tempDir)));
    }

    [Fact]
    public void HasBlizzardSignal_NoBattleNetDir_ReturnsFalse()
    {
        Assert.False(StoreSignalDetector.HasBlizzardSignal(AsDirInfo(_tempDir)));
    }

    [Fact]
    public void HasXboxSignal_NoMetadataFile_ReturnsFalse()
    {
        Assert.False(StoreSignalDetector.HasXboxSignal(AsDirInfo(_tempDir)));
    }

    [Fact]
    public void HasRockstarSignal_NoRglFile_ReturnsFalse()
    {
        Assert.False(StoreSignalDetector.HasRockstarSignal(AsDirInfo(_tempDir)));
    }

    [Fact]
    public void HasSteamSignal_NoAppId_ReturnsFalse()
    {
        Assert.False(StoreSignalDetector.HasSteamSignal(AsDirInfo(_tempDir)));
    }

    [Fact]
    public void HasSteamEmulatorSignal_NoApiDll_ReturnsFalse()
    {
        Assert.False(StoreSignalDetector.HasSteamEmulatorSignal(AsDirInfo(_tempDir)));
    }

    // ════════════════════════════════════════════════════════════════
    //  Ubisoft New Signals (Plan 112 Step 3)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_UplayDownloadDir_ReturnsUbisoftConnect()
    {
        CreateDir("uplay_download");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.UbisoftConnect, result);
    }

    [Fact]
    public void DetectType_UppExe_ReturnsUbisoftConnect()
    {
        CreateFile("GRB_UPP.exe");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.UbisoftConnect, result);
    }

    [Fact]
    public void DetectType_UppVulkanExe_ReturnsUbisoftConnect()
    {
        CreateFile("GRB_UPP_vulkan.exe");

        var result = StoreSignalDetector.DetectType(AsDirInfo(_tempDir));

        Assert.Equal(GameSourceKind.UbisoftConnect, result);
    }

    [Fact]
    public void HasUbisoftSignal_UplayDownloadDir_ReturnsTrue()
    {
        CreateDir("uplay_download");

        Assert.True(StoreSignalDetector.HasUbisoftSignal(AsDirInfo(_tempDir)));
    }

    [Fact]
    public void HasUbisoftSignal_UppExe_ReturnsTrue()
    {
        CreateFile("GRB_UPP.exe");

        Assert.True(StoreSignalDetector.HasUbisoftSignal(AsDirInfo(_tempDir)));
    }

    [Fact]
    public void HasUbisoftSignal_NoSignals_ReturnsFalse()
    {
        Assert.False(StoreSignalDetector.HasUbisoftSignal(AsDirInfo(_tempDir)));
    }

    // ════════════════════════════════════════════════════════════════
    //  UbisoftReadmeParser (Plan 112 Step 3B)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void UbisoftReadmeParser_StandardFormat_ParsesTitle()
    {
        string readmeDir = CreateDir("Support", "Readme");
        File.WriteAllText(Path.Combine(readmeDir, "Readme.txt"),
            "Ubisoft\r\nGhost Recon Breakpoint\r\n© 2019 Ubisoft Entertainment\r\n");

        var result = UbisoftReadmeParser.TryParse(AsDirInfo(_tempDir));

        Assert.NotNull(result);
        Assert.Equal("Ubisoft", result.Publisher);
        Assert.Equal("Ghost Recon Breakpoint", result.GameTitle);
    }

    [Fact]
    public void UbisoftReadmeParser_CaseInsensitive_Works()
    {
        string readmeDir = CreateDir("support", "readme");
        File.WriteAllText(Path.Combine(readmeDir, "game.txt"),
            "Publisher\r\nGame Title\r\n");

        var result = UbisoftReadmeParser.TryParse(AsDirInfo(_tempDir));

        Assert.NotNull(result);
        Assert.Equal("Game Title", result.GameTitle);
    }

    [Fact]
    public void UbisoftReadmeParser_NoReadmeDir_ReturnsNull()
    {
        var result = UbisoftReadmeParser.TryParse(AsDirInfo(_tempDir));

        Assert.Null(result);
    }

    [Fact]
    public void UbisoftReadmeParser_EmptyFile_ReturnsNull()
    {
        string readmeDir = CreateDir("Support", "Readme");
        File.WriteAllText(Path.Combine(readmeDir, "Readme.txt"), "");

        var result = UbisoftReadmeParser.TryParse(AsDirInfo(_tempDir));

        Assert.Null(result);
    }

    [Fact]
    public void UbisoftReadmeParser_SingleLine_ReturnsPublisherOnly()
    {
        string readmeDir = CreateDir("Support", "Readme");
        File.WriteAllText(Path.Combine(readmeDir, "Readme.txt"), "Ubisoft\r\n");

        var result = UbisoftReadmeParser.TryParse(AsDirInfo(_tempDir));

        Assert.NotNull(result);
        Assert.Equal("Ubisoft", result.Publisher);
        Assert.Null(result.GameTitle);
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

    private void CreateFile(string fileName)
    {
        File.WriteAllBytes(Path.Combine(_tempDir, fileName), new byte[16]);
    }

    private DirectoryInfo AsDirInfo(string path) => new(path);
}
