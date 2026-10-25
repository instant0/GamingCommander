# Task T62: Fix Launch Execution

**Tier:** 3 — Logic/Behavior
**Phase:** H — MVP
**Effort:** ~20 min
**Risk:** Medium
**Status:** Complete
**Prerequisites:** T61
**WP:** WP-1

---

## Objective

`MainWindow.LaunchSelectedGameAsync()` has two branches: one for `steam://` URIs and one for filesystem paths. The URI branch works correctly when reached. However, it never passes `CommandLineArguments` for non-URI launches, and it doesn't read `CommandLineArguments` from the view model at all. After T61, the VM carries the right data — this task wires it into `ProcessStartInfo`.

## What Needs to Change

### 1. `src/GamingCommander.App/MainWindow.axaml.cs` — `LaunchSelectedGameAsync()`

**Current state:** Lines 222-279. The `else` branch (non-URI) creates `ProcessStartInfo` with no `Arguments` property. The method never reads `item.CommandLineArguments`.

**Actions:**
- [ ] In the `else` branch (non-URI launch), extract arguments from the VM:
  ```csharp
  // Only pass args for non-URI launches (steam:// is the entire target)
  string args = item.CommandLineArguments.StartsWith("steam://", StringComparison.OrdinalIgnoreCase)
      ? string.Empty
      : item.CommandLineArguments;
  ```
- [ ] Add `Arguments = args` to the `ProcessStartInfo`:
  ```csharp
  using var proc = Process.Start(new ProcessStartInfo
  {
      FileName = target,
      Arguments = args,
      UseShellExecute = true,
      WorkingDirectory = Path.GetDirectoryName(target) ?? "",
  });
  ```
- [ ] Update the status message to include args when present:
  ```csharp
  _viewModel.StatusText = string.IsNullOrEmpty(args)
      ? $"Launching: {target}"
      : $"Launching: {target} {args}";
  ```

### 2. No-exe guard (already exists but verify)

**Current state:** Lines 231-235 already check `item.LaunchTarget is null || item.LaunchTarget.Length == 0` and show "No executable path for this entry."

**Actions:**
- [ ] Verify this guard covers the new empty-LaunchTarget case from T61 (games with no exe and no URI)
- [ ] Update the message to `"No launch target for {item.Title}"` if it currently shows a generic message

## Context

- `ProcessStartInfo.Arguments` is the standard .NET way to pass command-line args
- `UseShellExecute = true` is required for `steam://` URIs on Windows (to invoke the protocol handler)
- For non-URI launches, `UseShellExecute = true` is kept to handle `.exe` files, `.bat` files, and games that need elevation
- Standalone GOG games may have args like `--windowed --nosound` — these must be passed for the game to work correctly
- Steam games use URI-only launch (no args needed after URI), so `Arguments` should be empty for URI targets

## Requirements

- [ ] Non-URI launches pass `CommandLineArguments` via `ProcessStartInfo.Arguments`
- [ ] URI launches (`steam://`) do NOT set `Arguments` (the URI is the entire target)
- [ ] Empty `CommandLineArguments` produces no arguments (no trailing spaces)
- [ ] No-exe games show a user-readable status message, not an exception
- [ ] Status bar shows launch intent (with args when present)

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (no regressions)
- [ ] Manual trace: Standalone game with `CommandLineArguments = "--windowed"` → `ProcessStartInfo.Arguments = "--windowed"`
- [ ] Manual trace: Steam game with `CommandLineArguments = "steam://rungameid/123"` → `ProcessStartInfo.Arguments` is empty (or not set)

## Completion Notes

- **Completed:** 2026-07-25
- **What was done:**
  - Updated no-exe guard: message now reads `"No launch target for {item.Title}"` with proper null handling
  - Moved `args` computation before the URI/filesystem branching so it's available for status display
  - Status bar now shows `"Launching: {target} {args}"` when args are present, `"Launching: {target}"` when empty
  - URI launches do not set `Arguments` (the URI is the entire target)
  - Fixed CS8602 nullable warning introduced by including `item.Title` in the status message
- **Verification:** Build clean (0 errors), 99 tests passing (0 regressions)
- **Issues encountered:** None
