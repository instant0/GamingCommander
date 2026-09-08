using GamingCommander.Core.Services;

namespace GamingCommander.UI.ViewModels;

/// <summary>
/// Sidecar extras surface of <see cref="ShellViewModel"/> (Plan 119, Plan 125
/// final stage): PCGW-fetched developer/publisher/genre/engine/date fields and
/// the config/save/args/video path summaries in the right pane.
/// </summary>
public sealed partial class ShellViewModel
{
    /// <summary>Sidecar extras for the selected game (Plan 119). Empty when no sidecar row.</summary>
    public string DetailsDeveloper => _selectedMetadata?.Developer ?? string.Empty;
    public string DetailsPublisher => _selectedMetadata?.Publisher ?? string.Empty;
    public string DetailsGenre => _selectedMetadata?.Genre ?? string.Empty;
    public string DetailsEngine => _selectedMetadata?.Engine ?? string.Empty;
    public string DetailsReleaseDate => _selectedMetadata?.ReleaseDate ?? string.Empty;
    public string DetailsMetacritic => _selectedMetadata?.MetacriticScore?.ToString() ?? string.Empty;
    public string DetailsPcgwUrl => _selectedMetadata?.PcGamingWikiUrl ?? string.Empty;
    /// <summary>True when the sidecar has at least one extra field for the selection.</summary>
    public bool HasMetadataExtras => _selectedMetadata?.HasDisplayableExtras == true;

    /// <summary>Windows config path from PCGW, tokens expanded. Empty when unknown.</summary>
    public string DetailsConfigPath =>
        MetadataDetailsFormatter.WindowsConfig(_selectedMetadata?.Details, SelectedInstallDirectory);
    /// <summary>Windows save path from PCGW, tokens expanded. Empty when unknown.</summary>
    public string DetailsSavePath =>
        MetadataDetailsFormatter.WindowsSaves(_selectedMetadata?.Details, SelectedInstallDirectory);
    public bool DetailsConfigPathClickable =>
        WindowsExplorer.IsClickableFolder(DetailsConfigPath, SelectedInstallDirectory);
    public bool DetailsSavePathClickable =>
        WindowsExplorer.IsClickableFolder(DetailsSavePath, SelectedInstallDirectory);
    public bool DetailsConfigPathIsRegistry =>
        PcgwPathTokens.IsRegistry(DetailsConfigPath);
    public bool DetailsConfigPathDisplayOnly =>
        !string.IsNullOrEmpty(DetailsConfigPath) && !DetailsConfigPathClickable && !DetailsConfigPathIsRegistry;
    public bool DetailsSavePathDisplayOnly =>
        !string.IsNullOrEmpty(DetailsSavePath) && !DetailsSavePathClickable;

    /// <summary>Short PCGW argument catalog for the right pane.</summary>
    public string DetailsCommandLine => MetadataDetailsFormatter.CommandLineSummary(_selectedMetadata?.Details);
    /// <summary>Short video caps (fov, ultrawide, …).</summary>
    public string DetailsVideo => MetadataDetailsFormatter.VideoSummary(_selectedMetadata?.Details);
    /// <summary>True when sidecar operator details exist (paths / args / video).</summary>
    public bool HasMetadataDetails => _selectedMetadata?.Details?.HasAny == true;
}