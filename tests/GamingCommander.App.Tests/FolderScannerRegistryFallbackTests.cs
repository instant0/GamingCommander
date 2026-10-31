using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

/// <summary>
/// FolderScanner pass 1c: when a registry reader is supplied, a folder with no
/// filesystem store signal is classified from its per-game registry key.
/// </summary>
public sealed class FolderScannerRegistryFallbackTests
{
    [Fact]
    public void Scan_RegistryPerGameKey_ClassifiesFolderWithoutFilesystemSignal()
    {
        using var temp = new TempDirectory("gc_regscan_");
        string root = temp.CreateDirectory("Root");
        string gameDir = temp.CreateDirectory("Root", "Dead Space 3");

        string regContent = $"""
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games\Dead Space 3]
            "Install Dir"=str(2):"{gameDir.Replace("\\", "\\\\")}"
            "DisplayName"=str(2):"Dead Space 3"
            """;

        var scanner = new FolderScanner(
            [], BlacklistData.Empty, new MockRegistryReader(regContent));

        GameEntry entry = Assert.Single(scanner.Scan(root, GameSourceKind.Standalone));

        Assert.Equal("Dead Space 3", entry.FolderName);
        Assert.Equal(GameSourceKind.EaApp, entry.GameSource);
        Assert.True(entry.IsSourceOverridden);
    }

    [Fact]
    public void Scan_RegistryFallbackDisabled_MakesZeroRegistryReadsAndKeepsFileSignals()
    {
        using var temp = new TempDirectory("gc_regscan_");
        string root = temp.CreateDirectory("Root");
        string gameDir = temp.CreateDirectory("Root", "File Signal Game");
        temp.CreateDirectory("Root", "File Signal Game", "__Installer");
        temp.CreateFile(Path.Combine("Root", "File Signal Game", "FileSignalGame.exe"), new byte[16]);

        var reader = new CountingRegistryReader(new MockRegistryReader(EaRegistry(gameDir)));
        var scanner = new FolderScanner(
            [], BlacklistData.Empty, reader, allowRegistryFallback: () => false);

        GameEntry entry = Assert.Single(scanner.Scan(root, GameSourceKind.Standalone));

        Assert.Equal(GameSourceKind.EaApp, entry.GameSource);
        Assert.Equal(0, reader.Reads);
    }

    [Fact]
    public void Scan_RegistryFallbackEnabled_UsesTheFallback()
    {
        using var temp = new TempDirectory("gc_regscan_");
        string root = temp.CreateDirectory("Root");
        string gameDir = temp.CreateDirectory("Root", "Registry Only Game");

        var reader = new CountingRegistryReader(new MockRegistryReader(EaRegistry(gameDir)));
        var scanner = new FolderScanner(
            [], BlacklistData.Empty, reader, allowRegistryFallback: () => true);

        GameEntry entry = Assert.Single(scanner.Scan(root, GameSourceKind.Standalone));

        Assert.Equal(GameSourceKind.EaApp, entry.GameSource);
        Assert.True(reader.Reads > 0);
    }

    [Fact]
    public void Scan_GateChange_TakesEffectOnTheNextScanWithoutReconstruction()
    {
        using var temp = new TempDirectory("gc_regscan_");
        string root = temp.CreateDirectory("Root");
        string gameDir = temp.CreateDirectory("Root", "Registry Only Game");
        temp.CreateFile(Path.Combine("Root", "Registry Only Game", "RegistryOnlyGame.exe"), new byte[16]);

        bool allowRegistryFallback = false;
        var reader = new CountingRegistryReader(new MockRegistryReader(EaRegistry(gameDir)));
        var scanner = new FolderScanner(
            [], BlacklistData.Empty, reader, allowRegistryFallback: () => allowRegistryFallback);

        GameEntry disabled = Assert.Single(scanner.Scan(root, GameSourceKind.Standalone));
        Assert.Equal(GameSourceKind.Standalone, disabled.GameSource);
        Assert.Equal(0, reader.Reads);

        allowRegistryFallback = true;
        GameEntry enabled = Assert.Single(scanner.Scan(root, GameSourceKind.Standalone));
        Assert.Equal(GameSourceKind.EaApp, enabled.GameSource);
        int readsAfterEnable = reader.Reads;
        Assert.True(readsAfterEnable > 0);

        allowRegistryFallback = false;
        GameEntry disabledAgain = Assert.Single(scanner.Scan(root, GameSourceKind.Standalone));
        Assert.Equal(GameSourceKind.Standalone, disabledAgain.GameSource);
        Assert.Equal(readsAfterEnable, reader.Reads);
    }

    private static string EaRegistry(string gameDir) => $"""
        Windows Registry Editor Version 5.00

        [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games\Registry Only Game]
        "Install Dir"=str(2):"{gameDir.Replace("\\", "\\\\")}"
        "DisplayName"=str(2):"Registry Only Game"
        """;
}
