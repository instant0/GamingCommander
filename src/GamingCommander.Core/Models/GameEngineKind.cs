namespace GamingCommander.Core.Models;

/// <summary>
/// Game engine detected from local folder signals (Plan 102 Phase 2).
/// Values above 100 are reserved for custom/unregistered engines.
/// </summary>
public enum GameEngineKind
{
    /// <summary>No engine signal found.</summary>
    Unknown = 0,

    /// <summary>Unreal Engine (Engine/ + Binaries/ layout).</summary>
    UnrealEngine = 1,

    /// <summary>Unity (UnityPlayer.dll + *_Data/).</summary>
    Unity = 2,

    /// <summary>Rockstar Advanced Game Engine.</summary>
    Rage = 3,

    /// <summary>EA DICE Frostbite.</summary>
    Frostbite = 4,

    /// <summary>Valve Source.</summary>
    Source = 5,

    /// <summary>Godot.</summary>
    Godot = 6,

    /// <summary>CryEngine.</summary>
    CryEngine = 7,
}
