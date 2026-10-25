# Plan 129: Store Capabilities and Launcher-Driven Inventory

**Created:** 2026-10-25
**Priority:** P1
**Status:** PROPOSED — matrix published from four read-only audits; implementation split into Phase M.
**Source:** code trace of every store scanner/parser, `docs/research/*`, `docs/detection/*`, and four read-only sub-agent audits.

---

## 0. The question

For each store, do we know:

1. how to find the launcher itself;
2. where the launcher records the games it believes are installed;
3. which of those records we already read;
4. how accurate our metadata and exe selection are;
5. whether we can detect installed-but-missing / moved / orphaned games;
6. whether the source is safe to read (plain file vs SQLite/encrypted/package API).

Then: can we offer a first-run **"Detect game launchers?"** flow that imports each launcher's own
inventory and populates the game list through the normal scanner pipeline?

## 1. Verdict

There was no single matrix; coverage was spread across detection docs and research. The audits show:

- **Steam** has a real launcher locator and inventory reader (ACF/VDF), but the locator is narrower than documented and ACF metadata is partially discarded.
- **Epic** has a real manifest catalog reader, but no launcher locator and does not read `LauncherInstalled.dat`; orphan discovery is dead code.
- **GOG, EA, Ubisoft, Battle.net, Xbox, Rockstar** are **detection-only**: registry/filesystem signals type a folder that the user already added. Their launcher inventories are **not imported**.
- **Safe read-only inventories:** Steam (ACF/VDF), Epic (ProgramData `.item`/`.mancpn`), GOG (per-game registry + `.info`), Rockstar (`Settings.json` + registry).
- **Unsafe/undocumented until verified:** EA App `IS` (encrypted), Ubisoft `uplay_install.manifest`, Battle.net `product.db` (SQLite schema unverified), Xbox AppX (package API/ACL).
- **Epic `.item` write is not registration** (server/account-gated). Keep detection read-only.

## 2. Capability matrix

Legend: ✅ shipped, ⚠️ partial/unverified, ❌ missing.

| Capability | Steam | Epic | GOG | EA | Ubisoft | Battle.net | Xbox | Rockstar |
|---|---|---|---|---|---|---|---|---|
| Launcher install located by app | ⚠️ HKLM `Valve\Steam\InstallPath` only | ⚠️ fixed ProgramData manifests path only | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Inventory source the launcher trusts | `libraryfolders.vdf` + `appmanifest_*.acf` + `common/` | ProgramData `*.item`; `LauncherInstalled.dat` documented | per-game registry + `.info`; Galaxy SQLite unverified | encrypted EA App `IS`; legacy Origin `.mfst`; registry | `Installs\{gameId}\InstallDir` + `uplay_install.state` | `%ProgramData%\Battle.net\Agent\product.db` (SQLite, unverified) | AppX package registration (`Get-AppxPackage`) | `Settings.json` + per-game registry |
| Inventory read by app | ✅ ACF/VDF + `common/` | ⚠️ ProgramData `.item` only | ❌ registry classifies only | ❌ | ❌ | ❌ | ❌ | ❌ |
| Per-game metadata read | ⚠️ appid/name/installdir/state/size/build (some discarded) | ⚠️ display/install/exe/catalog ids | ⚠️ `.info` name/id/exe/args | ⚠️ InstallLog name/studio | ⚠️ readme title only | ⚠️ `.build.info` product code | ❌ | ❌ (RAGE engine only) |
| Executable discovery | ⚠️ heuristic; launch via `steam://` | ⚠️ `.item` exe else scored heuristic | ⚠️ heuristic, `.info` fallback | ⚠️ heuristic | ⚠️ heuristic | ⚠️ heuristic | ⚠️ heuristic; no package exe | ⚠️ heuristic |
| Health states | ✅ Installed/Moved/Orphaned/Missing | ⚠️ Installed/Missing/Orphaned (fuzzy) | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Startup incremental sync | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Read-only safety | ✅ | ✅ identification only | ⚠️ registry/`.info` safe; Galaxy DB unverified | ⚠️ encrypted `IS`; legacy `.mfst` safe | ⚠️ registry safe; manifest encrypted | ⚠️ SQLite schema unverified | ⚠️ package API/ACL | ✅ `Settings.json`/registry |

### What is missing per store

