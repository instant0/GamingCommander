using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

/// <summary>
/// Tests for VdfParser — a minimal parser for Valve's VDF/ACF key-value format.
/// The parser expects the opening brace `{` to be on the SAME line as the key,
/// i.e., <c>"key" { ... }</c> not <c>"key"\n{ ... }</c>.
/// </summary>
public sealed class VdfParserTests
{
    // ════════════════════════════════════════════════════════════════
    //  Basic Parsing
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_KeyValues_ParsesAll()
    {
        var single = VdfParser.Parse("\"key\" \"value\"");
        Assert.Single(single);
        Assert.Equal("value", single["key"]);

        string vdf = "\"key1\" \"value1\"\n\"key2\" \"value2\"\n\"key3\" \"value3\"";
        var result = VdfParser.Parse(vdf);

        Assert.Equal(3, result.Count);
        Assert.Equal("value1", result["key1"]);
        Assert.Equal("value2", result["key2"]);
        Assert.Equal("value3", result["key3"]);

        // Tabs and runs of spaces are equivalent separators (token stream model).
        var mixed = VdfParser.Parse("\"key1\"\t\"value1\"\n\"key2\"  \"value2\"");
        Assert.Equal(2, mixed.Count);
        Assert.Equal("value1", mixed["key1"]);
        Assert.Equal("value2", mixed["key2"]);
    }

    [Fact]
    public void Parse_NestedBlocks_ParsesHierarchy()
    {
        // { must be on the same line as the key
        string vdf = "\"root\" {\n    \"child\" \"value\"\n}";
        var result = VdfParser.Parse(vdf);

        Assert.Single(result);
        Assert.IsType<Dictionary<string, object>>(result["root"]);
        var nested = (Dictionary<string, object>)result["root"];
        Assert.Equal("value", nested["child"]);

        string multiple = "\"block1\" {\n    \"key1\" \"val1\"\n}\n\"block2\" {\n    \"key2\" \"val2\"\n}";
        var multi = VdfParser.Parse(multiple);
        Assert.Equal(2, multi.Count);
        var b1 = (Dictionary<string, object>)multi["block1"];
        var b2 = (Dictionary<string, object>)multi["block2"];
        Assert.Equal("val1", b1["key1"]);
        Assert.Equal("val2", b2["key2"]);
    }

    // ════════════════════════════════════════════════════════════════
    //  Edge Cases
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_EmptyInput_ReturnsEmptyDict()
    {
        Assert.Empty(VdfParser.Parse(""));
        Assert.Empty(VdfParser.Parse("   \n  \t  \n  "));
    }

    [Fact]
    public void Parse_EscapedAndQuotedValues_PreservesContent()
    {
        var spaces = VdfParser.Parse("\"key\" \"value with spaces\"");
        Assert.Equal("value with spaces", spaces["key"]);

        var escaped = VdfParser.Parse("\"key\" \"value with \\\"quotes\\\"\"");
        Assert.Equal("value with \"quotes\"", escaped["key"]);

        var midQuote = VdfParser.Parse("\"key\" \"val\\\"ue\"");
        Assert.Equal("val\"ue", midQuote["key"]);
    }

    // ════════════════════════════════════════════════════════════════
    //  Error Handling
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_MalformedInput_ReturnsPartialDict()
    {
        string vdf = "\"key1\" \"value1\"\ninvalid line without quotes\n\"key2\" \"value2\"";
        var result = VdfParser.Parse(vdf);

        // Should parse valid lines, skip malformed
        Assert.Equal(2, result.Count);
        Assert.Equal("value1", result["key1"]);
        Assert.Equal("value2", result["key2"]);

        // ExtractFields over an unparsable payload is empty, not a throw.
        var extracted = VdfParser.ExtractFields("not valid vdf at all {{{", ["key"]);
        Assert.NotNull(extracted);
        Assert.Empty(extracted!);
    }

    [Fact]
    public void Parse_UnclosedBlock_HandlesGracefully()
    {
        // Missing closing brace — parser reads to end of input
        string vdf = "\"root\" {\n    \"child\" \"value\"";
        var result = VdfParser.Parse(vdf);

        Assert.Single(result);
        var nested = (Dictionary<string, object>)result["root"];
        Assert.Equal("value", nested["child"]);
    }

    [Fact]
    public void Parse_StandaloneKeyWithoutValue_IsSkipped()
    {
        // Token-stream model (2026-09-07): whitespace/newlines are irrelevant.
        // "orphan" is followed by the next quoted token "key1", so it consumes it
        // as its value; the trailing "value1" is a dangling key at EOF (skipped).
        string vdf = "\"orphan\"\n\"key1\" \"value1\"";
        var result = VdfParser.Parse(vdf);

        Assert.Single(result);
        Assert.Equal("key1", result["orphan"]);
    }

