using GamingCommander.App.Services;

namespace GamingCommander.App.Tests;

public sealed class EpicItemClassifierTests
{
    [Fact]
    public void Addon_NotPlayable()
    {
        var dlc = new EpicManifestParser.EpicItemData(
            "Civ DLC", @"N:\Games\Civ", "", "ns", "id", "app", false,
            IsApplication: false, AppCategories: ["addons"]);
        Assert.True(EpicItemClassifier.IsAddon(dlc));
        Assert.False(EpicItemClassifier.IsPlayableBase(dlc));
    }

    [Fact]
    public void BaseGame_Playable()
    {
        var game = new EpicManifestParser.EpicItemData(
            "Civ", @"N:\Games\Civ", "Launcher.exe", "ns", "id", "Kinglet", false,
            IsApplication: true, AppCategories: ["public", "games", "applications"]);
        Assert.False(EpicItemClassifier.IsAddon(game));
        Assert.True(EpicItemClassifier.IsPlayableBase(game));
    }
}
