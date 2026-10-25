namespace GamingCommander.Core.Models;

/// <summary>
/// Usage tracking for a game entry (Plan 127 B7). Populated on launch; play-time is best-effort
/// (direct exe launches only — URI launches have no process handle).
/// </summary>
public sealed record TrackingInfo
{
    /// <summary>When the game was first recorded (first launch or first scan with this record).</summary>
    public DateTimeOffset? FirstSeen { get; init; }

    /// <summary>When the game was last launched.</summary>
    public DateTimeOffset? LastPlayed { get; init; }

    /// <summary>How many times the game has been launched.</summary>
    public int LaunchCount { get; init; }

    /// <summary>Total tracked play time in seconds (best-effort).</summary>
    public long PlayTimeSeconds { get; init; }

    /// <summary>Duration of the most recent tracked session in seconds (best-effort).</summary>
    public long LastSessionSeconds { get; init; }

    /// <summary>True when no tracking has been recorded.</summary>
    public bool IsEmpty => LastPlayed is null && LaunchCount == 0 && PlayTimeSeconds == 0;
}
