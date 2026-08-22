using System.Text.Json.Serialization;
using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Services.Metadata;

/// <summary>
/// JSON sidecar at data/games_metadata.json (Plan 119 step 1).
/// Isolated from the VFS — never opens games.json.
/// </summary>
public sealed class MetadataStore : IMetadataStore
{
    private readonly string _path;
    private SidecarFile? _cache;

    /// <summary>Creates a store bound to a sidecar file path.</summary>
    public MetadataStore(string sidecarPath)
    {
        _path = sidecarPath;
    }

    /// <inheritdoc />
    public GameMetadataRecord? Get(string gameEntryId)
    {
        if (string.IsNullOrWhiteSpace(gameEntryId))
            return null;

        SidecarFile file = Load();
        if (!file.Entries.TryGetValue(gameEntryId, out SidecarEntry? entry))
            return null;

        GameMetadataRecord? merged = entry.Merged;
        if (merged is null)
            return null;

        return string.IsNullOrEmpty(merged.GameEntryId)
            ? merged with { GameEntryId = gameEntryId }
            : merged;
    }

    /// <inheritdoc />
    public void Upsert(string gameEntryId, GameMetadataRecord merged)
    {
        if (string.IsNullOrWhiteSpace(gameEntryId))
            throw new ArgumentException("Game id is required.", nameof(gameEntryId));

        SidecarFile file = Load();
        file.Entries[gameEntryId] = new SidecarEntry
        {
            Merged = merged with { GameEntryId = gameEntryId },
            Sources = file.Entries.TryGetValue(gameEntryId, out SidecarEntry? existing)
                ? existing.Sources
                : [],
        };
        Save(file);
    }

    private SidecarFile Load()
    {
        if (_cache is not null)
            return _cache;

        _cache = JsonFileHelper.ReadFromFile(
            _path,
            () => new SidecarFile(),
            JsonFileHelper.DefaultOptions) ?? new SidecarFile();
        _cache.Entries ??= [];
        return _cache;
    }

    private void Save(SidecarFile file)
    {
        file.Version = file.Version == 0 ? 1 : file.Version;
        JsonFileHelper.WriteToFile(_path, file);
        _cache = file;
    }

    private sealed class SidecarFile
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("entries")]
        public Dictionary<string, SidecarEntry> Entries { get; set; } = [];
    }

    private sealed class SidecarEntry
    {
        [JsonPropertyName("merged")]
        public GameMetadataRecord? Merged { get; set; }

        [JsonPropertyName("sources")]
        public Dictionary<string, object>? Sources { get; set; }
    }
}
