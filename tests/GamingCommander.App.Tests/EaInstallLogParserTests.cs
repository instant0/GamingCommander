using GamingCommander.App.Services;
using Xunit;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for EaInstallLogParser — EA __Installer/InstallLog.txt parsing.
/// Verifies extraction of game name, display name, and studio from EA's legacy installer logs.
/// </summary>
public sealed class EaInstallLogParserTests : IDisposable
{
    private readonly string _tempDir;

    public EaInstallLogParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "EaInstallLogTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private static void WriteInstallLog(string gameDir, string logContent)
    {
        string installerDir = Path.Combine(gameDir, "__Installer");
        Directory.CreateDirectory(installerDir);
        // InstallLog.txt is UTF-16 encoded
        File.WriteAllText(Path.Combine(installerDir, "InstallLog.txt"), logContent, System.Text.Encoding.Unicode);
    }

    // ════════════════════════════════════════════════════════════════
    //  Basic parsing
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TryParse_StandardLog_ExtractsAllFields()
    {
        string log = """
            ****************************************
            Install Date: 10/19/2020
            19:28:22  Started logging
            ****************************************

            19:28:22  Install Location: Q:\games\Dragon Age Inquisition\
            19:28:30  (Config)Studio: BioWare
            19:28:30  (Config)Game Name: Dragon Age Inquisition
            19:28:30  (Config)Display Game Name: Dragon Age™: Inquisition
            """;
        WriteInstallLog(_tempDir, log);

        bool found = EaInstallLogParser.TryParse(new DirectoryInfo(_tempDir), out var info);

        Assert.True(found);
        Assert.NotNull(info);
        Assert.Equal("Dragon Age Inquisition", info.GameName);
        Assert.Equal("Dragon Age™: Inquisition", info.DisplayName);
        Assert.Equal("BioWare", info.Studio);
    }

    [Fact]
    public void TryParse_MultipleSessions_UsesLastSession()
    {
        // First session: old game name
        // Second session: updated game name
        string log = """
            ****************************************
            Install Date: 10/19/2020
            19:28:22  Started logging
            ****************************************
            19:28:30  (Config)Game Name: Old Name
            19:28:30  (Config)Display Game Name: Old Display Name
            19:28:30  (Config)Studio: Old Studio

            19:28:35  Stopping install logging

            ****************************************
            Install Date: 06/01/2025
            18:24:24  Started logging
            ****************************************
            18:24:28  (Config)Game Name: Dragon Age Inquisition
            18:24:28  (Config)Display Game Name: Dragon Age™: Inquisition
            18:24:28  (Config)Studio: BioWare
            """;
        WriteInstallLog(_tempDir, log);

        bool found = EaInstallLogParser.TryParse(new DirectoryInfo(_tempDir), out var info);

        Assert.True(found);
        Assert.NotNull(info);
        Assert.Equal("Dragon Age Inquisition", info.GameName);
        Assert.Equal("BioWare", info.Studio);
    }

    // ════════════════════════════════════════════════════════════════
    //  Missing data
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TryParse_NoInstallLog_ReturnsFalse()
    {
        bool found = EaInstallLogParser.TryParse(new DirectoryInfo(_tempDir), out var info);

        Assert.False(found);
        Assert.Null(info);
    }

    [Fact]
    public void TryParse_NoConfigLines_ReturnsFalse()
    {
        string log = """
            Install Location: E:\Games\Some Game\
            CommandLine: install -locale en_US
            """;
        WriteInstallLog(_tempDir, log);

        bool found = EaInstallLogParser.TryParse(new DirectoryInfo(_tempDir), out var info);

        Assert.False(found);
        Assert.Null(info);
    }

    [Fact]
    public void TryParse_GameNameOnly_NoDisplayName()
    {
        string log = """
            (Config)Studio: BioWare
            (Config)Game Name: Dragon Age Inquisition
            """;
        WriteInstallLog(_tempDir, log);

        bool found = EaInstallLogParser.TryParse(new DirectoryInfo(_tempDir), out var info);

        Assert.True(found);
        Assert.NotNull(info);
        Assert.Equal("Dragon Age Inquisition", info.GameName);
        // DisplayName falls back to GameName when not present
        Assert.Equal("Dragon Age Inquisition", info.DisplayName);
    }

    [Fact]
    public void TryParse_DisplayNameOnly_NoGameName()
    {
        string log = """
            (Config)Display Game Name: Dragon Age™: Inquisition
            """;
        WriteInstallLog(_tempDir, log);

        bool found = EaInstallLogParser.TryParse(new DirectoryInfo(_tempDir), out var info);

        Assert.True(found);
        Assert.NotNull(info);
        Assert.Equal("Dragon Age™: Inquisition", info.DisplayName);
        Assert.Empty(info.GameName);
    }

    [Fact]
    public void TryParse_NoStudio_StudioIsEmpty()
    {
        string log = """
            (Config)Game Name: Dragon Age Inquisition
            """;
        WriteInstallLog(_tempDir, log);

        bool found = EaInstallLogParser.TryParse(new DirectoryInfo(_tempDir), out var info);

        Assert.True(found);
        Assert.NotNull(info);
        Assert.Empty(info.Studio);
    }

    // ════════════════════════════════════════════════════════════════
    //  Install location NOT trusted
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TryParse_WrongInstallLocation_StillExtractsMetadata()
    {
        // User moved the game but log still references old path
        string log = """
            18:24:24  Install Location: Q:\OLD_LOCATION\Dragon Age Inquisition\
            18:24:28  (Config)Studio: BioWare
            18:24:28  (Config)Game Name: Dragon Age Inquisition
            18:24:28  (Config)Display Game Name: Dragon Age™: Inquisition
            """;
        WriteInstallLog(_tempDir, log);

        bool found = EaInstallLogParser.TryParse(new DirectoryInfo(_tempDir), out var info);

        Assert.True(found);
        Assert.NotNull(info);
        Assert.Equal("Dragon Age Inquisition", info.GameName);
        Assert.Equal("Dragon Age™: Inquisition", info.DisplayName);
        Assert.Equal("BioWare", info.Studio);
    }
}
