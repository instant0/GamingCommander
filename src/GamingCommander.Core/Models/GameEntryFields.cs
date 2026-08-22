namespace GamingCommander.Core.Models;

/// <summary>
/// Field name constants for GameEntry. Used by UserOverrides dictionary
/// to track which fields the user has manually set.
/// </summary>
public static class GameEntryFields
{
    /// <summary>Human-readable game name shown in the UI.</summary>
    public const string DisplayName = "DisplayName";

    /// <summary>Path to the primary game executable.</summary>
    public const string ExecutablePath = "ExecutablePath";

    /// <summary>Path to the game's launcher executable (if any).</summary>
    public const string LauncherPath = "LauncherPath";

    /// <summary>Command-line arguments passed to the executable on launch.</summary>
    public const string CommandLineArguments = "CommandLineArguments";

    /// <summary>Constructed extras from F4 PCGW toggles / free text. Exe launch only.</summary>
    public const string ExtraLaunchArguments = "ExtraLaunchArguments";

    /// <summary>Path to the launcher manifest file (e.g., Steam ACF).</summary>
    public const string ManifestPath = "ManifestPath";

    /// <summary>Detected or overridden store/platform type.</summary>
    public const string GameSource = "GameSource";

    /// <summary>User-defined tags (e.g., "RPG", "Co-op").</summary>
    public const string Tags = "Tags";
}
