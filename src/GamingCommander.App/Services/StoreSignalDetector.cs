using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

/// <summary>
/// Detects game store/platform type from filesystem signals in a game folder.
/// Returns the detected GameSourceKind or Unknown if no signals found.
/// Priority order: GOG → EA → Ubisoft Emu → Ubisoft → Epic → Blizzard → Xbox → Rockstar → Steam Emu → Steam.
/// </summary>
internal static class StoreSignalDetector
{
    /// <summary>
    /// Tests each store/platform signal in priority order and returns the first match.
    /// Returns GameSourceKind.Unknown if no signals are detected.
    /// </summary>
    internal static GameSourceKind DetectType(DirectoryInfo subDir)
    {
        // 1 — GOG: goggame* files at root
        if (HasGogSignal(subDir))
            return GameSourceKind.Gog;

        // 2 — EA: __Installer/ directory at root
        if (HasEaSignal(subDir))
            return GameSourceKind.EaApp;

        // 3 — Ubisoft Emulator: uplay_loader* + INI with username/accountid
        if (HasUbisoftEmulatorSignal(subDir))
            return GameSourceKind.UbisoftConnect;

        // 4 — Ubisoft: uplay_install.manifest / uplay_r*_loader*.dll
        if (HasUbisoftSignal(subDir))
            return GameSourceKind.UbisoftConnect;

        // 5 — Epic: .egstore/ or .egsstore/ directory at root
        if (HasEpicSignal(subDir))
            return GameSourceKind.Epic;

        // 6 — Blizzard: .battle.net/ directory at root
        if (HasBlizzardSignal(subDir))
            return GameSourceKind.BattleNet;

        // 7 — Xbox: default-metadata.json at root
        if (HasXboxSignal(subDir))
            return GameSourceKind.Xbox;

        // 8 — Rockstar: title.rgl at root
        if (HasRockstarSignal(subDir))
            return GameSourceKind.Rockstar;

        // 9 — Steam Emu (strong signal): steam_api64.dll / steam_api.dll at root
        if (HasSteamEmulatorSignal(subDir))
            return GameSourceKind.SteamEmu;

        // 10 — Steam Emu (weak signal): steam_appid.txt alone.
        if (HasSteamSignal(subDir))
            return GameSourceKind.SteamEmu;

        return GameSourceKind.Unknown;
    }

    // ── Signal check helpers ────────────────────────────────────

