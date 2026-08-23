# Epic Games Store — Manifest Schema

## Purpose
Document the Epic Games Store installation format and manifest schema for detection, metadata extraction, and migration.

## Reference

**The definitive format documentation for Epic Games Store `.item` manifests lives at:**

➡️ **`docs/research/epic_item_format.md`** (120 lines)

That document covers:
- `.item` JSON manifest structure
- Required fields for detection: `AppId`, `AppName`, `DisplayName`, `InstallLocation`, `LaunchExecutable`
- Optional fields: `Categories`, `Metadata`, `Requirements`
- Parsing approach for C# implementation

## This File Exists For

This file exists in `docs/research/` for consistency — all launcher format docs live here. It serves as a stable reference pointer to the main Epic format documentation.

## Detection Summary

### Primary Signal: `.egstore/` or `.egsstore/` directory

An `.egstore/` or `.egsstore/` directory at the game folder root identifies an Epic Games Store-managed game. These directories contain:
- `catalog.item` — cached catalog data
- `manifests/<AppId>.item` — the install manifest (JSON)

### Marker Check (from `_check_epic()`)
```python
(d / ".egstore").is_dir() or (d / ".egsstore").is_dir()
```

### Install Locations

| Method | Path / Key | Notes |
|--------|-----------|-------|
| Default games root | `C:\Program Files\Epic Games\` | Per-game subfolder |
| Registry | `HKCU\Software\Epic Games\EOS\ModSdkV2\` | SDK paths (not primary) |
| Manifests root | `%PROGRAMDATA%\Epic\EpicGamesLauncher\Data\Manifests\` | `.item` files for all installed games |
| Launcher install | `%LOCALAPPDATA%\EpicGamesLauncher\` | Launcher config/data |

### Launch Patterns

1. **Epic URI scheme:** `com.epicgames.launcher://apps/<AppId>?action=launch` — preferred for full launcher integration
2. **Direct executable:** `<GameRoot>/<LaunchExecutable>` — works standalone

## Migration Considerations

See `docs/research/epic_item_format.md` (Migration section) for full details. Summary:

**GamingCommander does NOT move game files.** The user moves files with OS tools.
GamingCommander repairs the store registration after the move.

- **Manifest Repair** — The `.item` file is JSON; after the user moves the game folder,
  GamingCommander detects the path mismatch and updates the `InstallLocation` field.
  This is the only operation GamingCommander performs.
- **No file movement** — 100 GB game transfers are handled by the user's file manager.
- **No junctions/symlinks** — The app only touches registration files, not game data.

## Additional Tools

| Tool | Location | Purpose |
|------|----------|---------|
| `tools/decode_manifest.py` (368 lines) | Parse Epic `.manifest` binary + generate `.item` files |
| `tools/parse_manifest.py` (146 lines) | Simpler Epic `.manifest` parser |
| `tools/epic_search.py` (74 lines) | Query Epic API for namespace/catalog metadata |
| `tools/decrypt_manifest.py` (114 lines) | Decrypt encrypted Epic manifests |

## External reference (binary `.manifest`, not Legendary)

**[meszmate/manifest](https://github.com/meszmate/manifest)** — Go parser our `tools/parse_manifest.py` cites. It decodes the **binary** `.egstore/*.manifest` (header, metadata, file list, chunks). It does **not** call store GraphQL.

From that library, a parsed manifest already has:

- `Metadata.AppName`, `BuildVersion`, `LaunchExe`, `LaunchCommand`, prereqs
- `Header.SHAHash` (manifest body hash)
- `TotalInstallSize()` — **sum of file sizes** (real `InstallSize`, not 0, not invented)
- `TotalDownloadSize()` — sum of chunk sizes
- Custom fields, file tags, chunk GUIDs

CDN `BaseURLs` are **game-specific and come from the authenticated Epic API**. Their README: auto-discovery is out of scope.

That project does **not** contain a store catalog for “Death Stranding”. Storefront GraphQL is a different API (`store.epicgames.com/graphql`).

## References
- `docs/research/epic_item_format.md` — `.item` JSON + GraphQL stub notes
- [meszmate/manifest](https://github.com/meszmate/manifest) — binary `.manifest` format
- `tools/decode_manifest.py` — Python port + `.item` generation
- `tools/parse_manifest.py` — “Based on meszmate/manifest Go implementation”
