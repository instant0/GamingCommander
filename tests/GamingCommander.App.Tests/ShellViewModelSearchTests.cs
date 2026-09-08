using GamingCommander.Core;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;
using GamingCommander.UI.ViewModels;

namespace GamingCommander.App.Tests;

/// <summary>
/// Plan 122 — type-to-search: silent capture, 3-character threshold,
/// live wildcard filtering across libraries (anchors), backspace/escape semantics.
/// </summary>
public sealed class ShellViewModelSearchTests
{
    private const string LibA = "LibA";
    private const string LibB = "LibB";
    private const string RootA = @"C:\LibA";
    private const string RootB = @"C:\LibB";

    [Fact]
    public void BelowThreshold_DoesNotFilter()
    {
        ShellViewModel vm = CreateViewModel();
        int rootItems = vm.Items.Count;

        vm.AppendSearchChar('a');
        vm.AppendSearchChar('s');

        Assert.Null(vm.ActiveFilter);
        Assert.Equal("as", vm.SearchText);
        Assert.True(vm.IsSearching);
        Assert.Equal(rootItems, vm.Items.Count); // still the roots view
        Assert.Contains("'as'", vm.LeftPaneTitle);
    }

    [Fact]
    public void ThirdChar_AppliesCrossLibraryWildcardFilter()
    {
        ShellViewModel vm = CreateViewModel();

        TypeText(vm, "assa");

        Assert.NotNull(vm.ActiveFilter);
        Assert.Equal(GameFilterKind.Wildcard, vm.ActiveFilter.Kind);
        Assert.Equal("assa", vm.ActiveFilter.Value);

        // Matches from both libraries, plus the leading ".." clear row.
        IReadOnlyList<ShellPaneItemViewModel> games = vm.Items.Skip(1).ToList();
        Assert.Contains(games, g => g.Title == "Assassin's Creed");
        Assert.Contains(games, g => g.Title == "Assault Zone");
        Assert.DoesNotContain(games, g => g.Title == "Doom");

        // Header shows the typed text while the search buffer lives.
        Assert.Contains("Search: 'assa'", vm.LeftPaneTitle);
    }

    [Fact]
    public void TagSubstring_Matches()
    {
        ShellViewModel vm = CreateViewModel();

        TypeText(vm, "coop");

        IReadOnlyList<ShellPaneItemViewModel> games = vm.Items.Skip(1).ToList();
        Assert.Contains(games, g => g.Title == "Coop Quest");
        Assert.DoesNotContain(games, g => g.Title == "Doom");
    }

    [Fact]
    public void TypingRefines_ResultsShrink()
    {
        ShellViewModel vm = CreateViewModel();
        TypeText(vm, "assa");
        Assert.Equal(2, vm.Items.Count - 1); // Assassin's Creed + Assault Zone

        TypeText(vm, "ssin"); // buffer becomes "assassin"

        IReadOnlyList<ShellPaneItemViewModel> games = vm.Items.Skip(1).ToList();
        Assert.Single(games);
        Assert.Equal("Assassin's Creed", games[0].Title);
        Assert.Equal("assassin", vm.ActiveFilter?.Value);
        Assert.Equal(0, vm.SelectedIndex); // selection resets on re-filter
    }

    [Fact]
    public void Backspace_ReFiltersWhileAtOrAboveThreshold()
    {
        ShellViewModel vm = CreateViewModel();
        TypeText(vm, "doom");
        Assert.Equal(1, vm.Items.Count - 1);

        Assert.True(vm.SearchBackspace()); // "doo" — still ≥ threshold
        Assert.Equal("doo", vm.ActiveFilter?.Value);
        Assert.Equal(1, vm.Items.Count - 1); // substring "doo" still matches Doom
    }

    [Fact]
    public void BackspaceBelowThreshold_EndsSearchAndReturnsToRoots()
    {
        ShellViewModel vm = CreateViewModel();
        int rootItems = vm.Items.Count;
        TypeText(vm, "abc");

        Assert.True(vm.SearchBackspace()); // drops to "ab" → below threshold

        Assert.Null(vm.ActiveFilter);
        Assert.Equal(string.Empty, vm.SearchText);
        Assert.False(vm.IsSearching);
        Assert.Equal(rootItems, vm.Items.Count);
        Assert.Equal("Library Roots", vm.LeftPaneTitle);
    }

    [Fact]
    public void Escape_CancelsActiveSearch_ButNavigatesUpWhenIdle()
    {
        ShellViewModel vm = CreateViewModel();
        Assert.False(vm.CancelSearch()); // no search in progress

        TypeText(vm, "assa");
        Assert.True(vm.CancelSearch());
        Assert.Null(vm.ActiveFilter);
        Assert.Equal(string.Empty, vm.SearchText);
        Assert.Equal("Library Roots", vm.LeftPaneTitle);
    }

