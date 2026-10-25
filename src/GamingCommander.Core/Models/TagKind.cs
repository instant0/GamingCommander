namespace GamingCommander.Core.Models;

/// <summary>Source/kind of a tag (Plan 127 B13).</summary>
public enum TagKind
{
    /// <summary>Added by the user.</summary>
    User = 0,

    /// <summary>Derived from the store/platform.</summary>
    Store = 1,

    /// <summary>Derived from the detected/fetched engine.</summary>
    Engine = 2,

    /// <summary>Derived from genre metadata.</summary>
    Genre = 3,
}
