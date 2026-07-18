# Task T33: Fix ScoreExecutable to Use JSON Blacklist (Bug 7)

**Tier:** 3 — Logic/Behavior
**Phase:** E — Stabilization
**Effort:** ~30 min
**Risk:** Medium
**Status:** pending
**Prerequisites:** T29 (Blacklist tier preservation)

---

## Objective

`ScoreExecutable()` only penalizes ~10 hardcoded launcher patterns (like "launcher", "updater", "bootstrap"). JSON blacklist patterns like "patch", "activate", "trial", "config" are not penalized in scoring. This means non-game exes with these names can be incorrectly selected as the primary executable.

## What Needs to Change

### `src/GamingCommander.App/Services/FolderScanner.cs` (or `ExecutableDiscovery.cs` after T24)

**Current state:** `ScoreExecutable(string exePath, string gameFolderName)` only penalizes `_launcherPatterns` (hardcoded 10 patterns)
**Actions:**
- [ ] Update `ScoreExecutable` signature to accept `IReadOnlyList<string> noisePatterns` and `Func<string, int> tierLookup`:
  ```csharp
  internal static int ScoreExecutable(
      string exePath,
      string gameFolderName,
      IReadOnlyList<string> noisePatterns,
      Func<string, int> tierLookup)
  ```
- [ ] Add scoring logic for noise patterns:
  ```csharp
  // Penalize known noise patterns
  string exeNameLower = Path.GetFileNameWithoutExtension(exePath).ToLowerInvariant();
  foreach (string pattern in noisePatterns)
  {
      if (exeNameLower.Contains(pattern, StringComparison.OrdinalIgnoreCase))
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
          break; // Only penalize once (first match)
      }
  }
  ```
- [ ] Keep existing launcher pattern penalty as well (it's a superset check)
- [ ] Update call site to pass noise patterns and tier lookup:
  ```csharp
  int score = ExecutableDiscovery.ScoreExecutable(
      exePath,
      gameFolderName,
      _noiseExePatterns,
      GetExePatternTier);
  ```

## Context

- Bug 6 (T29) provides the tier information needed for scaled penalties
- The current hardcoded launcher patterns are a subset of the JSON blacklist
- Tier 1-5 patterns are universal noise (unins000, setup, installer) — should be heavily penalized
- Tier 16+ patterns are store bootstraps (epicgameslauncher) — sometimes legitimate, lighter penalty
- This fix makes primary exe selection more accurate across all noise patterns

## Requirements

- [ ] `ScoreExecutable` accepts noise patterns and tier lookup parameters
- [ ] Scoring logic applies tier-based penalties for noise patterns
- [ ] Existing launcher pattern penalty is preserved
- [ ] Call site updated to pass the new parameters
- [ ] No regression in existing test cases

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -c "tierLookup\|GetExePatternTier" src/` returns 2+ (definition + usage)
- [ ] Manual test: Run `dotnet run --project src/GamingCommander.App` and verify primary exe selection is correct

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