    /// <summary>GOG signal: goggame* files at folder root, or "Launch *.lnk" shortcut.</summary>
    internal static bool HasGogSignal(DirectoryInfo dir)
    {
        // Primary: goggame* files (goggame.dll, goggame-*.info, gog_*)
        if (FileSystemHelper.GetFilesSafe(dir, "goggame*").Length > 0)
            return true;

        // Secondary: "Launch <gamename>.lnk" shortcut (strong GOG signal)
        // GOG installers place a single .lnk file with "Launch" prefix in each game root
        try
        {
            foreach (string lnk in Directory.EnumerateFiles(dir.FullName, "*.lnk", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(lnk);
                if (name.StartsWith("Launch ", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch { }

        return false;
    }

    /// <summary>EA signal: __Installer/ directory, Touchup.exe, or ActivationUI.exe at folder root.</summary>
    internal static bool HasEaSignal(DirectoryInfo dir)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, "__Installer")))
            return true;

        // Some EA games ship with Touchup.exe or ActivationUI.exe at root instead of __Installer/
        string lower = dir.FullName;
        return File.Exists(Path.Combine(lower, "Touchup.exe"))
            || File.Exists(Path.Combine(lower, "ActivationUI.exe"));
    }

    /// <summary>Ubisoft Emulator signal: uplay_loader* executable + INI with Username= and AccountId=.</summary>
    internal static bool HasUbisoftEmulatorSignal(DirectoryInfo dir)
    {
        bool hasLoader = false;
        try
        {
            foreach (string file in Directory.EnumerateFiles(dir.FullName, "*", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(file).ToLowerInvariant();
                if (name.StartsWith("uplay_loader") || name.StartsWith("uplay_r"))
                    hasLoader = true;
                if (hasLoader && name.EndsWith(".ini"))
                {
                    try
                    {
                        string text = File.ReadAllText(file);
                        if (text.Contains("Username=", StringComparison.OrdinalIgnoreCase) &&
                            text.Contains("AccountId=", StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                    catch { }
                }
            }
        }
        catch { }
        return false;
    }

    /// <summary>Ubisoft signal: uplay_install.manifest, uplay_r* loader DLLs, uplay_download/ directory, or *_UPP*.exe subscription variants.</summary>
    internal static bool HasUbisoftSignal(DirectoryInfo dir)
    {
        try
        {
            // Check for uplay_download/ directory (Plan 112 Step 3A)
            if (Directory.Exists(Path.Combine(dir.FullName, "uplay_download")))
                return true;

            // Single enumeration for all file-based signals
            foreach (string file in Directory.EnumerateFiles(dir.FullName, "*", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(file).ToLowerInvariant();

                // Original signals: manifest and loader DLLs
                if (name == "uplay_install.manifest" || name == "uplay_install.state")
                    return true;
                if (name is "uplay_r1_loader64.dll" or "uplay_r2_loader64.dll"
                    or "uplay_r1_loader32.dll" or "uplay_r2_loader32.dll")
                    return true;

                // Plan 112 Step 3C: *_UPP*.exe subscription variants.
                // Note: _upp is also a tier 12 noise pattern in blacklist.json (filtered during exe scoring).
                // This is intentional — signal detection identifies the *store*, while noise scoring selects the best *exe*.
                if (name.Contains("_upp") && name.EndsWith(".exe"))
                    return true;
            }
        }
        catch { }
        return false;
    }

    /// <summary>Epic signal: .egstore/ or .egsstore/ directory at folder root.</summary>
    internal static bool HasEpicSignal(DirectoryInfo dir)
    {
        return Directory.Exists(Path.Combine(dir.FullName, ".egstore"))
            || Directory.Exists(Path.Combine(dir.FullName, ".egsstore"));
    }

    /// <summary>
    /// Blizzard signal: checks for BattleNet-specific filesystem markers.
    /// Primary: .battle.net/ directory (BattleNet Agent runtime data)
    /// Secondary: .build.info file (created during game installation — unique to BattleNet games)
    /// Tertiary: .product.db file (created during game installation)
    /// </summary>
    internal static bool HasBlizzardSignal(DirectoryInfo dir)
    {
        // Primary: .battle.net/ directory (BattleNet Agent runtime data)
        if (Directory.Exists(Path.Combine(dir.FullName, ".battle.net")))
            return true;

        // Secondary: .build.info file (created during game installation)
        // This is the most reliable signal — unique to BattleNet games
        if (File.Exists(Path.Combine(dir.FullName, ".build.info")))
            return true;

        // Tertiary: .product.db file (created during game installation)
        if (File.Exists(Path.Combine(dir.FullName, ".product.db")))
            return true;

        return false;
    }

    /// <summary>
    /// Extracts the Blizzard product codename from a .build.info file.
    /// The CDN Path field (index 6) contains values like "tpr/diablo3", "prometheus", "agent".
    /// Returns the codename (e.g., "diablo3") or null if not found.
    /// </summary>
    internal static string? ExtractBlizzardProduct(DirectoryInfo dir)
    {
        string buildInfoPath = Path.Combine(dir.FullName, ".build.info");
        if (!File.Exists(buildInfoPath))
            return null;

        try
        {
            string[] lines = File.ReadAllLines(buildInfoPath);
            // Line 0 is headers, line 1+ is data
            if (lines.Length < 2)
                return null;

            string[] fields = lines[1].Split('|');
            // CDN Path field is at index 6, format: "tpr/diablo3"
            if (fields.Length <= 6 || string.IsNullOrEmpty(fields[6]))
                return null;

            string cdnPath = fields[6];
            // Extract product codename after the last "/"
            int lastSlash = cdnPath.LastIndexOf('/');
            return lastSlash >= 0 ? cdnPath[(lastSlash + 1)..] : cdnPath;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Xbox signal: default-metadata.json at folder root.</summary>
    internal static bool HasXboxSignal(DirectoryInfo dir)
    {
        return File.Exists(Path.Combine(dir.FullName, "default-metadata.json"));
    }

    /// <summary>Rockstar signal: title.rgl at folder root.</summary>
    internal static bool HasRockstarSignal(DirectoryInfo dir)
    {
        return File.Exists(Path.Combine(dir.FullName, "title.rgl"));
    }

    /// <summary>Steam signal (weak): steam_appid.txt at folder root.</summary>
    internal static bool HasSteamSignal(DirectoryInfo dir)
    {
        return File.Exists(Path.Combine(dir.FullName, "steam_appid.txt"));
    }

    /// <summary>Steam Emulator signal (strong): steam_api64.dll or steam_api.dll at folder root.</summary>
    internal static bool HasSteamEmulatorSignal(DirectoryInfo dir)
    {
        return File.Exists(Path.Combine(dir.FullName, "steam_api64.dll"))
            || File.Exists(Path.Combine(dir.FullName, "steam_api.dll"));
    }

}
