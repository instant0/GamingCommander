namespace GamingCommander.App.Services;

/// <summary>
/// Immutable result from BlacklistLoader containing all noise patterns,
/// organized by category.
/// </summary>
public sealed record BlacklistData(
    IReadOnlyList<string> ExeNamePatterns,
    IReadOnlyList<string> DirectoryPatterns,
    IReadOnlyList<string> PeMetadataPatterns,
    IReadOnlyList<string> PcgwTitleNoise)
{
    public static readonly BlacklistData Empty = new(
        ExeNamePatterns: [],
        DirectoryPatterns: [],
        PeMetadataPatterns: [],
        PcgwTitleNoise: []);
}
