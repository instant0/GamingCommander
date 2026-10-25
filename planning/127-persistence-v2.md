# Plan 127 — Persistence v2: Typed Game Records, Identity & Detection Provenance

**Status:** IN PROGRESS — A1–A6 and B7–B13 done. C deferred.
**Type:** Data-model refactor (pre-ship; **no back-compat** — clean cut, no migration, no dual-write).
**Informed by:** `docs/DATA-FORMAT.md`, `META/CODE_MAP.md`, `planning/123-detection-bugfixes.md` (tiers), `planning/124-anchor-library-game-db.md`, `planning/04-phase-2-syncmove.md`.
**Scope:** the four files under `{app-base}/data/` — `settings.json`, `libraries.json`, `games.json`, `games_metadata.json`.

### Back-compat policy (2026-10-14)

**Nothing has shipped. All persisted schemas are disposable.** Therefore:
- No migration, no dual-write, no "keep the legacy field until nothing reads it" transitional steps.
- Persisted key strings / property names may be renamed freely; contract pin tests that exist only to protect on-disk stability are removed.
- `docs/DATA-FORMAT.md` documents the **current** schema only (no "previous implementation not migrated" caveat).

### Implementation progress (2026-10-14)

- **A3 DONE** — `IdentificationTier` enum; `DetectionInfo` record; `DetectionTierDeriver`; wired into `FolderScanner`, `SteamLibraryScanner` (3 paths), `EpicLibraryScanner` (2 paths); persisted.
- **A2 DONE** — `PlatformMetadata` string bag **removed**; replaced by typed `DetectionInfo`, `StoreInfo`, `SteamAcfInfo`, `EpicInfo`, `LocationInfo` records. `PlatformMetadataKeys` + its contract test deleted. All writers/readers migrated (scanners, persistence, MainWindow repair/metadata, GameSetupWindow, ShellViewModel, MetadataLookupQueue).
- **A1 DONE** — `GameEntry` is composed of `Identity` / `Location` / `Launch` (positional) + `Detection` / `Store` / `Acf` / `Epic` (body); flat names kept as read-only accessors. All constructors, `with` expressions, DTO mapping, scanners, UI, and tests migrated.
- **A5 DONE (first slice)** — `IdentityKey`/`IdentityKeyKinds` + `IdentityInfo.IdentityKeys` + `LocationInfo.PreviousLocations`; `GameIdentity.WithRebuiltKeys`; reconciliation in `GamesDatabaseService.SetGamesForLibrary` (`FindRelocated`) relinks external moves (strong store-id keys match regardless of disk; weak folder-name key matches only when the old folder is gone) and preserves the existing `Id` + sidecar. **Deviation:** `Id` stays folder-derived (`GameEntryId.ComputeId`) rather than a fresh GUID — reconciliation preserves it across moves, which satisfies the requirement with less churn. Cross-library moves + an explicit `InstallState.Missing` marker are follow-ups.
- **A6 DONE (boundary made explicit)** — on inspection the sidecar's `SteamAppId`/`GogGameId`/`Engine` are **fetched** values (PCGW/Steam), not the detected `GameEntry.Store`/`GameEngine`; deleting them would lose the metadata-identity signal and the Steam fallback. Resolution: rename them `FetchedSteamAppId`/`FetchedGogGameId`/`FetchedEngine`, document that `games.json` is authoritative and the sidecar owns fetched enrichment only, and keep `GameEntry.Store` the winner in lookup paths. `GameMetadataRecord` XML docs + `docs/DATA-FORMAT.md` §5 updated.
- **A4 DONE** — `SchemaVersioning.Current` (Core) + `IsReadable` guard; all four files write `version` as current and discard a newer-version file on read. Lean mechanism (no speculative migrator registry).
- **B7/B8/B9 DONE** — `TrackingInfo`, `InstallInfo`+`InstallState`, `UserStateInfo`+`CompletionState` on `GameEntry`, persisted; `Install.State` wired via `InstallStateMap` in the scanners; launch tracking wired in `MainWindow.RecordLaunch`.
- **B10–B13 DONE** — `LaunchProfile[]` (B10), `RelationsInfo`/`GameKind` (B11), `Collection`+`CollectionsDatabaseService` (B12), typed `Tag`/`TagKind` replacing `List<string>` (B13).
- **C** — not started.
- **Verification:** build 0 errors; **601 tests pass** (173 Core + 1 Migration + 427 App).
- **Verification:** build 0 errors; **578 tests pass** (163 Core + 1 Migration + 414 App).

