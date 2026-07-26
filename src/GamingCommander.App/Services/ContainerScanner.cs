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
        "Redist", "Support", "Tools", "_CommonRedist", "CommonRedist",
        "vcredist", "dotnet", "directx", "physx", "installer",
        "_installer", "install", "easyanticheat", "devtools", "docs",
        "licenses", "steam controller configs", "steamworks shared",
        "dlc", "program files", "windowsapps", "squirreltemp",
        "portable", "uninstall",
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

    /// <summary>Checks if a folder is clearly not a game (non-game name, data-only, etc.).</summary>
    internal static bool IsNonGameFolder(DirectoryInfo dir)
    {
        return s_nonGameFolderNames.Contains(dir.Name)
            || FileSystemHelper.NoiseSubDirNames.Contains(dir.Name);
    }
}
