using GamingCommander.Core.Models;

namespace GamingCommander.Core.Tests;

public sealed class FileSystemEntryKindTests
{
    [Fact]
    public void Directory_IsNotFileOrParentDirectory()
    {
        Assert.NotEqual(FileSystemEntryKind.Directory, FileSystemEntryKind.File);
        Assert.NotEqual(FileSystemEntryKind.Directory, FileSystemEntryKind.ParentDirectory);
    }

    [Fact]
    public void File_IsNotDirectoryOrParentDirectory()
    {
        Assert.NotEqual(FileSystemEntryKind.File, FileSystemEntryKind.Directory);
        Assert.NotEqual(FileSystemEntryKind.File, FileSystemEntryKind.ParentDirectory);
    }

    [Fact]
    public void ParentDirectory_IsNotDirectoryOrFile()
    {
        Assert.NotEqual(FileSystemEntryKind.ParentDirectory, FileSystemEntryKind.Directory);
        Assert.NotEqual(FileSystemEntryKind.ParentDirectory, FileSystemEntryKind.File);
    }

    [Fact]
    public void AllKinds_AreDistinct()
    {
        var all = Enum.GetValues<FileSystemEntryKind>();
        Assert.Equal(3, all.Length);
        Assert.Equal(0, (int)FileSystemEntryKind.Directory);
        Assert.Equal(1, (int)FileSystemEntryKind.File);
        Assert.Equal(2, (int)FileSystemEntryKind.ParentDirectory);
    }
}
