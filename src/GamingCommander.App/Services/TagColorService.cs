using System.Text.Json;
using System.Text.Json.Serialization;
using GamingCommander.Core.Models;
using GamingCommander.UI.ViewModels;

namespace GamingCommander.App.Services;

/// <summary>
/// Provides configurable tag colors from a user-editable JSON file.
/// Tag types: User (neutral default), Store (store-specific), Engine (engine-specific).
/// </summary>
public sealed class TagColorService : ITagColorProvider
{
    private readonly TagColorConfig _config;

    /// <summary>Known store tag names (case-insensitive).</summary>
    private static readonly HashSet<string> KnownStoreTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "Steam", "GOG", "Epic", "BattleNet", "EA", "Ubisoft", "Rockstar", "Xbox", "Standalone", "Steam Emu"
    };

    /// <summary>Known engine tag names (case-insensitive).</summary>
    private static readonly HashSet<string> KnownEngineTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "Unreal Engine", "Unity", "RAGE", "Frostbite", "Source", "Godot", "CryEngine"
    };

    public TagColorService(string configPath)
    {
        _config = LoadConfig(configPath);
    }

    /// <summary>
    /// Get color pair (background, foreground) for a tag.
    /// Checks stores → engines → default.
    /// </summary>
    public (string Background, string Foreground) GetColor(string tag, TagType type)
    {
        TagColor? color = type switch
        {
            TagType.Store => _config.Stores.GetValueOrDefault(tag, _config.Default),
            TagType.Engine => _config.Engines.GetValueOrDefault(tag, _config.Default),
            TagType.User => _config.Default,
            _ => _config.Default
        };

        return (color?.Background ?? _config.Default.Background, color?.Foreground ?? _config.Default.Foreground);
    }

    /// <summary>Determine tag type from tag name.</summary>
    public TagType GetTagType(string tag)
    {
        if (KnownStoreTags.Contains(tag)) return TagType.Store;
        if (KnownEngineTags.Contains(tag)) return TagType.Engine;
        return TagType.User;
    }

    private static TagColorConfig LoadConfig(string path)
    {
        if (!File.Exists(path))
            return CreateDefaultConfig();

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<TagColorConfig>(json) ?? CreateDefaultConfig();
        }
        catch
        {
            return CreateDefaultConfig();
        }
    }

    private static TagColorConfig CreateDefaultConfig() => new()
    {
        Default = new TagColor { Background = "#2A3A4A", Foreground = "#B8C8D8" },
        Stores = new Dictionary<string, TagColor>
        {
            ["Steam"] = new() { Background = "#1B2838", Foreground = "#B8C8D8" },
            ["GOG"] = new() { Background = "#86328A", Foreground = "#FFFFFF" },
            ["Epic"] = new() { Background = "#0078F2", Foreground = "#FFFFFF" },
            ["BattleNet"] = new() { Background = "#00AEEF", Foreground = "#FFFFFF" },
            ["EA"] = new() { Background = "#F5F5F5", Foreground = "#000000" },
            ["Ubisoft"] = new() { Background = "#000000", Foreground = "#FFFFFF" },
            ["Rockstar"] = new() { Background = "#FFC107", Foreground = "#000000" },
            ["Xbox"] = new() { Background = "#107C10", Foreground = "#FFFFFF" },
            ["Standalone"] = new() { Background = "#2A3A4A", Foreground = "#B8C8D8" },
        },
        Engines = new Dictionary<string, TagColor>
        {
            ["Unreal Engine"] = new() { Background = "#1A1A2E", Foreground = "#E94560" },
            ["Unity"] = new() { Background = "#222222", Foreground = "#FFFFFF" },
            ["RAGE"] = new() { Background = "#1A1A1A", Foreground = "#FFD700" },
            ["Frostbite"] = new() { Background = "#0D1B2A", Foreground = "#1B9AAA" },
            ["Source"] = new() { Background = "#FF6600", Foreground = "#FFFFFF" },
            ["Godot"] = new() { Background = "#478CBF", Foreground = "#FFFFFF" },
            ["CryEngine"] = new() { Background = "#003366", Foreground = "#FFFFFF" },
        },
    };
}

/// <summary>Color pair for tag badge rendering.</summary>
public sealed class TagColor
{
    [JsonPropertyName("background")]
    public string Background { get; set; } = "#2A3A4A";

    [JsonPropertyName("foreground")]
    public string Foreground { get; set; } = "#B8C8D8";
}

/// <summary>Root configuration object for tag_colors.json.</summary>
public sealed class TagColorConfig
{
    [JsonPropertyName("default")]
    public TagColor Default { get; set; } = new();

    [JsonPropertyName("stores")]
    public Dictionary<string, TagColor> Stores { get; set; } = new();

    [JsonPropertyName("engines")]
    public Dictionary<string, TagColor> Engines { get; set; } = new();
}
