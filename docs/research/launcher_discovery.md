# Launcher Discovery Reference

## Purpose
Document how to discover each game launcher's installation path on a Windows system. These paths are used by the detection services to locate game manifests and game folders.

## Discovery Methods

For each launcher, the paths below are the known default locations and registry keys that store the current installation path.

---

### Steam

| Method | Path / Key | Notes |
|--------|-----------|-------|
| Default executable | `C:\Program Files (x86)\Steam\Steam.exe` | Standard install path |
| Alternative 1 | `C:\Program Files\Steam\Steam.exe` | Less common |
| Registry (user) | `HKCU\Software\Valve\Steam\SteamPath` | Points to Steam root (e.g., `C:\Program Files (x86)\Steam`) |
| Registry (machine) | `HKLM\SOFTWARE\WOW6432Node\Valve\Steam\InstallPath` | Alternate registry location |

After finding Steam root, libraries are enumerated from:
- `{SteamRoot}/steamapps/libraryfolders.vdf` — lists all Steam library paths
- `{SteamRoot}/steamapps/` — default library's app manifests

---

### GOG Galaxy

| Method | Path / Key | Notes |
|--------|-----------|-------|
| Default games dir | `C:\Program Files (x86)\GOG Galaxy\Games\` | Default game install location |
| Galaxy client | `C:\Program Files (x86)\GOG Galaxy\GalaxyClient.exe` | Launcher executable |
| Registry | `HKLM\SOFTWARE\WOW6432Node\GOG.com\GalaxyClient\paths\common` | Galaxy client install path |
| Game install dir | `{GalaxyClient}/Games/` | Relative to Galaxy installation |

GOG does not have a `libraryfolders.vdf` equivalent — all games live under `Games/` subdirectory.

---

### EA App (formerly Origin)

| Method | Path / Key | Notes |
|--------|-----------|-------|
| Default games dir | `C:\Program Files\EA Games\` | Default install location |
| Alternative | `C:\Program Files (x86)\EA Games\` | Alternate |
| EA Desktop | `C:\Program Files\EA Games\EA Desktop\EADesktop.exe` | Launcher executable |
| Registry | `HKLM\SOFTWARE\Electronic Arts\EA Desktop\InstallDir` | EA Desktop install path |
| Registry (Origin legacy) | `HKLM\SOFTWARE\Electronic Arts\EA Games\{contentId}\Install Dir` | Per-game install path |
| Registry (Origin legacy) | `HKLM\SOFTWARE\WOW6432Node\Electronic Arts\EA Games\{contentId}\Install Dir` | 32-bit per-game path |

The game directory is configurable in EA App settings. Individual game folders can be in different locations.

---

### Ubisoft Connect

| Method | Path / Key | Notes |
|--------|-----------|-------|
| Default launcher dir | `C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\` | Launcher installation |
| Default games dir | `{LauncherDir}\games\` | Game install directory (relative to launcher) |
| Registry | `HKLM\SOFTWARE\WOW6432Node\Ubisoft\Launcher\InstallDir` | Launcher install path |
| Cache | `%ProgramData%\Ubisoft\Ubisoft Game Launcher\cache\` | Cached data/ownership |
| Settings | `%LocalAppData%\Ubisoft Game Launcher\settings.yaml` | User settings (YAML format) |
| Ownership cache | `%LocalAppData%\Ubisoft Game Launcher\cache\ownership\{uuid}\` | Per-game ownership cache |

Ubisoft games typically live directly under the launcher's `games/` subfolder, though advanced installs may use other directories.

---

### Epic Games Store

| Method | Path / Key | Notes |
|--------|-----------|-------|
| Default launcher dir | `C:\Program Files (x86)\Epic Games\Launcher\` | Launcher installation |
| Default games dir | `C:\Program Files (x86)\Epic Games\` | Default install location (configurable) |
| Registry | `HKLM\SOFTWARE\WOW6432Node\Epic Games\EpicGamesLauncher\AppDataPath` | Launcher app data path |
| Manifests dir | `%ProgramData%\Epic\EpicGamesLauncher\Data\Manifests\` | `.item` files live here |
| Engine installs | `HKLM\SOFTWARE\Epic Games\Unreal Engine\{version}\InstalledDirectory` | UE install paths |

Each Epic game has:
- `.egstore/` directory inside the game folder containing the `.manifest` file
- A corresponding `.item` file in the Manifests directory used by the launcher for display

---

## Registry Discovery Flow

The C# detection services should follow this priority:

1. **Default path** — check common locations first (fastest)
2. **Registry (CurrentUser)** — check HKCU for path overrides
3. **Registry (LocalMachine)** — check HKLM 64-bit, then 32-bit (WOW6432Node)
4. **Process scan** — as last resort, scan running processes for launcher executables

---

## Cross-Platform Note

For the Linux development environment, launcher discovery relies on:
- Mock registry files in `data/mock/registry/` (`.reg` format)
- Mock game folder trees in `data/mock/`
- Python tools that simulate registry reads
