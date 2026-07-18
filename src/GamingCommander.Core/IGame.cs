using GamingCommander.Core.Models;

namespace GamingCommander.Core;

/// <summary>
/// Represents a discovered game with its metadata and paths.
/// Implemented by GameRecord for use in the UI layer.
/// </summary>
public interface IGame
{
    /// <summary>Deterministic unique identifier (MD5-based, 16-char hex).</summary>
    string Id { get; }

    /// <summary>Display name of the game.</summary>
    string Title { get; }

    /// <summary>The store/platform this game was detected from.</summary>
    GameSourceKind Source { get; }

    /// <summary>Absolute path to the game's installation directory.</summary>
    string InstallPath { get; }

    /// <summary>Absolute path to the game's primary executable file.</summary>
    string ExecutablePath { get; }

    /// <summary>The file or URL used to launch the game (may differ from ExecutablePath).</summary>
    string LaunchTarget { get; }

    /// <summary>Timestamp of the most recent modification to the game's installation directory.</summary>
    DateTimeOffset LastModified { get; }
}
