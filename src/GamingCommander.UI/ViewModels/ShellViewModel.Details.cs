using System.IO;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.UI.ViewModels;

/// <summary>
/// Right-pane details surface of <see cref="ShellViewModel"/> (Plan 125 final
/// stage): selection-derived properties, platform status/repair flags, the
/// details refresh batch, and generic detail formatting. Sidecar extras and
/// Steam/Epic status-detail text live in the sibling partials.
/// </summary>
public sealed partial class ShellViewModel
{
    private GameMetadataRecord? _selectedMetadata;

    /// <summary>Display name of the currently selected game, shown in the details panel.</summary>
    public string DetailsName => SelectedItem?.Title ?? string.Empty;
    /// <summary>Install path of the currently selected game.</summary>
    public string DetailsPath => SelectedItem?.PathSummary ?? string.Empty;
    /// <summary>Folder that contains the game exe. Empty when unknown.</summary>
    public string DetailsGameFolder => SelectedInstallDirectory ?? string.Empty;
    public bool DetailsGameFolderClickable =>
        WindowsExplorer.IsClickableFolder(DetailsGameFolder, SelectedInstallDirectory);
    /// <summary>Source type of the currently selected game (e.g., Steam, GOG).</summary>
    public string DetailsType => SelectedItem?.SourceLabel ?? string.Empty;
    public string DetailsTypeLine
    {
        get
        {
            if (string.IsNullOrEmpty(DetailsType))
                return string.Empty;
            if (DetailsStoreBadge is not null)
                return HasOverride ? "Type: (Override)" : "Type:";
            return HasOverride ? $"Type: {DetailsType} (Override)" : $"Type: {DetailsType}";
        }
    }
    public TagBadgeViewModel? DetailsStoreBadge => SelectedItem?.StoreBadge;
    public bool HasStoreBadge => DetailsStoreBadge is not null;
    /// <summary>Primary executable path of the currently selected game.</summary>
    public string DetailsExecutable => SelectedItem?.LaunchTarget ?? string.Empty;
    /// <summary>Exe path for the footer (not steam://).</summary>
    public string DetailsExePath
    {
        get
        {
            string? path = SelectedItem?.PathSummary;
            if (!string.IsNullOrWhiteSpace(path)
                && !path.StartsWith("steam://", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            return DetailsExecutable;
        }
    }
    public bool HasMultipleExes => SelectedItem?.HasMultipleExes == true;
    public string MultipleExeWarning =>
        HasMultipleExes
            ? "Multiple EXE files detected — press F4 to choose the main one."
            : string.Empty;
    /// <summary>Folder last-write; used as fallback when the exe time is unknown.</summary>
    public string DetailsLastModified => FormatTimestamp(SelectedItem?.LastModified);
    public string DetailsDetectedOn => FormatDate(SelectedItem?.LastScanned);
    public bool HasDetectedOn =>
        SelectedItem is { Kind: FileSystemEntryKind.File } item
        && item.LastScanned != default;
    public string DetailsExeModified => ExeOrFolderModified();
    /// <summary>The resolved source type as a human-readable string.</summary>
    public string DetailsResolvedType => SelectedItem?.ResolvedType ?? string.Empty;
    /// <summary>Platform-specific identifier (Steam App ID, Epic Catalog ID, etc.).</summary>
    public string DetailsPlatformId => SelectedItem?.PlatformId ?? string.Empty;
    /// <summary>True when a platform-specific identifier is available for the selected game.</summary>
    public bool HasPlatformId => !string.IsNullOrEmpty(SelectedItem?.PlatformId);
    /// <summary>Platform status text (Installed, Moved, Orphaned, Missing).</summary>
    public string DetailsPlatformStatus => SelectedItem?.PlatformStatus ?? string.Empty;
    /// <summary>True when platform status information is available.</summary>
    public bool HasPlatformStatus => !string.IsNullOrEmpty(SelectedItem?.PlatformStatus);
    /// <summary>Hex color code for the platform status display.</summary>
    public string DetailsPlatformStatusColor => SelectedItem?.PlatformStatusColor ?? string.Empty;
    /// <summary>Detailed status text (e.g., 'Moved — ACF expects: D:\...').</summary>
    public string DetailsPlatformStatusDetail => SelectedItem?.PlatformStatusDetail ?? string.Empty;
    /// <summary>True when detailed platform status information is available.</summary>
    public bool HasPlatformStatusDetail => !string.IsNullOrEmpty(SelectedItem?.PlatformStatusDetail);
    public bool IsSteamOrphaned =>
        string.Equals(DetailsPlatformStatus, "Orphaned", StringComparison.OrdinalIgnoreCase)
        && SelectedItem?.SourceLabel is not "Epic"
        && !string.Equals(SelectedItem?.ResolvedType, "Epic", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(SelectedItem?.ResolvedType, "Epic (override)", StringComparison.OrdinalIgnoreCase);

    public bool IsEpicOrphaned =>
        string.Equals(DetailsPlatformStatus, "Orphaned", StringComparison.OrdinalIgnoreCase)
        && (SelectedItem?.SourceLabel == "Epic"
            || (SelectedItem?.ResolvedType?.StartsWith("Epic", StringComparison.OrdinalIgnoreCase) ?? false));
    /// <summary>Sidecar or ACF AppID — required to write <c>appmanifest_{id}.acf</c>.</summary>
    public string SteamAppIdForAcf
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(SelectedItem?.PlatformId)
                && SelectedItem.PlatformId.All(char.IsDigit))
            {
                return SelectedItem.PlatformId;
            }

            string? side = _selectedMetadata?.SteamAppId;
            return !string.IsNullOrWhiteSpace(side) && side.All(char.IsDigit) ? side.Trim() : string.Empty;
        }
    }
    public bool CanWriteSteamAcf => IsSteamOrphaned && SteamAppIdForAcf.Length > 0;
    /// <summary>True when any item is selected in the left pane.</summary>
    public bool HasSelection => SelectedItem is not null;
    /// <summary>True when a game file (not a directory or parent) is selected.</summary>
    public bool HasGameSelected => SelectedItem is { Kind: FileSystemEntryKind.File };
    /// <summary>True when the selected game has a user-defined folder override.</summary>
    public bool HasOverride => SelectedItem?.HasOverride == true;

