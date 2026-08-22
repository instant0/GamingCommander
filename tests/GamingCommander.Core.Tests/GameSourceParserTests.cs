using GamingCommander.Core.Models;

namespace GamingCommander.Core.Tests;

public sealed class GameSourceParserTests
{
    [Theory]
    [InlineData(GameSourceKind.UbisoftConnect, "Ubisoft Connect")]
    [InlineData(GameSourceKind.EaApp, "EA App")]
    [InlineData(GameSourceKind.Steam, "Steam")]
    [InlineData(GameSourceKind.SteamEmu, "Steam Emulator")]
    [InlineData(GameSourceKind.BattleNet, "Battle.net")]
    public void ToDisplayName_IsAComboItem(GameSourceKind kind, string label)
    {
        Assert.Equal(label, GameSourceParser.ToDisplayName(kind));
        Assert.Contains(label, GameSourceParser.SourceDisplayNames);
        Assert.Equal(kind, GameSourceParser.ParseFromString(label));
    }

    [Fact]
    public void EnumToString_IsNotAComboItem()
    {
        Assert.DoesNotContain("UbisoftConnect", GameSourceParser.SourceDisplayNames);
        Assert.Equal(GameSourceKind.Standalone, GameSourceParser.ParseFromString("UbisoftConnect"));
    }
}
