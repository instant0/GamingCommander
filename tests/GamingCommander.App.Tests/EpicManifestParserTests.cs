using GamingCommander.App.Services;
using Xunit;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for EpicManifestParser — Epic Games Store .item/.mancpn parsing,
/// local identifier extraction, global cross-reference, and path resolution.
/// </summary>
public sealed class EpicManifestParserTests : IDisposable
{
    private readonly string _tempDir;

    public EpicManifestParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "EpicManifestTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    // ════════════════════════════════════════════════════════════════
    //  Helper methods
    // ════════════════════════════════════════════════════════════════

    private static string MakeItemJson(
        string displayName = "Test Game",
        string installLocation = "D:/Games/TestGame",
        string launchExecutable = "Binaries/Win64/TestGame.exe",
        string catalogNamespace = "a1b2c3d4-e5f6-7890-abcd-ef0123456789",
        string catalogItemId = "b2c3d4e5-f6a7-8901-bcde-f01234567890",
        string appName = "TestGame",
        bool isIncompleteInstall = false)
    {
        return "{\"FormatVersion\":0,"
            + $"\"bIsIncompleteInstall\":{isIncompleteInstall.ToString().ToLowerInvariant()},"
            + $"\"LaunchExecutable\":\"{launchExecutable}\","
            + $"\"DisplayName\":\"{displayName}\","
            + $"\"InstallLocation\":\"{installLocation}\","
            + $"\"CatalogNamespace\":\"{catalogNamespace}\","
            + $"\"CatalogItemId\":\"{catalogItemId}\","
            + $"\"AppName\":\"{appName}\""
            + "}";
    }

    private static string MakeMancpnJson(
        string catalogNamespace = "a1b2c3d4-e5f6-7890-abcd-ef0123456789",
        string catalogItemId = "b2c3d4e5-f6a7-8901-bcde-f01234567890",
        string appName = "TestGame")
    {
        return "{\"CatalogItemId\":\"" + catalogItemId + "\","
            + "\"CatalogNamespace\":\"" + catalogNamespace + "\","
            + "\"AppName\":\"" + appName + "\""
            + "}";
    }

    // ════════════════════════════════════════════════════════════════
    //  ParseItemFile tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ParseItemFile_BasicFields_ExtractsMetadata()
    {
        string filePath = Path.Combine(_tempDir, "test.item");
        File.WriteAllText(filePath, MakeItemJson(
            displayName: "Fortnite",
            launchExecutable: "FortniteGame/Binaries/Win64/FortniteClient-Win64-Shipping.exe",
            catalogNamespace: "caca23a0-954f-4c1a-ba1f-dd7e277b81e2",
            catalogItemId: "abc123d4-e5f6-7890-abcd-ef0123456789",
            appName: "Fortnite"));

        var result = EpicManifestParser.ParseItemFile(filePath);

        Assert.NotNull(result);
        Assert.Equal("Fortnite", result.DisplayName);
        Assert.Equal("caca23a0-954f-4c1a-ba1f-dd7e277b81e2", result.CatalogNamespace);
        Assert.Equal("abc123d4-e5f6-7890-abcd-ef0123456789", result.CatalogItemId);
        Assert.Equal("Fortnite", result.AppName);
        Assert.Contains("FortniteClient-Win64-Shipping.exe", result.LaunchExecutable);
        Assert.False(result.IsIncompleteInstall);
    }

    [Fact]
    public void ParseItemFile_IncompleteInstall_ReturnsNull()
    {
        string filePath = Path.Combine(_tempDir, "incomplete.item");
        File.WriteAllText(filePath, MakeItemJson(isIncompleteInstall: true));

        var result = EpicManifestParser.ParseItemFile(filePath);

        Assert.Null(result);
    }

    [Fact]
    public void ParseItemFile_MalformedJson_ReturnsNull()
    {
        string filePath = Path.Combine(_tempDir, "broken.item");
        File.WriteAllText(filePath, "{ broken json");

        var result = EpicManifestParser.ParseItemFile(filePath);

        Assert.Null(result);
    }

    [Fact]
    public void ParseItemFile_MissingFile_ReturnsNull()
    {
        var result = EpicManifestParser.ParseItemFile(Path.Combine(_tempDir, "nonexistent.item"));

        Assert.Null(result);
    }

