# Rockstar Games Launcher Format

## Purpose
Document the Rockstar Games Launcher game installation format for detection, metadata extraction, and migration scoping.

## Overview

Rockstar Games Launcher (also referred to as the Rockstar Games Library or RGL) manages titles from Rockstar Games (Grand Theft Auto series, Red Dead Redemption series, Max Payne 3, L.A. Noire, etc.). Games are installed in a user-configured directory. Detection relies on the `title.rgl` marker file at the game root, and the RAGE engine can be confirmed by the presence of `common.rpf`.

## Detection Markers

### Primary Signal: `title.rgl`
The `title.rgl` file at the game folder root identifies a Rockstar Games Launcher-managed title. This is a metadata file containing the game's title identifier used by RGL.

### Secondary Signal: `common.rpf` (RAGE Engine)
Rockstar's proprietary RAGE engine uses `.rpf` (Rockstar Package File) archives for game assets. The presence of `common.rpf` alongside `title.rgl` confirms:
- The game uses the RAGE engine
- Can be used for engine-specific launch arguments or configuration

### Fallback Detection (`root_lnk`)
Some older Rockstar titles (pre-RGL) were distributed as standalone installs with only a Windows shortcut at root. These should be detected as a fallback when `title.rgl` is not present.

### False Positive Avoidance
`title.rgl` is unique to Rockstar titles — no known middleware uses this file extension. `common.rpf` is also Rockstar-specific. Combined, they are definitive.

## Install Locations

### Default Game Install Root
- `C:\Program Files\Rockstar Games\` — common for older titles
- `C:\Program Files (x86)\Rockstar Games\` — 32-bit titles
- User-configurable per game in RGL settings

### Launcher Installation
| Method | Path / Key | Notes |
|--------|-----------|-------|
| Default executable | `C:\Program Files\Rockstar Games\Launcher\Launcher.exe` | RGL launcher |
| Registry (user) | `HKCU\Software\Rockstar Games\Launcher\InstallFolder` | Launcher install path |
| Registry (machine) | `HKLM\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\InstallFolder` | Alternate location |

### Installed Games List
RGL tracks installed titles via:
- `%LOCALAPPDATA%\Rockstar Games\Launcher\Settings.json` — JSON config with game library paths
- `%PROGRAMDATA%\Rockstar Games\Launcher\*.json` — additional metadata

The Settings.json file contains:
- `installFolder` — default install directory
- `gameLibraryFolders` — list of library search paths
- Per-game entries with install state and paths

## Game Folder Structure

```
<GameRoot>/
├── title.rgl                     # Detection marker — Rockstar title identifier
├── common.rpf                    # RAGE engine package file (RAGE titles only)
├── <GameExecutable>.exe          # Main game executable
├── Play<Game>.*                  # Launch helper (e.g. PlayGTAV.exe)
├── <Game> Launcher.exe           # Optional launcher helper
├── x64a.rpf / x64b.rpf / ...    # RAGE package files (split archives)
├── update/                       # Game update data
├── redist/                       # Redistributables
└── ...                           # Game-specific files
```

## Launch Patterns

Rockstar games can be launched via:
1. **RGL URI scheme:** `rockstar-games://<title>/` — launches via the Rockstar Games Launcher
2. **Direct executable:** `<GameRoot>/<GameExecutable>.exe` — works for most titles (may skip RGL overlay)
3. **Play<Game>.exe:** Some titles have a wrapper executable (e.g. `PlayGTAV.exe`)
4. **Launcher.exe:** `<GameRoot>/<Game> Launcher.exe` — some titles bundle their own launcher helper

### RAGE Engine Launch Notes
RAGE engine titles (GTA V, RDR2) often require additional launch arguments:
- `-windowed` — force windowed mode
- `-fullscreen` — force fullscreen
- `-framelock` — disable frame limiter
- `-GPUCount <N>` — multi-GPU configuration

These should be documented in the game's metadata (via PCGamingWiki lookup in Phase 2.2), not hardcoded.

## Migration Considerations

### What's Possible
- **Copy/move game folder** — game data is self-contained
- **Update Settings.json** — JSON format is easily parsed and modified
- **RGL re-detection** — RGL rescans library folders on launch and may re-detect moved games

### What's Difficult
- Some titles have additional DRM bindings (Social Club account linking)
- `title.rgl` contents should be preserved during migration
- RGL overlay DLLs (e.g. `SocialClub.dll`, `dinput8.dll`) may be path-dependent

### Migration / Repair Notes

**GamingCommander does NOT move game files.** The user moves files with OS tools.
GamingCommander only repairs registration after detection of a move.

| Repair Option | Feasibility | Notes |
|--------------|-------------|-------|
| Detect moved game | ✅ Yes | Scan finds game at new path via `title.rgl` signal; manifest mismatch flagged |
| Update Settings.json | ✅ Yes | JSON file in `%LOCALAPPDATA%\Rockstar Games\Launcher\` — easily parsed and updated |
| Registry repair | ❌ No | RGL re-writes registry on launch; manual changes overwritten |

**Recommendation:** Update `Settings.json` to point to the new game path. RGL will
re-scan on next launch and detect the game at the new location.

## References
- `tools/detect_folder.py` — `_check_rockstar()`, `_has_rockstar_rage_signal()` detection functions
- `docs/research/launcher_discovery.md` — registry key locations
- `tools/generate_mock_registry.py` — mock registry key generation
