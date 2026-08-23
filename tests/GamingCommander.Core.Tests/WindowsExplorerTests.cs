using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

public sealed class WindowsExplorerTests
{
    [Fact]
    public void Normalize_QuotesNotRequiredForSimpleDrivePath()
    {
        Assert.True(WindowsExplorer.TryNormalizeFolder(@"C:\Users\Name\Documents\EA\Dead Space", out string folder));
        Assert.Equal(@"C:\Users\Name\Documents\EA\Dead Space", folder);
        Assert.True(WindowsExplorer.TryBuildOpenFolder(@"C:\Games\Foo", @"C:\Games", out string exe, out string args));
        Assert.Equal("explorer.exe", exe);
        Assert.Equal("\"C:\\Games\\Foo\"", args);
    }

    [Fact]
    public void Build_QuotesPathsWithSpaces()
    {
        Assert.True(WindowsExplorer.TryBuildOpenFolder(
            @"C:\Users\Name\Documents\Electronic Arts\Dead Space",
            @"C:\Users\Name\Documents\Electronic Arts\Dead Space",
            out _,
            out string args));
        Assert.Equal("\"C:\\Users\\Name\\Documents\\Electronic Arts\\Dead Space\"", args);
    }

    [Theory]
    [InlineData(@"C:\foo\..\Windows")]
    [InlineData(@"C:\malware.exe")]
    [InlineData("https://example.com")]
    [InlineData(@"{{p|userprofile}}\x")]
    [InlineData(@"\\evil\share\folder")]
    [InlineData(@"C:\foo:ads")]
    [InlineData("%COMSPEC%")]
    [InlineData("")]
    public void RejectsUnsafe(string path)
    {
        Assert.False(WindowsExplorer.TryNormalizeFolder(path, out _));
        Assert.False(WindowsExplorer.TryBuildOpenFolder(path, gameDirectory: null, out _, out _));
    }

    [Fact]
    public void Clickable_UplaySavegamesPrefix()
    {
        string? previous = Environment.GetEnvironmentVariable("PROGRAMFILES(X86)");
        try
        {
            Environment.SetEnvironmentVariable("PROGRAMFILES(X86)", @"C:\Program Files (x86)");
            string display =
                @"%PROGRAMFILES(X86)%\Ubisoft\Ubisoft Game Launcher\savegames\<user-id>\54\";
            Assert.True(WindowsExplorer.IsClickableFolder(display, gameDirectory: null));
            Assert.True(WindowsExplorer.TryBuildOpenFolder(display, null, out _, out string args));
            Assert.Equal(
                "\"C:\\Program Files (x86)\\Ubisoft\\Ubisoft Game Launcher\\savegames\"",
                args);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PROGRAMFILES(X86)", previous);
        }
    }

    [Fact]
    public void Clickable_GameInstallFolderItself()
    {
        Assert.True(WindowsExplorer.IsClickableFolder(@"D:\SteamLibrary\steamapps\common\Foo",
            @"D:\SteamLibrary\steamapps\common\Foo"));
        Assert.True(WindowsExplorer.TryBuildOpenFolder(
            @"D:\SteamLibrary\steamapps\common\Foo",
            @"D:\SteamLibrary\steamapps\common\Foo",
            out _, out string args));
        Assert.Equal("\"D:\\SteamLibrary\\steamapps\\common\\Foo\"", args);
    }

    [Fact]
    public void Clickable_OnlyGameOrUserProfileRoots()
    {
        string? previous = Environment.GetEnvironmentVariable("USERPROFILE");
        try
        {
            Environment.SetEnvironmentVariable("USERPROFILE", @"C:\Users\Test");
            Assert.True(WindowsExplorer.IsClickableFolder(
                @"C:\Users\Test\Documents\My Games\X", gameDirectory: null));
            Assert.True(WindowsExplorer.IsClickableFolder(
                @"C:\Games\Dead Space\config", @"C:\Games\Dead Space"));
            Assert.False(WindowsExplorer.IsClickableFolder(
                @"C:\Windows\System32", gameDirectory: null));
            Assert.False(WindowsExplorer.IsClickableFolder(
                @"C:\Other\Game", gameDirectory: @"C:\Games\Dead Space"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("USERPROFILE", previous);
        }
    }
}

public sealed class MetadataTextTests
{
    [Fact]
    public void SafePathTemplate_KeepsPcgwToken()
    {
        Assert.Equal(
            @"{{p|userprofile\Documents}}\Electronic Arts\Dead Space\",
            MetadataText.SafePathTemplate(@"{{p|userprofile\Documents}}\Electronic Arts\Dead Space\"));
    }

    [Fact]
    public void SafePathTemplate_RejectsUrl() =>
        Assert.Null(MetadataText.SafePathTemplate("https://evil.example/x"));

    [Fact]
    public void SafeArgument_AcceptsFlagsOnly()
    {
        Assert.Equal("--launcher-skip", MetadataText.SafeArgument("--launcher-skip"));
        Assert.Equal("-width X", MetadataText.SafeArgument("-width X"));
        Assert.Null(MetadataText.SafeArgument("cmd.exe /c calc"));
        Assert.Null(MetadataText.SafeArgument("http://x"));
    }

    [Fact]
    public void SafeSteamAppId_DigitsOnly()
    {
        Assert.Equal("17470", MetadataText.SafeSteamAppId("17470"));
        Assert.Null(MetadataText.SafeSteamAppId("1091500;calc"));
    }
}
