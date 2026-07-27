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
                    "tier_10_dev_editor_tools": ["tool"],
                    "tier_20_utility_tools": ["util"]
                }
            }
            """;
        WriteBlacklistJson(json);

        var loader = new BlacklistLoader(_tempDir);
        var data = loader.Load();

        foreach (var entry in data.TieredExePatterns)
        {
            Assert.InRange(entry.Tier, 1, 20);
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

    // ════════════════════════════════════════════════════════════════
    //  Tier Name Mismatch Regression (Plan 112 Step 1)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Load_WithAllTiers_AllJsonKeysDeserialized()
    {
        // Uses the real blacklist.json to verify all 20 tiers deserialize correctly.
        // This catches JsonPropertyName mismatches between C# DTO and JSON keys.
        string json = """
            {
                "exe_name_patterns": {
                    "tier_1_universal_noise": ["unins", "setup"],
                    "tier_2_launcher_stubs": ["launcher"],
                    "tier_3_store_bootstraps": ["galaxy", "epic"],
                    "tier_4_anticheat_drm": ["easyanticheat", "battleye"],
                    "tier_5_error_crash_reporting": ["crash", "error"],
                    "tier_6_drm_wrappers": ["xlive"],
                    "tier_7_installer_utilities": ["autorun", "7za"],
                    "tier_8_server_loader_stub": ["dedicatedserver", "stub"],
                    "tier_9_distribution_tools": ["sdcr", "tachyon"],
                    "tier_10_dev_editor_tools": ["editor", "modmanager"],
                    "tier_11_utilities_debug": ["install", "debug"],
                    "tier_12_trial_demo_stub": ["trial", "_upp"],
                    "tier_13_media_codec_tools": ["ffmpeg"],
                    "tier_14_installer_frameworks": ["squirrel"],
                    "tier_15_runtime_interpreters": ["python"],
                    "tier_16_web_ui_overlay": ["coherentui", "cefhost"],
                    "tier_17_repair_service_helper": ["repair", "service"],
                    "tier_18_unreal_build_tools": ["unrealpak"],
                    "tier_19_patch_update": ["patch"],
                    "tier_20_utility_tools": ["winscp"]
                }
            }
            """;
        WriteBlacklistJson(json);

        var loader = new BlacklistLoader(_tempDir);
        var data = loader.Load();

        // All 20 tiers should produce tiered entries
        Assert.Equal(20, data.TieredExePatterns.Select(t => t.Tier).Distinct().Count());
    }

    [Fact]
    public void Load_Tier5_IsErrorCrashReporting_NotUnrealBuildDebug()
    {
        // Regression: Tier 5 JSON key is "tier_5_error_crash_reporting" — C# must match.
        string json = """
            {
                "exe_name_patterns": {
                    "tier_5_error_crash_reporting": ["crash", "error", "bugsplat"]
                }
            }
            """;
        WriteBlacklistJson(json);

        var loader = new BlacklistLoader(_tempDir);
        var data = loader.Load();

        Assert.NotEmpty(data.TieredExePatterns);
        Assert.Equal(5, data.TieredExePatterns[0].Tier);
        Assert.Contains("crash", data.ExeNamePatterns);
        Assert.Contains("error", data.ExeNamePatterns);
        Assert.Contains("bugsplat", data.ExeNamePatterns);
    }

    [Fact]
    public void Load_Tier12_ContainsTrialAndUpp()
    {
        // Regression: Tier 12 JSON key is "tier_12_trial_demo_stub" — contains _upp pattern.
        string json = """
            {
                "exe_name_patterns": {
                    "tier_12_trial_demo_stub": ["trial", "_upp"]
                }
            }
            """;
        WriteBlacklistJson(json);

        var loader = new BlacklistLoader(_tempDir);
        var data = loader.Load();

        Assert.NotEmpty(data.TieredExePatterns);
        Assert.Equal(12, data.TieredExePatterns[0].Tier);
        Assert.Contains("_upp", data.ExeNamePatterns);
        Assert.Contains("trial", data.ExeNamePatterns);
    }

    [Fact]
    public void Load_Tier18_IsUnrealBuildTools_NotRepairServiceHelper()
    {
        // Regression: Tier 18 JSON key is "tier_18_unreal_build_tools" — C# must match.
        string json = """
            {
                "exe_name_patterns": {
                    "tier_18_unreal_build_tools": ["unrealpak"]
                }
            }
            """;
        WriteBlacklistJson(json);

        var loader = new BlacklistLoader(_tempDir);
        var data = loader.Load();

        Assert.NotEmpty(data.TieredExePatterns);
        Assert.Equal(18, data.TieredExePatterns[0].Tier);
        Assert.Contains("unrealpak", data.ExeNamePatterns);
    }

    // ════════════════════════════════════════════════════════════════
    //  Phase 1 — Plan 114 Blacklist Additions (B27, B28)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Load_Tier10_ContainsBuilderAndWorldbuilderAndConfigtool()
    {
        // B27/B28: tier_10_dev_editor_tools should include builder, worldbuilder, configtool
        string json = """
            {
                "exe_name_patterns": {
                    "tier_10_dev_editor_tools": [
                        "datacompiler", "editor", "modmanager", "packagemanager", "reminder",
                        "contented", "leveled", "resourceed",
                        "builder", "worldbuilder", "configtool"
                    ]
                }
            }
            """;
        WriteBlacklistJson(json);

        var loader = new BlacklistLoader(_tempDir);
        var data = loader.Load();

        Assert.NotEmpty(data.TieredExePatterns);
        Assert.Equal(10, data.TieredExePatterns[0].Tier);
        Assert.Contains("builder", data.ExeNamePatterns);
        Assert.Contains("worldbuilder", data.ExeNamePatterns);
        Assert.Contains("configtool", data.ExeNamePatterns);
    }

    // ── Helpers ───────────────────────────────────────────────

    private void WriteBlacklistJson(string json)
    {
        string dataDir = Path.Combine(_tempDir, "data");
        Directory.CreateDirectory(dataDir);
        File.WriteAllText(Path.Combine(dataDir, "blacklist.json"), json);
    }
}
