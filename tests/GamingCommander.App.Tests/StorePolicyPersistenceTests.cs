using System.Text.Json;
using GamingCommander.App.Services;
using GamingCommander.Core.Models;
using Xunit;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for per-store policy persistence through JsonConfigService.
/// </summary>
public sealed class StorePolicyPersistenceTests : IDisposable
{
    private readonly string _tempDir;

    public StorePolicyPersistenceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "StorePolicyTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private JsonConfigService CreateService() =>
        new(Path.Combine(_tempDir, "settings.json"));

    private void WriteSettings(string json) =>
        File.WriteAllText(Path.Combine(_tempDir, "settings.json"), json);

    [Fact]
    public void Save_PoliciesWithAllFlagsAndOverride_RoundTrips()
    {
        var svc = CreateService();
        AppConfig config = new AppConfig([], IsFirstRun: false) with
        {
            StorePolicies = new StorePolicySettings
            {
                Stores =
                [
                    new StorePolicy(GameSourceKind.Steam, true, true, true, false, "D:/steam-inventory"),
                    new StorePolicy(GameSourceKind.Epic, true, false, true, true, null),
                ],
            },
        };

        svc.Save(config);
        StorePolicySettings policies = Assert.IsType<StorePolicySettings>(svc.Load().StorePolicies);

        Assert.Equal(2, policies.Stores.Count);
        Assert.Equal(
            new StorePolicy(GameSourceKind.Steam, true, true, true, false, "D:/steam-inventory"),
            policies.Stores[0]);
        Assert.Equal(
            new StorePolicy(GameSourceKind.Epic, true, false, true, true, null),
            policies.Stores[1]);
    }

    [Fact]
    public void Save_WritesKindAsEnumName()
    {
        var svc = CreateService();
        svc.Save(new AppConfig([], IsFirstRun: false) with
        {
            StorePolicies = new StorePolicySettings
            {
                Stores = [new StorePolicy(GameSourceKind.Gog, IndexEnabled: true)],
            },
        });

        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(_tempDir, "settings.json")));
        JsonElement store = doc.RootElement.GetProperty("StorePolicies").GetProperty("Stores")[0];

        Assert.Equal("Gog", store.GetProperty("Kind").GetString());
        Assert.True(store.GetProperty("IndexEnabled").GetBoolean());
    }

    [Fact]
    public void Load_MissingFile_ReturnsEmptyPolicies()
    {
        StorePolicySettings policies = Assert.IsType<StorePolicySettings>(CreateService().Load().StorePolicies);

        Assert.Empty(policies.Stores);
    }

    [Fact]
    public void Load_PartialStoreEntries_FillsSafeDefaults()
    {
        WriteSettings("""
            {
              "Version": 1,
              "HiddenFolders": [],
              "StorePolicies": {
                "Stores": [
                  { "Kind": "Steam", "IndexEnabled": true },
                  { "Kind": "Gog" }
                ]
              }
            }
            """);

        StorePolicySettings policies = Assert.IsType<StorePolicySettings>(CreateService().Load().StorePolicies);

        Assert.Equal(2, policies.Stores.Count);
        Assert.Equal(
            new StorePolicy(GameSourceKind.Steam, true, false, false, true, null),
            policies.Stores[0]);
        Assert.Equal(
            new StorePolicy(GameSourceKind.Gog, false, false, false, true, null),
            policies.Stores[1]);
    }

    [Fact]
    public void Load_UnknownAndNonLauncherKinds_AreIgnored()
    {
        WriteSettings("""
            {
              "Version": 1,
              "HiddenFolders": [],
              "StorePolicies": {
                "Stores": [
                  { "Kind": "NotAStore", "IndexEnabled": true },
                  { "Kind": "Standalone", "IndexEnabled": true },
                  { "Kind": "Unknown", "IndexEnabled": true },
                  { "Kind": "SteamEmu", "IndexEnabled": true },
                  { "Kind": "Epic", "IndexEnabled": true }
                ]
              }
            }
            """);

        StorePolicySettings policies = Assert.IsType<StorePolicySettings>(CreateService().Load().StorePolicies);

        StorePolicy policy = Assert.Single(policies.Stores);
        Assert.Equal(GameSourceKind.Epic, policy.Kind);
    }

    [Fact]
    public void Load_DisabledStore_ForcesCheckAndEnrichOff()
    {
        WriteSettings("""
            {
              "Version": 1,
              "HiddenFolders": [],
              "StorePolicies": {
                "Stores": [
                  { "Kind": "Steam", "IndexEnabled": false, "CheckOnLaunch": true, "EnrichNewGames": true },
                  { "Kind": "Epic", "IndexEnabled": true, "CheckOnLaunch": true, "EnrichNewGames": true }
                ]
              }
            }
            """);

        StorePolicySettings policies = Assert.IsType<StorePolicySettings>(CreateService().Load().StorePolicies);

        Assert.False(policies.Stores[0].CheckOnLaunch);
        Assert.False(policies.Stores[0].EnrichNewGames);
        Assert.True(policies.Stores[1].CheckOnLaunch);
        Assert.True(policies.Stores[1].EnrichNewGames);
    }

    [Fact]
    public void Load_MissingStorePolicies_UsesDefaults()
    {
        WriteSettings("""
            {
              "Version": 1,
              "HiddenFolders": []
            }
            """);

        StorePolicySettings policies = Assert.IsType<StorePolicySettings>(CreateService().Load().StorePolicies);

        Assert.Empty(policies.Stores);
    }
}
