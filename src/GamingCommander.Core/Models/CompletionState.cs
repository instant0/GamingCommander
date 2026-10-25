namespace GamingCommander.Core.Models;

/// <summary>Backlog/completion state for a game entry (Plan 127 B9).</summary>
public enum CompletionState
{
    /// <summary>Not tracked.</summary>
    None = 0,

    /// <summary>Owned but not started.</summary>
    Backlog = 1,

    /// <summary>Currently playing.</summary>
    Playing = 2,

    /// <summary>Finished.</summary>
    Completed = 3,

    /// <summary>Dropped.</summary>
    Abandoned = 4,
}
