namespace GamingCommander.Core.Services;

/// <summary>
/// Abstracts Windows registry access for cross-platform testability.
/// Production implementations use Microsoft.Win32.Registry; test implementations
/// read from mock .reg files.
/// </summary>
public interface IRegistryReader
{
    /// <summary>
    /// Reads a string value from the registry.
    /// Returns null if the key or value does not exist.
    /// </summary>
    string? ReadStringValue(string keyPath, string valueName);

    /// <summary>
    /// Reads all string values under a registry key.
    /// Returns an empty dictionary if the key does not exist.
    /// </summary>
    IReadOnlyDictionary<string, string> ReadKeyValues(string keyPath);

    /// <summary>
    /// Enumerates immediate subkey names under a registry key.
    /// Returns an empty list if the key does not exist.
    /// </summary>
    IReadOnlyList<string> EnumerateSubKeyNames(string keyPath);
}
