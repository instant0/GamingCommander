using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

public sealed class MetadataDetailsFormatterTests
{
    [Fact]
    public void WindowsPaths_ExpandTokens()
    {
        var details = new GameMetadataDetails
        {
            ConfigPaths =
            [
                new GameMetadataPath
                {
                    Kind = "config",
                    Os = "Windows",
                    Template = @"{{P|localappdata}}\CD Projekt Red\Cyberpunk 2077",
                },
            ],
            SavePaths =
            [
                new GameMetadataPath
                {
                    Kind = "saves",
                    Os = "Windows",
                    Template = @"{{p|userprofile}}\Saved Games\CD Projekt Red\Cyberpunk 2077",
                },
            ],
        };

        Assert.Equal(
            @"%LOCALAPPDATA%\CD Projekt Red\Cyberpunk 2077",
            MetadataDetailsFormatter.WindowsConfig(details));
        Assert.Equal(
            @"%USERPROFILE%\Saved Games\CD Projekt Red\Cyberpunk 2077",
            MetadataDetailsFormatter.WindowsSaves(details));
    }

    [Fact]
    public void CommandLineSummary_ListsArgs()
    {
        var details = new GameMetadataDetails
        {
            CommandLine =
            [
                new GameMetadataCommandLine { Argument = "--launcher-skip", Notes = "skips the separate launcher" },
                new GameMetadataCommandLine { Argument = "-width X", Notes = "resolution width" },
            ],
        };

        string text = MetadataDetailsFormatter.CommandLineSummary(details);
        Assert.Contains("--launcher-skip", text);
        Assert.DoesNotContain("skips the separate launcher", text);
        Assert.Contains("-width X", text);
    }

    [Fact]
    public void VideoSummary_PicksKnownCaps()
    {
        var details = new GameMetadataDetails
        {
            Video = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["fov"] = "true",
                ["ultrawide"] = "limited",
                ["hdr"] = "limited",
            },
        };

        string text = MetadataDetailsFormatter.VideoSummary(details);
        Assert.Contains("fov: yes", text);
        Assert.Contains("ultrawide: limited", text);
        Assert.Contains("hdr: limited", text);
        Assert.DoesNotContain(" · ", text);
    }

    [Fact]
    public void VideoSummary_DropsUrlsAndFalse()
    {
        var details = new GameMetadataDetails
        {
            Video = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["fov"] = "hackable",
                ["4k"] = "use [https://github.com/x] mouse-fix",
                ["hdr"] = "false",
            },
        };

        string text = MetadataDetailsFormatter.VideoSummary(details);
        Assert.Equal("fov: hackable", text);
        Assert.DoesNotContain("http", text);
        Assert.DoesNotContain("hdr", text);
    }

    [Fact]
    public void WindowsSaves_UserProfileDocumentsToken()
    {
        var details = new GameMetadataDetails
        {
            SavePaths =
            [
                new GameMetadataPath
                {
                    Kind = "saves",
                    Os = "Windows",
                    Template = @"{{p|userprofile\Documents}}\Electronic Arts\Dead Space\",
                },
            ],
        };

        Assert.Equal(
            @"%USERPROFILE%\Documents\Electronic Arts\Dead Space\",
            MetadataDetailsFormatter.WindowsSaves(details));
    }
}
