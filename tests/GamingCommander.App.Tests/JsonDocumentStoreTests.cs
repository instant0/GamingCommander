using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GamingCommander.App.Services;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Tests;

/// <summary>
/// Tests for the shared JSON store: four read states, one rolling backup,
/// <c>.corrupt</c> preservation, and write verification.
/// </summary>
public sealed class JsonDocumentStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _path;

    public JsonDocumentStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "JsonStoreTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _path = Path.Combine(_tempDir, "games.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    // ════════════════════════════════════════════════════════════════
    //  Read states
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Read_MissingFile_ReturnsMissingDefaults_AndCreatesNothing()
    {
        StoreReadResult<Doc> result = CreateStore().Read(Defaults);

        Assert.Equal(StoreReadStatus.Missing, result.Status);
        Assert.Equal("default", result.Value!.Name);
        Assert.Null(result.Message);
        Assert.Null(result.PreservedPath);
        Assert.Empty(Directory.GetFiles(_tempDir));
    }

    [Fact]
    public void WriteThenRead_RoundTrips()
    {
        JsonDocumentStore<Doc> store = CreateStore();
        store.Write(new Doc { Name = "first", Items = ["a", "b"] });

        StoreReadResult<Doc> result = store.Read(Defaults);

        Assert.Equal(StoreReadStatus.Ok, result.Status);
        Assert.Equal("first", result.Value!.Name);
        Assert.Equal(["a", "b"], result.Value.Items);
        Assert.Null(result.Message);
    }

    [Fact]
    public void Read_CorruptWithBackup_RecoversBackupAndPreservesDamagedBytes()
    {
        JsonDocumentStore<Doc> store = CreateStore();
        store.Write(new Doc { Name = "first" });
        store.Write(new Doc { Name = "second" }); // .bak now holds "first"

        byte[] damaged = Encoding.UTF8.GetBytes("not valid json {{{");
        File.WriteAllBytes(_path, damaged);

        var warnings = new List<string>();
        StoreReadResult<Doc> result = CreateStore(warnings.Add).Read(Defaults);

        Assert.Equal(StoreReadStatus.Corrupt, result.Status);
        Assert.Equal("first", result.Value!.Name);
        Assert.Equal(_path + ".corrupt", result.PreservedPath);
        Assert.Equal(damaged, File.ReadAllBytes(_path + ".corrupt"));
        Assert.Equal("first", ReadDoc(_path).Name);
        Assert.Single(warnings);
        Assert.Equal(result.Message, warnings[0]);
        Assert.Contains("games.json", result.Message);
        Assert.Contains("games.json.corrupt", result.Message);
        Assert.Contains("games.json.bak", result.Message);
    }

    [Fact]
    public void Read_CorruptWithoutBackup_ReturnsDefaultsAndKeepsDamagedFile()
    {
        byte[] damaged = Encoding.UTF8.GetBytes("not valid json {{{");
        File.WriteAllBytes(_path, damaged);

        StoreReadResult<Doc> result = CreateStore().Read(Defaults);

        Assert.Equal(StoreReadStatus.Corrupt, result.Status);
        Assert.Equal("default", result.Value!.Name);
        Assert.True(File.Exists(_path + ".corrupt"));
        Assert.Equal(damaged, File.ReadAllBytes(_path + ".corrupt"));
        Assert.Equal(damaged, File.ReadAllBytes(_path));
    }

    [Fact]
    public void Read_ZeroByteFile_IsCorruptNotEmptyMissing()
    {
        File.WriteAllBytes(_path, []);

        StoreReadResult<Doc> result = CreateStore().Read(Defaults);

        Assert.Equal(StoreReadStatus.Corrupt, result.Status);
        Assert.Equal("default", result.Value!.Name);
        Assert.True(File.Exists(_path + ".corrupt"));
        Assert.Equal(0, new FileInfo(_path + ".corrupt").Length);
    }

    [Fact]
    public void Read_TruncatedFileWithBackup_RecoversPreviousCopy()
    {
        JsonDocumentStore<Doc> store = CreateStore();
        store.Write(new Doc { Name = "first" });
        store.Write(new Doc { Name = "second" });

        File.WriteAllText(_path, """{"Version":1,"Name":"sec""");

        StoreReadResult<Doc> result = CreateStore().Read(Defaults);

        Assert.Equal(StoreReadStatus.Corrupt, result.Status);
        Assert.Equal("first", result.Value!.Name);
        Assert.Equal("first", ReadDoc(_path).Name);
    }

    [Fact]
    public void Read_CorruptWithUnreadableBackup_ReturnsDefaults()
    {
        JsonDocumentStore<Doc> store = CreateStore();
        store.Write(new Doc { Name = "first" });
        store.Write(new Doc { Name = "second" });

        File.WriteAllText(_path, "broken");
        File.WriteAllText(_path + ".bak", "also broken");

        StoreReadResult<Doc> result = CreateStore().Read(Defaults);

        Assert.Equal(StoreReadStatus.Corrupt, result.Status);
        Assert.Equal("default", result.Value!.Name);
        Assert.True(File.Exists(_path + ".corrupt"));
    }

    [Fact]
    public void Read_NewerVersion_IsUnreadableAndLeavesFileUntouched()
    {
        byte[] future = JsonSerializer.SerializeToUtf8Bytes(new Doc
        {
            Version = SchemaVersioning.Current + 1,
            Name = "future",
        });
        File.WriteAllBytes(_path, future);

        var warnings = new List<string>();
        StoreReadResult<Doc> result = CreateStore(warnings.Add)
            .Read(Defaults, dto => SchemaVersioning.IsReadable(dto.Version));

        Assert.Equal(StoreReadStatus.Unreadable, result.Status);
        Assert.Equal("default", result.Value!.Name);
        Assert.Equal(future, File.ReadAllBytes(_path));
        Assert.Null(result.PreservedPath);
        Assert.Single(warnings);
        Assert.Equal(result.Message, warnings[0]);
    }

    // ════════════════════════════════════════════════════════════════
    //  Write path
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Write_SecondSave_BacksUpPreviousBytes_AndKeepsOneBackup()
    {
        JsonDocumentStore<Doc> store = CreateStore();
        store.Write(new Doc { Name = "first" });
        store.Write(new Doc { Name = "second" });

        Assert.True(File.Exists(_path + ".bak"));
        Assert.Equal("first", ReadDoc(_path + ".bak").Name);
        Assert.Equal("second", ReadDoc(_path).Name);

        string[] files = Directory.GetFiles(_tempDir)
            .Select(path => Path.GetFileName(path)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["games.json", "games.json.bak"], files);
    }

    [Fact]
    public void Write_WhenVerificationFails_RestoresBackupAndThrows()
    {
        JsonDocumentStore<Doc> store = CreateStore();
        store.Write(new Doc { Name = "first" });

        var failingOptions = new JsonSerializerOptions { WriteIndented = true };
        failingOptions.Converters.Add(new FailingReadConverter());
        JsonDocumentStore<Doc> failingStore = CreateStore(options: failingOptions);

        Assert.Throws<InvalidOperationException>(() => failingStore.Write(new Doc { Name = "second" }));

        Assert.Equal("first", ReadDoc(_path).Name);
        Assert.Equal("first", ReadDoc(_path + ".bak").Name);
    }

    [Fact]
    public void Write_ToExistingParentOutsideAppRoot_Succeeds()
    {
        // Test-owned temp directories already exist: the guard only fires when a parent is missing.
        var store = new JsonDocumentStore<Doc>(_path);
        store.Write(new Doc { Name = "temp" });

        Assert.Equal("temp", ReadDoc(_path).Name);
    }

    // ── Helpers ────────────────────────────────────────────────────

    private JsonDocumentStore<Doc> CreateStore(
        Action<string>? warningSink = null,
        JsonSerializerOptions? options = null) =>
        new(_path, warningSink, options);

    private static Doc Defaults() => new() { Name = "default", Items = [] };

    private static Doc ReadDoc(string path) =>
        JsonSerializer.Deserialize<Doc>(File.ReadAllText(path))!;

    private sealed class Doc
    {
        public int Version { get; set; } = SchemaVersioning.Current;
        public string? Name { get; set; }
        public List<string>? Items { get; set; }
    }

    /// <summary>Serializes valid JSON but refuses to read it back, to force a verify failure.</summary>
    private sealed class FailingReadConverter : JsonConverter<Doc>
    {
        public override Doc Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new JsonException("injected read failure");

        public override void Write(Utf8JsonWriter writer, Doc value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("Version", value.Version);
            writer.WriteString("Name", value.Name);
            writer.WriteEndObject();
        }
    }
}
