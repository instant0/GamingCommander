using GamingCommander.Core.Models;

namespace GamingCommander.Core.Services;

/// <summary>
/// Single source of truth for per-store opt-in policy. Reads never throw for unknown or
/// removed kinds; writes reject anything that is not a store launcher.
/// </summary>
public interface IStorePolicyProvider
{
    /// <summary>Returns the effective policy for <paramref name="kind"/>, disabled when unset.</summary>
    StorePolicy Get(GameSourceKind kind);

    /// <summary>Persists a normalized policy. Throws <see cref="ArgumentException"/> for non-launcher kinds.</summary>
    void Set(StorePolicy policy);

    /// <summary>Launcher kinds whose indexing is enabled, in display order.</summary>
    IReadOnlyList<GameSourceKind> EnabledStores { get; }

    /// <summary>Launcher kinds enabled for indexing and startup checks, in display order.</summary>
    IReadOnlyList<GameSourceKind> CheckOnLaunchStores { get; }

    /// <summary>True when the store may be imported/indexed.</summary>
    bool CanImport(GameSourceKind kind);

    /// <summary>True when the store may be checked during startup sync.</summary>
    bool CanSync(GameSourceKind kind);
}
