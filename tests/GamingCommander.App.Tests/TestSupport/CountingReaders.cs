using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Tests;

/// <summary>Registry reader that counts every access; wraps an inner reader when one is given.</summary>
internal sealed class CountingRegistryReader(IRegistryReader? inner = null) : IRegistryReader
{
    public int Reads { get; private set; }

    public string? ReadStringValue(string keyPath, string valueName)
    {
        Reads++;
        return inner?.ReadStringValue(keyPath, valueName);
    }

    public IReadOnlyDictionary<string, string> ReadKeyValues(string keyPath)
    {
        Reads++;
        return inner?.ReadKeyValues(keyPath) ?? new Dictionary<string, string>();
    }

    public IReadOnlyList<string> EnumerateSubKeyNames(string keyPath)
    {
        Reads++;
        return inner?.EnumerateSubKeyNames(keyPath) ?? [];
    }
}

/// <summary>Launcher locator with a probe counter for explicit-action and startup tests.</summary>
internal sealed class CountingLauncherLocator(GameSourceKind kind, LauncherDiscoveryResult result) : ILauncherLocator
{
    public int Probes { get; private set; }

    public GameSourceKind Kind => kind;

    public LauncherDiscoveryResult Probe()
    {
        Probes++;
        return result;
    }
}
