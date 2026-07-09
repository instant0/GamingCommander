# META/SESSION/CURRENT.md — Current Project State

**Nature:** Scratch. **Overwritten** every session handoff.
**Audience:** All agents. Read every session.

---

## Phase
**Keyboard Layout Optimization + VFS Cache + Future-Proofing** — UX improvements and architecture hardening

## Objective
1. ✅ Research F-key conventions across NC/TC/Windows/game launchers — propose optimal layout
2. ✅ Implement new keyboard layout: Enter launches games, F4=Edit/Retag, F6=Rescan, F7=Add Root, Esc=Go up, double-tap launches
3. ✅ Command bar shows all 10 F-keys in sequential order
4. ✅ Audit all filesystem access patterns — confirm no writes to game folders during VFS navigation
5. ✅ Implement in-memory cache for GamesDatabaseService to eliminate redundant disk reads during UI navigation
6. ✅ Create plan for build versioning + version-aware re-wizard + online metadata config option

## Completed This Session

### Keyboard Layout Overhaul
Implemented the full proposal from `planning/92-keyboard-layout-proposal.md` across 4 files:

**ShellViewModel.cs:**
- Added `RequestLaunch` event — game files fire this instead of being silently ignored
- `NavigateInto()` now launches on `Kind=File` (Enter works on game entries), drills in on `Kind=Directory`
- Added `ApplyRescannedGames()` for F6 rescan support
- Updated `Commands` collection to show all 10 F-keys in order (F1-F10, no gaps)
- Updated `InteractionHint` to reference F4 (not T), F5, Esc, etc.

**MainWindow.axaml.cs:**
- Wired `RequestLaunch` → `LaunchSelectedGameAsync()` in constructor
- Added F4 → `OpenGameSetupAsync()` (retag — was hidden T key only)
- Added F6 → `RefreshCurrentRootAsync()` (rescan current root via FolderScanner)
- Added F7 → `AddRootAsync()` (uses modern StorageProvider API instead of deprecated OpenFolderDialog)
- Added Escape → `NavigateUp()` (parallel to Backspace)
- `LeftListBox_DoubleTapped` now launches games on double-tap (was no-op for games)
- `CommandButtonPressed` handles all 10 command-bar buttons
- `RefreshCurrentRootAsync()` returns `Task` synchronously (no unnecessary `async`)
- Added `using System.Linq` and `using Avalonia.Platform.Storage`

**MainWindow.axaml:**
- Updated hint text: "Press F4 to edit this entry's type/tags" (was "Press T to tag...")

### In-Memory VFS Cache
`GamesDatabaseService` now caches the database in-memory after first `Load()`:
- `_cachedDb` field: populated on first `Load()` from disk
- Subsequent `Load()` calls return cached instance — zero disk IO
- `Save()` updates cache first, then persists to disk
- All mutation methods (`AddRoot`, `RemoveRoot`, `RescanRoot`, etc.) already call `Save()` — cache stays in sync transparently
- Pattern analysis confirmed all mutation code creates new objects (`with` + `ToList()`) rather than mutating in-place, so cache integrity is maintained

### Filesystem Safety Audit
Complete audit of all filesystem access:
- **All writes go to `data/` directory only** — `data/games.json`, `data/settings.json`, `data/startup.log`
- **Zero writes to game library folders** — `File.Move`/`File.Copy`/`File.Delete` don't exist anywhere
- **Zero writes during UI navigation** — no IO except the now-cached `data/games.json`
- **All game-folder reads happen only during explicit scanning** (AddRoot, Refresh, F2 Setup, F6 Rescan)
- **SyncMove/ACF manifest modification** — exists only in planning doc `04-phase-2-syncmove.md`; `DesignTimeMigrationPlanner` returns `IsDryRunOnly: true` stub; no write code exists
- **`ManifestPath`** is currently read-only metadata (stored string), used only for display and Epic manifest detection during scanning

### Plan Created
`planning/93-in-memory-cache-and-wizard-versioning.md` covers:
- Part A: In-memory cache (✓ implemented)
- Part B: Build versioning via `Directory.Build.props` + `AppConfig.LastSeenVersion`
- Part C: Version-aware re-wizard triggering (shows "what's new" on version bump)
- Part D: Online metadata config option (`EnableOnlineMetadata`)
- Part E: Metadata expansion over time (fields vs phases)
- Implementation order with 8 steps

## Test Status
**17 tests passing** (5 Core + 1 Migration + 11 App). Build clean, 0 errors.

## Key Architecture Decisions
- **VFS is hot in memory** — GamesDatabaseService caches the full database after first read. No disk access during navigation.
- **Enter on game = launch** — the single most impactful UX fix. `NavigateInto()` delegates to `RequestLaunch` for File entries.
- **F-key bar shows all 10 keys** — no visual gaps. WrapPanel handles 2-line layout automatically.
- **StorageProvider replaces OpenFolderDialog** — uses modern Avalonia 11 API.
- **Cache-before-write** in `Save()` ensures even if disk write fails, in-memory state is correct.

---

**Next session: Read META/SESSION/NEXT.md before starting.**
