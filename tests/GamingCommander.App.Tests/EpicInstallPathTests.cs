using GamingCommander.App.Services;

namespace GamingCommander.App.Tests;

public sealed class EpicInstallPathTests
{
    [Fact]
    public void Same_IgnoresSlashAndCase()
    {
        Assert.True(EpicInstallPath.Same(@"D:\games\cavestoryplus", @"d:/games/cavestoryplus/"));
    }

    [Fact]
    public void Same_FolderNameOnly()
    {
        Assert.True(EpicInstallPath.Same(@"D:\games\cavestoryplus", @"E:\elsewhere\cavestoryplus"));
    }
}
