# In-Memory Cache, Build Versioning & First-Run Wizard Evolution

## Current State

### Problem: No VFS cache
`GamesDatabaseService.Load()` reads `data/games.json` from disk on every call.
This is called on **every** `NavigateInto()` drill-in, plus every mutation
(AddRoot, RemoveRoot, RescanRoot, RetagGame, etc.). The file is small (~50 KB)
but the IO is unnecessary — the database is the VFS and should live in memory.

### Problem: No build version tracking
The `IsFirstRun` flag in `AppConfig` is a simple boolean. Once set to `false`,
it stays `false` forever. New features added in future builds have no way to
re-trigger the wizard or notify the user of new configuration options.

### Problem: Online metadata has no config switch
Metadata enrichment (online lookup, PE analysis) is not yet implemented, but
when it is, it should be opt-in. There's no configuration flag for it yet.

---

## Part A: In-Memory Cache for GamesDatabaseService (IMMEDIATE)

### Goal
Eliminate disk reads during UI navigation. GamesDatabaseService holds the
VFS (Virtual File System) — it should be hot in memory.

### Design

```
┌─ GamesDatabaseService ─────────────────────────────────┐
│                                                        │
│  ┌──────────────┐    Load() → return cache             │
│  │  _cachedDb   │ ──────────────────────────►          │
│  │  (in memory) │    Save(db) → cache = db + disk I/O  │
│  └──────────────┘                                      │
│                                                        │
│  • Load()  → if _cachedDb is null: read disk → cache   │
│              else: return _cachedDb                     │
│  • Save()  → _cachedDb = db → write disk               │
│  • GetGamesForRoot() → from _cachedDb (no disk IO)     │
│  • All mutation methods (AddRoot, RemoveRoot, …)        │
│    already call Save() internally → cache stays in sync │
└────────────────────────────────────────────────────────┘
```

### Thread safety
Not required — Avalonia runs all ViewModel mutations on the UI thread.
If a background thread is used (e.g. scanning), the cache is populated
before the background operation returns.

### File changed
- `src/GamingCommander.App/Services/GamesDatabaseService.cs`

### Risk
If a user manually edits `data/games.json` while the app is running, the
cache is stale. Mitigation: the app owns this file and only writes to it
through `GamesDatabaseService`. Manual edits while running are unlikely.
If this becomes a problem, we can add a file-watch invalidation later.

---

## Part B: Build Versioning (Plan)

### Goal
Every build has a version number. The config stores the last-seen version.
On startup, if the config version is older than the current build version,
the new-version wizard is shown.

### Components

#### 1. Add `Version` to `Directory.Build.props`

```xml
<Version>0.3.0</Version>
<FileVersion>0.3.0.0</FileVersion>
```

This makes `[assembly: AssemblyVersion("0.3.0.0")]` available at runtime
via `typeof(App).Assembly.GetName().Version`.

#### 2. Add `LastSeenVersion` to `AppConfig`

```csharp
public sealed record AppConfig(
    IReadOnlyList<LibraryRoot> LibraryRoots,
    IReadOnlyList<FolderOverride> FolderOverrides,
    IReadOnlyList<string> HiddenFolders,
    bool IsFirstRun,
    string? LastSeenVersion,        // NEW
    bool EnableOnlineMetadata);      // NEW
```

- `LastSeenVersion`: nullable string, e.g. `"0.3.0"`. Null = never seen any version.
- `EnableOnlineMetadata`: bool, default false. Config switch for online metadata lookup.

#### 3. Version comparison on startup

In `App.OnFrameworkInitializationCompleted()`:

```csharp
string currentVersion = typeof(App).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
bool needsWizard = config.IsFirstRun 
                || config.LastSeenVersion is null 
                || CompareVersions(config.LastSeenVersion, currentVersion) < 0
                || config.LibraryRoots.Count == 0;
```

The version string comparison uses semantic versioning (Major.Minor.Patch).

#### 4. After wizard, persist version

```csharp
config = config with { 
    IsFirstRun = false, 
    LastSeenVersion = currentVersion 
};
configService.Save(config);
```

### File changes
- `Directory.Build.props` — add Version
- `src/GamingCommander.Core/Models/AppConfig.cs` — add LastSeenVersion, EnableOnlineMetadata
- `src/GamingCommander.App/Services/JsonConfigService.cs` — serialize new fields
- `src/GamingCommander.App/App.axaml.cs` — use version comparison for wizard trigger
- `src/GamingCommander.App/ViewModels/WizardViewModel.cs` — persist version on finish/cancel
- `src/GamingCommander.App/ViewModels/LibrarySetupViewModel.cs` — preserve version in Save calls

---

## Part C: Version-Aware Re-Wizard Triggering

### When the wizard re-appears

