namespace GamingCommander.Core.Models;

/// <summary>
/// Launch fields for a game entry (Plan 127 A1): how the game is started. Grouped out of the
/// flat <see cref="GameEntry"/> surface. The executable is a mutable launch detail — not identity.
/// </summary>
public sealed record LaunchInfo
{
    /// <summary>Path to the primary game executable.</summary>
    public string ExecutablePath { get; init; } = string.Empty;

    /// <summary>Path to the game's launcher executable (if any).</summary>
    public string LauncherPath { get; init; } = string.Empty;

    /// <summary>Command-line arguments passed to the executable on launch.</summary>
    public string CommandLineArguments { get; init; } = string.Empty;

    /// <summary>User-selected extras from the PCGW catalog / free text. Used on exe launch only.</summary>
    public string ExtraLaunchArguments { get; init; } = string.Empty;

    /// <summary>Path to the launcher manifest file (e.g., Steam ACF).</summary>
    public string ManifestPath { get; init; } = string.Empty;
}
