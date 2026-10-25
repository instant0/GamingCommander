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
    private readonly string _path;

    public CollectionsDatabaseService(string path)
    {
        _path = path;
    }

    /// <inheritdoc />
    public IReadOnlyList<Collection> Collections
    {
        get
        {
            CollectionsDto? dto = JsonFileHelper.ReadFromFile<CollectionsDto>(_path, () => new CollectionsDto());
            if (dto?.Collections is null || !SchemaVersioning.IsReadable(dto.Version))
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
        CollectionsDto? dto = JsonFileHelper.ReadFromFile<CollectionsDto>(_path, () => new CollectionsDto());
        if (dto?.Collections is null || !SchemaVersioning.IsReadable(dto.Version)
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
        CollectionsDto dto = JsonFileHelper.ReadFromFile<CollectionsDto>(_path, () => new CollectionsDto())
            ?? new CollectionsDto();
        return SchemaVersioning.IsReadable(dto.Version) ? dto : new CollectionsDto();
    }

    private void Write(CollectionsDto dto)
    {
        dto.Version = SchemaVersioning.Current;
        JsonFileHelper.WriteToFile(_path, dto);
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
