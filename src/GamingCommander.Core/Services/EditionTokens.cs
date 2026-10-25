namespace GamingCommander.Core.Services;

/// <summary>
/// Shared edition vocabulary for title display normalization and PCGW search stripping.
/// The two callers deliberately use different token sets and matching algorithms:
/// display removes substrings anywhere in a folder name, while search strips only an
/// anchored <c>-</c>/<c>:</c> tail. Keeping the sets here means a token is added once.
/// </summary>
public static class EditionTokens
{
    /// <summary>
    /// Edition words removed from folder names for display, in removal order.
    /// Specific editions must precede the generic <c>Edition</c> so removing the
    /// generic word does not leave a stray qualifier behind.
    /// </summary>
    public static readonly IReadOnlyList<string> DisplayTokens =
    [
        "Remastered",
        "Definitive Edition",
        "Enhanced Edition",
        "Ultimate Edition",
        "Special Edition",
        "GOTY",
        "Edition",
    ];

    /// <summary>
    /// Edition words stripped only when they form a <c>-</c>/<c>:</c> tail
    /// (optionally followed by <c>Edition</c>) during PCGW search normalization.
    /// </summary>
    public static readonly IReadOnlyList<string> TailTokens =
    [
        "Reloaded",
        "Definitive",
        "Complete",
        "Enhanced",
        "Ultimate",
        "Deluxe",
        "Gold",
        "Anniversary",
        "Game of the Year",
        "GOTY",
    ];
}
