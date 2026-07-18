# Task T23: Extract FolderScanner Signal Detection

**Tier:** 2 — Code Structure
**Phase:** D — Complexity Reduction
**Effort:** ~40 min
**Risk:** Low
**Status:** pending
**Prerequisites:** T16 (FileSystemHelper), T21 (Noise check consolidation)

---

## Objective

`FolderScanner.cs` (749 lines) contains 10 store-signal detection methods (`HasGogSignal`, `HasEaSignal`, `HasUbisoftSignal`, etc.) plus the `DetectType()` dispatcher. These are pure detection logic with no state dependencies. Extract them to a dedicated class to reduce FolderScanner's size and improve separation of concerns.

## What Needs to Change

### 1. New file: `src/GamingCommander.App/Services/StoreSignalDetector.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create `StoreSignalDetector.cs` with namespace `GamingCommander.App.Services`
- [ ] Add `/// <summary>` to class: "Detects game store/platform type from filesystem signals in a game folder. Returns the detected GameSourceKind or Unknown."
- [ ] Move the following methods from `FolderScanner.cs`:
  - `DetectType(DirectoryInfo)` (lines 139-188) → `StoreSignalDetector.DetectType(DirectoryInfo)`
  - `HasGogSignal(DirectoryInfo)` (line 192) → `StoreSignalDetector.HasGogSignal(DirectoryInfo)`
  - `HasEaSignal(DirectoryInfo)` (line 197) → `StoreSignalDetector.HasEaSignal(DirectoryInfo)`
  - `HasUbisoftEmulatorSignal(DirectoryInfo)` (line 202) → `StoreSignalDetector.HasUbisoftEmulatorSignal(DirectoryInfo)`
  - `HasUbisoftSignal(DirectoryInfo)` (line 230) → `StoreSignalDetector.HasUbisoftSignal(DirectoryInfo)`
  - `HasEpicSignal(DirectoryInfo)` (line 248) → `StoreSignalDetector.HasEpicSignal(DirectoryInfo)`
  - `HasBlizzardSignal(DirectoryInfo)` (line 254) → `StoreSignalDetector.HasBlizzardSignal(DirectoryInfo)`
  - `HasXboxSignal(DirectoryInfo)` (line 259) → `StoreSignalDetector.HasXboxSignal(DirectoryInfo)`
  - `HasRockstarSignal(DirectoryInfo)` (line 264) → `StoreSignalDetector.HasRockstarSignal(DirectoryInfo)`
  - `HasSteamSignal(DirectoryInfo)` (line 269) → `StoreSignalDetector.HasSteamSignal(DirectoryInfo)`
  - `HasSteamEmulatorSignal(DirectoryInfo)` (line 274) → `StoreSignalDetector.HasSteamEmulatorSignal(DirectoryInfo)`
- [ ] All methods stay `internal static` (no state dependencies)
- [ ] Add `/// <summary>` XML docs to each signal method explaining what filesystem markers it checks

### 2. `src/GamingCommander.App/Services/FolderScanner.cs`

**Current state:** Lines 139-278 contain `DetectType` and all 10 signal helpers
**Actions:**
- [ ] Delete all 11 methods (lines 139-278)
- [ ] Update `Scan()` method (line ~107) to call `StoreSignalDetector.DetectType(subDir)` instead of `DetectType(subDir)`
- [ ] Reduce FolderScanner from ~749 to ~610 lines

## Context

- All signal methods are `private static` with no dependencies on FolderScanner state
- They only use `Directory.Exists()`, `File.Exists()`, and `Directory.EnumerateFiles()` — pure filesystem checks
- `DetectType` is the dispatcher that tries each signal in priority order
- FolderScanner still handles fallback detection (`DetectFallbackType`), executable discovery, scoring, and container detection
- The signal methods are self-contained — no shared state needed

## Requirements

- [ ] `StoreSignalDetector.cs` created with `DetectType` + 10 signal methods
- [ ] All methods have `/// <summary>` XML docs
- [ ] FolderScanner.cs no longer contains any of the 11 moved methods
- [ ] FolderScanner.Scan() calls `StoreSignalDetector.DetectType()`
- [ ] No behavior change — same detection logic, same priority order
- [ ] `StoreSignalDetector` class is `internal static`

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -c "HasGogSignal\|HasEaSignal\|HasUbisoftSignal\|HasEpicSignal\|HasBlizzardSignal\|HasXboxSignal\|HasRockstarSignal\|HasSteamSignal\|HasSteamEmulatorSignal" src/GamingCommander.App/Services/FolderScanner.cs` returns 0 (all moved)
- [ ] `grep -c "HasGogSignal\|HasEaSignal\|HasUbisoftSignal" src/GamingCommander.App/Services/StoreSignalDetector.cs` returns 3+ (all present)

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
