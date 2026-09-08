using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.UI.ViewModels;

/// <summary>
/// Tag and store-badge half of <see cref="ShellViewModel"/> (Plan 125 final stage):
/// merged user/metadata tags, colored tag badges, and the store badge mapping.
/// </summary>
public sealed partial class ShellViewModel
{
    /// <summary>User tags plus sidecar genre/engine.</summary>
    public string DetailsTags => string.Join(", ", SelectedMergedTags());
    /// <summary>True when the selected game has user or metadata tags.</summary>
    public bool HasTags => SelectedMergedTags().Count > 0;
    /// <summary>Tag badges with colors for the selected game.</summary>
    public List<TagBadgeViewModel> DetailsTagBadges =>
        BuildTagBadges(SelectedMergedTags(), TagNormalizer.SplitList(_selectedMetadata?.Engine));

    private List<string> SelectedMergedTags()
    {
        IEnumerable<string> user = string.IsNullOrWhiteSpace(SelectedItem?.Tags)
            ? []
            : TagNormalizer.ParseFromCommaSeparated(SelectedItem.Tags);
        return TagNormalizer.Merge(
            user, TagNormalizer.FromMetadata(_selectedMetadata?.Genre, _selectedMetadata?.Engine));
    }

    /// <summary>
    /// Builds tag badge view models with configurable colors for each tag.
    /// </summary>
    private List<TagBadgeViewModel> BuildTagBadges(
        IReadOnlyList<string> tags,
        IReadOnlyList<string>? engineNames = null)
    {
        if (tags.Count == 0) return [];

        var engines = new HashSet<string>(engineNames ?? [], StringComparer.OrdinalIgnoreCase);
        var badges = new List<TagBadgeViewModel>(tags.Count);
        foreach (string tag in tags)
        {
            TagType tagType = engines.Contains(tag)
                ? TagType.Engine
                : _tagColorProvider?.GetTagType(tag) ?? TagType.User;
            var (bg, fg) = _tagColorProvider?.GetColor(tag, tagType) ?? ("#2A3A4A", "#B8C8D8");
            badges.Add(new TagBadgeViewModel { Name = tag, Background = bg, Foreground = fg });
        }
        return badges;
    }

    /// <summary>
    /// Mapping from GameSourceKind to (display label, color config key).
    /// Display label is what appears on the badge; color key is looked up in tag_colors.json.
    /// </summary>
    private static readonly Dictionary<GameSourceKind, (string Label, string ColorKey)> s_storeBadgeMap = new()
    {
        [GameSourceKind.Steam] = ("Steam", "Steam"),
        [GameSourceKind.Gog] = ("GOG", "GOG"),
        [GameSourceKind.Epic] = ("Epic", "Epic"),
        [GameSourceKind.EaApp] = ("EA", "EA"),
        [GameSourceKind.UbisoftConnect] = ("Ubisoft", "Ubisoft"),
        [GameSourceKind.BattleNet] = ("Battle.net", "BattleNet"),
        [GameSourceKind.Xbox] = ("Xbox", "Xbox"),
        [GameSourceKind.Rockstar] = ("Rockstar", "Rockstar"),
        [GameSourceKind.SteamEmu] = ("Steam Emu", "Steam Emu"),
    };

    /// <summary>
    /// Builds a store badge for the left pane. Returns null for Standalone/Unknown (no badge shown).
    /// </summary>
    private TagBadgeViewModel? BuildStoreBadge(GameSourceKind source)
    {
        if (!s_storeBadgeMap.TryGetValue(source, out var entry))
            return null;

        var (bg, fg) = _tagColorProvider?.GetColor(entry.ColorKey, TagType.Store) ?? ("#2A3A4A", "#B8C8D8");
        return new TagBadgeViewModel { Name = entry.Label, Background = bg, Foreground = fg };
    }
}