- **Steam:** HKCU/`WOW6432Node`/default-path locator fallbacks; VDF parsed as VDF (not line-split); ACF required fields and `StateFlags` interpreted; `LastUpdated` persisted; ACF map keyed by appid (not first-wins `installdir`); cross-library repair; file-level integrity.
- **Epic:** launcher locator; `LauncherInstalled.dat` inventory; wire orphan discovery; read `InstallSize`/`AppVersionString`/`InstallationGuid`/`CompleteManifestPath`; honor a configurable manifests root; harden JSON parsing; optional `com.epicgames.launcher://` launch.
- **GOG:** launcher locator; enumerate `GOG.com\Games` as inventory (not just classification); read full registry metadata; merge all `.info` files; prefer `.info` exe; health states; verify Galaxy SQLite.
- **EA:** launcher locator; `installerdata.xml` parser (content id/title/launcher path/version); legacy Origin `.mfst`; enumerate registry inventory; EA content ID; verify a complete modern install.
- **Ubisoft:** launcher locator; enumerate `Installs\{gameId}` as inventory; store the game ID; parse `uplay_install.state`; persist publisher; health states.
- **Battle.net:** launcher locator; read Agent `product.db` read-only (schema verification first); distinguish Agent DB from game-root `.product.db`; metadata beyond product code.
- **Xbox:** package locator (`Get-AppxPackage`/Windows package API); read `default-metadata.json` and `appxmanifest.xml`; package identity/exe; `shell:appsFolder` launch; health based on package presence.
- **Rockstar:** launcher-key locator (currently skipped); read `Settings.json`; title ID from `title.rgl`; registry inventory; health states.

## 3. Proposal (reviewed and revised)

**Original proposal:** on first run, offer "Detect launchers?", find Steam/Epic/Ubi/etc., run a
unique detection method to find the install and manifest locations, then populate the game
inventory as a normal file scanner does.

**Reviewed:** the direction is right, but the original assumes every launcher exposes a readable
manifest. Three revisions:

1. **Verify before building.** Some inventories are encrypted or undocumented (EA App `IS`,
   Ubisoft manifest, Battle.net `product.db`, Galaxy DB). Confirm the source per store first.
2. **Store inventory is a hint, disk is truth.** Import the launcher's records, then cross-check the
   folder and exe. Mark `Installed`/`Moved`/`Missing`/`Orphaned`; never claim a game is healthy just
   because the launcher says so. This is also how we surface the launcher's own wrong entries.
3. **Reuse the scanner, do not build a parallel inventory.** Map each record to a normal `GameEntry`
   with a confidence tier, then run the existing exe/title enrichment. One pipeline, one model.

**First run (opt-in):**

```
First run
  → LauncherDiscoveryService probes each store (registry / known path / package API)
  → Preview: "Steam found (42 games), Epic found (8), GOG not found …"
  → Per store, the user chooses: Index this store / Check on every launch / Enrich new games
  → Persist the policy (all off by default)
  → For each enabled store: StoreInventoryReader returns the launcher's list (read once)
  → For each record: stat the folder + launch exe only — no directory walk
  → Map each record to GameEntry {Store, StoreId, InstallPath, StoreState, DetectionOrigin}
  → Persist via the existing database; never write store files
```

**Every later launch (only for stores with CheckOnLaunch):**

```
Startup (non-blocking, time-boxed)
  → if the manifest fingerprint (path + mtime + size) is unchanged: skip the store
  → read the store inventory once (no directory walk)
  → stat each record's folder + launch exe (existence only)
  → diff by StoreKey against the in-memory index (O(manifest), not O(all files))
  → new → add; changed path/version → update; gone → mark Missing (never delete)
  → if EnrichNewGames: run the normal folder scan on newly added paths only
  → log one StoreSyncReport line; failures are non-fatal
```

**Constraints:** read-only; per-store opt-in; all features default off; startup sync never blocks
the UI; removals are marked `Missing` (never deleted); no decryption or manifest writing; unsupported
stores fall back to the normal folder scan.

### 3.1 Two complementary sources

| Source | What it answers | Cost |
|---|---|---|
| **Store manifest** | "What does the launcher believe is installed?" | one file read + one stat per record |
| **File scan** | "What is actually on disk?" | directory walk; used for discovery/enrichment, not store sync |

The store check is **manifest → stat**, not **referenced files → walk**. The file scan remains the
fallback for folders with no store record and the opt-in enrichment step for new games.

### 3.2 Store check algorithm (fast path)

