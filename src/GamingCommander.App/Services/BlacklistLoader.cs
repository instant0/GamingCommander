using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamingCommander.App.Services;

/// <summary>
/// Loads the noise-pattern blacklist from data/blacklist.json at startup.
/// Provides flattened lists of exe-name substrings and directory-name substrings
/// for use by FolderScanner.
/// </summary>
public sealed class BlacklistLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _basePath;

    public BlacklistLoader(string basePath)
    {
        _basePath = basePath;
    }

    /// <summary>
    /// Loads and returns the blacklist patterns. If the file is missing or
    /// malformed, returns an empty result (no blacklist applied) rather than
    /// crashing.
    /// </summary>
    public BlacklistData Load()
    {
        string jsonPath = Path.Combine(_basePath, "data", "blacklist.json");

        if (!File.Exists(jsonPath))
            return BlacklistData.Empty;

        try
        {
            string json = File.ReadAllText(jsonPath);
            var dto = JsonSerializer.Deserialize<BlacklistDto>(json, JsonOptions);
            if (dto?.ExeNamePatterns is null)
                return BlacklistData.Empty;

            var exePatterns = new List<string>();
            foreach (var tier in dto.ExeNamePatterns.GetTiers())
                exePatterns.AddRange(tier);

            var dirPatterns = dto.DirectoryPatterns?.Patterns ?? [];
            var peMetaPatterns = dto.PeMetadataBlacklist?.Patterns ?? [];
            var pcgwNoise = dto.PcgwPageTitleNoise?.Patterns ?? [];

            return new BlacklistData(
                ExeNamePatterns: exePatterns,
                DirectoryPatterns: dirPatterns,
                PeMetadataPatterns: peMetaPatterns,
                PcgwTitleNoise: pcgwNoise);
        }
        catch
        {
            // Silently return empty blacklist on parse failure
            return BlacklistData.Empty;
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

        [JsonPropertyName("tier_5_unreal_build_debug")]
        public List<string>? Tier5UnrealBuildDebug { get; set; }

        [JsonPropertyName("tier_6_crash_reporting")]
        public List<string>? Tier6CrashReporting { get; set; }

        [JsonPropertyName("tier_7_drm_wrappers")]
        public List<string>? Tier7DrmWrappers { get; set; }

        [JsonPropertyName("tier_8_installer_utilities")]
        public List<string>? Tier8InstallerUtilities { get; set; }

        [JsonPropertyName("tier_9_server_loader_stub")]
        public List<string>? Tier9ServerLoaderStub { get; set; }

        [JsonPropertyName("tier_10_distribution_tools")]
        public List<string>? Tier10DistributionTools { get; set; }

        [JsonPropertyName("tier_11_dev_editor_tools")]
        public List<string>? Tier11DevEditorTools { get; set; }

        [JsonPropertyName("tier_12_utilities_debug")]
        public List<string>? Tier12UtilitiesDebug { get; set; }

        [JsonPropertyName("tier_13_trial_demo_stub")]
        public List<string>? Tier13TrialDemoStub { get; set; }

        [JsonPropertyName("tier_14_media_codec_tools")]
        public List<string>? Tier14MediaCodecTools { get; set; }

        [JsonPropertyName("tier_15_installer_frameworks")]
        public List<string>? Tier15InstallerFrameworks { get; set; }

        [JsonPropertyName("tier_16_runtime_interpreters")]
        public List<string>? Tier16RuntimeInterpreters { get; set; }

        [JsonPropertyName("tier_17_web_ui_overlay")]
        public List<string>? Tier17WebUiOverlay { get; set; }

        [JsonPropertyName("tier_18_repair_service_helper")]
        public List<string>? Tier18RepairServiceHelper { get; set; }

        [JsonPropertyName("tier_19_unreal_build_tools")]
        public List<string>? Tier19UnrealBuildTools { get; set; }

        [JsonPropertyName("tier_20_patch_update")]
        public List<string>? Tier20PatchUpdate { get; set; }

        [JsonPropertyName("tier_21_utility_tools")]
        public List<string>? Tier21UtilityTools { get; set; }

        /// <summary>
        /// Returns all tier lists that have values, in tier order.
        /// </summary>
        public IEnumerable<List<string>> GetTiers()
        {
            if (Tier1UniversalNoise is { Count: > 0 }) yield return Tier1UniversalNoise;
            if (Tier2LauncherStubs is { Count: > 0 }) yield return Tier2LauncherStubs;
            if (Tier3StoreBootstraps is { Count: > 0 }) yield return Tier3StoreBootstraps;
            if (Tier4AnticheatDrm is { Count: > 0 }) yield return Tier4AnticheatDrm;
            if (Tier5UnrealBuildDebug is { Count: > 0 }) yield return Tier5UnrealBuildDebug;
            if (Tier6CrashReporting is { Count: > 0 }) yield return Tier6CrashReporting;
            if (Tier7DrmWrappers is { Count: > 0 }) yield return Tier7DrmWrappers;
            if (Tier8InstallerUtilities is { Count: > 0 }) yield return Tier8InstallerUtilities;
            if (Tier9ServerLoaderStub is { Count: > 0 }) yield return Tier9ServerLoaderStub;
            if (Tier10DistributionTools is { Count: > 0 }) yield return Tier10DistributionTools;
            if (Tier11DevEditorTools is { Count: > 0 }) yield return Tier11DevEditorTools;
            if (Tier12UtilitiesDebug is { Count: > 0 }) yield return Tier12UtilitiesDebug;
            if (Tier13TrialDemoStub is { Count: > 0 }) yield return Tier13TrialDemoStub;
            if (Tier14MediaCodecTools is { Count: > 0 }) yield return Tier14MediaCodecTools;
            if (Tier15InstallerFrameworks is { Count: > 0 }) yield return Tier15InstallerFrameworks;
            if (Tier16RuntimeInterpreters is { Count: > 0 }) yield return Tier16RuntimeInterpreters;
            if (Tier17WebUiOverlay is { Count: > 0 }) yield return Tier17WebUiOverlay;
            if (Tier18RepairServiceHelper is { Count: > 0 }) yield return Tier18RepairServiceHelper;
            if (Tier19UnrealBuildTools is { Count: > 0 }) yield return Tier19UnrealBuildTools;
            if (Tier20PatchUpdate is { Count: > 0 }) yield return Tier20PatchUpdate;
            if (Tier21UtilityTools is { Count: > 0 }) yield return Tier21UtilityTools;
        }
    }

    private sealed class TierDto
    {
        [JsonPropertyName("patterns")]
        public List<string>? Patterns { get; set; }
    }
}
