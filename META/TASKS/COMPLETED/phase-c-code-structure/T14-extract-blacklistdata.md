# Task T14: Extract BlacklistData from BlacklistLoader

**Tier:** 2 — Code Structure
**Phase:** C — Code Structure
**Effort:** ~15 min
**Risk:** Low
**Status:** completed

---

## Objective

`BlacklistLoader.cs` contains a public `BlacklistData` record alongside the loader class. `BlacklistData` is a public API type (used by `FolderScanner` constructor) and should be in its own file.

## What Needs to Change

### 1. `src/GamingCommander.App/Services/BlacklistLoader.cs`

**Current state:** Contains `BlacklistLoader` class + `BlacklistData` public record + 4 internal DTO classes.

**Actions:**
- Extract `BlacklistData` record to new file `BlacklistData.cs`
- Keep `BlacklistLoader` class + DTO classes in `BlacklistLoader.cs`
- Update any `using` statements if needed (same namespace, so likely no change)

### 2. Create `src/GamingCommander.App/Services/BlacklistData.cs`

```csharp
namespace GamingCommander.App.Services;

/// <summary>
/// Flattened blacklist data loaded from data/blacklist.json.
/// Contains noise-pattern substrings for exe names and directory names.
/// </summary>
public sealed record BlacklistData(
    IReadOnlyList<string> ExeNamePatterns,
    IReadOnlyList<string> DirectoryPatterns)
{
    public static readonly BlacklistData Empty = new([], []);
}
```

## Context

- `BlacklistData` is referenced by `FolderScanner` constructor (line 54: `BlacklistData blacklist`)
- `BlacklistData` is referenced by `App.axaml.cs` startup code
- The `Empty` static field is used when the JSON file is missing
- DTO classes (`BlacklistDto`, `ExeNamePatternsDto`, etc.) stay in `BlacklistLoader.cs` — they're internal

## Requirements

- [ ] Extract `BlacklistData` record to `BlacklistData.cs`
- [ ] Preserve the `/// <summary>` documentation
- [ ] Keep DTO classes in `BlacklistLoader.cs`
- [ ] No logic changes

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `BlacklistData.cs` exists with the record + Empty field
- [ ] `BlacklistLoader.cs` no longer contains `BlacklistData`

## Completion Notes

- **Completed:** 2026-07-18
- **What was done:** Extracted `BlacklistData` record from `BlacklistLoader.cs` to new `BlacklistData.cs`
- The actual record had 4 properties (ExeNamePatterns, DirectoryPatterns, PeMetadataPatterns, PcgwTitleNoise) — more than the task file's stub which showed 2
- DTO classes remain internal in `BlacklistLoader.cs`
- **Verification:** Build clean, 17 tests passing
- **No issues encountered.**
