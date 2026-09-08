using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

/// <summary>
/// Real implementation of ILibraryManager over anchors.
///
/// A Library (anchor) is a named catalog owning one or more physical folders.
/// Scanning a physical folder is always tied to a library-anchor context, so the
/// manager stamps <see cref="GameEntry.Library"/> = anchor name onto every entry
/// found. The per-entry game-detection may later re-anchor a title to a different
/// anchor whose type matches (e.g. an Epic game found under d:\games → EPIC),
/// which moves it between anchors in the VFS.
/// </summary>
public sealed class LibraryManager : ILibraryManager
{
    private readonly ILibrariesService _librariesService;
    private readonly IGamesDatabaseService _databaseService;
    private readonly FolderScanner _scanner;
    private readonly SteamLibraryScanner? _steamScanner;
    private readonly EpicLibraryScanner _epicScanner = new();

    public LibraryManager(
        ILibrariesService librariesService,
        IGamesDatabaseService databaseService,
        FolderScanner scanner,
        SteamLibraryScanner? steamScanner = null)
    {
        _librariesService = librariesService;
        _databaseService = databaseService;
        _scanner = scanner;
        _steamScanner = steamScanner;
    }

    /// <summary>All configured libraries (anchors), read live from persistence.</summary>
    public IReadOnlyList<Library> Libraries => _librariesService.Libraries;

    /// <summary>Returns game entries linked to the specified library anchor.</summary>
    public IReadOnlyList<GameEntry> GetGamesForLibrary(string libraryName)
    {
        return _databaseService.GetGamesForLibrary(libraryName);
    }

    /// <summary>Adds (or updates) a library anchor owning the given folders.</summary>
    public void UpsertLibrary(string name, GameSourceKind type, IReadOnlyList<string> folders)
    {
        _librariesService.Upsert(new Library(name, type, folders));
    }

    /// <summary>Removes a library anchor and its game entries. Returns true if removed.</summary>
    public bool RemoveLibrary(string name)
    {
        bool removed = _librariesService.Remove(name);
        if (removed)
            _databaseService.RemoveGamesForLibrary(name);
        return removed;
    }

    /// <summary>
    /// Scans a single physical folder in the context of a library anchor and stamps
    /// <see cref="GameEntry.Library"/> onto the discovered entries. Does not persist.
    /// </summary>
    public IReadOnlyList<GameEntry> ScanFolder(string folderPath, string libraryName, GameSourceKind type)
    {
        IReadOnlyList<GameEntry> found = SelectScannerAndScan(folderPath, type);
        return found
            .Select(e => e with { Library = libraryName })
            .ToList();
    }

    /// <summary>
    /// Rescans all physical folders of a library anchor and persists the merged
    /// results under that anchor (preserving user overrides and existing entries).
    /// </summary>
    public void RescanLibrary(string libraryName, CancellationToken ct = default)
    {
        Library? lib = _librariesService.Get(libraryName);
        if (lib is null)
            return;

        var found = new List<GameEntry>();
        foreach (string folder in lib.Folders)
        {
            ct.ThrowIfCancellationRequested();
            if (!Directory.Exists(folder))
                continue;
            found.AddRange(ScanFolder(folder, libraryName, lib.Type));
        }

        _databaseService.SetGamesForLibrary(libraryName, found);
    }

    /// <summary>Rescans a physical folder with the appropriate scanner (no anchor stamping).</summary>
    public IReadOnlyList<GameEntry> SelectScannerAndScan(
        string folderPath, GameSourceKind configuredType,
        CancellationToken ct = default)
    {
        if (_steamScanner != null && (LooksLikeSteamLibrary(folderPath) || configuredType == GameSourceKind.Steam))
            return _steamScanner.Scan(folderPath);

        if (configuredType == GameSourceKind.Epic || EpicItemCatalog.LooksLikeManifestsDir(folderPath))
        {
            // Known Epic games from other libraries, keyed by their recorded physical
            // folder (never the anchor name — anchors can be display-only catalogs).
            var known = new List<GameEntry>();
            foreach (Library lib in Libraries)
            {
                if (lib.Type != GameSourceKind.Epic)
                    continue;
                known.AddRange(_databaseService.GetGamesForLibrary(lib.Name));
            }
            return _epicScanner.Scan(folderPath, known);
        }

        return _scanner.Scan(folderPath, configuredType, ct);
    }

    /// <summary>Updates a game entry in the database.</summary>
    public void UpdateGameEntry(GameEntry updatedEntry)
    {
        _databaseService.UpdateGameEntry(updatedEntry);
    }

    /// <summary>Deletes a game entry from the database.</summary>
    public void DeleteGameEntry(string gameId)
    {
        _databaseService.DeleteGameEntry(gameId);
    }

    /// <summary>Retags a game entry with a new source type (re-anchoring support).</summary>
    public void RetagGame(string gameId, GameSourceKind newType)
    {
        _databaseService.RetagGame(gameId, newType);
    }

    /// <summary>Structural check: does this folder have steamapps/common/ ?</summary>
    public static bool LooksLikeSteamLibrary(string rootPath)
    {
        return Directory.Exists(Path.Combine(rootPath, "steamapps", "common"));
    }

    /// <summary>
    /// If the user picks a path inside a Steam library tree (e.g. steamapps/common/ or
    /// a game folder within it), walk up to find the library root. If no Steam structure
    /// is found, return the original path unchanged.
    /// </summary>
    public static string NormalizeLibraryRoot(string selectedPath)
    {
        string? candidate = selectedPath;
        while (candidate != null)
        {
            if (LooksLikeSteamLibrary(candidate))
                return candidate;
            candidate = Path.GetDirectoryName(candidate);
        }
        return selectedPath;
    }

    /// <summary>Returns true if <paramref name="childPath"/> is inside <paramref name="parentPath"/>.</summary>
    public static bool IsChildOf(string childPath, string parentPath)
    {
        string child = childPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '\\');
        string parent = parentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '\\');

        if (!child.StartsWith(parent, StringComparison.OrdinalIgnoreCase))
            return false;

        // Exact match is not a child
        if (child.Length == parent.Length)
            return false;

        // Must be followed by a separator (not a partial directory name match like "games2" vs "games")
        char next = child[parent.Length];
        return next == Path.DirectorySeparatorChar
            || next == Path.AltDirectorySeparatorChar
            || next == '\\';
    }
}
