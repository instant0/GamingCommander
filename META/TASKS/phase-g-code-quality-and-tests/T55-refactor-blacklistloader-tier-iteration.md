# Task T55: Refactor BlacklistLoader Tier Iteration

**Tier:** 4 — Code Quality
**Phase:** G — Code Quality & Tests
**Effort:** ~25 min
**Risk:** Low
**Status:** pending

---

## Objective

`BlacklistLoader.GetTiers()` and `GetTieredTiers()` each contain 21 nearly identical yield-return blocks. The `ExeNamePatternsDto` has 21 individual properties. Refactor to data-driven iteration.

## What Needs to Change

### `src/GamingCommander.App/Services/BlacklistLoader.cs`
- [ ] Replace 21 individual `ExeNamePatternsDto` properties with a `Dictionary<int, List<string>> Tiers` property:
  ```csharp
  public sealed class ExeNamePatternsDto
  {
      public Dictionary<int, List<string>> Tiers { get; set; } = [];
  }
  ```
- [ ] Rewrite `GetTieredTiers()` as a simple loop:
  ```csharp
  public IEnumerable<BlacklistTierEntry> GetTieredTiers()
  {
      if (_exePatterns?.Tiers is null) yield break;
      foreach (var (tier, patterns) in _exePatterns.Tiers.OrderBy(t => t.Key))
      {
          if (patterns is { Count: > 0 })
              yield return new BlacklistTierEntry(tier, patterns);
      }
  }
  ```
- [ ] Remove `GetTiers()` method (replaced by `GetTieredTiers()`)
- [ ] Update JSON deserialization to handle the new dictionary format
- [ ] Verify `data/blacklist.json` format is compatible or update it

## Context

- Current implementation: 21 properties (`Tier01`, `Tier02`, ..., `Tier21`) each with individual yield-return
- New implementation: single `Dictionary<int, List<string>>` with a 5-line loop
- Reduces ~120 lines to ~15 lines
- JSON format change: from flat properties to nested dictionary — requires checking blacklist.json structure

## Requirements

- [ ] Tier iteration reduced to a simple loop
- [ ] 21 individual properties replaced with dictionary
- [ ] Existing tests still pass
- [ ] `BlacklistLoaderTests` tier preservation test still passes

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test --filter "FullyQualifiedName~BlacklistLoaderTests"` — all 11 tests pass
- [ ] `dotnet test` passes (99+ tests)

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
