using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Services;

/// <summary>
/// JSON-file implementation of ILibrariesService. Reads/writes libraries.json.
/// Structure: { version, libraries: { name: { type, folders[] } } }.
/// </summary>
public sealed class LibrariesDatabaseService : ILibrariesService
{
    private readonly string _librariesPath;

    public LibrariesDatabaseService(string librariesPath)
    {
        _librariesPath = librariesPath;
    }

    /// <summary>All configured libraries, read live from disk.</summary>
    public IReadOnlyList<Library> Libraries
    {
        get
        {
            LibrariesDto? dto = JsonFileHelper.ReadFromFile<LibrariesDto>(
                _librariesPath,
                () => new LibrariesDto());
            if (dto?.Libraries is null)
                return [];

            return dto.Libraries
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Key))
                .Select(kv => new Library(
                    Name: kv.Key,
                    Type: kv.Value.Type,
                    Folders: kv.Value.Folders ?? []))
                .ToList();
        }
    }

    /// <summary>Adds or replaces a library by name.</summary>
    public void Upsert(Library library)
    {
        LibrariesDto dto = ReadDto();
        dto.Libraries ??= new Dictionary<string, LibraryDto>();
        dto.Libraries[library.Name] = new LibraryDto
        {
            Type = library.Type,
            Folders = library.Folders.ToList(),
        };
        Write(dto);
    }

    /// <summary>Removes a library by name. Returns true if it was removed.</summary>
    public bool Remove(string name)
    {
        LibrariesDto dto = ReadDto();
        if (dto.Libraries is null || !dto.Libraries.Remove(name))
            return false;
        Write(dto);
        return true;
    }

    /// <summary>Gets a library by name, or null.</summary>
    public Library? Get(string name)
    {
        LibrariesDto? dto = JsonFileHelper.ReadFromFile<LibrariesDto>(
            _librariesPath,
            () => new LibrariesDto());
        if (dto?.Libraries is null || !dto.Libraries.TryGetValue(name, out LibraryDto? lib) || lib is null)
            return null;
        return new Library(name, lib.Type, lib.Folders ?? []);
    }

    private LibrariesDto ReadDto()
    {
        return JsonFileHelper.ReadFromFile<LibrariesDto>(
            _librariesPath,
            () => new LibrariesDto()) ?? new LibrariesDto();
    }

    private void Write(LibrariesDto dto)
    {
        JsonFileHelper.WriteToFile(_librariesPath, dto);
    }

    private sealed class LibrariesDto
    {
        public int Version { get; set; } = 1;
        public Dictionary<string, LibraryDto>? Libraries { get; set; }
    }

    private sealed class LibraryDto
    {
        public GameSourceKind Type { get; set; }
        public List<string>? Folders { get; set; }
    }
}
