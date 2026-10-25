namespace GamingCommander.Core.Services;

/// <summary>
/// Central schema version for the persisted JSON files (Plan 127 A4): <c>settings.json</c>,
/// <c>libraries.json</c>, <c>games.json</c>, <c>games_metadata.json</c>.
/// <para>
/// Nothing has shipped, so the current version is 1 and no migrations exist yet. When a breaking
/// schema change lands: bump <see cref="Current"/> and add a migration step in the relevant store's
/// load path. A file written by a newer build is rejected rather than silently misread.
/// </para>
/// </summary>
public static class SchemaVersioning
{
    /// <summary>Current schema version understood by this build.</summary>
    public const int Current = 1;

    /// <summary>
    /// True when a file written at <paramref name="fileVersion"/> can be read by this build.
    /// </summary>
    public static bool IsReadable(int fileVersion) => fileVersion <= Current;
}
