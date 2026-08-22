using System.Text.RegularExpressions;

namespace GamingCommander.Core.Services;

/// <summary>
/// Resolves PCGW <c>{{P|…}}</c> / <c>{{p|…}}</c> tokens for Windows display only.
/// Unknown tokens are left intact (and then fail the Explorer allowlist).
/// </summary>
public static class PcgwPathTokens
{
    private static readonly Regex Token = new(
        @"\{\{[Pp]\|([^}]+)\}\}", RegexOptions.CultureInvariant);

    private static readonly HashSet<string> AllowedEnv = new(StringComparer.OrdinalIgnoreCase)
    {
        "LOCALAPPDATA", "APPDATA", "PROGRAMDATA", "WINDIR", "USERPROFILE",
        "PROGRAMFILES", "PROGRAMFILES(X86)", "PUBLIC", "TEMP",
    };

    /// <summary>Replace known Windows tokens. Does not invent Linux paths.</summary>
    public static string ResolveWindows(string template, string? gameDirectory = null)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        string game = string.IsNullOrWhiteSpace(gameDirectory)
            ? "%GAME%"
            : gameDirectory.TrimEnd('\\', '/');

        return Token.Replace(template, m =>
        {
            string key = m.Groups[1].Value.Trim().Replace('/', '\\').ToLowerInvariant();
            return key switch
            {
                "localappdata" => "%LOCALAPPDATA%",
                "appdata" => "%APPDATA%",
                "programdata" or "allusersprofile" => "%PROGRAMDATA%",
                "windir" => "%WINDIR%",
                "temp" => "%TEMP%",
                "public" => "%PUBLIC%",
                "username" => "%USERNAME%",
                "programfiles" => "%PROGRAMFILES%",
                "userprofile" => "%USERPROFILE%",
                "userprofile\\documents" => @"%USERPROFILE%\Documents",
                "userprofile\\savedgames" => @"%USERPROFILE%\Saved Games",
                "userprofile\\appdata\\locallow" => @"%USERPROFILE%\AppData\LocalLow",
                "game" => game,
                "uid" => "<user-id>",
                "steam" => @"%PROGRAMFILES(X86)%\Steam",
                "steamlibrary" => "<SteamLibrary-folder>",
                "uplay" or "ubisoftconnect" =>
                    @"%PROGRAMFILES(X86)%\Ubisoft\Ubisoft Game Launcher",
                "hkcu" or "hkey_current_user" => "HKCU",
                "hklm" or "hkey_local_machine" => "HKLM",
                "wow64" => "Wow6432Node",
                _ => m.Value,
            };
        });
    }

    /// <summary>
    /// Expand only the env names we ourselves emit. Never <see cref="Environment.ExpandEnvironmentVariables"/>
    /// (that would honor attacker <c>%COMSPEC%</c>).
    /// </summary>
    public static string? ExpandForExplorer(string? displayPath)
    {
        if (string.IsNullOrWhiteSpace(displayPath))
            return null;
        if (displayPath.Contains("{{", StringComparison.Ordinal))
            return null;
        if (displayPath.Contains("%GAME%", StringComparison.OrdinalIgnoreCase)
            || displayPath.Contains('<')
            || displayPath.Contains('>'))
            return null;

        string text = displayPath.Trim();
        foreach (Match m in Regex.Matches(text, "%([A-Za-z][A-Za-z0-9_()]*)%"))
        {
            string name = m.Groups[1].Value;
            if (!AllowedEnv.Contains(name))
                return null;

            string? value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                return null;

            text = text.Replace(m.Value, value.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
        }

        return text.Contains('%') ? null : text;
    }

    /// <summary>
    /// Folder to open in Explorer: drop <c>&lt;user-id&gt;</c> and everything after it
    /// (e.g. stop at <c>…\savegames\</c>).
    /// </summary>
    public static string? ForExplorer(string? displayPath)
    {
        if (string.IsNullOrWhiteSpace(displayPath))
            return null;

        string text = displayPath.Trim();
        int cut = text.IndexOf('<');
        if (cut >= 0)
            text = text[..cut].TrimEnd('\\', '/');

        return ExpandForExplorer(text);
    }

    /// <summary>True when the template is a registry key (<c>{{P|hkcu}}</c> / <c>HKCU\</c>), not a folder.</summary>
    public static bool IsRegistry(string? templateOrDisplay)
    {
        if (string.IsNullOrWhiteSpace(templateOrDisplay))
            return false;

        string text = templateOrDisplay.Trim();
        return text.Contains("{{p|hkcu}}", StringComparison.OrdinalIgnoreCase)
            || text.Contains("{{p|hklm}}", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("HKCU", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("HKLM", StringComparison.OrdinalIgnoreCase);
    }
}
