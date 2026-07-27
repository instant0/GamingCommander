# GOG Galaxy Game Format

## Purpose
Document the GOG Galaxy game installation format for detection and metadata extraction.

## Overview

GOG Galaxy games are installed in a user-configured directory (default: `C:\Program Files (x86)\GOG Galaxy\Games\`). Each game has its own subfolder. Detection relies on `goggame-*` marker files at the game root.

## Detection Markers

| Marker | Type | Description |
|--------|------|-------------|
| `goggame-*.info` | File | Game metadata (JSON format) |
| `goggame-*.hashdb` | File | File hash database for integrity checking |
| `goggame-*.ico` | File | Game icon |
| `goggame-galaxyFileList.ini` | File | Galaxy file list |
| `goggame.dll` | File | Legacy GOG DLL (rare) |
| `gog.ico` | File | GOG icon marker |
| `goglog.ini` | File | GOG log file |
| `launcher-configuration.json` | File | Galaxy launcher configuration |

### Priority
GOG is checked **first** (highest priority) in the scanner priority order.

## File Formats

### `goggame-<id>.info`
JSON format containing game metadata. Example structure:

```json
{
  "gameId": 1495134320,
  "name": "The Witcher 3: Wild Hunt - Game of the Year Edition",
  "playTasks": [
    {
      "category": "game",
      "isPrimary": true,
      "path": "bin/x64/witcher3.exe",
      "name": "The Witcher 3"
    }
  ],
  "supportTasks": [
    {
      "category": "editor",
      "path": "bin/x64/witcher3.exe",
      "name": "Modkit"
    }
  ],
  "languages": ["en", "fr", "de", ...]
}
```

Key fields for identification:
- `gameId` — GOG internal numeric ID
- `name` — Display name
- `playTasks` — Primary and alternate launch tasks (executable paths relative to game root)

### `goggame-<id>.hashdb`
Binary hash database. Not parsed for basic identification; used for file integrity verification.

### `goggame-galaxyFileList.ini`
INI-format file listing all game files for Galaxy's sync/verify feature.

### `launcher-configuration.json`
JSON with launcher-specific configuration:
```json
{
  "ignore": [],
  "dependencies": [],
  "supportSystemComponents": []
}
```

## Multiple goggame-* IDs

A single game folder may contain multiple `goggame-<id>` file sets. This happens when:
1. A game has multiple products/editions sharing the same folder
2. DLC or additional content installed alongside the base game
3. Different variants (e.g., standard vs. GOTY edition)

Example from Cyberpunk 2077:
- `goggame-1256837418.info` + `.hashdb` (base game)
- `goggame-1423049311.info` + `.hashdb` + `.ico` (DLC/expansion)

Extract metadata from **all** `goggame-*.info` files and merge the results.

## Common Files

In addition to markers, GOG game folders commonly contain:
- `gog.ico` — GOG icon
- `goglog.ini` — Log file (contains `[LOG]`, `FileListPath`, etc.)
- `Launch <gamename>.lnk` — Windows shortcut to launch the game (e.g., `Launch The Witcher 3 - Wild Hunt - Game of the Year Edition.lnk`). This is a **strong GOG signal** — every GOG-installed game has exactly one `.lnk` file with the "Launch" prefix. Generic `.lnk` files (without the "Launch" prefix) are NOT a GOG signal.
- `_dls/` — DLC directory
- `dlc/` — DLC content
- `content/` — Main game content
- `bin/` — Game binaries

## Executable Collection

Primary executables are found via:
1. `goggame-*.info` → `playTasks[].path` (most reliable)
2. `Launch <gamename>.lnk` shortcuts (strong GOG signal, may point to game exe)
3. Standard depth-limited walk (max 4 levels) as fallback
4. Common subdirectories: `bin/`, `bin/x64/`, `Binaries/`, `Binaries/Win64/`

## Launch Method

GOG games can be launched via:
1. `galaxy://launchgame/{gameId}` URI scheme
2. Direct executable path from `goggame-*.info` playTasks
3. No launcher needed for DRM-free GOG games

## References

- Real data examined:
  - `The Witcher 3 Wild Hunt GOTY` at `P:\Program Files (x86)\GOG Galaxy\Games\The Witcher 3 Wild Hunt GOTY\`
  - `Cyberpunk 2077` at `P:\Program Files (x86)\GOG Galaxy\Games\Cyberpunk 2077\`
  - `Baldurs Gate 3` at `P:\Program Files (x86)\GOG Galaxy\Games\Baldurs Gate 3\`
