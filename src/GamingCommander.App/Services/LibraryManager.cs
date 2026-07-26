using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

/// <summary>
/// Real implementation of ILibraryManager that reads library roots from
/// IConfigService directly (no stale in-memory copy), and routes scanning
/// to the appropriate scanner based on folder structure + configured type.
///
/// Scanner selection logic:
///   - If a folder has steamapps/common/ → SteamLibraryScanner (structural = definitive)
///   - If configured type is Steam but no steamapps/ → SteamLibraryScanner (respects override)
///   - Otherwise → FolderScanner
/// </summary>
public sealed class LibraryManager : ILibraryManager
{
    private readonly IConfigService _configService;
    private readonly IGamesDatabaseService _databaseService;
    private readonly FolderScanner _scanner;
    private readonly SteamLibraryScanner? _steamScanner;

    /// <summary>Creates a new LibraryManager with the specified services.</summary>
    public LibraryManager(
        IConfigService configService,
        IGamesDatabaseService databaseService,
        FolderScanner scanner,
        SteamLibraryScanner? steamScanner = null)
    {
        _configService = configService;
        _databaseService = databaseService;
        _scanner = scanner;
        _steamScanner = steamScanner;
    }

    /// <summary>
    /// Roots are read live from persisted config — never stale.
    /// </summary>
    public IReadOnlyList<LibraryRoot> LibraryRoots =>
        _configService.Load().LibraryRoots;

    /// <summary>Returns game entries for the specified root from the database.</summary>
    public IReadOnlyList<GameEntry> GetGamesForRoot(string rootPath)
    {
        return _databaseService.GetGamesForRoot(rootPath);
    }

    /// <summary>
    /// Adds a new library root. Scans if no games provided. Persists to both config and database.
    /// Returns true if the root was added, false if the folder was empty (0 games found).
    /// </summary>
    public bool AddRoot(string rootPath, GameSourceKind defaultType, IReadOnlyList<GameEntry> initialGames)
    {
        AppConfig config = _configService.Load();

        // If no games were provided, scan the folder to discover them
        IReadOnlyList<GameEntry> resolved = initialGames;
        if (resolved.Count == 0 && Directory.Exists(rootPath))
            resolved = SelectScannerAndScan(rootPath, defaultType);

        // Don't add root if no games found — user can rescan after adding games
        if (resolved.Count == 0)
            return false;

        // Persist to games database
        _databaseService.AddRoot(rootPath, defaultType, resolved);

        // Persist root to config (append if not already present)
        var roots = config.LibraryRoots.ToList();
        if (!roots.Any(r => r.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase)))
        {
            roots.Add(new LibraryRoot(rootPath, defaultType));
            _configService.Save(config with { LibraryRoots = roots });
        }

        return true;
    }

    /// <summary>Removes a root from both the games database and config.</summary>
    public void RemoveRoot(string rootPath)
    {
        // Remove from games database
        _databaseService.RemoveRoot(rootPath);

        // Remove from config
        AppConfig config = _configService.Load();
        var roots = config.LibraryRoots
            .Where(r => !r.RootPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
            .ToList();
        _configService.Save(config with { LibraryRoots = roots });
    }

    /// <summary>
    /// Re-scans all configured library roots and updates the games database.
    /// Scanner selection uses SelectScannerAndScan (structural check + type hint).
    /// Called at startup (if roots exist) or on explicit refresh.
    /// </summary>
    public void Refresh(CancellationToken ct = default)
    {
        AppConfig config = _configService.Load();
        foreach (LibraryRoot root in config.LibraryRoots)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                if (!Directory.Exists(root.RootPath))
                    continue;

                IReadOnlyList<GameEntry> games = SelectScannerAndScan(root.RootPath, root.DefaultType, ct);
                _databaseService.RescanRoot(root.RootPath, games);
            }
            catch (OperationCanceledException)
            {
                throw; // Propagate cancellation
            }
            catch
            {
                // Continue with next root — don't let one failing root skip the rest
            }
        }
    }

    /// <summary>Delegates rescan results to the database service.</summary>
    public void RescanRoot(string rootPath, IReadOnlyList<GameEntry> games)
    {
        _databaseService.RescanRoot(rootPath, games);
    }

    /// <summary>Delegates game entry update to the database service.</summary>
    public void UpdateGameEntry(string rootPath, GameEntry updatedEntry)
    {
        _databaseService.UpdateGameEntry(rootPath, updatedEntry);
    }

    /// <summary>Delegates game entry deletion to the database service.</summary>
    public void DeleteGameEntry(string rootPath, string gameId)
    {
        _databaseService.DeleteGameEntry(rootPath, gameId);
    }

    /// <summary>Delegates game retag to the database service.</summary>
    public void RetagGame(string rootPath, string gameId, GameSourceKind newType)
    {
        _databaseService.RetagGame(rootPath, gameId, newType);
    }

    // ════════════════════════════════════════════════════════════════
    //  Scanner Selection
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Select the best scanner for the given root path.
    ///
    /// Rule:
    ///   1. If steamapps/common/ exists → SteamLibraryScanner (structural = definitive).
    ///      No other platform uses the "steamapps" directory — this check is safe.
    ///   2. If configured type is Steam but no steamapps/ → SteamLibraryScanner still
    ///      (respects explicit user override).
    ///   3. Otherwise → FolderScanner.
    /// </summary>
    public IReadOnlyList<GameEntry> SelectScannerAndScan(
        string rootPath, GameSourceKind configuredType,
        CancellationToken ct = default)
    {
        if (_steamScanner != null && (LooksLikeSteamLibrary(rootPath) || configuredType == GameSourceKind.Steam))
            return _steamScanner.Scan(rootPath);

        return _scanner.Scan(rootPath, configuredType, ct);
    }

    /// <summary>
    /// Structural check: does this folder have steamapps/common/ ?
    /// </summary>
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
