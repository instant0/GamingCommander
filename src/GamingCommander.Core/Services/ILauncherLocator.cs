using GamingCommander.Core.Models;

namespace GamingCommander.Core.Services;

/// <summary>
/// Read-only probe for one launcher: where it is installed and which inventory source can be read.
/// Implementations must not write, create directories, or use the network.
/// </summary>
public interface ILauncherLocator
{
    /// <summary>The store this locator probes.</summary>
    GameSourceKind Kind { get; }

    /// <summary>Probes the launcher. Never throws for a missing registry key or folder.</summary>
    LauncherDiscoveryResult Probe();
}
