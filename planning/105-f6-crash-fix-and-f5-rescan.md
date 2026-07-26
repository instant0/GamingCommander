# Plan 105 — Move F6 Rescan to F5 + Fix Rescan Crash

**Status:** ✅ COMPLETE  
**Audience:** Builder  
**Priority:** P0 (crash fix + UX convention)  
**Depends on:** None  

---

## 0. Problem Statement

1. **F6 rescan crashes** the application with existing library configuration (Bug 8, TECH_DEBT.md)
2. **F5 is the universal refresh/rescan key** in every application (browsers, file managers, IDEs). GamingCommander removed F5 (T71) but should repurpose it for rescan.
3. **F6 is non-standard** — no application uses F6 for refresh. Norton Commander used F2 for copy and F5 for copy, but modern Windows conventions differ.

---

## 1. What Changes

### Key Rebinding

| Key | Before | After |
|-----|--------|-------|
| F5 | Removed (was "Launch") | **Rescan current root / all roots** |
| F6 | Rescan | Removed (or reassigned) |

### Crash Fixes (prerequisite to rebinding)

The rescan crash must be fixed **before** moving the keybind, since the same code path is used.

---

## 2. Crash Fixes

### Fix 1: ContainerScanner — Safe Filesystem Access

**File:** `src/GamingCommander.App/Services/ContainerScanner.cs`  
**Lines:** 104-105

**Current (crashes):**
```csharp
FileInfo[] files = child.GetFiles("*", SearchOption.TopDirectoryOnly);
if (files.Length == 0 && child.GetDirectories().Length > 0)
```

**Fix:**
```csharp
FileInfo[] files;
try
{
    files = child.GetFiles("*", SearchOption.TopDirectoryOnly);
}
catch (Exception)
{
    files = Array.Empty<FileInfo>();
}

DirectoryInfo[] dirs;
try
{
    dirs = child.GetDirectories();
}
catch (Exception)
{
    dirs = Array.Empty<DirectoryInfo>();
}

if (files.Length == 0 && dirs.Length > 0)
```

Or use `FileSystemHelper.GetFilesSafe` / `FileSystemHelper.GetDirectoriesSafe` equivalents for `DirectoryInfo[]`.

### Fix 2: Top-Level Try-Catch in RefreshCurrentRootAsync

**File:** `src/GamingCommander.App/MainWindow.axaml.cs`  
**Method:** `RefreshCurrentRootAsync()`

Wrap the entire method body in try-catch:
```csharp
private async Task RefreshCurrentRootAsync()
{
    try
    {
        // ... existing code ...
    }
    catch (Exception ex)
    {
        SetStatusWithAutoClear($"Rescan failed: {ex.Message}");
    }
}
```

### Fix 3: Re-Entrancy Guard

**File:** `src/GamingCommander.App/MainWindow.axaml.cs`

Add a field:
```csharp
private bool _isRefreshing;
```

At the start of `RefreshCurrentRootAsync`:
```csharp
if (_isRefreshing) return;
_isRefreshing = true;
try
{
    // ... existing code ...
}
finally
{
    _isRefreshing = false;
}
```

### Fix 4: Duplicate ID Handling in RescanRoot

**File:** `src/GamingCommander.App/Services/GamesDatabaseService.cs`  
**Line:** 137

**Current (crashes on duplicates):**
```csharp
var existingGamesLookup = existing.Games.ToDictionary(g => g.Id);
```

**Fix:**
```csharp
var existingGamesLookup = new Dictionary<string, GameEntry>();
foreach (var game in existing.Games)
{
    // Last-write-wins on duplicates (shouldn't happen, but be safe)
    existingGamesLookup[game.Id] = game;
}
```

### Fix 5: Per-Root Error Handling in LibraryManager.Refresh

**File:** `src/GamingCommander.App/Services/LibraryManager.cs`  
**Method:** `Refresh()`

Wrap each root iteration in try-catch:
```csharp
public void Refresh()
{
    AppConfig config = _configService.Load();
    foreach (LibraryRoot root in config.LibraryRoots)
    {
        try
        {
            if (!Directory.Exists(root.RootPath))
                continue;
            IReadOnlyList<GameEntry> games = SelectScannerAndScan(root.RootPath, root.DefaultType);
            _databaseService.RescanRoot(root.RootPath, games);
        }
        catch (Exception)
        {
            // Log but continue — don't let one root crash skip the rest
        }
    }
}
```

---

## 3. Key Rebinding

### MainWindow.axaml.cs — OnKeyDown

**Remove:**
```csharp
case Key.F6:
    await RefreshCurrentRootAsync();
    e.Handled = true;
    break;
```

**Add:**
```csharp
case Key.F5:
    await RefreshCurrentRootAsync();
    e.Handled = true;
    break;
```

### MainWindow.axaml.cs — CommandButtonPressed

Update the command dispatcher to map F5 → rescan instead of F6.

### MainWindow.axaml — Command Bar

Update F5 button label from removed state to `"Rescan"`.
Update F6 button label from `"Rescan"` to removed/placeholder.

### ShellViewModel.Commands

Update the F5 command entry:
```csharp
new ShellCommandViewModel { Hotkey = "F5", Label = "Rescan" }
```

Remove or repurpose the F6 entry.

### HelpDialogBuilder

Update help dialog:
- F5: "Rescan current root / all roots"
- F6: Remove or repurpose

### InteractionHint

Update the hint string to show F5 instead of F6.

---

## 4. Files Changed

| File | Change |
|------|--------|
| `App/MainWindow.axaml.cs` | F6→F5 keybind, try-catch, re-entrancy guard |
| `App/MainWindow.axaml` | F5/F6 button labels |
| `UI/ViewModels/ShellViewModel.cs` | F5 command entry |
| `App/Services/HelpDialogBuilder.cs` | F5/F6 help text |
| `App/Services/ContainerScanner.cs` | Safe filesystem access (lines 104-105) |
| `App/Services/GamesDatabaseService.cs` | Duplicate ID handling (line 137) |
| `App/Services/LibraryManager.cs` | Per-root try-catch in Refresh() |

---

## 5. Tests

- `ContainerScannerTests.cs`: Add test for permission-denied directory (mock or temp dir with restricted access)
- `GamesDatabaseServiceTests.cs`: Add test for duplicate IDs in RescanRoot
- `MainWindowRescanTests.cs`: Verify F5 triggers rescan, F6 does not

---

## 6. Success Criteria

- [x] F5 triggers rescan (at root level: all roots; inside root: current root)
- [x] F6 no longer triggers rescan (moved to F5)
- [x] Rescan does not crash on permission-denied directories
- [x] Rescan does not crash on duplicate database IDs
- [x] Rescan does not crash on any single root failure (continues with others)
- [x] Double-press F5 does not cause concurrent rescans
- [x] Help dialog, command bar, interaction hint all reflect F5
- [x] Build clean, all tests pass (219 tests)

---

## 7. Norton Commander Heritage Note

In classic Norton Commander:
- F2 = Copy
- F5 = Copy
- F6 = Rename/Move
- F8 = Delete

Modern conventions (Windows Explorer, VS Code, browsers):
- F5 = Refresh/Reload

GamingCommander should follow modern conventions. F5 = Rescan is the correct choice.
