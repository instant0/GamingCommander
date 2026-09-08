namespace GamingCommander.Core.Models;

/// <summary>
/// Constant names for the <see cref="GameEntry.PlatformMetadata"/> dictionary keys.
/// These keys are PERSISTED DATA (games.json) — the values must never be renamed,
/// only the C# references to them may change. Use these constants instead of raw
/// string literals when reading or writing platform metadata (Plan 125 Phase 2b).
/// </summary>
public static class PlatformMetadataKeys
{
    // --- Steam ---
    public const string SteamAppId = "SteamAppId";
    public const string SteamStatus = "SteamStatus";
    public const string AcfFilePath = "AcfFilePath";
    public const string AcfLibraryPath = "AcfLibraryPath";
    public const string AcfExpectedPath = "AcfExpectedPath";
    public const string AcfBuildId = "AcfBuildId";
    public const string AcfSizeOnDisk = "AcfSizeOnDisk";
    public const string AcfStateFlags = "AcfStateFlags";

    // --- Epic ---
    public const string EpicStatus = "EpicStatus";
    public const string EpicItemPath = "EpicItemPath";
    public const string EpicCatalogItemId = "EpicCatalogItemId";
    public const string EpicCatalogNamespace = "EpicCatalogNamespace";
    public const string EpicAppName = "EpicAppName";

    // --- Shared / location ---
    public const string LibraryRoot = "LibraryRoot";
    public const string ActualLibraryRoot = "ActualLibraryRoot";
    public const string GameFolder = "GameFolder";
    public const string FolderName = "FolderName";

    // --- Executable candidates (emitted by scanners, edited by F4) ---
    public const string ExeCandidates = "ExeCandidates";
    public const string ExeCandidateCount = "ExeCandidateCount";
    public const string PeFileDescription = "PeFileDescription";

    // --- Store title / studio enrichment ---
    public const string GogGameId = "GogGameId";
    public const string Studio = "Studio";
    public const string EaGameName = "EaGameName";
    public const string BlizzardProduct = "BlizzardProduct";

    // --- Title persistence (E8) ---
    public const string TitleSource = "TitleSource";
    public const string AutoDetectedTitle = "AutoDetectedTitle";
}