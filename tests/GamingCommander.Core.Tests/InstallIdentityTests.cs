using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

/// <summary>
/// Tests for the install identity seams: full path is identity, folder name is only a
/// store-scoped weak relocation hint. Path-formatting roles are owned by PathTextTests.
/// </summary>
public sealed class InstallIdentityTests
{
    [Fact]
    public void PathKey_UncPathIsOpaque()
    {
        Assert.Equal(
            @"\\server\share\Foo",
            InstallIdentity.PathKey(@"\\server\share\Foo\"),
            ignoreCase: true);
        Assert.Equal(
            InstallIdentity.PathKey(@"\\server\share\Foo"),
            InstallIdentity.PathKey(@"\\SERVER\Share\foo"),
            ignoreCase: true);
    }

    [Fact]
    public void WeakFolderKey_IsStoreScoped()
    {
        string steam = InstallIdentity.WeakFolderKey(GameSourceKind.Steam, "Foo");
        string epic = InstallIdentity.WeakFolderKey(GameSourceKind.Epic, "Foo");

        Assert.False(string.Equals(steam, epic, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WeakFolderKey_TrimsAndIgnoresEmptyName()
    {
        Assert.Equal(
            InstallIdentity.WeakFolderKey(GameSourceKind.Steam, "Foo"),
            InstallIdentity.WeakFolderKey(GameSourceKind.Steam, "  Foo  "),
            ignoreCase: true);
        Assert.Equal(string.Empty, InstallIdentity.WeakFolderKey(GameSourceKind.Steam, null));
        Assert.Equal(string.Empty, InstallIdentity.WeakFolderKey(GameSourceKind.Steam, "   "));
    }
}
