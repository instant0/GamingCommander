namespace GamingCommander.Core.Models;

/// <summary>
/// Identity fields for a game entry (Plan 127 A1): what the game IS, independent of where it
/// lives or how it is launched. Grouped out of the flat <see cref="GameEntry"/> surface.
/// </summary>
public sealed record IdentityInfo
{
    /// <summary>Deterministic unique identifier (MD5-based).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Library (anchor) name this game belongs to (e.g. "Steam", "GOG").</summary>
    public string Library { get; init; } = string.Empty;

    /// <summary>Human-readable game name shown in the UI.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Detected or overridden store/platform type.</summary>
    public GameSourceKind GameSource { get; init; } = GameSourceKind.Unknown;

    /// <summary>True if the user manually changed the source type.</summary>
    public bool IsSourceOverridden { get; init; }

    /// <summary>
    /// Natural keys used to re-match this game after an external move/rename (Plan 127 A5).
    /// Rebuilt from the scan; not user state.
    /// </summary>
    public List<IdentityKey> IdentityKeys { get; init; } = [];
}
