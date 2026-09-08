using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

/// <summary>
/// JSON-file implementation of IConfigService. Reads/writes settings.json with DTO mapping.
/// Library (anchor) definitions live in libraries.json (ILibrariesService), not here.
/// </summary>
public sealed class JsonConfigService : IConfigService
{
    private readonly string _configPath;

    /// <summary>Creates a new config service targeting the specified JSON file path.</summary>
    public JsonConfigService(string configPath)
    {
        _configPath = configPath;
    }

    /// <summary>Loads application configuration from disk. Returns defaults if file missing.</summary>
    public AppConfig Load()
    {
        bool fileExists = File.Exists(_configPath);
        ConfigDto? loaded = JsonFileHelper.ReadFromFile<ConfigDto>(
            _configPath,
            () => new ConfigDto());
        if (loaded is null)
        {
            return new AppConfig(HiddenFolders: [], IsFirstRun: true);
        }

        IReadOnlyList<string> hiddenFolders = loaded.HiddenFolders ?? [];
        return new AppConfig(
            HiddenFolders: hiddenFolders,
            IsFirstRun: !fileExists,
            LastSeenVersion: loaded.LastSeenVersion,
            EnableOnlineMetadata: loaded.EnableOnlineMetadata);
    }

    /// <summary>Serializes and persists the application configuration to disk.</summary>
    public void Save(AppConfig config)
    {
        var dto = new ConfigDto
        {
            HiddenFolders = config.HiddenFolders.ToList(),
            IsFirstRun = config.IsFirstRun,
            LastSeenVersion = config.LastSeenVersion,
            EnableOnlineMetadata = config.EnableOnlineMetadata,
        };

        JsonFileHelper.WriteToFile(_configPath, dto);
    }

    private sealed class ConfigDto
    {
        public List<string>? HiddenFolders { get; set; }
        public bool IsFirstRun { get; set; }
        public string? LastSeenVersion { get; set; }
        public bool EnableOnlineMetadata { get; set; }
    }
}
