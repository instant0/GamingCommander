namespace GamingCommander.Core.Models;

/// <summary>
/// The game database: a flat list of all game entries. Each entry links to a
/// Library (anchor) by the <see cref="GameEntry.Library"/> name and records its
/// own physical <see cref="GameEntry.FolderPath"/>.
/// </summary>
public sealed record GamesDatabase(
    /// <summary>All game entries.</summary>
    List<GameEntry> Games);
