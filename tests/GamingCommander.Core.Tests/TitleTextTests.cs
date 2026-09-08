using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.Core.Tests;

public sealed class TitleTextTests
{
    [Theory]
    [InlineData("Dark Souls® III", "Dark Souls III")]
    [InlineData("Dead Space™ 3", "Dead Space 3")]
    [InlineData("Foo© Bar", "Foo Bar")]
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
    [InlineData("ELEX", "ELEX")]
    public void MatchesFolderAndExe_GothicStyle(string folder, string exe) =>
        Assert.True(TitleText.MatchesFolderAndExe(folder, exe));

    [Fact]
    public void MatchesFolderAndExe_NotSystem() =>
        Assert.False(TitleText.MatchesFolderAndExe("elexII", "System"));

    [Fact]
    public void IsGenericLabel_System() =>
        Assert.True(TitleText.IsGenericLabel("System"));

    [Fact]
    public void StripEdition_Reloaded() =>
        Assert.Equal(
            "Dying Light 2 Stay Human",
            TitleText.StripEdition("Dying Light 2 Stay Human - Reloaded Edition"));

    [Fact]
    public void LookupName_EpicUsesItemDisplayName() =>
        Assert.Equal(
            "Sid Meier's Civilization VI",
            TitleText.LookupName("Sid Meier's Civilization VI", "SidMeiersCivilizationVI", GameSourceKind.Epic));

    [Fact]
    public void SharesNameToken_RejectsSystemForElex() =>
        Assert.False(TitleText.SharesNameToken("System", "elexII"));

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
    public void AcronymMatchesTitle_EveMatchesEveOnline()
    {
        // Rule (a): "eveonline" starts with "eve".
        Assert.True(TitleText.AcronymMatchesTitle("EVE Online", "eve"));
    }

    [Fact]
    public void AcronymMatchesTitle_Ra3MatchesRedAlert3Launcher()
    {
        // Rule (b): initials R-A-3 (launcher is a stopword) == "ra3".
        Assert.True(TitleText.AcronymMatchesTitle("Red Alert 3 Launcher", "ra3"));
    }

    [Fact]
    public void AcronymMatchesTitle_ToeeMatchesToEERemake()
    {
        // Rule (c): "toee" letters prefix "ToEE" first word, no digits.
        Assert.True(TitleText.AcronymMatchesTitle("ToEE Front-End X", "toee"));
    }

    [Fact]
    public void AcronymMatchesTitle_RejectsSystemForElexII()
    {
        // The original guard's protection must hold: elexII is 6 letters, out of
        // scope for the acronym rule (which requires 3-4).
        Assert.False(TitleText.AcronymMatchesTitle("System", "elexII"));
    }

    [Fact]
    public void AcronymMatchesTitle_RejectsJag2ForJustAnotherGenericGame2()
    {
        // False-positive guard: "Just Another Generic Game 2" initials are
        // J-A-G-G, not "jag2"; "jag" does not prefix "just".
        Assert.False(TitleText.AcronymMatchesTitle("Just Another Generic Game 2", "jag2"));
    }

    [Fact]
    public void AcronymMatchesTitle_RejectsMonsterMixXlForMmxl()
    {
        // "Monster Mix XL" initials M-M-X, not "mmxl"; "mmx" does not prefix "monster".
        Assert.False(TitleText.AcronymMatchesTitle("Monster Mix XL", "mmxl"));
    }

    [Fact]
    public void AcronymMatchesTitle_RejectsLongFolders()
    {
        // The acronym rule is scoped to 3-4 char folders only.
        Assert.False(TitleText.AcronymMatchesTitle("Neverwinter", "neverwinter_en"));
        Assert.False(TitleText.AcronymMatchesTitle("System", "system"));
    }
}
