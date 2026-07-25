using System.Text;
using GamingCommander.App.Services;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for LnkParser — extracts exe names from .lnk binary files
/// and resolves actual exe paths via subdir search.
/// </summary>
public sealed class LnkParserTests : IDisposable
{
    private readonly string _tempDir;

    public LnkParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "LnkParserTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    // ════════════════════════════════════════════════════════════════
    //  TryGetExeName — basic extraction
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TryGetExeName_WithEmbeddedExeName_ReturnsTrue()
    {
        // Create a .lnk file with embedded exe name
        string lnkPath = Path.Combine(_tempDir, "Game.lnk");
        byte[] fakeLnk = CreateFakeLnk("Penumbra.exe");
        File.WriteAllBytes(lnkPath, fakeLnk);

        bool result = LnkParser.TryGetExeName(lnkPath, out string? exeName);

        Assert.True(result);
        Assert.Equal("Penumbra.exe", exeName);
    }

    [Fact]
    public void TryGetExeName_WithFullPathContainingExe_ExtractsFilename()
    {
        // .lnk might contain full path like "D:\Games\Penumbra\Binaries\Penumbra.exe"
        string lnkPath = Path.Combine(_tempDir, "Game.lnk");
        byte[] fakeLnk = CreateFakeLnk("D:\\Games\\Penumbra\\Binaries\\Penumbra.exe");
        File.WriteAllBytes(lnkPath, fakeLnk);

        bool result = LnkParser.TryGetExeName(lnkPath, out string? exeName);

        Assert.True(result);
        // Regex captures the exe filename from the path
        Assert.Equal("Penumbra.exe", exeName);
    }

    [Fact]
    public void TryGetExeName_WithMultipleCandidates_PicksLongest()
    {
        // Multiple exe names — should pick the longest (most likely the game)
        string lnkPath = Path.Combine(_tempDir, "Game.lnk");
        byte[] fakeLnk = CreateFakeLnk("setup.exe and MyAwesomeGame.exe");
        File.WriteAllBytes(lnkPath, fakeLnk);

        bool result = LnkParser.TryGetExeName(lnkPath, out string? exeName);

        Assert.True(result);
        Assert.Equal("MyAwesomeGame.exe", exeName);
    }

    [Fact]
    public void TryGetExeName_SkipsDlls_NotGameExe()
    {
        // steam_api64.dll appears in .lnk but isn't a game exe
        string lnkPath = Path.Combine(_tempDir, "Game.lnk");
        byte[] fakeLnk = CreateFakeLnk("steam_api64.dll and Game.exe");
        File.WriteAllBytes(lnkPath, fakeLnk);

        bool result = LnkParser.TryGetExeName(lnkPath, out string? exeName);

        Assert.True(result);
        Assert.Equal("Game.exe", exeName);
    }

    [Fact]
    public void TryGetExeName_Malformed_ReturnsFalse()
    {
        string lnkPath = Path.Combine(_tempDir, "Bad.lnk");
        File.WriteAllBytes(lnkPath, new byte[] { 0x00, 0x01, 0x02, 0x03 });

        bool result = LnkParser.TryGetExeName(lnkPath, out string? exeName);

        Assert.False(result);
        Assert.Null(exeName);
    }

    [Fact]
    public void TryGetExeName_NonexistentFile_ReturnsFalse()
    {
        bool result = LnkParser.TryGetExeName("/nonexistent/file.lnk", out string? exeName);

        Assert.False(result);
        Assert.Null(exeName);
    }

    // ════════════════════════════════════════════════════════════════
    //  ResolveExeFromLnk — exe resolution
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ResolveExeFromLnk_ExactMatch_Resolves()
    {
        // Game/ shortcut.lnk → Game/Game.exe
        string gameDir = Path.Combine(_tempDir, "MyGame");
        Directory.CreateDirectory(gameDir);
        File.WriteAllBytes(Path.Combine(gameDir, "Game.exe"), new byte[1024]);
        File.WriteAllBytes(Path.Combine(gameDir, "shortcut.lnk"), CreateFakeLnk("Game.exe"));

        string? result = LnkParser.ResolveExeFromLnk(new DirectoryInfo(gameDir));

        Assert.NotNull(result);
        Assert.Contains("Game.exe", result);
    }

