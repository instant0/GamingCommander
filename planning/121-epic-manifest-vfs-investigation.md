# Plan 121 — Epic manifests → VFS, Missing/Orphaned, F2 offer (investigation)

**Status:** COMPLETE (2026-08) — shipped: one Epic catalog root via F2 "Add Epic Games Store", Installed/Missing rows, orphan `.item` write (`EpicItemWriter`). Completion record: `META/COMPLETED/plan-121-epic-catalog-vfs.md`.  

**C# files (new, small):**

| File | Job |
|------|-----|
| `EpicItemClassifier.cs` | Base vs `addons` |
| `EpicItemCatalog.cs` | Read all `*.item` from a Manifests dir |
| `EpicLibraryScanner.cs` | Catalog → `GameEntry` (Installed / Missing) |
| `EpicItemWriter.cs` | `.mancpn` + exe → ProgramData `.item` |

Wire: `LibraryManager` if type Epic or folder is a Manifests dir; F2 **Add Epic Games Store**; details **Write Epic .item** on Orphaned. Launch = resolved exe (not a new URI policy).  

---

**Previous status:** I1 done. Regen via `.mancpn` accepted by launcher.  
**Depends on:** Plan 109 (folder enrich).  
**Live C# today:** Folder scan only. ProgramData `*.item` **enriches** a folder you already added. No VFS rows from manifests. No F2 Epic offer. No Epic Missing/Orphaned. No C# `.item` writer.

**Why:** More Epic installs than ProgramData `.item` files. Manifests also point at several install parents. Need a Steam-like catalog pass + honest regen.

---

## Already true (do not re-research)

| Fact | Where |
|------|--------|
| Parse `.item` / `.mancpn`; match `InstallLocation` | `EpicManifestParser` (Plan 109) |
| Default Manifests dir | `EpicManifestPaths.DefaultManifestsDir` |
| **`.item` generation from binary `.manifest` + Epic store GraphQL** | `tools/decode_manifest.py` (`generate_item` + `search_epic_namespace` → `store.epicgames.com/graphql` `searchStore`) |
| Same GraphQL CLI | `tools/epic_search.py` |
| Field list + GraphQL snippet | `docs/research/epic_item_format.md` |
| Live launcher findings (dev vs public namespace, relative exe, incomplete flag) | `docs/EPIC-MANIFEST-ENRICHMENT.md` §7.4 |
| GraphQL **not in the C# app** | Plan 109/119 deferred (app extras). Tooling already used it for regen. |
| **Base vs DLC + orphan `.egstore` handling** | `docs/research/epic_item_format.md` (2026-08). Live `MainGame*` fields are empty — do not use them. |
| Plan 119 note | A past app-side GraphQL probe saw 500/404 — **re-probe** before porting, do not assume dead |

Generation was tested in **Python tools**, not in GamingCommander.exe. `make_epic_item()` in mock data only proves JSON shape, not launcher accept.

---

## I1 — Inventory ProgramData `.item` — **DONE** (this machine, counts only)

Default Manifests folder is present and readable.

| | Count |
|--|------:|
| `*.item` files | 16 |
| `InstallLocation` set | 16 |
| Folder exists | 16 |
| Folder missing | 0 |
| `bIsIncompleteInstall` | 0 |
| Empty `DisplayName` / catalog ids / `AppName` | 0 |
| Empty `LaunchExecutable` | 10 (likely DLC / extra items) |
| Distinct install **parents** | 3 |

No game names or paths recorded here.

**Implications**
- ProgramData is a usable catalog (all 16 paths exist).
- **I3 Missing** is real as a status type, but **not** this box’s problem.
- User “more games than manifests” = **I4 Orphaned** (folders without a `.item`), not missing disks.
- **I6** should offer the **3 parent roots**, not ProgramData itself.

---

## Remaining investigation tasks

### I2 — Manifests whose parent is not an F2 root

