# Task T76: Library Root Nesting Prevention

**Tier:** 1 — Core
**Phase:** H — MVP
**Effort:** ~20 min
**Risk:** Low
**Status:** Complete
**Prerequisites:** T75 Complete
**WP:** WP-5

---

## Objective

Prevent duplicate game entries caused by nested library roots. If a user adds both `d:\games\blizzard\` (child) and `d:\games\` (parent) as library roots, the scanner walks into `blizzard\` from the parent root, producing duplicate entries for every game in the child root.

## The Problem

Two scenarios when paths conflict:

| Scenario | User Action | Result |
|----------|-------------|--------|
| **A** | Has `d:\games\` root, tries to add `d:\games\blizzard\` | **Reject.** "This folder is inside an existing library root." |
| **B** | Has `d:\games\blizzard\` root, tries to add `d:\games\` | **Reject.** "An existing library root is inside this folder. Remove it first." |

Both cases show a message and refuse the add. The user picks one or the other.

Note: `d:\games\EPIC` and `d:\games\blizzard` are **not** conflicting — they are sibling directories, both children of `d:\games\`, and are allowed.

## Implementation

### Single helper: `LibraryManager.IsChildOf()`

A static method on `LibraryManager` (alongside existing `LooksLikeSteamLibrary()` and `NormalizeLibraryRoot()`):

```csharp
public static bool IsChildOf(string childPath, string parentPath)
{
    // Trim separators, compare prefix case-insensitively
    // Reject exact matches and partial name matches (games2 vs games)
}
```

### Check in ViewModels before adding

Both `LibrarySetupViewModel.AddRootAsync()` (F2) and `WizardViewModel.AddEntryAsync()` (first-run) check against their existing entries before proceeding:

```csharp
foreach (var existing in Entries)
{
    if (LibraryManager.IsChildOf(path, existing.Path))
    {
        ScanStatus = "This folder is inside an existing library root. Pick one or the other.";
        return;
    }
    if (LibraryManager.IsChildOf(existing.Path, path))
    {
        ScanStatus = "An existing library root is inside this folder. Remove it first.";
        return;
    }
}
```

No changes to `ILibraryManager`, `AddRoot()`, or any other code.

## Files Changed

| File | Change |
|------|--------|
| `LibraryManager.cs` | Added `IsChildOf()` static method (~15 lines) |
| `LibrarySetupViewModel.cs` | Added nesting check loop in `AddRootAsync()` (~12 lines) |
| `WizardViewModel.cs` | Added nesting check loop in `AddEntryAsync()` (~12 lines) |
| `LibraryManagerTests.cs` | New: 8 tests for `IsChildOf` |

**Total: ~40 lines added. No existing code modified. No interfaces changed.**

## Tests

- `IsChildOf` — child inside parent (trailing separators, mixed separators, different trees, exact match, partial name, case-insensitive)
- 8 test cases covering all edge cases

## Verification

- [x] `dotnet build` passes (0 errors)
- [x] `dotnet test` passes (217 tests, 0 regressions)
- [x] F2: Add child root when parent exists → rejected with message
- [x] F2: Add parent root when child exists → rejected with message
- [x] F2: Add sibling roots → both allowed
- [x] Wizard: Same behavior as F2
