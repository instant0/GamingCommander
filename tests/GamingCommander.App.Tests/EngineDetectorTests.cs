using GamingCommander.App.Services;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Tests;

public sealed class EngineDetectorTests : IDisposable
{
    private readonly string _tempDir;

    public EngineDetectorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "EngineDet_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Detect_MissingFolder_ReturnsUnknown()
    {
        Assert.Equal(GameEngineKind.Unknown, EngineDetector.Detect(Path.Combine(_tempDir, "nope")));
    }

    [Fact]
    public void Detect_UnrealEngineBinaries_ReturnsUnreal()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "Engine", "Binaries"));

        Assert.Equal(GameEngineKind.UnrealEngine, EngineDetector.Detect(_tempDir));
        Assert.Equal("Unreal Engine", EngineDetector.ToTag(GameEngineKind.UnrealEngine));
    }

    [Fact]
    public void Detect_UnrealChildWin64_ReturnsUnreal()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, Path.GetFileName(_tempDir), "Binaries", "Win64"));

        Assert.Equal(GameEngineKind.UnrealEngine, EngineDetector.Detect(_tempDir));
    }

    [Fact]
    public void Detect_Unity_ReturnsUnity()
    {
        File.WriteAllText(Path.Combine(_tempDir, "UnityPlayer.dll"), "");
        Directory.CreateDirectory(Path.Combine(_tempDir, "HollowKnight_Data"));

        Assert.Equal(GameEngineKind.Unity, EngineDetector.Detect(_tempDir));
        Assert.Equal("Unity", EngineDetector.ToTag(GameEngineKind.Unity));
    }

    [Fact]
    public void Detect_UnityDllWithoutData_ReturnsUnknown()
    {
        File.WriteAllText(Path.Combine(_tempDir, "UnityPlayer.dll"), "");

        Assert.Equal(GameEngineKind.Unknown, EngineDetector.Detect(_tempDir));
    }

    [Fact]
    public void Detect_Rage_ReturnsRage()
    {
        File.WriteAllText(Path.Combine(_tempDir, "title.rgl"), "");
        File.WriteAllText(Path.Combine(_tempDir, "common.rpf"), "");

        Assert.Equal(GameEngineKind.Rage, EngineDetector.Detect(_tempDir));
    }

    [Fact]
    public void Detect_Frostbite_ReturnsFrostbite()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Engine.BuildInfo_Win64_retail.dll"), "");

        Assert.Equal(GameEngineKind.Frostbite, EngineDetector.Detect(_tempDir));
    }

    [Fact]
    public void Detect_EmptyFolder_ReturnsUnknown()
    {
        Assert.Equal(GameEngineKind.Unknown, EngineDetector.Detect(_tempDir));
        Assert.Equal(string.Empty, EngineDetector.ToTag(GameEngineKind.Unknown));
    }
}
