# Task T17: Extract JsonFileHelper Utility

**Tier:** 2 — Code Structure
**Phase:** D — Complexity Reduction
**Effort:** ~25 min
**Risk:** Minimal
**Status:** pending
**Prerequisites:** None

---

## Objective

Three services (`GamesDatabaseService.cs`, `JsonConfigService.cs`, `BlacklistLoader.cs`) all implement the same JSON file read/write pattern: check file exists, read text, deserialize, handle errors. Two of them also share identical `JsonSerializerOptions` configuration. Extract the shared JSON operations into a utility class.

## What Needs to Change

### 1. New file: `src/GamingCommander.App/Services/JsonFileHelper.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create `JsonFileHelper.cs` with namespace `GamingCommander.App.Services`
- [ ] Add `/// <summary>` to class: "Shared JSON file serialization and deserialization operations."
- [ ] Add shared `JsonSerializerOptions` property:
  ```csharp
  internal static JsonSerializerOptions SharedJsonOptions { get; } = new()
  {
      WriteIndented = true,
      PropertyNameCaseInsensitive = true
  };
  ```
  - Add `/// <summary>`: "Shared JSON serialization options used by all file-based services."
- [ ] Add `ReadFromFile<T>(string filePath, Func<T> defaultFactory)` method:
  ```csharp
  internal static T? ReadFromFile<T>(string filePath, Func<T> defaultFactory) where T : class
  {
      if (!File.Exists(filePath))
          return defaultFactory();
      try
      {
          string json = File.ReadAllText(filePath);
          T? result = JsonSerializer.Deserialize<T>(json, SharedJsonOptions);
          return result ?? defaultFactory();
      }
      catch
      {
          return defaultFactory();
      }
  }
  ```
  - Add `/// <summary>`: "Reads and deserializes a JSON file. Returns defaultFactory() result if file is missing, empty, or corrupt."
- [ ] Add `WriteToFile<T>(string filePath, T data)` method:
  ```csharp
  internal static void WriteToFile<T>(string filePath, T data) where T : class
  {
      string json = JsonSerializer.Serialize(data, SharedJsonOptions);
      string? directory = Path.GetDirectoryName(filePath);
      if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
          Directory.CreateDirectory(directory);
      File.WriteAllText(filePath, json);
  }
  ```
  - Add `/// <summary>`: "Serializes data to JSON and writes to file. Creates parent directory if it doesn't exist."
- [ ] Add `EnsureDirectoryExists(string filePath)` method:
  ```csharp
  internal static void EnsureDirectoryExists(string filePath)
  {
      string? directory = Path.GetDirectoryName(filePath);
      if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
          Directory.CreateDirectory(directory);
  }
  ```
  - Add `/// <summary>`: "Ensures the parent directory of the given file path exists."

### 2. `src/GamingCommander.App/Services/GamesDatabaseService.cs`

**Current state:** Lines 15-19 define `JsonOptions`, lines 28-77 implement read pattern, lines 80-114 implement write pattern
**Actions:**
- [ ] Delete the `JsonOptions` static property (lines 15-19)
- [ ] Replace read logic in `Load()` (lines 28-77) with call to `JsonFileHelper.ReadFromFile<GamesDatabaseDto>(_dbPath, () => new GamesDatabaseDto { Roots = [] })`
- [ ] Replace write logic in `Save()` (lines 80-114) with call to `JsonFileHelper.WriteToFile(_dbPath, dto)`
- [ ] Update all references from `JsonOptions` to `JsonFileHelper.SharedJsonOptions` if any remain
- [ ] Keep the DTO mapping logic — only replace the file I/O

### 3. `src/GamingCommander.App/Services/JsonConfigService.cs`

**Current state:** Lines 13-17 define `JsonOptions`, lines 26-71 implement read pattern, lines 75-102 implement write pattern
**Actions:**
- [ ] Delete the `JsonOptions` static property (lines 13-17)
- [ ] Replace read logic in `Load()` (lines 26-71) with call to `JsonFileHelper.ReadFromFile<AppConfigDto>(_configPath, () => new AppConfigDto())`
- [ ] Replace write logic in `Save()` (lines 75-102) with call to `JsonFileHelper.WriteToFile(_configPath, dto)`
- [ ] Keep the DTO mapping logic — only replace the file I/O

### 4. `src/GamingCommander.App/Services/BlacklistLoader.cs`

**Current state:** Lines 32-64 implement read pattern (no write)
**Actions:**
- [ ] Replace read logic in `Load()` (lines 32-64) with call to `JsonFileHelper.ReadFromFile<BlacklistData>(_blacklistPath, () => new BlacklistData())`
  - Note: `BlacklistData` is the DTO — it deserializes directly from the JSON file
- [ ] Keep the path construction logic — only replace the file I/O

## Context

- The read pattern appears 3 times with identical structure
- The write pattern appears 2 times with identical structure
- `JsonSerializerOptions` is defined identically in 2 files
- The DTO mapping logic in `GamesDatabaseService` and `JsonConfigService` is service-specific and stays in those files
- `BlacklistLoader` doesn't use DTOs — it deserializes `BlacklistData` directly

## Requirements

- [ ] `JsonFileHelper.cs` created with `SharedJsonOptions`, `ReadFromFile<T>`, `WriteToFile<T>`, `EnsureDirectoryExists`
- [ ] All methods have `/// <summary>` XML docs
- [ ] `GamesDatabaseService.cs` no longer defines `JsonOptions`
- [ ] `JsonConfigService.cs` no longer defines `JsonOptions`
- [ ] `BlacklistLoader.cs` uses `JsonFileHelper.ReadFromFile` instead of manual read pattern
- [ ] No behavior change — same error handling, same defaults

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -c "JsonSerializerOptions" src/` returns 1 (only in JsonFileHelper)
- [ ] `grep -r "File.ReadAllText.*JsonSerializer" src/` returns 0 (no manual read patterns remain in services)
- [ ] `grep -r "File.WriteAllText.*JsonSerializer" src/` returns 0 (no manual write patterns remain in services)

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
