using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamingCommander.App.Services;

/// <summary>
/// Loads the noise-pattern blacklist from data/blacklist.json at startup.
/// Provides flattened lists of exe-name substrings and directory-name substrings
/// for use by FolderScanner.
///
/// Bug 16: if the on-disk blacklist is missing, empty, or corrupt, the loader
/// falls back to the copy embedded in the assembly (GamingCommander.App.data.blacklist.json)
/// so that wiping the user data/ directory never silently disables noise filtering.
/// When the file is merely missing, the embedded default is also written back to disk.
/// </summary>
public sealed class BlacklistLoader
{
    private const string EmbeddedBlacklistResource = "GamingCommander.App.data.blacklist.json";

    private static readonly JsonSerializerOptions BlacklistOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _basePath;

    /// <summary>Creates a new blacklist loader that reads from data/blacklist.json relative to the given base path.</summary>
    public BlacklistLoader(string basePath)
    {
        _basePath = basePath;
    }

    /// <summary>
    /// Loads and returns the blacklist patterns.
    /// Priority: on-disk data/blacklist.json → embedded resource default.
    /// If both fail, returns an empty result (no blacklist applied) rather than crashing.
    /// </summary>
    public BlacklistData Load()
    {
        string jsonPath = Path.Combine(_basePath, "data", "blacklist.json");
        string json = ReadBlacklistJson(jsonPath) ?? RestoreFromEmbeddedResource(jsonPath);

        BlacklistDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<BlacklistDto>(json, BlacklistOptions);
        }
        catch
        {
            // Corrupt on-disk file → retry with the embedded default (Bug 16).
            // The embedded resource always yields valid JSON ("{}" as last resort).
            dto = JsonSerializer.Deserialize<BlacklistDto>(
                RestoreFromEmbeddedResource(jsonPath), BlacklistOptions);
        }

        if (dto?.ExeNamePatterns is null)
            return BlacklistData.Empty;

        // Build tiered entries with tier numbers preserved
        var tieredEntries = new List<BlacklistTierEntry>();
        foreach (var (tierNumber, patterns) in dto.ExeNamePatterns.GetTieredTiers())
        {
            foreach (string pattern in patterns)
                tieredEntries.Add(new BlacklistTierEntry(pattern, tierNumber));
        }

        // Backward-compatible flat list (all patterns, no tier info)
        var flatPatterns = tieredEntries.Select(t => t.Pattern).ToList();

        var directoryPatterns = dto.DirectoryPatterns?.Patterns ?? [];
        var peMetadataPatterns = dto.PeMetadataBlacklist?.Patterns ?? [];
        var pcgwTitleNoise = dto.PcgwPageTitleNoise?.Patterns ?? [];

