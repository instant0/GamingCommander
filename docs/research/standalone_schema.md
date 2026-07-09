# Standalone Directory Game Detection

## Purpose

Scan a user-configured "standalone games" root directory and classify each
subfolder by its launcher origin, mark it as a standalone game, or flag it
for manual review. Results feed the game database alongside Steam/Epic/etc.
detection paths.

## Three-Tier Classification

The scanner uses a simple, predictable classification system that prioritizes
accuracy over guessing:

| Tier | Condition | Classification | Confidence | Action |
|------|-----------|---------------|------------|--------|
| 1 | Launcher/store signals found | GOG/EA/Ubisoft/Epic/Blizzard/Xbox/Rockstar/SteamEmu | High | Auto-add to DB |
| 2 | Game-structure signals found | Standalone | Medium | Auto-add to DB |
| 3 | No signals found | Unknown / needs_review | Low | Flag for user review |

### Tier 1 — Launcher Markers (HIGH confidence)

The scanner checks for known launcher-specific files at the folder root:

| Store | Signal | Match Type |
|-------|--------|-----------|
| GOG | `goggame*` files (`.info`, `.hashdb`, `.ico`, `goggame.dll`) | prefix |
| EA | `__Installer/` directory | dir |
| Ubisoft | `uplay_install.manifest`, `uplay_install.state`, `uplay_r*_loader*.dll` | exact |
| Ubisoft | `uplay_download/` directory | dir |
| Ubisoft legacy | `UbiStats.dll` in root or immediate child directory | exact |
| Epic | `.egsstore/`, `.egstore/` directories | dir |
| Blizzard | `.battle.net/` directory, including one-level container child | dir |
| Xbox / Microsoft Store | `default-metadata.json` | exact |
| Rockstar | `title.rgl` | exact |
| Steam Emulator | root `steam_api64.dll`, root `steam_api.dll`, or `steam_emu.ini` (root, child, or UE Steamworks path) | exact |

First match wins. Priority order: GOG → EA → Ubisoft → Epic → Blizzard →
Xbox → Rockstar → Steam Emulator → Standalone.

Steam Emulator is intentionally late in priority. Many legitimate games bundle
Steamworks SDK files under paths such as
`Engine/Binaries/ThirdParty/Steamworks/`; those files alone are **not** a
valid Steam Emulator signal. Treat as Steam Emulator only when `steam_emu.ini`
is present or a root-level `steam_api*.dll` appears outside a Steam library.

### EA `__Installer/` Directory
The `__Installer/` directory at game root is the primary EA detection marker.
EA games may also have files with a `_DiP_Staged` suffix (update staging),
but this is only confirmed in staged/incomplete installs — the pattern may
differ in complete installations. `__Installer/` alone is sufficient for
EA detection.

### `.lnk` Shortcuts
Windows shortcut (`.lnk`) files may appear at game root (e.g.,
`Launch The Witcher 3 - Wild Hunt - Game of the Year Edition.lnk`).
These are not used for launcher classification but may point to the
primary game executable for launch operations.

### Tier 2 — Standalone Game Structure (MEDIUM/LOW confidence)

If a folder has no store marker but has a recognized game-folder structure, it
is classified as *Standalone*.

Standalone signals currently include:

| Signal | Meaning |
|--------|---------|
| Root `.exe` after noise filtering | Typical standalone install |
| Root `.lnk` shortcut | Older installer/shortcut-based game folder |
| `Engine/` plus `*/Binaries/Win64/*.exe` | Unreal Engine game with nested executable |

The user can override this in the application if needed.

Noise-filtered exe substrings: `installer`, `crash_reporter`, `unins`,
`crashhandler`, `vc_redist`, `dxsetup`, `oalinst`, `setup`, `uninstall`,
`dxwebsetup`.

### Tier 3 — Unknown / Needs Review

Folders with no launcher/store signal and no standalone game-structure signal
cannot be reliably classified by automation. They are listed as unrecognized
for manual investigation and future signal improvement.

These entries are presented to the user for manual classification in the
application UI.

---

## Container Detection

Container folders (publisher groupings like `ubi/`, `EA/`, `COD/`) are
auto-flattened by one-level recursion. A folder is a container ONLY if:

