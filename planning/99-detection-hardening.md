# Detection Logic Hardening Plan

**Status:** Partially Completed (Fixes 1-6 done. EA/GOG signals, UE scanning, .lnk parsing, --log flag added.)
**Priority:** P0 (detection accuracy) → P1 (launch configuration) → P2 (unknowns UX)
**Addresses:** Exe scoring, container detection, non-game filtering, .bat launchers, unknowns

---

## Core Principles

### Detection Philosophy
1. **Identify games with high accuracy** — the scan should correctly classify 95%+ of folders
2. **Filter out KNOWN non-games** — skip lists for redist, tools, mod managers, data folders
3. **Unknowns go to VFS** — folders that can't be classified are added to the VFS as "Unknown"
4. **User toggles unknowns** — a toggle in the UI reveals unknowns for manual configuration
5. **Manual edits are marked** — user-configured entries show "edited" indicator in VFS

### The Unknowns Flow
```
Detection scan
  ├── Classified as game → VFS (default view)
  ├── Classified as non-game → skipped
  └── Unknown/uncertain → VFS (hidden by default, toggle to show)
       └── User can: set name, pick exe, select store, add launch args
```

### Multi-Folder Games
Games like FFXIV have separate `boot/` and `game/` subdirectories.
These should be detected as ONE game, not two. The parent container
(`SquareEnix/FINAL FANTASY XIV - A Realm Reborn/`) should be the game entry,
with the primary exe identified from the `game/` subfolder.

### GOG SCUMMVM Games
GOG games using SCUMMVM (DOS games) have `.info` files containing:
- Game name
- Launcher arguments (SCUMMVM flags)
- Main executable path

This data should be extracted and used to populate launch arguments
and metadata in the application.

---

## Completed Fixes

### Fix 1: Exe Scoring — Backup/Tool Penalty ✅

**Result:** All 6 exe selection bugs fixed.

Scoring penalties added:
```
"copy of" prefix            → -30 (backup)
"_copy" / " - Copy"         → -25 (Windows copy)
"_org_" / "org_" prefix     → -20 (backup/original copy)
"original" in name          → -15 (backup)
Tool names (20+ patterns)   → -25 (config tools, editors, utilities)
"unins" / "uninstal"        → -30 (uninstaller)
Size < 100KB                → -15 (tiny tool)
Size < 500KB                → -5  (small utility)
```

Scoring bonuses added:
```
Folder name tokens in exe   → +10 (name match)
Exact folder token match    → +15 (e.g. "heroes4" = "heroes4")
Abbreviation match          → +8  (e.g. "g3" ≈ "g3", first letter match)
Roman numeral match         → +12 (e.g. "u9" ≈ "ix", "heroes4" ≈ "iv")
PE FileDescription match    → +15 (strongest signal)
PE ProductName match        → +10
UE standard path (Win64)    → +5
```

### Fix 2: Deep Exe Discovery ✅

**Result:** BioShock Infinite now correctly finds `Binaries/Win64/BioShockInfinite.exe`.

- Added `*/bin/*.exe` check (older games like Gothic, Jagged Alliance)
- Added 2-level deep walk when root has no exes
- Returns both exe candidates and .bat launchers

### Fix 3: Container Detection ✅

**Result:** Store/publisher folders (Blizzard/, UBI/, Epic Games/) correctly detected.

- Launcher directories added to SKIP_NAMES (epiclauncher, battle.net, etc.)
- Store container detection: root has only dirs + children have game-like structure
- Container recursion uses `container=True` flag to filter data-only subfolders
- Data subfolders (Item Data, Misc, vo_soundsets, etc.) no longer promoted as games

### Fix 4: Non-Game Classification ✅

**Result:** 0 unknowns (was 9).

Added to skip lists:
- Known apps: `wiiu`, `reshade`, `sweetfx`, `enbseries`, `enb`
- Mod managers: `nexus mod manager`, `vortex`, `mod organizer`
- Data subdirs: `item data`, `misc`, `vo_soundsets`, `vo_en`, `depot`, `_gamedata`
- Noise exes: `intro`, `dedicatedserver`, `kernelmodedriverloader`, `driverloader`
- Server exes: removed "server" from noise (Minecraft_Server.exe is a valid game)

### Fix 6: PE Metadata Integration ✅

**Result:** PE metadata used in Phase 4 enrichment for unknowns.

- FileDescription/ProductName matched against folder name (+15 bonus)
- Top 3 candidates checked for PE metadata during scoring

---

## In Progress: Fix 5 — .bat Launcher Detection + Configuration

### Problem

Games like Anachronox have `.bat` files with launch arguments:
```bat
anox_1280gl.bat  → anox.exe +w 1280 +h 1024
anox_640gl.bat   → anox.exe +w 640 +h 480
anox_800gl.bat   → anox.exe +w 800 +h 600
anox_v2v3.bat    → anox.exe -v2 -v3
anox_window.bat  → anox.exe -window
```

User should be able to:
1. See these as launch options in the application
2. Select which launch mode to use
3. Understand what each argument does (via PCGamingWiki or manual input)

### Proposed Architecture