    // ════════════════════════════════════════════════════════════════
    //  Steam-Specific Formats (matching parser's actual behavior)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_LibraryfoldersVdf_SameLineBrace_ParsesHierarchy()
    {
        // { on same line as key — this is the format the parser supports
        string vdf =
            "\"libraryfolders\" {\n" +
            "    \"0\" {\n" +
            "        \"path\" \"/home/steam\"\n" +
            "        \"apps\" {\n" +
            "            \"228980\" \"12345\"\n" +
            "            \"730\" \"67890\"\n" +
            "        }\n" +
            "    }\n" +
            "    \"1\" {\n" +
            "        \"path\" \"D:/Games/Steam\"\n" +
            "        \"apps\" {\n" +
            "            \"1234\" \"5678\"\n" +
            "        }\n" +
            "    }\n" +
            "}";
        var result = VdfParser.Parse(vdf);

        Assert.Single(result);
        var root = (Dictionary<string, object>)result["libraryfolders"];
        Assert.Equal(2, root.Count);

        var folder0 = (Dictionary<string, object>)root["0"];
        Assert.Equal("/home/steam", folder0["path"]);

        var folder1 = (Dictionary<string, object>)root["1"];
        Assert.Equal("D:/Games/Steam", folder1["path"]);
    }

    [Fact]
    public void Parse_AppmanifestAcf_SameLineBrace_ParsesAllFields()
    {
        string vdf =
            "\"AppState\" {\n" +
            "    \"appid\" \"730\"\n" +
            "    \"Universe\" \"1\"\n" +
            "    \"name\" \"Counter-Strike 2\"\n" +
            "    \"stateflags\" \"4\"\n" +
            "    \"installdir\" \"Counter-Strike Global Offensive\"\n" +
            "    \"LastUpdated\" \"1700000000\"\n" +
            "    \"SizeOnDisk\" \"30000000000\"\n" +
            "}";
        var result = VdfParser.Parse(vdf);

        Assert.Single(result);
        var appState = (Dictionary<string, object>)result["AppState"];
        Assert.Equal("730", appState["appid"]);
        Assert.Equal("Counter-Strike 2", appState["name"]);
        Assert.Equal("Counter-Strike Global Offensive", appState["installdir"]);
        Assert.Equal("1700000000", appState["LastUpdated"]);
    }

    [Fact]
    public void Parse_AcfWithDeeplyNestedBlocks_ParsesCorrectly()
    {
        string vdf =
            "\"AppState\" {\n" +
            "    \"appid\" \"730\"\n" +
            "    \"InstalledDepots\" {\n" +
            "        \"12345\" {\n" +
            "            \"manifest\" \"abc123\"\n" +
            "            \"size\" \"5000000000\"\n" +
            "        }\n" +
            "    }\n" +
            "}";
        var result = VdfParser.Parse(vdf);

        var appState = (Dictionary<string, object>)result["AppState"];
        Assert.Equal("730", appState["appid"]);

        var depots = (Dictionary<string, object>)appState["InstalledDepots"];
        var depot = (Dictionary<string, object>)depots["12345"];
        Assert.Equal("abc123", depot["manifest"]);
        Assert.Equal("5000000000", depot["size"]);
    }

    // ════════════════════════════════════════════════════════════════
    //  ExtractFields Tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractFields_RequiredKeys_ReturnsOnlyPresentKeys()
    {
        string vdf =
            "\"AppState\" {\n" +
            "    \"appid\" \"730\"\n" +
            "    \"name\" \"Counter-Strike 2\"\n" +
            "    \"installdir\" \"CS2\"\n" +
            "}";
        string[] required = ["appid", "name", "installdir"];
        var result = VdfParser.ExtractFields(vdf, required);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Count);
        Assert.Equal("730", result["appid"]);
        Assert.Equal("Counter-Strike 2", result["name"]);
        Assert.Equal("CS2", result["installdir"]);

        // A required key that is absent is skipped, not fabricated.
        string withoutDir =
            "\"AppState\" {\n" +
            "    \"appid\" \"730\"\n" +
            "    \"name\" \"CS2\"\n" +
            "}";
        var partial = VdfParser.ExtractFields(withoutDir, ["appid", "name", "missing_key"]);
        Assert.NotNull(partial);
        Assert.Equal(2, partial!.Count);
        Assert.False(partial.ContainsKey("missing_key"));
    }

    [Fact]
    public void ExtractFields_CaseInsensitiveKeyLookup()
    {
        string vdf =
            "\"AppState\" {\n" +
            "    \"AppID\" \"730\"\n" +
            "    \"Name\" \"CS2\"\n" +
            "}";
        string[] required = ["appid", "name"];
        var result = VdfParser.ExtractFields(vdf, required);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal("730", result["appid"]);
        Assert.Equal("CS2", result["name"]);
    }
}
