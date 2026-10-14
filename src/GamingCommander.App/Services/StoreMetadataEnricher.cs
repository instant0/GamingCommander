using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

/// <summary>
/// Mutable build context threaded through <see cref="FolderScanner.AddGameEntry"/>'s
/// enrichment pipeline (Plan 125 Phase 2). Holds the shared locals the store and
/// title enrichers may update: display name, exe path, launch args, platform
/// metadata, and the "store already set a title" flag that gates title enrichment.
/// </summary>
internal sealed class GameEntryBuildContext
{
    public GameEntryBuildContext(string initialDisplayName)
    {
        DisplayName = initialDisplayName;
    }

    /// <summary>Current display name — starts as the folder-derived title, may be replaced by store/PE/readme enrichment.</summary>
    public string DisplayName { get; set; }

    /// <summary>Resolved exe path — may be back-filled by GOG .info or Epic .item when discovery found nothing.</summary>
    public string? ExePath { get; set; }

    /// <summary>Launch command-line arguments (GOG .info).</summary>
    public string CommandLineArgs { get; set; } = string.Empty;

    /// <summary>True once a store parser replaced the folder-derived title — gates PE/readme title enrichment.</summary>
    public bool StoreEnrichedDisplayName { get; set; }

    // --- Detection signals (Plan 127 A2/A3) ---
    public string TitleSource { get; set; } = string.Empty;
    public string AutoDetectedTitle { get; set; } = string.Empty;
    public string PeFileDescription { get; set; } = string.Empty;
    public List<string> ExeCandidates { get; } = [];

    // --- Store identifiers (Plan 127 A2) ---
    public string GogGameId { get; set; } = string.Empty;
    public string EpicCatalogItemId { get; set; } = string.Empty;
    public string EpicCatalogNamespace { get; set; } = string.Empty;
    public string EpicAppName { get; set; } = string.Empty;
    public string Studio { get; set; } = string.Empty;
    public string EaGameName { get; set; } = string.Empty;
    public string BlizzardProduct { get; set; } = string.Empty;
    public string UbisoftReadme { get; set; } = string.Empty;

    // --- Epic status (Plan 127 A2) ---
    public string EpicStatus { get; set; } = string.Empty;
    public string EpicItemPath { get; set; } = string.Empty;
}

