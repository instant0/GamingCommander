# Plan 111: Logging Toggle + Online Connectivity Docs + Metadata Source Tracking

## Context

The startup logging feature is currently controlled only by the `GC_STARTUP_LOGGING` environment variable. Users should be able to toggle it from the F2 Setup screen like the online metadata toggle. The readme needs an online connectivity section explaining what the app connects to (when online features are enabled). The metadata source tracking question needs evaluation.

**Related plans:**
- `planning/110-user-tags-source-tagging.md` — UserOverrides, TitleSource = "User", Developer field, Favourites/Completed, Random game, Metadata enrichment UI

## Task 1: Add "Enable Startup Logging" toggle to F2 Setup

### What changes

**AppConfig** (`src/GamingCommander.Core/Models/AppConfig.cs`):
- Add `bool EnableStartupLogging = true` parameter (default ON for backward compatibility)

**JsonConfigService** (`src/GamingCommander.App/Services/JsonConfigService.cs`):
- Add `EnableStartupLogging` to `ConfigDto` class
- Map in `Load()` and `Save()` methods

**LibrarySetupViewModel** (`src/GamingCommander.App/ViewModels/LibrarySetupViewModel.cs`):
- Add `_enableStartupLogging` backing field + `EnableStartupLogging` reactive property
- Load from config in constructor
- Persist in `Close()` method

**LibrarySetupWindow.axaml** (`src/GamingCommander.App/LibrarySetupWindow.axaml`):
- Add checkbox below the online metadata checkbox, same style:
  ```xml
  <CheckBox IsChecked="{Binding EnableStartupLogging}" ...>
    Enable startup logging <Run ...>(data/startup.log)</Run>
  </CheckBox>
  ```

**App.axaml.cs** (`src/GamingCommander.App/App.axaml.cs`):
- Change `StartupLoggingEnabled` from env-var-only to: check `AppConfig.EnableStartupLogging` first, fall back to env var
- Load config early in `Initialize()` before logging calls begin
- The env var `GC_STARTUP_LOGGING=0` should still override (power user escape hatch)

### Why default ON
- Backward compatible: existing users see no change
- Log file is small (~2-5KB per launch)
- Users who want to disable it can do so from F2

## Task 2: Update readme with online connectivity section

### What changes

**GamingCommander.Readme.txt**:
Add a new section "Online Connectivity" after the permissions section. Content:

- **Current status**: The application is offline-only. No network requests are made.
- **When online features are enabled** (future, not yet implemented):
  - **Steam Store API**: `https://store.steampowered.com/api/appdetails?appids={appid}` — 1 request per game with a Steam App ID, rate-limited to 1 request per 10 seconds. Sends: App ID number. Receives: game name, developers, publishers, genres, release date, Metacritic score, header image URL.
  - **PCGamingWiki API**: `https://www.pcgamingwiki.com/w/api.php?action=cargoquery` — 1 request per game, rate-limited to 0.6 seconds between calls. Sends: game name or Steam App ID. Receives: developers, publishers, genres, engine, modes, perspectives, themes, cover art URL, official website.
  - **PCGamingWiki AppID resolution**: `https://www.pcgamingwiki.com/api/appid.php?appid={appid}` — 1 request per Steam game. Sends: App ID. Receives: PCGW page title for game.
  - **Epic Games Store GraphQL** (future): `https://store.epicgames.com/graphql` — 1 request per Epic game without a local manifest. Sends: catalog namespace ID. Receives: game title, description, publisher.
  - **IGDB** (future, requires API key): `https://api.igdb.com/v4/games` — 1 request per game not resolved by above. Sends: game name query. Receives: all metadata fields.
- **What is sent**: Only game identifiers (App IDs, game names). No personal data, no file contents, no system information.
- **Caching**: All responses cached locally in `data/games_metadata.json`. Subsequent launches use cache, no redundant requests.
- **Estimated total requests**: For a 100-game library, approximately 200-300 HTTP requests on first scan (across all providers). After caching, zero requests on subsequent launches unless metadata is stale (>30 days).

## Task 3: Metadata source tracking evaluation

### Current state

