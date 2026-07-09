# META/SESSION/NEXT.md — Next Action

**Nature:** Scratch. **Overwritten** every session handoff.
**Audience:** Builder. Read before implementing.

---

## ✅ COMPLETED: Keyboard Layout + VFS Cache + Audit

### Keyboard Layout
- Enter launches games (was: nothing)
- F4=Edit/Retag (was: hidden T key)
- F6=Rescan current root (was: unused)
- F7=Add Root via folder picker (was: F2 Settings only)
- Esc=Go up (parallel to Backspace)
- Double-tap launches games (was: no-op for games)
- Command bar shows all 10 F-keys in order
- T key still works as legacy shortcut

### VFS Cache
- `GamesDatabaseService` caches in memory after first `Load()`
- Zero IO during UI navigation — only cache hits
- `Save()` updates cache before disk write

### Filesystem Safety Audit
- No writes to game library folders anywhere
- SyncMove/ACF patching exists only in planning docs — no write code exists
- All game-folder reads are bounded to explicit scanning operations

### Plan Created
- `planning/93-in-memory-cache-and-wizard-versioning.md`
- Covers: build versioning, version-aware re-wizard, online metadata config option, metadata expansion phases

---

## What Comes Next (Priority Order)

### Step 1: Build Versioning + Re-Wizard System
Implement Steps 2-8 from `planning/93-in-memory-cache-and-wizard-versioning.md`:
- Add `<Version>` to `Directory.Build.props`
- Add `LastSeenVersion` + `EnableOnlineMetadata` to `AppConfig`
- Wire version comparison in `App.axaml.cs`
- Update `JsonConfigService`, `WizardViewModel`, `LibrarySetupViewModel`
- Add online metadata checkbox to wizard UI
- Build and test

### Step 2: User Blacklist Editor
See `planning/91-user-blacklist-editor.md`:
- Hotkey to add/remove blacklist patterns from VFS
- 6 user categories with separate persistence file
- Dimmed/greyed VFS display for blacklisted items

### Step 3: Metadata Lookup (F3/View)
- PE metadata extraction (FileDescription, ProductName, CompanyName)
- PCGW Cargo API queries with rate limiting
- Store-first architecture: Epic → Steam → GOG → PCGW enrichment

### Future: SyncMove (Phase 2.1)
- Scan-time mismatch detection
- F6 repair dialog with dry run
- ACF/Manifest backup-before-write
- See `planning/04-phase-2-syncmove.md`

### Known Leftover Issues
- `DesignTimeLibraryManager.cs` still exists (dead code, no callers)
- `GamingCommander.Detection` project is empty
- `DesignTimeMigrationPlanner` created in `App.axaml.cs` but unused
- No test coverage for `LibraryManager`, `BlacklistLoader`, or `GamesDatabaseService`
