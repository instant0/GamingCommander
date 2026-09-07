using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

/// <summary>
/// Detects container/publisher folders and recursively scans for game entries.
/// A container is a folder with no signals itself, but whose children have game signals.
/// Organization folders (≥2 game children) recurse into all children.
/// </summary>
internal static class ContainerScanner
{
    /// <summary>Non-game folder names to skip during container recursion.</summary>
    private static readonly HashSet<string> s_nonGameFolderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Soundtrack", "Soundtracks", "Original Soundtrack",
        "Manuals", "Manual", "Item Data", "Misc", "Bonus Content",
        "Artwork", "Wallpapers", "Music",
        "Redist", "Redistributable", "Support", "Tools", "_CommonRedist", "CommonRedist",
        "vcredist", "dotnet", "directx", "jdk", "physx", "installer",
        "_installer", "install", "easyanticheat", "devtools", "docs",
        "licenses", "steam controller configs", "steamworks shared",
        "dlc", "program files", "windowsapps", "squirreltemp",
        "portable", "uninstall",
        // Platform-specific build directories (not games)
        "win32", "win64", "x86", "x64",
        // Backup directory copies
        "x64 - copy", "x86 - copy",
        // Store launcher directories — these are game stores, not games
        // NOTE: "blizzard" and "battle.net" REMOVED — they are publisher containers with game subdirs
        "epic games", "origin", "uplay", "gog galaxy",
        "ea app", "rockstar games",
    };

    /// <summary>
    /// Recursively scans child directories of a container (store/publisher folder) for game entries.
    /// Bounded to maxDepth 2 (container → child → grandchild).
    /// Organization detection: ≥2 children with game signals → recurse into all.
    /// </summary>
    /// <param name="entries">Results list to append discovered games to.</param>
    /// <param name="containerDir">The container directory to scan.</param>
    /// <param name="rootPath">Library root path (for ID computation).</param>
    /// <param name="defaultType">Default source type for the library root.</param>
    /// <param name="addGameEntry">Callback to create a GameEntry from a directory (delegates to FolderScanner).</param>
    /// <param name="hiddenFolderNames">Folder names to skip.</param>
    /// <param name="noiseExePatterns">Executable noise patterns for signal detection.</param>
    /// <param name="depth">Current recursion depth (0-based, max 1).</param>
    /// <param name="ct">Cancellation token.</param>
    internal static void ScanContainerChildren(
        List<GameEntry> entries,
        DirectoryInfo containerDir,
        string rootPath,
        GameSourceKind defaultType,
        Action<List<GameEntry>, DirectoryInfo, string, GameSourceKind> addGameEntry,
        IReadOnlySet<string> hiddenFolderNames,
        IReadOnlyList<string> noiseExePatterns,
        int depth = 0,
        CancellationToken ct = default)
    {
        if (depth > 1) return; // Bounded: max 2 levels

        var children = FileSystemHelper.GetDirectoriesSafe(containerDir.FullName);

        // Count children with game signals (for organization detection)
        int gameSignalCount = 0;
        foreach (DirectoryInfo child in children)
        {
            if (IsNonGameFolder(child)) continue;
            if (StoreSignalDetector.DetectType(child) != GameSourceKind.Unknown
                || FallbackSignalDetector.HasRootExecutableSignal(child, noiseExePatterns)
                || FallbackSignalDetector.HasUnrealLayoutSignal(child, noiseExePatterns))
            {
                gameSignalCount++;
            }
        }

        foreach (DirectoryInfo child in children)
        {
            ct.ThrowIfCancellationRequested();

            if (hiddenFolderNames.Contains(child.Name))
                continue;
            if (IsNonGameFolder(child))
                continue;

            GameSourceKind childType = StoreSignalDetector.DetectType(child);

            // Store signals — always promote
            if (childType != GameSourceKind.Unknown)
            {
                addGameEntry(entries, child, rootPath, childType);
                continue;
            }

            // NOTE: BattleNet path-based detection REMOVED.
            // Detection is based on signal files inside the folder (.build.info, .product.db, .battle.net/),
            // NOT on path names. A game at Q:\random\Diablo III\ with .build.info is BattleNet — period.
            // StoreSignalDetector.DetectType() already handles this correctly.

            // Organization (≥2 game children) or single game child — promote standalone
            if (gameSignalCount >= 1)
            {
                if (FallbackSignalDetector.HasRootExecutableSignal(child, noiseExePatterns)
                    || FallbackSignalDetector.HasUnrealLayoutSignal(child, noiseExePatterns))
                {
                    addGameEntry(entries, child, rootPath, GameSourceKind.Standalone);
                    continue;
                }
                // Organization: recurse into children without direct signals
                if (gameSignalCount >= 2)
                {
                    ScanContainerChildren(entries, child, rootPath, defaultType,
                        addGameEntry, hiddenFolderNames, noiseExePatterns, depth + 1, ct);
                }
                continue;
            }

            // Publisher folder pattern: root has only dirs, no game children → recurse
            FileInfo[] files = FileSystemHelper.GetFilesSafe(child);
            DirectoryInfo[] dirs = FileSystemHelper.GetDirectoriesSafe(child.FullName);
            if (files.Length == 0 && dirs.Length > 0)
            {
                ScanContainerChildren(entries, child, rootPath, defaultType,
                    addGameEntry, hiddenFolderNames, noiseExePatterns, depth + 1, ct);
            }
        }
    }

    /// <summary>
    /// Checks if a folder is clearly not a game (non-game name, data-only, etc.).
    /// Mirrors detect.py _is_non_game_folder (three layers, 2026-09-07):
    ///   1. Exact name match (s_nonGameFolderNames / NoiseSubDirNames)
    ///   2. Child-all-non-game: ALL children are non-game subdirs → skip
    ///   3. File-type analysis: no non-noise exe + no meaningful file → skip
    /// Layers 2-3 are false-positive suppressors (folder promoted as game title).
    /// </summary>
    internal static bool IsNonGameFolder(DirectoryInfo dir)
    {
        // Layer 1: exact name match
        if (s_nonGameFolderNames.Contains(dir.Name)
            || FileSystemHelper.NoiseSubDirNames.Contains(dir.Name))
        {
            return true;
        }

        // Layer 2: all children are non-game subdirectories
        var children = FileSystemHelper.GetDirectoriesSafe(dir.FullName);
        if (children.Length > 0)
        {
            bool allNonGame = true;
            foreach (DirectoryInfo child in children)
            {
                if (!s_nonGameFolderNames.Contains(child.Name)
                    && !FileSystemHelper.NoiseSubDirNames.Contains(child.Name))
                {
                    allNonGame = false;
                    break;
                }
            }
            if (allNonGame)
                return true;
        }

        // Layer 3: file-type analysis — only non-game files (music/docs/data),
        // no non-noise exe, no unknown-meaningful files → not a game.
        // NOTE: a folder with NO files at all (dirs-only, e.g. a UE game wrapper
        // whose exe lives deep in Game/Binaries/Win64) is NOT rejected here —
        // deeper resolution decides it. Layer 3 only suppresses folders that
        // contain files and none are game evidence.
        try
        {
            bool hasNonNoiseExe = false;
            bool hasMeaningfulFile = false;
            bool hasAnyFile = false;
            foreach (FileInfo file in FileSystemHelper.GetFilesSafe(dir))
            {
                hasAnyFile = true;
                string ext = file.Extension.ToLowerInvariant();
                if (ext == ".exe")
                {
                    if (!FileSystemHelper.IsNoiseExeName(file.Name, s_noiseExePatternsForNonGame))
                        hasNonNoiseExe = true;
                }
                else if (ext is ".mp3" or ".flac" or ".ogg" or ".wav" or ".pdf" or ".txt" or ".md" or ".dll" or ".dat" or ".db" or ".ini" or ".json" or ".png" or ".jpg")
                {
                    // music / docs / support — neutral, not game evidence
                }
                else
                {
                    hasMeaningfulFile = true; // unknown ext — might be game data
                }
            }
            if (hasAnyFile && !hasNonNoiseExe && !hasMeaningfulFile)
            {
                // A store marker (goggame.dll, .egstore/, title.rgl, .build.info,
                // uplay_install.manifest, etc.) makes this a game regardless of
                // file types — never reject a store-typed folder (GogGame + goggame.dll).
                if (StoreSignalDetector.DetectType(dir) == GameSourceKind.Unknown)
                    return true;
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return false;
    }

    /// <summary>
    /// Noise patterns used by the non-game file-type layer (layer 3). A root exe
    /// matching these is not game evidence; otherwise a folder with only support
    /// files is data-only, not a game.
    /// </summary>
    private static readonly string[] s_noiseExePatternsForNonGame =
    [
        "install", "setup", "unins", "redist", "vcredist", "dxsetup", "oalinst",
        "launcher", "updater", "bootstrap", "crash", "error", "service", "tool",
        "editor", "config", "helper", "update",
    ];
}
