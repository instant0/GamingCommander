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
}
