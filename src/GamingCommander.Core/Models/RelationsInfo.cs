namespace GamingCommander.Core.Models;

/// <summary>
/// Relationships for a game entry (Plan 127 B11): what kind of entry it is and how it links to
/// other entries (base game ↔ DLC/tools). Additive; defaults to a standalone game.
/// </summary>
public sealed record RelationsInfo
{
    /// <summary>Kind of entry (game, DLC, tool, demo, soundtrack, mod).</summary>
    public GameKind Kind { get; init; } = GameKind.Game;

    /// <summary>Id of the parent/base entry when this is an add-on; empty otherwise.</summary>
    public string ParentId { get; init; } = string.Empty;

    /// <summary>Ids of add-ons belonging to this entry.</summary>
    public List<string> AddOnIds { get; init; } = [];

    /// <summary>True when this is a normal standalone game with no links.</summary>
    public bool IsPlainGame => Kind == GameKind.Game && ParentId.Length == 0 && AddOnIds.Count == 0;
}
