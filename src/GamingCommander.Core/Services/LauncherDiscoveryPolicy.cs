using GamingCommander.Core.Models;

namespace GamingCommander.Core.Services;

/// <summary>
/// Pure answer to "is this launcher read allowed?" for one <see cref="LauncherDiscoverySetting"/>.
/// It has no I/O and no cache: callers hold the current policy and ask it.
/// </summary>
public sealed class LauncherDiscoveryPolicy
{
    private readonly HashSet<GameSourceKind> _selected;

    /// <summary>Creates a policy for a setting and the launchers the user selected.</summary>
    public LauncherDiscoveryPolicy(
        LauncherDiscoverySetting setting,
        IReadOnlyList<GameSourceKind>? selectedLaunchers = null)
    {
        Setting = setting;
        _selected = selectedLaunchers is null
            ? []
            : new HashSet<GameSourceKind>(selectedLaunchers);
        SelectedLaunchers = LauncherKinds.All.Where(_selected.Contains).ToList();
    }

    /// <summary>The selected Setup choice.</summary>
    public LauncherDiscoverySetting Setting { get; }

    /// <summary>Selected launcher kinds, in <see cref="LauncherKinds.All"/> order.</summary>
    public IReadOnlyList<GameSourceKind> SelectedLaunchers { get; }

    /// <summary>
    /// True when this launcher may be probed on an explicit action. A launcher with no
    /// registered locator still reports <c>Unsupported</c>; this only answers the setting.
    /// </summary>
    public bool AllowsDiscovery(GameSourceKind kind) =>
        LauncherKinds.IsLauncher(kind)
        && Setting switch
        {
            LauncherDiscoverySetting.Automatic => true,
            LauncherDiscoverySetting.Selected => _selected.Contains(kind),
            _ => false,
        };

    /// <summary>True when folder scanning may classify a game from its registry install key.</summary>
    public bool AllowsRegistryFallback =>
        Setting is LauncherDiscoverySetting.Automatic or LauncherDiscoverySetting.Selected;

    /// <summary>True when the Epic ProgramData item catalog may be read.</summary>
    public bool AllowsEpicCatalog => AllowsDiscovery(GameSourceKind.Epic);

    /// <summary>True when per-store indexing/launch policies are actionable.</summary>
    public bool AllowsStorePolicies =>
        Setting is LauncherDiscoverySetting.Automatic or LauncherDiscoverySetting.Selected;
}
