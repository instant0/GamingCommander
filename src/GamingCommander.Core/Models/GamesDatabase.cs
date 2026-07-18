namespace GamingCommander.Core.Models;

/// <summary>
/// Top-level database of all library roots and their games.
/// </summary>
public sealed record GamesDatabase(
    /// <summary>All configured library roots.</summary>
    List<GameRoot> Roots);
