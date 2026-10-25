namespace GamingCommander.Core.Models;

/// <summary>
/// A typed tag on a game entry (Plan 127 B13). Replaces the bare <c>string</c> tags so the
/// source/kind, an optional colour override, and whether it was auto-derived are all explicit.
/// </summary>
public sealed record Tag
{
    /// <summary>Tag text (e.g. "RPG", "Unreal Engine 5").</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Where the tag came from.</summary>
    public TagKind Kind { get; init; } = TagKind.User;

    /// <summary>Optional colour override (e.g. "#RRGGBB"); empty to use the default palette.</summary>
    public string Color { get; init; } = string.Empty;

    /// <summary>True when the tag was derived automatically (engine/genre) rather than added by the user.</summary>
    public bool Auto { get; init; }

    /// <summary>Convenience constructor for a plain user tag.</summary>
    public static Tag User(string name) => new() { Name = name, Kind = TagKind.User };

    /// <summary>Convenience constructor for an auto-derived tag.</summary>
    public static Tag Derived(string name, TagKind kind) => new() { Name = name, Kind = kind, Auto = true };
}
