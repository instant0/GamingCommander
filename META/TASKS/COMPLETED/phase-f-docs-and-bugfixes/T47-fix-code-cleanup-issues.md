# Task T47: Fix Code Cleanup Issues

**Tier:** 2 — Cleanup
**Phase:** F — Docs & Bug Fixes
**Effort:** ~10 min
**Risk:** Minimal
**Status:** ✅ completed

---

## Objective

Fix minor code hygiene issues: self-referencing using, redundant using, and unnecessary project references.

## What Needs to Change

### `src/GamingCommander.Core/Models/GameRecord.cs`
- [ ] Remove self-referencing `using GamingCommander.Core.Models;` (line 1) — file is already in that namespace

### `src/GamingCommander.App/Program.cs`
- [ ] Remove redundant `using System;` (line 2) — `ImplicitUsings` is enabled in the project

### `src/GamingCommander.UI/GamingCommander.UI.csproj`
- [ ] Remove `<ProjectReference Include="..\GamingCommander.Detection\..." />` — no types from Detection are used in UI
- [ ] Remove `<ProjectReference Include="..\GamingCommander.Migration\..." />` — no types from Migration are used in UI

## Context

- The self-referencing using in `GameRecord.cs` is a common C# oversight. Harmless but sloppy.
- `ImplicitUsings` in .NET 6+ auto-imports `System`, `System.Collections.Generic`, `System.IO`, `System.Linq`, `System.Threading`, `System.Threading.Tasks`.
- UI references Detection and Migration but `ShellViewModel` only uses `Core` and `Core.Models`.

## Requirements

- [x] All three files cleaned up
- [x] No build errors introduced
- [x] Tests still pass

## Verification

- [x] `dotnet build` passes (0 errors)
- [x] `dotnet test` passes (99 tests)

## Completion Notes

- **Completed:** 2026-07-19
- **What was done:** Removed self-referencing `using GamingCommander.Core.Models;` from GameRecord.cs. Removed redundant `using System;` from Program.cs. Removed unnecessary Detection and Migration project references from UI.csproj.
- **Verification:** Build clean, 99 tests passing.
- **Issues encountered:** None.
