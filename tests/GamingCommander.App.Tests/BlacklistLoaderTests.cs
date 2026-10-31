using GamingCommander.App.Services;

namespace GamingCommander.App.Tests;

public sealed class BlacklistLoaderTests : IDisposable
{
    private readonly string _tempDir;

    public BlacklistLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "BlacklistLoaderTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    // ════════════════════════════════════════════════════════════════
    //  Loading
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Load_WithValidFile_PopulatesAllSections()
    {
        WriteBlacklistJson("""
            {
                "exe_name_patterns": {
                    "tier_1_universal_noise": ["unins", "setup"],
                    "tier_2_launcher_stubs": ["launcher"]
                },
                "directory_patterns": {
                    "patterns": ["__installer", "redist"]
                },
                "pe_metadata_blacklist": {
                    "patterns": ["cheat"]
                },
                "pcgw_page_title_noise": {
                    "patterns": ["stub"]
                }
            }
            """);

        var data = new BlacklistLoader(_tempDir).Load();

        Assert.Contains("unins", data.ExeNamePatterns);
        Assert.Contains("setup", data.ExeNamePatterns);
        Assert.Contains("__installer", data.DirectoryPatterns);
        Assert.Contains("redist", data.DirectoryPatterns);
        Assert.NotEmpty(data.PeMetadataPatterns);
        Assert.NotEmpty(data.PcgwTitleNoise);
    }

    [Fact]
    public void Load_WithMissingFile_ReturnsEmbeddedDefaultsAndRestoresFile()
    {
        // The loader only creates the parent when it is missing and inside the app data root.
        // A test-owned directory already exists, so restoring into it is allowed.
        Directory.CreateDirectory(Path.Combine(_tempDir, "data"));

        var data = new BlacklistLoader(_tempDir).Load();

        // Bug 16: missing file → embedded default resource, never silently empty.
        Assert.NotEmpty(data.ExeNamePatterns);
        Assert.NotEmpty(data.TieredExePatterns);
        Assert.NotEmpty(data.DirectoryPatterns);

        // The embedded default should have been restored to disk for future loads.
        string restored = Path.Combine(_tempDir, "data", "blacklist.json");
        Assert.True(File.Exists(restored));
        Assert.False(string.IsNullOrWhiteSpace(File.ReadAllText(restored)));
    }

    [Fact]
    public void Load_WithEmptyFile_ReturnsDefaults()
    {
        WriteBlacklistJson("{}");

        var data = new BlacklistLoader(_tempDir).Load();

        Assert.Empty(data.ExeNamePatterns);
    }

    [Fact]
    public void Load_WithCorruptFile_ReturnsEmbeddedDefaults()
    {
        WriteBlacklistJson("not valid json {{{");

        var data = new BlacklistLoader(_tempDir).Load();

        // Bug 16: corrupt file → embedded default resource instead of empty filtering.
        Assert.NotEmpty(data.ExeNamePatterns);
        Assert.NotEmpty(data.TieredExePatterns);
    }

    [Fact]
    public void Load_WithMissingDirectory_ReturnsEmbeddedDefaults()
    {
        string nonexistent = Path.Combine(_tempDir, "nonexistent", "path");

        var data = new BlacklistLoader(nonexistent).Load();

        // Bug 16: should not throw; embedded default keeps filtering active.
        Assert.NotEmpty(data.ExeNamePatterns);
        // The guard refuses to create a directory outside the app data root.
        Assert.False(Directory.Exists(Path.Combine(nonexistent, "data")));
    }

