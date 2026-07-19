using System.Text.Json;

namespace GamingCommander.App.Services;

/// <summary>
/// Shared JSON file serialization and deserialization operations.
/// Provides a unified read/write pattern with sensible defaults.
/// </summary>
internal static class JsonFileHelper
{
    /// <summary>
    /// Default JSON serialization options: indented output, case-insensitive property names.
    /// </summary>
    internal static JsonSerializerOptions DefaultOptions { get; } = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Reads and deserializes a JSON file. Returns the default value if the file
    /// does not exist, is empty, or cannot be deserialized.
    /// </summary>
    /// <typeparam name="T">The target deserialization type.</typeparam>
    /// <param name="filePath">Full path to the JSON file.</param>
    /// <param name="defaultFactory">Factory that creates the default value to return on failure.</param>
    /// <param name="options">Optional serializer options. Uses <see cref="DefaultOptions"/> if null.</param>
    internal static T? ReadFromFile<T>(string filePath, Func<T> defaultFactory, JsonSerializerOptions? options = null)
    {
        if (!File.Exists(filePath))
            return defaultFactory();

        try
        {
            string json = File.ReadAllText(filePath);
            var result = JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
            return result ?? defaultFactory();
        }
        catch
        {
            return defaultFactory();
        }
    }

    /// <summary>
    /// Serializes data to JSON and writes it to a file.
    /// Creates the parent directory if it does not exist.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="filePath">Full path to the output file.</param>
    /// <param name="data">The object to serialize.</param>
    /// <param name="options">Optional serializer options. Uses <see cref="DefaultOptions"/> if null.</param>
    internal static void WriteToFile<T>(string filePath, T data, JsonSerializerOptions? options = null)
    {
        EnsureDirectoryExists(filePath);
        string json = JsonSerializer.Serialize(data, options ?? DefaultOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Creates the parent directory of the specified file path if it does not exist.
    /// </summary>
    /// <param name="filePath">A file path whose parent directory should exist.</param>
    internal static void EnsureDirectoryExists(string filePath)
    {
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }
}
