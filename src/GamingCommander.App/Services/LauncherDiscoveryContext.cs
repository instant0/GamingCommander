using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>
/// The one shared launcher discovery setting for the running process. Composition builds it
/// from the saved config once; Setup updates it after persisting the choice. Scanners read a
/// policy snapshot instead of reloading settings.json per folder.
/// </summary>
public sealed class LauncherDiscoveryContext
{
    private readonly object _gate = new();
    private LauncherDiscoveryPolicy _policy;

    /// <summary>Creates a context for a setting and its selected launchers.</summary>
    public LauncherDiscoveryContext(
        LauncherDiscoverySetting setting,
        IReadOnlyList<GameSourceKind>? selectedLaunchers = null)
    {
        _policy = new LauncherDiscoveryPolicy(setting, selectedLaunchers);
    }

    /// <summary>Creates the runtime context from the loaded configuration.</summary>
    public static LauncherDiscoveryContext FromConfig(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return new LauncherDiscoveryContext(config.LauncherDiscovery, config.SelectedLaunchers);
    }

    /// <summary>A snapshot of the current policy. Safe to hold for one scan or one action.</summary>
    public LauncherDiscoveryPolicy Policy
    {
        get
        {
            lock (_gate)
            {
                return _policy;
            }
        }
    }

    /// <summary>The current Setup choice.</summary>
    public LauncherDiscoverySetting Setting => Policy.Setting;

    /// <summary>The currently selected launcher kinds.</summary>
    public IReadOnlyList<GameSourceKind> SelectedLaunchers => Policy.SelectedLaunchers;

    /// <summary>Replaces the setting and selection; the next scan uses the new policy.</summary>
    public void Update(
        LauncherDiscoverySetting setting,
        IReadOnlyList<GameSourceKind>? selectedLaunchers = null)
    {
        lock (_gate)
        {
            _policy = new LauncherDiscoveryPolicy(setting, selectedLaunchers);
        }
    }
}
