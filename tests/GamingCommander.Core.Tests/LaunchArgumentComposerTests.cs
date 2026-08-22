using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

public sealed class LaunchArgumentComposerTests
{
    [Fact]
    public void Toggle_AddsAndRemovesFlag()
    {
        string extras = LaunchArgumentComposer.Toggle("", "--launcher-skip", enable: true);
        Assert.Equal("--launcher-skip", extras);
        extras = LaunchArgumentComposer.Toggle(extras, "-modded", enable: true);
        Assert.Equal("--launcher-skip -modded", extras);
        extras = LaunchArgumentComposer.Toggle(extras, "--launcher-skip", enable: false);
        Assert.Equal("-modded", extras);
    }

    [Fact]
    public void Toggle_NeedsValue_DoesNotEnable()
    {
        string extras = LaunchArgumentComposer.Toggle("", "-width X", enable: true);
        Assert.Equal("", extras);
    }

    [Fact]
    public void ForProcessStart_SteamUri_IgnoresExtras()
    {
        string args = LaunchArgumentComposer.ForProcessStart(
            "steam://rungameid/1091500",
            "--launcher-skip");
        Assert.Equal("", args);
    }

    [Fact]
    public void ForProcessStart_Exe_CombinesExistingAndExtras()
    {
        string args = LaunchArgumentComposer.ForProcessStart("-windowed", "--launcher-skip");
        Assert.Equal("-windowed --launcher-skip", args);
    }

    [Fact]
    public void ContainsToken_MatchesPrimary()
    {
        Assert.True(LaunchArgumentComposer.ContainsToken("--launcher-skip -modded", "--launcher-skip"));
        Assert.False(LaunchArgumentComposer.ContainsToken("--launcher-skip", "-windowed"));
    }
}
