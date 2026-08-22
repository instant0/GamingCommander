using GamingCommander.App.Services.Metadata;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Tests;

public sealed class PcgwSectionParserTests
{
    [Fact]
    public void ParseGameDataPaths_WindowsConfigAndSaves_KeepTokens()
    {
        string wt = Read("cyberpunk_gamedata.wikitext");
        IReadOnlyList<PcgwGameDataPath> paths = PcgwSectionParser.ParseGameDataPaths(wt);

        Assert.Contains(paths, p =>
            p.Kind == "config" && p.Os == "Windows"
            && p.Template == @"{{P|localappdata}}\CD Projekt Red\Cyberpunk 2077");
        Assert.Contains(paths, p =>
            p.Kind == "saves" && p.Os == "Windows"
            && p.Template == @"{{p|userprofile}}\Saved Games\CD Projekt Red\Cyberpunk 2077");
        Assert.Contains(paths, p => p.Kind == "config" && p.Os == "OS X");
        Assert.Contains(paths, p => p.Kind == "saves" && p.Os == "OS X");
    }

    [Fact]
    public void ResolveWindows_ExpandsKnownTokensOnly()
    {
        string resolved = PcgwPathTokens.ResolveWindows(
            @"{{P|localappdata}}\CD Projekt Red\Cyberpunk 2077");
        Assert.Equal(@"%LOCALAPPDATA%\CD Projekt Red\Cyberpunk 2077", resolved);

        string saves = PcgwPathTokens.ResolveWindows(
            @"{{p|userprofile}}\Saved Games\CD Projekt Red\Cyberpunk 2077");
        Assert.Equal(@"%USERPROFILE%\Saved Games\CD Projekt Red\Cyberpunk 2077", saves);

        string osx = PcgwPathTokens.ResolveWindows(
            "{{P|osxhome}}/Library/Application Support/CD Projekt Red/Cyberpunk 2077");
        Assert.Contains("{{P|osxhome}}", osx);

        string docs = PcgwPathTokens.ResolveWindows(
            @"{{p|userprofile\Documents}}\Electronic Arts\Dead Space\");
        Assert.Equal(@"%USERPROFILE%\Documents\Electronic Arts\Dead Space\", docs);

        Assert.Null(PcgwPathTokens.ExpandForExplorer(@"{{p|userprofile\Documents}}\x"));
        Assert.Null(PcgwPathTokens.ExpandForExplorer(@"%GAME%\bin"));
    }

    [Fact]
    public void ResolveWindows_HkcuIsRegistryNotFolder()
    {
        string resolved = PcgwPathTokens.ResolveWindows(
            @"{{P|hkcu}}\Software\Nival Red\Prime World: Defenders\");
        Assert.Equal(@"HKCU\Software\Nival Red\Prime World: Defenders\", resolved);
        Assert.True(PcgwPathTokens.IsRegistry(resolved));
        Assert.False(WindowsExplorer.IsClickableFolder(resolved, gameDirectory: null));
    }

    [Fact]
    public void ResolveWindows_UplayUidSavePath()
    {
        string resolved = PcgwPathTokens.ResolveWindows(
            @"{{p|uplay}}\savegames\{{p|uid}}\54\");
        Assert.Equal(
            @"%PROGRAMFILES(X86)%\Ubisoft\Ubisoft Game Launcher\savegames\<user-id>\54\",
            resolved);
        Assert.Null(PcgwPathTokens.ExpandForExplorer(resolved));
        string? previous = Environment.GetEnvironmentVariable("PROGRAMFILES(X86)");
        try
        {
            Environment.SetEnvironmentVariable("PROGRAMFILES(X86)", @"C:\Program Files (x86)");
            Assert.Equal(
                @"C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\savegames",
                PcgwPathTokens.ForExplorer(resolved));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PROGRAMFILES(X86)", previous);
        }
    }

    [Fact]
    public void ParseCommandLineTable_WidthAndSkipStart()
    {
        string wt = Read("cyberpunk_cmdline.wikitext");
        IReadOnlyList<PcgwCommandLineEntry> rows = PcgwSectionParser.ParseCommandLineTable(wt);

        PcgwCommandLineEntry width = Assert.Single(rows, r => r.Argument == "-width X");
        Assert.True(width.NeedsValue);
        Assert.Contains("resolution width", width.Notes, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("table", width.Source);

        PcgwCommandLineEntry skip = Assert.Single(rows, r => r.Argument == "-skipStartScreen");
        Assert.False(skip.NeedsValue);
        Assert.Contains("Breaching", skip.Notes, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("{{key", skip.Notes, StringComparison.Ordinal);

        Assert.Contains(rows, r => r.Argument == "-fullscreen");
        Assert.Contains(rows, r => r.Argument == "-gpuFlag FLAG" && r.NeedsValue);
        Assert.Equal(15, rows.Count);
    }

    [Fact]
    public void ParseFixboxes_DirectExeAndBypassArgs()
    {
        string wt = Read("cyberpunk_essential.wikitext");
        IReadOnlyList<PcgwFix> fixes = PcgwSectionParser.ParseFixboxes(wt);

        PcgwFix exe = Assert.Single(fixes, f => f.SuggestedExecutable is not null);
        Assert.Equal(@"{{P|game}}\bin\x64\Cyberpunk2077.exe", exe.SuggestedExecutable);

        PcgwFix bypass = Assert.Single(fixes, f => f.SuggestedArgs is not null);
        Assert.Equal("--launcher-skip -skipStartScreen -modded", bypass.SuggestedArgs);
        Assert.Contains("Bypass launcher", bypass.Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MergeCommandLine_AddsFixboxFlagsMissingFromTable()
    {
        string sections = Read("cyberpunk_sections.wikitext");
        PcgwSectionFacts facts = PcgwSectionParser.ParseAll(sections);

        Assert.Contains(facts.CommandLine, r => r.Argument == "-width X");
        PcgwCommandLineEntry skipLauncher = Assert.Single(facts.CommandLine, r => r.Argument == "--launcher-skip");
        Assert.Equal("fixbox", skipLauncher.Source);
        Assert.Contains("separate launcher", skipLauncher.Notes, StringComparison.OrdinalIgnoreCase);

        Assert.Contains(facts.CommandLine, r => r.Argument == "-modded" && r.Source == "fixbox");

        PcgwCommandLineEntry skipStart = Assert.Single(facts.CommandLine, r => r.Argument == "-skipStartScreen");
        Assert.Equal("table", skipStart.Source);
    }

    [Fact]
    public void ParseGameDataPaths_UserProfileDocuments()
    {
        string wt = "{{Game data/saves|Windows|{{p|userprofile\\Documents}}\\Electronic Arts\\Dead Space\\}}";
        IReadOnlyList<PcgwGameDataPath> paths = PcgwSectionParser.ParseGameDataPaths(wt);
        Assert.Contains(paths, p =>
            p.Kind == "saves" && p.Os == "Windows"
            && p.Template.Contains("userprofile\\Documents", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParseVideoCaps_KeepsShortCaps_DropsAwards()
    {
        string wt = Read("cyberpunk_video.wikitext");
        IReadOnlyDictionary<string, string> video = PcgwSectionParser.ParseVideoCaps(wt);

        Assert.Equal("true", video["widescreen"]);
        Assert.Equal("limited", video["ultrawide"]);
        Assert.Equal("true", video["fov"]);
        Assert.Equal("true", video["4k"]);
        Assert.Equal("true", video["60fps"]);
        Assert.Equal("true", video["120fps"]);
        Assert.Equal("limited", video["hdr"]);
        Assert.Equal("true", video["raytracing"]);
        Assert.Equal("always on", video["antialiasing"]);
        Assert.All(video.Keys, k => Assert.DoesNotContain("wsgf", k, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParseCloudSync_SteamGogEpic()
    {
        string wt = Read("cyberpunk_gamedata.wikitext");
        IReadOnlyDictionary<string, string> cloud = PcgwSectionParser.ParseCloudSync(wt);

        Assert.Equal("true", cloud["steam cloud"]);
        Assert.Equal("true", cloud["gog galaxy"]);
        Assert.Equal("true", cloud["epic games launcher"]);
        Assert.DoesNotContain("xbox cloud", cloud.Keys);
        Assert.DoesNotContain("steam cloud notes", cloud.Keys);
    }

    [Fact]
    public void ParseStoreIds_SteamAndGog()
    {
        string wt = """
            {{Availability/row| Steam | 1091500 }}
            {{Availability/row| GOG.com | cyberpunk_2077 }}
            {{Infobox game/row/store|Steam|1091500}}
            """;
        IReadOnlyDictionary<string, string> ids = PcgwSectionParser.ParseStoreIds(wt);
        Assert.Equal("1091500", ids["Steam"]);
        Assert.Equal("cyberpunk_2077", ids["GOG"]);
    }

    [Fact]
    public void ParseAll_Empty_ReturnsEmptyCollections()
    {
        PcgwSectionFacts facts = PcgwSectionParser.ParseAll("");
        Assert.Empty(facts.Paths);
        Assert.Empty(facts.CommandLine);
        Assert.Empty(facts.Fixes);
        Assert.Empty(facts.Video);
        Assert.Empty(facts.CloudSync);
        Assert.Empty(facts.StoreIds);
    }

    [Fact]
    public void ParseAll_CombinedFixture_HasOperatorBlocks()
    {
        PcgwSectionFacts facts = PcgwSectionParser.ParseAll(Read("cyberpunk_sections.wikitext"));
        Assert.True(facts.Paths.Count >= 4);
        Assert.True(facts.CommandLine.Count >= 16);
        Assert.True(facts.Fixes.Count >= 2);
        Assert.True(facts.Video.Count >= 8);
        Assert.Equal(3, facts.CloudSync.Count);
        Assert.Equal("1091500", facts.StoreIds["Steam"]);
    }

    private static string Read(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "pcgw", name));
}
