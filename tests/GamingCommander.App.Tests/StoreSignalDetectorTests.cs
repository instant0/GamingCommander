using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for StoreSignalDetector — one marker theory per store predicate plus the
/// branch cases that can change a verdict (priority, weak Steam, emulator INI).
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
    //  Marker predicates (priority 1–10)
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("goggame-12345.info", false)]
    [InlineData("gog_com_game.dll", false)]
    public void DetectType_GogMarkers_ReturnsGog(string marker, bool isDirectory)
    {
        CreateMarker(marker, isDirectory);

        Assert.Equal(GameSourceKind.Gog, StoreSignalDetector.DetectType(AsDirInfo(_tempDir)));
    }

    [Theory]
    [InlineData("__Installer", true)]
    [InlineData("Touchup.exe", false)]
    [InlineData("ActivationUI.exe", false)]
    public void DetectType_EaMarkers_ReturnsEaApp(string marker, bool isDirectory)
    {
        CreateMarker(marker, isDirectory);

        Assert.Equal(GameSourceKind.EaApp, StoreSignalDetector.DetectType(AsDirInfo(_tempDir)));
    }

    [Theory]
    [InlineData("uplay_install.manifest", false)]
    [InlineData("uplay_r1_loader64.dll", false)]
    [InlineData("GRB_UPP.exe", false)]
    [InlineData("uplay_download", true)]
    public void DetectType_UbisoftMarkers_ReturnsUbisoftConnect(string marker, bool isDirectory)
    {
        CreateMarker(marker, isDirectory);

        Assert.Equal(GameSourceKind.UbisoftConnect, StoreSignalDetector.DetectType(AsDirInfo(_tempDir)));
    }

    [Theory]
    [InlineData(".egstore")]
    [InlineData(".egsstore")]
    public void DetectType_EpicMarkers_ReturnsEpic(string marker)
    {
        CreateMarker(marker, isDirectory: true);

        Assert.Equal(GameSourceKind.Epic, StoreSignalDetector.DetectType(AsDirInfo(_tempDir)));
    }

    [Theory]
    [InlineData(".battle.net", true)]
    [InlineData(".build.info", false)]
    [InlineData(".product.db", false)]
    public void DetectType_BlizzardMarkers_ReturnsBattleNet(string marker, bool isDirectory)
    {
        CreateMarker(marker, isDirectory);

        Assert.Equal(GameSourceKind.BattleNet, StoreSignalDetector.DetectType(AsDirInfo(_tempDir)));
    }

    [Fact]
    public void DetectType_XboxMetadata_ReturnsXbox()
    {
        CreateFile("default-metadata.json");

        Assert.Equal(GameSourceKind.Xbox, StoreSignalDetector.DetectType(AsDirInfo(_tempDir)));
    }

    [Fact]
    public void DetectType_TitleRgl_ReturnsRockstar()
    {
        CreateFile("title.rgl");

        Assert.Equal(GameSourceKind.Rockstar, StoreSignalDetector.DetectType(AsDirInfo(_tempDir)));
    }

    [Theory]
    [InlineData("steam_api64.dll")]
    [InlineData("steam_appid.txt")]
    public void DetectType_SteamEmuMarkers_ReturnsSteamEmu(string marker)
    {
        CreateFile(marker);

        Assert.Equal(GameSourceKind.SteamEmu, StoreSignalDetector.DetectType(AsDirInfo(_tempDir)));
    }

    // ════════════════════════════════════════════════════════════════
    //  Ubisoft emulator INI
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_UbisoftEmulatorWithIni_ReturnsUbisoftConnect()
    {
        CreateFile("uplay_loader.exe");
        File.WriteAllText(Path.Combine(_tempDir, "settings.ini"),
            "Username=testuser\nAccountId=12345\n");

        Assert.Equal(GameSourceKind.UbisoftConnect, StoreSignalDetector.DetectType(AsDirInfo(_tempDir)));
    }

    [Fact]
    public void DetectType_UbisoftEmulatorWithoutIni_ReturnsUnknown()
    {
        CreateFile("uplay_loader.exe");

        // No INI with Username/AccountId → not emulator signal
        Assert.Equal(GameSourceKind.Unknown, StoreSignalDetector.DetectType(AsDirInfo(_tempDir)));
    }

    // ════════════════════════════════════════════════════════════════
    //  Steam location (weak signal upgraded to Steam)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_SteamApi64Dll_SteamLibraryLocation_ReturnsSteam()
    {
        CreateFile("steam_api64.dll");
        DirectoryInfo dir = AsDirInfo(_tempDir);

        Assert.Equal(GameSourceKind.Steam, StoreSignalDetector.DetectType(dir, true));
        Assert.Equal(GameSourceKind.SteamEmu, StoreSignalDetector.DetectType(dir, false));
    }

    // ════════════════════════════════════════════════════════════════
    //  No signal and priority ordering
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DetectType_EmptyDir_ReturnsUnknown()
    {
        Assert.Equal(GameSourceKind.Unknown, StoreSignalDetector.DetectType(AsDirInfo(_tempDir)));
    }

    [Fact]
    public void DetectType_GogAndEaPresent_ReturnsGog()
    {
        CreateFile("goggame.dll");
        CreateDir("__Installer");

        // GOG (priority 1) > EA (priority 2)
        Assert.Equal(GameSourceKind.Gog, StoreSignalDetector.DetectType(AsDirInfo(_tempDir)));
    }

    [Fact]
    public void DetectType_EaAndSteamEmuPresent_ReturnsEaApp()
    {
        CreateDir("__Installer");
        CreateFile("steam_api64.dll");

        // EA (priority 2) > Steam Emu (priority 9)
        Assert.Equal(GameSourceKind.EaApp, StoreSignalDetector.DetectType(AsDirInfo(_tempDir)));
    }

    // ════════════════════════════════════════════════════════════════
    //  Blizzard product codename (external .build.info shape)
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("tpr/diablo3", "diablo3")]
    [InlineData("prometheus", "prometheus")]
    public void ExtractBlizzardProduct_ReturnsCodename(string cdnPath, string expected)
    {
        File.WriteAllText(Path.Combine(_tempDir, ".build.info"),
            "Header\n" +
            $"eu|1|key|key2|||{cdnPath}|host\n");

        Assert.Equal(expected, StoreSignalDetector.ExtractBlizzardProduct(AsDirInfo(_tempDir)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExtractBlizzardProduct_NoProduct_ReturnsNull(bool createEmptyFile)
    {
        if (createEmptyFile)
            File.WriteAllText(Path.Combine(_tempDir, ".build.info"), "");

        Assert.Null(StoreSignalDetector.ExtractBlizzardProduct(AsDirInfo(_tempDir)));
    }

    // ════════════════════════════════════════════════════════════════
    //  UbisoftReadmeParser — external readme shape
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("Support", "Readme", "Readme.txt", "Ubisoft\r\nGhost Recon Breakpoint\r\n© 2019 Ubisoft Entertainment\r\n", "Ubisoft", "Ghost Recon Breakpoint")]
    public void UbisoftReadmeParser_ParsesPublisherAndTitle(
        string supportDir, string readmeDir, string fileName, string content,
        string expectedPublisher, string expectedTitle)
    {
        string dir = CreateDir(supportDir, readmeDir);
        File.WriteAllText(Path.Combine(dir, fileName), content);

        var result = UbisoftReadmeParser.TryParse(AsDirInfo(_tempDir));

        Assert.NotNull(result);
        Assert.Equal(expectedPublisher, result.Publisher);
        Assert.Equal(expectedTitle, result.GameTitle);
    }

    [Theory]
    [InlineData("Ubisoft Entertainment\r\nUbisoft Entertainment\r\n© 2012 Ubisoft Entertainment\r\n", "Ubisoft Entertainment")]
    public void UbisoftReadmeParser_PublisherOnLine2_Rejected(string content, string expectedPublisher)
    {
        // B23: Some Ubisoft readmes repeat the publisher on line 2 instead of the title.
        string dir = CreateDir("Support", "Readme");
        File.WriteAllText(Path.Combine(dir, "Readme.txt"), content);

        var result = UbisoftReadmeParser.TryParse(AsDirInfo(_tempDir));

        Assert.NotNull(result);
        Assert.Equal(expectedPublisher, result.Publisher);
        Assert.Null(result.GameTitle);
    }

    [Fact]
    public void UbisoftReadmeParser_SingleLine_ReturnsPublisherOnly()
    {
        string dir = CreateDir("Support", "Readme");
        File.WriteAllText(Path.Combine(dir, "Readme.txt"), "Ubisoft\r\n");

        var result = UbisoftReadmeParser.TryParse(AsDirInfo(_tempDir));

        Assert.NotNull(result);
        Assert.Equal("Ubisoft", result.Publisher);
        Assert.Null(result.GameTitle);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UbisoftReadmeParser_NoContent_ReturnsNull(bool createEmptyFile)
    {
        if (createEmptyFile)
        {
            string dir = CreateDir("Support", "Readme");
            File.WriteAllText(Path.Combine(dir, "Readme.txt"), "");
        }

        Assert.Null(UbisoftReadmeParser.TryParse(AsDirInfo(_tempDir)));
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

    private void CreateMarker(string name, bool isDirectory)
    {
        if (isDirectory)
            CreateDir(name);
        else
            CreateFile(name);
    }

    private void CreateFile(string fileName)
    {
        File.WriteAllBytes(Path.Combine(_tempDir, fileName), new byte[16]);
    }

    private DirectoryInfo AsDirInfo(string path) => new(path);
}
