# Task T21: Consolidate Noise-Check Methods in FolderScanner

**Tier:** 2 — Code Structure
**Phase:** D — Complexity Reduction
**Effort:** ~30 min
**Risk:** Low
**Status:** ✅ completed
**Prerequisites:** T16 (FileSystemHelper extracted)

---

## Objective

`FolderScanner.cs` has three noise-check methods with overlapping purpose:
- `IsNoiseExePattern` (static, line 601) — dead code, never called by any production code or test
- `IsNoiseExeName` (instance, line 617) — takes an already-extracted name string, checks against `_noiseExePatterns`
- `IsNonGameExe` (instance, line 622) — takes a full exe path, extracts filename, checks against `_noiseExePatterns`

Delete the dead static method. Keep the two instance methods with clearer names.

## What Needs to Change

### `src/GamingCommander.App/Services/FolderScanner.cs`

**Current state:** Three methods (lines 601-626):

| Method | Line | Type | Takes | Logic |
|--------|------|------|-------|-------|
| `IsNoiseExePattern` | 601 | `private static` | `string name` | Checks `DefaultNoiseExePatterns` (25 items) — **never called** |
| `IsNoiseExeName` | 617 | `private` (instance) | `string name` | Checks `_noiseExePatterns` (full JSON list) |
| `IsNonGameExe` | 622 | `private` (instance) | `string exePath` | Extracts filename from path, checks `_noiseExePatterns` |

**Actions:**
- [ ] **Delete** `IsNoiseExePattern` (lines 601-610) — dead code, zero callers
- [ ] **Rename** `IsNonGameExe` → `IsNoiseExeByPath` — clearer: takes a full path, extracts filename internally
  - Update 4 call sites in `FindExecutablesDeep()` (lines 460, 472, 482, 492)
- [ ] **Keep** `IsNoiseExeName` as-is — already has a clear name (takes an extracted name)
  - Update 2 call sites in `HasUnrealLayoutSignal` (line 380) and `HasRootExecutableSignal` (line 397) — no change needed, names stay the same
- [ ] **Add XML docs** to both remaining methods:
  - `IsNoiseExeName`: "Checks if an executable name (without extension) matches any noise pattern. Used by signal detection to filter non-game exes."
  - `IsNoiseExeByPath`: "Checks if an executable file path matches any noise pattern. Extracts the filename from the path before checking. Used by executable discovery and filtering."

### `src/GamingCommander.App/Services/SteamLibraryScanner.cs`

**Current state:** Line 414 has its own `IsNoiseExe(string)` static method with a hardcoded 7-item subset
**Actions:**
- [ ] **Leave as-is** — SteamLibraryScanner's `IsNoiseExe` is intentionally a smaller, Steam-specific subset
- [ ] Add `/// <summary>`: "Checks if an executable is a known Steam noise file (installer, uninstaller, etc.). Subset of the full noise list — Steam-specific."

## Context

- The static `IsNoiseExePattern` was created when Bug 5 was fixed but never wired up — it's dead code
- `IsNoiseExeName` and `IsNonGameExe` are NOT identical — one takes a name, the other takes a path
- The rename `IsNonGameExe` → `IsNoiseExeByPath` makes the distinction clear: one operates on names, one on paths
- `DefaultNoiseExePatterns` (the static list) is still used by the `FolderScanner()` no-arg constructor and as a fallback — keep the field, just remove the dead method
- After consolidation: two clear methods with distinct signatures and clear names

## Requirements

- [ ] Two noise-check methods remain in FolderScanner: `IsNoiseExeName` and `IsNoiseExeByPath`
- [ ] No static `IsNoiseExePattern` method remains (dead code removed)
- [ ] No `IsNonGameExe` method remains (renamed to `IsNoiseExeByPath`)
- [ ] All call sites updated to use the correct method names
- [ ] Both methods have `/// <summary>` XML docs
- [ ] No behavior change — same pattern matching logic

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -c "IsNoiseExePattern\|IsNoiseExeName\|IsNoiseExeByPath\|IsNonGameExe" src/GamingCommander.App/Services/FolderScanner.cs` returns 2 (two method definitions)
- [ ] `grep -c "IsNonGameExe" src/` returns 0 (old name removed)
- [ ] `grep -c "IsNoiseExePattern" src/GamingCommander.App/Services/FolderScanner.cs` returns 0 (dead method removed)

## Completion Notes

- **Completed:** 2026-07-19
- **What was done:** Deleted dead `IsNoiseExePattern`. Renamed `IsNonGameExe`→`IsNoiseExeByPath`. Kept `IsNoiseExeName`. Added XML docs to both remaining methods. Updated all call sites.
- **Verification:** Build clean, all tests passing.
- **Issues encountered:** None.
