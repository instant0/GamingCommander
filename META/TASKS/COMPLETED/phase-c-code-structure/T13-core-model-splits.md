# Task T13: Split Core Model Multi-Type Files

**Tier:** 2 — Code Structure
**Phase:** C — Code Structure
**Effort:** ~20 min
**Risk:** Minimal
**Status:** completed

---

## Objective

Several Core model files contain multiple types (records, enums) that should each be in their own file. This is a pure structural refactor — no logic changes, just file reorganization.

## What Needs to Change

### 1. `src/GamingCommander.Core/Models/GameEntry.cs`

Currently contains 3 records in one file: `GameEntry`, `GameRoot`, `GamesDatabase`.

**Actions:**
- Keep `GameEntry` in `GameEntry.cs`
- Extract `GameRoot` to new file `GameRoot.cs`
- Extract `GamesDatabase` to new file `GamesDatabase.cs`
- Each file gets only its own record + namespace declaration

**New file `GameRoot.cs`:**
```csharp
namespace GamingCommander.Core.Models;

public sealed record GameRoot(
    string RootPath,
    GameSourceKind DefaultType,
    List<GameEntry> Games);
```

**New file `GamesDatabase.cs`:**
```csharp
namespace GamingCommander.Core.Models;

public sealed record GamesDatabase(
    List<GameRoot> Roots);
```

### 2. `src/GamingCommander.Core/Models/LibraryRoot.cs`

Currently contains 2 records: `LibraryRoot`, `FolderOverride`.

**Actions:**
- Keep `LibraryRoot` in `LibraryRoot.cs`
- Extract `FolderOverride` to new file `FolderOverride.cs`

**New file `FolderOverride.cs`:**
```csharp
namespace GamingCommander.Core.Models;

public sealed record FolderOverride(
    string FolderPath,
    GameSourceKind Type);
```

### 3. `src/GamingCommander.Core/Models/FileSystemEntry.cs`

Currently contains 1 enum + 1 record: `FileSystemEntryKind`, `FileSystemEntry`.

**Actions:**
- Extract `FileSystemEntryKind` to new file `FileSystemEntryKind.cs`
- Keep `FileSystemEntry` in `FileSystemEntry.cs`

**New file `FileSystemEntryKind.cs`:**
```csharp
namespace GamingCommander.Core.Models;

public enum FileSystemEntryKind
{
    Directory,
    File,
    ParentDirectory,
}
```

## Context

- These are pure moves — no logic changes
- All types stay in the same namespace (`GamingCommander.Core.Models`)
- No using statements need to change (same namespace)
- The `GameRecord.cs` file already has a single type — no change needed
- `GameSourceKind.cs`, `MigrationMode.cs`, `MigrationPlanSummary.cs`, `AppConfig.cs` — all single-type files, no change needed

## Requirements

- [ ] Create 4 new files (GameRoot.cs, GamesDatabase.cs, FolderOverride.cs, FileSystemEntryKind.cs)
- [ ] Remove extracted types from original files
- [ ] Preserve XML docs (from T09) on each type
- [ ] All types remain in `GamingCommander.Core.Models` namespace
- [ ] No logic changes

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] Each file in `Core/Models/` contains exactly one type
- [ ] No duplicate type definitions across files

## Completion Notes

- **Completed:** 2026-07-18
- **What was done:** Split 3 multi-type files into single-type files:
  1. `GameEntry.cs` → extracted `GameRoot.cs` and `GamesDatabase.cs`
  2. `LibraryRoot.cs` → extracted `FolderOverride.cs`
  3. `FileSystemEntry.cs` → extracted `FileSystemEntryKind.cs`
- Created 4 new files, edited 3 original files
- All XML docs preserved, all types in `GamingCommander.Core.Models` namespace
- **Verification:** Build clean, 17 tests passing, each file has exactly 1 public type
- **No issues encountered.**
