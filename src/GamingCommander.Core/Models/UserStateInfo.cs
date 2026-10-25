namespace GamingCommander.Core.Models;

/// <summary>
/// User-owned state for a game entry (Plan 127 B9): how the user categorises and annotates it,
/// independent of scanned/detected data. Separate from <see cref="GameEntry.Tags"/> (typed later
/// in B13) and <see cref="GameEntry.UserOverrides"/> (field-edit markers).
/// </summary>
public sealed record UserStateInfo
{
    public bool Favourite { get; init; }
    public bool Hidden { get; init; }
    public bool Pinned { get; init; }
    public bool Archived { get; init; }

    /// <summary>Personal rating 0–5, or null when unrated.</summary>
    public int? Rating { get; init; }

    /// <summary>Free-form personal notes.</summary>
    public string Notes { get; init; } = string.Empty;

    /// <summary>Backlog/completion state.</summary>
    public CompletionState Completion { get; init; } = CompletionState.None;
}
