namespace GamingCommander.App.Services;

/// <summary>
/// A single exe name pattern with its severity tier.
/// Tier 1 = highest severity (universal noise like uninstallers).
/// Tier 20 = lowest severity (utility tools, rare edge cases).
/// </summary>
public sealed record BlacklistTierEntry(string Pattern, int Tier);

/// <summary>
/// Immutable result from BlacklistLoader containing all noise patterns,
/// organized by category.
/// </summary>
public sealed record BlacklistData(
    IReadOnlyList<string> ExeNamePatterns,
    IReadOnlyList<BlacklistTierEntry> TieredExePatterns,
    IReadOnlyList<string> DirectoryPatterns,
    IReadOnlyList<string> PeMetadataPatterns,
    IReadOnlyList<string> PcgwTitleNoise)
{
    /// <summary>Empty singleton instance representing no blacklist data.</summary>
    public static readonly BlacklistData Empty = new(
        ExeNamePatterns: [],
        TieredExePatterns: [],
        DirectoryPatterns: [],
        PeMetadataPatterns: [],
        PcgwTitleNoise: []);
}
