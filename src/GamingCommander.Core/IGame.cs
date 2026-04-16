using GamingCommander.Core.Models;

namespace GamingCommander.Core;

public interface IGame
{
    string Id { get; }
    string Title { get; }
    GameSourceKind Source { get; }
    string InstallPath { get; }
    string ExecutablePath { get; }
    string LaunchTarget { get; }
    DateTimeOffset LastModified { get; }
}
