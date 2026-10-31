using GamingCommander.App.Services;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Tests;

public sealed class LauncherLocatorTests
{
    private const string MachineKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam";
    private const string UserKey = @"HKEY_CURRENT_USER\Software\Valve\Steam";
    private const string GogPathsKey =
        @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\GalaxyClient\paths";
    private const string UbisoftInstallsKey =
        @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs";
    private const string RockstarUserKey =
        @"HKEY_CURRENT_USER\Software\Rockstar Games\Launcher";
    private const string RockstarMachineKey =
        @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Launcher";

    [Fact]
    public void Steam_ConfiguredLibrary_FoundWithoutRegistryRead()
    {
        using var temp = new TempDirectory();
        temp.CreateDirectory("steamapps", "common");
        var registry = new CountingRegistry();

        LauncherDiscoveryResult result = new SteamLauncherLocator(() => [temp.Root], registry).Probe();

        Assert.True(result.Found);
        Assert.Equal(temp.Root, result.InstallPath);
        Assert.Equal(temp.Combine("steamapps"), result.InventorySource);
        Assert.Equal(0, registry.ReadCount);
    }

    [Fact]
    public void Steam_ConfiguredLibraryWithVdf_UsesIndexAsSource()
    {
        using var temp = new TempDirectory();
        string vdf = temp.CreateFile(Path.Combine("steamapps", "libraryfolders.vdf"), "\"libraryfolders\" {}");
        var registry = new CountingRegistry();

        LauncherDiscoveryResult result = new SteamLauncherLocator(() => [temp.Root], registry).Probe();

        Assert.True(result.Found);
        Assert.Equal(vdf, result.InventorySource);
        Assert.Equal(0, registry.ReadCount);
    }

    [Fact]
    public void Steam_AcfsWithoutCommon_StillAValidLibrary()
    {
        using var temp = new TempDirectory();
        temp.CreateFile(Path.Combine("steamapps", "appmanifest_1.acf"), "\"AppState\" {}");
        var registry = new CountingRegistry();

        LauncherDiscoveryResult result = new SteamLauncherLocator(() => [temp.Root], registry).Probe();

        Assert.True(result.Found);
        Assert.Equal(temp.Combine("steamapps"), result.InventorySource);
    }

    [Fact]
    public void Steam_ConfiguredRootWithoutStructure_FallsBackToRegistry()
    {
        using var temp = new TempDirectory();
        string plain = temp.CreateDirectory("plain");
        string steam = temp.CreateDirectory("steam");
        temp.CreateFile(Path.Combine("steam", "steamapps", "libraryfolders.vdf"), "\"libraryfolders\" {}");
        var registry = new MockRegistryReader($"[{MachineKey}]\r\n\"InstallPath\"=str(2):\"{steam}\"\r\n");

        LauncherDiscoveryResult result = new SteamLauncherLocator(() => [plain], registry).Probe();

        Assert.True(result.Found);
        Assert.Equal(steam, result.InstallPath);
    }

    [Fact]
    public void Steam_NoRoot_UsesUserRegistryFallback()
    {
        using var temp = new TempDirectory();
        temp.CreateFile(Path.Combine("steamapps", "libraryfolders.vdf"), "\"libraryfolders\" {}");
        var registry = new MockRegistryReader($"[{UserKey}]\r\n\"SteamPath\"=str(2):\"{temp.Root}\"\r\n");

        LauncherDiscoveryResult result = new SteamLauncherLocator(() => [], registry).Probe();

        Assert.True(result.Found);
        Assert.Equal(temp.Root, result.InstallPath);
    }

    [Fact]
    public void Steam_NoRootNoRegistry_NotFound()
    {
        var result = new SteamLauncherLocator(() => [], new CountingRegistry()).Probe();

        Assert.False(result.Found);
        Assert.Equal(GameSourceKind.Steam, result.Kind);
        Assert.Equal(LauncherReadOnlySafety.Unsupported, result.Safety);
        Assert.Contains("No Steam library", result.Reason);
    }

