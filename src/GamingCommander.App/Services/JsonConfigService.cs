using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

/// <summary>
/// JSON-file implementation of IConfigService. Reads/writes settings.json with DTO mapping.
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
            return new AppConfig(LibraryRoots: [], FolderOverrides: [], HiddenFolders: [], IsFirstRun: true);
        }

        var roots = new List<LibraryRoot>();
        if (loaded.LibraryRoots != null)
        {
            foreach (ConfigLibraryRootDto root in loaded.LibraryRoots)
            {
                if (!string.IsNullOrWhiteSpace(root.Path))
                {
                    roots.Add(new LibraryRoot(RootPath: root.Path, DefaultType: root.Type));
                }
            }
        }

        var overrides = new List<FolderOverride>();
        if (loaded.FolderOverrides != null)
        {
            foreach (ConfigFolderOverrideDto folderOverride in loaded.FolderOverrides)
            {
                if (!string.IsNullOrWhiteSpace(folderOverride.FolderPath))
                {
                    overrides.Add(new FolderOverride(FolderPath: folderOverride.FolderPath, OverrideType: folderOverride.Type));
                }
            }
        }

        IReadOnlyList<string> hiddenFolders = loaded.HiddenFolders ?? [];
        return new AppConfig(
            LibraryRoots: roots,
            FolderOverrides: overrides,
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
            LibraryRoots = config.LibraryRoots.Select(r => new ConfigLibraryRootDto
            {
                Path = r.RootPath,
                Type = r.DefaultType,
            }).ToList(),
            FolderOverrides = config.FolderOverrides.Select(o => new ConfigFolderOverrideDto
            {
                FolderPath = o.FolderPath,
                Type = o.OverrideType,
            }).ToList(),
            HiddenFolders = config.HiddenFolders.ToList(),
            IsFirstRun = config.IsFirstRun,
            LastSeenVersion = config.LastSeenVersion,
            EnableOnlineMetadata = config.EnableOnlineMetadata,
        };

        JsonFileHelper.WriteToFile(_configPath, dto);
    }

    private sealed class ConfigDto
    {
        public List<ConfigLibraryRootDto>? LibraryRoots { get; set; }
        public List<ConfigFolderOverrideDto>? FolderOverrides { get; set; }
        public List<string>? HiddenFolders { get; set; }
        public bool IsFirstRun { get; set; }
        public string? LastSeenVersion { get; set; }
        public bool EnableOnlineMetadata { get; set; }
    }

    private sealed class ConfigLibraryRootDto
    {
        public string Path { get; set; } = string.Empty;
        public GameSourceKind Type { get; set; } = GameSourceKind.Standalone;
    }

    private sealed class ConfigFolderOverrideDto
    {
        public string FolderPath { get; set; } = string.Empty;
        public GameSourceKind Type { get; set; } = GameSourceKind.Standalone;
    }
}
