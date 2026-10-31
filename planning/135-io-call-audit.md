# 135 — Registry and Filesystem Call Audit: One Read, One Write

**Status:** applied.
**Scope:** every `File.*`, `Directory.*` and `Registry*` call in `src/` (excluding `bin/`, `obj/`).
**Companions:** `META/CONNECTION_MAP.md` §3 (scan pipeline), §7 (trust boundaries), §8 (per-store
paths); `planning/132` (persistence writes); `planning/133` (repair writes).

The rule used here is not "fewer calls". A **read** takes its answer from the operation already being
performed; a **write** performs the state change and nothing else. Existence is kept only where
existence *is* the answer: a user-chosen folder, a store marker, a target that must be absent, a target
that must exist, or a restore that must not overwrite user edits.

Line numbers are as of this change.

---

## 1. Result

| Grep | Before | After | Removed |
|---|---|---|---|
| `File.Exists` | 39 | 32 | 7 |
| `Directory.Exists` | 49 | 33 | 16 |
| `Directory.CreateDirectory` | 1 | 1 | 0 (the one left is `AppPaths`) |
| `File.Delete` | 2 calls | 2 calls | 0 (neither is preceded by `Exists`) |
| `SetValue` / `CreateSubKey` / `DeleteSubKey` | 0 | 0 | 0 |

**23 existence calls removed** (7 `File.Exists`, 16 `Directory.Exists`): 1 write-precheck, 6
exists-before-read, 1 probe before a call that already reports its own failure, 15
exists-before-enumeration. No read was replaced by a probe, no write was added, and no path changed.

---

## 2. Fixes applied, one pattern per change

### W1 — `Directory.Exists` before `Directory.CreateDirectory`

| Site | Change |
|---|---|
| `AppPaths.cs:33` | `EnsureAppDataPath` calls `Directory.CreateDirectory` unconditionally; `CreateDirectory` is idempotent, so the preceding `Exists` only added a call and a race. The `PathText.IsUnder` refusal is untouched. |

`BlacklistLoader`'s restore guard is W1-shaped but is **not** redundant; it is listed in §3.
`JsonDocumentStore`'s parent-directory helper is in the persistence write sequence and was left
untouched; it is also listed in §3.

### P1 — `File.Exists` before a read

| Site | Read that reports absence | Note |
|---|---|---|
| `BlacklistLoader.ReadBlacklistJson` | `File.ReadAllText` in a `try` with a returning `catch` | missing file → `null`, the same answer as before |
| `TagColorService.LoadConfig` | `File.ReadAllText` → defaults on any failure | missing file and bad JSON were already the same outcome |
| `EaInstallLogParser.TryParse` | `File.ReadAllText(logPath, Unicode)` under `catch (IOException)` | — |
| `EpicBinaryManifest.TryRead` | `File.ReadAllBytes` under a bare `catch` | — |
| `StoreSignalDetector.ExtractBlizzardProduct` | `File.ReadAllLines` under a bare `catch` | — |
| `SteamAcfParser.DiscoverLibraryPaths` | `File.ReadLines` materialised in a `try` | returns no libraries instead of throwing through the scan; `IOException`, `UnauthorizedAccessException` and `ArgumentException` (the answers `Directory.Exists` used to give for a malformed path) |

Probe before a call that reports its own failure:

| Site | Change |
|---|---|
| `PublishLayout.ResolveNative` | `NativeLibrary.TryLoad` returns `false` for an absent file, so the preceding `File.Exists` was a second stat; it is gone. |

### P2 — `Directory.Exists` before an enumeration

Each of these sat in front of an enumeration whose failure already produced the same "contributes
nothing" answer. Some are inside an existing `try`; the rest gained no new handler beyond widening a
catch to the exception the enumeration raises.

