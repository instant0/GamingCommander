namespace GamingCommander.Core.Models;

/// <summary>
/// Shared logic for inferring GameSourceKind from folder paths and parsing display names.
/// Used by LibrarySetupViewModel and GameSetupWindow.
/// </summary>
public static class GameSourceParser
{
    /// <summary>
    /// Ordered (kind, display-name) pairs — the single source of truth for the
    /// enum ↔ display-name mapping. Combo-box order is the array order.
    /// Plan 125 Phase 2d — replaces the three manually-synced parallel lists
    /// (SourceDisplayNames / ParseFromString / ToDisplayName).
    /// </summary>
    private static readonly (GameSourceKind Kind, string DisplayName)[] SourceKinds =
    [
        (GameSourceKind.Standalone, "Standalone"),
        (GameSourceKind.Steam, "Steam"),
        (GameSourceKind.Gog, "GOG"),
        (GameSourceKind.Epic, "Epic"),
        (GameSourceKind.EaApp, "EA App"),
        (GameSourceKind.UbisoftConnect, "Ubisoft Connect"),
        (GameSourceKind.BattleNet, "Battle.net"),
        (GameSourceKind.Xbox, "Xbox"),
        (GameSourceKind.Rockstar, "Rockstar"),
        (GameSourceKind.SteamEmu, "Steam Emulator"),
    ];

    /// <summary>
    /// Human-readable display names for all supported game source types.
    /// Used by UI dropdowns and combo boxes in GameSetup, LibrarySetup, and Wizard windows.
    /// </summary>
    public static readonly string[] SourceDisplayNames =
        SourceKinds.Select(p => p.DisplayName).ToArray();

    private static readonly IReadOnlyDictionary<GameSourceKind, string> KindToDisplay =
        SourceKinds.ToDictionary(p => p.Kind, p => p.DisplayName);

    private static readonly IReadOnlyDictionary<string, GameSourceKind> DisplayToKind =
        SourceKinds.ToDictionary(p => p.DisplayName, p => p.Kind, StringComparer.Ordinal);

    /// <summary>
    /// Infer GameSourceKind from a file path by matching known store name tokens.
    /// Used when adding a library root to suggest the most likely store type.
    /// </summary>
    public static GameSourceKind InferFromPath(string path)
    {
        string lower = path.ToLowerInvariant();
        if (lower.Contains("steam")) return GameSourceKind.Steam;
        if (lower.Contains("epicgameslauncher") || lower.Contains("epic")) return GameSourceKind.Epic;
        if (lower.Contains("gog")) return GameSourceKind.Gog;
        if (lower.Contains("ea ") || lower.Contains("electronic arts")) return GameSourceKind.EaApp;
        if (lower.Contains("ubisoft")) return GameSourceKind.UbisoftConnect;
        if (lower.Contains("battle.net") || lower.Contains("blizzard") || lower.Contains("battlenet")) return GameSourceKind.BattleNet;
        if (lower.Contains("xbox")) return GameSourceKind.Xbox;
        if (lower.Contains("rockstar")) return GameSourceKind.Rockstar;
        return GameSourceKind.Standalone;
    }

    /// <summary>
    /// Parse a display-type string (e.g. "Steam", "EA App") into a GameSourceKind enum value.
    /// Exact-match on the combo-box labels; unknown strings (e.g. enum ToString values like
    /// "UbisoftConnect") and null parse to <see cref="GameSourceKind.Standalone"/>.
    /// Used when the user selects a type from a combo box.
    /// </summary>
    public static GameSourceKind ParseFromString(string? displayName) =>
        displayName is not null && DisplayToKind.TryGetValue(displayName, out GameSourceKind kind)
            ? kind
            : GameSourceKind.Standalone;

    /// <summary>Combo-box label for <paramref name="kind"/> (same source of truth as <see cref="ParseFromString"/>).</summary>
    public static string ToDisplayName(GameSourceKind kind) =>
        KindToDisplay.TryGetValue(kind, out string? label) ? label : "Standalone";
}