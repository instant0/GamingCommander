using GamingCommander.Core.Models;

namespace GamingCommander.Core.Services;

/// <summary>
/// Steam install with a run URI → Steam protocol. Everything else → exe + extras.
/// No switch that turns a Steam game into a raw exe.
/// </summary>
public static class GameLaunchResolver
{
    /// <summary>Resolve launch target and argument string.</summary>
    public static (string Target, string Arguments) Resolve(GameEntry game)
    {
        if (LaunchArgumentComposer.IsSteamUri(game.CommandLineArguments))
            return (game.CommandLineArguments, string.Empty);

        string arguments = LaunchArgumentComposer.ForProcessStart(
            game.CommandLineArguments,
            game.ExtraLaunchArguments);
        return (game.ExecutablePath ?? string.Empty, arguments);
    }
}