**What:** Compare the 3 `InstallLocation` parents to configured `settings.json` roots. Rule: suggest each **parent folder** as an Epic (or mixed) library root. Do not add ProgramData as a game root.  
**Done when:** I6 uses the **3 parents**. VFS skip rule is already written: `addons` / empty exe / incomplete — see `docs/research/epic_item_format.md`.

### I3 — Missing (manifest, folder gone)

Keep as Steam-parallel status. This inventory: **zero**. Implement after I2/I4 unless another machine shows Missing.

### I4 — Orphaned (`.egstore`, no matching ProgramData `.item`)

**What:** This is the extra-install case. Detect via folder scan signal vs catalog of InstallLocations from I1.  
**Done when:** Scan implementation uses the orphan rule in `docs/research/epic_item_format.md` (`.egstore` + `.mancpn` / `.manifest`, no ProgramData match). Handling is documented; code not written.

### I5 — Regen — **updated verdict** (not a blank)

| Path | Proven? | Enough for Epic Launcher? | Enough for our VFS? |
|------|---------|---------------------------|---------------------|
| Binary `.egstore/*.manifest` → JSON fields (AppName, launch exe, guid) | Yes — `decode_manifest.py` `parse` | No ids | Yes (name + exe) |
| Local `.mancpn` + folder exe → identification `.item` | Yes — `tools/generate_epic_item.py`. **Live: Epic Launcher accepted the written `.item` (orphan folder, no GraphQL, no binary-manifest parse).** | **Yes** (this case) | Yes |
| `searchStore` GraphQL by **keywords** then `generate_item` | Yes — **tools**, used for regen | That was the working path | Yes |
| C# writer + GraphQL in the app | **Never shipped** | — | — |

**Working regen recipe (tools, already written):**

1. Parse `.egstore` binary `.manifest`  
2. `POST https://store.epicgames.com/graphql` `searchStore(keywords: "<name>")`  
3. Fill `CatalogNamespace`, `CatalogItemId`, `DisplayName`  
4. Write `.item` JSON (`generate_item`)

**I5 leftover (do before C# writer):**

1. Re-probe `searchStore` (Plan 119 saw 500/404 from the *app* extras probe — may be a different query).  
2. Prefer **local `.mancpn`** when public namespace is present (no HTTP).  
3. Product gate: same as other HTTP — F2 online flag + Online chip. Not silent.  
4. Decide: write into ProgramData Manifests (launcher sees it) vs identification-only for VFS (like a thin Steam ACF).

Until 1–4: **do not implement** the C# writer. Do not treat GraphQL as “unknown” either — treat it as **ported from a working tool**, pending re-probe.

### I6 — F2 offer

If default Manifests dir has `*.item`, suggest adding each distinct **InstallLocation parent** (here: 3). Checkbox, no auto-add.

### I7 — Orphan → Write `.item`

After I4 + I5 leftover: list orphans; write only with `.mancpn` ids **or** successful store lookup. Never invent catalog UUIDs.

### I8 — Scan cost

Enumerate `*.item` only + `Directory.Exists(InstallLocation)`. Skip empty LaunchExecutable / non-application / incomplete. No tree walk, no EngineDetector.

### I9 — Contract

After product decision: `docs/ONLINE-AND-DATA.md` — read ProgramData; optional user-started `.item` write; optional `store.epicgames.com/graphql` only when regen needs ids and online is on.

---

## Order

I1 ✅ → **I2** (F2 parents) → **I4** (orphans vs catalog) → **I5 leftover** (re-probe + write target) → I6/I7/I8/I9. I3 when needed.

---

## Success criteria

- [x] Real Manifests inventory (counts; no titles/paths in repo)
- [ ] Rule: suggest InstallLocation **parents**
- [ ] Skip rule for DLC / no LaunchExecutable
- [ ] Orphan definition vs the 16-item catalog
- [ ] GraphQL re-probe result + write-target decision
- [ ] F2 sketch (3 parents)

## Out of scope until then

C# Epic catalog scanner, F2 checkboxes, `.item` writer, enabling GraphQL in the app, SyncMove for Epic.
