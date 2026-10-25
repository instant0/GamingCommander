namespace GamingCommander.Core.Models;

/// <summary>
/// High-level provenance for a game entry: which source path created or last confirmed it
/// (Plan 129 §3.3). The specific manifest is recorded separately in
/// <see cref="DetectionInfo.ManifestKind"/>.
/// </summary>
public enum DetectionOrigin
{
    /// <summary>Identified from a store inventory/manifest.</summary>
    StoreManifest = 0,

    /// <summary>Identified by walking the filesystem.</summary>
    FileScan = 1,

    /// <summary>Added or confirmed by the user.</summary>
    User = 2,

    /// <summary>Reconciled from more than one source.</summary>
    Merged = 3,
}