    [Fact]
    public void Steam_RegistryPathWithoutStructure_NotFound()
    {
        using var temp = new TempDirectory();
        string plain = temp.CreateDirectory("plain");
        var registry = new MockRegistryReader($"[{MachineKey}]\r\n\"InstallPath\"=str(2):\"{plain}\"\r\n");

        LauncherDiscoveryResult result = new SteamLauncherLocator(() => [], registry).Probe();

        Assert.False(result.Found);
        Assert.Contains("steamapps", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Epic_FoundWhenManifestsDirHasItem()
    {
        using var temp = new TempDirectory();
        temp.CreateFile(Path.Combine("manifests", "Game.item"), "{}");
        string manifests = temp.Combine("manifests");

        LauncherDiscoveryResult result = new EpicLauncherLocator(manifests).Probe();

        Assert.True(result.Found);
        Assert.Null(result.InstallPath);
        Assert.Equal(manifests, result.InventorySource);
        Assert.Equal(LauncherReadOnlySafety.Safe, result.Safety);
    }

    [Fact]
    public void Epic_MissingOrEmptyManifestsDir_NotFound()
    {
        using var temp = new TempDirectory();
        string manifests = temp.CreateDirectory("manifests");

        LauncherDiscoveryResult result = new EpicLauncherLocator(manifests).Probe();

        Assert.False(result.Found);
        Assert.Contains(".item", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Epic_MissingManifestsDir_NotFound()
    {
        using var temp = new TempDirectory();
        string manifests = temp.Combine("missing");

        LauncherDiscoveryResult result = new EpicLauncherLocator(manifests).Probe();

        Assert.False(result.Found);
        Assert.Contains("not found", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Gog_CommonPathKey_FoundNeedsVerification()
    {
        using var temp = new TempDirectory();
        var registry = new MockRegistryReader(
            $"[{GogPathsKey}]\r\n\"common\"=str(2):\"{temp.Root}\"\r\n");

        LauncherDiscoveryResult result = new GogLauncherLocator(registry).Probe();

        Assert.True(result.Found);
        Assert.Equal(temp.Root, result.InstallPath);
        Assert.Equal(temp.Root, result.InventorySource);
        Assert.Equal(LauncherReadOnlySafety.NeedsVerification, result.Safety);
    }

    [Fact]
    public void Gog_ClientPathKeyIsFallback()
    {
        using var temp = new TempDirectory();
        var registry = new MockRegistryReader(
            $"[{GogPathsKey}]\r\n\"client\"=str(2):\"{temp.Root}\"\r\n");

        LauncherDiscoveryResult result = new GogLauncherLocator(registry).Probe();

        Assert.True(result.Found);
        Assert.Equal(temp.Root, result.InstallPath);
    }

    [Fact]
    public void Gog_DefaultPathIsFallback()
    {
        using var temp = new TempDirectory();
        var registry = new MockRegistryReader("");

        LauncherDiscoveryResult result = new GogLauncherLocator(registry, [temp.Root]).Probe();

        Assert.True(result.Found);
        Assert.Equal(temp.Root, result.InstallPath);
    }

    [Fact]
    public void Gog_NoKeyOrDefault_NotFound()
    {
        var result = new GogLauncherLocator(new MockRegistryReader(""), []).Probe();

        Assert.False(result.Found);
        Assert.Equal(GameSourceKind.Gog, result.Kind);
        Assert.Equal(LauncherReadOnlySafety.Unsupported, result.Safety);
    }

    [Fact]
    public void Gog_KeyPathMissing_NotFound()
    {
        using var temp = new TempDirectory();
        string missing = temp.Combine("missing");
        var registry = new MockRegistryReader(
            $"[{GogPathsKey}]\r\n\"common\"=str(2):\"{missing}\"\r\n");

        LauncherDiscoveryResult result = new GogLauncherLocator(registry, []).Probe();

        Assert.False(result.Found);
        Assert.Contains("does not exist", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Ubisoft_InstallRecords_FoundNeedsVerification()
    {
        var registry = new MockRegistryReader(
            $"[{UbisoftInstallsKey}\\635]\r\n\"InstallDir\"=str(2):\"C:\\Games\\UbiOne\"\r\n" +
            $"[{UbisoftInstallsKey}\\636]\r\n\"InstallDir\"=str(2):\"C:\\Games\\UbiTwo\"\r\n");

        LauncherDiscoveryResult result = new UbisoftLauncherLocator(registry).Probe();

        Assert.True(result.Found);
        Assert.Null(result.InstallPath);
        Assert.Equal(UbisoftInstallsKey, result.InventorySource);
        Assert.Equal(LauncherReadOnlySafety.NeedsVerification, result.Safety);
    }

    [Fact]
    public void Ubisoft_MissingOrEmptyKey_NotFound()
    {
        var missing = new UbisoftLauncherLocator(new MockRegistryReader("")).Probe();
        var empty = new UbisoftLauncherLocator(
            new MockRegistryReader($"[{UbisoftInstallsKey}]\r\n\"Other\"=str(2):\"x\"\r\n")).Probe();

        Assert.False(missing.Found);
        Assert.False(empty.Found);
        Assert.Equal(LauncherReadOnlySafety.Unsupported, missing.Safety);
    }

    [Fact]
    public void Rockstar_InstallKeyAndSettings_FoundNeedsVerification()
    {
        using var temp = new TempDirectory();
        string settings = temp.CreateFile(
            Path.Combine("Rockstar Games", "Launcher", "Settings.json"), "{}");
        var registry = new MockRegistryReader(
            $"[{RockstarUserKey}]\r\n\"InstallFolder\"=str(2):\"C:\\Program Files\\Rockstar Games\\Launcher\"\r\n");

        LauncherDiscoveryResult result = new RockstarLauncherLocator(registry, settings).Probe();

        Assert.True(result.Found);
        Assert.Equal(@"C:\Program Files\Rockstar Games\Launcher", result.InstallPath);
        Assert.Equal(settings, result.InventorySource);
        Assert.Equal(LauncherReadOnlySafety.NeedsVerification, result.Safety);
    }

    [Fact]
    public void Rockstar_MachineKeyIsFallback()
    {
        using var temp = new TempDirectory();
        string settings = temp.CreateFile(
            Path.Combine("Rockstar Games", "Launcher", "Settings.json"), "{}");
        var registry = new MockRegistryReader(
            $"[{RockstarMachineKey}]\r\n\"InstallFolder\"=str(2):\"C:\\Rockstar\"\r\n");

        LauncherDiscoveryResult result = new RockstarLauncherLocator(registry, settings).Probe();

        Assert.True(result.Found);
        Assert.Equal(@"C:\Rockstar", result.InstallPath);
    }

    [Fact]
    public void Rockstar_MissingKeyOrSettings_NotFound()
    {
        using var temp = new TempDirectory();
        string settings = temp.CreateFile(
            Path.Combine("Rockstar Games", "Launcher", "Settings.json"), "{}");

        LauncherDiscoveryResult noKey =
            new RockstarLauncherLocator(new MockRegistryReader(""), settings).Probe();
        LauncherDiscoveryResult noSettings = new RockstarLauncherLocator(
            new MockRegistryReader(
                $"[{RockstarUserKey}]\r\n\"InstallFolder\"=str(2):\"C:\\Rockstar\"\r\n"),
            temp.Combine("missing", "Settings.json")).Probe();

        Assert.False(noKey.Found);
        Assert.False(noSettings.Found);
        Assert.Equal(LauncherReadOnlySafety.Unsupported, noKey.Safety);
    }

    [Fact]
    public void Xbox_AlwaysUnsupported()
    {
        LauncherDiscoveryResult result = new XboxLauncherLocator().Probe();

        Assert.False(result.Found);
        Assert.Equal(GameSourceKind.Xbox, result.Kind);
        Assert.Equal(LauncherReadOnlySafety.Unsupported, result.Safety);
        Assert.Contains("no inventory reader", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DiscoverAll_IncludesRockstarAndXboxRows()
    {
        using var temp = new TempDirectory();
        string settings = temp.CreateFile(
            Path.Combine("Rockstar Games", "Launcher", "Settings.json"), "{}");
        var discovery = new LauncherDiscoveryService(
        [
            new RockstarLauncherLocator(new MockRegistryReader(""), settings),
            new XboxLauncherLocator(),
        ]);

        IReadOnlyList<LauncherDiscoveryResult> results = discovery.DiscoverAll();

        Assert.Contains(results, r => r.Kind == GameSourceKind.Rockstar);
        Assert.Contains(results, r => r.Kind == GameSourceKind.Xbox);
    }

    /// <summary>Registry double that fails if the locator reads it when a configured root is enough.</summary>
    private sealed class CountingRegistry : IRegistryReader
    {
        public int ReadCount { get; private set; }

        public string? ReadStringValue(string keyPath, string valueName)
        {
            ReadCount++;
            return null;
        }

        public IReadOnlyDictionary<string, string> ReadKeyValues(string keyPath) => new Dictionary<string, string>();

        public IReadOnlyList<string> EnumerateSubKeyNames(string keyPath) => [];
    }
}