    [Fact]
    public void Projection_LibraryDetailAndFilteredRows_ShareCoreSurface()
    {
        // Plan 125 Phase 1b: both loaders call ToPaneItem, so the library-detail
        // row and the cross-library filter row for the same game carry the same
        // core surface (Title/SourceLabel/Tags/StoreBadge/PlatformStatus).
        // LeftPath intentionally differs (exe filename vs library name).
        const string lib = "SteamLib";
        var libraries = new List<Library> { new(lib, GameSourceKind.Steam, [@"C:\SteamLib"]) };
        var steamGame = new GameEntry(
            Id: "s1",
            Library: lib,
            FolderPath: @"C:\SteamLib\common\HalfLife",
            FolderName: "HalfLife",
            DisplayName: "Half-Life",
            GameSource: GameSourceKind.Steam,
            IsSourceOverridden: false,
            ExecutablePath: @"C:\SteamLib\common\HalfLife\hl.exe",
            LauncherPath: string.Empty,
            CommandLineArguments: string.Empty,
            ManifestPath: string.Empty,
            LastScanned: DateTimeOffset.UtcNow,
            LastModified: DateTimeOffset.UtcNow,
            PlatformMetadata: new Dictionary<string, string>
            {
                ["SteamAppId"] = "70",
                ["SteamStatus"] = "Moved",
            },
            Tags: ["FPS"],
            UserOverrides: new Dictionary<string, string>());
        var games = new Dictionary<string, IReadOnlyList<GameEntry>> { [lib] = [steamGame] };

        // Library-detail row: drill into the anchor.
        ShellViewModel detailVm = CreateViewModel(libraries, games);
        detailVm.NavigateInto();
        Assert.False(detailVm.IsAtRootLevel);
        var detailRow = detailVm.Items.Single(i => i.Kind == FileSystemEntryKind.File);

        // Cross-library filter row: type-to-search (3-char threshold).
        ShellViewModel filterVm = CreateViewModel(libraries, games);
        TypeText(filterVm, "half");
        var filteredRow = filterVm.Items.Skip(1).Single(i => i.Kind == FileSystemEntryKind.File);

        Assert.Equal(detailRow.Title, filteredRow.Title);
        Assert.Equal(detailRow.SourceLabel, filteredRow.SourceLabel);
        Assert.Equal(detailRow.Tags, filteredRow.Tags);
        Assert.Equal(detailRow.StoreBadge?.Name, filteredRow.StoreBadge?.Name);
        Assert.Equal(detailRow.PlatformStatus, filteredRow.PlatformStatus);
        Assert.Equal(detailRow.LibraryName, filteredRow.LibraryName);

        // Detail-only extras pin the collapsed color switch (one computation).
        Assert.Equal("#E8C547", detailRow.PlatformStatusColor);
        Assert.Equal("#E8C547", detailRow.ItemStatusColor);
    }

    private static ShellViewModel CreateViewModel(
        IReadOnlyList<Library>? libraries = null,
        Dictionary<string, IReadOnlyList<GameEntry>>? gamesByLibrary = null)
    {
        var config = new AppConfig(
            HiddenFolders: [],
            IsFirstRun: false);

        libraries ??=
        [
            new(LibA, GameSourceKind.Standalone, [RootA]),
            new(LibB, GameSourceKind.Standalone, [RootB]),
        ];

        gamesByLibrary ??= new Dictionary<string, IReadOnlyList<GameEntry>>
        {
            [LibA] =
            [
                MakeGame("a1", LibA, "AssassinsCreed", "Assassin's Creed", ["Stealth"]),
                MakeGame("a2", LibA, "Doom", "Doom", ["FPS"]),
            ],
            [LibB] =
            [
                MakeGame("b1", LibB, "AssaultZone", "Assault Zone"),
                MakeGame("b2", LibB, "CoopQuest", "Coop Quest", ["COOP", "Party"]),
            ],
        };

        return new ShellViewModel(new FakeLibraryManager(libraries, gamesByLibrary), new FakeConfigService(config));
    }

    private static void TypeText(ShellViewModel vm, string text)
    {
        foreach (char c in text)
            vm.AppendSearchChar(c);
    }

    private static GameEntry MakeGame(
        string id,
        string library,
        string folder,
        string display,
        params string[] tags) =>
        new(
            Id: id,
            Library: library,
            FolderPath: $@"{RootA}\{folder}",
            FolderName: folder,
            DisplayName: display,
            GameSource: GameSourceKind.Standalone,
            IsSourceOverridden: false,
            ExecutablePath: $@"{RootA}\{folder}\game.exe",
            LauncherPath: string.Empty,
            CommandLineArguments: string.Empty,
            ManifestPath: string.Empty,
            LastScanned: DateTimeOffset.UtcNow,
            LastModified: DateTimeOffset.UtcNow,
            PlatformMetadata: new Dictionary<string, string>(),
            Tags: tags.ToList(),
            UserOverrides: new Dictionary<string, string>());

    private sealed class FakeConfigService(AppConfig config) : IConfigService
    {
        public AppConfig Load() => config;
        public void Save(AppConfig config) { }
    }

    private sealed class FakeLibraryManager(
        IReadOnlyList<Library> libraries,
        Dictionary<string, IReadOnlyList<GameEntry>> gamesByLibrary) : ILibraryManager
    {
        public IReadOnlyList<Library> Libraries => libraries;

        public IReadOnlyList<GameEntry> GetGamesForLibrary(string libraryName) =>
            gamesByLibrary.GetValueOrDefault(libraryName) ?? [];

        public void UpsertLibrary(string name, GameSourceKind type, IReadOnlyList<string> folders) { }
        public bool RemoveLibrary(string name) => true;
        public IReadOnlyList<GameEntry> ScanFolder(string folderPath, string libraryName, GameSourceKind type) => [];
        public void RescanLibrary(string libraryName, CancellationToken ct = default) { }
        public IReadOnlyList<GameEntry> SelectScannerAndScan(string folderPath, GameSourceKind type, CancellationToken ct = default) => [];
        public void UpdateGameEntry(GameEntry updatedEntry) { }
        public void DeleteGameEntry(string gameId) { }
        public void RetagGame(string gameId, GameSourceKind newType) { }
    }
}