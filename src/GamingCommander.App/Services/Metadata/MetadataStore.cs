using System.Text.Json.Serialization;
using GamingCommander.Core;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services.Metadata;

/// <summary>
/// JSON sidecar at data/games_metadata.json (Plan 119 step 1).
/// Isolated from the VFS — never opens games.json.
/// </summary>
public sealed class MetadataStore : IMetadataStore
{
    private readonly JsonDocumentStore<SidecarFile> _store;
    private SidecarFile? _cache;

    /// <summary>Creates a store bound to a sidecar file path.</summary>
    /// <param name="sidecarPath">Full path of games_metadata.json.</param>
    /// <param name="warningSink">Optional receiver for store read warnings.</param>
    public MetadataStore(string sidecarPath, Action<string>? warningSink = null)
    {
        _store = new JsonDocumentStore<SidecarFile>(sidecarPath, warningSink);
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

        _cache = _store.Read(
            () => new SidecarFile(),
            file => SchemaVersioning.IsReadable(file.Version)).Value ?? new SidecarFile();
        _cache.Entries ??= [];
        return _cache;
    }

    private void Save(SidecarFile file)
    {
        file.Version = SchemaVersioning.Current;
        _store.Write(file);
        _cache = file;
    }

    private sealed class SidecarFile
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = SchemaVersioning.Current;

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
