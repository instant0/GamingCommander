# EA App Game Format

## Purpose
Document the EA App (formerly Origin) game installation format for detection and metadata extraction.

## Overview

EA App games live in a user-configured directory (default: `C:\Program Files\EA Games\` or `C:\Program Files (x86)\EA Games\`). Each game has its own subfolder. Detection relies on the presence of an `__Installer/` directory at the game root.

## Detection Markers

| Marker | Type | Description | Reliability |
|--------|------|-------------|-------------|
| `__Installer/` | Directory | Required — EA installer metadata directory | ✅ Confirmed present even in staged installs |

### Priority
EA is checked **after GOG** and **before Ubisoft** in the scanner priority order.

### False Positive Prevention
- The `__Installer/` directory is unique to EA and should not appear in other launcher game folders.
- If `__Installer/` is present, do NOT check for Steam emulator markers (`steam_api64.dll`) — EA games may bundle these for cross-platform features.

## `__Installer/` Directory Structure (Staged Install — Needs Verification)

The only EA game available for inspection (`Battlefield 6` at `P:\Program Files (x86)\EA\Battlefield 6\`) is a **staged/incomplete installation**. The observed `__Installer/` structure may differ from a complete install:

```
__Installer/
  vc/                            # VC++ redist
```

Files with `_DiP_Staged` suffix were also present in this staged install (see below). In a complete install, these files would likely appear **without** the `_DiP_Staged` suffix.

**⚠️ CAVEAT**: Everything below is based on a staged install. A complete EA install needs to be inspected for definitive format documentation.

## The `_DiP_Staged` Suffix

EA uses a "Download in Progress" staging system where pending update files are written with a `_DiP_Staged` suffix. In the staged install, the following files were observed inside `__Installer/`:

| File | Purpose |
|------|---------|
| `installerdata.xml_DiP_Staged` | DiPManifest XML (version 4.0) |
| `Cleanup.dat_DiP_Staged` | Cleanup data |
| `Cleanup.exe_DiP_Staged` | Cleanup executable |
| `Touchup.dat_DiP_Staged` | Touchup data |
| `Touchup.exe_DiP_Staged` | Touchup executable |

In a **complete** install:
- These files would likely be named WITHOUT `_DiP_Staged` suffix
- The `__Installer/` directory may or may not persist
- The game executable would be present at root or in a subdirectory

## DiPManifest XML Format

The manifest file found (inside staged install) is `installerdata.xml_DiP_Staged`. Root element is `<DiPManifest>`. In a complete install, the filename would be `installerdata.xml`.

### Key Fields (from staged Battlefield 6 data)

| XPath | Value | Purpose |
|-------|-------|---------|
| `DiPManifest/buildMetaData/featureFlags/@autoUpdateEnabled` | `1` | Update behavior |
| `DiPManifest/buildMetaData/gameVersion/@version` | `1.0.399.22669` | Game version |
| `DiPManifest/contentIDs/contentID` | `16426154` | EA internal content ID |
| `DiPManifest/gameTitles/gameTitle[@locale="en_US"]` | `Battlefield™ 6` | Display name |
| `DiPManifest/runtime/launcher/name[@locale="en_US"]` | `Battlefield™ 6` | Launcher display name |
| `DiPManifest/runtime/launcher/filePath` | `[Registry]EAAntiCheat.GameServiceLauncher.exe` | Launch executable path (may reference registry) |
| `DiPManifest/uninstall/path` | Registry key | Uninstall registry reference |

### Registry References in filePath
The `filePath` field often contains registry references in `[HKEY...]` format:
```
[HKEY_LOCAL_MACHINE\SOFTWARE\EA Games\Battlefield 6\Install Dir]EAAntiCheat.GameServiceLauncher.exe
```
This means: read the registry key for the install directory, then append the executable name.

## Executable Collection

**Unverified**: The staged install had zero `.exe` files committed. In a complete install:
- Main executable likely at game root or in a subdirectory (e.g., `x64/`, `Binaries/`)
- May be referenced via registry indirection in `installerdata.xml`
- May include anti-cheat launchers (e.g., `EAAntiCheat.GameServiceLauncher.exe`)
- Standard depth-limited walk (max 4 levels) as fallback

## Launcher Identification

The EA App launcher itself is found at:
- Default: `C:\Program Files\EA Games\EA Desktop\EADesktop.exe`
- Registry: `HKLM\SOFTWARE\Electronic Arts\EA Desktop\InstallDir`

Launch URI scheme: `origin://launchgame/{contentID}` or via the EA Desktop protocol.

## References

- Real data examined: `Battlefield 6` at `P:\Program Files (x86)\EA\Battlefield 6\` **(staged/incomplete install — not a complete reference)**
- DiPManifest version observed: `4.0`
- EAInstaller version observed: `5.07.24.00`
- **Next step**: Inspect a complete EA game install to validate file structure, executable locations, and manifest naming.