---

## 0. Context and principles

The format works for today's shipped features but mixes five concerns in one 18-param record and
routes store/detection facts through a 26-key `Dictionary<string,string>` (`PlatformMetadata`).
Nothing has shipped, so the schema is free to change.

**Principles**
1. **One owner per fact** — no field duplicated across files.
2. **Typed over stringly-typed** — records/enums, not string bags.
3. **Provenance is first-class** — record *why* an entry is a game and *how confident*.
4. **Scanned state ≠ user state** — a rescan must never clobber user customisation.
5. **Forward-compatible** — an `Extensions` escape hatch for unmodelled data.

**Blast radius (measured):** `PlatformMetadata` in ~15 src + ~12 test files; 16 `GameEntry`
constructor sites; key strings pinned by `PlatformMetadataKeysContractTests`. Therefore A1 proceeds
**additively first** (new records alongside existing fields), removing flat fields last.

---

## 1. Identity model (A5) — detailed

### The problem
`GameEntry.Id = MD5("{physicalLibraryRoot|folderName}")`. It is folder-based, which is the right
instinct: the **GAME is the folder; the executable is just the current launcher.** But it breaks on:

- external move / rename / drive-letter change (path changes);
- library re-anchoring is safe (anchor is metadata, not identity) — good;
- the sidecar (`games_metadata.json`) is keyed by `Id`, so any id break **orphans metadata**.

A large content update (v3 → v4, 32→64-bit, new launcher) **must not** change identity — and with
folder-based identity it doesn't. Good.

### Requirements
- Stable across content updates/patches (exe/launcher changes).
- Stable across moves the app performs (we know old→new).
- Recoverable across external moves/renames (re-link, not duplicate).
- Stable across re-anchoring.
- Sidecar + user state survive all of the above.

### Design
- **`GameId`** — app-owned stable id, created once (GUID; existing MD5 seeds initial value). Primary
  key for the sidecar and user state. **Never derived from the exe.**
- **`Location`** — current physical folder path (mutable).
- **`PreviousLocations[]`** — prior paths (reconciliation + display).
- **`IdentityKeys[]`** — natural keys for re-matching after a move:
  `{ kind: FolderName | SteamAppId | EpicCatalogItemId | GogGameId, value }`.
- **Reconciliation on scan:** if a known `GameId`'s `Location` is gone, look for a new folder whose
  `IdentityKeys` match → re-link (update `Location`, keep `GameId`). No match → `InstallState.Missing`.
- **Patch/update:** same folder → identity unchanged. The exe change is recorded as launch state
  (`LastExecutableChange`), never identity.
- **Deferred idea:** an opt-in `.gc-id` marker file inside the game folder to survive arbitrary
  moves — conflicts with the "never write to game installs" invariant, so deferred and opt-in only.

### Acceptance criteria
- Move a game in-app → `GameId` + sidecar preserved.
- External move/rename → detected and re-linked, or flagged `Missing` (never silently duplicated).
- v3→v4 exe/launcher change → identity preserved; new exe picked up as `Launch`.
- Drive-letter change (`D:`→`E:`) treated as an external move.

### Risks / open questions
- Folder-rename re-match heuristic when no store id exists (content-signal fallback? user confirm?).
- Path normalization must be case-insensitive + separator-insensitive on read.

---

## 2. File ownership (A6) — detailed

### The overlap today
`SteamAppId`, `GogGameId` and engine appear **both** in `games.json` (`PlatformMetadata` /
`GameEngine`) and in the sidecar (`GameMetadataRecord.SteamAppId/GogGameId/Engine`). That is
duplication with no defined winner.

