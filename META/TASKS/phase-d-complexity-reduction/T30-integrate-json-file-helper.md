# Task T30: Integrate JsonFileHelper into Services

**Tier:** 2 — Code Structure
**Phase:** D — Complexity Reduction
**Effort:** ~20 min
**Risk:** Low
**Status:** pending
**Prerequisites:** T17 (JsonFileHelper extracted)

---

## Objective

After T17 creates `JsonFileHelper`, integrate it into all three JSON-based services (`GamesDatabaseService`, `JsonConfigService`, `BlacklistLoader`) to replace their manual file I/O code. This completes the DRY extraction started in T17.

## What Needs to Change

### 1. `src/GamingCommander.App/Services/GamesDatabaseService.cs`

**Current state:** Contains manual JSON read/write logic (lines 28-114)
**Actions:**
- [ ] Replace `Load()` body with:
  ```csharp
  public GamesDatabase Load()
  {
      if (_cachedDatabase is not null)
          return _cachedDatabase;

      GamesDatabaseDto? dto = JsonFileHelper.ReadFromFile<GamesDatabaseDto>(
          _dbPath,
          () => new GamesDatabaseDto { Roots = [] });

      _cachedDatabase = MapToDomain(dto!);
      return _cachedDatabase;
  }
  ```
- [ ] Replace `Save()` body with:
  ```csharp
  public void Save(GamesDatabase database)
  {
      GamesDatabaseDto dto = MapToDto(database);
      JsonFileHelper.WriteToFile(_dbPath, dto);
      _cachedDatabase = database;
  }
  ```
- [ ] Keep `MapToDomain` and `MapToDto` methods — they're service-specific
- [ ] Remove any remaining `JsonSerializerOptions` references (now in JsonFileHelper)

### 2. `src/GamingCommander.App/Services/JsonConfigService.cs`

**Current state:** Contains manual JSON read/write logic (lines 26-102)
**Actions:**
- [ ] Replace `Load()` body with:
  ```csharp
  public AppConfig Load()
  {
      AppConfigDto? dto = JsonFileHelper.ReadFromFile<AppConfigDto>(
          _configPath,
          () => new AppConfigDto());

      return MapToDomain(dto!);
  }
  ```
- [ ] Replace `Save()` body with:
  ```csharp
  public void Save(AppConfig config)
  {
      AppConfigDto dto = MapToDto(config);
      JsonFileHelper.WriteToFile(_configPath, dto);
  }
  ```
- [ ] Keep `MapToDomain` and `MapToDto` methods

### 3. `src/GamingCommander.App/Services/BlacklistLoader.cs`

**Current state:** Contains manual JSON read logic (lines 32-64)
**Actions:**
- [ ] Replace `Load()` body with:
  ```csharp
  public BlacklistData Load()
  {
      string blacklistPath = Path.Combine(_basePath, "data", "blacklist.json");
      BlacklistData? data = JsonFileHelper.ReadFromFile<BlacklistData>(
          blacklistPath,
          () => new BlacklistData());

      // Fallback to hardcoded defaults if file is empty/missing
      if (data?.ExeNamePatterns.Count == 0)
      {
          data = data with
          {
              ExeNamePatterns = FolderScanner.DefaultNoiseExePatterns.ToList()
          };
      }

      return data!;
  }
  ```
- [ ] Remove any remaining `JsonSerializerOptions` references

## Context

- T17 creates `JsonFileHelper` with `ReadFromFile<T>` and `WriteToFile<T>`
- This task integrates those utilities into the three services
- Each service keeps its own DTO mapping logic (that's service-specific)
- The fallback logic in BlacklistLoader stays (hardcoded defaults when file is missing)

## Requirements

- [ ] `GamesDatabaseService.Load()` uses `JsonFileHelper.ReadFromFile`
- [ ] `GamesDatabaseService.Save()` uses `JsonFileHelper.WriteToFile`
- [ ] `JsonConfigService.Load()` uses `JsonFileHelper.ReadFromFile`
- [ ] `JsonConfigService.Save()` uses `JsonFileHelper.WriteToFile`
- [ ] `BlacklistLoader.Load()` uses `JsonFileHelper.ReadFromFile`
- [ ] No manual `File.ReadAllText` + `JsonSerializer.Deserialize` remains in services
- [ ] No `JsonSerializerOptions` defined in services (only in JsonFileHelper)

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -c "File.ReadAllText" src/GamingCommander.App/Services/` returns 0 (no manual reads)
- [ ] `grep -c "File.WriteAllText" src/GamingCommander.App/Services/` returns 0 (no manual writes)
- [ ] `grep -c "JsonSerializerOptions" src/GamingCommander.App/Services/` returns 1 (only in JsonFileHelper)

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
