namespace GamingCommander.Core.Models;

/// <summary>
/// Confidence tier for a detected game entry, derived from the signals that produced it
/// (Plan 123 §0.1, Plan 127 A3). Higher = more certain.
/// </summary>
public enum IdentificationTier
{
    /// <summary>No identifying signal; the entry is a guess.</summary>
    Unknown = 0,

    /// <summary>Exe/engine/container evidence only (e.g. a folder with a plausible exe).</summary>
    Candidate = 1,

    /// <summary>A local store marker or manifest identifies the game.</summary>
    Secure = 2,

    /// <summary>An authoritative structural match (Steam ACF, Epic global .item).</summary>
    Locked = 3,
}
