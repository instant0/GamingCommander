using GamingCommander.Core.Models;

namespace GamingCommander.Core.Models;

/// <summary>
/// Full game record implementing IGame, with accessibility capability flags.
/// </summary>
public sealed record GameRecord(
    /// <summary>Deterministic unique identifier (MD5-based).</summary>
    string Id,
    /// <summary>Display name of the game.</summary>
    string Title,
    /// <summary>The store/platform this game was detected from.</summary>
    GameSourceKind Source,
    /// <summary>Absolute path to the game's installation directory.</summary>
    string InstallPath,
    /// <summary>The file or URL used to launch the game.</summary>
    string LaunchTarget,
    /// <summary>Absolute path to the game's primary executable file.</summary>
    string ExecutablePath,
    /// <summary>Timestamp of the most recent modification to the game's installation directory.</summary>
    DateTimeOffset LastModified,
    /// <summary>True if the game supports mouse/pointer input.</summary>
    bool SupportsPointerInteraction,
    /// <summary>True if the game can be navigated with keyboard only.</summary>
    bool SupportsKeyboardOnlyFlow) : IGame;
