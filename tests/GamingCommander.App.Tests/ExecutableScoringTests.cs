using GamingCommander.App.Services;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for ExecutableDiscovery.ScoreExecutable — ranks exe candidates
/// based on folder-name matching, launcher penalties, noise pattern penalties,
/// shipping bonuses, backup variants, and file size.
/// </summary>
public sealed class ExecutableScoringTests : IDisposable
{
    private readonly string _tempDir;

    public ExecutableScoringTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ExeScoringTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    // ════════════════════════════════════════════════════════════════
    //  Folder-name matching
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("system", "elex.exe", "elex", "system", "crashreporter.exe")]
    [InlineData("", "heroesiv.exe", "Heroes 4", "", "other.exe")]
    public void ScoreExecutable_FolderNameMatch_BeatsUnrelated(
        string matchDir, string matchExe, string folderName, string otherDir, string otherExe)
    {
        string match = CreateExeIn(matchDir, matchExe);
        string other = CreateExeIn(otherDir, otherExe);

        int matchScore = ExecutableDiscovery.ScoreExecutable(match, folderName, [], [], _ => 999).Score;
        int otherScore = ExecutableDiscovery.ScoreExecutable(other, folderName, [], [], _ => 999).Score;

        Assert.True(matchScore > otherScore, $"{matchExe} ({matchScore}) vs {otherExe} ({otherScore})");
    }

    [Theory]
    [InlineData("Game.exe", "My Game")]
    public void ScoreExecutable_ExactOrTokenNameMatch_AddsBonus(string exeName, string folderName)
    {
        string exe = CreateTempExe(exeName);

        int score = ExecutableDiscovery.ScoreExecutable(exe, folderName, [], [], _ => 999).Score;

        Assert.True(score > 0, $"Expected positive score, got {score}");
    }

    // ════════════════════════════════════════════════════════════════
    //  Launcher penalty and safety predicates
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ScoreExecutable_LauncherPattern_Penalizes()
    {
        string launcher = CreateTempExe("GameLauncher.exe");
        string game = CreateTempExe("MyGame.exe");

        var launcherPatterns = new List<string> { "launcher" };
        int launcherScore = ExecutableDiscovery.ScoreExecutable(
            launcher, "MyGame", launcherPatterns, [], _ => 999).Score;
        int gameScore = ExecutableDiscovery.ScoreExecutable(
            game, "MyGame", launcherPatterns, [], _ => 999).Score;

        Assert.True(gameScore > launcherScore,
            $"Game exe ({gameScore}) should score higher than launcher ({launcherScore})");
    }

    [Fact]
    public void IsForbiddenLaunchExe_Uninstallers()
    {
        Assert.True(ExecutableDiscovery.IsForbiddenLaunchExe(@"D:\games\foo\unins000.exe"));
        Assert.True(ExecutableDiscovery.IsForbiddenLaunchExe("Unins001.exe"));
        Assert.True(ExecutableDiscovery.IsForbiddenLaunchExe("Uninstallation.exe"));
        Assert.False(ExecutableDiscovery.IsForbiddenLaunchExe("sky.exe"));
        Assert.False(ExecutableDiscovery.IsForbiddenLaunchExe(""));
        Assert.False(ExecutableDiscovery.IsForbiddenLaunchExe(null));
    }

    [Fact]
    public void FindPrimaryExecutable_DoesNotFallBackToUnins000()
    {
        string dir = Path.Combine(_tempDir, "bass");
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, "unins000.exe"), new byte[2 * 1024 * 1024]);
        File.WriteAllBytes(Path.Combine(dir, "scummvm.exe"), new byte[200 * 1024]);

        var result = ExecutableDiscovery.FindPrimaryExecutable(
            new DirectoryInfo(dir),
            Directory.GetFiles(dir, "*.exe"),
            ["unins", "scummvm"],
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            ["launcher"]);

        Assert.Null(result.ExePath);
        Assert.DoesNotContain(result.Candidates,
            p => Path.GetFileName(p).StartsWith("unins", StringComparison.OrdinalIgnoreCase));
    }

    // ════════════════════════════════════════════════════════════════
    //  Noise pattern penalties (tier-based)
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("unins000.exe", "unins", 1, true)]
    [InlineData("epicgameslauncher.exe", "epicgameslauncher", 21, false)]
    public void ScoreExecutable_NoisePatternTier_Penalizes(
        string noiseExe, string pattern, int tier, bool highSeverity)
    {
        string noise = CreateTempExe(noiseExe);
        string game = CreateTempExe("MyGame.exe");

        var noisePatterns = new List<string> { pattern };
        int noiseScore = ExecutableDiscovery.ScoreExecutable(
            noise, "MyGame", [], noisePatterns, _ => tier).Score;
        int gameScore = ExecutableDiscovery.ScoreExecutable(
            game, "MyGame", [], noisePatterns, _ => 999).Score;

        Assert.True(gameScore > noiseScore,
            $"Game exe ({gameScore}) should score higher than noise ({noiseScore})");
        // Tier 1-5 gets the -30 penalty; a low-severity tier does not.
        Assert.Equal(highSeverity, noiseScore <= -30);
    }

    // ════════════════════════════════════════════════════════════════
    //  Shipping / Win64 bonus
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ScoreExecutable_Win64Shipping_BeatsRootGameExe()
    {
        string root = Path.Combine(_tempDir, "MyGame");
        Directory.CreateDirectory(Path.Combine(root, "Binaries", "Win64"));
        string stub = Path.Combine(root, "game.exe");
        string shipping = Path.Combine(root, "Binaries", "Win64", "MyGame-Win64-Shipping.exe");
        File.WriteAllBytes(stub, new byte[200 * 1024]);
        File.WriteAllBytes(shipping, new byte[200 * 1024]);

        int stubScore = ExecutableDiscovery.ScoreExecutable(stub, "MyGame", [], [], _ => 999).Score;
        int shipScore = ExecutableDiscovery.ScoreExecutable(shipping, "MyGame", [], [], _ => 999).Score;

        Assert.True(shipScore > stubScore,
            $"Win64-Shipping ({shipScore}) should beat root game.exe ({stubScore})");
    }

    [Theory]
    [InlineData("MyGame-Win64-Shipping.exe", "MyGame-Regular.exe")]
    public void ScoreExecutable_ShippingOrWin64Binary_AddsBonus(string bonusExe, string regularExe)
    {
        string bonus = CreateTempExe(bonusExe);
        string regular = CreateTempExe(regularExe);

        int bonusScore = ExecutableDiscovery.ScoreExecutable(bonus, "MyGame", [], [], _ => 999).Score;
        int regularScore = ExecutableDiscovery.ScoreExecutable(regular, "MyGame", [], [], _ => 999).Score;

        Assert.True(bonusScore > regularScore,
            $"{bonusExe} ({bonusScore}) should score higher than {regularExe} ({regularScore})");
    }

    // ════════════════════════════════════════════════════════════════
    //  Edge cases and result contract
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ScoreExecutable_EmptyFolderName_ReturnsBaseScore()
    {
        string exe = CreateTempExe("MyGame.exe");

        int score = ExecutableDiscovery.ScoreExecutable(exe, "", [], [], _ => 999).Score;

        // No folder context → no token match bonus, but the score is still valid.
        Assert.True(score >= 0, $"Expected non-negative score, got {score}");
    }

    [Fact]
    public void ScoreExecutable_ReturnsFileDescription_InResult()
    {
        // Temp exes have no PE headers, so FileDescription will be null.
        string exe = CreateTempExe("MyGame.exe");

        var result = ExecutableDiscovery.ScoreExecutable(exe, "MyGame", [], [], _ => 999);

        Assert.IsType<ExecutableDiscovery.ExeScoreResult>(result);
        Assert.True(result.Score > 0);
        Assert.Null(result.FileDescription);
    }

    // ════════════════════════════════════════════════════════════════
    //  File size bands
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ScoreExecutable_FileSizeBands_OrderBySize()
    {
        string veryLarge = CreateTempExeWithSize("a.exe", 100 * 1024 * 1024); // >= 100MB
        string large = CreateTempExeWithSize("b.exe", 600 * 1024);            // >= 500KB
        string medium = CreateTempExeWithSize("c.exe", 300 * 1024);           // 100–500KB
        string tiny = CreateTempExeWithSize("d.exe", 50 * 1024);              // < 100KB

        int veryLargeScore = Score(veryLarge);
        int largeScore = Score(large);
        int mediumScore = Score(medium);
        int tinyScore = Score(tiny);

        Assert.True(veryLargeScore > largeScore, $"{veryLargeScore} vs {largeScore}");
        Assert.True(largeScore > mediumScore, $"{largeScore} vs {mediumScore}");
        Assert.True(mediumScore > tinyScore, $"{mediumScore} vs {tinyScore}");

        static int Score(string exe) =>
            ExecutableDiscovery.ScoreExecutable(exe, "X", [], [], _ => 999).Score;
    }

    // ════════════════════════════════════════════════════════════════
    //  Backup / org / crack / tool penalties
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("copy of MyGame.exe")]
    [InlineData("org_MyGame.exe")]
    [InlineData("MyGame_crack.exe")]
    public void ScoreExecutable_BackupVariant_Penalized(string variantName)
    {
        string original = CreateTempExeWithSize("MyGame.exe", 600_000);
        string variant = CreateTempExeWithSize(variantName, 600_000);

        int originalScore = ExecutableDiscovery.ScoreExecutable(
            original, "MyGame", [], [], _ => 999).Score;
        int variantScore = ExecutableDiscovery.ScoreExecutable(
            variant, "MyGame", [], [], _ => 999).Score;

        Assert.True(originalScore > variantScore,
            $"Original ({originalScore}) should score higher than {variantName} ({variantScore})");
    }

    // ════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════

    private string CreateExeIn(string relativeDir, string fileName)
    {
        string dir = relativeDir.Length == 0
            ? _tempDir
            : Path.Combine(_tempDir, relativeDir);
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, fileName);
        File.WriteAllBytes(path, new byte[200 * 1024]);
        return path;
    }

    private string CreateTempExe(string fileName)
    {
        string path = Path.Combine(_tempDir, fileName);
        File.WriteAllBytes(path, new byte[200 * 1024]);
        return path;
    }

    private string CreateTempExeWithSize(string fileName, long sizeBytes)
    {
        string path = Path.Combine(_tempDir, fileName);
        byte[] data = new byte[Math.Min(sizeBytes, 1024 * 1024)];
        File.WriteAllBytes(path, data);
        if (sizeBytes > data.Length)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Write);
            fs.SetLength(sizeBytes);
        }
        return path;
    }
}
