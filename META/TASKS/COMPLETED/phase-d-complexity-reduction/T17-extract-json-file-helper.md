# Task T17: Extract JsonFileHelper Utility

**Tier:** 2 — Code Structure
**Phase:** D — Complexity Reduction
**Effort:** ~25 min
**Risk:** Minimal
**Status:** ✅ completed
**Prerequisites:** None

---

## Objective

Three services (`GamesDatabaseService.cs`, `JsonConfigService.cs`, `BlacklistLoader.cs`) all implement JSON file read/write patterns. `GamesDatabaseService` and `JsonConfigService` share identical `JsonSerializerOptions`. Extract the shared JSON operations into a utility class with a parameterized options approach to handle BlacklistLoader's different needs.

## Key Finding: BlacklistLoader Options Differ

`BlacklistLoader.JsonOptions` has `ReadCommentHandling = JsonCommentHandling.Skip` and `AllowTrailingCommas = true` — the other two services don't have these. The `ReadFromFile<T>` method must accept an optional `JsonSerializerOptions` parameter.

## What Needs to Change

### 1. New file: `src/GamingCommander.App/Services/JsonFileHelper.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create `JsonFileHelper.cs` with namespace `GamingCommander.App.Services`
- [ ] Add `/// <summary>` to class: "Shared JSON file serialization and deserialization operations."
- [ ] Add shared `JsonSerializerOptions` property (for GamesDatabaseService and JsonConfigService):
  ```csharp
  /// <summary>
  /// Default JSON serialization options: indented output, case-insensitive property names.
  /// </summary>
  internal static JsonSerializerOptions DefaultOptions { get; } = new()
  {
      WriteIndented = true,
      PropertyNameCaseInsensitive = true
  };
  ```
- [ ] Add `ReadFromFile<T>(string filePath, Func<T> defaultFactory, JsonSerializerOptions? options = null)` method:
  - Uses `options ?? DefaultOptions` for deserialization
  - Returns `defaultFactory()` if file missing, empty, or corrupt
- [ ] Add `WriteToFile<T>(string filePath, T data)` method:
  - Uses `DefaultOptions` for serialization (write options are always the same)
  - Creates parent directory if it doesn't exist
- [ ] Add `EnsureDirectoryExists(string filePath)` method
- [ ] Add `/// <summary>` XML docs to all methods explaining purpose

### 2. Integration: `src/GamingCommander.App/Services/GamesDatabaseService.cs`

**Current state:** Lines 15-19 define `JsonOptions`, lines 28-77 read, lines 80-114 write.
**Actions:**
- [ ] Delete the `JsonOptions` static property (lines 15-19)
- [ ] Replace `Load()` body with `JsonFileHelper.ReadFromFile<GamesDatabaseDto>(_dbPath, () => new GamesDatabaseDto { Roots = [] })` + existing DTO mapping
- [ ] Replace `Save()` body with `JsonFileHelper.WriteToFile(_dbPath, dto)` + existing DTO mapping
- [ ] Keep `MapToDomain`/`MapToDto` logic — only replace file I/O

### 3. Integration: `src/GamingCommander.App/Services/JsonConfigService.cs`

**Current state:** Lines 13-17 define `JsonOptions`, lines 26-71 read, lines 75-102 write.
**Actions:**
- [ ] Delete the `JsonOptions` static property (lines 13-17)
- [ ] Replace `Load()` body with `JsonFileHelper.ReadFromFile<ConfigDto>(_configPath, () => new ConfigDto())` + existing DTO mapping
- [ ] Replace `Save()` body with `JsonFileHelper.WriteToFile(_configPath, dto)` + existing DTO mapping

### 4. Integration: `src/GamingCommander.App/Services/BlacklistLoader.cs`

**Current state:** Lines 13-18 define `JsonOptions` (different: adds `ReadCommentHandling`, `AllowTrailingCommas`), lines 32-64 read.
**Actions:**
- [ ] Delete the `JsonOptions` static property (lines 13-18)
- [ ] Create a local options instance for BlacklistLoader's read needs:
  ```csharp
  private static readonly JsonSerializerOptions BlacklistOptions = new()
  {
      PropertyNameCaseInsensitive = true,
      ReadCommentHandling = JsonCommentHandling.Skip,
      AllowTrailingCommas = true,
  };
  ```
  Wait — this defeats the purpose. Better approach: pass options to `ReadFromFile`.
- [ ] Replace `Load()` body with:
  ```csharp
  string jsonPath = Path.Combine(_basePath, "data", "blacklist.json");
  BlacklistDto? dto = JsonFileHelper.ReadFromFile<BlacklistDto>(
      jsonPath,
      () => new BlacklistDto(),
      BlacklistOptions);
  // ... existing tier-flattening logic stays
  ```
- [ ] Define `BlacklistOptions` as a local static field in `BlacklistLoader` (not in `JsonFileHelper`)
- [ ] Keep the tier-flattening logic — `BlacklistDto` JSON shape doesn't map directly to `BlacklistData`

## Context

- The read pattern appears 3 times with identical structure
- The write pattern appears 2 times with identical structure
- `JsonSerializerOptions` is defined identically in `GamesDatabaseService` and `JsonConfigService`
- `BlacklistLoader` needs comment-handling and trailing-comma tolerance — different options
- `BlacklistLoader` deserializes to `BlacklistDto` (nested tiers), not `BlacklistData` (flat list) — the tier-flattening logic stays
- This task absorbs the integration work from the original T30 (now merged)

## Requirements

- [ ] `JsonFileHelper.cs` created with `DefaultOptions`, `ReadFromFile<T>`, `WriteToFile<T>`, `EnsureDirectoryExists`
- [ ] `ReadFromFile<T>` accepts optional `JsonSerializerOptions` parameter
- [ ] All methods have `/// <summary>` XML docs
- [ ] `GamesDatabaseService.cs` no longer defines `JsonOptions`
- [ ] `JsonConfigService.cs` no longer defines `JsonOptions`
- [ ] `BlacklistLoader.cs` no longer defines its own `JsonOptions` — uses `ReadFromFile` with custom options
- [ ] No behavior change — same error handling, same defaults
- [ ] All three services still use their own DTO mapping logic

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -c "JsonSerializerOptions" src/` returns 2 (one in JsonFileHelper, one in BlacklistLoader for read-specific options)
- [ ] `grep -r "File.ReadAllText.*JsonSerializer" src/` returns 0 (no manual read patterns remain in services)
- [ ] `grep -r "File.WriteAllText.*JsonSerializer" src/` returns 0 (no manual write patterns remain in services)

## Completion Notes

- **Completed:** 2026-07-19
- **What was done:** Created `JsonFileHelper.cs` with `DefaultOptions`, `ReadFromFile<T>`, `WriteToFile<T>`, `EnsureDirectoryExists`. Integrated into GamesDatabaseService (removed JsonOptions, replaced Load/Save), JsonConfigService (removed JsonOptions, replaced Load/Save), BlacklistLoader (removed JsonOptions, added BlacklistOptions, replaced Load). Absorbed T30 integration work.
- **Verification:** Build clean (0 errors), 17 tests passing. JsonSerializerOptions defined in exactly 2 files (JsonFileHelper + BlacklistLoader). No manual File.ReadAllText/WriteAllText+JsonSerializer patterns remain in services.
- **Issues encountered:** None
