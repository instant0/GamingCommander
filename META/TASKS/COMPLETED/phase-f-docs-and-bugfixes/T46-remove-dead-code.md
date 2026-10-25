# Task T46: Remove Dead Code

**Tier:** 2 — Cleanup
**Phase:** F — Docs & Bug Fixes
**Effort:** ~10 min
**Risk:** Minimal
**Status:** ✅ completed

---

## Objective

Two methods are defined but never called anywhere in the codebase. Remove them to reduce maintenance burden.

## What Needs to Change

### `src/GamingCommander.App/Services/SteamLibraryScanner.cs`
- [ ] Delete `CollectAllCommonFolderNames()` method (lines ~311-322)
- [ ] Verify no callers exist: `grep -r "CollectAllCommonFolderNames" src/`

### `src/GamingCommander.App/ViewModels/WizardViewModel.cs`
- [ ] Delete `AddRecommendedPaths()` method (lines ~149-169)
- [ ] Verify no callers exist: `grep -r "AddRecommendedPaths" src/`

### Cleanup:
- [ ] Remove any unused `using` statements that become orphaned after deletion
- [ ] Remove hardcoded paths (`@"D:\Games"`, `@"E:\Games"`) that were only in `AddRecommendedPaths`

## Context

- `CollectAllCommonFolderNames` was likely used during early development but superseded by the ACF-based approach.
- `AddRecommendedPaths` contains hardcoded Windows paths that were intended for a wizard feature that was never wired up.

## Requirements

- [x] Both methods deleted
- [x] No callers broken (verified by grep)
- [x] Build still passes

## Verification

- [x] `dotnet build` passes (0 errors)
- [x] `dotnet test` passes (99 tests)
- [x] `grep -r "CollectAllCommonFolderNames\|AddRecommendedPaths" src/` — returns nothing

## Completion Notes

- **Completed:** 2026-07-19
- **What was done:** Deleted `CollectAllCommonFolderNames()` from SteamLibraryScanner (lines 311-322) and `AddRecommendedPaths()` from WizardViewModel (lines 149-169). Both were never called.
- **Verification:** Build clean, 99 tests passing, grep confirms no references remain.
- **Issues encountered:** None.
