using System.Text.RegularExpressions;
using GamingCommander.Core.Services;

namespace GamingCommander.App.Services;

/// <summary>
/// Scoring half of <see cref="ExecutableDiscovery"/> (Plan 125 Phase 2):
/// ranks exe candidates for primary selection. Kept in its own file so the
/// discovery/search/parsing half stays readable. Pure relocation from the
/// former single-file implementation — behavior, thresholds, and all
/// metadata keys are unchanged.
/// </summary>
internal static partial class ExecutableDiscovery
{
    /// <summary>
    /// Result of scoring an executable for primary selection.
    /// Contains the numeric score and the PE FileDescription (if read successfully).
    /// </summary>
    internal sealed record ExeScoreResult(int Score, string? FileDescription);

    internal static ExeScoreResult ScoreExecutable(
        string exePath,
        string folderName,
        IReadOnlyList<string> launcherPatterns,
        IReadOnlyList<string> noiseExePatterns,
        Func<string, int> tierLookup)
    {
        int score = 0;
        string name = Path.GetFileNameWithoutExtension(exePath).ToLowerInvariant();
        string folderLower = TitleText.ForSearch(folderName).ToLowerInvariant();
        string nameKey = TitleText.LettersAndDigits(name);
        string folderKey = TitleText.LettersAndDigits(folderLower);

        if (TitleText.MatchesFolderAndExe(folderName, Path.GetFileNameWithoutExtension(exePath)))
            score += 40;
        else if (folderKey.Length >= 3 && StripPlatformTokens(nameKey) == folderKey)
            score += 40; // platform-suffixed exact match (MyGame-Win64.exe): the suffix marks the real binary

        // Bonus for exe name containing folder name (+15)
        if (name.Contains(folderLower) || (folderKey.Length > 2 && nameKey == folderKey))
            score += 15;
        // Bonus for folder name containing exe stem (+15)
        else if (folderLower.Contains(name) || (nameKey.Length > 2 && folderKey.Contains(nameKey)))
            score += 15;

        // Folder name token match (+10 per matching token)
        char[] separators = [' ', '_', '-', '.', ':'];
        string[] folderTokens = folderLower.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in folderTokens)
        {
            if (token.Length > 1 && name.Contains(token))
                score += 10;
        }

        // Penalize launcher/updater/bootstrapper stubs (-20)
        if (launcherPatterns.Any(p => name.Contains(p)))
            score -= 20;

        // Penalize backup copies / cracks / org groups (org_ and 12-org-game)
        if (name.Contains("copy of") || name.Contains(" - copy"))
            score -= 25;
        else if (name.Contains("org_", StringComparison.Ordinal)
                 || name.Contains("-org-", StringComparison.Ordinal)
                 || name.Contains("-org_", StringComparison.Ordinal)
                 || name.StartsWith("org_", StringComparison.Ordinal)
                 || name.StartsWith("org-", StringComparison.Ordinal)
                 || Regex.IsMatch(name, @"^\d{1,3}-org"))
            score -= 25;
        else if (name.Contains("original"))
            score -= 15;
        if (name.Contains("crack"))
            score -= 25;

        // Classic main binary names (Silent Storm, many 2000s titles)
        if (name is "game" or "start" or "play")
            score += 18;

        score += RomanNumeralBonus(name, folderLower, folderTokens);
        score += AbbreviationBonus(name, folderLower);

        // Cache FileInfo.Length — avoid redundant filesystem syscalls (Plan 112 Step 4B)
        long fileSize = 0;
        try
        {
            fileSize = new FileInfo(exePath).Length;
            if (fileSize < 100_000) // < 100KB
                score -= 15;
        }
        catch { }

        // Penalize known noise patterns with tier-based severity
        bool isHighSeverityNoise = false;
        foreach (string pattern in noiseExePatterns)
        {
            if (name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                int tier = tierLookup(pattern);
                // Tier 1-5: -30 (universal noise, always non-game)
                // Tier 6-10: -20 (likely non-game)
                // Tier 11-15: -10 (possibly non-game)
                // Tier 16+: -5 (might be legitimate)
                int penalty = tier switch
                {
                    <= 5 => -30,
                    <= 10 => -20,
                    <= 15 => -10,
                    _ => -5
                };
                score += penalty;
                isHighSeverityNoise = tier <= 4;
                break; // Only penalize once (first match)
            }
        }