        return new BlacklistData(
            ExeNamePatterns: flatPatterns,
            TieredExePatterns: tieredEntries,
            DirectoryPatterns: directoryPatterns,
            PeMetadataPatterns: peMetadataPatterns,
            PcgwTitleNoise: pcgwTitleNoise);
    }

    // ── Source resolution (Bug 16) ─────────────────────────────────────

    /// <summary>
    /// Reads the on-disk blacklist JSON. Returns null when the file is missing,
    /// unreadable, or effectively empty (whitespace) so the caller can fall back.
    /// </summary>
    private static string? ReadBlacklistJson(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            return null;

        try
        {
            string json = File.ReadAllText(jsonPath);
            return string.IsNullOrWhiteSpace(json) ? null : json;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Loads the embedded default blacklist (Bug 16). If the on-disk file is missing,
    /// the embedded copy is also written back to disk so future loads are self-sufficient.
    /// Returns an empty JSON object when no embedded resource is available.
    /// </summary>
    private static string RestoreFromEmbeddedResource(string jsonPath)
    {
        try
        {
            Assembly assembly = typeof(BlacklistLoader).Assembly;
            using Stream? stream = assembly.GetManifestResourceStream(EmbeddedBlacklistResource);
            if (stream is null)
                return "{}";

            using var reader = new StreamReader(stream);
            string json = reader.ReadToEnd();
            if (string.IsNullOrWhiteSpace(json))
                return "{}";

            // Restore the file only when it is missing (never overwrite user edits or
            // a corrupt-but-recoverable file).
            if (!File.Exists(jsonPath))
            {
                try
                {
                    JsonFileHelper.EnsureDirectoryExists(jsonPath);
                    File.WriteAllText(jsonPath, json);
                }
                catch
                {
                    // Non-fatal — the in-memory fallback still applies this load.
                }
            }

            return json;
        }
        catch
        {
            return "{}";
        }
    }

    // ── DTO for deserialization ──────────────────────────────────────────

    private sealed class BlacklistDto
    {
        [JsonPropertyName("exe_name_patterns")]
        public ExeNamePatternsDto? ExeNamePatterns { get; set; }

        [JsonPropertyName("directory_patterns")]
        public TierDto? DirectoryPatterns { get; set; }

        [JsonPropertyName("pe_metadata_blacklist")]
        public TierDto? PeMetadataBlacklist { get; set; }

        [JsonPropertyName("pcgw_page_title_noise")]
        public TierDto? PcgwPageTitleNoise { get; set; }
    }

    private sealed class ExeNamePatternsDto
    {
        [JsonPropertyName("tier_1_universal_noise")]
        public List<string>? Tier1UniversalNoise { get; set; }

        [JsonPropertyName("tier_2_launcher_stubs")]
        public List<string>? Tier2LauncherStubs { get; set; }

        [JsonPropertyName("tier_3_store_bootstraps")]
        public List<string>? Tier3StoreBootstraps { get; set; }

        [JsonPropertyName("tier_4_anticheat_drm")]
        public List<string>? Tier4AnticheatDrm { get; set; }

        [JsonPropertyName("tier_5_error_crash_reporting")]
        public List<string>? Tier5ErrorCrashReporting { get; set; }

        [JsonPropertyName("tier_6_drm_wrappers")]
        public List<string>? Tier6DrmWrappers { get; set; }

        [JsonPropertyName("tier_7_installer_utilities")]
        public List<string>? Tier7InstallerUtilities { get; set; }

        [JsonPropertyName("tier_8_server_loader_stub")]
        public List<string>? Tier8ServerLoaderStub { get; set; }

        [JsonPropertyName("tier_9_distribution_tools")]
        public List<string>? Tier9DistributionTools { get; set; }

        [JsonPropertyName("tier_10_dev_editor_tools")]
        public List<string>? Tier10DevEditorTools { get; set; }

        [JsonPropertyName("tier_11_utilities_debug")]
        public List<string>? Tier11UtilitiesDebug { get; set; }

        [JsonPropertyName("tier_12_trial_demo_stub")]
        public List<string>? Tier12TrialDemoStub { get; set; }

        [JsonPropertyName("tier_13_media_codec_tools")]
        public List<string>? Tier13MediaCodecTools { get; set; }

        [JsonPropertyName("tier_14_installer_frameworks")]
        public List<string>? Tier14InstallerFrameworks { get; set; }

        [JsonPropertyName("tier_15_runtime_interpreters")]
        public List<string>? Tier15RuntimeInterpreters { get; set; }

        [JsonPropertyName("tier_16_web_ui_overlay")]
        public List<string>? Tier16WebUiOverlay { get; set; }

        [JsonPropertyName("tier_17_repair_service_helper")]
        public List<string>? Tier17RepairServiceHelper { get; set; }

        [JsonPropertyName("tier_18_unreal_build_tools")]
        public List<string>? Tier18UnrealBuildTools { get; set; }

        [JsonPropertyName("tier_19_patch_update")]
        public List<string>? Tier19PatchUpdate { get; set; }

        [JsonPropertyName("tier_20_utility_tools")]
        public List<string>? Tier20UtilityTools { get; set; }

        /// <summary>
        /// Returns all tier lists that have values, in tier order.
        /// </summary>
        public IEnumerable<List<string>> GetTiers()
        {
            if (Tier1UniversalNoise is { Count: > 0 }) yield return Tier1UniversalNoise;
            if (Tier2LauncherStubs is { Count: > 0 }) yield return Tier2LauncherStubs;
            if (Tier3StoreBootstraps is { Count: > 0 }) yield return Tier3StoreBootstraps;
            if (Tier4AnticheatDrm is { Count: > 0 }) yield return Tier4AnticheatDrm;
            if (Tier5ErrorCrashReporting is { Count: > 0 }) yield return Tier5ErrorCrashReporting;
            if (Tier6DrmWrappers is { Count: > 0 }) yield return Tier6DrmWrappers;
            if (Tier7InstallerUtilities is { Count: > 0 }) yield return Tier7InstallerUtilities;
            if (Tier8ServerLoaderStub is { Count: > 0 }) yield return Tier8ServerLoaderStub;
            if (Tier9DistributionTools is { Count: > 0 }) yield return Tier9DistributionTools;
            if (Tier10DevEditorTools is { Count: > 0 }) yield return Tier10DevEditorTools;
            if (Tier11UtilitiesDebug is { Count: > 0 }) yield return Tier11UtilitiesDebug;
            if (Tier12TrialDemoStub is { Count: > 0 }) yield return Tier12TrialDemoStub;
            if (Tier13MediaCodecTools is { Count: > 0 }) yield return Tier13MediaCodecTools;
            if (Tier14InstallerFrameworks is { Count: > 0 }) yield return Tier14InstallerFrameworks;
            if (Tier15RuntimeInterpreters is { Count: > 0 }) yield return Tier15RuntimeInterpreters;
            if (Tier16WebUiOverlay is { Count: > 0 }) yield return Tier16WebUiOverlay;
            if (Tier17RepairServiceHelper is { Count: > 0 }) yield return Tier17RepairServiceHelper;
            if (Tier18UnrealBuildTools is { Count: > 0 }) yield return Tier18UnrealBuildTools;
            if (Tier19PatchUpdate is { Count: > 0 }) yield return Tier19PatchUpdate;
            if (Tier20UtilityTools is { Count: > 0 }) yield return Tier20UtilityTools;
        }

        /// <summary>
        /// Returns tier lists with their tier numbers, for building TieredExePatterns.
        /// Each tuple contains (tierNumber, patterns).
        /// </summary>
        public IEnumerable<(int Tier, List<string> Patterns)> GetTieredTiers()
        {
            if (Tier1UniversalNoise is { Count: > 0 }) yield return (1, Tier1UniversalNoise);
            if (Tier2LauncherStubs is { Count: > 0 }) yield return (2, Tier2LauncherStubs);
            if (Tier3StoreBootstraps is { Count: > 0 }) yield return (3, Tier3StoreBootstraps);
            if (Tier4AnticheatDrm is { Count: > 0 }) yield return (4, Tier4AnticheatDrm);
            if (Tier5ErrorCrashReporting is { Count: > 0 }) yield return (5, Tier5ErrorCrashReporting);
            if (Tier6DrmWrappers is { Count: > 0 }) yield return (6, Tier6DrmWrappers);
            if (Tier7InstallerUtilities is { Count: > 0 }) yield return (7, Tier7InstallerUtilities);
            if (Tier8ServerLoaderStub is { Count: > 0 }) yield return (8, Tier8ServerLoaderStub);
            if (Tier9DistributionTools is { Count: > 0 }) yield return (9, Tier9DistributionTools);
            if (Tier10DevEditorTools is { Count: > 0 }) yield return (10, Tier10DevEditorTools);
            if (Tier11UtilitiesDebug is { Count: > 0 }) yield return (11, Tier11UtilitiesDebug);
            if (Tier12TrialDemoStub is { Count: > 0 }) yield return (12, Tier12TrialDemoStub);
            if (Tier13MediaCodecTools is { Count: > 0 }) yield return (13, Tier13MediaCodecTools);
            if (Tier14InstallerFrameworks is { Count: > 0 }) yield return (14, Tier14InstallerFrameworks);
            if (Tier15RuntimeInterpreters is { Count: > 0 }) yield return (15, Tier15RuntimeInterpreters);
            if (Tier16WebUiOverlay is { Count: > 0 }) yield return (16, Tier16WebUiOverlay);
            if (Tier17RepairServiceHelper is { Count: > 0 }) yield return (17, Tier17RepairServiceHelper);
            if (Tier18UnrealBuildTools is { Count: > 0 }) yield return (18, Tier18UnrealBuildTools);
            if (Tier19PatchUpdate is { Count: > 0 }) yield return (19, Tier19PatchUpdate);
            if (Tier20UtilityTools is { Count: > 0 }) yield return (20, Tier20UtilityTools);
        }
    }

    private sealed class TierDto
    {
        [JsonPropertyName("patterns")]
        public List<string>? Patterns { get; set; }
    }
}