| Site | Enumeration | Behaviour after |
|---|---|---|
| `EpicItemCatalog` ctor | `Directory.GetFiles(ManifestsDir, "*.item")` | empty catalogue |
| `EpicItemCatalog.LooksLikeManifestsDir` | `Directory.EnumerateFiles(path, "*.item").Any()` | `false` |
| `CheapExeFinder.FindFirst` | `Directory.GetFiles(dir, "*.exe")` | next candidate folder |
| `SteamLibraryScanner.Scan` (`common/`) | `FileSystemHelper.GetDirectoriesSafe` | empty list |
| `SteamLibraryScanner.CollectAcfMap` | `Directory.EnumerateFiles(steamapps, "appmanifest_*.acf")` | no ACFs for that library |
| `EpicItemWriter.TryReadMancpn` | `Directory.GetFiles(dir, "*.mancpn")` | next store folder |
| `EpicItemWriter.FindManifestGuid` | `Directory.GetFiles(dir, "*.manifest")` | next store folder |
| `EpicManifestParser.ExtractLocalIdentifiers` | `Directory.GetFiles(storeDir/manifests, "*.item")`, `.mancpn` | next candidate directory |
| `EpicManifestParser.CrossReferenceGlobalManifests` | `Directory.GetFiles(manifestsPath, "*.item")` | `null` |
| `ExecutableDiscovery.FindParentBoundExe` | `Directory.EnumerateFiles(childPath, "*.exe")` | next parent-bound child |
| `ExecutableDiscovery.FindEpicManifest` | `new DirectoryInfo(dir).GetFiles(pattern)` | next candidate |
| `EpicOrphanDiscovery.Find` | `FileSystemHelper.GetDirectoriesSafe(root)` | no children |
| `EngineDetector.Detect` | the four layout probes below it | all probes answer `false` → `Unknown` |
| `FallbackSignalDetector.HasSteamEmuDeepSignal` | `Directory.GetDirectories(Steamworks)` | no signal |

Where removing a probe would have let a `DirectoryNotFoundException` abort a **sibling** loop instead
of skipping one candidate, the probe stays; see §5.

---

## 3. Remaining inventory — reads

Every remaining hit is listed. "Deliberate" means the call produces the answer; it is not a probe
before another call.

### `File.Exists` (32)

| Site | Role | Verdict | Reason |
|---|---|---|---|
| `PublishLayout.cs:27` | assembly resolver | deliberate | The resolver's answer *is* "this assembly is not here" → `null`. Replacing it with a throwing load would make a normal lookup an exception. |
| `RepairWriteGuard.cs:275` | Steam marker | deliberate | Contract precondition: `libraryfolders.vdf` must exist or the folder is not a Steam library. |
| `RepairWriteGuard.cs:281` | Steam target | deliberate | Contract precondition: target must be **absent** (create-only). |
| `RepairWriteGuard.cs:298` | Epic target | deliberate | Contract precondition: target must **exist** (modify-only); parse validation follows in the same guard. |
| `BlacklistLoader.cs:129` | restore | deliberate | W6: the embedded blacklist is written only when the file is missing, so user edits are never overwritten. |
| `JsonDocumentStore.cs:83` | read shape | deliberate | Distinguishes `Missing` from `Corrupt`; that distinction is the read contract. A read that throws cannot tell the two apart. |
| `JsonDocumentStore.cs:137` | write sequence | deliberate | Persistence contract: back up only when the current file has content. Left as specified. |
| `JsonDocumentStore.cs:181` | message | deliberate | Chooses "previous copy is unreadable" vs "there is no previous copy" for the user; existence is the answer. |
| `JsonDocumentStore.cs:192` | backup recovery | deliberate | Same reason: "no backup" and "backup unreadable" are different reports. |
| `JsonDocumentStore.cs:229` | restore | deliberate | Same reason: distinguishes a missing backup from a failed copy in the returned message. |
| `EpicLaunchResolver.cs:28` | manifest exe | deliberate | Existence is the answer: an `.item` launch path that is gone must not be offered; the resolver then scores the folder instead. |
| `EngineDetector.cs:51,67,68,73` | engine signals | deliberate | Each file *is* the engine signal (Unity player, RAGE pair, Frostbite DLL); no read follows. |
| `FallbackSignalDetector.cs:54,60,70,84,90` | emulator/legacy signals | deliberate | Signal probes: `steam_emu.ini`, `UbiStats.dll` at root or in a child. `:70` also guards the leaf probe in the same candidate. |
| `StoreSignalDetector.cs:79,106,107,195,199,203,245,251,257,263,264` | store signals | deliberate | GOG icon/exes, EA exes, Battle.net markers, Xbox/Rockstar/Steam markers. Existence is the detection answer. |
| `ShellViewModel.Details.cs:230` | detail timestamp | deliberate | Decides whether to show the exe's modified time or the entry's; `GetLastWriteTimeUtc` returns a sentinel instead of throwing for a missing file, so the check is the only way to fall back. |

