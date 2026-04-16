using System.Text.Json;
using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

public sealed class JsonConfigService : IConfigService
{
    private readonly string _configPath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public JsonConfigService(string configPath)
    {
        _configPath = configPath;
    }

    public AppConfig Load()
    {
        if (!File.Exists(_configPath))
        {
            return new AppConfig(LibraryRoots: [], FolderOverrides: [], IsFirstRun: true);
        }

        string json = File.ReadAllText(_configPath);
        var loaded = JsonSerializer.Deserialize<ConfigDto>(json, JsonOptions);
        if (loaded is null)
        {
            return new AppConfig(LibraryRoots: [], FolderOverrides: [], IsFirstRun: true);
        }

        var roots = new List<LibraryRoot>();
        if (loaded.LibraryRoots != null)
        {
            foreach (ConfigLibraryRootDto root in loaded.LibraryRoots)
            {
                if (!string.IsNullOrWhiteSpace(root.Path))
                {
                    roots.Add(new LibraryRoot(Path: root.Path, DefaultType: root.Type));
                }
            }
        }

        var overrides = new List<FolderOverride>();
        if (loaded.FolderOverrides != null)
        {
            foreach (ConfigFolderOverrideDto ov in loaded.FolderOverrides)
            {
                if (!string.IsNullOrWhiteSpace(ov.FolderPath))
                {
                    overrides.Add(new FolderOverride(FolderPath: ov.FolderPath, Type: ov.Type));
                }
            }
        }

        return new AppConfig(LibraryRoots: roots, FolderOverrides: overrides, IsFirstRun: loaded.IsFirstRun);
    }

    public void Save(AppConfig config)
    {
        var dto = new ConfigDto
        {
            LibraryRoots = config.LibraryRoots.Select(r => new ConfigLibraryRootDto
            {
                Path = r.Path,
                Type = r.DefaultType,
            }).ToList(),
            FolderOverrides = config.FolderOverrides.Select(o => new ConfigFolderOverrideDto
            {
                FolderPath = o.FolderPath,
                Type = o.Type,
            }).ToList(),
            IsFirstRun = config.IsFirstRun,
        };

        string json = JsonSerializer.Serialize(dto, JsonOptions);
        string? dir = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(_configPath, json);
    }

    private sealed class ConfigDto
    {
        public List<ConfigLibraryRootDto>? LibraryRoots { get; set; }
        public List<ConfigFolderOverrideDto>? FolderOverrides { get; set; }
        public bool IsFirstRun { get; set; }
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
