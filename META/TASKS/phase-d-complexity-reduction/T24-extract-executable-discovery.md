# Task T24: Extract FolderScanner Executable Discovery

**Tier:** 2 — Code Structure
**Phase:** D — Complexity Reduction
**Effort:** ~40 min
**Risk:** Low
**Status:** pending
**Prerequisites:** T16 (FileSystemHelper), T21 (Noise check consolidation)

---

## Objective

`FolderScanner.cs` contains executable discovery logic (`FindExecutablesDeep`, `FindPrimaryExecutable`, `ScoreExecutable`) that is independent of the detection/scoring pipeline. Extract these to a dedicated class to reduce FolderScanner's size further and make the executable logic reusable.

## What Needs to Change

### 1. New file: `src/GamingCommander.App/Services/ExecutableDiscovery.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create `ExecutableDiscovery.cs` with namespace `GamingCommander.App.Services`
- [ ] Add `/// <summary>` to class: "Discovers and scores executable files within a game directory. Handles deep search, UE-aware paths, and primary exe selection."
- [ ] Move the following methods from `FolderScanner.cs`:
  - `FindExecutablesDeep(DirectoryInfo gameDir)` (lines 451-509) → `ExecutableDiscovery.FindExecutablesDeep(DirectoryInfo gameDir, IReadOnlySet<string> noiseExePatterns)`
  - `FindPrimaryExecutable(DirectoryInfo gameDir, string[] candidateExes)` (lines 549-570) → `ExecutableDiscovery.FindPrimaryExecutable(DirectoryInfo gameDir, string[] candidateExes, IReadOnlySet<string> noiseExePatterns)`
  - `ScoreExecutable(string exePath, string gameFolderName)` (lines 515-547) → `ExecutableDiscovery.ScoreExecutable(string exePath, string gameFolderName)`
  - `ExeNameMatchesFolderName(string exeName, string folderName)` (lines 572-597) → `ExecutableDiscovery.ExeNameMatchesFolderName(string exeName, string folderName)`
  - `FindEpicManifest(DirectoryInfo gameDir)` (lines 659-683) → `ExecutableDiscovery.FindEpicManifest(DirectoryInfo gameDir)`
- [ ] Update method signatures to accept `noiseExePatterns` parameter instead of using `_noiseExePatterns` field
- [ ] All methods become `internal static` (no state dependencies beyond parameters)
- [ ] Add `/// <summary>` XML docs to each method

### 2. `src/GamingCommander.App/Services/FolderScanner.cs`

**Current state:** Lines 451-597 contain executable discovery and scoring logic
**Actions:**
- [ ] Delete all 5 methods (lines 451-597)
- [ ] Update `Scan()` method to call `ExecutableDiscovery.FindExecutablesDeep(subDir, _noiseExePatterns)` and `ExecutableDiscovery.FindPrimaryExecutable(subDir, candidates, _noiseExePatterns)`
- [ ] Reduce FolderScanner from ~610 (after T23) to ~460 lines

## Context

- `FindExecutablesDeep` searches root, children, Binaries/Win64/, Binaries/WinGDK/ for non-noise .exe files
- `ScoreExecutable` scores candidates by folder-name match, launcher penalty, shipping bonus
- `FindPrimaryExecutable` picks the best exe from scored candidates
- `ExeNameMatchesFolderName` does bidirectional substring + token matching
- `FindEpicManifest` searches for .egstore/ manifests
- All are pure functions — they take inputs and return results, no mutable state
- The noise patterns are passed as a parameter instead of using the instance field

## Requirements

- [ ] `ExecutableDiscovery.cs` created with all 5 methods
- [ ] All methods have `/// <summary>` XML docs
- [ ] FolderScanner.cs no longer contains any of the 5 moved methods
- [ ] `FindExecutablesDeep` and `FindPrimaryExecutable` accept `noiseExePatterns` parameter
- [ ] No behavior change — same discovery and scoring logic
- [ ] `ExecutableDiscovery` class is `internal static`

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -c "FindExecutablesDeep\|FindPrimaryExecutable\|ScoreExecutable\|ExeNameMatchesFolderName\|FindEpicManifest" src/GamingCommander.App/Services/FolderScanner.cs` returns 0 (all moved)
- [ ] `grep -c "FindExecutablesDeep\|FindPrimaryExecutable\|ScoreExecutable\|ExeNameMatchesFolderName\|FindEpicManifest" src/GamingCommander.App/Services/ExecutableDiscovery.cs` returns 5 (all present)

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
