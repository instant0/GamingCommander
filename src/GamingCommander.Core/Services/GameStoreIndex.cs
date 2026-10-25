using GamingCommander.Core.Models;

namespace GamingCommander.Core.Services;

/// <summary>
/// Read-only, in-memory view of the game database keyed by store identity (Plan 129 §3.3).
/// Built from an existing <see cref="GameEntry"/> sequence; performs no file or directory I/O.
/// <c>games.json</c> remains the source of truth and the index is rebuilt on load or mutation.
/// </summary>
public sealed class GameStoreIndex
{
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

    private readonly Dictionary<string, GameEntry> _byStoreKey;
    private readonly Dictionary<string, GameEntry> _byPath;
    private readonly Dictionary<GameSourceKind, IReadOnlyList<GameEntry>> _byStore;
    private readonly Dictionary<string, IReadOnlyList<GameEntry>> _duplicates;

    private GameStoreIndex(
        IReadOnlyList<GameEntry> entries,
        Dictionary<string, GameEntry> byStoreKey,
        Dictionary<string, GameEntry> byPath,
        Dictionary<GameSourceKind, IReadOnlyList<GameEntry>> byStore,
        Dictionary<string, IReadOnlyList<GameEntry>> duplicates)
    {
        Entries = entries;
        _byStoreKey = byStoreKey;
        _byPath = byPath;
        _byStore = byStore;
        _duplicates = duplicates;
    }

    /// <summary>All indexed entries in their original order.</summary>
    public IReadOnlyList<GameEntry> Entries { get; }

    /// <summary>Primary lookup: stable <see cref="StoreInfo.StoreKey"/> → entry. Entries without a key are omitted.</summary>
    public IReadOnlyDictionary<string, GameEntry> ByStoreKey => _byStoreKey;

    /// <summary>Normalized folder path → entry. Paths differing only by separator style, trailing slash, or case match.</summary>
    public IReadOnlyDictionary<string, GameEntry> ByPath => _byPath;

    /// <summary>Store kind → all entries detected from that store.</summary>
    public IReadOnlyDictionary<GameSourceKind, IReadOnlyList<GameEntry>> ByStore => _byStore;

    /// <summary>
    /// Store keys shared by more than one entry. A non-empty map means the database has a
    /// duplicate-store-identity error; the first entry remains the <see cref="ByStoreKey"/> lookup.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<GameEntry>> Duplicates => _duplicates;

    /// <summary>Builds an index from an existing set of entries without touching the filesystem.</summary>
    public static GameStoreIndex Build(IEnumerable<GameEntry> games)
    {
        ArgumentNullException.ThrowIfNull(games);

        var entries = games.ToList();
        var keyGroups = new Dictionary<string, List<GameEntry>>(KeyComparer);
        var byPath = new Dictionary<string, GameEntry>(KeyComparer);
        var storeGroups = new Dictionary<GameSourceKind, List<GameEntry>>();

        foreach (GameEntry game in entries)
        {
            string storeKey = (game.Store.StoreKey ?? string.Empty).Trim();
            if (storeKey.Length > 0)
            {
                if (!keyGroups.TryGetValue(storeKey, out List<GameEntry>? group))
                {
                    group = [];
                    keyGroups[storeKey] = group;
                }
                group.Add(game);
            }

            string normalizedPath = NormalizePath(game.FolderPath);
            if (normalizedPath.Length > 0)
                byPath.TryAdd(normalizedPath, game);

            if (!storeGroups.TryGetValue(game.GameSource, out List<GameEntry>? storeEntries))
            {
                storeEntries = [];
                storeGroups[game.GameSource] = storeEntries;
            }
            storeEntries.Add(game);
        }

        var byStoreKey = new Dictionary<string, GameEntry>(KeyComparer);
        var duplicates = new Dictionary<string, IReadOnlyList<GameEntry>>(KeyComparer);
        foreach (KeyValuePair<string, List<GameEntry>> pair in keyGroups)
        {
            byStoreKey[pair.Key] = pair.Value[0];
            if (pair.Value.Count > 1)
                duplicates[pair.Key] = pair.Value.AsReadOnly();
        }

        var byStore = storeGroups.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<GameEntry>)pair.Value.AsReadOnly());

        return new GameStoreIndex(entries.AsReadOnly(), byStoreKey, byPath, byStore, duplicates);
    }

    /// <summary>Returns true when an entry with the given store key exists.</summary>
    public bool ContainsStoreKey(string? storeKey) => TryGetByStoreKey(storeKey, out _);

    /// <summary>Looks up an entry by its stable store key (case-insensitive).</summary>
    public bool TryGetByStoreKey(string? storeKey, out GameEntry? entry)
    {
        if (string.IsNullOrWhiteSpace(storeKey))
        {
            entry = null;
            return false;
        }

        return _byStoreKey.TryGetValue(storeKey.Trim(), out entry);
    }

    /// <summary>Returns the entry for <paramref name="storeKey"/>, or null.</summary>
    public GameEntry? FindByStoreKey(string? storeKey) =>
        TryGetByStoreKey(storeKey, out GameEntry? entry) ? entry : null;

    /// <summary>Looks up an entry by normalized folder path (case-insensitive).</summary>
    public bool TryGetByPath(string? path, out GameEntry? entry)
    {
        string normalized = NormalizePath(path);
        if (normalized.Length == 0)
        {
            entry = null;
            return false;
        }

        return _byPath.TryGetValue(normalized, out entry);
    }

    /// <summary>Returns the entry installed at <paramref name="path"/>, or null.</summary>
    public GameEntry? FindByPath(string? path) =>
        TryGetByPath(path, out GameEntry? entry) ? entry : null;

    /// <summary>Returns all entries detected from the given store kind (empty when none).</summary>
    public IReadOnlyList<GameEntry> GetForStore(GameSourceKind source) =>
        _byStore.TryGetValue(source, out IReadOnlyList<GameEntry>? entries) ? entries : [];

    /// <summary>
    /// Returns the manifest store keys that have no matching entry in the index, preserving
    /// manifest order. Duplicate input keys are reported once.
    /// </summary>
    public IReadOnlyList<string> GetMissingStoreKeys(IEnumerable<string> manifestStoreKeys)
    {
        ArgumentNullException.ThrowIfNull(manifestStoreKeys);

        var missing = new List<string>();
        var seen = new HashSet<string>(KeyComparer);
        foreach (string rawKey in manifestStoreKeys)
        {
            string storeKey = rawKey?.Trim() ?? string.Empty;
            if (storeKey.Length == 0 || !seen.Add(storeKey))
                continue;
            if (!_byStoreKey.ContainsKey(storeKey))
                missing.Add(storeKey);
        }

        return missing;
    }

    /// <summary>Normalizes a folder path for comparison: forward slashes, no trailing separator, trimmed.</summary>
    public static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return path.Trim().Replace('\\', '/').TrimEnd('/');
    }
}