/// <summary>
/// Store metadata enrichment for <see cref="FolderScanner"/> (Plan 125 Phase 2).
/// Runs the GOG → EA → Epic → BattleNet blocks in that exact order, immediately
/// after executable discovery and BEFORE title enrichment. Reads only — never
/// writes manifests/configs.
/// </summary>
internal static class StoreMetadataEnricher
{
    /// <summary>
    /// Applies the store-specific enrichment blocks to <paramref name="ctx"/>.
    /// Preserves the exact enrichment ordering and all metadata keys from the
    /// original <c>FolderScanner.AddGameEntry</c> implementation.
    /// </summary>
    public static void Enrich(
        GameSourceKind resolvedType,
        DirectoryInfo subDir,
        IReadOnlySet<string> noiseDirectoryPatterns,
        GameEntryBuildContext ctx)
    {
        // GOG enrichment: parse goggame-*.info for title, exe, args, and game ID
        if (resolvedType == GameSourceKind.Gog
            && GogInfoParser.TryParse(subDir, noiseDirectoryPatterns, out var gogInfo)
            && gogInfo is not null)
        {
            // Title: GOG .info is the official source
            if (!string.IsNullOrEmpty(gogInfo.Title))
            {
                ctx.AutoDetectedTitle = ctx.DisplayName;
                ctx.DisplayName = gogInfo.Title;
                ctx.TitleSource = TitleSourceValues.GogInfo;
                ctx.StoreEnrichedDisplayName = true;
            }

            // Exe: GOG .info is a fallback when ExecutableDiscovery finds nothing
            if (string.IsNullOrEmpty(ctx.ExePath) && !string.IsNullOrEmpty(gogInfo.ExePath))
            {
                ctx.ExePath = gogInfo.ExePath;
            }

            // Launch args
            if (!string.IsNullOrEmpty(gogInfo.LaunchArgs))
            {
                ctx.CommandLineArgs = gogInfo.LaunchArgs;
            }

            // Platform metadata
            ctx.GogGameId = gogInfo.GameId;
        }

        // EA enrichment: parse __Installer/InstallLog.txt for game name, display name, studio.
        // The Install Location field may reference an old/wrong path, but game name and studio are reliable.
        if (resolvedType == GameSourceKind.EaApp
            && EaInstallLogParser.TryParse(subDir, out var eaInfo)
            && eaInfo is not null)
        {
            // Display name: EA display name is authoritative (e.g., "Dragon Age™: Inquisition")
            if (!string.IsNullOrEmpty(eaInfo.DisplayName))
            {
                ctx.AutoDetectedTitle = ctx.DisplayName;
                ctx.DisplayName = eaInfo.DisplayName;
                ctx.TitleSource = TitleSourceValues.EaInstallLog;
                ctx.StoreEnrichedDisplayName = true;
            }

            // Studio metadata
            if (!string.IsNullOrEmpty(eaInfo.Studio))
            {
                ctx.Studio = eaInfo.Studio;
            }

            // Game name (non-trademarked)
            if (!string.IsNullOrEmpty(eaInfo.GameName))
            {
                ctx.EaGameName = eaInfo.GameName;
            }
        }

        // Epic enrichment: extract metadata from .mancpn/.item files, cross-reference global manifests
        if (resolvedType == GameSourceKind.Epic)
        {
            // Strategy 1: Local identifier extraction from .egstore/ or .egsstore/
            var localIds = EpicManifestParser.ExtractLocalIdentifiers(subDir);

            // Strategy 2: Global .item cross-reference from ProgramData
            var globalItem = EpicManifestParser.CrossReferenceGlobalManifests(subDir);
            if (globalItem is not null && !string.IsNullOrEmpty(globalItem.DisplayName))
            {
                ctx.AutoDetectedTitle = ctx.DisplayName;
                ctx.DisplayName = globalItem.DisplayName;
                ctx.TitleSource = TitleSourceValues.EpicItemManifest;
                ctx.StoreEnrichedDisplayName = true;

                // Override local namespace with correct public namespace from global .item
                localIds = new EpicManifestParser.EpicIdentifiers(
                    CatalogNamespace: globalItem.CatalogNamespace,
                    CatalogItemId: globalItem.CatalogItemId,
                    AppName: globalItem.AppName,
                    DisplayName: globalItem.DisplayName,
                    LaunchExecutable: globalItem.LaunchExecutable);
            }

            // Store GUID identifiers
            if (localIds is not null)
            {
                if (!string.IsNullOrEmpty(localIds.CatalogItemId))
                    ctx.EpicCatalogItemId = localIds.CatalogItemId;
                if (!string.IsNullOrEmpty(localIds.CatalogNamespace))
                    ctx.EpicCatalogNamespace = localIds.CatalogNamespace;
                if (!string.IsNullOrEmpty(localIds.AppName))
                    ctx.EpicAppName = localIds.AppName;
            }

            ctx.EpicStatus = globalItem is null ? "Orphaned" : "Installed";

            // Resolve LaunchExecutable from .item if available (and no exe found yet)
            if (globalItem is not null
                && !string.IsNullOrEmpty(globalItem.LaunchExecutable)
                && !string.IsNullOrEmpty(globalItem.InstallLocation)
                && string.IsNullOrEmpty(ctx.ExePath))
            {
                string resolvedExe = EpicManifestParser.ResolveLaunchExecutable(
                    globalItem.InstallLocation, globalItem.LaunchExecutable);
                if (!string.IsNullOrEmpty(resolvedExe))
                {
                    ctx.ExePath = resolvedExe;
                }
            }
        }

        // BattleNet enrichment: extract product codename from .build.info
        if (resolvedType == GameSourceKind.BattleNet)
        {
            string? product = StoreSignalDetector.ExtractBlizzardProduct(subDir);
            if (!string.IsNullOrEmpty(product))
            {
                ctx.BlizzardProduct = product;
            }
        }
    }
}