### `Directory.Exists` (33)

| Site | Role | Verdict | Reason |
|---|---|---|---|
| `PublishLayout.cs:18` | publish layout | deliberate | One call at start-up decides whether the resolver is installed at all; without it every failed resolution would pay a load attempt. |
| `BlacklistLoader.cs:134` | restore create path | deliberate | The app-root guard may only run when a directory must be created; an existing directory (including a test-owned one) is used as-is. Removing this needs a different guard, not a deleted line. |
| `EpicItemWriter.cs:105` | user-facing refusal | deliberate | "Game folder missing." is the message; existence is the answer. |
| `JsonDocumentStore.cs:246` | persistence dir rule | deliberate | Inside the persistence write sequence; left untouched by decision (see §6). |
| `EngineDetector.cs:44,46,53` | engine layout | deliberate | `Engine/Binaries`, `<name>/Binaries/Win64`, `<name>_Data` are the layout signals; the enumeration at `:57` is the fallback. |
| `GamesDatabaseService.cs:148` | identity | deliberate | Weak (folder-name) relocation is accepted only when the old folder is gone; existence is the answer. |
| `FallbackSignalDetector.cs:70` | signal leaf | deliberate | Guards `steam_emu.ini` inside a discovered version directory. |
| `FallbackSignalDetector.cs:111` | UE4-5 gate | deliberate | The `Engine/` folder *is* the UE4-5 requirement; without it the child/Binaries scan would classify a different layout. |
| `FallbackSignalDetector.cs:123,152` | per-platform skip | deliberate | `Directory.EnumerateFiles` throws for a missing platform folder and the `catch` is outside the loop; the probe is what keeps one missing platform from aborting its siblings. |
| `FallbackSignalDetector.cs:144` | UE3 gate | deliberate | `Binaries/` at the root *is* the UE3 layout. |
| `RepairWriteGuard.cs:79` | store folder | deliberate | Contract precondition: refuse a missing store folder, never create it. |
| `SteamLibraryScanner.cs:40` | library root | deliberate | A configured library root that is gone yields no entries; the root is the user's choice, not a probe. |
| `SteamLibraryScanner.cs:87` | missing-game detection | deliberate | For each ACF installdir, presence in any library *is* the Installed/Missing answer. |
| `UbisoftReadmeParser.cs:57` | readme location | deliberate | Picks the case variant that exists (four candidates); the choice is the answer. |
| `EpicLibraryScanner.cs:32` | install state | deliberate | `folderOk` drives Installed/Missing and exe resolution. |
| `FolderScanner.cs:120` | library root | deliberate | Same as `SteamLibraryScanner`: a missing configured root yields no entries. |
| `FolderScanner.cs:260` | nested Steam tree | deliberate | `steamapps/common` *is* the structural check that keeps a Steam tree out of a folder scan. |
| `LibraryManager.cs:87` | library folder | deliberate | User-configured folder; a missing one is skipped, and that skip is the answer. |
| `LibraryManager.cs:141` | Steam structure | deliberate | `LooksLikeSteamLibrary` is a structural predicate. |
| `ExecutableDiscovery.cs:60,71` | per-folder skip | deliberate | Both sit inside one `try` that spans the child loop; removing them would let a missing `Binaries/<platform>` or `bin/` abort the remaining children (same reason as the detector rows above). |
| `StoreSignalDetector.cs:101,144,175,176,190` | store signals | deliberate | `__Installer`, `uplay_download`, `.egstore`/`.egsstore`, `.battle.net`. |
| `EpicOrphanDiscovery.cs:49,50` | Epic marker | deliberate | `.egstore`/`.egsstore` is the orphan signal. |
| `GameSetupWindow.axaml.cs:225,227` | picker start | deliberate | Chooses a start location from the recorded folder, then the current one; existence is the answer. |

### `new FileInfo` / timestamps (P3, P4)

