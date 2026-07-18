# Task T21: Consolidate Noise-Check Methods in FolderScanner

**Tier:** 2 — Code Structure
**Phase:** D — Complexity Reduction
**Effort:** ~30 min
**Risk:** Low
**Status:** pending
**Prerequisites:** T16 (FileSystemHelper extracted)

---

## Objective

`FolderScanner.cs` has three overlapping noise-check methods that do essentially the same thing: `IsNoiseExePattern()` (static, hardcoded 25 patterns), `IsNoiseExeName()` (instance, full JSON blacklist), and `IsNonGameExe()` (instance, full JSON blacklist). `IsNoiseExeName` and `IsNonGameExe` are functionally identical. Consolidate to two clear methods: one static (for external callers) and one instance (for internal use).

## What Needs to Change

### `src/GamingCommander.App/Services/FolderScanner.cs`

**Current state:** Three methods that check if an exe is noise:
- `IsNoiseExePattern(string)` (line 601, static, uses `DefaultNoiseExePatterns`) — called by external test code
- `IsNoiseExeName(string)` (line 617, instance, uses `_noiseExePatterns`) — called by `HasRootExecutableSignal`, `HasUnrealLayoutSignal`
- `IsNonGameExe(string)` (line 622, instance, uses `_noiseExePatterns`) — called by `FindExecutablesDeep`, `ScoreExecutable`

**Actions:**
- [ ] **Rename** `IsNoiseExeName` to `IsNoiseExePattern` (instance version) — this replaces the current static version's name
- [ ] **Delete** the static `IsNoiseExePattern` (line 601) — it only uses `DefaultNoiseExePatterns` which is a subset of the full list
- [ ] **Rename** `IsNonGameExe` to `IsNoiseExeName` — this is the descriptive name
- [ ] **Verify** that `IsNoiseExeName` (formerly `IsNonGameExe`) has the same logic as `IsNoiseExeName` (the one being deleted)
  - Both extract filename, lowercase it, and check `Any(p => name.Contains(p))`
  - The difference: `IsNonGameExe` also handles the `.lnk` shortcut case — keep that logic
- [ ] **Update all call sites:**
  - `HasRootExecutableSignal()` (line ~380): was calling `IsNoiseExeName()` — now calls `IsNoiseExePattern()`
  - `HasUnrealLayoutSignal()` (line ~356): was calling `IsNoiseExeName()` — now calls `IsNoiseExePattern()`
  - `FindExecutablesDeep()` (line ~470): was calling `IsNonGameExe()` — now calls `IsNoiseExeName()`
  - `ScoreExecutable()` (line ~530): was calling `IsNonGameExe()` — now calls `IsNoiseExeName()`
- [ ] **Add XML docs** to both remaining methods:
  - `IsNoiseExePattern(string)`: "Checks if an executable name matches any noise pattern. Used by signal detection to filter non-game exes."
  - `IsNoiseExeName(string)`: "Checks if an executable name matches any noise pattern, including .lnk shortcut handling. Used by scoring and filtering."

### `src/GamingCommander.App/Services/SteamLibraryScanner.cs`

**Current state:** Line 414 has its own `IsNoiseExe(string)` static method with a hardcoded subset
**Actions:**
- [ ] **Leave as-is** — SteamLibraryScanner's `IsNoiseExe` is intentionally a smaller, Steam-specific subset
- [ ] Add `/// <summary>`: "Checks if an executable is a known Steam noise file (installer, uninstaller, etc.). Subset of the full noise list — Steam-specific."

## Context

- The three-method overlap was created when Bug 5 was fixed — `IsNoiseExePattern` was made instance, then `IsNonGameExe` was added separately
- After consolidation: `IsNoiseExePattern` = instance method using full `_noiseExePatterns`, `IsNoiseExeName` = instance method with .lnk handling
- The static version is removed — all callers now use the instance version
- `DefaultNoiseExePatterns` (the static list) is still used by the `FolderScanner()` no-arg constructor and test code — keep the field, just remove the static method

## Requirements

- [ ] Two noise-check methods remain in FolderScanner: `IsNoiseExePattern` and `IsNoiseExeName`
- [ ] No static `IsNoiseExePattern` method remains
- [ ] All call sites updated to use the correct method
- [ ] Both methods have `/// <summary>` XML docs
- [ ] No behavior change — same pattern matching logic

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -c "IsNoiseExePattern\|IsNoiseExeName\|IsNonGameExe" src/GamingCommander.App/Services/FolderScanner.cs` returns 2 (two method definitions)
- [ ] `grep -c "IsNonGameExe" src/` returns 0 (old name removed)

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