1. It has no launcher markers at root (Tier 3 classification)
2. It has no standalone game-structure signal
3. **At least one of its immediate children has launcher/store markers**

Child folders that only contain standalone signals do NOT make the parent a
container — those executables may be utility/support folders inside an actual
game tree.

Containers are recursed into; their launcher/store child folders are emitted
with a combined path such as `EA/Battlefield 2042` or
`ubi/Tom Clancy's Rainbow Six Siege X`.

---

## Game Engine Detection

Engine detection is separate from store detection. It uses local filesystem
signals only; if no reliable signal exists, the engine is reported as
`Unknown`. PCGamingWiki or other metadata sources may later enrich or override
this value.

Current local engine signals:

| Engine | Signal |
|--------|--------|
| Unreal Engine | `Engine/` plus `Engine/Binaries/` or `*/Binaries/Win64/` |
| Unity | Root `UnityPlayer.dll` plus a root `*_Data/` directory |
| RAGE | Rockstar `title.rgl` plus `common.rpf` |
| Frostbite | Root `Engine.BuildInfo_Win64_retail.dll` |

Store SDK folders are not engine signals. For example,
`Engine/Binaries/ThirdParty/Steamworks/` may appear in Unreal Engine builds
from multiple stores and is not enough to infer Steam origin.

---

## Executable Metadata Extraction

When folder names and executable names are abbreviated (`DG`, `HORGOW`, etc.),
name resolution should proceed in this order:

1. Store manifest name if available (GOG `.info`, Steam ACF, Epic `.item`, etc.)
2. Folder name and lightweight executable-name candidates
3. Optional PCGamingWiki lookup to verify likely candidates
4. PE version metadata only for entries still unresolved/weak after PCGW lookup

The scanner can inspect Windows PE version resources on likely game executables
as a fallback. Useful fields include:

| PE Field | Use |
|----------|-----|
| `FileDescription` | Best local candidate for user-visible game name |
| `ProductName` | Secondary candidate for game name |
| `OriginalFilename` | Helps distinguish game exe from launchers/helpers |
| `CompanyName` | Publisher/developer hint only |

Python research tooling may use the `pefile` package for this, but only as an
optional fallback because full PE parsing can be slow on large libraries. The
actual C# Windows application must implement equivalent extraction with
.NET/Windows PE version resource APIs (for example `FileVersionInfo`) and must
not depend on Python helper scripts at runtime.

Entries that cannot be verified by store manifests, name/exe candidates,
PCGamingWiki, or PE metadata should be flagged for deeper manual inspection.

---

## Executable Collection

Executables are collected via a **depth-limited iterative walk** (stack-based
`os.scandir`, max 4 levels deep). This avoids the performance cost of
`os.walk` or `rglob` on large game trees while still catching common
patterns like `Binaries/Win64/game.exe`.

Results are sorted by file size descending. The first entry is the most
likely main game executable.

---

## Data Fields

Each entry in the JSON output:

| Field | Type | Description |
|-------|------|-------------|
| `folder` | string | Subdirectory name (prefixed with container path if nested). |
| `path` | string | Absolute path (platform-native). |
| `store` | string | GOG, EA, Ubisoft, Epic, Steam Emulator, Standalone, or Unknown. |
| `engine` | string | Locally detected engine: Unreal Engine, Unity, RAGE, Frostbite, or Unknown. |
| `exe` | string | Best local executable candidate when available. |
| `exe_metadata` | object | Optional PE version resource fields from the selected executable. |
| `confidence` | string | "High" if marker found, "Low" otherwise. |
| `markers` | string[] | Matched marker filenames (case-preserved from disk). |
| `exe_count` | integer | Number of non-filtered executables found. |
| `exes` | object[] | Sorted list: `{path, size, name}`. |
| `gog_metadata` | object\|null | If GOG: `{title, game_id}` from `goggame-*.info`. |
| `needs_review` | bool | True when `store` is Unknown; signals manual classification needed. |
| `container` | string\|null | If nested inside a container folder, the container's folder name. |

---

## Usage

```bash
# Basic scan — classify every subfolder
python tools/list_standalone_games.py /path/to/games

# Exclude Steam libraries (detected through ACF parsing)
python tools/list_standalone_games.py /path/to/games \
  --steam-libraries "/path/to/SteamLibrary1" "/path/to/SteamLibrary2"
```
