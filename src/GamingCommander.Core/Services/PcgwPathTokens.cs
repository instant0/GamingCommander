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
        if (displayPath.Contains("%GAME%", StringComparison.OrdinalIgnoreCase))
            return null;

        string text = displayPath.Trim();
        foreach (Match m in Regex.Matches(text, "%([A-Za-z][A-Za-z0-9_]*)%"))
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
}