| Site | Pattern | Verdict | Reason |
|---|---|---|---|
| `ExecutableDiscovery.Scoring.cs:96` | P4 | deliberate | The path is a string from a candidate list; no `FileInfo` instance is in hand. Length drives the score. |
| `ExecutableDiscovery.cs:373` | P4 | deliberate | Fallback ordering needs each candidate's length; keys are computed once per element by `OrderByDescending`. |
| `RepairWriteGuard.cs:150,165,252` | W4 | deliberate | Contract verification: staged length, final length, journal length. |
| `JsonDocumentStore.cs:137` | P4/W4 | deliberate | Persistence contract: back up only a non-empty file. |
| `SteamLibraryScanner.cs:224,255`, `FolderScanner.cs:378` | P4 | clean | `GetLastWriteTimeSafe` reads `DirectoryInfo.LastWriteTime` on the instance returned by the enumeration — the same stat, not a second one. |
| `ShellViewModel.Details.cs:234`, `Core/Services/PeProductYear.cs:32` | P4 | deliberate | Path strings from the UI/details or a caller, not from an enumeration. |

### P5–P9

| Pattern | Finding | Verdict |
|---|---|---|
| P5 (`GetDirectories` + `GetFiles` on one directory) | none. `StoreSignalDetector.HasGogSignal` enumerates one folder with three different globs (`goggame*`, `gog_*`, `*.lnk`); merging them needs a predicate over one enumeration, i.e. a refactor. | deliberate, recorded |
| P6 (same `Directory.Exists(samePath)` per game) | none. Every per-item check uses a path derived from that item (install path, platform folder, candidate exe), never one scan-constant path. | clean |
| P7 (same file read twice) | The Epic repair reads the target `.item` for validation, for the member count and for the payload in one operation. The parent-count proof is the reason for the second parse. | deliberate (repair contract) |
| P8 (recursive walk for a shallow question) | `ExecutableDiscovery.FindExactFolderMatch` bounds its *answer* to relative depth 4 but enumerates with `SearchOption.AllDirectories`. See §6. | left, recorded |
| P9 (`Path.Combine` per item for a scan constant) | none: every combination uses a per-item base (game folder, install path, platform folder). Static relative-folder arrays are already hoisted. | clean |

---

## 4. Remaining inventory — writes and registry

### Filesystem writes (W1–W7)

| Site | Pattern | Verdict | Reason |
|---|---|---|---|
| `AppPaths.cs:35` | W1 | fixed | The only `Directory.CreateDirectory`; unconditional, idempotent. |
| `BlacklistLoader.cs:129` | W6 | deliberate | Restore-from-resource only when missing; never overwrites user edits. |
| `RepairWriteGuard.cs:281` | W3 | deliberate | Steam target must be absent (create-only). |
| `RepairWriteGuard.cs:298` | W3 | deliberate | Epic target must exist and parse (modify-only). |
| `RepairWriteGuard.cs:161,200` | W7 | deliberate | One `File.Copy` to the target, one backup copy under `data/backups/`. Staging lives in `data/staging/`. |
| `RepairWriteGuard.cs:165,173` | W4 | deliberate | Length check then parse read-back: the contract's two proofs, not extra stats. |
| `RepairWriteGuard.cs:239` | — | deliberate | One `File.AppendAllText` to the repair journal; failure is swallowed by contract. |
| `RepairWriteGuard.cs:415,445` | W2 | clean | `File.Delete` with no `Exists` in front; absence is success (commented at the call). |
| `JsonDocumentStore.cs:138,140,144` | W4/W7 | deliberate | Persistence contract: copy the previous file, write, re-read and parse. Two files per store, no temp, no rename. |
| `App.axaml.cs:39`, `MainWindow.axaml.cs:45` | — | deliberate | `startup.log`, one append, best effort. |
| W5 | — | clean | No `Directory.CreateDirectory` for a store folder anywhere; both repair writers refuse a missing store folder instead. |

### Registry (R1–R9)

`WindowsRegistryReader` is the only registry surface. `grep -rn "SetValue\|CreateSubKey\|DeleteSubKey" src`
stays empty: the application writes zero registry values.

