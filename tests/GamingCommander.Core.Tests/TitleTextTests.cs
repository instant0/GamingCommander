using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

public sealed class TitleTextTests
{
    [Theory]
    [InlineData("Dark Souls® III", "Dark Souls III")]
    public void ForSearch_StripsMarks(string raw, string expected) =>
        Assert.Equal(expected, TitleText.ForSearch(raw));

    [Fact]
    public void LettersAndDigits_MatchesConcatenatedExe() =>
        Assert.Equal("darksoulsiii", TitleText.LettersAndDigits("Dark Souls® III"));

    [Fact]
    public void ExpandPacked_SplitsCamelCase() =>
        Assert.Equal("Deep Rock", TitleText.ExpandPacked("DeepRock"));

    [Fact]
    public void FromFolderName_ElexIi() =>
        Assert.Equal("elex II", TitleText.FromFolderName("elexII"));

    [Theory]
    [InlineData("elex", "elex")]
    [InlineData("elexII", "ELEX2")]
    public void MatchesFolderAndExe_GothicStyle(string folder, string exe) =>
        Assert.True(TitleText.MatchesFolderAndExe(folder, exe));

    [Fact]
    public void MatchesFolderAndExe_NotSystem()
    {
        // "System" is a generic label, does not share a name token, and is not a match.
        Assert.True(TitleText.IsGenericLabel("System"));
        Assert.False(TitleText.SharesNameToken("System", "elexII"));
        Assert.False(TitleText.MatchesFolderAndExe("elexII", "System"));
    }

    [Fact]
    public void StripEdition_StripsTailTokenAndSuffixes()
    {
        const string title = "Dying Light 2 Stay Human";

        Assert.Equal(title, TitleText.StripEdition($"{title} - Reloaded Edition"));
        Assert.Equal(title, TitleText.StripEdition($"{title} - Reloaded"));
        Assert.Equal(title, TitleText.StripEdition($"{title}: Reloaded"));
    }

    [Theory]
    [InlineData("Reloaded - Dying Light 2 Stay Human")]
    [InlineData("Dying Light 2 Stay Human - Reloaded Edition - Stay Human")]
    public void StripEdition_LeavesMidTitleToken(string input) =>
        Assert.Equal(input, TitleText.StripEdition(input));

    [Theory]
    [InlineData("Dying Light 2 Stay Human - Reloaded Edition")]
    [InlineData("Dying Light 2 Stay Human")]
    public void StripEdition_IsIdempotent(string input)
    {
        string once = TitleText.StripEdition(input);
        Assert.Equal(once, TitleText.StripEdition(once));
    }

    [Theory]
    [InlineData(GameSourceKind.Steam)]
    [InlineData(GameSourceKind.Epic)]
    public void LookupName_StoreTitleStillResolves(GameSourceKind source)
    {
        // Store-sourced display titles are used verbatim (TitleSource contract);
        // edition stripping only supplies an extra search variant.
        const string display = "Dying Light 2 Stay Human - Reloaded Edition";

        Assert.Equal(display, TitleText.LookupName(display, "DyingLight2", source));
        Assert.Contains("Dying Light 2 Stay Human", TitleText.SearchQueries(display, "DyingLight2"));
    }

    [Fact]
    public void SearchQueries_IncludesFolderAndPacked()
    {
        IReadOnlyList<string> q = TitleText.SearchQueries("FSD", "deeprock", "Deep Rock Galactic");
        Assert.Contains("Deep Rock Galactic", q);
        Assert.Contains("deeprock", q);
    }

    // ════════════════════════════════════════════════════════════════
    //  AcronymMatchesTitle (E6 acronym relaxation, 2026-09-07)
    //  Validated against real corpus PE data (/mnt/d).
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void AcronymMatchesTitle_Jag2MatchesJaggedAlliance2Gold()
    {
        // Real S9 case: jag2 folder + ja2.exe PE "Jagged Alliance 2 Gold".
        // Rule (c): "jag" prefixes "Jagged", digit 2 appears in the title.
        Assert.True(TitleText.AcronymMatchesTitle("Jagged Alliance 2 Gold", "jag2"));
    }

    [Fact]
    public void AcronymMatchesTitle_MmxlMatchesMightAndMagicXLegacy()
    {
        // Real S5 case: mmxl + "Might and Magic X Legacy".
        // Rule (b): initials M-M-X-L == "mmxl".
        Assert.True(TitleText.AcronymMatchesTitle("Might and Magic X Legacy", "mmxl"));
    }

    [Fact]
    public void AcronymMatchesTitle_NierMatchesNierAutomata()
    {
        // Rule (a): title key starts with "nier".
        Assert.True(TitleText.AcronymMatchesTitle("NieR:Automata", "nier"));
    }

    [Fact]
    public void AcronymMatchesTitle_Ra3MatchesRedAlert3Launcher()
    {
        // Rule (b): initials R-A-3 (launcher is a stopword) == "ra3".
        Assert.True(TitleText.AcronymMatchesTitle("Red Alert 3 Launcher", "ra3"));
    }

    [Theory]
    [InlineData("Just Another Generic Game 2", "jag2")]
    [InlineData("Monster Mix XL", "mmxl")]
    public void AcronymMatchesTitle_RejectsFalsePositives(string title, string folder)
    {
        // "Just Another Generic Game 2" initials are J-A-G-G, not "jag2";
        // "Monster Mix XL" initials M-M-X, not "mmxl"; neither prefix rule applies.
        Assert.False(TitleText.AcronymMatchesTitle(title, folder));
    }

    [Fact]
    public void AcronymMatchesTitle_RejectsSystemForElexII()
    {
        // The original guard's protection must hold: elexII is 6 letters, out of
        // scope for the acronym rule (which requires 3-4).
        Assert.False(TitleText.AcronymMatchesTitle("System", "elexII"));
    }

    [Fact]
    public void AcronymMatchesTitle_RejectsLongFolders()
    {
        // The acronym rule is scoped to 3-4 char folders only.
        Assert.False(TitleText.AcronymMatchesTitle("Neverwinter", "neverwinter_en"));
        Assert.False(TitleText.AcronymMatchesTitle("System", "system"));
    }
}