#### Step 1: .bat File Parsing (detect.py)

Parse .bat files to extract the command and arguments:
```python
def _parse_bat_launcher(bat_path: Path) -> dict | None:
    """Parse a .bat file to extract the exe and arguments.
    Returns {"exe": "anox.exe", "args": "+w 1280 +h 1024"} or None."""
    try:
        text = bat_path.read_text(encoding="utf-8", errors="ignore")
    except OSError:
        return None
    for line in text.splitlines():
        line = line.strip()
        if line.startswith("@") or line.startswith("rem") or not line:
            continue
        # Look for exe invocation
        parts = line.split()
        if parts and parts[0].lower().endswith(".exe"):
            exe = parts[0]
            args = parts[1:] if len(parts) > 1 else []
            return {"exe": exe, "args": " ".join(args)}
    return None
```

#### Step 2: PCGamingWiki Argument Lookup (detect.py)

Query PCGamingWiki for known command-line arguments:
```python
def _pcgw_lookup_arguments(game_name: str) -> list[dict] | None:
    """Query PCGamingWiki for known command-line arguments.
    Returns [{"arg": "-windowed", "description": "Run in windowed mode"}, ...] or None."""
    # Use PCGamingWiki API to fetch game page
    # Parse "Command line arguments" section
    # Return structured argument list
```

#### Step 3: Result Enrichment

Add to game result:
```json
{
  "folder": "Anachronox",
  "exe": "anox.exe",
  "bat_launchers": [
    {"file": "anox_1280gl.bat", "exe": "anox.exe", "args": "+w 1280 +h 1024"},
    {"file": "anox_640gl.bat", "exe": "anox.exe", "args": "+w 640 +h 480"},
    ...
  ],
  "known_args": [
    {"arg": "-windowed", "description": "Run in windowed mode"},
    {"arg": "+w <width>", "description": "Set window width"},
    ...
  ]
}
```

#### Step 4: Application UI — Configure Dialog

In the application, when user right-clicks a game → "Configure":

```
┌─────────────────────────────────────────────┐
│  Configure: Anachronox                      │
├─────────────────────────────────────────────┤
│  Display Name: [Anachronox          ]       │
│  Primary EXE:  [anox.exe            ] [Browse]│
│                                             │
│  Launch Arguments:                          │
│  ┌─────────────────────────────────────────┐│
│  │ ☑ -windowed  Run in windowed mode      ││
│  │ ☐ +w 1280    Set window width          ││
│  │ ☐ +h 1024    Set window height         ││
│  │ ☐ -v2        Enable V2 renderer        ││
│  │ ☐ -v3        Enable V3 renderer        ││
│  │                                         ││
│  │ Custom args: [________________]         ││
│  └─────────────────────────────────────────┘│
│                                             │
│  Source: PCGamingWiki / Manual              │
│                                             │
│  [Save]  [Cancel]  [Remove Game]           │
└─────────────────────────────────────────────┘
```

Features:
- Checkboxes for known arguments (from PCGamingWiki or bat file parsing)
- Free-text field for custom/wildcard arguments
- Arguments from .bat files shown as presets
- User can save configuration per-game
- Configuration persisted in game library JSON

#### Step 5: Launch Integration

When launching a game, combine primary exe + selected arguments:
```csharp
// Instead of: Process.Start("anox_1280gl.bat")
// Do: Process.Start("anox.exe", "+w 1280 +h 1024")
```

Benefits:
- Direct exe launch is more reliable than .bat files
- User-selected arguments are applied
- No dependency on .bat file existence

### Data Model Extension

```csharp
public class GameConfiguration
{
    public string DisplayName { get; set; }
    public string PrimaryExe { get; set; }
    public List<string> LaunchArguments { get; set; }
    public List<KnownArgument> KnownArguments { get; set; }
    public string ArgumentSource { get; set; } // "pcgw", "bat_file", "manual"
}

public class KnownArgument
{
    public string Argument { get; set; }
    public string Description { get; set; }
    public bool IsSelected { get; set; }
}
```

### Implementation Order

1. Parse .bat files in detect.py (Step 1)
2. Add bat_launchers to JSON output (Step 3)
3. PCGamingWiki argument lookup (Step 2)
4. C# data model for game configuration (Step 5)
5. Configure dialog in Avalonia UI (Step 4)
6. Launch integration with arguments (Step 5)

---

## Application UX: Identified Games + Unknowns Toggle

### Default View: Identified Games Only

The application shows only detected games by default. No noise, no unknowns. User sees a clean library.

### Toggle: "Show Unknowns"

A toggle switch (or menu option) reveals folders that detection couldn't classify. These are shown in a separate section or with a visual indicator (e.g., grayed out, "?" icon).

### Manual Configuration for Unknowns

When user selects an unknown folder, they can:
1. **Set game name** — type the display name (e.g., "Ghost Recon Wildlands")
2. **Select game store** — dropdown: Steam, GOG, Epic, EA, Ubisoft, Standalone, etc.
3. **Pick the correct EXE** — browse or select from detected exes
4. **Set launch arguments** — from .bat files, PCGamingWiki, or manual input
5. **Mark as not a game** — remove from list permanently

