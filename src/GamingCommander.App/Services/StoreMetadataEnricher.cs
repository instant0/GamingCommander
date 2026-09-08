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

    /// <summary>Platform metadata key/values collected during the scan.</summary>
    public Dictionary<string, string> PlatformMetadata { get; } = [];

    /// <summary>True once a store parser replaced the folder-derived title — gates PE/readme title enrichment.</summary>
    public bool StoreEnrichedDisplayName { get; set; }
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
                ctx.PlatformMetadata[PlatformMetadataKeys.AutoDetectedTitle] = ctx.DisplayName;
                ctx.DisplayName = gogInfo.Title;
                ctx.PlatformMetadata[PlatformMetadataKeys.TitleSource] = TitleSourceValues.GogInfo;
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
            ctx.PlatformMetadata[PlatformMetadataKeys.GogGameId] = gogInfo.GameId;
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
                ctx.PlatformMetadata[PlatformMetadataKeys.AutoDetectedTitle] = ctx.DisplayName;
                ctx.DisplayName = eaInfo.DisplayName;
                ctx.PlatformMetadata[PlatformMetadataKeys.TitleSource] = TitleSourceValues.EaInstallLog;
                ctx.StoreEnrichedDisplayName = true;
            }

            // Studio metadata
            if (!string.IsNullOrEmpty(eaInfo.Studio))
            {
                ctx.PlatformMetadata[PlatformMetadataKeys.Studio] = eaInfo.Studio;
            }

            // Game name (non-trademarked)
            if (!string.IsNullOrEmpty(eaInfo.GameName))
            {
                ctx.PlatformMetadata[PlatformMetadataKeys.EaGameName] = eaInfo.GameName;
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
                ctx.PlatformMetadata[PlatformMetadataKeys.AutoDetectedTitle] = ctx.DisplayName;
                ctx.DisplayName = globalItem.DisplayName;
                ctx.PlatformMetadata[PlatformMetadataKeys.TitleSource] = TitleSourceValues.EpicItemManifest;
                ctx.StoreEnrichedDisplayName = true;

                // Override local namespace with correct public namespace from global .item
                localIds = new EpicManifestParser.EpicIdentifiers(
                    CatalogNamespace: globalItem.CatalogNamespace,
                    CatalogItemId: globalItem.CatalogItemId,
                    AppName: globalItem.AppName,
                    DisplayName: globalItem.DisplayName,
                    LaunchExecutable: globalItem.LaunchExecutable);
            }

            // Store GUID identifiers in platform metadata
            if (localIds is not null)
            {
                if (!string.IsNullOrEmpty(localIds.CatalogItemId))
                    ctx.PlatformMetadata[PlatformMetadataKeys.EpicCatalogItemId] = localIds.CatalogItemId;
                if (!string.IsNullOrEmpty(localIds.CatalogNamespace))
                    ctx.PlatformMetadata[PlatformMetadataKeys.EpicCatalogNamespace] = localIds.CatalogNamespace;
                if (!string.IsNullOrEmpty(localIds.AppName))
                    ctx.PlatformMetadata[PlatformMetadataKeys.EpicAppName] = localIds.AppName;
            }

            ctx.PlatformMetadata[PlatformMetadataKeys.EpicStatus] = globalItem is null ? "Orphaned" : "Installed";

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
                ctx.PlatformMetadata[PlatformMetadataKeys.BlizzardProduct] = product;
            }
        }
    }
}