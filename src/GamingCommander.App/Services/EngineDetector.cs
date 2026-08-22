using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

/// <summary>
/// Detects a game engine from local filesystem signals.
/// Port of detect.py <c>_detect_engine</c> (Plan 102 Phase 2 / Plan 103).
/// </summary>
public static class EngineDetector
{
    /// <summary>Returns the first matching engine, or <see cref="GameEngineKind.Unknown"/>.</summary>
    public static GameEngineKind Detect(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return GameEngineKind.Unknown;

        var dir = new DirectoryInfo(folderPath);
        if (HasUnrealEngine(dir))
            return GameEngineKind.UnrealEngine;
        if (HasUnity(dir))
            return GameEngineKind.Unity;
        if (HasRage(dir))
            return GameEngineKind.Rage;
        if (HasFrostbite(dir))
            return GameEngineKind.Frostbite;
        return GameEngineKind.Unknown;
    }

    /// <summary>Display tag for a detected engine, or empty when unknown.</summary>
    public static string ToTag(GameEngineKind kind) => kind switch
    {
        GameEngineKind.UnrealEngine => "Unreal Engine",
        GameEngineKind.Unity => "Unity",
        GameEngineKind.Rage => "RAGE",
        GameEngineKind.Frostbite => "Frostbite",
        GameEngineKind.Source => "Source",
        GameEngineKind.Godot => "Godot",
        GameEngineKind.CryEngine => "CryEngine",
        _ => string.Empty,
    };

    private static bool HasUnrealEngine(DirectoryInfo d)
    {
        if (!Directory.Exists(Path.Combine(d.FullName, "Engine")))
            return false;
        if (Directory.Exists(Path.Combine(d.FullName, "Engine", "Binaries")))
            return true;

        foreach (DirectoryInfo child in FileSystemHelper.GetDirectoriesSafe(d.FullName))
        {
            if (Directory.Exists(Path.Combine(child.FullName, "Binaries", "Win64")))
                return true;
        }

        return false;
    }

    private static bool HasUnity(DirectoryInfo d)
    {
        if (!File.Exists(Path.Combine(d.FullName, "UnityPlayer.dll")))
            return false;

        foreach (DirectoryInfo child in FileSystemHelper.GetDirectoriesSafe(d.FullName))
        {
            if (child.Name.EndsWith("_Data", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool HasRage(DirectoryInfo d)
    {
        return File.Exists(Path.Combine(d.FullName, "title.rgl"))
            && File.Exists(Path.Combine(d.FullName, "common.rpf"));
    }

    private static bool HasFrostbite(DirectoryInfo d)
    {
        return File.Exists(Path.Combine(d.FullName, "Engine.BuildInfo_Win64_retail.dll"));
    }
}
