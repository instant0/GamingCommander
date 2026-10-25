namespace GamingCommander.Core.Models;

/// <summary>
/// A named launch configuration for a game (Plan 127 B10), e.g. "Play", "Play VR", "Modded".
/// The default launch (no profile) uses <see cref="LaunchInfo"/> directly.
/// </summary>
public sealed record LaunchProfile
{
    /// <summary>Display name of the profile.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Executable to run; falls back to the entry's <see cref="LaunchInfo.ExecutablePath"/> when empty.</summary>
    public string ExecutablePath { get; init; } = string.Empty;

    /// <summary>Command-line arguments for this profile.</summary>
    public string Arguments { get; init; } = string.Empty;

    /// <summary>Extra environment variables to set for the launched process.</summary>
    public Dictionary<string, string> Environment { get; init; } = [];

    /// <summary>Working directory; falls back to the executable's folder when empty.</summary>
    public string WorkingDirectory { get; init; } = string.Empty;

    /// <summary>True to request elevation (Windows only).</summary>
    public bool RunAsAdmin { get; init; }

    /// <summary>Optional engine label (free text).</summary>
    public string Engine { get; init; } = string.Empty;
}
