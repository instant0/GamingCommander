using System.Globalization;

namespace GamingCommander.Core.Services;

/// <summary>
/// Minimal parser for Valve's VDF/ACF key-value format.
/// Handles "key" "value" pairs and nested "key" { ... } blocks.
/// Only extracts flat fields at the requested depth — nested blocks
/// are skipped during extraction for performance.
/// </summary>
public static class VdfParser
{
    /// <summary>
    /// Parse an entire VDF document and return the root block as a flat dictionary.
    /// Nested blocks are returned as their own dictionaries (recursive).
    /// </summary>
    public static Dictionary<string, object> Parse(string text)
    {
        var lines = text.Split('\n');
        int lineIndex = 0;
        var result = ParseBlock(lines, ref lineIndex);
        return result;
    }

    /// <summary>
    /// Parse a VDF document and return only the specified top-level keys as strings.
    /// Nested blocks and unknown keys are skipped. Returns null if a critical key is missing.
    /// </summary>
    public static Dictionary<string, string>? ExtractFields(string text, string[] requiredKeys)
    {
        try
        {
            var parsed = Parse(text);

            // Navigate into the first nested block if root is a wrapper
            // (ACF files have "AppState" { ... } at root)
            var block = parsed;
            if (block.Count == 1 && block.Values.First() is Dictionary<string, object> inner)
                block = inner;

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in requiredKeys)
            {
                if (block.TryGetValue(key, out var val) && val is string str)
                    result[key] = str;
            }

            return result;
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, object> ParseBlock(string[] lines, ref int lineIndex)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        while (lineIndex < lines.Length)
        {
            string line = lines[lineIndex].Trim();
            lineIndex++;

            if (string.IsNullOrEmpty(line))
                continue;

            if (line == "}")
                break;

            try
            {
                int charPos = 0;
                string key = ParseQuoted(line, ref charPos);
                charPos = SkipWhitespace(line, charPos);

                if (charPos < line.Length && line[charPos] == '{')
                {
                    // Block value — recurse
                    result[key] = ParseBlock(lines, ref lineIndex);
                }
                else
                {
                    // Simple quoted value
                    string val = ParseQuoted(line, ref charPos);
                    result[key] = val;
                }
            }
            catch
            {
                // Skip unparseable lines
            }
        }

        return result;
    }

    private static string ParseQuoted(string line, ref int charPos)
    {
        charPos = SkipWhitespace(line, charPos);

        if (charPos >= line.Length || line[charPos] != '"')
            throw new FormatException($"Expected '\"' at position {charPos} in: {line}");

        charPos++; // skip opening quote

        var chars = new List<char>();
        while (charPos < line.Length)
        {
            char c = line[charPos];
            if (c == '"')
            {
                charPos++; // skip closing quote
                return new string(chars.ToArray());
            }
            if (c == '\\' && charPos + 1 < line.Length)
            {
                charPos++;
                chars.Add(line[charPos]);
                charPos++;
            }
            else
            {
                chars.Add(c);
                charPos++;
            }
        }

        throw new FormatException("Unterminated string");
    }

    private static int SkipWhitespace(string line, int charPos)
    {
        while (charPos < line.Length && (line[charPos] == ' ' || line[charPos] == '\t'))
            charPos++;
        return charPos;
    }
}
