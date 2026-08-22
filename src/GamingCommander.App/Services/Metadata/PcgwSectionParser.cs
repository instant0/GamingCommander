using System.Text;
using System.Text.RegularExpressions;

namespace GamingCommander.App.Services.Metadata;

/// <summary>
/// Extracts operator sections from PCGW Parse wikitext (Plan 120).
/// No HTTP. Infobox identity stays in <see cref="PcgwInfoboxParser"/>.
/// </summary>
public static class PcgwSectionParser
{
    private static readonly Regex ArgToken = new(
        @"--?[A-Za-z][\w-]*", RegexOptions.CultureInvariant);

    private static readonly HashSet<string> VideoKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "widescreen resolution", "ultrawidescreen", "4k ultra hd", "fov",
        "windowed", "borderless windowed", "vsync", "60 fps", "120 fps",
        "hdr", "ray tracing", "antialiasing", "upscaling", "framegen",
        "color blind", "anisotropic", "multimonitor",
    };

    private static readonly HashSet<string> CloudKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "steam cloud", "gog galaxy", "epic games launcher", "ubisoft connect",
        "xbox cloud", "origin", "discord", "icloud",
    };

    /// <summary>Run every extractor on the same wikitext and merge the argument catalog.</summary>
    public static PcgwSectionFacts ParseAll(string? wikitext)
    {
        string text = wikitext ?? "";
        IReadOnlyList<PcgwCommandLineEntry> table = ParseCommandLineTable(text);
        IReadOnlyList<PcgwFix> fixes = ParseFixboxes(text);
        IReadOnlyList<PcgwCommandLineEntry> merged = MergeCommandLine(table, fixes, text);
        return new PcgwSectionFacts(
            ParseGameDataPaths(text),
            merged,
            fixes,
            ParseVideoCaps(text),
            ParseCloudSync(text),
            ParseStoreIds(text));
    }

    /// <summary>
    /// Steam / GOG / Epic ids from <c>Availability/row</c> and <c>Infobox game/row/store</c>.
    /// Steam values are digits only.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ParseStoreIds(string wikitext)
    {
        var ids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string name in new[] { "Availability/row", "Infobox game/row/store" })
        {
            foreach (string inner in ReadTemplates(wikitext, name))
            {
                string[] parts = SplitTopLevelPipes(inner);
                if (parts.Length < 2)
                    continue;

                string store = CanonicalStore(parts[0]);
                string value = parts[1].Trim();
                if (store.Length == 0 || value.Length == 0)
                    continue;

                if (store.Equals("Steam", StringComparison.OrdinalIgnoreCase)
                    && !value.All(char.IsDigit))
                {
                    continue;
                }

                if (!ids.ContainsKey(store))
                    ids[store] = value;
            }
        }

        return ids;
    }

    private static string[] SplitTopLevelPipes(string inner)
    {
        var parts = new List<string>();
        var current = new System.Text.StringBuilder();
        int depth = 0;
        for (int i = 0; i < inner.Length; i++)
        {
            if (i < inner.Length - 1 && inner[i] == '{' && inner[i + 1] == '{')
            {
                depth++;
                current.Append("{{");
                i++;
                continue;
            }

            if (i < inner.Length - 1 && inner[i] == '}' && inner[i + 1] == '}')
            {
                depth--;
                current.Append("}}");
                i++;
                continue;
            }

            if (depth == 0 && inner[i] == '|')
            {
                parts.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(inner[i]);
        }

        if (current.Length > 0)
            parts.Add(current.ToString());

        return parts.ToArray();
    }

    private static string CanonicalStore(string raw)
    {
        string key = raw.Trim().Trim('\'').ToLowerInvariant();
        if (key.Contains("steam", StringComparison.Ordinal))
            return "Steam";
        if (key.Contains("gog", StringComparison.Ordinal))
            return "GOG";
        if (key.Contains("epic", StringComparison.Ordinal))
            return "Epic";
        return raw.Trim();
    }

    /// <summary>All <c>Game data/config</c> and <c>Game data/saves</c> rows. Tokens kept raw.</summary>
    public static IReadOnlyList<PcgwGameDataPath> ParseGameDataPaths(string wikitext)
    {
        var list = new List<PcgwGameDataPath>();
        foreach (string name in new[] { "Game data/config", "Game data/saves" })
        {
            string kind = name.EndsWith("saves", StringComparison.OrdinalIgnoreCase) ? "saves" : "config";
            foreach (string inner in ReadTemplates(wikitext, name))
            {
                int pipe = IndexOfTopLevel(inner, '|');
                if (pipe < 0)
                    continue;

                string os = inner[..pipe].Trim();
                string template = inner[(pipe + 1)..].Trim();
                if (os.Length == 0 || template.Length == 0)
                    continue;

                list.Add(new PcgwGameDataPath(kind, os, template));
            }
        }

        return list;
    }

    /// <summary>Rows from <c>{{Standard table/row|arg|notes}}</c>.</summary>
    public static IReadOnlyList<PcgwCommandLineEntry> ParseCommandLineTable(string wikitext)
    {
        var list = new List<PcgwCommandLineEntry>();
        foreach (string inner in ReadTemplates(wikitext, "Standard table/row"))
        {
            int pipe = IndexOfTopLevel(inner, '|');
            if (pipe < 0)
                continue;

            string argument = inner[..pipe].Trim();
            string notes = CleanNotes(inner[(pipe + 1)..]);
            if (argument.Length == 0)
                continue;

            list.Add(new PcgwCommandLineEntry(argument, notes, ArgumentNeedsValue(argument), "table"));
        }

        return list;
    }

    /// <summary>Fixboxes (essential improvements and others). Args are not auto-enabled.</summary>
    public static IReadOnlyList<PcgwFix> ParseFixboxes(string wikitext)
    {
        var list = new List<PcgwFix>();
        foreach (string inner in ReadTemplates(wikitext, "Fixbox"))
        {
            Dictionary<string, string> fields = ParseNamedPipes(inner);
            if (!fields.TryGetValue("description", out string? description) || string.IsNullOrWhiteSpace(description))
                continue;

            string title = CleanNotes(description);
            if (title.Length == 0)
                continue;

            string? suggestedArgs = ExtractSuggestedArgs(description);
            string? exe = ExtractFilePath(description);
            list.Add(new PcgwFix(title, suggestedArgs, exe));
        }

        return list;
    }

    /// <summary>Selected <c>{{Video}}</c> caps. Long notes and WSGF awards are dropped.</summary>
    public static IReadOnlyDictionary<string, string> ParseVideoCaps(string wikitext)
    {
        var caps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string inner in ReadTemplates(wikitext, "Video"))
        {
            foreach (KeyValuePair<string, string> pair in ParseEqualsBlock(inner))
            {
                if (!VideoKeys.Contains(pair.Key))
                    continue;
                string value = pair.Value.Trim();
                if (value.Length == 0 || value.Length > 24
                    || value.Contains("http", StringComparison.OrdinalIgnoreCase)
                    || value.Contains('['))
                {
                    continue;
                }

                caps[CanonicalVideoKey(pair.Key)] = value;
            }
        }

        return caps;
    }

    /// <summary>Non-empty <c>{{Save game cloud syncing}}</c> launcher flags.</summary>
    public static IReadOnlyDictionary<string, string> ParseCloudSync(string wikitext)
    {
        var caps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string inner in ReadTemplates(wikitext, "Save game cloud syncing"))
        {
            foreach (KeyValuePair<string, string> pair in ParseEqualsBlock(inner))
            {
                if (!CloudKeys.Contains(pair.Key))
                    continue;
                if (string.IsNullOrWhiteSpace(pair.Value))
                    continue;

                caps[pair.Key.Trim()] = pair.Value.Trim();
            }
        }

        return caps;
    }

    /// <summary>Table rows first; Fixbox flags fill gaps (e.g. <c>--launcher-skip</c>).</summary>
    public static IReadOnlyList<PcgwCommandLineEntry> MergeCommandLine(
        IReadOnlyList<PcgwCommandLineEntry> table,
        IReadOnlyList<PcgwFix> fixes,
        string wikitext)
    {
        var merged = new List<PcgwCommandLineEntry>(table);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (PcgwCommandLineEntry row in table)
            seen.Add(PrimaryToken(row.Argument));

        Dictionary<string, string> perArgNotes = ExtractPerArgNotes(wikitext);
        foreach (PcgwFix fix in fixes)
        {
            if (string.IsNullOrWhiteSpace(fix.SuggestedArgs))
                continue;

            foreach (Match m in ArgToken.Matches(fix.SuggestedArgs))
            {
                string token = m.Value;
                if (!seen.Add(token))
                    continue;

                string notes = perArgNotes.TryGetValue(token, out string? n) && n.Length > 0
                    ? n
                    : fix.Title;
                merged.Add(new PcgwCommandLineEntry(token, notes, ArgumentNeedsValue(token), "fixbox"));
            }
        }

        return merged;
    }

    private static string CanonicalVideoKey(string key) => key.Trim().ToLowerInvariant() switch
    {
        "widescreen resolution" => "widescreen",
        "ultrawidescreen" => "ultrawide",
        "4k ultra hd" => "4k",
        "borderless windowed" => "borderless",
        "60 fps" => "60fps",
        "120 fps" => "120fps",
        "ray tracing" => "raytracing",
        "color blind" => "colorblind",
        string other => other.Replace(' ', '_'),
    };

    private static bool ArgumentNeedsValue(string argument)
    {
        string[] parts = argument.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return false;

        string placeholder = parts[^1];
        return placeholder.Length <= 8
               && placeholder.All(c => char.IsLetter(c))
               && placeholder.Any(char.IsUpper);
    }

    private static string PrimaryToken(string argument)
    {
        int space = argument.IndexOf(' ');
        return space < 0 ? argument : argument[..space];
    }

    private static string? ExtractSuggestedArgs(string description)
    {
        var tokens = new List<string>();
        foreach (Match code in Regex.Matches(description, @"<code>([^<]+)</code>", RegexOptions.IgnoreCase))
        {
            foreach (Match arg in ArgToken.Matches(code.Groups[1].Value))
            {
                if (!tokens.Contains(arg.Value, StringComparer.OrdinalIgnoreCase))
                    tokens.Add(arg.Value);
            }
        }

        if (tokens.Count == 0)
        {
            foreach (Match arg in ArgToken.Matches(description))
            {
                if (!tokens.Contains(arg.Value, StringComparer.OrdinalIgnoreCase))
                    tokens.Add(arg.Value);
            }
        }

        return tokens.Count == 0 ? null : string.Join(' ', tokens);
    }

    private static string? ExtractFilePath(string description)
    {
        foreach (string inner in ReadTemplates(description, "file"))
        {
            int pipe = IndexOfTopLevel(inner, '|');
            string path = (pipe < 0 ? inner : inner[..pipe]).Trim();
            if (path.Contains('\\') || path.Contains('/') || path.Contains("{{P|game}}", StringComparison.OrdinalIgnoreCase)
                || path.Contains("{{p|game}}", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
        }

        return null;
    }

    private static Dictionary<string, string> ExtractPerArgNotes(string wikitext)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string inner in ReadTemplates(wikitext, "note"))
        {
            string body = inner;
            int eq = inner.IndexOf('=');
            if (inner.StartsWith("note", StringComparison.OrdinalIgnoreCase) && eq >= 0)
                body = inner[(eq + 1)..];

            foreach (Match m in Regex.Matches(body, @"<code>([^<]+)</code>\s*(.+)"))
            {
                string token = m.Groups[1].Value.Trim();
                string notes = CleanNotes(m.Groups[2].Value);
                if (token.Length > 0 && notes.Length > 0)
                    map[token] = notes.TrimStart('*', ' ', '-');
            }
        }

        return map;
    }

    private static string CleanNotes(string text)
    {
        text = Regex.Replace(text, @"\{\{key\|([^}]+)\}\}", "$1", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<!--.*?-->", "", RegexOptions.Singleline);
        text = Regex.Replace(text, @"<ref[^>]*/>", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<ref\b[^>]*>.*?</ref>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        text = Regex.Replace(text, @"\{\{Refurl\|.*?\}\}", "", RegexOptions.Singleline);
        text = Regex.Replace(text, @"\{\{Refcheck\|.*?\}\}", "", RegexOptions.Singleline);
        text = Regex.Replace(text, @"<code>(.*?)</code>", "$1", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        text = Regex.Replace(text, @"\[\[[^|\]]*\|([^]]*)\]\]", "$1");
        text = Regex.Replace(text, @"\[\[([^]]*)\]\]", "$1");
        text = Regex.Replace(text, @"\{\{[^}]*\}\}", "");
        text = Regex.Replace(text, @"<[^>]+>", "");
        text = Regex.Replace(text, @"'{2,}", "");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text;
    }

    private static IReadOnlyList<string> ReadTemplates(string text, string name)
    {
        var list = new List<string>();
        if (string.IsNullOrEmpty(text))
            return list;

        string open = "{{" + name;
        int i = 0;
        while (i < text.Length)
        {
            int start = text.IndexOf(open, i, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                break;

            int afterName = start + open.Length;
            if (afterName < text.Length)
            {
                char next = text[afterName];
                if (next is not '|' and not '}' and not '\r' and not '\n' and not ' ' and not '\t')
                {
                    i = start + 2;
                    continue;
                }
            }

            if (!TryReadBalanced(text, start, out int end, out string inner))
            {
                i = start + 2;
                continue;
            }

            list.Add(inner);
            i = end;
        }

        return list;
    }

    private static bool TryReadBalanced(string text, int openAt, out int endExclusive, out string inner)
    {
        int depth = 0;
        for (int i = openAt; i < text.Length - 1; i++)
        {
            if (text[i] == '{' && text[i + 1] == '{')
            {
                depth++;
                i++;
                continue;
            }

            if (text[i] == '}' && text[i + 1] == '}')
            {
                depth--;
                i++;
                if (depth == 0)
                {
                    endExclusive = i + 1;
                    int innerStart = openAt + 2;
                    int firstDelim = text.IndexOfAny(['|', '\n', '\r'], innerStart);
                    if (firstDelim < 0 || firstDelim >= endExclusive - 2)
                    {
                        inner = "";
                        return true;
                    }

                    if (text[firstDelim] == '|')
                        firstDelim++;

                    inner = text[firstDelim..(endExclusive - 2)];
                    return true;
                }
            }
        }

        endExclusive = openAt;
        inner = "";
        return false;
    }

    private static int IndexOfTopLevel(string text, char ch)
    {
        int depth = 0;
        for (int i = 0; i < text.Length - 1; i++)
        {
            if (text[i] == '{' && text[i + 1] == '{')
            {
                depth++;
                i++;
                continue;
            }

            if (text[i] == '}' && text[i + 1] == '}')
            {
                depth--;
                i++;
                continue;
            }

            if (depth == 0 && text[i] == ch)
                return i;
        }

        if (text.Length > 0 && depth == 0 && text[^1] == ch)
            return text.Length - 1;

        return -1;
    }

    private static Dictionary<string, string> ParseNamedPipes(string inner)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string padded = inner.StartsWith('|') ? inner : "|" + inner;
        var parts = new List<string>();
        var current = new StringBuilder();
        int depth = 0;
        for (int i = 0; i < padded.Length; i++)
        {
            if (i < padded.Length - 1 && padded[i] == '{' && padded[i + 1] == '{')
            {
                depth++;
                current.Append("{{");
                i++;
                continue;
            }

            if (i < padded.Length - 1 && padded[i] == '}' && padded[i + 1] == '}')
            {
                depth--;
                current.Append("}}");
                i++;
                continue;
            }

            if (depth == 0 && padded[i] == '|')
            {
                if (current.Length > 0)
                    parts.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(padded[i]);
        }

        if (current.Length > 0)
            parts.Add(current.ToString());

        foreach (string part in parts)
        {
            int eq = part.IndexOf('=');
            if (eq <= 0)
                continue;
            result[part[..eq].Trim()] = part[(eq + 1)..];
        }

        return result;
    }

    private static Dictionary<string, string> ParseEqualsBlock(string inner)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? key = null;
        var value = new StringBuilder();

        void Flush()
        {
            if (key is null)
                return;
            if (key.EndsWith(" notes", StringComparison.OrdinalIgnoreCase)
                || key.Contains("wsgf", StringComparison.OrdinalIgnoreCase)
                || key.EndsWith(" tech", StringComparison.OrdinalIgnoreCase))
            {
                key = null;
                value.Clear();
                return;
            }

            result[key] = value.ToString().Trim();
            key = null;
            value.Clear();
        }

        foreach (string raw in inner.Replace("\r\n", "\n").Split('\n'))
        {
            string trimmed = raw.Trim();
            if (trimmed.StartsWith('|'))
            {
                Flush();
                string rest = trimmed[1..];
                int eq = rest.IndexOf('=');
                if (eq < 0)
                {
                    key = rest.Trim();
                    continue;
                }

                key = rest[..eq].Trim();
                value.Append(rest[(eq + 1)..].Trim());
            }
            else if (key is not null)
            {
                if (value.Length > 0)
                    value.Append(' ');
                value.Append(trimmed);
            }
        }

        Flush();
        return result;
    }
}
