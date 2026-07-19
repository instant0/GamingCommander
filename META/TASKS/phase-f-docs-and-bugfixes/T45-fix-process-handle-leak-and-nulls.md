# Task T45: Fix Process Handle Leak and Null Issues

**Tier:** 2 — Bug Fix
**Phase:** F — Docs & Bug Fixes
**Effort:** ~15 min
**Risk:** Low
**Status:** ✅ completed

---

## Objective

`MainWindow.axaml.cs` has three bugs: a `Process` handle leak on game launch, a null-forgiving `!` on a nullable return, and an unhandled null from `Path.GetDirectoryName()`.

## What Needs to Change

### `src/GamingCommander.App/MainWindow.axaml.cs`

**Bug 1 — Process handle leak (line 255-268):**
- [ ] Wrap `Process.Start()` in `using` statement to ensure native handle disposal
  ```csharp
  // Before:
  Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
  // After:
  using var proc = Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
  ```

**Bug 2 — Null-forgiving on nullable return (line 304):**
- [ ] Add null guard before `GetGamesForRoot()` call
  ```csharp
  string? rootPath = _viewModel.GetCurrentRootPath();
  if (rootPath is null) return;
  var games = dbService.GetGamesForRoot(rootPath);
  ```

**Bug 3 — `GetDirectoryName` null return (line 267):**
- [ ] Add null-coalescing for `WorkingDirectory`
  ```csharp
  WorkingDirectory = Path.GetDirectoryName(target) ?? "",
  ```

## Context

- Bug 1: Every game launch leaks a `Process` handle. In a long session with many launches, this accumulates.
- Bug 2: `GetCurrentRootPath()` returns `null` when `IsAtRootLevel` is true. A TOCTOU race exists if state changes between the early return check and this call.
- Bug 3: `Path.GetDirectoryName("C:\")` returns `null`. With `UseShellExecute = true` this is likely tolerated, but it's unclean.

## Requirements

- [x] All three bugs fixed
- [x] No new warnings introduced
- [x] Game launching still works (manual verification or test)

## Verification

- [x] `dotnet build` passes (0 errors)
- [x] `dotnet test` passes (99 tests)

## Completion Notes

- **Completed:** 2026-07-19
- **What was done:** Fixed Process handle leak by wrapping `Process.Start()` in `using` statements. Added null guard for `GetCurrentRootPath()` return. Added null-coalescing for `Path.GetDirectoryName()` return.
- **Verification:** Build clean, 99 tests passing.
- **Issues encountered:** None.