### Visual Indication

Manually configured entries show a small indicator (e.g., pencil icon, "edited" tag) so the user knows these weren't auto-detected.

### Data Persistence

Manual configurations are saved per-game in the library JSON:
```json
{
  "folder": "GhostREconWild",
  "name": "Ghost Recon Wildlands",
  "store": "Ubisoft",
  "exe": "Tom Clancy's Ghost Recon Wildlands/bin/GRW.exe",
  "manually_edited": true,
  "edit_timestamp": "2026-07-17T12:00:00Z"
}
```

---

## PE Detection Role: Fallback Only

PE metadata extraction (FileDescription, ProductName) is a **fallback mechanism**, not a primary scoring tool. The current decision pipeline handles most cases:

1. **Backup penalties** — `org_`, `copy`, `original` in name → -20 to -30
2. **Tool penalties** — known tool names → -25
3. **Folder name matching** — exe name tokens match folder → +10
4. **Size heuristics** — tiny exes (< 100KB) penalized → -15
5. **UE standard paths** — `Binaries/Win64` bonus → +5

PE metadata is only used when:
- Multiple exes have the same score after name-based scoring
- No clear winner emerges from the above rules
- The `--metadata` flag is explicitly passed for enrichment

The pipeline should resolve 95%+ of cases without PE. PE catches the remaining edge cases.

---

## Remaining Issues

### FFXIV Multi-Folder Detection
`SquareEnix/FINAL FANTASY XIV - A Realm Reborn/` has `boot/` and `game/` subdirs.
Currently detected as two separate games. Should be one game with `game/ffxiv.exe` as primary.

**Fix:** When a container has children that are all part of the same game (same parent name),
merge them into a single game entry. Detect by: parent folder has game-like name,
children are `boot/`, `game/`, `boot_`, etc.

### GOG SCUMMVM .info Extraction
GOG games using SCUMMVM have `.info` files with game name, args, and exe path.
Currently not parsed. Should extract and use for launch arguments.

**Fix:** In `_extract_gog_metadata()`, also parse SCUMMVM-specific `.info` fields
for command-line arguments and executable path.

### Games with No Root Exe (16 cases)
**Fixed:** UE-aware scanning, .lnk backup rename matching, GOG .info exe fallback.
Now 0 cases on both drives.

### EA Store Signals — TOUCHUP and ActivationUI
EA games ship with `Touchup.exe` and `ActivationUI.exe` in `__Installer/`.
These are reliable EA signals alongside the existing `__Installer` directory check.

**Fixed:** Added `touchup.exe` and `activationui.exe` to EA signal detection.

### EA Install Metadata (Future — Application Feature)
EA install logs contain rich metadata useful for the application:
- `__Installer/InstallLog.txt` — game name, studio, install path, registry keys, redistributables
- `__Installer/installerdata.xml` — content IDs, title, locale, EULA paths

This data should be parsed and stored in game metadata for:
- Display name (from `<title>` in XML)
- Registry key detection (for EA App integration)
- Redistributable requirements
- Game migration (knowing what files are needed)

**Status:** Detection logic done. Application metadata parsing planned for later.

### GOG Metadata — .info Files and gog.ico
GOG games have multiple signal types:
- `goggame-*.info` — JSON with game name, exe path, launch args, gameId
- `gog.ico` — GOG icon file (now detected as store signal)
- `.lnk` files — shortcut targets pointing to game exe

**Fixed:** `gog.ico` added to GOG detection. `.info` files searched in root + subdirs.
Launch arguments extracted from `playTasks[].arguments`.

### SCUMMVM/DOSBox Game Category (Future — Application Feature)
GOG SCUMMVM and DOSBox games could have a "Retro" or "DOS Game" category,
similar to how Engine metadata is already tracked. The `.info` files contain
the specific engine (SCUMMVM, DOSBox) and arguments needed.

**Status:** Detection done. Category type planned for application.

### Store Signal Returns Losing Exe Info
**Fixed:** Store signal returns now pass through `has_root_exe` and `root_exe`
instead of hardcoding `False, None`. This ensures games with both a store signal
and a root exe (e.g., sysshock2.25 with steam_api64.dll + SystemShock2Remastered.exe)
correctly identify the exe.

---

| Metric | Before | After |
|--------|--------|-------|
| Unknowns | 9 | 0 |
| Wrong exe selected | 6+ | 0 |
| Games with no exe | 30+ | 0 |
| Container detection | Basic | Full |
| Store signal + exe | Lost exe | Preserved |
| Roman numeral match | None | u9↔IX, g3↔III, heroes4↔IV |
| .bat launcher support | None | Detection done, UI planned |
| .lnk shortcut parsing | None | Exe name + backup rename matching |
| EA signals | __Installer only | +touchup.exe, activationui.exe |
| GOG signals | goggame-* only | +gog.ico, .info subdirs |
| UE exe discovery | Root only | Engine-guided: */Binaries/Win64/ |
| D drive games | 129 (57s) | 120 (3s) |
| E drive games | 37 (5s) | 37 (1.3s) |
| Detection log | None | --log FILE flag |
