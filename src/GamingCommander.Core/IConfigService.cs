using GamingCommander.Core.Models;

namespace GamingCommander.Core;

/// <summary>
/// Loads and saves application configuration (library roots, overrides, preferences).
/// </summary>
public interface IConfigService
{
    /// <summary>Loads the application configuration from persistent storage. Returns defaults if no config exists.</summary>
    AppConfig Load();

    /// <summary>Persists the given application configuration to storage.</summary>
    void Save(AppConfig config);
}
