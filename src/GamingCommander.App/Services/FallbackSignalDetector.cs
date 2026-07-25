using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

/// <summary>
/// Detects fallback game type from deeper filesystem signals when no store signal is found.
/// Checks 5 signals in priority order: Steam Emulator deep → Ubisoft legacy → UE layout → root exe → root .lnk.
/// Returns GameSourceKind.Unknown if no fallback signals match.
/// </summary>
internal static class FallbackSignalDetector
{
    /// <summary>UE platform directory names under Binaries/. Matches ExecutableDiscovery.</summary>
    private static readonly string[] s_uePlatformNames = ["Win64", "Win32", "WinGDK", "Steam"];

    /// <summary>
    /// Tests fallback signals in priority order and returns the first match.
    /// Returns GameSourceKind.Unknown if no fallback signals are detected.
    /// </summary>
    internal static GameSourceKind DetectFallbackType(
        DirectoryInfo dir,
        IReadOnlyList<string> noiseExePatterns)
    {
        // 1 — Steam Emulator deep: steam_emu.ini at root, child, or UE path
        if (HasSteamEmuDeepSignal(dir))
            return GameSourceKind.SteamEmu;

        // 2 — Ubisoft legacy: UbiStats.dll at root or immediate child
        if (HasUbisoftLegacySignal(dir))
            return GameSourceKind.UbisoftConnect;

        // 3 — Standalone (Unreal layout): Engine/ + */Binaries/{platform}/*.exe, or Binaries/{platform}/*.exe at root
        if (HasUnrealLayoutSignal(dir, noiseExePatterns))
            return GameSourceKind.Standalone;

        // 4 — Standalone: any non-noise .exe at root
        if (HasRootExecutableSignal(dir, noiseExePatterns))
            return GameSourceKind.Standalone;

        // 5 — Standalone: .lnk shortcut at root
        if (HasRootLnkSignal(dir))
            return GameSourceKind.Standalone;

        return GameSourceKind.Unknown;
    }

    // ── Deep signal check helpers ───────────────────────────────

    /// <summary>Checks for Steam emulator deep signals: root-level INI, child-level DLLs, and UE Steamworks path.</summary>
    internal static bool HasSteamEmuDeepSignal(DirectoryInfo dir)
    {
        try
        {
            // Check root
            if (File.Exists(Path.Combine(dir.FullName, "steam_emu.ini")))
                return true;

            // Check immediate children
            foreach (DirectoryInfo child in FileSystemHelper.GetDirectoriesSafe(dir.FullName))
            {
                if (File.Exists(Path.Combine(child.FullName, "steam_emu.ini")))
                    return true;
            }

            // UE Steamworks path: Engine/Binaries/ThirdParty/Steamworks/Steamv*/Win64/
            string steamworksPath = Path.Combine(dir.FullName, "Engine", "Binaries", "ThirdParty", "Steamworks");
            if (Directory.Exists(steamworksPath))
            {
                foreach (string steamworksVersionDir in Directory.GetDirectories(steamworksPath))
                {
                    string win64 = Path.Combine(steamworksVersionDir, "Win64");
                    if (Directory.Exists(win64) && File.Exists(Path.Combine(win64, "steam_emu.ini")))
                        return true;
                }
            }
        }
        catch { }
        return false;
    }

    /// <summary>Checks for legacy Ubisoft launcher signals: UbiStats.dll or Ubisoft.ini.</summary>
    internal static bool HasUbisoftLegacySignal(DirectoryInfo dir)
    {
        try
        {
            // Check root
            if (File.Exists(Path.Combine(dir.FullName, "UbiStats.dll")))
                return true;

            // Check immediate children
            foreach (DirectoryInfo child in FileSystemHelper.GetDirectoriesSafe(dir.FullName))
            {
                if (File.Exists(Path.Combine(child.FullName, "UbiStats.dll")))
                    return true;
            }
        }
        catch { }
        return false;
    }

    /// <summary>
    /// Checks for Unreal Engine directory layout:
    /// - UE4-5: Engine/ folder with child/Binaries/{platform}/*.exe
    /// - UE3: Binaries/{platform}/*.exe directly at root (no Engine/ needed)
    /// </summary>
    internal static bool HasUnrealLayoutSignal(DirectoryInfo dir, IReadOnlyList<string> noiseExePatterns)
    {
        // Fast path: UE3 — Binaries/ at root
        if (HasBinariesAtRoot(dir, noiseExePatterns))
            return true;

        // UE4-5: need Engine/ directory
        string enginePath = Path.Combine(dir.FullName, "Engine");
        if (!Directory.Exists(enginePath))
            return false;

        // Check for any child with Binaries/{platform}/*.exe
        try
        {
            foreach (DirectoryInfo child in FileSystemHelper.GetDirectoriesSafe(dir.FullName))
            {
                if (child.Name == "Engine") continue;
                foreach (string platform in s_uePlatformNames)
                {
                    string platPath = Path.Combine(child.FullName, "Binaries", platform);
                    if (!Directory.Exists(platPath)) continue;
                    foreach (string exe in Directory.EnumerateFiles(platPath, "*.exe", SearchOption.TopDirectoryOnly))
                    {
                        string name = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
                        if (!FileSystemHelper.IsNoiseExeName(name, noiseExePatterns))
                            return true;
                    }
                }
            }
        }
        catch { }
        return false;
    }

    /// <summary>
    /// UE3 fast path: Binaries/ directly at root with platform subdirs.
    /// Games like Unreal Tournament 3, Gothic 3 use this layout.
    /// </summary>
    internal static bool HasBinariesAtRoot(DirectoryInfo dir, IReadOnlyList<string> noiseExePatterns)
    {
        string binariesPath = Path.Combine(dir.FullName, "Binaries");
        if (!Directory.Exists(binariesPath))
            return false;

        try
        {
            foreach (string platform in s_uePlatformNames)
            {
                string platPath = Path.Combine(binariesPath, platform);
                if (!Directory.Exists(platPath)) continue;
                foreach (string exe in Directory.EnumerateFiles(platPath, "*.exe", SearchOption.TopDirectoryOnly))
                {
                    string name = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
                    if (!FileSystemHelper.IsNoiseExeName(name, noiseExePatterns))
                        return true;
                }
            }
        }
        catch { }
        return false;
    }

    /// <summary>Checks if the game folder contains non-noise executables at the root level.</summary>
    internal static bool HasRootExecutableSignal(DirectoryInfo dir, IReadOnlyList<string> noiseExePatterns)
    {
        try
        {
            foreach (string exe in Directory.EnumerateFiles(dir.FullName, "*.exe", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
                if (!FileSystemHelper.IsNoiseExeName(name, noiseExePatterns))
                    return true;
            }
        }
        catch { }
        return false;
    }

    /// <summary>Checks for .lnk shortcut files at the root level that point to executables.</summary>
    internal static bool HasRootLnkSignal(DirectoryInfo dir)
    {
        try
        {
            return Directory.EnumerateFiles(dir.FullName, "*.lnk", SearchOption.TopDirectoryOnly).Any();
        }
        catch { }
        return false;
    }
}
