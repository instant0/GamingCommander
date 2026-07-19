namespace GamingCommander.Core.Models;

/// <summary>
/// Shared logic for inferring GameSourceKind from folder paths and parsing display names.
/// Used by WizardViewModel, LibrarySetupViewModel, and GameSetupWindow.
/// </summary>
public static class GameSourceParser
{
    /// <summary>
    /// Human-readable display names for all supported game source types.
    /// Used by UI dropdowns and combo boxes in GameSetup, LibrarySetup, and Wizard windows.
    /// </summary>
    public static readonly string[] SourceDisplayNames =
    [
        "Standalone", "Steam", "GOG", "Epic", "EA App",
        "Ubisoft Connect", "Battle.net", "Xbox", "Rockstar", "Steam Emulator"
    ];

    /// <summary>
    /// Infer GameSourceKind from a file path by matching known store name tokens.
    /// Used when adding a library root to suggest the most likely store type.
    /// </summary>
    public static GameSourceKind InferFromPath(string path)
    {
        string lower = path.ToLowerInvariant();
        if (lower.Contains("steam")) return GameSourceKind.Steam;
        if (lower.Contains("epic")) return GameSourceKind.Epic;
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
    /// Used when the user selects a type from a combo box.
    /// </summary>
    public static GameSourceKind ParseFromString(string displayName) => displayName switch
    {
        "Steam" => GameSourceKind.Steam,
        "GOG" => GameSourceKind.Gog,
        "Epic" => GameSourceKind.Epic,
        "EA App" => GameSourceKind.EaApp,
        "Ubisoft Connect" => GameSourceKind.UbisoftConnect,
        "Battle.net" => GameSourceKind.BattleNet,
        "Xbox" => GameSourceKind.Xbox,
        "Rockstar" => GameSourceKind.Rockstar,
        "Steam Emulator" => GameSourceKind.SteamEmu,
        _ => GameSourceKind.Standalone,
    };
}