    // ════════════════════════════════════════════════════════════════
    //  ParseMancpnFile tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ParseMancpnFile_IdentifiersOnly_ExtractsIds()
    {
        string filePath = Path.Combine(_tempDir, "test.mancpn");
        File.WriteAllText(filePath, MakeMancpnJson(
            catalogNamespace: "caca23a0-954f-4c1a-ba1f-dd7e277b81e2",
            catalogItemId: "abc123d4-e5f6-7890-abcd-ef0123456789",
            appName: "Fortnite"));

        var result = EpicManifestParser.ParseMancpnFile(filePath);

        Assert.NotNull(result);
        Assert.Equal("caca23a0-954f-4c1a-ba1f-dd7e277b81e2", result.CatalogNamespace);
        Assert.Equal("abc123d4-e5f6-7890-abcd-ef0123456789", result.CatalogItemId);
        Assert.Equal("Fortnite", result.AppName);
        // .mancpn doesn't have DisplayName or LaunchExecutable
        Assert.Empty(result.DisplayName);
        Assert.Empty(result.LaunchExecutable);
    }

    // ════════════════════════════════════════════════════════════════
    //  ExtractLocalIdentifiers tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractLocalIdentifiers_PrefersItemOverMancpn()
    {
        // Create .egstore/manifests/ with both .item and .mancpn
        string manifestsDir = Path.Combine(_tempDir, ".egstore", "manifests");
        Directory.CreateDirectory(manifestsDir);

        File.WriteAllText(Path.Combine(manifestsDir, "test.item"), MakeItemJson(
            displayName: "From Item",
            appName: "ItemApp"));
        File.WriteAllText(Path.Combine(manifestsDir, "test.mancpn"), MakeMancpnJson(
            appName: "MancpnApp"));

        var result = EpicManifestParser.ExtractLocalIdentifiers(new DirectoryInfo(_tempDir));

        Assert.NotNull(result);
        Assert.Equal("From Item", result.DisplayName);
        Assert.Equal("ItemApp", result.AppName);
    }

    [Fact]
    public void ExtractLocalIdentifiers_FallsBackToMancpn()
    {
        // Create .egstore/ with only .mancpn
        string storeDir = Path.Combine(_tempDir, ".egstore");
        Directory.CreateDirectory(storeDir);

        File.WriteAllText(Path.Combine(storeDir, "test.mancpn"), MakeMancpnJson(
            appName: "MancpnOnly"));

        var result = EpicManifestParser.ExtractLocalIdentifiers(new DirectoryInfo(_tempDir));

        Assert.NotNull(result);
        Assert.Equal("MancpnOnly", result.AppName);
        Assert.Empty(result.DisplayName);
    }

    [Fact]
    public void ExtractLocalIdentifiers_EgsstoreDir_Works()
    {
        // Create .egsstore/manifests/ (alternative directory name)
        string manifestsDir = Path.Combine(_tempDir, ".egsstore", "manifests");
        Directory.CreateDirectory(manifestsDir);

        File.WriteAllText(Path.Combine(manifestsDir, "test.item"), MakeItemJson(
            displayName: "Egsstore Game"));

        var result = EpicManifestParser.ExtractLocalIdentifiers(new DirectoryInfo(_tempDir));

        Assert.NotNull(result);
        Assert.Equal("Egsstore Game", result.DisplayName);
    }

    [Fact]
    public void ExtractLocalIdentifiers_MissingDir_ReturnsNull()
    {
        var result = EpicManifestParser.ExtractLocalIdentifiers(new DirectoryInfo(_tempDir));

        Assert.Null(result);
    }

    // ════════════════════════════════════════════════════════════════
    //  CrossReferenceGlobalManifests tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void CrossReferenceGlobalManifests_Match_ReturnsItemData()
    {
        // Create global manifests dir with .item file
        string globalDir = Path.Combine(_tempDir, "global_manifests");
        Directory.CreateDirectory(globalDir);

        // Use the temp dir as the "game folder"
        string gamePath = Path.Combine(_tempDir, "MyGame");
        Directory.CreateDirectory(gamePath);

        File.WriteAllText(Path.Combine(globalDir, "test.item"), MakeItemJson(
            displayName: "Global Game",
            installLocation: gamePath));

        var result = EpicManifestParser.CrossReferenceGlobalManifests(
            new DirectoryInfo(gamePath), globalDir);

        Assert.NotNull(result);
        Assert.Equal("Global Game", result.DisplayName);
    }

