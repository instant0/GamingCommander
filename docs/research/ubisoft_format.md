# Ubisoft Connect Game Format

## Purpose
Document the Ubisoft Connect (formerly Uplay) game installation format for detection and metadata extraction.

## Overview

Ubisoft Connect games are installed under the launcher's `games/` directory (default: `C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\games\`). Each game has its own subfolder. Detection relies on `uplay_*` marker files at the game root.

## Detection Markers

| Marker | Type | Description |
|--------|------|-------------|
| `uplay_install.manifest` | File | Binary/encrypted install manifest |
| `uplay_install.state` | File | Plain-text install state data (contains game info, languages) |
| `uplay_r1_loader64.dll` | File | 64-bit Uplay loader DLL |
| `uplay_r1_loader32.dll` | File | 32-bit Uplay loader DLL |
| `uplay_r2_loader64.dll` | File | 64-bit Uplay R2 loader DLL |
| `uplay_download/` | Directory | Download staging directory |

### Priority
Ubisoft is checked **after EA** and **before Epic** in the scanner priority order.

## File Formats

### `uplay_install.state`
A plain-text file (with some binary sections) containing game metadata. Observed structure includes:

```
EULA <lcid>
directx
eacredist
vcredist2012
vcredist2015
vcredist2022
...
```

Contains language tags, readme paths, game title, and installer actions. Example from The Division 2:

```
EULA
directx
eacredist
vcredist2012
vcredist2015
vcredist2022
release,retail
de-DE  voice,data,steam_ge
data,release,retail
streaming,data
fr-FR  voice,data,steam_fr
it-IT  voice,data,steam_it
es-ES  voice,data,steam_sp
es-MX  voice,data,steam_es-mx
pt-BR  voice,data,steam_pt
ja-JP  voice,data,steam_jp
ar-SA  voice,data,steam_ar
ru-RU  voice,data,steam_ru
```

Game title appears as: `Tom Clancy's The Division® 2`

### `uplay_install.manifest`
Binary format, appears to be encrypted or use a custom encoding. Not currently parsed — used purely as a detection marker.

### `uplay_r*_loader*.dll`
The Uplay overlay/runtime DLL executables. Presence indicates the game uses Ubisoft Connect integration.

## Launcher `data/` Directory

The Ubisoft Game Launcher has a `data/` directory containing icon files with hashed hex names:

```
data/
  00842e79ba6c2bf42759a65da1e0cc59.ico
  00e1cd2f99985225bbfa3d66c5ce01fb.ico
  15a8918b82e47c0da7e24b34d42d71c1.ico
  ...
```

These appear to be game icons with hashed filenames (possibly MD5 or similar). Not critical for basic detection.

## Launcher Log Files

The launcher maintains logs at:
```
logs/
  launcher_log.txt
  overlay_log.txt
  network_info.txt
  game_starter_log.txt
  extension_log.txt
  service_log.txt
```

## Common Game Files

Ubisoft game folders commonly contain:
- The main game executable (e.g., `TheDivision2.exe`, `TheDivision2Launcher.exe`)
- Anti-cheat files: `EACLaunch.exe`, `EasyAntiCheat/`
- SDK files: `CgSDK.x64_2015.dll`, `EOSSDK-Win64-Shipping.dll`
- Support/ directory with readme files
- Tools/ directory
- Data files: `*.forge` (proprietary Ubisoft format)

## Install Log

`Installed_files.txt` at the launcher root:
- UTF-16 encoded log
- Contains a complete inventory of every file installed (paths with drive letters)
- Useful for auditing but not for real-time detection

## Executable Collection

Primary executables are:
1. The main game `.exe` in the game root (e.g., `TheDivision2.exe`)
2. The launcher wrapper `.exe` (e.g., `TheDivision2Launcher.exe`)
3. Standard depth-limited walk (max 4 levels) as fallback

## Launch Method

Ubisoft Connect games can be launched via:
1. `uplay://launch/{game_id}` URI scheme
2. Direct executable path
3. The launcher program itself

## References

- Real data examined:
  - `Tom Clancy's The Division 2` at `P:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\games\Tom Clancy's The Division 2\`
  - `Immortals Fenyx Rising` at `P:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\games\Immortals Fenyx Rising\`
  - Ubisoft Game Launcher root at `P:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\`
