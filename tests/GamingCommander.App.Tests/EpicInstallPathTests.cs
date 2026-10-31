using GamingCommander.App.Services;

namespace GamingCommander.App.Tests;

public sealed class EpicInstallPathTests
{
    [Fact]
    public void Same_DoesNotMatchOnFolderNameAlone()
    {
        // Path comparison (slash/case/empty) is owned by PathTextTests; the Epic
        // claim key must still tell two folders with the same leaf apart.
        Assert.False(EpicInstallPath.Same(@"D:\games\cavestoryplus", @"E:\elsewhere\cavestoryplus"));
    }
}
