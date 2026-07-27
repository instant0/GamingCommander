namespace GamingCommander.UI.ViewModels;

/// <summary>
/// View model for a single tag badge with configurable colors.
/// Used in both left-pane list and right-pane details.
/// </summary>
public sealed class TagBadgeViewModel
{
    /// <summary>Display name of the tag (e.g., "Steam", "Unreal Engine", "RPG").</summary>
    public required string Name { get; init; }

    /// <summary>Background hex color for the badge (e.g., "#1B2838").</summary>
    public required string Background { get; init; }

    /// <summary>Foreground (text) hex color for the badge (e.g., "#B8C8D8").</summary>
    public required string Foreground { get; init; }
}
