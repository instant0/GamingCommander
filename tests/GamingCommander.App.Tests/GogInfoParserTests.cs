using GamingCommander.App.Services;
using Xunit;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for GogInfoParser — GOG goggame-*.info JSON parsing, DLC filtering, and path resolution.
/// </summary>
public sealed class GogInfoParserTests : IDisposable
{
    private readonly string _tempDir;
    private static readonly IReadOnlySet<string> EmptyNoiseDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public GogInfoParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "GogInfoTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private static void WriteInfoFile(string dirPath, string gameId, string rootGameId, string name,
        string? exePath = null, string? exeArgs = null, bool isPrimary = true)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"gameId\": \"{gameId}\",");
        sb.AppendLine($"  \"rootGameId\": \"{rootGameId}\",");
        sb.AppendLine($"  \"name\": \"{name}\"");
        if (exePath is not null)
        {
            sb.AppendLine("  ,\"playTasks\": [");
            string argsPart = exeArgs is not null ? $", \"arguments\": \"{exeArgs}\"" : "";
            sb.AppendLine($"    {{\"isPrimary\": {isPrimary.ToString().ToLower()}, \"path\": \"{exePath}\"{argsPart}}}");
            sb.AppendLine("  ]");
        }
        else
        {
            sb.AppendLine("  ,\"playTasks\": []");
        }
        sb.AppendLine("}");

        File.WriteAllText(Path.Combine(dirPath, $"goggame-{gameId}.info"), sb.ToString());
    }

    // ════════════════════════════════════════════════════════════════
    //  Basic parsing
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TryParse_MainGame_ExtractsMetadata()
    {
        WriteInfoFile(_tempDir, "12345", "12345", "The Witcher 3",
            exePath: "bin/x64/witcher3.exe", exeArgs: "--skip-menu");

        bool found = GogInfoParser.TryParse(
            new DirectoryInfo(_tempDir), EmptyNoiseDirs, out var info);

        Assert.True(found);
        Assert.NotNull(info);
        Assert.Equal("The Witcher 3", info.Title);
        Assert.Equal("12345", info.GameId);
        Assert.Contains("witcher3.exe", info.ExePath);
        Assert.Equal("--skip-menu", info.LaunchArgs);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TryParse_PathIsResolved(bool absolute)
    {
        string relativeLeaf = Path.Combine("bin", "x64", "game.exe");
        string absolutePath = Path.Combine(_tempDir, "game.exe");
        WriteInfoFile(_tempDir, "12345", "12345", "Game",
            exePath: absolute ? absolutePath : "bin/x64/game.exe");

        GogInfoParser.TryParse(
            new DirectoryInfo(_tempDir), EmptyNoiseDirs, out var info);

        Assert.NotNull(info);
        if (absolute)
        {
            Assert.Equal(absolutePath, info.ExePath);
        }
        else
        {
            Assert.True(Path.IsPathRooted(info.ExePath), $"Expected absolute path, got: {info.ExePath}");
            Assert.EndsWith(relativeLeaf, info.ExePath);
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  DLC filtering
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TryParse_DlcInfo_SkippedWhenMainExists()
    {
        // Main game
        WriteInfoFile(_tempDir, "12345", "12345", "The Witcher 3",
            exePath: "witcher3.exe");
        // DLC — different gameId
        WriteInfoFile(_tempDir, "12346", "12345", "Hearts of Stone",
            exePath: "dlc.exe");

        GogInfoParser.TryParse(
            new DirectoryInfo(_tempDir), EmptyNoiseDirs, out var info);

        Assert.NotNull(info);
        Assert.Equal("The Witcher 3", info.Title);
        Assert.Equal("12345", info.GameId);
        Assert.Contains("witcher3.exe", info.ExePath);
    }

    [Fact]
    public void TryParse_OnlyDlcInfo_UsesDlcAsFallback()
    {
        // Only DLC .info exists (no main game)
        WriteInfoFile(_tempDir, "12346", "12345", "Hearts of Stone",
            exePath: "dlc.exe");

        GogInfoParser.TryParse(
            new DirectoryInfo(_tempDir), EmptyNoiseDirs, out var info);

        Assert.NotNull(info);
        Assert.Equal("Hearts of Stone", info.Title);
        Assert.Equal("12346", info.GameId);
    }

    // ════════════════════════════════════════════════════════════════
    //  Edge cases
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void TryParse_NoUsableInfo_ReturnsFalse()
    {
        bool found = GogInfoParser.TryParse(
            new DirectoryInfo(_tempDir), EmptyNoiseDirs, out var info);

        Assert.False(found);
        Assert.Null(info);

        File.WriteAllText(Path.Combine(_tempDir, "goggame-12345.info"), "{ broken json");

        found = GogInfoParser.TryParse(
            new DirectoryInfo(_tempDir), EmptyNoiseDirs, out var malformed);

        Assert.False(found);
        Assert.Null(malformed);
    }

    [Fact]
    public void TryParse_ExeFallback_NoTasksThenNoPrimary()
    {
        // Metadata only: no playTasks at all.
        File.WriteAllText(Path.Combine(_tempDir, "goggame-12345.info"), """
        {
            "gameId": "12345",
            "rootGameId": "12345",
            "name": "Game Without Exe"
        }
        """);

        bool found = GogInfoParser.TryParse(
            new DirectoryInfo(_tempDir), EmptyNoiseDirs, out var noExe);

        Assert.True(found);
        Assert.NotNull(noExe);
        Assert.Equal("Game Without Exe", noExe.Title);
        Assert.Equal("12345", noExe.GameId);
        Assert.Empty(noExe.ExePath);

        // No primary task: first task with a path wins.
        File.WriteAllText(Path.Combine(_tempDir, "goggame-12345.info"), """
        {
            "gameId": "12345",
            "rootGameId": "12345",
            "name": "Game",
            "playTasks": [
                { "isPrimary": false, "path": "game.exe", "arguments": "--windowed" }
            ]
        }
        """);

        GogInfoParser.TryParse(
            new DirectoryInfo(_tempDir), EmptyNoiseDirs, out var firstWithPath);

        Assert.NotNull(firstWithPath);
        Assert.Contains("game.exe", firstWithPath.ExePath);
        Assert.Equal("--windowed", firstWithPath.LaunchArgs);
    }

    [Fact]
    public void TryParse_SubdirectoryInfo_Parsed()
    {
        string subDir = Path.Combine(_tempDir, "subdir");
        Directory.CreateDirectory(subDir);
        WriteInfoFile(subDir, "12345", "12345", "Game In Subdir",
            exePath: "game.exe");

        GogInfoParser.TryParse(
            new DirectoryInfo(_tempDir), EmptyNoiseDirs, out var info);

        Assert.NotNull(info);
        Assert.Equal("Game In Subdir", info.Title);
    }
}