### Why the sidecar exists
Plan 119 deliberately isolated **network-fetched** metadata from the VFS: fetching must not rewrite
`games.json`, and a failed fetch must not corrupt library state.

### Options
- **Option 1 — Scanned vs fetched split.** `games.json` = scanner + user state; sidecar = fetched only.
- **Option 2 — Single file.** Drop the sidecar; simplest truth, but every metadata fetch rewrites the
  whole game DB and couples network failures to library state. Rejected.
- **Option 3 — Keep two files, define ownership by origin, remove duplication. (Recommended.)**

### Recommendation (Option 3)
| File | Owns |
|------|------|
| `games.json` | Identity, Location, Launch, **Detection**, **Store ids**, Install state, UserState, Tracking |
| `games_metadata.json` | **Fetched enrichment only**: Developer, Publisher, ReleaseDate, Genre, Description, Metacritic, CoverArtUrl, OfficialWebsite, PcGamingWikiUrl, LastMetadataSource, LastUpdated, Details |

- Remove `SteamAppId` / `GogGameId` / `Engine` from the sidecar (read from `games.json`).
- Sidecar keyed by `GameId` (not path-derived).
- If metadata must be fetched before a scan, the sidecar may hold a transient copy, but `games.json`
  stays authoritative.

**As-implemented correction (2026-10-14):** on inspection those three sidecar fields are **fetched**
values (from PCGW/Steam), not the detected `GameEntry.Store`/`GameEngine`. They can exist when the
entry has none (e.g. PCGW reveals a Steam AppID for a standalone game) and they drive
`MetadataIdentity` matching + the Steam store fallback. Deleting them would lose capability.
Resolution: **rename** them to `FetchedSteamAppId` / `FetchedGogGameId` / `FetchedEngine` so their
ownership is explicit; `games.json` remains authoritative and the lookup path prefers the entry's id.

### Acceptance criteria
- No field exists in both files.
- A metadata fetch rewrites only the sidecar.
- A rescan never clobbers fetched metadata.

---

## 3. A — Structural changes

### A1. Break `GameEntry` into typed sub-records
**Problem:** 18-param positional record; every addition is a breaking, error-prone change.
**Target shape**
```
GameEntry
  Identity      : GameId, Library (anchor), DisplayName, GameSource, IsSourceOverridden
  Location      : FolderPath, FolderName, PreviousLocations[]
  Launch        : ExecutablePath, LauncherPath, CommandLineArguments, ExtraLaunchArguments, ManifestPath
  Detection     : DetectionInfo
  Store         : StoreInfo
  Install       : InstallInfo
  Tracking      : TrackingInfo
  UserState     : UserStateInfo
  Extensions    : Dictionary<string, JsonElement> (forward-compat)
```
**Steps (clean cut — no dual-write)**
- [x] Define sub-record types (A2/A3 below).
- [x] Add them to `GameEntry` as body `init` properties.
- [ ] Move every writer to set sub-records.
- [ ] Move every reader to sub-records.
- [ ] Remove the flat duplicate fields in one pass (no transitional period).
**Criteria:** build/test green; DTO round-trip; no flat duplicates remain.

### A2. Retire the `PlatformMetadata` string bag
**Problem:** 26 keys mixing signals, store ids, statuses, paths, provenance; untyped.
**Mapping**
- `SteamStatus`/`Acf*` → `AcfInfo` (+ `InstallState`).
- `Epic*` → `EpicInfo`.
- `SteamAppId`/`GogGameId`/`EaGameName`/`BlizzardProduct`/`UbisoftReadme` → `StoreInfo`.
- `TitleSource`/`AutoDetectedTitle`/`PeFileDescription`/`ExeCandidates*` → `DetectionInfo`.
- `LibraryRoot`/`ActualLibraryRoot`/`GameFolder` → `Location`/`Store` as appropriate.
- Unmapped/unknown → `Extensions` (escape hatch).
**Steps (clean cut):** define records → set them in every writer → point every reader at them →
**delete the bag and `PlatformMetadataKeys` in one pass.** No dual-write.
**Criteria:** no `Dictionary<string,string>` metadata field remains on `GameEntry`; no reader references `PlatformMetadataKeys`.

