namespace GamingCommander.App.Tests;

/// <summary>
/// Disposable scratch directory for filesystem tests. Creates a unique directory
/// under the temp path and removes it on dispose; never touches repository data.
/// </summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory(string prefix = "gc_test_")
    {
        Root = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            prefix + Guid.NewGuid().ToString("N")[..12]);
        Directory.CreateDirectory(Root);
    }

    /// <summary>Absolute path of the created scratch directory.</summary>
    public string Root { get; }

    /// <summary>Combines <paramref name="parts"/> under <see cref="Root"/> without creating anything.</summary>
    public string Combine(params string[] parts) =>
        System.IO.Path.Combine([Root, .. parts]);

    /// <summary>Creates (if needed) and returns a directory under <see cref="Root"/>.</summary>
    public string CreateDirectory(params string[] parts)
    {
        string path = Combine(parts);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>Writes a text file under <see cref="Root"/> and returns its full path.</summary>
    public string CreateFile(string relativePath, string content)
    {
        string path = Combine(relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>Writes a binary file under <see cref="Root"/> and returns its full path.</summary>
    public string CreateFile(string relativePath, byte[] content)
    {
        string path = Combine(relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, content);
        return path;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A failed cleanup must not fail a test run.
        }
    }
}
