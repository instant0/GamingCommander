using System.Text.RegularExpressions;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Tests;

/// <summary>
/// Mock registry reader that parses Windows .reg files for testing.
/// Mirrors the logic from tools/parse_registry.py.
/// Supports: string values (str(2)).
/// </summary>
internal sealed partial class MockRegistryReader : IRegistryReader
{
    private readonly Dictionary<string, Dictionary<string, string>> _data;

    /// <summary>Creates a MockRegistryReader by parsing .reg content string.</summary>
    public MockRegistryReader(string regContent)
    {
        _data = ParseRegFile(regContent);
    }

    public string? ReadStringValue(string keyPath, string valueName)
    {
        if (_data.TryGetValue(keyPath, out var values) && values.TryGetValue(valueName, out var value))
            return value;
        return null;
    }

    public IReadOnlyDictionary<string, string> ReadKeyValues(string keyPath)
    {
        if (_data.TryGetValue(keyPath, out var values))
            return values;
        return new Dictionary<string, string>();
    }

    public IReadOnlyList<string> EnumerateSubKeyNames(string keyPath)
    {
        string prefix = keyPath.TrimEnd('\\') + "\\";
        var result = new List<string>();
        foreach (string fullKey in _data.Keys)
        {
            if (fullKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                string remainder = fullKey[prefix.Length..];
                // Only immediate subkeys (no more backslashes)
                if (!remainder.Contains('\\'))
                    result.Add(remainder);
            }
        }
        return result;
    }

    // ── .reg file parser ──────────────────────────────────────────

    private static Dictionary<string, Dictionary<string, string>> ParseRegFile(string text)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        string? currentKey = null;

        foreach (string rawLine in text.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r').TrimEnd();

            // Skip empty lines, comments, and version header
            if (string.IsNullOrEmpty(line) || line.StartsWith(';'))
                continue;
            if (line.StartsWith("Windows Registry Editor", StringComparison.OrdinalIgnoreCase))
                continue;

            // Key header: [HKEY_PATH]
            var keyMatch = KeyHeaderRegex().Match(line);
            if (keyMatch.Success)
            {
                currentKey = keyMatch.Groups[1].Value;
                if (!result.ContainsKey(currentKey))
                    result[currentKey] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                continue;
            }

            if (currentKey is null)
                continue;

            // String value: "Name"=str(2):"Data"
            var strMatch = StringValueRegex().Match(line);
            if (strMatch.Success)
            {
                string name = strMatch.Groups[1].Value;
                string data = strMatch.Groups[2].Value;
                data = data.Replace("\\\\", "\\").Replace("\\\"", "\"");
                result[currentKey][name] = data;
            }
        }

        return result;
    }

    [GeneratedRegex(@"^\[(.+)\]$")]
    private static partial Regex KeyHeaderRegex();

    [GeneratedRegex(@"^""([^""]+)""\s*=\s*str\(2\):\s*""((?:[^""\\]|\\.)*)""\s*$")]
    private static partial Regex StringValueRegex();
}
