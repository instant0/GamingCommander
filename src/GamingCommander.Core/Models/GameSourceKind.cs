namespace GamingCommander.Core.Models;

/// <summary>
/// Identifies the store or platform a game was detected from.
/// </summary>
public enum GameSourceKind
{
    /// <summary>Source could not be determined.</summary>
    Unknown = 0,

    /// <summary>Standalone game with no launcher integration.</summary>
    Standalone = 1,

    /// <summary>Valve Steam platform.</summary>
    Steam = 2,

    /// <summary>GOG.com (DRM-free).</summary>
    Gog = 3,

    /// <summary>Epic Games Store.</summary>
    Epic = 4,

    /// <summary>EA App (formerly Origin).</summary>
    EaApp = 5,

    /// <summary>Ubisoft Connect (formerly Uplay).</summary>
    UbisoftConnect = 6,

    /// <summary>Blizzard Battle.net.</summary>
    BattleNet = 7,

    /// <summary>Xbox / Microsoft Store.</summary>
    Xbox = 8,

    /// <summary>Rockstar Games Launcher.</summary>
    Rockstar = 9,

    /// <summary>Steam emulator (e.g., CreamAPI, GreenLuma).</summary>
    SteamEmu = 10,
}