    private string? SelectedInstallDirectory =>
        string.IsNullOrWhiteSpace(SelectedItem?.InstallDirectory) ? null : SelectedItem.InstallDirectory;

    private void UpdateDetailsForSelection()
    {
        string? gameId = SelectedItem?.GameId;
        _selectedMetadata = gameId is null ? null : _metadataStore?.Get(gameId);

        OnPropertyChanged(nameof(SelectedItem));
        OnPropertyChanged(nameof(DetailsName));
        OnPropertyChanged(nameof(DetailsPath));
        OnPropertyChanged(nameof(DetailsGameFolder));
        OnPropertyChanged(nameof(DetailsGameFolderClickable));
        OnPropertyChanged(nameof(DetailsType));
        OnPropertyChanged(nameof(DetailsTypeLine));
        OnPropertyChanged(nameof(DetailsStoreBadge));
        OnPropertyChanged(nameof(HasStoreBadge));
        OnPropertyChanged(nameof(DetailsExePath));
        OnPropertyChanged(nameof(DetailsExecutable));
        OnPropertyChanged(nameof(HasMultipleExes));
        OnPropertyChanged(nameof(MultipleExeWarning));
        OnPropertyChanged(nameof(DetailsLastModified));
        OnPropertyChanged(nameof(DetailsDetectedOn));
        OnPropertyChanged(nameof(HasDetectedOn));
        OnPropertyChanged(nameof(DetailsExeModified));
        OnPropertyChanged(nameof(DetailsPlatformId));
        OnPropertyChanged(nameof(HasPlatformId));
        OnPropertyChanged(nameof(DetailsPlatformStatus));
        OnPropertyChanged(nameof(HasPlatformStatus));
        OnPropertyChanged(nameof(DetailsPlatformStatusColor));
        OnPropertyChanged(nameof(DetailsPlatformStatusDetail));
        OnPropertyChanged(nameof(HasPlatformStatusDetail));
        OnPropertyChanged(nameof(IsSteamOrphaned));
        OnPropertyChanged(nameof(IsEpicOrphaned));
        OnPropertyChanged(nameof(SteamAppIdForAcf));
        OnPropertyChanged(nameof(CanWriteSteamAcf));
        OnPropertyChanged(nameof(DetailsResolvedType));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(HasGameSelected));
        OnPropertyChanged(nameof(HasOverride));
        OnPropertyChanged(nameof(DetailsTags));
        OnPropertyChanged(nameof(HasTags));
        OnPropertyChanged(nameof(DetailsTagBadges));
        OnPropertyChanged(nameof(DetailsDeveloper));
        OnPropertyChanged(nameof(DetailsPublisher));
        OnPropertyChanged(nameof(DetailsGenre));
        OnPropertyChanged(nameof(DetailsEngine));
        OnPropertyChanged(nameof(DetailsReleaseDate));
        OnPropertyChanged(nameof(DetailsMetacritic));
        OnPropertyChanged(nameof(DetailsPcgwUrl));
        OnPropertyChanged(nameof(HasMetadataExtras));
        OnPropertyChanged(nameof(DetailsConfigPath));
        OnPropertyChanged(nameof(DetailsSavePath));
        OnPropertyChanged(nameof(DetailsConfigPathClickable));
        OnPropertyChanged(nameof(DetailsSavePathClickable));
        OnPropertyChanged(nameof(DetailsConfigPathIsRegistry));
        OnPropertyChanged(nameof(DetailsConfigPathDisplayOnly));
        OnPropertyChanged(nameof(DetailsSavePathDisplayOnly));
        OnPropertyChanged(nameof(DetailsCommandLine));
        OnPropertyChanged(nameof(DetailsVideo));
        OnPropertyChanged(nameof(HasMetadataDetails));
    }

    /// <summary>Applies a sidecar row after a background refresh (Plan 119 step 5). Call on the UI thread.</summary>
    public void ApplySidecarMetadata(string gameEntryId, GameMetadataRecord? record)
    {
        if (SelectedItem?.GameId != gameEntryId)
            return;

        _selectedMetadata = record;
        OnPropertyChanged(nameof(DetailsDeveloper));
        OnPropertyChanged(nameof(DetailsPublisher));
        OnPropertyChanged(nameof(DetailsGenre));
        OnPropertyChanged(nameof(DetailsEngine));
        OnPropertyChanged(nameof(DetailsReleaseDate));
        OnPropertyChanged(nameof(DetailsMetacritic));
        OnPropertyChanged(nameof(DetailsPcgwUrl));
        OnPropertyChanged(nameof(HasMetadataExtras));
        OnPropertyChanged(nameof(DetailsTags));
        OnPropertyChanged(nameof(HasTags));
        OnPropertyChanged(nameof(DetailsTagBadges));
        OnPropertyChanged(nameof(DetailsConfigPath));
        OnPropertyChanged(nameof(DetailsSavePath));
        OnPropertyChanged(nameof(DetailsConfigPathClickable));
        OnPropertyChanged(nameof(DetailsSavePathClickable));
        OnPropertyChanged(nameof(DetailsConfigPathIsRegistry));
        OnPropertyChanged(nameof(DetailsConfigPathDisplayOnly));
        OnPropertyChanged(nameof(DetailsSavePathDisplayOnly));
        OnPropertyChanged(nameof(DetailsCommandLine));
        OnPropertyChanged(nameof(DetailsVideo));
        OnPropertyChanged(nameof(HasMetadataDetails));
        OnPropertyChanged(nameof(SteamAppIdForAcf));
        OnPropertyChanged(nameof(CanWriteSteamAcf));
    }

    private static string FormatTimestamp(DateTimeOffset? timestamp)
    {
        if (!timestamp.HasValue || timestamp.Value == default) return "—";
        return timestamp.Value.ToString("yyyy-MM-dd HH:mm");
    }

    private static string FormatDate(DateTimeOffset? timestamp)
    {
        if (!timestamp.HasValue || timestamp.Value == default) return string.Empty;
        return timestamp.Value.ToString("yyyy-MM-dd");
    }

    private string ExeOrFolderModified()
    {
        string? exe = SelectedItem?.PathSummary;
        if (!string.IsNullOrWhiteSpace(exe)
            && !exe.StartsWith("steam://", StringComparison.OrdinalIgnoreCase)
            && File.Exists(exe))
        {
            try
            {
                return FormatTimestamp(new DateTimeOffset(File.GetLastWriteTimeUtc(exe)));
            }
            catch
            {
            }
        }

        return DetailsLastModified;
    }
}