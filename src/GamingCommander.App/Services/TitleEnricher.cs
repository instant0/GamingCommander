using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>
/// Non-store title enrichment for <see cref="FolderScanner"/> (Plan 125 Phase 2).
/// Applied AFTER store enrichment and only while no store parser has set a title:
/// FolderExeMatch → PE FileDescription (with guards) → Ubisoft readme.
/// Reads only — never writes manifests/configs.
/// </summary>
internal static class TitleEnricher
{
    /// <summary>
    /// Applies the FolderExeMatch / PE FileDescription / Ubisoft readme title rules
    /// to <paramref name="ctx"/>, preserving the exact guards, ordering and metadata
    /// keys from the original <c>FolderScanner.AddGameEntry</c> implementation.
    /// </summary>
    /// <param name="gameDir">Game folder (for the Ubisoft readme rule).</param>
    /// <param name="primaryExePath">The discovery primary exe path (may be null — the LNK fallback path is not used by the FolderExeMatch rule).</param>
    /// <param name="primaryPeFileDescription">PE FileDescription of the primary exe (may be null/whitespace).</param>
    public static void EnrichNonStore(
        DirectoryInfo gameDir,
        string? primaryExePath,
        string? primaryPeFileDescription,
        GameSourceKind resolvedType,
        IReadOnlyList<string> peMetadataBlacklist,
        GameEntryBuildContext ctx)
    {
        if (!ctx.StoreEnrichedDisplayName
            && primaryExePath is not null
            && TitleText.MatchesFolderAndExe(gameDir.Name, Path.GetFileNameWithoutExtension(primaryExePath)))
        {
            ctx.PlatformMetadata[PlatformMetadataKeys.TitleSource] = TitleSourceValues.FolderExeMatch;
            ctx.StoreEnrichedDisplayName = true;
        }

        // PE FileDescription enrichment for non-store-enriched games (Plan 112 Step 2).
        // Uses FileDescription from the primary exe's PE metadata as the display name
        // when no store parser has provided one and the PE data passes guard conditions.
        if (!ctx.StoreEnrichedDisplayName && !string.IsNullOrWhiteSpace(primaryPeFileDescription))
        {
            string peDesc = primaryPeFileDescription;

            // Guard: reject short strings (likely noise like single/double chars)
            bool lengthOk = peDesc.Length > 2;

            // Guard: reject generic placeholders from pe_metadata_blacklist
            bool notPlaceholder = true;
            if (peMetadataBlacklist.Count > 0)
            {
                string peDescLower = peDesc.ToLowerInvariant();
                notPlaceholder = !peMetadataBlacklist.Any(p => peDescLower.Contains(p));
            }

            // Guard: token share with the folder — replaced by the acronym
            // relaxation for SHORT folders (E6, 2026-09-07). An acronym folder
            // (jag2, mmxl) has no substring share with the full PE title by
            // definition; AcronymMatchesTitle validates via initial letters.
            // elexII ↔ "System" still fails both and stays blocked.
            bool nameMatches = TitleText.SharesNameToken(peDesc, gameDir.Name)
                || TitleText.AcronymMatchesTitle(peDesc, gameDir.Name);

            if (lengthOk && notPlaceholder
                && PeProductYear.IsUsefulTitle(peDesc)
                && !TitleText.IsGenericLabel(peDesc)
                && nameMatches)
            {
                ctx.PlatformMetadata[PlatformMetadataKeys.AutoDetectedTitle] = ctx.DisplayName;
                ctx.DisplayName = peDesc;
                ctx.PlatformMetadata[PlatformMetadataKeys.TitleSource] = TitleSourceValues.PeFileDescription;
                ctx.StoreEnrichedDisplayName = true;
            }
        }

        // Ubisoft Support/Readme enrichment (Plan 112 Step 3B).
        // Ubisoft games ship Support/Readme/ with publisher and game title on lines 1-2.
        // Applied only for Ubisoft Connect games and only when no store/PE enrichment was used.
        if (!ctx.StoreEnrichedDisplayName && resolvedType == GameSourceKind.UbisoftConnect)
        {
            var ubiInfo = UbisoftReadmeParser.TryParse(gameDir);
            if (ubiInfo?.GameTitle is not null)
            {
                ctx.PlatformMetadata[PlatformMetadataKeys.AutoDetectedTitle] = ctx.DisplayName;
                ctx.DisplayName = ubiInfo.GameTitle;
                ctx.PlatformMetadata[PlatformMetadataKeys.TitleSource] = TitleSourceValues.UbisoftReadme;
            }
        }
    }
}