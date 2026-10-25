namespace GamingCommander.Core.Models;

/// <summary>
/// The eight store launchers that can carry an indexing policy. Standalone, Unknown, and
/// SteamEmu are discovery sources rather than stores, so they are never policy kinds.
/// </summary>
public static class LauncherKinds
{
    private static readonly HashSet<GameSourceKind> LauncherSet =
    [
        GameSourceKind.Steam,
        GameSourceKind.Epic,
        GameSourceKind.Gog,
        GameSourceKind.EaApp,
        GameSourceKind.UbisoftConnect,
        GameSourceKind.BattleNet,
        GameSourceKind.Xbox,
        GameSourceKind.Rockstar,
    ];

    /// <summary>All supported launcher kinds in display and policy order.</summary>
    public static IReadOnlyList<GameSourceKind> All { get; } =
    [
        GameSourceKind.Steam,
        GameSourceKind.Epic,
        GameSourceKind.Gog,
        GameSourceKind.EaApp,
        GameSourceKind.UbisoftConnect,
        GameSourceKind.BattleNet,
        GameSourceKind.Xbox,
        GameSourceKind.Rockstar,
    ];

    /// <summary>True when <paramref name="kind"/> is one of the supported store launchers.</summary>
    public static bool IsLauncher(GameSourceKind kind) => LauncherSet.Contains(kind);
}
