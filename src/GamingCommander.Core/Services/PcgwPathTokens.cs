using System.Text.RegularExpressions;

namespace GamingCommander.Core.Services;

/// <summary>
/// Resolves PCGW <c>{{P|…}}</c> / <c>{{p|…}}</c> tokens for Windows display only.
/// Unknown tokens (e.g. <c>osxhome</c>) are left intact.
/// </summary>
public static class PcgwPathTokens
{
    private static readonly Regex Token = new(
        @"\{\{[Pp]\|([^}]+)\}\}", RegexOptions.CultureInvariant);

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
                "programdata" => "%PROGRAMDATA%",
                "windir" => "%WINDIR%",
                "userprofile" => "%USERPROFILE%",
                "userprofile\\documents" => @"%USERPROFILE%\Documents",
                "userprofile\\savedgames" => @"%USERPROFILE%\Saved Games",
                "game" => game,
                _ => m.Value,
            };
        });
    }

    /// <summary>Expand <c>%VAR%</c> for Explorer. Returns null when the path is not openable.</summary>
    public static string? ExpandForExplorer(string? displayPath)
    {
        if (string.IsNullOrWhiteSpace(displayPath))
            return null;
        if (displayPath.Contains("{{", StringComparison.Ordinal))
            return null;
        if (displayPath.Contains("%GAME%", StringComparison.OrdinalIgnoreCase))
            return null;

        string expanded = Environment.ExpandEnvironmentVariables(displayPath.Trim());
        return string.IsNullOrWhiteSpace(expanded) || expanded.Contains('%') ? null : expanded;
    }
}