### A3. First-class `DetectionInfo` (this is T79)
**Fields:** `Tier` (`Unknown|Candidate|Secure|Locked`), `TitleSource`, `ManifestKind`,
`Signals[]`, `CandidateExes[]`, `IdentifiedAt`, `Confidence`.
**Derivation (from existing signals)**
- Steam structural / ACF match → `Locked`.
- Epic global `.item` matched to install location → `Locked`.
- Epic local `.item`/`.mancpn`, other store marker, registry fallback → `Secure`.
- Root exe / engine layout / container child with evidence → `Candidate`.
- No signal → `Unknown`.
**Steps**
- [ ] `IdentificationTier` enum.
- [ ] `DetectionInfo` record.
- [ ] Derivation helper (App layer).
- [ ] Wire `FolderScanner`, `SteamLibraryScanner`, `EpicLibraryScanner` to set it.
- [ ] Persist in the DTO; expose in the details pane.
- [ ] Tests per fixture + a pin test for tier value strings.
**Criteria:** every entry carries a tier; fixtures assert expected tiers; no title becomes confident from a folder name alone.

### A4. Schema versioning (lean — as implemented)
- All four files carry a `version` field written as `SchemaVersioning.Current` (Core).
- On read, `SchemaVersioning.IsReadable(fileVersion)` rejects a file from a newer build (returned as
  defaults rather than silently misread).
- **Migration seam:** no migrations exist pre-ship. When a breaking change lands, bump
  `SchemaVersioning.Current` and add the upgrade step in the relevant store's load path.
- Rejected the full "ordered migrator registry" as speculative pre-ship (avoid overengineering);
  the version constant + readability guard is the real, minimal mechanism.

### A5 / A6
See §1 and §2.

---

## 4. B — Near-term feature records

| ID | Record | Fields | Enables |
|----|--------|--------|---------|
| B7 | `TrackingInfo` | LastPlayed, LaunchCount, PlayTimeSeconds, FirstSeen, LastSessionSeconds | usage stats, "recently played" |
| B8 | `InstallInfo` | State (`InstallState` enum), Version, UpdateAvailable, LastVerified | health, SyncMove, space planning. **Scoped 2026-10-14:** `SizeOnDisk`/`BuildId`/`StateFlags` are **not** duplicated here — they stay on `SteamAcfInfo` (one owner per fact) |
| B9 | `UserStateInfo` | Favourite, Hidden, Pinned, Archived, Rating, Notes, CompletionState (`CompletionState` enum) | launcher UX, backlog |
| B10 | `LaunchProfile[]` | Name, Exe, Args, Env, WorkingDir, RunAsAdmin, Engine | "Play", "Play VR", "Modded" |
| B11 | Relationships | `Kind` (Game/Dlc/Tool/Demo/Soundtrack/Mod), ParentId, AddOns[] | DLC/tools not lost |
| B12 | Collections | named playlists + smart-filter definitions | Plan 101 category browse |
| B13 | Typed tags | `{ name, kind: User/Store/Engine/Genre, color, auto }` | filters, display |

**B7/B8/B9 DONE (2026-10-14)** — `TrackingInfo`, `InstallInfo` (+`InstallState`), `UserStateInfo` (+`CompletionState`) added as `GameEntry` body records, persisted, and surfaced: `Install.State` set by the Steam/Epic/Folder scanners via `InstallStateMap`; `Tracking` incremented on launch (`MainWindow.RecordLaunch`); `UserState` is model+persistence only (UI toggles are a follow-up). Play-time (`PlayTimeSeconds`/`LastSessionSeconds`) is best-effort and not yet populated — URI launches have no process handle.

