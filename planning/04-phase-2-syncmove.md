# Phase 2.1: SyncMove — Manifest Repair After User-Initiated File Relocation

> **⚠️ Design philosophy:** GamingCommander does NOT move game files. The user moves files
> using their OS tools (File Explorer, robocopy, etc.). GamingCommander's role is to detect
> that files have moved and offer to **repair the store registration** (manifests, ACF files,
> registry entries) so the launcher recognizes the game at its new location.
>
> This keeps the tool lightweight, avoids 100 GB file transfer complexity, eliminates
> junction/symlink fragility, and respects that the user owns their file system.

## Goal

After a user moves a game's files from one location to another (e.g. from `D:\games\game` to
`E:\games\game`), provide a scan-time detection and one-click repair of the launcher's
registration so the game continues to work without re-download or launcher reconfiguration.

## Background

When a user moves a game folder manually:

| Store | What Breaks | What Needs Repair |
|-------|-------------|-------------------|
| **Steam** | `installdir` in ACF still points to old folder (or ACF sits in old library root) | Update ACF `installdir` field; if cross-library, move ACF to new library's `steamapps/` |
| **Epic** | `.item` manifest has stale `InstallLocation` path | Update `InstallLocation` field in the `.item` JSON file |
| **GOG** | GOG Galaxy DB has stale path; `goggame-*.info` is self-contained in the game folder but GOG launcher doesn't re-scan | Update GOG's registry or database entry |
| **EA App** | Registry points to old install path | Update registry key |
| **Ubisoft Connect** | Cache JSON in Ubisoft data folder has stale path | Update cache file |
| **Standalone** | Nothing — no manifest to repair | No action needed |

## How It Works

### Detection at Scan Time

When the user runs a library scan (or the app rescans on startup), the scanner already
detects games by their signal files (`.egstore`, `steam_api64.dll`, `goggame-*`, etc. per
`docs/research/steam_emu_format.md`). For each detected game, the app checks:

```
Found game at:      E:\games\MyEpicGame\       (from scan)
Manifest says:      D:\games\MyEpicGame\       (from .item InstallLocation)
Status:              MISMATCH → needs repair
```

This check runs for every game that has a known `manifestPath` in `data/games.json`.

### F6 — SyncMove Dialog

When a mismatched game is selected, F6 opens the repair dialog showing:

```
┌─────────────────────────────────────────────┐
│  Game: My Epic Game                         │
│  Store: Epic                                │
│                                             │
│  Game files found at:                       │
│    E:\games\MyEpicGame\                     │
│                                             │
│  Manifest (.item) expects:                  │
│    D:\games\MyEpicGame\                     │
│                                             │
│  ┌──────────────────────────────────┐       │
│  │ [Fix Manifest to match files]    │       │
│  ├──────────────────────────────────┤       │
│  │ [Dry Run — show what would change]│      │
│  └──────────────────────────────────┘       │
│                                             │
│  Repair action: Update InstallLocation in   │
│  C:\ProgramData\Epic\...\manifests\xyz.item │
│    Old: D:\games\MyEpicGame\                │
│    New: E:\games\MyEpicGame\                │
│                                             │
│  Backup saved to: data/backups/xyz.item.bak │
└─────────────────────────────────────────────┘
```

### Launch-Time Check (Optional)

If a game fails to launch (exit code, missing files), the app can prompt:
> "Game files not found at registered location. Did you move them? Run scan to detect."

This is a future UX enhancement — Phase 2.1 scope is scan-time detection only.

## Manifest Repair Per Store

### Steam (ACF)

When a Steam game's files are moved:

#### Case 1: Same library, new folder name
```
Old: D:\SteamLibrary\steamapps\common\GameA
New: D:\SteamLibrary\steamapps\common\GameA_Renamed
```
- ACF still at `D:\SteamLibrary\steamapps\appmanifest_12345.acf`
- ACF `installdir` = `"GameA"` → update to `"GameA_Renamed"`
- **Action:** Update the `installdir` field in the ACF

