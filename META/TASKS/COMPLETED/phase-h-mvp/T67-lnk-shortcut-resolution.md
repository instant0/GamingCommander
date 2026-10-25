# Task T67: .lnk Shortcut Exe Resolution

**Tier:** 3 — Logic/Behavior
**Phase:** H — MVP
**Effort:** ~30 min
**Risk:** Low
**Status:** Complete
**Prerequisites:** None
**WP:** WP-3 (3.3)

---

## Objective

GOG Galaxy and some standalone installers place `.lnk` shortcuts in the game root instead of (or alongside) the actual `.exe`. The C# scanner detects `.lnk` existence (line 257-265) but never parses them, so games with shortcut-only roots get no executable. Port the `.lnk` binary parsing from `detect.py` `_parse_lnk_exe_name` and `_find_exe_via_lnk`.

**Key insight:** `.lnk` files contain the full exe name (and often the directory path). We just need to extract the exe filename and search for it — much simpler than a wildcard `*` search.

**Fallback:** If byte-parsing `.lnk` files proves unreliable across Windows versions, ship without `.lnk` support and document the limitation in TECH_DEBT.md. The GOG `.info` parser (T65) covers the majority of GOG cases.

## Design Principles

### 1. LNK files are simple binary blobs

The `.lnk` format stores the target path as a readable string embedded in the binary data. We don't need full COM interop (`IShellLink`) — just:
- Read bytes → latin-1 decode → regex `([A-Za-z0-9_\-\.]+\.exe)` → pick longest candidate

### 2. LNK often contains the full path, not just the filename

A `.lnk` might contain `D:\Games\GOG Galaxy\Games\Penumbra\Binaries\Penumbra.exe`. We extract `Penumbra.exe` and search for it — we don't need to parse the full path.

### 3. Targeted search, not wildcard

Since we know the exe name from the `.lnk`, we search for that specific file in subfolders (up to 3 levels). This is fast and precise — no `*.exe` wildcard scanning.

### 4. Backup renames are common

GOG and users sometimes rename exes: `-Penumbra.exe`, `copy of Penumbra.exe`, `Penumbra (backup).exe`. The stem-based fuzzy match catches these.

---

## What Needs to Change

### 1. New file: `src/GamingCommander.App/Services/LnkParser.cs`

**Current state:** Does not exist.

**Implementation (~80 lines):**

```csharp
namespace GamingCommander.App.Services;

/// <summary>
/// Parses Windows .lnk shortcut files to extract target executable names.
/// Uses binary decoding (latin-1) + regex instead of COM interop for cross-platform safety.
/// </summary>
internal static class LnkParser
{
    // DLLs and patterns to skip (not real game exes)
    private static readonly HashSet<string> s_skipExeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "steam_api.dll", "steam_api64.dll", "eos.dll", "upc.dll",
    };

    /// <summary>
    /// Extracts the target .exe filename from a .lnk shortcut file.
    /// Returns true if a valid exe name was found.
    /// </summary>
    internal static bool TryGetExeName(string lnkPath, out string? exeName)
    {
        exeName = null;
        try
        {
            byte[] data = File.ReadAllBytes(lnkPath);
            string text = Encoding.Latin1.GetString(data); // .lnk uses legacy encoding
            var matches = Regex.Matches(text, @"([A-Za-z0-9_\-\.]+\.exe)", RegexOptions.IgnoreCase);
            if (matches.Count == 0) return false;

            // Pick longest candidate (most likely the real game exe)
            string? best = null;
            foreach (Match m in matches)
            {
                string candidate = m.Value;
                if (s_skipExeNames.Contains(candidate)) continue;
                if (best is null || candidate.Length > best.Length)
                    best = candidate;
            }

            exeName = best;
            return best is not null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Resolves the actual .exe path from .lnk files in the game root.
    /// Searches subdirectories up to maxDepth for the target exe.
    /// Handles backup renames (-Name.exe, "copy of Name.exe").
    /// Returns the resolved exe path, or null if not found.
    /// </summary>
    internal static string? ResolveExeFromLnk(DirectoryInfo gameDir, int maxDepth = 3)
    {
        try
        {
            foreach (string lnkPath in Directory.EnumerateFiles(gameDir.FullName, "*.lnk", SearchOption.TopDirectoryOnly))
            {
                if (!TryGetExeName(lnkPath, out string? exeName) || exeName is null)
                    continue;

                string exeLower = exeName.ToLowerInvariant();
                string exeStem = exeLower[..exeLower.LastIndexOf('.')]; // e.g., "penumbra"

                // Search subdirs for the exe (exact match first, then fuzzy)
                string? fuzzyMatch = null;
                foreach (string exePath in FindExesInSubdirs(gameDir, exeName, maxDepth))
                {
                    string foundName = Path.GetFileName(exePath).ToLowerInvariant();
                    if (foundName == exeLower)
                        return exePath; // Exact match — return immediately

                    // Fuzzy: backup renames
                    if (fuzzyMatch is null)
                    {
                        if (foundName.StartsWith("-") && foundName[1..] == exeLower)
                            fuzzyMatch = exePath;
                        else if (foundName.StartsWith("copy of ") && foundName[8..] == exeLower)
                            fuzzyMatch = exePath;
                        else if (exeStem.Length > 2 && foundName.Contains(exeStem) && foundName.EndsWith(".exe"))
                            fuzzyMatch = exePath;
                    }
                }

                if (fuzzyMatch is not null)
                    return fuzzyMatch;
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// Searches for a specific exe by name in subdirectories up to maxDepth.
    /// Does NOT use wildcard scanning — targets the known exe name.
    /// </summary>
    private static IEnumerable<string> FindExesInSubdirs(
        DirectoryInfo root, string exeName, int maxDepth, int depth = 0)
    {
        if (depth > maxDepth) yield break;

        foreach (DirectoryInfo child in FileSystemHelper.GetDirectoriesSafe(root.FullName))
        {
            if (FileSystemHelper.NoiseSubDirNames.Contains(child.Name))
                continue;

            // Check for the exe in this directory
            string targetPath = Path.Combine(child.FullName, exeName);
            if (File.Exists(targetPath))
                yield return targetPath;

            // Also check case-insensitive variant
            foreach (string file in Directory.EnumerateFiles(child.FullName, "*.exe", SearchOption.TopDirectoryOnly))
            {
                if (Path.GetFileName(file).Equals(exeName, StringComparison.OrdinalIgnoreCase))
                    yield return file;
            }

            // Recurse
            foreach (string found in FindExesInSubdirs(child, exeName, maxDepth, depth + 1))
                yield return found;
        }
    }
}
```