    // ════════════════════════════════════════════════════════════════
    //  Tier keys and preservation
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Load_WithAllTiers_AllJsonKeysDeserialized()
    {
        // All 20 JSON tier keys must map to the C# DTO (catches JsonPropertyName drift).
        string[] tierKeys =
        [
            "tier_1_universal_noise", "tier_2_launcher_stubs", "tier_3_store_bootstraps",
            "tier_4_anticheat_drm", "tier_5_error_crash_reporting", "tier_6_drm_wrappers",
            "tier_7_installer_utilities", "tier_8_server_loader_stub", "tier_9_distribution_tools",
            "tier_10_dev_editor_tools", "tier_11_utilities_debug", "tier_12_trial_demo_stub",
            "tier_13_media_codec_tools", "tier_14_installer_frameworks", "tier_15_runtime_interpreters",
            "tier_16_web_ui_overlay", "tier_17_repair_service_helper", "tier_18_unreal_build_tools",
            "tier_19_patch_update", "tier_20_utility_tools",
        ];
        string entries = string.Join(",", tierKeys.Select(k => $"\"{k}\":[\"p{k.Split('_')[1]}\"]"));
        WriteBlacklistJson($"{{\"exe_name_patterns\":{{{entries}}}}}");

        var data = new BlacklistLoader(_tempDir).Load();

        Assert.Equal(20, data.TieredExePatterns.Select(t => t.Tier).Distinct().Count());
        Assert.All(data.TieredExePatterns, t => Assert.InRange(t.Tier, 1, 20));
    }

    [Theory]
    [InlineData("tier_5_error_crash_reporting", 5, "crash")]
    [InlineData("tier_18_unreal_build_tools", 18, "unrealpak")]
    public void Load_TierKey_MapsToExpectedTier(string jsonKey, int expectedTier, string pattern)
    {
        WriteBlacklistJson($"{{\"exe_name_patterns\":{{\"{jsonKey}\":[\"{pattern}\"]}}}}");

        var data = new BlacklistLoader(_tempDir).Load();

        var entry = Assert.Single(data.TieredExePatterns);
        Assert.Equal(expectedTier, entry.Tier);
        Assert.Equal(pattern, entry.Pattern);
        Assert.Contains(pattern, data.ExeNamePatterns);
    }

    [Fact]
    public void Load_WithValidFile_TieredPatternsPreserved()
    {
        WriteBlacklistJson("""
            {
                "exe_name_patterns": {
                    "tier_1_universal_noise": ["unins", "setup"],
                    "tier_2_launcher_stubs": ["launcher"],
                    "tier_3_store_bootstraps": ["epicgameslauncher"]
                }
            }
            """);

        var data = new BlacklistLoader(_tempDir).Load();

        // 2 tier-1 + 1 tier-2 + 1 tier-3 = 4 entries
        Assert.Equal(4, data.TieredExePatterns.Count);
        Assert.Equal(3, data.TieredExePatterns.Select(t => t.Tier).Distinct().Count());

        var tier1 = data.TieredExePatterns.Where(t => t.Tier == 1).ToList();
        Assert.Equal(2, tier1.Count);
        Assert.Contains(tier1, t => t.Pattern == "unins");
        Assert.Contains(tier1, t => t.Pattern == "setup");

        var tier2 = Assert.Single(data.TieredExePatterns.Where(t => t.Tier == 2));
        Assert.Equal("launcher", tier2.Pattern);
    }

    [Fact]
    public void Load_WithValidFile_FlatListContainsAllTieredPatterns()
    {
        WriteBlacklistJson("""
            {
                "exe_name_patterns": {
                    "tier_1_universal_noise": ["unins", "setup"],
                    "tier_2_launcher_stubs": ["launcher"]
                }
            }
            """);

        var data = new BlacklistLoader(_tempDir).Load();

        Assert.Equal(data.TieredExePatterns.Count, data.ExeNamePatterns.Count);
        foreach (var entry in data.TieredExePatterns)
            Assert.Contains(entry.Pattern, data.ExeNamePatterns);
    }

    // ── Helpers ───────────────────────────────────────────────

    private void WriteBlacklistJson(string json)
    {
        string dataDir = Path.Combine(_tempDir, "data");
        Directory.CreateDirectory(dataDir);
        File.WriteAllText(Path.Combine(dataDir, "blacklist.json"), json);
    }
}