    [Fact]
    public void CrossReferenceGlobalManifests_NoMatch_ReturnsNull()
    {
        // Create global manifests dir with .item pointing to different path
        string globalDir = Path.Combine(_tempDir, "global_manifests");
        Directory.CreateDirectory(globalDir);

        string gamePath = Path.Combine(_tempDir, "MyGame");
        Directory.CreateDirectory(gamePath);

        File.WriteAllText(Path.Combine(globalDir, "test.item"), MakeItemJson(
            installLocation: "D:/Different/Path"));

        var result = EpicManifestParser.CrossReferenceGlobalManifests(
            new DirectoryInfo(gamePath), globalDir);

        Assert.Null(result);
    }

    [Fact]
    public void CrossReferenceGlobalManifests_MissingDir_ReturnsNull()
    {
        string gamePath = Path.Combine(_tempDir, "MyGame");
        Directory.CreateDirectory(gamePath);

        var result = EpicManifestParser.CrossReferenceGlobalManifests(
            new DirectoryInfo(gamePath), Path.Combine(_tempDir, "nonexistent"));

        Assert.Null(result);
    }

    [Fact]
    public void CrossReferenceGlobalManifests_CaseInsensitive_Matches()
    {
        // Create global manifests dir
        string globalDir = Path.Combine(_tempDir, "global_manifests");
        Directory.CreateDirectory(globalDir);

        // Game path with different casing
        string gamePath = Path.Combine(_tempDir, "MyGame");
        Directory.CreateDirectory(gamePath);

        // .item has lowercase InstallLocation
        File.WriteAllText(Path.Combine(globalDir, "test.item"), MakeItemJson(
            installLocation: gamePath.ToLowerInvariant()));

        var result = EpicManifestParser.CrossReferenceGlobalManifests(
            new DirectoryInfo(gamePath), globalDir);

        Assert.NotNull(result);
    }

    [Fact]
    public void CrossReferenceGlobalManifests_TrailingSeparator_Matches()
    {
        // Create global manifests dir
        string globalDir = Path.Combine(_tempDir, "global_manifests");
        Directory.CreateDirectory(globalDir);

        // Game path
        string gamePath = Path.Combine(_tempDir, "MyGame");
        Directory.CreateDirectory(gamePath);

        // .item has trailing forward slash (Linux) or backslash (Windows)
        string trailingSep = Path.DirectorySeparatorChar.ToString();
        File.WriteAllText(Path.Combine(globalDir, "test.item"), MakeItemJson(
            installLocation: gamePath + trailingSep));

        var result = EpicManifestParser.CrossReferenceGlobalManifests(
            new DirectoryInfo(gamePath), globalDir);

        Assert.NotNull(result);
    }

    // ════════════════════════════════════════════════════════════════
    //  ResolveLaunchExecutable tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ResolveLaunchExecutable_RelativePath_ReturnsAbsolute()
    {
        string installLocation = Path.Combine(_tempDir, "MyGame");
        string launchExecutable = "Binaries/Win64/Game.exe";

        string result = EpicManifestParser.ResolveLaunchExecutable(installLocation, launchExecutable);

        Assert.True(Path.IsPathRooted(result), $"Expected absolute path, got: {result}");
        Assert.EndsWith(Path.Combine("Binaries", "Win64", "Game.exe"), result);
    }

    [Fact]
    public void ResolveLaunchExecutable_AbsolutePath_PreservedAsIs()
    {
        string absolutePath = Path.Combine(_tempDir, "Game.exe");

        string result = EpicManifestParser.ResolveLaunchExecutable(
            Path.Combine(_tempDir, "MyGame"), absolutePath);

        Assert.Equal(absolutePath, result);
    }

    [Fact]
    public void ResolveLaunchExecutable_EmptyLaunchExe_ReturnsEmpty()
    {
        string result = EpicManifestParser.ResolveLaunchExecutable(
            Path.Combine(_tempDir, "MyGame"), "");

        Assert.Empty(result);
    }
}
