using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

/// <summary>Config service backed by a mutable in-memory <see cref="AppConfig"/>.</summary>
internal sealed class InMemoryConfigService(AppConfig? initial = null) : IConfigService
{
    public AppConfig Current { get; set; } = initial ?? new AppConfig([], IsFirstRun: false);

    public AppConfig Load() => Current;

    public void Save(AppConfig config) => Current = config;
}
