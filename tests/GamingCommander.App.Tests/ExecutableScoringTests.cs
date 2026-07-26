using GamingCommander.App.Services;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for ExecutableDiscovery.ScoreExecutable — ranks exe candidates
/// based on folder-name matching, launcher penalties, noise pattern penalties,
/// shipping bonuses, and file size.
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
    //  Folder-Name Matching
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ScoreExecutable_ExactNameMatch_AddsBonus()
    {
        string exe = CreateTempExe("MyGame.exe");
        int score = ExecutableDiscovery.ScoreExecutable(
            exe, "MyGame", [], [], _ => 999).Score;

        // "mygame" contains token "mygame" → +10
        Assert.True(score > 0, $"Expected positive score, got {score}");
    }

    [Fact]
    public void ScoreExecutable_TokenMatch_AddsBonus()
    {
        string exe = CreateTempExe("Game.exe");
        int score = ExecutableDiscovery.ScoreExecutable(
            exe, "My Game", [], [], _ => 999).Score;

        // "My Game" splits into tokens ["my", "game"] → "game" matches → +10
        Assert.True(score > 0, $"Expected positive score, got {score}");
    }

    // ════════════════════════════════════════════════════════════════
    //  Launcher Penalties
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

        // Launcher should score lower than game exe
        Assert.True(gameScore > launcherScore,
            $"Game exe ({gameScore}) should score higher than launcher ({launcherScore})");
    }

    // ════════════════════════════════════════════════════════════════
    //  Noise Pattern Penalties (Tier-Based)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ScoreExecutable_HighTierNoisePattern_HeavilyPenalizes()
    {
        string uninstaller = CreateTempExe("unins000.exe");
        string game = CreateTempExe("MyGame.exe");

        var noisePatterns = new List<string> { "unins" };
        // Tier 1 = highest severity
        int noiseScore = ExecutableDiscovery.ScoreExecutable(
            uninstaller, "MyGame", [], noisePatterns, _ => 1).Score;
        int gameScore = ExecutableDiscovery.ScoreExecutable(
            game, "MyGame", [], noisePatterns, _ => 999).Score;

        // Noise pattern should score much lower
        Assert.True(gameScore > noiseScore,
            $"Game exe ({gameScore}) should score higher than noise ({noiseScore})");
    }

    [Fact]
    public void ScoreExecutable_LowTierNoisePattern_LightPenalty()
    {
        string bootstrap = CreateTempExe("epicgameslauncher.exe");
        string game = CreateTempExe("MyGame.exe");

        var noisePatterns = new List<string> { "epicgameslauncher" };
        // Tier 21 = lowest severity
        int bootstrapScore = ExecutableDiscovery.ScoreExecutable(
            bootstrap, "MyGame", [], noisePatterns, _ => 21).Score;
        int gameScore = ExecutableDiscovery.ScoreExecutable(
            game, "MyGame", [], noisePatterns, _ => 999).Score;

        // Both should have some score, but game should be higher
        Assert.True(gameScore > bootstrapScore,
            $"Game exe ({gameScore}) should score higher than bootstrap ({bootstrapScore})");
    }

    // ════════════════════════════════════════════════════════════════
    //  Shipping Bonus
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ScoreExecutable_ShippingBinary_AddsBonus()
    {
        string shipping = CreateTempExe("MyGame-Win64-Shipping.exe");
        string regular = CreateTempExe("MyGame-Regular.exe");

        int shippingScore = ExecutableDiscovery.ScoreExecutable(
            shipping, "MyGame", [], [], _ => 999).Score;
        int regularScore = ExecutableDiscovery.ScoreExecutable(
            regular, "MyGame", [], [], _ => 999).Score;

        // Shipping binary should score higher due to +5 bonus
        Assert.True(shippingScore > regularScore,
            $"Shipping exe ({shippingScore}) should score higher than regular ({regularScore})");
    }

    [Fact]
    public void ScoreExecutable_Win64Binary_AddsBonus()
    {
        string win64 = CreateTempExe("MyGame-Win64.exe");
        string regular = CreateTempExe("MyGame.exe");

        int win64Score = ExecutableDiscovery.ScoreExecutable(
            win64, "MyGame", [], [], _ => 999).Score;
        int regularScore = ExecutableDiscovery.ScoreExecutable(
            regular, "MyGame", [], [], _ => 999).Score;

        // Win64 binary should score higher due to +5 bonus
        Assert.True(win64Score > regularScore,
            $"Win64 exe ({win64Score}) should score higher than regular ({regularScore})");
    }

    // ════════════════════════════════════════════════════════════════
    //  File Size Bonus
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ScoreExecutable_LargeExe_AddsBonus()
    {
        string large = CreateTempExeWithSize("MyGame.exe", 100 * 1024 * 1024); // 100MB
        string small = CreateTempExeWithSize("MyGameSmall.exe", 1024); // 1KB

        int largeScore = ExecutableDiscovery.ScoreExecutable(
            large, "MyGame", [], [], _ => 999).Score;
        int smallScore = ExecutableDiscovery.ScoreExecutable(
            small, "MyGame", [], [], _ => 999).Score;

        // Large exe should score higher due to file size bonus
        Assert.True(largeScore > smallScore,
            $"Large exe ({largeScore}) should score higher than small ({smallScore})");
    }

    // ════════════════════════════════════════════════════════════════
    //  Edge Cases
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ScoreExecutable_EmptyFolderName_ReturnsBaseScore()
    {
        string exe = CreateTempExe("MyGame.exe");
        int score = ExecutableDiscovery.ScoreExecutable(
            exe, "", [], [], _ => 999).Score;

        // No folder context → no token match bonus
        // But file size bonus may apply
        Assert.True(score >= 0, $"Expected non-negative score, got {score}");
    }

    [Fact]
    public void ScoreExecutable_CombinesAllFactors()
    {
        // Best case: name match + shipping + large file
        string best = CreateTempExeWithSize("MyGame-Win64-Shipping.exe", 100 * 1024 * 1024);
        // Worst case: noise pattern
        string worst = CreateTempExe("unins000.exe");

        var noisePatterns = new List<string> { "unins" };
        int bestScore = ExecutableDiscovery.ScoreExecutable(
            best, "MyGame", [], noisePatterns, _ => 999).Score;
        int worstScore = ExecutableDiscovery.ScoreExecutable(
            worst, "MyGame", [], noisePatterns, _ => 1).Score;

        Assert.True(bestScore > worstScore,
            $"Best exe ({bestScore}) should score higher than worst ({worstScore})");
    }

    // ════════════════════════════════════════════════════════════════
    //  Backup Penalties
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ScoreExecutable_CopyOfExe_Penalizes()
    {
        string original = CreateTempExe("MyGame.exe");
        string copy = CreateTempExe("copy of MyGame.exe");

        int originalScore = ExecutableDiscovery.ScoreExecutable(
            original, "MyGame", [], [], _ => 999).Score;
        int copyScore = ExecutableDiscovery.ScoreExecutable(
            copy, "MyGame", [], [], _ => 999).Score;

        Assert.True(originalScore > copyScore,
            $"Original ({originalScore}) should score higher than copy ({copyScore})");
    }

    [Fact]
    public void ScoreExecutable_DashCopyExe_Penalizes()
    {
        string original = CreateTempExe("MyGame.exe");
        string copy = CreateTempExe("MyGame - Copy.exe");

        int originalScore = ExecutableDiscovery.ScoreExecutable(
            original, "MyGame", [], [], _ => 999).Score;
        int copyScore = ExecutableDiscovery.ScoreExecutable(
            copy, "MyGame", [], [], _ => 999).Score;

        Assert.True(originalScore > copyScore,
            $"Original ({originalScore}) should score higher than dash-copy ({copyScore})");
    }

    [Fact]
    public void ScoreExecutable_OrgPrefixExe_Penalizes()
    {
        string original = CreateTempExe("MyGame.exe");
        string orgCopy = CreateTempExe("org_MyGame.exe");

        int originalScore = ExecutableDiscovery.ScoreExecutable(
            original, "MyGame", [], [], _ => 999).Score;
        int orgScore = ExecutableDiscovery.ScoreExecutable(
            orgCopy, "MyGame", [], [], _ => 999).Score;

        Assert.True(originalScore > orgScore,
            $"Original ({originalScore}) should score higher than org_ prefix ({orgScore})");
    }

    [Fact]
    public void ScoreExecutable_OriginalKeywordExe_Penalizes()
    {
        string original = CreateTempExe("MyGame.exe");
        string originalCopy = CreateTempExe("MyGameOriginal.exe");

        int originalScore = ExecutableDiscovery.ScoreExecutable(
            original, "MyGame", [], [], _ => 999).Score;
        int originalCopyScore = ExecutableDiscovery.ScoreExecutable(
            originalCopy, "MyGame", [], [], _ => 999).Score;

        Assert.True(originalScore > originalCopyScore,
            $"Original ({originalScore}) should score higher than 'original' keyword ({originalCopyScore})");
    }

    // ════════════════════════════════════════════════════════════════
    //  Small Exe Penalty
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ScoreExecutable_TinyExe_LowerThanLarge()
    {
        string tiny = CreateTempExeWithSize("Helper.exe", 50 * 1024); // 50KB
        string large = CreateTempExeWithSize("MyGame.exe", 200 * 1024); // 200KB

        int tinyScore = ExecutableDiscovery.ScoreExecutable(
            tiny, "MyGame", [], [], _ => 999).Score;
        int largeScore = ExecutableDiscovery.ScoreExecutable(
            large, "MyGame", [], [], _ => 999).Score;

        // Tiny exe gets -15 penalty, large doesn't
        Assert.True(largeScore > tinyScore,
            $"Large exe ({largeScore}) should score higher than tiny ({tinyScore})");
    }

    // ════════════════════════════════════════════════════════════════
    //  FileDescription Propagation (Plan 112 Step 2)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ScoreExecutable_ReturnsFileDescription_InResult()
    {
        // Temp exes have no PE headers, so FileDescription will be null.
        // Verifies the return type works and doesn't crash.
        string exe = CreateTempExe("MyGame.exe");
        var result = ExecutableDiscovery.ScoreExecutable(exe, "MyGame", [], [], _ => 999);

        Assert.IsType<ExecutableDiscovery.ExeScoreResult>(result);
        Assert.True(result.Score > 0);
        Assert.Null(result.FileDescription);
    }

    [Fact]
    public void ScoreExecutable_Tier1Noise_SkipsPERead()
    {
        // Plan 112 Step 4C: Tier 1-4 noise skips PE read entirely.
        string noise = CreateTempExe("unins000.exe");
        var noiseResult = ExecutableDiscovery.ScoreExecutable(
            noise, "MyGame", [], ["unins"], _ => 1);

        Assert.True(noiseResult.Score < 0,
            $"Tier 1 noise should have negative score, got {noiseResult.Score}");
    }

    // ── Helpers ───────────────────────────────────────────────

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
