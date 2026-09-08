using System.IO;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.UI.ViewModels;

/// <summary>
/// Row-loading half of <see cref="ShellViewModel"/> (Plan 125 Phase 1b + final
/// stage): the library-detail and cross-library filter loaders plus the shared
/// <c>GameEntry → ShellPaneItemViewModel</c> projection.
/// </summary>
public sealed partial class ShellViewModel
{
    private void LoadGamesForLibrary(string libraryName)
    {
        Items.Clear();
        IReadOnlyList<GameEntry> games = _libraryManager.GetGamesForLibrary(libraryName);

        // Add ".." parent-directory entry at the top
        Items.Add(new ShellPaneItemViewModel
        {
            Title = "..",
            SourceLabel = string.Empty,
            PathSummary = "Parent directory",
            LaunchTarget = string.Empty,
            Kind = FileSystemEntryKind.ParentDirectory,
            LastModified = default,
            ResolvedType = string.Empty,
            HasOverride = false,
            GameId = null,
            GameCount = 0,
        });

        foreach (GameEntry game in games)
        {
            ShellPaneItemViewModel item = ToPaneItem(game, libraryName, leftPathOverride: null);

            // Library-detail extras (filter rows intentionally stay plain):
            // status colors + rich status detail + multiple-exe hints.
            // Design: Installed = white (default), only show colors for problems.
            string statusColor = item.PlatformStatus switch
            {
                "Moved" => "#E8C547",
                "Orphaned" => "#E87070",
                "Missing" => "#E87070",
                _ => string.Empty,
            };
            item.PlatformStatusColor = statusColor;
            item.ItemStatusColor = statusColor;

            item.PlatformStatusDetail = item.PlatformStatus switch
            {
                "Moved" => FormatMovedDetail(game),
                "Missing" => FormatMissingDetail(game),
                "Orphaned" => FormatOrphanedDetail(game),
                _ => string.Empty,
            };

            item.HasMultipleExes = game.PlatformMetadata.TryGetValue(PlatformMetadataKeys.ExeCandidateCount, out string? exeCount)
                && int.TryParse(exeCount, out int n) && n > 1;
            item.AlternateExes = game.PlatformMetadata.GetValueOrDefault(PlatformMetadataKeys.ExeCandidates, string.Empty);

            Items.Add(item);
        }

        OnPropertyChanged(nameof(ItemCount));
        NavigationChanged?.Invoke();
    }

    private void LoadFilteredGames()
    {
        GameFilter? filter = ActiveFilter;
        Items.Clear();
        Items.Add(new ShellPaneItemViewModel
        {
            Title = "..",
            SourceLabel = string.Empty,
            PathSummary = "Clear filter",
            LaunchTarget = string.Empty,
            Kind = FileSystemEntryKind.ParentDirectory,
            LastModified = default,
            ResolvedType = string.Empty,
        });

        if (filter is null)
        {
            OnPropertyChanged(nameof(ItemCount));
            NavigationChanged?.Invoke();
            UpdateDetailsForSelection();
            return;
        }

        foreach ((string libraryName, GameEntry game, IReadOnlyList<string> extra) in EnumerateGamesWithExtraTags())
        {
            if (!GameFilterMatcher.Matches(game, filter, extra))
                continue;

            Items.Add(ToPaneItem(game, libraryName, leftPathOverride: libraryName));
        }

        _selectedIndex = 0;
        OnPropertyChanged(nameof(SelectedIndex));
        OnPropertyChanged(nameof(ItemCount));
        UpdateDetailsForSelection();
        NavigationChanged?.Invoke();
    }

    /// <summary>
    /// Shared GameEntry → ShellPaneItemViewModel projection used by both the
    /// library-detail loader and the cross-library filter loader. Produces the
    /// common row surface; library-detail-only extras (status colors, rich
    /// status detail, multiple-exe hints) are added by the caller.
    /// <paramref name="leftPathOverride"/> is the left-pane path to display
    /// (filter rows show the library name); when null/empty the exe filename
    /// is shown (library-detail rows).
    /// </summary>
    private ShellPaneItemViewModel ToPaneItem(
        GameEntry game, string libraryName, string? leftPathOverride)
    {
        // Extract platform-specific metadata from Extra dictionary
        string platformId = game.GameSource switch
        {
            GameSourceKind.Steam => game.PlatformMetadata.TryGetValue(PlatformMetadataKeys.SteamAppId, out var steamAppId) ? steamAppId : string.Empty,
            GameSourceKind.Epic => game.PlatformMetadata.TryGetValue(PlatformMetadataKeys.EpicCatalogItemId, out var epicCatalogItemId) ? epicCatalogItemId : string.Empty,
            _ => string.Empty,
        };

        string platformStatus = game.GameSource switch
        {
            GameSourceKind.Steam => game.PlatformMetadata.TryGetValue(PlatformMetadataKeys.SteamStatus, out var status) ? status : string.Empty,
            GameSourceKind.Epic => game.PlatformMetadata.TryGetValue(PlatformMetadataKeys.EpicStatus, out var epicStatus) ? epicStatus : string.Empty,
            _ => string.Empty,
        };

        // Parenthetical subtitle: tags if present, empty otherwise
        GameMetadataRecord? sidecar = _metadataStore?.Get(game.Id);
        List<string> engineTags = TagNormalizer.SplitList(sidecar?.Engine);
        List<string> mergedTags = TagNormalizer.Merge(
            game.Tags, TagNormalizer.FromMetadata(sidecar?.Genre, sidecar?.Engine));
        string subtitle = mergedTags.Count > 0 ? $"({string.Join(", ", mergedTags)})" : string.Empty;

        // Resolve launch target: prefer steam:// URI over raw exe path
        string launchTarget = game.CommandLineArguments.StartsWith("steam://", StringComparison.OrdinalIgnoreCase)
            ? game.CommandLineArguments
            : game.ExecutablePath;

        return new ShellPaneItemViewModel
        {
            Title = game.DisplayName,
            Subtitle = subtitle,
            LeftPath = leftPathOverride is { Length: > 0 }
                ? leftPathOverride
                : Path.GetFileName(game.ExecutablePath),
            SourceLabel = GameSourceParser.ToDisplayName(game.GameSource),
            PathSummary = string.IsNullOrWhiteSpace(game.ExecutablePath)
                ? game.PlatformMetadata.GetValueOrDefault(PlatformMetadataKeys.GameFolder, "")
                : game.ExecutablePath,
            InstallDirectory = game.PlatformMetadata.GetValueOrDefault(PlatformMetadataKeys.GameFolder, "") is { Length: > 0 } gameFolder
                ? gameFolder
                : WindowsExplorer.ParentDirectory(game.ExecutablePath) ?? string.Empty,
            LaunchTarget = launchTarget,
            CommandLineArguments = game.CommandLineArguments,
            Kind = FileSystemEntryKind.File,
            LastModified = game.LastModified,
            LastScanned = game.LastScanned,
            ResolvedType = game.IsSourceOverridden
                ? GameSourceParser.ToDisplayName(game.GameSource) + " (override)"
                : GameSourceParser.ToDisplayName(game.GameSource),
            HasOverride = game.IsSourceOverridden,
            GameId = game.Id,
            PlatformId = platformId,
            PlatformStatus = platformStatus,
            GameCount = 0,
            Tags = mergedTags.Count > 0 ? string.Join(", ", mergedTags) : string.Empty,
            TagBadges = BuildTagBadges(mergedTags, engineTags),
            StoreBadge = BuildStoreBadge(game.GameSource),
            LibraryName = libraryName,
        };
    }
}