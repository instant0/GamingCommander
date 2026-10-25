namespace GamingCommander.Core.Models;

/// <summary>What kind of entry this is (Plan 127 B11).</summary>
public enum GameKind
{
    /// <summary>A normal game.</summary>
    Game = 0,

    /// <summary>Downloadable content / add-on.</summary>
    Dlc = 1,

    /// <summary>A tool / editor / utility shipped with a game.</summary>
    Tool = 2,

    /// <summary>A demo or trial.</summary>
    Demo = 3,

    /// <summary>A soundtrack.</summary>
    Soundtrack = 4,

    /// <summary>A mod.</summary>
    Mod = 5,
}
