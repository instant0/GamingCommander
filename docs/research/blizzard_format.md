# Blizzard / Battle.net Game Format

## Purpose
Document the Blizzard Battle.net game installation format for detection, metadata extraction, and migration scoping.

## Overview

Battle.net (formerly Blizzard App) manages games for Blizzard Entertainment titles (World of Warcraft, Diablo series, StarCraft series, Overwatch, Call of Duty series, etc.). Games are installed in a user-configured directory. Detection relies on the `.battle.net/` marker directory at the game root.

## Detection Markers

### Primary Signal: `.battle.net/` directory

A `.battle.net/` directory at the game folder root identifies a Battle.net-managed game. This is a hidden directory (dot-prefixed on Linux, hidden attribute on Windows).

### Fallback: Deep check via child directories

Container/organizer folders (e.g. `COD/` containing `Call of Duty/` as a child) are detected via `_has_blizzard_deep()` — scans immediate children for `.battle.net/` directories.

### False Positive Avoidance

The `.battle.net/` name is specific enough that false positives are rare. No known engine middleware or SDK creates this directory.

## Install Locations

### Default Game Install Root
- `C:\Program Files (x86)\Battle.net\Games\` — common for Blizzard games
- `C:\ProgramData\Battle.net\` — agent data, not game files
- User-configurable per game (each game can have its own path)

### Launcher Installation
| Method | Path / Key | Notes |
|--------|-----------|-------|
| Default executable | `C:\Program Files (x86)\Battle.net\Battle.net.exe` | Standard install |
| Registry (machine) | `HKLM\SOFTWARE\WOW6432Node\Battle.net\InstallPath` | Launcher location |
| Registry (user) | `HKCU\Software\Battle.net\InstallPath` | Per-user override |

### Product DB (Installed Games List)
Battle.net tracks installed products in:
- `%LOCALAPPDATA%\Battle.net\Battle.net.config` — XML config with product install paths
- `%PROGRAMDATA%\Battle.net\Agent\product.db` — SQLite database of installed products

The SQLite database (`product.db`) is the authoritative source. It contains:
- `product` table: `code`, `uid`, `platform`, `installed`, `install_path`, `game_uid`
- Each game has a unique product code (e.g. `wow` for World of Warcraft, `d3` for Diablo III, `s2` for StarCraft II)

## Game Folder Structure

```
<GameRoot>/
├── .battle.net/                 # Marker directory
│   └── <product_code>/          # Product-specific metadata
├── <GameExecutable>.exe         # Main game executable
├── <GameExecutable> Launcher.exe # Optional launcher helper
├── _retail_/                    # Retail game data (common pattern)
├── _ptr_/                       # PTR/test server data (if present)
├── Data/                        # Game data files
└── ...                          # Engine-specific files
```

## Launch Patterns

Battle.net games can be launched via:
1. **Battle.net URI scheme:** `battle.net://<product>/` (e.g. `battle.net://d3/`)
2. **Direct executable:** `<GameRoot>/<GameExecutable>.exe` (bypasses launcher)
3. **Launcher executable:** `<GameRoot>/<Game> Launcher.exe` (launches via Battle.net agent)

The C# implementation should prefer the direct executable for standalone launch, or the battle.net:// URI for full launcher integration.

## Migration Considerations

### What's Possible
- **Copy/move game folder** to new location — game data is self-contained in the game root
- **Update Battle.net.config** to point to new path — requires XML parsing

### What's Difficult
- The `product.db` SQLite database stores absolute install paths — would need SQLite write access to update
- Some games (Call of Duty: Warzone, Overwatch 2) have additional anti-cheat bindings tied to install path
- `.battle.net/` directory contains product bindings — must be moved with the game
- Battle.net agent may re-detect games at new location on restart (Steam-like behavior)

### Migration / Repair Notes

**GamingCommander does NOT move game files.** The user moves files with OS tools.
GamingCommander only repairs registration after detection of a move.

| Repair Option | Feasibility | Notes |
|--------------|-------------|-------|
| Detect moved game | ✅ Yes | Scan finds game at new path via `.battle.net/` signal; manifest mismatch flagged |
| Update product.db | ❌ No | SQLite database — write access is risky without Battle.net agent cooperation |
| Update Battle.net.config | ⚠️ Partial | XML file in `%LOCALAPPDATA%` — may update path but agent may override on restart |
| Registry repair | ❌ No | Battle.net re-writes registry on launch; manual changes likely overwritten |

**Recommendation:** For Battle.net games, detection + flagging the mismatch is sufficient.
Battle.net's agent is aggressive about re-detecting games on launch and may self-heal.
If it doesn't, guide the user to use Battle.net's own "Locate Game" feature.

## References
- `tools/detect_folder.py` — `_check_blizzard()`, `_has_blizzard_deep()` detection functions
- `docs/research/launcher_discovery.md` — registry key locations
- `tools/generate_mock_registry.py` — mock registry key generation (`battle.net.reg`)