        // Unreal shipping binary beats root game.exe / launcher stubs
        string pathLower = exePath.Replace('/', '\\').ToLowerInvariant();
        bool shippingName = name.Contains("-win64-shipping")
            || name.Contains("-win32-shipping")
            || name.Contains("-wingdk-shipping")
            || name.EndsWith("-shipping", StringComparison.Ordinal);
        if (shippingName)
            score += 28;
        else if (name.Contains("shipping") || name.Contains("win64"))
            score += 5;

        if (pathLower.Contains(@"\binaries\win64\")
            || pathLower.Contains(@"\binaries\win32\")
            || pathLower.Contains(@"\binaries\wingdk\")
            || pathLower.Contains(@"\shipping\"))
            score += 12;

        // File size bonus: up to +5 for very large files (>= 100MB)
        // Uses cached fileSize from above (Plan 112 Step 4B)
        score += (int)Math.Min(fileSize / 20_000_000, 5);

        // PE metadata: skip read for confirmed-noise candidates (Plan 112 Step 4C).
        // A -30 penalty from Tier 1-4 noise cannot be rescued by PE metadata bonuses.
        string? fileDescription = null;
        if (!isHighSeverityNoise)
        {
            // PE metadata scoring: penalize noise patterns in Description/InternalName.
            // Uses System.Diagnostics.FileVersionInfo — built into .NET, no external dependencies.
            // Gracefully degrades on read failure (old/broken PE headers).
            try
            {
                var peInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(exePath);
                string desc = (peInfo.FileDescription ?? "").ToLowerInvariant();
                string internalName = (peInfo.InternalName ?? "").ToLowerInvariant();

                // Capture FileDescription for display name enrichment (Plan 112 Step 2)
                fileDescription = PeProductYear.IsUsefulTitle(peInfo.ProductName)
                    ? peInfo.ProductName!.Trim()
                    : PeProductYear.IsUsefulTitle(peInfo.FileDescription)
                        ? peInfo.FileDescription!.Trim()
                        : null;

                // Penalize noise in FileDescription (-25)
                if (desc.Contains("setup") || desc.Contains("microsoft") ||
                    desc.Contains("uninstall") || desc.Contains("redistributable") ||
                    desc.Contains("directx") || desc.Contains("cabinet"))
                    score -= 25;

                // Penalize noise in InternalName (-20)
                if (internalName == "setup" || internalName.Contains("launcher") ||
                    internalName.Contains("uninstall") || internalName.Contains("crash") ||
                    internalName.Contains("error"))
                    score -= 20;

                // Bonus for game-like descriptions (+10)
                if (desc.Contains("retail") || desc.Contains("client") ||
                    desc.Contains("shipping"))
                    score += 10;
            }
            catch
            {
                // PE read failed (broken header, old exe, etc.) — continue with existing score
            }
        }

        return new ExeScoreResult(score, fileDescription);
    }

    /// <summary>
    /// Plan 103: +12 when a folder digit token matches a roman numeral in the exe stem, or vice versa
    /// (e.g. folder "heroes 4" vs exe "heroesiv").
    /// </summary>
    private static int RomanNumeralBonus(string exeStem, string folderLower, string[] folderTokens)
    {
        foreach (var (digit, roman) in s_romanDigits)
        {
            bool folderHasDigit = folderTokens.Contains(digit);
            bool folderHasRoman = folderLower.Contains(roman);
            bool exeHasDigit = exeStem.Contains(digit);
            bool exeHasRoman = exeStem.Contains(roman);
            if ((folderHasDigit && exeHasRoman) || (folderHasRoman && exeHasDigit))
                return 12;
        }

        return 0;
    }

    /// <summary>
    /// Plan 103: +8 when a short exe stem (2–4 chars) is an ordered abbreviation of the folder name
    /// sharing the first letter (e.g. "hk" for "hollow knight").
    /// </summary>
    private static int AbbreviationBonus(string exeStem, string folderLower)
    {
        if (exeStem.Length is < 2 or > 4)
            return 0;
        if (folderLower.Length == 0 || exeStem[0] != folderLower[0])
            return 0;

        int fi = 0;
        foreach (char c in exeStem)
        {
            int found = folderLower.IndexOf(c, fi);
            if (found < 0)
                return 0;
            fi = found + 1;
        }

        return 8;
    }

    private static readonly (string Digit, string Roman)[] s_romanDigits =
    [
        ("2", "ii"), ("3", "iii"), ("4", "iv"), ("5", "v"),
        ("6", "vi"), ("7", "vii"), ("8", "viii"), ("9", "ix"),
    ];
}