Source tracking exists ONLY for game titles, and only for GOG and EA games:
- `PlatformMetadata["TitleSource"]` = `"GogInfo"` or `"EaInstallLog"`
- `PlatformMetadata["AutoDetectedTitle"]` = the folder-name fallback

Steam games have NO title source tracking. No other fields (developer, publisher, engine, etc.) have source tracking because those fields don't exist yet in the model.

### Plan 102's approach

Plan 102 proposes `LastMetadataSource` — a single string on `GameMetadataRecord` indicating which provider was used most recently. This is provider-level tracking, not per-field tracking.

### Evaluation: Is per-field source tracking useful?

**Arguments FOR per-field tracking:**
- When merging from 3+ providers (Steam, PCGW, IGDB), knowing "name came from Steam, engine came from PCGW" helps debug quality issues
- Could display provenance in F4 edit screen ("Name: The Witcher 3 (from Steam Store)")
- Enables smarter merge: if user trusts Steam names but prefers PCGW engines, could auto-resolve

**Arguments AGAINST per-field tracking:**
- Adds complexity to the data model (every field needs a companion `*Source` field)
- The merge strategy is priority-based: higher-priority provider wins for non-null fields. Source is implicit from priority order.
- Users rarely care *where* metadata came from — they care if it's *correct*
- The `UserOverrides` system (Plan 110) already handles the "user knows better" case
- Storage overhead: N extra strings per game entry

### Recommendation

**Track source at the record level (Plan 102's approach), not per-field.** Add one field:

```csharp
string? LastMetadataSource;  // "SteamStore", "PCGW", "IGDB", "Local", "User"
```

This is sufficient because:
1. The priority-based merge means higher-priority sources always win — source is deterministic from the merge order
2. If debugging is needed, log the full merge details to `data/metadata_merge.log` (debug mode only)
3. User overrides (Plan 110) protect manually-set fields regardless of source

**Exception**: Track `TitleSource` for Steam games too (currently missing). Add to `SteamLibraryScanner`:
```csharp
platformMetadata["TitleSource"] = "AcfName";
```

This is a one-line change that completes the existing pattern.

### What changes (minimal)

**SteamLibraryScanner** (`src/GamingCommander.App/Services/SteamLibraryScanner.cs`):
- Add `platformMetadata["TitleSource"] = "AcfName";` after line ~227 where `displayName` is set from ACF

**GameSetupWindow.axaml.cs** (`src/GamingCommander.App/GameSetupWindow.axaml.cs`):
- In `SaveAndClose()`, detect if `DisplayName` changed and set `PlatformMetadata["TitleSource"] = "User"` — see Plan 110 §9 for details

**GamingCommander.Readme.txt**:
- No change needed — source tracking is an internal detail, not a user-facing permission

## Files affected

| File | Change |
|------|--------|
| `src/GamingCommander.Core/Models/AppConfig.cs` | Add `EnableStartupLogging` field |
| `src/GamingCommander.App/Services/JsonConfigService.cs` | Add DTO field + mapping |
| `src/GamingCommander.App/ViewModels/LibrarySetupViewModel.cs` | Add property + load/persist |
| `src/GamingCommander.App/LibrarySetupWindow.axaml` | Add checkbox |
| `src/GamingCommander.App/App.axaml.cs` | Use config instead of env-var-only |
| `src/GamingCommander.App/Services/SteamLibraryScanner.cs` | Add `TitleSource` for Steam |
| `src/GamingCommander.App/GameSetupWindow.axaml.cs` | Add `TitleSource = "User"` on title edit |
| `GamingCommander.Readme.txt` | Add online connectivity section |

## Success criteria

- [ ] F2 Setup shows "Enable startup logging" checkbox, defaults ON
- [ ] Toggle persists to `settings.json` and takes effect on next launch
- [ ] `GC_STARTUP_LOGGING=0` env var still overrides the setting
- [ ] Steam games have `TitleSource = "AcfName"` in PlatformMetadata
- [ ] F4 title edit sets `TitleSource = "User"` in PlatformMetadata
- [ ] Readme has complete online connectivity section with URLs, rates, data sent/received
- [ ] All 227 existing tests pass