| Site | Pattern | Verdict | Reason |
|---|---|---|---|
| `WindowsRegistryReader.cs:28` | R5 | deliberate | `GetValueNames()` then `GetValue` per name: the value names are genuinely unknown; it reads every value of one key once (`ReadKeyValues`). |
| `WindowsRegistryReader.cs:42` | R4 | deliberate | `GetSubKeyNames()`; the caller opens each name only to read a value. |
| `WindowsRegistryReader.cs:52,54` | R1 | deliberate, recorded | `Registry.LocalMachine` / `Registry.CurrentUser` per call. The reader's seam is a full key path per call, so hoisting a base key means changing `IRegistryReader` — a refactor, not a deleted call. |
| `RegistryFallbackDetector` (all enumerations) | R7 | clean | Runs once when the folder scanner is built, not per game; `DetectType` afterwards is dictionary lookup only. |
| R2, R3, R6 | — | clean | No second open of the same key for another value, no `OpenSubKey(path) is not null` existence test, no `GetValue` then `GetValue(name, default)`. |
| R8 | — | clean | No store is read through both registry views; the 32-bit locations are addressed explicitly (`WOW6432Node`). |
| R9 | — | clean | Every `RegistryKey` is in a `using` (`WindowsRegistryReader.cs:17,24,39`). |

---

## 5. Contract preconditions kept

These are refusals, not probes, and were not touched:

1. **Steam ACF:** target must be absent, `steamapps` must exist, `libraryfolders.vdf` must exist
   (`RepairWriteGuard. SteamPreflight`).
2. **Epic `.item`:** target must exist and parse, and the folder must be the scan's manifests folder
   (`RepairWriteGuard. EpicPreflight`).
3. **Application data:** only `AppPaths.EnsureAppDataPath` creates a directory, and only under the app
   data root (`AppPaths`).
4. **User-chosen folders:** setup and library-root validation, where existence is the answer
   (`GameSetupWindow`, `LibraryManager`, `FolderScanner`, `SteamLibraryScanner`).
5. **Blacklist restore:** only when missing, so user edits survive (`BlacklistLoader`).
6. **Repair/journal verification:** one length check and one parse read-back
   (`RepairWriteGuard.TryCopyAndVerify`, `JsonDocumentStore.Write`).

---

## 6. Left for a refactor, recorded

These are real extra operations, but removing them changes a signature, a control flow, or an error
contract — beyond a deletion.

1. **`ExecutableDiscovery.FindExactFolderMatch`** bounds its answer to relative depth 4 yet enumerates
   with `SearchOption.AllDirectories` and discards deeper hits afterwards. The walk is required for the
   rule; the unbounded *descent* is not. A depth-bounded enumeration (`EnumerationOptions.MaxRecursionDepth`)
   would cut the directory reads, but it also changes the error behaviour (accessible-only enumeration)
   and needs an off-by-one check against the relative-depth filter. Left as-is.
2. **`FolderScanner.AddGameEntry`** enumerates the root `*.exe` and `ExecutableDiscovery.FindPrimaryExecutable`
   enumerates the same root again inside its deep search. The pre-enumerated list only feeds a fallback
   branch that is reachable only when the directory changed between the two enumerations. Removing it
   changes the method's parameter list, which tests use as a seam. Left as-is.
3. **Epic repair reads the target `.item` more than once** per operation (validate, member count,
   payload build). The pre/post member count is the proof that unknown members survived; sharing one
   parse across the three steps would change the repair contract's shape. Left as-is.
4. **Registry base key per read** (`R1`): the seam takes a key-path string, so one hive handle cannot be
   carried without changing `IRegistryReader`. The enumeration happens once per scan, so the cost is
   bounded. Left as-is.
5. **`WindowsRegistryReader.ReadKeyValues`** has no callers (interface surface). Dead public method, not
   an I/O call; recorded for a later cleanup.
6. **`StoreSignalDetector.HasGogSignal`** enumerates one folder with three globs. One enumeration plus a
   predicate is a refactor of a detection path; the answer is identical but the code is not a deletion.

---

## 7. Verification

- `dotnet build GamingCommander.sln` — 0 errors. The three Avalonia `AVLN3001` warnings are pre-existing
  and only surface when the XAML compiler reruns; no C# warnings.
- `dotnet test GamingCommander.sln --no-build` — 755 passing (Core 219, App 535, Migration 1), unchanged.
- No test touches a real store, `ProgramData`, or the registry; all use temp directories.
- `SetValue` / `CreateSubKey` / `DeleteSubKey` grep: empty.
- `Directory.CreateDirectory` grep: one hit, `AppPaths`.
- Detection was not changed: no verdict changes, no new fallback, and the same entries per scan. The
  22-case detection matrix is a human/corpus run and was not produced by this environment.
- Process Monitor spot check (optional, human-run): the same scan should show fewer `CreateFile` /
  `QueryDirectory` operations and no `CreateDirectory` outside `data/`.
