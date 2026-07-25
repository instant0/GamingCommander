# Task T76: Library Root Nesting Prevention

**Tier:** 1 — Core
**Phase:** H — MVP
**Effort:** ~1–2 hr
**Risk:** Low–Medium
**Status:** Pending
**Prerequisites:** T75 Complete
**WP:** WP-5

---

## Objective

Prevent duplicate game entries caused by nested library roots. If a user adds both `d:\games\blizzard\` (child) and `d:\games\` (parent) as library roots, the scanner walks into `blizzard\` from the parent root, producing duplicate entries for every game in the child root.

## Problem Analysis

### Current State

Three entry points can add library roots:

| Entry Point | Code Path | Nesting Check? |
|-------------|-----------|----------------|
| **F7** (add root) | `MainWindow.AddRootAsync()` → `LibraryManager.AddRoot()` | ❌ None |
| **F2** (library setup) | `LibrarySetupViewModel.AddRootAsync()` → `LibraryManager.AddRoot()` | ❌ None |
| **Wizard** (first-run) | `WizardViewModel.AddEntryAsync()` → `Finish()` writes config directly | ❌ None |

All three only check for **exact duplicate** paths (line-for-line match), not parent/child nesting.

### The Problem

| Scenario | User Action | Current Behavior | Desired Behavior |
|----------|-------------|------------------|------------------|
| **A** | Has `d:\games\` root, tries to add `d:\games\blizzard\` | Child added. Games in `blizzard\` appear **twice** (once from parent scan, once from child scan). | **Reject.** Show: "This folder is inside an existing library root." |
| **B** | Has `d:\games\blizzard\` root, tries to add `d:\games\` | Parent added. Games in `blizzard\` appear **twice**. | **Absorb.** Remove child root, add parent. User overrides from child are migrated. Show: "Absorbed blizzard\ into d:\games\." |

### Design Decision: Why Not Allow Nesting?

Two options were considered:

1. **Allow nesting, hide duplicates in VFS** — Simpler but fragile. The database would contain duplicate entries under different roots. Renaming a game in one root wouldn't reflect in the other. Rescan of the parent would re-create duplicates.

2. **Prevent nesting (this task)** — Cleaner invariant: every game folder belongs to exactly one library root. The scanner handles 98% of detection; the user fixes the remaining 2% with F4. This avoids an entire class of bugs.

**Decision: Option 2.** Prevent nesting at the `LibraryManager` level.

### When Nesting Is Detected

Three cases for path relationship (`IsParentOf` = path starts with other path + separator, case-insensitive):

```
newRoot = "d:\games\blizzard\"
existingRoots = ["d:\games\"]

→ newRoot is INSIDE existingRoot → Case A: Reject

newRoot = "d:\games\"
existingRoots = ["d:\games\blizzard\"]

→ newRoot CONTAINS existingRoot → Case B: Absorb
```

## Implementation

### 1. Add `ResolveRootNesting` helper to `LibraryManager`

New static method that checks a candidate root against all existing roots:

```csharp
/// <summary>
/// Determines how a candidate root relates to existing roots.
/// Returns Reject (child of existing), Absorb (parent of existing), or Add (no conflict).
/// </summary>
internal static RootNestingResult ResolveRootNesting(
    string candidateRoot,
    IReadOnlyList<LibraryRoot> existingRoots)
{
    foreach (var existing in existingRoots)
    {
        if (IsChildOf(candidateRoot, existing.RootPath))
        {
            // Candidate is inside an existing root → reject
            return new RootNestingResult(
                RootNestingAction.Reject,
                reason: $"This folder is inside an existing library root ({Path.GetFileName(existing.RootPath)}).");
        }
    }

    var absorbed = new List<string>();
    foreach (var existing in existingRoots)
    {
        if (IsChildOf(existing.RootPath, candidateRoot))
        {
            // Existing root is inside the candidate → absorb it
            absorbed.Add(existing.RootPath);
        }
    }

    if (absorbed.Count > 0)
    {
        return new RootNestingResult(
            RootNestingAction.Absorb,
            absorbedRoots: absorbed,
            reason: $"Absorbed {absorbed.Count} inner root(s) into {Path.GetFileName(candidateRoot)}.");
    }

    return new RootNestingResult(RootNestingAction.Add);
}

private static bool IsChildOf(string childPath, string parentPath)
{
    // Case-insensitive: child starts with parent + separator
    return childPath.StartsWith(parentPath, StringComparison.OrdinalIgnoreCase)
        && (childPath.Length == parentPath.Length
            || childPath[parentPath.Length] == Path.DirectorySeparatorChar
            || childPath[parentPath.Length] == Path.AltDirectorySeparatorChar);
}
```

### 2. Add enums/types

```csharp
internal enum RootNestingAction { Add, Reject, Absorb }

internal sealed record RootNestingResult(
    RootNestingAction Action,
    IReadOnlyList<string> AbsorbedRoots = [],
    string Reason = "")
{
    public static RootNestingResult Add => new(RootNestingAction.Add);
}
```

### 3. Modify `LibraryManager.AddRoot()` — Case A + B

```csharp
public bool AddRoot(string rootPath, GameSourceKind defaultType, IReadOnlyList<GameEntry> initialGames)
{
    AppConfig config = _configService.Load();

    // NEW: Check for nesting conflicts
    RootNestingResult nesting = ResolveRootNesting(rootPath, config.LibraryRoots);

    if (nesting.Action == RootNestingAction.Reject)
        return false; // Caller shows nesting.Reason as status message

    if (nesting.Action == RootNestingAction.Absorb)
    {
        // Remove absorbed roots from config and database
        foreach (string absorbedPath in nesting.AbsorbedRoots)
        {
            RemoveRoot(absorbedPath);
        }
    }

    // ... rest of existing AddRoot logic (scan, persist) ...
}
```

Return value changes: `AddRoot` now returns a `RootNestingResult` (or we add a separate `AddRootWithNesting` method to avoid breaking `ILibraryManager`).

**Approach A (simpler):** Change return type to `AddRootResult` with both `bool Added` and `string? RejectionReason`.

**Approach B (non-breaking):** Keep `bool AddRoot()`, add `string? LastRejectionReason` property that callers read after a `false` return.

**Decision: Approach A.** The interface is internal — no external consumers.

### 4. Update `ILibraryManager` interface

```csharp
sealed record AddRootResult(bool Added, string? RejectionReason = null);

public interface ILibraryManager
{
    // ...
    AddRootResult AddRoot(string rootPath, GameSourceKind defaultType, IReadOnlyList<GameEntry> initialGames);
    // ...
}
```

### 5. Update callers

#### `MainWindow.AddRootAsync()` (F7)

```csharp
var result = _libraryManager.AddRoot(result, detectedType, []);
if (result.Added)
{
    _viewModel?.Reload();
    SetStatusWithAutoClear($"Added root: {result_path}");
}
else if (result.RejectionReason is not null)
{
    SetStatusWithAutoClear(result.RejectionReason);
}
else
{
    SetStatusWithAutoClear($"No games found in {result_path}");
}
```

#### `LibrarySetupViewModel.AddRootAsync()` (F2)

```csharp
var result = await Task.Run(() => _libraryManager.AddRoot(path, defaultType, []));
if (result.Added)
{
    // Update game count in entry
}
else
{
    // Remove entry from UI, show rejection reason
}
```

#### `WizardViewModel` (First-run wizard)

The wizard writes roots directly to config via `Finish()`, bypassing `LibraryManager.AddRoot()`. Two options:

1. Route wizard through `LibraryManager.AddRoot()` — cleaner but requires more refactoring
2. Apply nesting check at wizard level only — simpler, self-contained

**Decision: Option 2** (wizard-level check). The wizard has its own `Entries` collection; nesting checks happen against that collection. `Finish()` also deduplicates the final list before writing config.

```csharp
// In WizardViewModel.AddEntryAsync():
public async Task AddEntryAsync()
{
    // ... existing picker code ...

    // NEW: Check nesting against existing wizard entries
    var mockRoots = Entries.Select(e => new LibraryRoot(e.Path, GameSourceParser.ParseFromString(e.SelectedType))).ToList();
    var nesting = LibraryManager.ResolveRootNesting(path, mockRoots);

    if (nesting.Action == RootNestingAction.Reject)
    {
        ScanStatus = nesting.Reason; // Show rejection
        return;
    }

    if (nesting.Action == RootNestingAction.Absorb)
    {
        // Remove absorbed entries from wizard
        foreach (string absorbedPath in nesting.AbsorbedRoots)
        {
            var absorbed = Entries.FirstOrDefault(e => e.Path.Equals(absorbedPath, StringComparison.OrdinalIgnoreCase));
            if (absorbed != null) Entries.Remove(absorbed);
        }
    }

    // ... existing add + scan logic ...
}
```

Also add nesting dedup in `Finish()` as a safety net:

```csharp
public void Finish()
{
    // Deduplicate: if any entry is a child of another, remove it
    var deduplicated = DeduplicateByNesting(Entries);
    // ... write to config ...
}

private static List<WizardLibraryEntry> DeduplicateByNesting(ObservableCollection<WizardLibraryEntry> entries)
{
    var roots = entries.Select(e => new LibraryRoot(e.Path, GameSourceParser.ParseFromString(e.SelectedType))).ToList();
    var result = new List<WizardLibraryEntry>();

    foreach (var entry in entries)
    {
        var otherRoots = roots.Where(r => !r.RootPath.Equals(entry.Path, StringComparison.OrdinalIgnoreCase)).ToList();
        var nesting = LibraryManager.ResolveRootNesting(entry.Path, otherRoots);
        if (nesting.Action != RootNestingAction.Reject)
            result.Add(entry);
    }

    return result;
}
```

### 6. Add tests

**New test file or additions to `LibraryManagerTests`:**

```
- ResolveRootNesting_NoConflict → Add
- ResolveRootNesting_ChildOfExisting → Reject
- ResolveRootNesting_ParentOfExisting → Absorb
- ResolveRootNesting_ExactDuplicate → (handled by existing dedup, not nesting)
- AddRoot_ChildOfExisting_ReturnsReject → false, games not duplicated
- AddRoot_ParentOfExisting_RemovesChild → true, child removed from config+DB
- AddRoot_BothDirections → add blizzard first, then games → blizzard absorbed
- WizardViewModel_AddEntry_NestedChildRejected → entry not added
- WizardViewModel_AddEntry_NestedParentAbsorbs → child removed from entries
```

## Edge Cases

1. **Multiple nested children** — Parent absorbs all children (e.g., `blizzard\` + `ea\` under `games\`)
2. **Chain nesting** — `games\blizzard\` absorbs `games\blizzard\diablo3\` (but this shouldn't happen if scanner is correct)
3. **Steam library normalization** — `NormalizeLibraryRoot()` already walks up to find `steamapps/common/`. If user picks `steamapps\common\Diablo III\`, it normalizes to `d:\steamlibrary\`. This already prevents the most common nesting case for Steam.
4. **Config on disk has pre-existing nesting** — Not handled by this task (would require migration on startup). Log a warning if detected. Could be a follow-up task.
5. **Wizard writes roots directly** — `WizardViewModel.Finish()` bypasses `LibraryManager`. The nesting dedup in `Finish()` is the safety net.

## Files to Change

| File | Change |
|------|--------|
| `src/GamingCommander.App/Services/LibraryManager.cs` | Add `ResolveRootNesting()`, `IsChildOf()`, modify `AddRoot()` |
| `src/GamingCommander.Core/ILibraryManager.cs` | Update `AddRoot()` return type to `AddRootResult` |
| `src/GamingCommander.App/MainWindow.axaml.cs` | Handle `AddRootResult` in `AddRootAsync()` |
| `src/GamingCommander.App/ViewModels/LibrarySetupViewModel.cs` | Handle `AddRootResult` in `AddRootAsync()` |
| `src/GamingCommander.App/ViewModels/WizardViewModel.cs` | Add nesting check in `AddEntryAsync()` + dedup in `Finish()` |
| `tests/GamingCommander.App.Tests/LibraryManagerTests.cs` | New nesting tests |

## Downstream Consequences

1. **`AddRoot()` return type change** — All callers must handle `AddRootResult` instead of `bool`. Three call sites to update.
2. **Wizard `Finish()` behavior** — Silently deduplicates nested entries. User sees child removed from wizard list when they add the parent.
3. **Pre-existing nesting on disk** — Not migrated in this task. If config already has nested roots from an older version, they remain until the user manually removes them. A future startup check could warn.
4. **No impact on rescan** — `Refresh()` iterates all roots independently. With nesting prevention, there's no risk of duplicate entries from rescan.

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (no regressions)
- [ ] New nesting tests pass
- [ ] Manual: Add child root when parent exists → "inside existing root" message
- [ ] Manual: Add parent root when child exists → child absorbed, games preserved
- [ ] Manual: Wizard — add parent after child → child removed from list
- [ ] Manual: No duplicate games after any nesting scenario
