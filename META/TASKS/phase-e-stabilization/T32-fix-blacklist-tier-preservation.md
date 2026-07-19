# Task T32: Fix BlacklistLoader Tier Preservation (Bug 6)

**Tier:** 3 — Logic/Behavior
**Phase:** E — Stabilization
**Effort:** ~40 min
**Risk:** Medium
**Status:** ✅ completed
**Prerequisites:** T17 (JsonFileHelper extracted)

---

## Objective

`BlacklistLoader.cs` currently flattens all 21 tiers of `exe_name_patterns` into a single `IReadOnlyList<string>`. This means a "tier_1 universal noise" exe (like `unins000.exe`) gets the same treatment as a "tier_17 store bootstrap" exe. Preserve tier information so scoring can apply per-tier severity.

## What Needs to Change

### 1. `src/GamingCommander.App/Services/BlacklistData.cs`

**Current state:** Contains `BlacklistData` record with `ExeNamePatterns` as `IReadOnlyList<string>`
**Actions:**
- [ ] Add a new record type:
  ```csharp
  /// <summary>
  /// A single exe name pattern with its severity tier.
  /// Tier 1 = highest severity (universal noise like uninstallers).
  /// Tier 21 = lowest severity (store bootstraps, rare edge cases).
  /// </summary>
  public sealed record BlacklistTierEntry(string Pattern, int Tier);
  ```
- [ ] Add new property to `BlacklistData`:
  ```csharp
  /// <summary>
  /// Exe name patterns organized by severity tier.
  /// Tier 1 = universal noise, Tier 21 = store bootstraps.
  /// </summary>
  public IReadOnlyList<BlacklistTierEntry> TieredExePatterns { get; init; } = [];
  ```
- [ ] Keep existing `ExeNamePatterns` property for backward compatibility (it will be populated from tiered data)

### 2. `src/GamingCommander.App/Services/BlacklistLoader.cs`

**Current state:** Lines 46-48 flatten tiers into a flat list
**Actions:**
- [ ] Parse the JSON tier structure properly — each tier has a `tier` field and `patterns` array
- [ ] Build `TieredExePatterns` list from the parsed tiers
- [ ] Still populate `ExeNamePatterns` as the flat list (backward compatibility)
  ```csharp
  // Backward-compatible flat list (all patterns, no tier info)
  var flatPatterns = tieredEntries.Select(t => t.Pattern).ToList();
  ```
- [ ] Add `/// <summary>` docs explaining the tier system

### 3. `src/GamingCommander.App/Services/FolderScanner.cs`

**Current state:** Uses `_noiseExePatterns` (flat list) for all noise checking
**Actions:**
- [ ] Add `_tieredNoiseExePatterns` field: `IReadOnlyList<BlacklistTierEntry>`
- [ ] Update constructor to accept `BlacklistData` and store both flat and tiered patterns
- [ ] Keep existing `IsNoiseExePattern` and `IsNoiseExeName` using flat list (no behavior change)
- [ ] Add new `GetExePatternTier(string pattern)` method for scoring:
  ```csharp
  /// <summary>
  /// Returns the severity tier for a noise pattern.
  /// Lower tier = higher severity. Returns 999 if pattern not found.
  /// </summary>
  internal int GetExePatternTier(string pattern)
  {
      var match = _tieredNoiseExePatterns.FirstOrDefault(t =>
          pattern.Contains(t.Pattern, StringComparison.OrdinalIgnoreCase));
      return match.Tier;
  }
  ```

## Context

- The JSON file `data/blacklist.json` has `exe_name_patterns` organized by tier
- Current loader flattens this to a flat list, losing tier information
- Tier 1 = universal noise (unins000, setup, installer) — always penalized heavily
- Tier 17+ = store bootstraps (epicgameslauncher, goggalaxy) — sometimes legitimate
- This fix preserves the tier data without breaking existing consumers
- The flat list is kept for backward compatibility with `IsNoiseExePattern` and `IsNoiseExeName`

## Requirements

- [x] `BlacklistData.TieredExePatterns` property exists with `/// <summary>` XML doc
- [x] `BlacklistLoader` populates `TieredExePatterns` from JSON tier structure
- [x] `BlacklistLoader` still populates flat `ExeNamePatterns` for backward compatibility
- [x] `FolderScanner` stores tiered patterns and has `GetExePatternTier` method
- [x] No behavior change for existing noise checking (flat list still used)
- [x] `BlacklistTierEntry` record has `/// <summary>` XML doc

## Verification

- [x] `dotnet build` passes (0 errors)
- [x] `dotnet test` passes (17 tests)
- [x] `grep -c "TieredExePatterns" src/` returns 3+ (definition + usage)
- [x] `grep -c "BlacklistTierEntry" src/` returns 3+ (definition + usage)
- [x] `grep -c "GetExePatternTier" src/` returns 1 (definition in FolderScanner)

## Completion Notes

- **Completed:** 2026-07-19
- **What was done:** Added `BlacklistTierEntry` record to BlacklistData.cs. Added `TieredExePatterns` property to `BlacklistData`. Updated `BlacklistLoader.Load()` to build tiered entries from JSON. Added `GetTieredTiers()` method to `ExeNamePatternsDto`. Updated `FolderScanner` to store `_tieredNoiseExePatterns` and added `GetExePatternTier()` method.
- **Verification:** Build clean, 17 tests passing.
- **Issues encountered:** Had to fix tuple destructuring in foreach loop. Fixed null reference warning by using foreach loop instead of LINQ FirstOrDefault.