#### Case 2: Different library root
```
Old: D:\SteamLibrary\steamapps\common\GameA    (library index 0)
New: E:\SteamLibrary\steamapps\common\GameA    (library index 1)
```
- ACF still at `D:\SteamLibrary\steamapps\appmanifest_12345.acf`
- Game folder is at `E:\SteamLibrary\steamapps\common\GameA`
- **Action:** Copy ACF from `D:\...\steamapps\` to `E:\...\steamapps\`, update `installdir`
  (keep original ACF as backup rather than delete — let Steam's VDF cache reconcile on restart)

#### Case 3: Moved outside all Steam libraries (became standalone)
```
Old: D:\SteamLibrary\steamapps\common\GameA
New: E:\NonSteam\GameA
```
- No Steam library root at `E:\NonSteam\`
- **Action:** Cannot repair ACF — Steam only recognizes games under `steamapps/common/`.
  Offer to mark the game as standalone. Game will appear as "Steam Emulator" or "Standalone"
  in the app. Steam will show it as uninstalled.

### Epic Games Store (.item JSON)

```
Old: D:\games\MyEpicGame\
New: E:\games\MyEpicGame\
```
- `.item` manifest at `C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests\<AppId>.item`
- Field to update: `"InstallLocation": "D:\\games\\MyEpicGame\\"` → `"E:\\games\\MyEpicGame\\"`
- **Action:** Read `.item` JSON, update `InstallLocation`, write (backup original first)
- Epic Games Store will re-scan on restart and detect the game at the new path

### GOG Galaxy

GOG stores install info in:
- `goggame-<id>.info` — lives inside the game folder (moves with the game — self-healing)
- Registry: `HKLM\SOFTWARE\WOW6432Node\GOG.com\Games\<id>\PATH`
- GOG Galaxy database: `%LOCALAPPDATA%\GOG.com\Galaxy\*.db` (SQLite)

**Action:**
1. Update registry key `PATH` to new location
2. Optionally update GOG Galaxy SQLite database if path can be identified

The `goggame-*.info` file is already at the correct location (it moved with the folder),
so the game's identity is preserved.

### EA App

EA App stores install paths in:
- Registry: `HKLM\SOFTWARE\WOW6432Node\EA Games\<game>\InstallDir` (or similar per-game)

**Action:** Update registry key to new path. Requires registry write access.

### Ubisoft Connect

Ubisoft Connect stores install info in:
- Cache files: `C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\cache\*.json` (or similar)
- `.ini` files in game directory (move with folder — self-healing)

**Action:** Update cache JSON file with new install path.

## Backup Strategy

Before any manifest modification:
1. Copy original manifest to `data/backups/<gameid>.<manifest_extension>.bak`
2. Log the operation to `data/migration_log.jsonl`:
   ```json
   {"timestamp": "...", "gameId": "...", "gameSource": "Epic",
    "oldPath": "D:\\games\\MyEpicGame", "newPath": "E:\\games\\MyEpicGame",
    "manifestPath": "C:\\ProgramData\\Epic\\...\\xyz.item",
    "action": "update_install_location", "backupPath": "data/backups/xyz.item.bak"}
   ```

## Constraints

- **No file movement.** The app never moves game files (`.exe`, `.dll`, data files).
  It only touches manifest files and registry entries.
- **No junction/symlink creation.** The user owns the file layout; the app just fixes the registration.
- **No raw file delete.** Original manifests are backed up, not deleted.
- **No manifest modification without backup.** Always copy before write.
- **Registry modification requires elevation.** If the target store uses registry, prompt
  for admin elevation or guide the user to run the repair manually.

## Scope

| Store | Phase | Repair Scope |
|-------|-------|-------------|
| Steam — same library | 2.1 | Update ACF `installdir` field |
| Steam — cross-library | 2.1 | Move ACF to new library root + update `installdir` |
| Steam — to standalone | 2.1 | Cannot repair ACF; offer retag as standalone |
| Epic | 2.1 | Update `.item` `InstallLocation` |
| GOG | 3.0 | Update registry, database |
| EA App | 3.0 | Update registry |
| Ubisoft Connect | 3.0 | Update cache JSON |
| Standalone | N/A | No manifest to repair — no action needed |

## Detection in the UI

Games needing repair are flagged in the game list with a visual indicator:

```
┌───────────────────────────────────────────────┐
│  My Epic Game            [Epic]  ⚠️ moved     │
│  Path: E:\games\MyEpicGame\                   │
│  Status: Manifest needs repair — press F6     │
└───────────────────────────────────────────────┘
```

The `⚠️ moved` badge appears when scan-time detection finds a path mismatch.
This is driven by a new `GameEntry` field: `bool ManifestMismatch`.

## Tasks

### 1. Scan-Time Mismatch Detection

- [ ] After scanning a root, compare each detected game's path against its stored `manifestPath`
- [ ] If paths differ (or manifest points to non-existent directory), set `ManifestMismatch = true`
- [ ] Store the detected new path in a `DetectedPath` field on `GameEntry`
- [ ] Persist mismatch state in `data/games.json` so it survives restarts

### 2. SyncMove Dialog (F6)

- [ ] F6 on a game with `ManifestMismatch == true` opens repair dialog
- [ ] Show: store, game name, old manifest path, detected new path, planned repair action
- [ ] "Fix Registration" button triggers the repair
- [ ] "Dry Run" previews changes without writing
- [ ] "Dismiss" clears the mismatch flag without repairing

### 3. Steam ACF Repair

- [ ] Read `appmanifest_<AppID>.acf`
- [ ] Parse and update `installdir` field to match new folder name
- [ ] For cross-library: copy ACF to new library's `steamapps/` (keep original as backup)
- [ ] Write updated ACF (backup first)

### 4. Epic .item Repair

- [ ] Read `.item` JSON manifest
- [ ] Update `InstallLocation` to new detected path
- [ ] Write updated `.item` (backup first)

### 5. Backup & Logging

- [ ] Backup manifest to `data/backups/` before modification
- [ ] Append operation record to `data/migration_log.jsonl`

### 6. UI Indicators

- [ ] Show `⚠️` badge for games with `ManifestMismatch`
- [ ] Show explanatory text in details panel
- [ ] Clear badge after successful repair

### 7. Out of Phase 2.1 Scope (deferred to Phase 3)

- [ ] GOG registry repair
- [ ] EA App registry repair
- [ ] Ubisoft cache repair
- [ ] Launch-time mismatch detection (post-launch failure)

## Deliverables

- [ ] Scan-time mismatch detection (Epic + Steam)
- [ ] F6 repair dialog with dry run
- [ ] Steam ACF `installdir` update
- [ ] Steam cross-library ACF move
- [ ] Epic `.item` `InstallLocation` update
- [ ] Backup system (`data/backups/`)
- [ ] Migration log (`data/migration_log.jsonl`)
- [ ] UI mismatch indicators
- [ ] App builds and runs cleanly on Windows

## Exit Criteria

Phase 2.1 is complete when:
- Scanning a library detects an Epic game whose files were moved and flags `ManifestMismatch`
- Scanning a library detects a Steam game whose files were moved and flags `ManifestMismatch`
- F6 shows the correct repair action (old path → new path, what manifest will be modified)
- Dry run shows accurate preview without modifying any files
- Repair correctly updates the manifest and clears the mismatch flag
- Cross-library Steam move copies the ACF to the correct library root
- Backup is created before each manifest modification
- All operations are logged
- The app builds and runs cleanly on Windows
