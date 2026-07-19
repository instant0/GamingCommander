# Task T53: Extract Scoring Constants and Status Constants

**Tier:** 4 — Code Quality
**Phase:** G — Code Quality & Tests
**Effort:** ~20 min
**Risk:** Minimal
**Status:** pending

---

## Objective

Magic numbers in `ScoreExecutable` and magic strings for Steam status are scattered across multiple files. Extract to named constants.

## What Needs to Change

### `src/GamingCommander.App/Services/ExecutableDiscovery.cs`
- [ ] Add named constants at top of class:
  ```csharp
  private const int TokenMatchBonus = 10;
  private const int ShippingBonus = 5;
  private const int LauncherPenalty = -20;
  private const int UniversalNoisePenalty = -30;
  private const int LikelyNoisePenalty = -20;
  private const int PossibleNoisePenalty = -10;
  private const int MildNoisePenalty = -5;
  private const long FileSizeBonusThreshold = 10_000_000;
  private const int MaxFileSizeBonus = 10;
  ```
- [ ] Replace all inline magic numbers in `ScoreExecutable` with these constants

### `src/GamingCommander.Core/Models/SteamStatus.cs` (new file)
- [ ] Create static class with string constants:
  ```csharp
  public static class SteamStatus
  {
      public const string Installed = "Installed";
      public const string Moved = "Moved";
      public const string Orphaned = "Orphaned";
      public const string Missing = "Missing";
  }
  ```

### `src/GamingCommander.App/Services/SteamLibraryScanner.cs`
- [ ] Replace `"Installed"`, `"Moved"`, `"Orphaned"`, `"Missing"` with `SteamStatus.Installed`, etc.

### `src/GamingCommander.UI/ViewModels/ShellViewModel.cs`
- [ ] Replace same magic strings with `SteamStatus.*` constants
- [ ] Also extract color hex strings to a small dictionary or constants

## Context

- Scoring constants: `+10`, `-20`, `-30`, `-10`, `-5`, `+5`, `10_000_000`, `10` are all inline
- Status strings: `"Installed"`, `"Moved"`, `"Orphaned"`, `"Missing"` passed through `Extra` dictionary and compared in 4+ locations
- A typo in any status string would silently break status display

## Requirements

- [ ] All scoring magic numbers replaced with named constants
- [ ] All Steam status magic strings replaced with `SteamStatus.*`
- [ ] No behavior change

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
