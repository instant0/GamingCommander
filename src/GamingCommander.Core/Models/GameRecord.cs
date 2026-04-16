using GamingCommander.Core.Models;

namespace GamingCommander.Core.Models;

public sealed record GameRecord(
    string Id,
    string Title,
    GameSourceKind Source,
    string InstallPath,
    string LaunchTarget,
    string ExecutablePath,
    DateTimeOffset LastModified,
    bool SupportsPointerInteraction,
    bool SupportsKeyboardOnlyFlow) : IGame;
