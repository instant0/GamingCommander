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
    public void Load_WithValidFile_ReturnsNonEmptyPatterns()
    {
        string json = """
            {
                "exe_name_patterns": {
                    "tier_1_universal_noise": ["unins", "setup"],
                    "tier_2_launcher_stubs": ["launcher"]
                },
                "directory_patterns": {
                    "patterns": ["__installer"]
                },
                "pe_metadata_blacklist": {
                    "patterns": ["cheat"]
                },
                "pcgw_page_title_noise": {
                    "patterns": ["stub"]
                }
            }
            """;
        WriteBlacklistJson(json);

        var loader = new BlacklistLoader(_tempDir);
        var data = loader.Load();

        Assert.NotEmpty(data.ExeNamePatterns);
        Assert.NotEmpty(data.DirectoryPatterns);
        Assert.NotEmpty(data.PeMetadataPatterns);
        Assert.NotEmpty(data.PcgwTitleNoise);
    }

    [Fact]
    public void Load_WithMissingFile_ReturnsEmptyData()
    {
        var loader = new BlacklistLoader(_tempDir);
        var data = loader.Load();

        // No file → returns empty data (BlacklistData.Empty)
        Assert.Empty(data.ExeNamePatterns);
        Assert.Empty(data.TieredExePatterns);
        Assert.Empty(data.DirectoryPatterns);
    }

    [Fact]
    public void Load_WithEmptyFile_ReturnsDefaults()
    {
        WriteBlacklistJson("{}");

        var loader = new BlacklistLoader(_tempDir);
        var data = loader.Load();

        // Empty JSON → no patterns, returns defaults
        Assert.Empty(data.ExeNamePatterns);
    }

    [Fact]
    public void Load_WithCorruptFile_ReturnsDefaults()
    {
        WriteBlacklistJson("not valid json {{{");

        var loader = new BlacklistLoader(_tempDir);
        var data = loader.Load();

        // Corrupt JSON → returns defaults
        Assert.Empty(data.ExeNamePatterns);
    }

    // ════════════════════════════════════════════════════════════════
    //  Pattern Verification
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Load_WithValidFile_ContainsKnownPatterns()
    {
        string json = """
            {
                "exe_name_patterns": {
                    "tier_1_universal_noise": ["unins", "setup", "installer"]
                }
            }
            """;
        WriteBlacklistJson(json);

        var loader = new BlacklistLoader(_tempDir);
        var data = loader.Load();

        Assert.Contains("unins", data.ExeNamePatterns);
        Assert.Contains("setup", data.ExeNamePatterns);
        Assert.Contains("installer", data.ExeNamePatterns);
    }

    [Fact]
    public void Load_WithValidFile_DirectoryPatternsPresent()
    {
        string json = """
            {
                "exe_name_patterns": {
                    "tier_1_universal_noise": ["unins"]
                },
                "directory_patterns": {
                    "patterns": ["__installer", "redist"]
                }
            }
            """;
        WriteBlacklistJson(json);

        var loader = new BlacklistLoader(_tempDir);
        var data = loader.Load();

        Assert.Contains("__installer", data.DirectoryPatterns);
        Assert.Contains("redist", data.DirectoryPatterns);
    }

    // ════════════════════════════════════════════════════════════════
    //  Tier Preservation
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Load_WithValidFile_TieredPatternsPopulated()
    {
        string json = """
            {
                "exe_name_patterns": {
                    "tier_1_universal_noise": ["unins", "setup"],
                    "tier_2_launcher_stubs": ["launcher"],
                    "tier_3_store_bootstraps": ["epicgameslauncher"]
                }
            }
            """;
        WriteBlacklistJson(json);

        var loader = new BlacklistLoader(_tempDir);
        var data = loader.Load();

        Assert.NotEmpty(data.TieredExePatterns);
        // 2 tier-1 + 1 tier-2 + 1 tier-3 = 4 entries
        Assert.Equal(4, data.TieredExePatterns.Count);
        Assert.Equal(3, data.TieredExePatterns.Select(t => t.Tier).Distinct().Count());
    }

    [Fact]
    public void Load_WithValidFile_TierRangeIsValid()
    {
        string json = """
            {
                "exe_name_patterns": {
                    "tier_1_universal_noise": ["unins"],
                    "tier_10_distribution_tools": ["tool"],
                    "tier_21_utility_tools": ["util"]
                }
            }
            """;
        WriteBlacklistJson(json);

        var loader = new BlacklistLoader(_tempDir);
        var data = loader.Load();

        foreach (var entry in data.TieredExePatterns)
        {
            Assert.InRange(entry.Tier, 1, 21);
        }
    }

    [Fact]
    public void Load_WithValidFile_PatternsMatchTiers()
    {
        string json = """
            {
                "exe_name_patterns": {
                    "tier_1_universal_noise": ["unins", "setup"],
                    "tier_2_launcher_stubs": ["launcher"]
                }
            }
            """;
        WriteBlacklistJson(json);

        var loader = new BlacklistLoader(_tempDir);
        var data = loader.Load();

        // Tier 1 patterns
        var tier1 = data.TieredExePatterns.Where(t => t.Tier == 1).ToList();
        Assert.Equal(2, tier1.Count);
        Assert.Contains(tier1, t => t.Pattern == "unins");
        Assert.Contains(tier1, t => t.Pattern == "setup");

        // Tier 2 patterns
        var tier2 = data.TieredExePatterns.Where(t => t.Tier == 2).ToList();
        Assert.Single(tier2);
        Assert.Equal("launcher", tier2[0].Pattern);
    }

    [Fact]
    public void Load_WithValidFile_FlatListContainsAllTieredPatterns()
    {
        string json = """
            {
                "exe_name_patterns": {
                    "tier_1_universal_noise": ["unins", "setup"],
                    "tier_2_launcher_stubs": ["launcher"]
                }
            }
            """;
        WriteBlacklistJson(json);

        var loader = new BlacklistLoader(_tempDir);
        var data = loader.Load();

        // Flat list should contain all patterns from all tiers
        Assert.Equal(data.TieredExePatterns.Count, data.ExeNamePatterns.Count);
        foreach (var entry in data.TieredExePatterns)
        {
            Assert.Contains(entry.Pattern, data.ExeNamePatterns);
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  Error Handling
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Load_WithMissingDirectory_ReturnsDefaults()
    {
        string nonexistent = Path.Combine(_tempDir, "nonexistent", "path");
        var loader = new BlacklistLoader(nonexistent);
        var data = loader.Load();

        // Should not throw, returns defaults
        Assert.Empty(data.ExeNamePatterns);
    }

    // ── Helpers ───────────────────────────────────────────────

    private void WriteBlacklistJson(string json)
    {
        string dataDir = Path.Combine(_tempDir, "data");
        Directory.CreateDirectory(dataDir);
        File.WriteAllText(Path.Combine(dataDir, "blacklist.json"), json);
    }
}
