namespace GamingCommander.Core.Services;

/// <summary>
/// Builds the process argument string from PCGW catalog toggles + free text.
/// Does not launch. Steam URIs are detected so extras are not stuffed into <c>steam://</c>.
/// </summary>
public static class LaunchArgumentComposer
{
    /// <summary>True when <paramref name="commandLineArguments"/> is a Steam run URI.</summary>
    public static bool IsSteamUri(string? commandLineArguments) =>
        commandLineArguments is not null
        && commandLineArguments.StartsWith("steam://", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whitespace-split tokens. Empty / null → empty list.</summary>
    public static List<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return [.. text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)];
    }

    /// <summary>Flag token only (<c>-width X</c> → <c>-width</c>).</summary>
    public static string PrimaryToken(string argument)
    {
        if (string.IsNullOrWhiteSpace(argument))
            return string.Empty;

        int space = argument.Trim().IndexOf(' ');
        return space < 0 ? argument.Trim() : argument.Trim()[..space];
    }

    /// <summary>Whether <paramref name="extras"/> already contains the catalog argument's primary token.</summary>
    public static bool ContainsToken(string? extras, string argument)
    {
        string token = PrimaryToken(argument);
        if (token.Length == 0)
            return false;

        return Tokenize(extras).Exists(t => t.Equals(token, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Add or remove a catalog flag. Arguments that need a value (<c>-width X</c>) are not toggled on —
    /// type those in free text.
    /// </summary>
    public static string Toggle(string? extras, string argument, bool enable)
    {
        string token = PrimaryToken(argument);
        var tokens = Tokenize(extras);
        tokens.RemoveAll(t => t.Equals(token, StringComparison.OrdinalIgnoreCase));
        if (enable && token.Length > 0 && !argument.Trim().Contains(' '))
            tokens.Add(token);

        return string.Join(' ', tokens);
    }

    /// <summary>Join non-URI launch args with extras (deduped, extras last).</summary>
    public static string Combine(string? existingNonUriArgs, string? extras)
    {
        var tokens = Tokenize(existingNonUriArgs);
        foreach (string extra in Tokenize(extras))
        {
            if (!tokens.Exists(t => t.Equals(extra, StringComparison.OrdinalIgnoreCase)))
                tokens.Add(extra);
        }

        return string.Join(' ', tokens);
    }

    /// <summary>
    /// Arguments for an exe start. Steam run URIs take no process arguments.
    /// </summary>
    public static string ForProcessStart(string? commandLineArguments, string? extraLaunchArguments)
    {
        if (IsSteamUri(commandLineArguments))
            return string.Empty;

        return Combine(commandLineArguments, extraLaunchArguments);
    }
}
