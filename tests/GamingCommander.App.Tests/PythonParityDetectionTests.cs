using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

/// <summary>
/// C# parity tests for the Python reference scanner fixes (2026-09-07).
/// Mirrors detect.py fixtures:
///   - Terminating rule (T1.5) — ArcInstall/neverwinter_en (S-D2)
///   - Whole-name match (GAP A) — Diablo III / Dead Space 3
///   - Backup exclusion guard — -Penumbra.exe never terminates
///   - UE-wrapped containers (GAP B) — Ashen/Ashen/Binaries/Win64
///   - epicgames noise refinement (E3) — IndianaEpicGameStore-Win64-Shipping.exe
///   - BattleNet .patch.result (E2)
/// </summary>
public sealed class PythonParityDetectionTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FolderScanner _scanner;

    public PythonParityDetectionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "PyParity_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _scanner = new FolderScanner();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    // ════════════════════════════════════════════════════════════════
    //  Terminating rule (T1.5) — Neverwinter shape
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_ArcInstallNeverwinter_TerminatingRuleExcludesNestedGameClient()
    {
        // Real corpus shape: neverwinter_en\Neverwinter.exe + nested
        // Neverwinter\Live\x64\GameClient.exe (rel-4) + double-nested duplicate.
        string nw = CreateDir("ArcInstall", "neverwinter_en");
        CreateExe(nw, "Neverwinter.exe");
        CreateExe(CreateDir("ArcInstall", "neverwinter_en", "Neverwinter", "Live", "x64"), "GameClient.exe");
        CreateExe(CreateDir("ArcInstall", "neverwinter_en", "Neverwinter_en", "Neverwinter", "Live", "x64"), "GameClient.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        // The wrapper (ArcInstall) is a container; the child neverwinter_en is the game.
        var nwEntry = entries.FirstOrDefault(e =>
            e.FolderName.Contains("neverwinter", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(nwEntry);
        Assert.Contains("Neverwinter.exe", nwEntry.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(entries, e =>
            e.ExecutablePath.Contains("GameClient.exe", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Scan_NeverwinterDirect_TerminatingRuleResolvesRootExe()
    {
        // Scan the game folder directly as a root (probe parity).
        string nw = CreateDir("neverwinter_en");
        CreateExe(nw, "Neverwinter.exe");
        CreateExe(CreateDir("neverwinter_en", "Neverwinter", "Live", "x64"), "GameClient.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        var entry = entries.FirstOrDefault(e =>
            e.FolderName.Contains("neverwinter", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(entry);
        Assert.Contains("Neverwinter.exe", entry.ExecutablePath, StringComparison.OrdinalIgnoreCase);
    }

    // ════════════════════════════════════════════════════════════════
    //  Whole-name match (GAP A)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_DiabloIII_WholeNameMatchWinsOverNestedBackup()
    {
        // Diablo III.exe matches folder "Diablo III" (whole-name); the nested
        // x64\Diablo III64.exe and x64 - Copy backup must NOT be primary.
        string diablo = CreateDir("Diablo III");
        CreateExe(diablo, "Diablo III.exe");
        CreateExe(CreateDir("Diablo III", "x64"), "Diablo III64.exe");
        CreateExe(CreateDir("Diablo III", "x64 - Copy"), "Diablo III64.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        var entry = entries.FirstOrDefault(e =>
            e.FolderName.Contains("Diablo", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(entry);
        Assert.Contains("Diablo III.exe", entry.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(entry.ExecutablePath, "x64");
    }

    [Fact]
    public void Scan_DeadSpace3_WholeNameNormalizedMatch()
    {
        // deadspace3.exe (no spaces) matches "Dead Space 3" (whole-name normalized).
        string ds = CreateDir("Dead Space 3");
        CreateExe(ds, "deadspace3.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        var entry = entries.FirstOrDefault(e =>
            e.FolderName.Contains("Dead Space", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(entry);
        Assert.Contains("deadspace3.exe", entry.ExecutablePath, StringComparison.OrdinalIgnoreCase);
    }

    // ════════════════════════════════════════════════════════════════
    //  Backup exclusion guard (E5)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_Penumbra_BackupDashExeNeverTerminates()
    {
        // -Penumbra.exe (backup) must not win; redist/PENUMBRA.EXE is the real exe.
        string pen = CreateDir("Penumbra");
        CreateExe(CreateDir("Penumbra", "redist"), "-Penumbra.exe");
        CreateExe(CreateDir("Penumbra", "redist"), "PENUMBRA.EXE");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        var entry = entries.FirstOrDefault(e =>
            e.FolderName.Contains("Penumbra", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(entry);
        Assert.Contains("PENUMBRA.EXE", entry.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(entry.ExecutablePath, "-Penumbra.exe", StringComparison.OrdinalIgnoreCase);
    }

    // ════════════════════════════════════════════════════════════════
    //  UE-wrapped containers (GAP B) — Ashen shape
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_AshenUeWrap_ContainerResolvesInnerGame()
    {
        // R1_Ashen\Ashen\Binaries\Win64\Ashen-Win64-Shipping.exe — the wrapper
        // is a container; the inner Ashen is the game (UE exe found via deep scan).
        string wrapper = CreateDir("R1_Ashen", "Ashen");
        CreateExe(CreateDir("R1_Ashen", "Ashen", "Binaries", "Win64"), "Ashen-Win64-Shipping.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        Assert.Contains(entries, e =>
            e.ExecutablePath.Contains("Ashen-Win64-Shipping.exe", StringComparison.OrdinalIgnoreCase));
    }

    // ════════════════════════════════════════════════════════════════
    //  epicgames noise refinement (E3)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void IsNoiseExeName_EpicSdkGameExe_IsNotNoise()
    {
        // IndianaEpicGameStore-Win64-Shipping.exe references the Epic SDK in its
        // name but is a REAL game exe — must NOT be noise.
        Assert.False(FileSystemHelper.IsNoiseExeName(
            "IndianaEpicGameStore-Win64-Shipping.exe", FolderScanner.DefaultNoiseExePatterns));
    }

    [Fact]
    public void IsNoiseExeName_EpicLauncher_IsNoise()
    {
        Assert.True(FileSystemHelper.IsNoiseExeName(
            "EpicGamesLauncher.exe", FolderScanner.DefaultNoiseExePatterns));
        Assert.True(FileSystemHelper.IsNoiseExeName(
            "EpicGamesUpdater.exe", FolderScanner.DefaultNoiseExePatterns));
    }

    // ════════════════════════════════════════════════════════════════
    //  BattleNet .patch.result (E2)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_DiabloImmortal_PatchResult_ClassifiedAsBattleNet()
    {
        // Real corpus shape: Diablo Immortal has .patch.result + .product.db.
        string di = CreateDir("Diablo Immortal");
        File.WriteAllText(Path.Combine(di, ".patch.result"), "result");
        File.WriteAllText(Path.Combine(di, ".product.db"), "db");
        CreateExe(di, "Diablo Immortal.exe");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        var entry = entries.FirstOrDefault(e =>
            e.FolderName.Contains("Diablo", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(entry);
        Assert.Equal(GameSourceKind.BattleNet, entry.GameSource);
    }

    // ════════════════════════════════════════════════════════════════
    //  Non-game folder layers 2-3
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Scan_DataOnlyFolderWithOnlyManuals_Skipped()
    {
        // Folder with only manuals/docs — Layer 3 file-type analysis rejects it.
        string data = CreateDir("Publisher", "DataFolder");
        File.WriteAllText(Path.Combine(data, "readme.txt"), "hello");
        File.WriteAllText(Path.Combine(data, "manual.pdf"), "pdf");

        var entries = _scanner.Scan(_tempDir, GameSourceKind.Standalone);

        Assert.DoesNotContain(entries, e =>
            e.FolderName.Contains("DataFolder", StringComparison.OrdinalIgnoreCase));
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