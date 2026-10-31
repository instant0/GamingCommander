using System.Text.Json;

namespace GamingCommander.App.Services;

/// <summary>Outcome of reading a JSON document from disk.</summary>
public enum StoreReadStatus
{
    /// <summary>The file was present and parsed.</summary>
    Ok,

    /// <summary>The file was absent (first run).</summary>
    Missing,

    /// <summary>The file was present but unparseable; it was preserved as <c>.corrupt</c>.</summary>
    Corrupt,

    /// <summary>The file was written by a newer build; it was left untouched.</summary>
    Unreadable,
}

/// <summary>Result of <see cref="JsonDocumentStore{T}.Read"/>.</summary>
/// <param name="Status">Which of the four read outcomes applies.</param>
/// <param name="Value">The parsed or default value; never null in practice.</param>
/// <param name="Message">One short sentence for the log and status line, when something went wrong.</param>
/// <param name="PreservedPath">Where the damaged bytes were kept, when they were preserved.</param>
public readonly record struct StoreReadResult<T>(
    StoreReadStatus Status,
    T? Value,
    string? Message,
    string? PreservedPath);

/// <summary>
/// The single JSON write path for local state. Reads distinguish missing, corrupt and
/// newer-version files; writes keep one rolling <c>.bak</c> and verify the new bytes parse
/// before accepting them. No temp file, no rename, no hash.
/// </summary>
public sealed class JsonDocumentStore<T> where T : class
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly JsonSerializerOptions _options;
    private readonly Action<string>? _warningSink;

    /// <summary>Creates a store bound to a JSON file path.</summary>
    /// <param name="path">Full path of the document.</param>
    /// <param name="warningSink">Optional receiver for the same message returned in the result.</param>
    /// <param name="options">Optional serializer options; defaults match the historical JSON format.</param>
    public JsonDocumentStore(
        string path,
        Action<string>? warningSink = null,
        JsonSerializerOptions? options = null)
    {
        Path = path;
        BackupPath = path + ".bak";
        CorruptPath = path + ".corrupt";
        _warningSink = warningSink;
        _options = options ?? DefaultOptions;
    }

    /// <summary>Path of the live document.</summary>
    public string Path { get; }

    /// <summary>Path of the single rolling previous copy.</summary>
    public string BackupPath { get; }

    /// <summary>Path where damaged bytes are preserved until the user removes them.</summary>
    public string CorruptPath { get; }

    /// <summary>
    /// Reads the document. Never writes on success, on a missing file, or on a newer-version file.
    /// A parse failure preserves the damaged bytes and recovers from the <c>.bak</c> when it parses.
    /// </summary>
    /// <param name="defaults">Factory for the value to use when nothing readable exists.</param>
    /// <param name="isReadable">Optional schema check; a parsed but unreadable value yields <see cref="StoreReadStatus.Unreadable"/>.</param>
    public StoreReadResult<T> Read(Func<T> defaults, Func<T, bool>? isReadable = null)
    {
        ArgumentNullException.ThrowIfNull(defaults);

        if (!File.Exists(Path))
            return new StoreReadResult<T>(StoreReadStatus.Missing, defaults(), null, null);

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(Path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            string message = $"{FileName} could not be read; using defaults.";
            return Warn(StoreReadStatus.Corrupt, defaults(), message, null);
        }

        if (bytes.Length == 0)
            return HandleDamaged(bytes, defaults, isReadable, "is empty");

        T? value;
        try
        {
            value = JsonSerializer.Deserialize<T>(bytes, _options);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            value = null;
        }

        if (value is null)
            return HandleDamaged(bytes, defaults, isReadable, "could not be parsed");

        if (isReadable is not null && !isReadable(value))
        {
            // A newer build's file is not ours to replace; leave the bytes exactly as they are.
            string message =
                $"{FileName} was written by a newer version; using defaults and leaving the file unchanged.";
            return Warn(StoreReadStatus.Unreadable, defaults(), message, null);
        }

        return new StoreReadResult<T>(StoreReadStatus.Ok, value, null, null);
    }

    /// <summary>
    /// Serializes, backs up the current file, writes, then re-reads to prove the new document
    /// parses. A failed verification restores the <c>.bak</c> and throws.
    /// </summary>
    public void Write(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        // Serialize first: a serializer failure must not reach the disk.
        string json = JsonSerializer.Serialize(value, _options);

        EnsureParentDirectory();

        if (File.Exists(Path) && new FileInfo(Path).Length > 0)
            File.Copy(Path, BackupPath, overwrite: true);

        File.WriteAllText(Path, json);

        try
        {
            string written = File.ReadAllText(Path);
            _ = JsonSerializer.Deserialize<T>(written, _options)
                ?? throw new InvalidOperationException("The saved document deserialized to null.");
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            bool restored = RestoreBackup();
            string outcome = restored
                ? $"restored the previous copy from {BackupFileName}"
                : "no previous copy exists";
            throw new InvalidOperationException(
                $"{FileName} could not be verified after writing; {outcome}.", ex);
        }
    }

    private StoreReadResult<T> HandleDamaged(
        byte[] bytes,
        Func<T> defaults,
        Func<T, bool>? isReadable,
        string reason)
    {
        string? preservedPath = PreserveDamagedBytes(bytes);

        if (TryReadBackup(isReadable, out T? recovered))
        {
            bool restored = RestoreBackup();
            string message =
                $"{FileName} {reason}; kept the damaged copy as {CorruptFileName} and "
                + $"restored the previous copy from {BackupFileName}.";
            if (!restored)
            {
                // The backup parsed, so use it in memory even if copying it back failed.
                message += " The restored copy could not be written back.";
            }
            return Warn(StoreReadStatus.Corrupt, recovered, message, preservedPath);
        }

        string fallbackMessage = File.Exists(BackupPath)
            ? $"{FileName} {reason}; kept the damaged copy as {CorruptFileName}. "
                + $"The previous copy is unreadable too; using defaults."
            : $"{FileName} {reason}; kept the damaged copy as {CorruptFileName}. "
                + "There is no previous copy; using defaults.";
        return Warn(StoreReadStatus.Corrupt, defaults(), fallbackMessage, preservedPath);
    }

    private bool TryReadBackup(Func<T, bool>? isReadable, out T? recovered)
    {
        recovered = null;
        if (!File.Exists(BackupPath))
            return false;

        try
        {
            byte[] bytes = File.ReadAllBytes(BackupPath);
            if (bytes.Length == 0)
                return false;

            T? value = JsonSerializer.Deserialize<T>(bytes, _options);
            if (value is null || (isReadable is not null && !isReadable(value)))
                return false;

            recovered = value;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return false;
        }
    }

    private string? PreserveDamagedBytes(byte[] bytes)
    {
        try
        {
            File.WriteAllBytes(CorruptPath, bytes);
            return CorruptPath;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private bool RestoreBackup()
    {
        if (!File.Exists(BackupPath))
            return false;

        try
        {
            File.Copy(BackupPath, Path, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void EnsureParentDirectory()
    {
        string? directory = System.IO.Path.GetDirectoryName(Path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            AppPaths.EnsureAppDataPath(Path);
    }

    private StoreReadResult<T> Warn(StoreReadStatus status, T? value, string message, string? preservedPath)
    {
        try
        {
            _warningSink?.Invoke(message);
        }
        catch
        {
            // A warning sink must never break a load.
        }

        return new StoreReadResult<T>(status, value, message, preservedPath);
    }

    private string FileName => System.IO.Path.GetFileName(Path);

    private string BackupFileName => System.IO.Path.GetFileName(BackupPath);

    private string CorruptFileName => System.IO.Path.GetFileName(CorruptPath);
}
