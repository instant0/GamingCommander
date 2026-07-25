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

    /// <summary>GOG signal: goggame* files at folder root.</summary>
    internal static bool HasGogSignal(DirectoryInfo dir)
    {
        return FileSystemHelper.GetFilesSafe(dir, "goggame*").Length > 0;
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

    /// <summary>Ubisoft signal: uplay_install.manifest or uplay_r* loader DLLs.</summary>
    internal static bool HasUbisoftSignal(DirectoryInfo dir)
    {
        try
        {
            foreach (string file in Directory.EnumerateFiles(dir.FullName, "*", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(file).ToLowerInvariant();
                if (name == "uplay_install.manifest" || name == "uplay_install.state")
                    return true;
                if (name is "uplay_r1_loader64.dll" or "uplay_r2_loader64.dll"
                    or "uplay_r1_loader32.dll" or "uplay_r2_loader32.dll")
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

    /// <summary>Blizzard signal: .battle.net/ directory at folder root.</summary>
    internal static bool HasBlizzardSignal(DirectoryInfo dir)
    {
        return Directory.Exists(Path.Combine(dir.FullName, ".battle.net"));
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

    /// <summary>
    /// BattleNet game signal: checks for BattleNet-specific game folder names or executable patterns.
    /// Used when parent folder has BattleNet signal (e.g., blizzard/diablo iii/).
    /// </summary>
    internal static bool HasBattleNetGameSignal(DirectoryInfo dir)
    {
        // Check for common BattleNet game folder names
        string[] battleNetGameNames =
        [
            "warcraft", "diablo", "overwatch", "starcraft",
            "hearthstone", "world of warcraft", "heroes of the storm",
            "call of duty", "crash bandicoot", "spyro",
        ];

        string dirName = dir.Name.ToLowerInvariant();
        if (battleNetGameNames.Any(name => dirName.Contains(name)))
            return true;

        // Check for BattleNet-specific executables
        string[] battleNetExes =
        [
            "DiabloIII.exe", "Retail.x86_64.exe",
            "Warcraft III.exe", "Frozen Throne.exe",
            "Overwatch.exe", "SC2Switcher.exe",
        ];

        foreach (string exe in battleNetExes)
        {
            if (File.Exists(Path.Combine(dir.FullName, exe)))
                return true;
        }

        return false;
    }
}