1. Locate the launcher's inventory file(s) (see the matrix).
2. If the fingerprint (path + last-write + size) matches the last run, skip the store.
3. Read the inventory once and map it to records.
4. For each record: `Directory.Exists(installPath)` and, when known,
   `File.Exists(Path.Combine(installPath, launchExe))`. Nothing else.
5. Diff by `StoreKey`; apply add/update/missing.
6. Report counts + elapsed; never throw.

No `EnumerateFiles`, no title/engine detection, no PE reads in this path.

### 3.3 Fast local inventory

- Give every entry a stable `StoreKey` (`"Ea:<contentId>"`, `"Steam:<appid>"`, …).
- Build an in-memory index at load: `ByStoreKey`, `ByPath`, `ByStore`.
- The diff is `inventory ∩ index`, not a scan of `games.json`.
- Keep `games.json` as the source of truth; do **not** denormalize a second store list into it.
  Rebuild the index on load and on mutations. Add a persisted cache only if load time is measured
  to be a problem.
- Record `DetectionOrigin` (`StoreManifest` / `FileScan` / `User` / `Merged`) and
  `LastStoreSeenUtc` so the UI can explain where an entry came from.
- Persist the per-store manifest fingerprint and last-run summary in a small disposable cache
  (`data/store-sync.json`), not in `settings.json` and not inside `games.json`. The cache is
  rebuildable and can be deleted safely.

### 3.4 Accuracy rules

| Store says | Folder | Exe | Result |
|---|---|---|---|
| installed | exists | exists | `Installed` |
| installed | exists | missing | `Installed` + `ExeMissing` (suggestion, not `Missing`) |
| installed | missing | — | `Missing` |
| update required | exists | exists | `Installed` + `UpdateAvailable` |
| not listed | exists | exists | `Orphaned` / `DiskOnly` (file scanner) |
| path differs from the record | exists elsewhere | exists | `Moved` |

Never infer "not installed" from a missing exe alone.

## 4. Design rules

- Read-only. Never write, repair, or decrypt a store file.
- Store inventory is evidence, not truth. Disk existence wins for `Installed`.
- One `GameEntry` model and one enrichment pipeline; no second game list.
- Confidence tiers: authoritative store record → `Secure`/`Locked`; disk-only → `Candidate`.
- Every store's inventory source must be documented and verified before code.
- First-run discovery never blocks startup; it is a skippable offer.
- Every store feature defaults **off**; the user opts in per store.
- Startup sync reads inventory only and is time-boxed; it never walks game folders (enrichment is a
  separate opt-in and runs only for newly added games).
- A game that disappears from a launcher is marked `Missing`, never deleted.
- The Setup screen is the single place to enable/disable indexing, launch checks, enrichment, and
  per-store detection.
- The store path is **manifest-first, stat-only**: one inventory read + existence checks; no recursive
  enumeration, no PE/metadata reads.
- Skip an unchanged store by manifest fingerprint before parsing it.
- Keep the store-reported state separate from the disk-derived `InstallState`.
- `DetectionOrigin` records which source created or last confirmed an entry.
- `ExeMissing` is a repair suggestion, not a missing game.

## 5. Out of scope

- Epic registration / `.item` re-creation (server-gated; read-only only).
- Store client installs, downloads, updates, DRM, anti-cheat.
- Writing or repairing store manifests (the existing user-started Steam ACF identification is separate).
- Decrypting EA App `IS`, Ubisoft `uplay_install.manifest`, or Galaxy DBs before verification.

## 6. Success criteria

- The matrix is published as a durable reference and each row cites code or a verified mechanism.
- Every store reports a discovery outcome: `Found` (with inventory source), `Not found`, or `Unsupported`.
- Imported entries carry the correct source, install path, stable store ID where available, and install state.
- Launcher-vs-disk mismatches are surfaced as suggestions; no writes occur.
- Per-store policy persists; startup sync adds new games, updates changes, and marks removals without
  duplicates or deletions.
- The Setup screen controls indexing, launch checks, enrichment, and detection per store.
- A launch with no changes completes within a small time budget and does not block the UI.
- A no-change launch reads one manifest per enabled store (fingerprint-skipped when unchanged) and
  performs only existence checks — no recursive directory walk.
- The store diff is O(manifest size) via `StoreKey`, not O(all games).
- `ExeMissing` / `UpdateAvailable` are surfaced separately from `Missing`.
- No regression to the existing Steam/Epic scanners or the detection matrix.