    [Fact]
    public void ResolveExeFromLnk_NestedExe_Resolves()
    {
        // Game/ shortcut.lnk → Game/Binaries/Win64/Game.exe
        string gameDir = Path.Combine(_tempDir, "Penumbra");
        string binDir = Path.Combine(gameDir, "Binaries", "Win64");
        Directory.CreateDirectory(binDir);
        File.WriteAllBytes(Path.Combine(binDir, "Penumbra.exe"), new byte[1024]);
        File.WriteAllBytes(Path.Combine(gameDir, "Play.lnk"), CreateFakeLnk("Penumbra.exe"));

        string? result = LnkParser.ResolveExeFromLnk(new DirectoryInfo(gameDir));

        Assert.NotNull(result);
        Assert.Contains("Penumbra.exe", result);
        Assert.Contains("Binaries", result);
    }

    [Fact]
    public void ResolveExeFromLnk_BackupRename_MatchesFuzzy()
    {
        // Game/ shortcut.lnk → Game.exe, but file is -Game.exe
        string gameDir = Path.Combine(_tempDir, "BackupGame");
        Directory.CreateDirectory(gameDir);
        File.WriteAllBytes(Path.Combine(gameDir, "-Game.exe"), new byte[1024]);
        File.WriteAllBytes(Path.Combine(gameDir, "Play.lnk"), CreateFakeLnk("Game.exe"));

        string? result = LnkParser.ResolveExeFromLnk(new DirectoryInfo(gameDir));

        Assert.NotNull(result);
        Assert.Contains("-Game.exe", result);
    }

    [Fact]
    public void ResolveExeFromLnk_CopyOf_MatchesFuzzy()
    {
        // Game/ shortcut.lnk → Game.exe, but file is "copy of Game.exe"
        string gameDir = Path.Combine(_tempDir, "CopyGame");
        Directory.CreateDirectory(gameDir);
        File.WriteAllBytes(Path.Combine(gameDir, "copy of Game.exe"), new byte[1024]);
        File.WriteAllBytes(Path.Combine(gameDir, "Play.lnk"), CreateFakeLnk("Game.exe"));

        string? result = LnkParser.ResolveExeFromLnk(new DirectoryInfo(gameDir));

        Assert.NotNull(result);
        Assert.Contains("copy of Game.exe", result);
    }

    [Fact]
    public void ResolveExeFromLnk_NoExeFound_ReturnsNull()
    {
        string gameDir = Path.Combine(_tempDir, "NoExe");
        Directory.CreateDirectory(gameDir);
        File.WriteAllBytes(Path.Combine(gameDir, "Play.lnk"), CreateFakeLnk("Nonexistent.exe"));

        string? result = LnkParser.ResolveExeFromLnk(new DirectoryInfo(gameDir));

        Assert.Null(result);
    }

    [Fact]
    public void ResolveExeFromLnk_NoLnkFiles_ReturnsNull()
    {
        string gameDir = Path.Combine(_tempDir, "NoLnk");
        Directory.CreateDirectory(gameDir);
        File.WriteAllBytes(Path.Combine(gameDir, "Game.exe"), new byte[1024]);

        string? result = LnkParser.ResolveExeFromLnk(new DirectoryInfo(gameDir));

        Assert.Null(result);
    }

    [Fact]
    public void ResolveExeFromLnk_ExactMatchPreferred_OverFuzzy()
    {
        // Both exact and fuzzy exist — exact wins
        string gameDir = Path.Combine(_tempDir, "ExactWins");
        Directory.CreateDirectory(gameDir);
        File.WriteAllBytes(Path.Combine(gameDir, "Game.exe"), new byte[1024]);
        File.WriteAllBytes(Path.Combine(gameDir, "-Game.exe"), new byte[1024]);
        File.WriteAllBytes(Path.Combine(gameDir, "Play.lnk"), CreateFakeLnk("Game.exe"));

        string? result = LnkParser.ResolveExeFromLnk(new DirectoryInfo(gameDir));

        Assert.NotNull(result);
        // Should be the exact match, not the backup
        Assert.Equal("Game.exe", Path.GetFileName(result));
    }

    // ════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a fake .lnk file with embedded exe name.
    /// Real .lnk files have a binary header, but for testing we just
    /// need the exe name to appear as readable text in the bytes.
    /// </summary>
    private static byte[] CreateFakeLnk(string embeddedText)
    {
        // Pad with some binary header bytes, then append the exe name as latin-1 text
        byte[] header = [0x4C, 0x00, 0x00, 0x00, 0x01, 0x14, 0x02, 0x00]; // Fake ShellLink header
        byte[] textBytes = Encoding.Latin1.GetBytes(embeddedText);
        byte[] result = new byte[header.Length + textBytes.Length];
        Buffer.BlockCopy(header, 0, result, 0, header.Length);
        Buffer.BlockCopy(textBytes, 0, result, header.Length, textBytes.Length);
        return result;
    }
}
