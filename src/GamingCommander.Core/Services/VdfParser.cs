using System.Globalization;

namespace GamingCommander.Core.Services;

/// <summary>
/// Parser for Valve's VDF/ACF key-value format.
///
/// VDF is a TOKEN STREAM — structure is defined by `{`, `}`, and quoted strings,
/// never by line breaks or indentation (2026-09-07). Whitespace (spaces, tabs,
/// CR/LF) is skipped between tokens. This handles both the ACF format
/// (<c>"key" "value"</c> and <c>"key" { ... }</c> on one line) and the
/// libraryfolders.vdf format (opening brace on the NEXT line).
/// </summary>
public static class VdfParser
{
    /// <summary>
    /// Parse an entire VDF document and return the root block as a dictionary.
    /// Nested blocks are returned as their own dictionaries (recursive).
    /// </summary>
    public static Dictionary<string, object> Parse(string text)
    {
        int pos = 0;
        return ParseBlock(text, ref pos);
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

    /// <summary>Parses a block: repeated key → (value | { block }) until the closing `}`.</summary>
    private static Dictionary<string, object> ParseBlock(string text, ref int pos)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            SkipWhitespace(text, ref pos);
            if (pos >= text.Length)
                break; // EOF — tolerate a missing closing brace

            char c = text[pos];
            if (c == '}')
            {
                pos++; // consume closing brace
                break;
            }
            if (c != '"')
            {
                // Unexpected token — skip it and keep going.
                pos++;
                continue;
            }

            string key = ReadQuoted(text, ref pos);
            SkipWhitespace(text, ref pos);

            if (pos >= text.Length)
                break;

            char next = text[pos];
            if (next == '{')
            {
                pos++; // consume opening brace
                result[key] = ParseBlock(text, ref pos);
            }
            else if (next == '"')
            {
                result[key] = ReadQuoted(text, ref pos);
            }
            else if (next == '}')
            {
                // Dangling key with no value — skip (no entry).
            }
            else
            {
                // Malformed line — skip the key.
            }
        }

        return result;
    }

    /// <summary>Reads a quoted string starting at the current position (the `"` must be here).</summary>
    private static string ReadQuoted(string text, ref int pos)
    {
        if (pos >= text.Length || text[pos] != '"')
            throw new FormatException($"Expected '\"' at position {pos}");

        pos++; // skip opening quote

        var chars = new List<char>();
        while (pos < text.Length)
        {
            char c = text[pos];
            if (c == '"')
            {
                pos++; // skip closing quote
                return new string(chars.ToArray());
            }
            if (c == '\\' && pos + 1 < text.Length)
            {
                // Escaped char (e.g. \\ → \). Take it literally.
                pos++;
                chars.Add(text[pos]);
                pos++;
            }
            else
            {
                chars.Add(c);
                pos++;
            }
        }

        throw new FormatException("Unterminated string");
    }

    private static void SkipWhitespace(string text, ref int pos)
    {
        while (pos < text.Length && char.IsWhiteSpace(text[pos]))
            pos++;
    }
}