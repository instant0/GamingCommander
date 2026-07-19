# Task T54: Cache AppTheme Brush Allocations

**Tier:** 4 — Code Quality
**Phase:** G — Code Quality & Tests
**Effort:** ~10 min
**Risk:** Minimal
**Status:** pending

---

## Objective

`AppTheme.Get()` creates a new `SolidColorBrush` on every property access. Since theme resources don't change at runtime, cache the brushes.

## What Needs to Change

### `src/GamingCommander.App/AppTheme.cs`
- [ ] Add a `Dictionary<string, SolidColorBrush>` cache field
- [ ] Modify `Get()` to check cache before allocating:
  ```csharp
  private static readonly Dictionary<string, SolidColorBrush> _cache = [];
  
  private static SolidColorBrush Get(string key)
  {
      if (_cache.TryGetValue(key, out var cached))
          return cached;
      
      if (Application.Current?.TryFindResource(key) is SolidColorBrush brush)
      {
          _cache[key] = brush;
          return brush;
      }
      return Brushes.White;
  }
  ```

## Context

- `AppTheme` properties like `TextPrimary`, `PaneBg`, etc. are accessed on every UI render
- Each access currently calls `Get()` which calls `TryFindResource()` and creates a new brush
- With 23 color properties, this creates significant GC pressure

## Requirements

- [ ] Brush instances cached after first access
- [ ] No behavior change

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