| Scenario | Trigger | Shows |
|----------|---------|-------|
| First launch, no config | `LastSeenVersion == null` | Full wizard |
| Config exists, same build | `LastSeenVersion == currentVersion` | Nothing |
| Config from older build | `LastSeenVersion < currentVersion` | "What's new" screen + new options |
| Config from newer build (downgrade) | `LastSeenVersion > currentVersion` | Nothing (don't nag) |
| User manually resets | Delete config or set `LastSeenVersion = null` | Full wizard |

### "What's New" screen

When the version bumps, the wizard shows a version-aware intro page:

```
┌──────────────────────────────────────┐
│  GamingCommander v0.4.0              │
│                                      │
│  What's new since you last ran:      │
│                                      │
│  ✓ Online metadata lookup            │
│  ✓ Improved game discovery           │
│  ✓ In-memory cache for faster nav    │
│                                      │
│  Your config has been preserved.     │
│  New options are highlighted.        │
│                                      │
│            [ Continue ]              │
└──────────────────────────────────────┘
```

New config options are highlighted in the settings UI with a "NEW" badge
when `config.LastSeenVersion < currentVersion`.

---

## Part D: Online Metadata Configuration Option

### Config field

```csharp
bool EnableOnlineMetadata  // default: false
```

### Where it's surfaced

1. **First-run wizard**: A checkbox on the final page:
   ```
   ☐ Enable online metadata lookup
      (game covers, descriptions, ratings from PCGamingWiki and other sources)
   ```

2. **Settings UI** (F2 → Library Setup → Settings tab):
   ```
   Online Metadata: [Enabled / Disabled]
   ```

3. **Startup check**: If wizard wasn't shown, no metadata is fetched.
   If enabled, metadata tasks run after VFS is loaded.

### When metadata runs

Metadata enrichment is a separate phase from game scanning:

```
┌─ Startup Sequence ─────────────────────────────────┐
│                                                     │
│  1. Load config (settings.json)                     │
│  2. Load VFS (games.json → cache)                   │
│  3. Check version → maybe show wizard               │
│  4. If EnableOnlineMetadata:                        │
│       → Queue background metadata fetch             │
│       → On completion: update VFS cache + save      │
│  5. Show main window                                │
│                                                     │
│  Metadata never runs during UI navigation.           │
│  It's always a deliberate phase (scan or background).│
└─────────────────────────────────────────────────────┘
```

---

## Part E: Metadata Expansion Over Time

The `GameEntry` record will grow as new detection capabilities are added.
Each field is populated by a specific phase:

| Field | Populated By | Phase |
|-------|-------------|-------|
| `Id`, `FolderName`, `DisplayName` | FolderScanner | Phase 0 (scanning) |
| `ExecutablePath`, `LauncherPath` | FolderScanner | Phase 0 |
| `GameSource`, `Override` | FolderScanner + PE scanner | Phase 0 + Phase 1 |
| `ManifestPath` | FolderScanner | Phase 0 |
| `LastScanned`, `LastModified` | FolderScanner | Phase 0 |
| `PcGamingWikiId` | Online lookup | Phase 2 (future) |
| `CoverUrl`, `Description` | Online lookup | Phase 2 (future) |
| `Categories` | PE analysis + online | Phase 1 + 2 (future) |
| `PESignatureInfo` | PE scanner | Phase 1 (future) |

Each phase is independent. Fields default to empty/null until their phase
runs. The VFS cache persists whatever fields have been populated.

---

## Implementation Order

| Step | What | Dependencies |
|------|------|-------------|
| 1 | **Cache in GamesDatabaseService** | None — do now |
| 2 | Add Version to Directory.Build.props | None |
| 3 | Add LastSeenVersion + EnableOnlineMetadata to AppConfig | Step 2 |
| 4 | Wire version comparison in App.axaml.cs | Step 3 |
| 5 | Update WizardViewModel to persist version | Step 3 |
| 6 | Update LibrarySetupViewModel to preserve new fields | Step 3 |
| 7 | Add online metadata checkbox to wizard UI | Step 3 |
| 8 | Build and test | Step 1-7 |

---

## Files Changed (Summary)

| File | Change |
|------|--------|
| `Directory.Build.props` | Add `<Version>0.3.0</Version>` |
| `Core/Models/AppConfig.cs` | Add `LastSeenVersion`, `EnableOnlineMetadata` |
| `App/Services/JsonConfigService.cs` | Serialize/deserialize new fields |
| `App/Services/GamesDatabaseService.cs` | Add `_cachedDb` field |
| `App/App.axaml.cs` | Version comparison logic |
| `App/ViewModels/WizardViewModel.cs` | Persist version; add online metadata toggle |
| `App/ViewModels/LibrarySetupViewModel.cs` | Preserve new fields in Save calls |
