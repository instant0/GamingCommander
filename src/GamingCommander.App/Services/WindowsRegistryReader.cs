using System.Runtime.Versioning;
using GamingCommander.Core.Services;
using Microsoft.Win32;

namespace GamingCommander.App.Services;

/// <summary>
/// Reads Windows registry values using Microsoft.Win32.Registry.
/// Only works on Windows; throws PlatformNotSupportedException on other OS.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsRegistryReader : IRegistryReader
{
    /// <summary>Reads a string value from the registry.</summary>
    public string? ReadStringValue(string keyPath, string valueName)
    {
        using RegistryKey? key = OpenKey(keyPath);
        return key?.GetValue(valueName) as string;
    }

    /// <summary>Reads all string values under a registry key.</summary>
    public IReadOnlyDictionary<string, string> ReadKeyValues(string keyPath)
    {
        using RegistryKey? key = OpenKey(keyPath);
        if (key is null) return new Dictionary<string, string>();

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string name in key.GetValueNames())
        {
            if (key.GetValue(name) is string value)
                result[name] = value;
        }
        return result;
    }

    /// <summary>Enumerates immediate subkey names under a registry key.</summary>
    public IReadOnlyList<string> EnumerateSubKeyNames(string keyPath)
    {
        using RegistryKey? key = OpenKey(keyPath);
        if (key is null) return [];

        return key.GetSubKeyNames();
    }

    /// <summary>
    /// Opens a registry key from a full key path (e.g., "HKEY_LOCAL_MACHINE\SOFTWARE\...").
    /// Supports HKLM and HKCU hives. Returns null for unrecognized hives.
    /// </summary>
    private static RegistryKey? OpenKey(string keyPath)
    {
        if (keyPath.StartsWith(@"HKEY_LOCAL_MACHINE\", StringComparison.OrdinalIgnoreCase))
            return Registry.LocalMachine.OpenSubKey(keyPath["HKEY_LOCAL_MACHINE\\".Length..]);
        if (keyPath.StartsWith(@"HKEY_CURRENT_USER\", StringComparison.OrdinalIgnoreCase))
            return Registry.CurrentUser.OpenSubKey(keyPath["HKEY_CURRENT_USER\\".Length..]);
        return null;
    }
}
