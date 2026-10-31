using GamingCommander.Core.Models;

namespace GamingCommander.Core.Services;

/// <summary>
/// Runs the registered launcher locators and returns one result per supported launcher, in
/// <see cref="LauncherKinds.All"/> order. Missing locators are reported as unsupported rather than
/// omitted, so a first-run preview always has a row per store.
/// </summary>
public sealed class LauncherDiscoveryService
{
    private readonly Dictionary<GameSourceKind, ILauncherLocator> _locators = [];

    /// <summary>Creates the service. Duplicate kinds keep the first registered locator.</summary>
    public LauncherDiscoveryService(IEnumerable<ILauncherLocator> locators)
    {
        ArgumentNullException.ThrowIfNull(locators);

        foreach (ILauncherLocator locator in locators)
        {
            if (locator is null)
                continue;
            _locators.TryAdd(locator.Kind, locator);
        }
    }

    /// <summary>One result per supported launcher, in policy/display order.</summary>
    public IReadOnlyList<LauncherDiscoveryResult> DiscoverAll() =>
        LauncherKinds.All.Select(Discover).ToList();

    /// <summary>
    /// One result per launcher the policy allows, in policy/display order. FileOnly and Unset
    /// yield an empty list without probing anything.
    /// </summary>
    public IReadOnlyList<LauncherDiscoveryResult> DiscoverForPolicy(LauncherDiscoveryPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return LauncherKinds.All
            .Where(policy.AllowsDiscovery)
            .Select(Discover)
            .ToList();
    }

    /// <summary>True when this build registered a locator for <paramref name="kind"/>.</summary>
    public bool Supports(GameSourceKind kind) => _locators.ContainsKey(kind);

    /// <summary>Probes one launcher; a missing locator or a failing locator yields an unsupported result.</summary>
    public LauncherDiscoveryResult Discover(GameSourceKind kind)
    {
        if (!_locators.TryGetValue(kind, out ILauncherLocator? locator))
            return LauncherDiscoveryResult.Unsupported(kind, "No locator in this build.");

        try
        {
            LauncherDiscoveryResult result = locator.Probe();
            if (result is null)
                return LauncherDiscoveryResult.Unsupported(kind, "Locator returned no result.");

            // The requested kind is authoritative for the row; a locator cannot invent another store.
            return result with { Kind = kind };
        }
        catch (Exception ex)
        {
            // The type is enough for support; the message may contain a private path.
            return LauncherDiscoveryResult.Unsupported(kind, $"Locator failed: {ex.GetType().Name}.");
        }
    }
}
