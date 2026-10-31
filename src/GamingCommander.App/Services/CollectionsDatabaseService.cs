using GamingCommander.Core;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>
/// JSON-file implementation of <see cref="ICollectionsService"/>. Reads/writes
/// <c>data/collections.json</c> as <c>{ version, collections: { name: { gameIds, smartQuery } } }</c>.
/// </summary>
public sealed class CollectionsDatabaseService : ICollectionsService
{
    private readonly JsonDocumentStore<CollectionsDto> _store;

    public CollectionsDatabaseService(string path, Action<string>? warningSink = null)
    {
        _store = new JsonDocumentStore<CollectionsDto>(path, warningSink);
    }

    /// <inheritdoc />
    public IReadOnlyList<Collection> Collections
    {
        get
        {
            CollectionsDto dto = ReadDto();
            if (dto.Collections is null)
                return [];

            return dto.Collections
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Key))
                .Select(kv => new Collection
                {
                    Name = kv.Key,
                    GameIds = kv.Value.GameIds ?? [],
                    SmartQuery = kv.Value.SmartQuery ?? string.Empty,
                })
                .ToList();
        }
    }

    /// <inheritdoc />
    public void Upsert(Collection collection)
    {
        CollectionsDto dto = ReadDto();
        dto.Collections ??= new Dictionary<string, CollectionDto>();
        dto.Collections[collection.Name] = new CollectionDto
        {
            GameIds = collection.GameIds.ToList(),
            SmartQuery = collection.SmartQuery,
        };
        Write(dto);
    }

    /// <inheritdoc />
    public bool Remove(string name)
    {
        CollectionsDto dto = ReadDto();
        if (dto.Collections is null || !dto.Collections.Remove(name))
            return false;
        Write(dto);
        return true;
    }

    /// <inheritdoc />
    public Collection? Get(string name)
    {
        CollectionsDto dto = ReadDto();
        if (dto.Collections is null
            || !dto.Collections.TryGetValue(name, out CollectionDto? c) || c is null)
            return null;

        return new Collection
        {
            Name = name,
            GameIds = c.GameIds ?? [],
            SmartQuery = c.SmartQuery ?? string.Empty,
        };
    }

    private CollectionsDto ReadDto()
    {
        return _store.Read(() => new CollectionsDto(), dto => SchemaVersioning.IsReadable(dto.Version)).Value
            ?? new CollectionsDto();
    }

    private void Write(CollectionsDto dto)
    {
        dto.Version = SchemaVersioning.Current;
        _store.Write(dto);
    }

    private sealed class CollectionsDto
    {
        public int Version { get; set; } = SchemaVersioning.Current;
        public Dictionary<string, CollectionDto>? Collections { get; set; }
    }

    private sealed class CollectionDto
    {
        public List<string>? GameIds { get; set; }
        public string? SmartQuery { get; set; }
    }
}