### 2. `FolderScanner.cs` — `AddGameEntry()` — LNK fallback

**Current state:** Line 337-338 runs `ExecutableDiscovery.FindPrimaryExecutable()`. If it returns null, no exe.

**Change:** After primary exe discovery fails, try .lnk resolution:

```csharp
// Line 337-338, after FindPrimaryExecutable:
string? exePath = ExecutableDiscovery.FindPrimaryExecutable(
    subDir, exeFiles, _noiseExePatterns, _noiseDirectoryPatterns, _launcherPatterns, GetExePatternTier);

// NEW: LNK fallback — if no exe found, try resolving from .lnk shortcuts
if (string.IsNullOrEmpty(exePath))
{
    exePath = LnkParser.ResolveExeFromLnk(subDir);
}
```

This is a **3-line change** in `AddGameEntry()`. The `.lnk` resolution is a fallback only — it triggers when primary exe discovery fails.

---

## Context

- **Reference:** `detect.py` lines 269-328 (`_parse_lnk_exe_name`, `_find_exe_via_lnk`)
- `.lnk` binary format: header + variable-length string data containing the target path
- Latin-1 decode: `.lnk` files use legacy encoding, not UTF-8
- Regex: `([A-Za-z0-9_\-\.]+\.exe)` — captures exe filenames from the binary blob
- Skip list: `steam_api.dll`, `steam_api64.dll`, `eos.dll`, `upc.dll` — DLLs that appear in .lnk but aren't game exes
- Backup renames: `-Penumbra.exe` matches `PENUMBRA.EXE` (stem-based fuzzy match)
- `maxDepth: 3` matches Python behavior (searches all subdirs, 3 levels deep)
- GOG .info parser (T65) covers most GOG cases; .lnk is a fallback for edge cases

---

## Requirements

- [ ] `LnkParser` class created with XML docs
- [ ] `.lnk` binary parsing extracts exe name via latin-1 decode + regex
- [ ] Known DLLs/misleading patterns filtered
- [ ] Exe resolution walks subdirs up to 3 levels (targeted search, not wildcard)
- [ ] Backup renames handled (stem matching: `-Name.exe`, `copy of Name.exe`, containment)
- [ ] Exact match preferred over fuzzy match
- [ ] `FolderScanner.AddGameEntry()` uses `.lnk` as fallback when primary exe not found
- [ ] Existing scanner tests still pass
- [ ] Graceful fallback: if `.lnk` parsing fails, no crash, just no exe

---

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (no regressions)
- [ ] Unit test: synthetic `.lnk` bytes with embedded exe name → extracted correctly
- [ ] Unit test: temp dir tree with shortcut pointing to nested exe → resolved
- [ ] Unit test: backup rename `-Game.exe` → fuzzy matched
- [ ] Unit test: malformed `.lnk` → returns null, no crash
- [ ] Unit test: `.lnk` with multiple candidates → picks longest (real game exe)

---

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
