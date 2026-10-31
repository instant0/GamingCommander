namespace GamingCommander.Core.Models;

/// <summary>
/// How safe it is to read a launcher's inventory source. A store may be located but have no readable
/// inventory; that is reported as <see cref="Unsupported"/> rather than guessed.
/// </summary>
public enum LauncherReadOnlySafety
{
    /// <summary>Plain files or documented registry values; read-only access is safe.</summary>
    Safe,

    /// <summary>Format or schema not verified; do not treat it as a complete inventory without evidence.</summary>
    NeedsVerification,

    /// <summary>No readable inventory in this build (encrypted, package-only, or absent).</summary>
    Unsupported,
}

/// <summary>
/// Result of probing one launcher. <see cref="Found"/> is true only when the launcher and a plausible
/// inventory source were both located. The result is a hint, never proof that a game is installed.
/// </summary>
public sealed record LauncherDiscoveryResult(
    GameSourceKind Kind,
    bool Found,
    string? InstallPath,
    string? InventorySource,
    LauncherReadOnlySafety Safety,
    string Reason)
{
    /// <summary>A store that was looked for and not found.</summary>
    public static LauncherDiscoveryResult NotFound(GameSourceKind kind, string reason) =>
        new(kind, false, null, null, LauncherReadOnlySafety.Unsupported, reason);

    /// <summary>A store with no readable inventory in this build.</summary>
    public static LauncherDiscoveryResult Unsupported(GameSourceKind kind, string reason) =>
        new(kind, false, null, null, LauncherReadOnlySafety.Unsupported, reason);
}
