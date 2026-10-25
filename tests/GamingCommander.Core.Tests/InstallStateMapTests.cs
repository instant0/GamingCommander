using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

public sealed class InstallStateMapTests
{
    [Theory]
    [InlineData("Installed", InstallState.Installed)]
    [InlineData("Moved", InstallState.Moved)]
    [InlineData("Missing", InstallState.Missing)]
    [InlineData("Orphaned", InstallState.Orphaned)]
    [InlineData("Updating", InstallState.Updating)]
    [InlineData("", InstallState.Unknown)]
    [InlineData(null, InstallState.Unknown)]
    public void FromStoreStatus_Maps(string? status, InstallState expected) =>
        Assert.Equal(expected, InstallStateMap.FromStoreStatus(status));
}
