using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

/// <summary>
/// Scanner-level install state for non-Steam entries. FolderScanner currently reports
/// every discovered folder as Installed; this pins that contract before health work
/// (missing/moved/orphaned states) changes it.
/// </summary>
public sealed class FolderScannerInstallStateTests
{
    [Fact]
    public void Scan_GogSignal_ReportsInstalled()
    {
        using var temp = new TempDirectory("gc_install_state_");
        string root = temp.CreateDirectory("Root");
        string gameDir = temp.CreateDirectory("Root", "GogGame");
        temp.CreateFile(System.IO.Path.Combine("Root", "GogGame", "goggame.dll"), new byte[8]);
        temp.CreateFile(System.IO.Path.Combine("Root", "GogGame", "GogGame.exe"), new byte[16]);

        GameEntry entry = Assert.Single(new FolderScanner().Scan(root, GameSourceKind.Standalone));

        Assert.Equal(gameDir, entry.FolderPath);
        Assert.Equal(GameSourceKind.Gog, entry.GameSource);
        Assert.Equal(InstallState.Installed, entry.Install.State);
    }
}
