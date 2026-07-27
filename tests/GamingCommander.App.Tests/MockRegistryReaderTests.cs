namespace GamingCommander.App.Tests;

public sealed class MockRegistryReaderTests
{
    // ── EA Registry Parsing ───────────────────────────────────────

    [Fact]
    public void ParseEaRegFile_PerGameKeysExtracted()
    {
        string regContent = """
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games\Dead Space 3]
            "GDFBinary"=str(2):"GDFBinary.dll"
            "DisplayName"=str(2):"Dead Space 3"
            "Install Dir"=str(2):"C:\\EA Games\\Dead Space 3"
            "Locale"=str(2):"en_US"

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Electronic Arts\EA Desktop]
            "InstallLocation"=str(2):"C:\\Program Files\\Electronic Arts\\EA Desktop\\"
            """;

        var reader = new MockRegistryReader(regContent);

        // Per-game key
        string? installDir = reader.ReadStringValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games\Dead Space 3",
            "Install Dir");
        Assert.Equal("C:\\EA Games\\Dead Space 3", installDir);

        string? displayName = reader.ReadStringValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games\Dead Space 3",
            "DisplayName");
        Assert.Equal("Dead Space 3", displayName);

        // Launcher key
        string? installLocation = reader.ReadStringValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Electronic Arts\EA Desktop",
            "InstallLocation");
        Assert.Equal("C:\\Program Files\\Electronic Arts\\EA Desktop\\", installLocation);
    }

    // ── Ubisoft Registry Parsing ──────────────────────────────────

    [Fact]
    public void ParseUbisoftRegFile_PerGameKeysExtracted()
    {
        string regContent = """
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs\11903]
            "InstallDir"=str(2):"E:\\Games\\Ghost Recon Breakpoint"
            "Language"=str(2):"en_US"

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs\4932]
            "InstallDir"=str(2):"E:\\Games\\The Division 2"
            """;

        var reader = new MockRegistryReader(regContent);

        string? breakPoint = reader.ReadStringValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs\11903",
            "InstallDir");
        Assert.Equal("E:\\Games\\Ghost Recon Breakpoint", breakPoint);

        string? division = reader.ReadStringValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs\4932",
            "InstallDir");
        Assert.Equal("E:\\Games\\The Division 2", division);
    }

    // ── GOG Registry Parsing ──────────────────────────────────────

    [Fact]
    public void ParseGogRegFile_PerGameKeysExtracted()
    {
        string regContent = """
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\Games\1201963702]
            "path"=str(2):"E:\\Games\\Blasphemous 2"
            "exe"=str(2):"\"E:\\Games\\Blasphemous 2\\Blasphemous2.exe\""
            "gameName"=str(2):"Blasphemous 2"
            "gameID"=str(2):"1201963702"
            """;

        var reader = new MockRegistryReader(regContent);

        string? path = reader.ReadStringValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\Games\1201963702",
            "path");
        Assert.Equal("E:\\Games\\Blasphemous 2", path);

        string? gameName = reader.ReadStringValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\Games\1201963702",
            "gameName");
        Assert.Equal("Blasphemous 2", gameName);
    }

    // ── DWORD Value Parsing ───────────────────────────────────────

    [Fact]
    public void ParseDwordValue_ConvertedToString()
    {
        string regContent = """
            Windows Registry Editor Version 5.00

            [HKEY_CURRENT_USER\Software\Valve\Steam\Apps\12345]
            "Installed"=dword:00000001
            """;

        var reader = new MockRegistryReader(regContent);

        string? value = reader.ReadStringValue(
            @"HKEY_CURRENT_USER\Software\Valve\Steam\Apps\12345",
            "Installed");
        Assert.Equal("1", value);
    }

    // ── Missing Key/Value Returns Null ────────────────────────────

    [Fact]
    public void ReadStringValue_MissingKey_ReturnsNull()
    {
        var reader = new MockRegistryReader("""
            Windows Registry Editor Version 5.00
            """);

        string? value = reader.ReadStringValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\NonExistent",
            "SomeValue");
        Assert.Null(value);
    }

    [Fact]
    public void ReadStringValue_MissingValue_ReturnsNull()
    {
        string regContent = """
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\SomeKey]
            "ExistingValue"=str(2):"hello"
            """;

        var reader = new MockRegistryReader(regContent);

        string? value = reader.ReadStringValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\SomeKey",
            "NonExistentValue");
        Assert.Null(value);
    }

    // ── EnumerateSubKeyNames ──────────────────────────────────────

    [Fact]
    public void EnumerateSubKeyNames_ReturnsImmediateSubKeys()
    {
        string regContent = """
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games\Dead Space 3]
            "Install Dir"=str(2):"C:\\EA Games\\Dead Space 3"

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games\Mass Effect 3]
            "Install Dir"=str(2):"C:\\EA Games\\Mass Effect 3"

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games\Dragon Age Inquisition]
            "Install Dir"=str(2):"C:\\EA Games\\Dragon Age Inquisition"
            """;

        var reader = new MockRegistryReader(regContent);

        IReadOnlyList<string> subKeys = reader.EnumerateSubKeyNames(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games");

        Assert.Equal(3, subKeys.Count);
        Assert.Contains("Dead Space 3", subKeys);
        Assert.Contains("Mass Effect 3", subKeys);
        Assert.Contains("Dragon Age Inquisition", subKeys);
    }

    [Fact]
    public void EnumerateSubKeyNames_EmptyKey_ReturnsEmptyList()
    {
        var reader = new MockRegistryReader("""
            Windows Registry Editor Version 5.00
            """);

        IReadOnlyList<string> subKeys = reader.EnumerateSubKeyNames(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\NonExistent");

        Assert.Empty(subKeys);
    }

    [Fact]
    public void EnumerateSubKeyNames_NoNestedSubKeys()
    {
        string regContent = """
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs\11903]
            "InstallDir"=str(2):"E:\\Games\\Ghost Recon Breakpoint"

            [HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs\4932]
            "InstallDir"=str(2):"E:\\Games\\The Division 2"
            """;

        var reader = new MockRegistryReader(regContent);

        IReadOnlyList<string> subKeys = reader.EnumerateSubKeyNames(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs");

        Assert.Equal(2, subKeys.Count);
        Assert.Contains("11903", subKeys);
        Assert.Contains("4932", subKeys);
    }

    // ── Multi-line Hex Skipped ────────────────────────────────────

    [Fact]
    public void ParseHexValue_SkippedGracefully()
    {
        string regContent = """
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\SomeKey]
            "BinaryValue"=hex:00,01,02,03,04,05,06,07,08,09
            "StringValue"=str(2):"still here"
            """;

        var reader = new MockRegistryReader(regContent);

        // Hex value is skipped but doesn't break parsing
        string? strValue = reader.ReadStringValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\SomeKey",
            "StringValue");
        Assert.Equal("still here", strValue);
    }

    // ── ReadKeyValues ─────────────────────────────────────────────

    [Fact]
    public void ReadKeyValues_ReturnsAllValues()
    {
        string regContent = """
            Windows Registry Editor Version 5.00

            [HKEY_LOCAL_MACHINE\SOFTWARE\TestKey]
            "Name1"=str(2):"Value1"
            "Name2"=str(2):"Value2"
            """;

        var reader = new MockRegistryReader(regContent);

        IReadOnlyDictionary<string, string> values = reader.ReadKeyValues(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\TestKey");

        Assert.Equal(2, values.Count);
        Assert.Equal("Value1", values["Name1"]);
        Assert.Equal("Value2", values["Name2"]);
    }

    [Fact]
    public void ReadKeyValues_MissingKey_ReturnsEmpty()
    {
        var reader = new MockRegistryReader("""
            Windows Registry Editor Version 5.00
            """);

        IReadOnlyDictionary<string, string> values = reader.ReadKeyValues(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\NonExistent");

        Assert.Empty(values);
    }
}
