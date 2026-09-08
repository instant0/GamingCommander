using GamingCommander.Core.Models;

namespace GamingCommander.UI.ViewModels;

/// <summary>
/// Scan-status half of <see cref="ShellViewModel"/> (Plan 125 final stage):
/// F5 background-scan state and the "⏳ Scanning..." root badges.
/// </summary>
public sealed partial class ShellViewModel
{
    /// <summary>
    /// Marks a root as currently being scanned. Updates the scanning badge on root entries.
    /// Must be called from the UI thread.
    /// </summary>
    public void SetScanning(string rootPath)
    {
        ScanningRootPath = rootPath;
        IsScanning = true;
        UpdateScanningBadges();
    }

    /// <summary>
    /// Clears scanning state and removes all scanning badges.
    /// Must be called from the UI thread.
    /// </summary>
    public void ClearScanning()
    {
        ScanningRootPath = null;
        IsScanning = false;
        UpdateScanningBadges();
    }

    /// <summary>
    /// Updates scanning badges on root-level items based on current ScanningRootPath.
    /// </summary>
    private void UpdateScanningBadges()
    {
        if (!IsAtRootLevel) return;

        for (int i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            string expectedBadge = string.Equals(item.PathSummary, ScanningRootPath, StringComparison.OrdinalIgnoreCase)
                ? "⏳ Scanning..."
                : string.Empty;

            if (item.ScanningBadge != expectedBadge)
            {
                Items[i] = new ShellPaneItemViewModel
                {
                    Title = item.Title,
                    Subtitle = item.Subtitle,
                    LeftPath = item.LeftPath,
                    SourceLabel = item.SourceLabel,
                    PathSummary = item.PathSummary,
                    LaunchTarget = item.LaunchTarget,
                    LastScanned = item.LastScanned,
                    CommandLineArguments = item.CommandLineArguments,
                    Kind = item.Kind,
                    LastModified = item.LastModified,
                    ResolvedType = item.ResolvedType,
                    HasOverride = item.HasOverride,
                    GameId = item.GameId,
                    PlatformId = item.PlatformId,
                    PlatformStatus = item.PlatformStatus,
                    PlatformStatusColor = item.PlatformStatusColor,
                    PlatformStatusDetail = item.PlatformStatusDetail,
                    ItemStatusColor = item.ItemStatusColor,
                    GameCount = item.GameCount,
                    ScanningBadge = expectedBadge,
                    Tags = item.Tags,
                    TagBadges = item.TagBadges,
                    StoreBadge = item.StoreBadge,
                    LibraryName = item.LibraryName,
                };
            }
        }
    }
}