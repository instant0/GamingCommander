# Task T26: Extract F-Key Command Dispatch to Shared Helper

**Tier:** 2 — Code Structure
**Phase:** D — Complexity Reduction
**Effort:** ~25 min
**Risk:** Low
**Status:** ⏭️ skipped — overengineered

---

## Evaluation Notes (2026-07-19)

**Reason for skipping:** The two F-key switch statements (OnKeyDown: 10 cases, CommandButtonPressed: 10 cases) are short, readable, and maintainable as-is. The proposed dispatcher class adds delegate registration complexity (`RegisterKey`, `RegisterHotkey`) for no real benefit. Additionally:
- Cross-reference error: task says "T25 will use" but T25 is SteamAcfParser
- `DispatchKey` modifier check (`KeyModifiers.None`) is wrong — F-keys work without modifier check in actual code
- Fire-and-forget `_ = handler()` loses await semantics from OnKeyDown
- Class should be `internal` not `public` (same assembly only)
**Prerequisites:** None

---

## Objective

`MainWindow.axaml.cs` has two dispatch methods that map F-key strings to async handlers: `OnKeyDown` (lines 115-222) and `CommandButtonPressed` (lines 502-539). Both contain the same F-key → method mapping. Extract this to a shared helper that both methods can use.

**Note:** This task creates the `KeyboardCommandDispatcher` class that T25 will use. T26 creates the class; T25 integrates it into MainWindow.

## What Needs to Change

### New file: `src/GamingCommander.App/Services/KeyboardCommandDispatcher.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create `KeyboardCommandDispatcher.cs` with namespace `GamingCommander.App.Services`
- [ ] Add `/// <summary>` to class: "Maps F-key shortcuts and command bar button names to async handler methods. Centralizes the keyboard/button dispatch logic."
- [ ] Add delegate: `public delegate Task AsyncCommandHandler();`
- [ ] Add fields:
  ```csharp
  private readonly Dictionary<Key, AsyncCommandHandler> _keyHandlers = new();
  private readonly Dictionary<string, AsyncCommandHandler> _hotkeyHandlers = new(StringComparer.OrdinalIgnoreCase);
  ```
- [ ] Add `RegisterKey(Key key, AsyncCommandHandler handler)` method with XML doc
- [ ] Add `RegisterHotkey(string hotkeyName, AsyncCommandHandler handler)` method with XML doc
  - `hotkeyName` is the F-key string like "F1", "F2", etc.
- [ ] Add `DispatchKey(KeyEventArgs e)` method:
  ```csharp
  /// <summary>
  /// Attempts to dispatch a key press to a registered handler.
  /// Returns true if a handler was found and invoked, false otherwise.
  /// Ignores key modifiers (Ctrl, Shift, Alt) — only dispatches plain key presses.
  /// </summary>
  public bool DispatchKey(KeyEventArgs e)
  {
      if (e.KeyModifiers != KeyModifiers.None)
          return false;
      if (_keyHandlers.TryGetValue(e.Key, out var handler))
      {
          _ = handler();
          return true;
      }
      return false;
  }
  ```
- [ ] Add `DispatchHotkey(string hotkeyName)` method:
  ```csharp
  /// <summary>
  /// Attempts to dispatch a command bar button click by its hotkey name (e.g., "F5").
  /// Returns true if a handler was found and invoked, false otherwise.
  /// </summary>
  public bool DispatchHotkey(string hotkeyName)
  {
      if (_hotkeyHandlers.TryGetValue(hotkeyName, out var handler))
      {
          _ = handler();
          return true;
      }
      return false;
  }
  ```

## Context

- This class is created in T26 and integrated into MainWindow in T25
- The class is intentionally simple — just two dictionaries and two dispatch methods
- `Key` is from `Avalonia.Input` — the class depends on Avalonia
- The fire-and-forget `_ = handler()` pattern is standard for async void event handlers in Avalonia
- Both `DispatchKey` and `DispatchHotkey` return bool so callers know if the event was handled

## Requirements

- [ ] `KeyboardCommandDispatcher.cs` created with `AsyncCommandHandler` delegate
- [ ] `RegisterKey` and `RegisterHotkey` methods exist with XML docs
- [ ] `DispatchKey` and `DispatchHotkey` methods exist with XML docs
- [ ] Class is `public` (used by MainWindow which is in the same assembly)
- [ ] No behavior changes (class not integrated yet — that's T25)

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -c "KeyboardCommandDispatcher" src/GamingCommander.App/Services/KeyboardCommandDispatcher.cs` returns 1 (class definition present)
- [ ] File is under 80 lines

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
