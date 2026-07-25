# Task T61: Fix Launch Target Resolution

**Tier:** 3 — Logic/Behavior
**Phase:** H — MVP
**Effort:** ~30 min
**Risk:** Medium
**Status:** Pending
**Prerequisites:** None
**WP:** WP-1

---

## Objective

Steam games store `CommandLineArguments = "steam://rungameid/{appid}"` but `ShellViewModel.LoadGamesForRoot()` discards this by setting `LaunchTarget = game.ExecutablePath` unconditionally. The URI is never propagated to the view model, so `LaunchSelectedGameAsync()` always launches the raw `.exe` instead of invoking the Steam client protocol. Standalone games with launch arguments (e.g. GOG SCUMMVM) also lose their args.

## What Needs to Change

### 1. `src/GamingCommander.UI/ViewModels/ShellPaneItemViewModel.cs`

**Current state:** Has `LaunchTarget` (string) but no `CommandLineArguments` property. The type is structurally incapable of carrying launch args.

**Actions:**
- [ ] Add `CommandLineArguments` property:
  ```csharp
  /// <summary>Command-line arguments to pass when launching the game. Empty for URI-only launches.</summary>
  public string CommandLineArguments { get; init; } = string.Empty;
  ```

### 2. `src/GamingCommander.UI/ViewModels/ShellViewModel.cs` — `LoadGamesForRoot()`

**Current state:** Line ~332 sets `LaunchTarget = game.ExecutablePath` unconditionally. `game.CommandLineArguments` is never read.

**Actions:**
- [ ] Before the `Items.Add(new ShellPaneItemViewModel { ... })` block, resolve the launch target:
  ```csharp
  string launchTarget = game.CommandLineArguments.StartsWith("steam://", StringComparison.OrdinalIgnoreCase)
      ? game.CommandLineArguments
      : game.ExecutablePath;
  ```
- [ ] Set `LaunchTarget = launchTarget` in the `ShellPaneItemViewModel` initializer
- [ ] Set `CommandLineArguments = game.CommandLineArguments` in the initializer
- [ ] Add fallback: if both `launchTarget` and `game.CommandLineArguments` are empty, set `LaunchTarget = string.Empty` (no-exe games)

## Context

- `SteamLibraryScanner.CreateEntry()` correctly populates `GameEntry.CommandLineArguments = $"steam://rungameid/{acf.AppId}"` — the data exists at scan time
- `GamesDatabaseService.Save()` persists it to `games.json` as `CmdlineArgs` — the data survives persistence
- `GamesDatabaseService.GetGamesForRoot()` returns it on load — the data is available
- The gap is purely in the ViewModel mapping step: `LoadGamesForRoot()` never reads `CommandLineArguments`
- The plan document specifies "prefer resolve by GameId from DB" but the simpler approach (carry `CommandLineArguments` through the VM) avoids model sprawl and is sufficient for MVP

## Requirements

- [ ] `ShellPaneItemViewModel` has a `CommandLineArguments` property
- [ ] Steam games: `LaunchTarget` is set to `steam://rungameid/{appid}` URI, not the `.exe` path
- [ ] Standalone games with args: `LaunchTarget` is the `.exe` path, `CommandLineArguments` carries the args
- [ ] Standalone games without args: `LaunchTarget` is the `.exe` path, `CommandLineArguments` is empty
- [ ] Games with no exe and no URI: `LaunchTarget` is empty (no crash)

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (no regressions)
- [ ] `grep -n "CommandLineArguments" src/GamingCommander.UI/ViewModels/ShellPaneItemViewModel.cs` shows the new property
- [ ] `grep -n "CommandLineArguments" src/GamingCommander.UI/ViewModels/ShellViewModel.cs` shows it being set
- [ ] Manual trace: Steam GameEntry with `CommandLineArguments = "steam://rungameid/123"` → VM `LaunchTarget` = `"steam://rungameid/123"`

## Completion Notes

- **Completed:** 2026-07-25
- **What was done:**
  - Added `CommandLineArguments` property to `ShellPaneItemViewModel` (line 23)
  - Updated `LoadGamesForRoot()` to resolve `LaunchTarget` — prefers `steam://` URI when `CommandLineArguments` starts with it; falls back to `ExecutablePath`
  - Updated `LaunchSelectedGameAsync()` to pass `CommandLineArguments` as `ProcessStartInfo.Arguments` for non-URI launches (guard prevents passing URI as args)
- **Verification:** Build clean (0 errors), 99 tests passing (0 regressions)
- **Issues encountered:** None
