# Sample Data & Research Input

This document records the real sample data used to drive development decisions. All data is from the user's local machine.

---

## Steam Library — `appmanifest_*.acf` Files

**Known structure:**
- Stored in `<SteamLibrary>/steamapps/`
- Filename pattern: `appmanifest_<AppID>.acf`
- Format: Valve's key-value format (similar to INI with quoted keys)
- Key fields: `AppID`, `AppName`, `installdir`, `StateFlags`, `LastUpdated`

**ACF format example (known structure):**
```
"AppState"
{
    "AppID"        "107410"
    "AppName"      "Arma 3"
    "installdir"   "Arma 3"
    "StateFlags"   "4"
    "LastUpdated"  "1234567890"
}
```

- Multiple ACF files exist per library root.
- Cross-reference `installdir` against `steamapps/common/<folder>` to find the actual install path.
- `libraryfolders.vdf` in the same directory lists all configured Steam library roots.

---

## Standalone Games — `Y:\Games`

**Known structure:**
- Top-level directories are game folders.
- Detection signals per launcher:
  - **Epic Games Store**: subfolder `.egsstore` (confirmed marker, appears inside game directory).
  - **Ubisoft Connect**: presence of Ubisoft Connect executables or registry keys.
  - **Steam**: cross-reference against Steam ACF `installdir` names.
  - **Standalone**: no launcher signal found.
- `.exe` files under each folder to identify the main executable.

---

## Library Roots for Development

| Path | Type |
|---|---|
| `<SteamLibrary>` | Steam |
| `<StandaloneGames>` | Standalone / Multi-launcher |

*(Concrete paths are used only by local Python scripts in `tools/` — not disclosed to the Agent.)*

### Explicit Folder Type Classification

When a user adds a library root, they must select its **default type** from: `Steam`, `Epic`, `EA App`, `GOG`, `Ubisoft Connect`, `Standalone`. This is the authoritative source of truth — not derived from marker files.

**Two-level model:**
- **Root level:** each library root has a `defaultType`. All sub-folders inherit this unless overridden.
- **Folder override:** individual game folders can be tagged with a different type. Override takes precedence. Example: `Y:\Games` (default: Standalone) contains `DyingLight2StayHuman/` tagged as `Epic` because it has an `.egsstore` inside it.

**Steam folders have a fixed structure:** `<SteamLibrary>\steamapps\common\<GameName>`. This means every sub-folder in a Steam library root is a Steam game — no tagging needed within a Steam root.

**Epic/GOG/EA/Ubisoft roots have no fixed structure.** They can be anywhere and contain mixed types. Per-folder tagging is essential for accuracy.

**Why not infer from marker files?** Steam `.acf` files and Epic `.egsstore` folders can appear outside their native launcher roots (e.g. a game copied manually for modding, or a portable EGS install). Marker files alone are not reliable — the user's intent is the ground truth.

---

## Research & Data Collection Approach

### Python Helper Scripts — Development Environment Only

Python scripts live in `tools/` and **never disclose their output or findings back to the Agent**. They are purely for the developer to validate parsing logic and confirm file format assumptions before writing C#.

**Why Python first:**
- Faster iteration for format validation.
- No C# compilation required — run directly on Windows or WSL.
- Output (JSON/CSV/text) used internally to drive test fixtures and spec documents.
- C# implementations come second, informed by what Python scripts discover.

**Privacy constraint:** Scripts operate on local machine data. Research findings are documented as generic structural notes in `docs/research/`, never as concrete paths, game names, or data that would disclose the developer's library.

### Scope — One Representative Sample Per Format

We already know the structural base of these formats. Full dataset analysis is not needed. Scripts validate the approach against:

| Format | Sample |
|---|---|
| Steam ACF | One `appmanifest_*.acf` file |
| Steam library folders | `steamapps/common/` listing |
| Epic manifest | One JSON manifest entry |
| Standalone directory | `Y:\Games\` — one folder with `.egsstore` marker |

The scripts confirm the parsing logic is correct; they do not enumerate the full library.

**Suggested scripts (Phase 1.2):**

| Script | Purpose | Scope |
|---|---|---|
| `tools/parse_steam_acf.py` | Parse one `.acf` file. Validate field extraction. | Single file |
| `tools/list_steam_common.py` | List `common/` folders. Validate cross-reference with ACF AppID. | One library root |
| `tools/list_y_games.py` | Scan one `Y:\Games` folder, detect `.egsstore`, list `.exe` files. | One folder |
| `tools/parse_epic_manifests.py` | Read one Epic JSON manifest. Validate schema. | One file |
| `tools/steam_registry.py` | Read Steam install path from registry. | Read-only |
| `tools/validate_approach.py` | Run all above, confirm parsing produces correct structure. | One sample each |

All output stays in `tools/` or `docs/research/` as generic structural documentation. No concrete game names, paths, or library contents are written to files that could be read by the Agent.

---

## Next Steps

1. Write `tools/parse_steam_acf.py` and validate against one ACF file.
2. Write `tools/list_steam_common.py` and validate cross-reference logic.
3. Write `tools/list_y_games.py` and confirm `.egsstore` detection.
4. Write `tools/parse_epic_manifests.py` and validate Epic JSON schema.
5. Run `tools/validate_approach.py` to confirm all parsers produce correct structure.
6. Document generic structural findings in `docs/research/` — format schemas only, no library specifics.
7. Feed findings into `IGame` interface design (Phase 1.0).