**B10–B13 DONE (2026-10-14)** — `LaunchProfile[]` on `LaunchInfo` (B10); `RelationsInfo` + `GameKind` on `GameEntry` (B11); `Collection` + `ICollectionsService` + `CollectionsDatabaseService` writing `data/collections.json` (B12); `Tag` + `TagKind` replacing the bare `List<string> Tags` on `GameEntry` (B13, 12 files migrated). Remaining follow-ups: profile picker UI + `GameLaunchResolver` profile overload; scanner wiring of `Relations.Kind` (e.g. Epic addons → Dlc); collections UI; per-tag colour from `tag_colors.json`.

Each B item: define record → add to `GameEntry` → persist → read/write at the feature site → tests.
B7/B8/B9 are the highest value (tracking + state) and are independent of A1's field removal.

---

## 5. C — Deferred: `Extensions{}` + emulator/ROM model

**User proposal (accepted as the C direction):** keep an open `Extensions{}` subsection per game.
An emulated game at `d:\games\switch\zelda2\zelda2.rom` is still an ordinary game entry; the
`Extensions.emulator { … }` payload holds the extra launch data / game type. The **library type**
("Nintendo Switch Images") classifies it, and simple extension-based detection (or a user step)
assigns the emulator.

**Design (deferred)**
- `GameEntry.Extensions` — `Dictionary<string, JsonElement>`, namespaced keys (`emulator`, `mod`, …).
- `Extensions.emulator`: `{ EmulatorId, EmulatorPath, RomPath, ConsoleType, LaunchArgs, Profile }`.
- **Library classification:** extend `Library` with a `Kind` that can represent ROM image sets
  (e.g. "Nintendo Switch Images") without overloading `GameSourceKind` (which is store-oriented).
  Proposal: add `Library.Kind` (string/enum) alongside `Type`, or a `PlatformKind`.
- **Detection:** map ROM extensions (`.nsp`, `.xci`, `.rom`, `.iso`, `.gba`, `.nds`, `.sfc`, …) to a
  console/type; a folder of such files → candidate entry; user assigns the emulator profile.
- Rationale: emulation libraries are naturally self-contained folders, so a per-library type plus a
  per-game `Extensions.emulator` is sufficient — no new top-level file.

**Status:** design only; do not implement until A/B land.

---

## 6. Test strategy
- DTO round-trip for every new record (write → read → equal).
- Identity: in-app move, external move, re-anchor, exe-change — assert `GameId` stability.
- Tier: one fixture per tier; pin the tier value strings.
- Ownership: assert no field duplicated across files; metadata fetch touches only the sidecar.
- Contract tests: update `PlatformMetadataKeysContractTests` as keys are retired.

## 7. Rollout order
```
A3 (DetectionInfo + tier)  →  A2 (typed store/acf/epic records)
        →  A1 (compose GameEntry; remove flat fields)
        →  A5 (identity) + A6 (ownership)  →  A4 (versioning)
        →  B7/B8/B9  →  B10–B13  →  C (deferred)
```
A3 first: it is the detection priority (T79), additive, and low-risk. A1 last among A: it is the
breaking restructure.

---

## 8. Critical analysis & open questions

- **A1 is the riskiest step** (16 ctor sites, `with` expressions, DTOs, ~27 files). Mitigation:
  additive records first, flat fields removed only when nothing reads them; build/test after each file.
- **A5 identity has a genuine heuristic hole:** a folder rename with no store id cannot be matched
  with certainty. Mitigation: prefer store ids; otherwise flag `Missing` + offer a manual re-link
  rather than guessing. Do **not** write marker files into game folders by default.
- **A6 recommendation removes store ids from the sidecar** — verify nothing fetches metadata for an
  unscanned game (if it does, keep a transient copy but keep `games.json` authoritative).
- **`Extensions` as `JsonElement`** is flexible but untyped; the emulator payload should get a real
  record when C is implemented, with `Extensions` used only as the transport.
- **Overload risk:** `Library.Kind` vs `GameSourceKind` — do not bloat `GameSourceKind` with ROM
  consoles; keep store identity and library classification separate.
- **No migration is required now**, but A4's mechanism must exist before the first release, or every
  future schema change becomes a compatibility problem.
- **Open:** do we need multi-user profiles in v2, or is single-user acceptable for v1? Deferred to C.
