# 132 — Persistence Write Contract

**Status:** proposed (review complete; implementation not started).
**Scope:** the five JSON stores under `data/`, plus the blacklist restore path. Repair writers are a
different class of write and belong to the repair-write plan.

---

## 1. What exists

All five stores go through `JsonFileHelper` (`App/Services/JsonFileHelper.cs`):

| Store | Path | Read | Write |
|---|---|---|---|
| `settings.json` | `data/settings.json` (`AppDataPaths.GetConfigPath`) | `JsonConfigService.cs:25` | `JsonConfigService.cs:57` |
| `libraries.json` | `data/libraries.json` (`GetLibrariesPath`) | `LibrariesDatabaseService.cs:25,67,78` | `LibrariesDatabaseService.cs:88` |
| `games.json` | `data/games.json` (`GetGamesDbPath`) | `GamesDatabaseService.cs:29` | `GamesDatabaseService.cs:52` |
| `games_metadata.json` | `data/games_metadata.json` (`GetMetadataDbPath`) | `MetadataStore.cs:64` | `MetadataStore.cs:78` |
| `collections.json` | `data/collections.json` (constructor argument) | `CollectionsDatabaseService.cs:25,67,82` | `CollectionsDatabaseService.cs:90` |
| `blacklist.json` | `data/blacklist.json` | `BlacklistLoader.cs:43,91` (direct `File.ReadAllText`) | `BlacklistLoader.cs:133` (direct `File.WriteAllText`, only when missing) |

`CollectionsDatabaseService` is not constructed by the app; only tests create it. The path in the table
is the documented one.

Other writes, out of scope here:

| Write | Location | Class |
|---|---|---|
| `steamapps/appmanifest_*.acf` | `SteamAcfWriter.cs:81`, dir at `:64` | repair write |
| Epic `.item` | `EpicItemWriter.cs:145`, dir at `:65` | repair write |
| `startup.log` | `App.axaml.cs:39`, `MainWindow.axaml.cs:45` (append) | diagnostic log |
| `data/` itself | `AppDataPaths.cs:17` (side effect of every path getter) | app-owned |

### Read behaviour today

`JsonFileHelper.ReadFromFile` (`:28`) returns the default factory when the file is missing, empty, or
unparseable, and swallows every exception. Callers then check `SchemaVersioning.IsReadable` and fall back
to an empty value on a newer version. Result: **missing, zero-byte, corrupt, and written-by-a-newer-build
are indistinguishable, and all four produce an empty catalogue.** `docs/FAQ.md:91,101,173` documents that
silent reset as intended.

### Directory creation today

`JsonFileHelper.EnsureDirectoryExists` (`:64`) creates the parent of any path it is handed. It is called
from `WriteToFile` (`:55`) and `BlacklistLoader.cs:132`. `AppDataPaths.cs:17` creates `data/` inside a
property getter. The two repair writers call `Directory.CreateDirectory` on store folders
(`SteamAcfWriter.cs:64`, `EpicItemWriter.cs:65`) — covered by the repair-write plan, not here.

### The failure chain being fixed

1. `File.WriteAllText` opens with `FileMode.Create`: the file is truncated before the new bytes land.
2. A crash or full disk between truncate and write leaves a partial file.
3. The next read cannot parse it, `ReadFromFile` returns defaults, the empty result is saved, and the
   previous content is gone.

---

## 2. Contract

### 2.1 Read result

```csharp
public enum StoreReadStatus { Ok, Missing, Corrupt, Unreadable }

public readonly record struct StoreReadResult<T>(
    StoreReadStatus Status,
    T? Value,
    string? Message,
    string? PreservedPath);
```

| Status | Meaning | Caller behaviour |
|---|---|---|
| `Ok` | parsed | use `Value` |
| `Missing` | file absent (first run) | use defaults, write on first change |
| `Corrupt` | present, unparseable, or zero-byte with a `.bak` | use the recovered `.bak` value if present; show `Message` |
| `Unreadable` | version newer than this build | use defaults **in memory only**; never save over it; show `Message` |

`Missing` and `Corrupt` must never be the same value. `Unreadable` must not be overwritten: a newer build's
file is data this build does not understand, and replacing it with an empty file is the worst possible
outcome.

### 2.2 Shared store

```csharp
public sealed class JsonDocumentStore<T>(string path) where T : class
{
    public string Path { get; }
    public string BackupPath { get; }        // Path + ".bak"
    public StoreReadResult<T> Read(Func<T> defaults);
    public void Write(T value);              // throws on verification failure after restoring
}
```

One instance per store, created where the store's service is created. The service keeps its public API;
only its load/save bodies change. DTOs stay private to their services (`GamesDatabaseDto` etc.) — the
shared store is generic and does not know any schema.

`Read` steps:

1. File absent → `Missing` + `defaults()`.
2. Read bytes. Zero length → treat as corrupt (step 4) unless `.bak` exists, in which case recover from it.
3. Deserialize. Success and version readable → `Ok`.
4. Failure or version unreadable:
   - copy the bad bytes to `Path + ".corrupt"` (overwrite the previous `.corrupt`);
   - if `.bak` parses and is readable → `Corrupt`, `Value` = recovered, `PreservedPath` = `.corrupt`;
   - else → `Corrupt` (or `Unreadable`), `Value` = `defaults()`, `PreservedPath` = `.corrupt`.
5. `Message` is one short sentence naming the file and what happened ("games.json was unreadable;
   restored the previous copy"). The shell shows it once per session in the status line and writes it to
   `startup.log`. No modal dialog.

The `.corrupt` file is never auto-deleted. The user can inspect it, and it is the evidence for a bug
report.

### 2.3 Write sequence

1. Serialize to a string **first**. A serializer failure must not touch disk.
2. If `Path` exists and its length > 0, `File.Copy(Path, BackupPath, overwrite: true)`.
3. `File.WriteAllText(Path, json)`.
4. Re-read `Path` and deserialize. If it fails:
   - if `BackupPath` exists → `File.Copy(BackupPath, Path, overwrite: true)`, throw with the parse error;
   - if not → leave the bytes in place, throw.
5. Done.

There is exactly one `.bak` per store, overwritten on every write. There is no `.bak.1`, no timestamped
copy, no chain.

**Why there is no temp file and no rename:** `File.Move(temp, path, overwrite: true)` is atomic-ish on
NTFS but requires a second file in `data/` for every store, and on failure the directory holds a stray
temp file the app must clean up. The copy → write → re-read sequence needs one extra file that is reused,
and its failure mode is the same one the `.corrupt` path already handles. Two files per store, total.

**Why there is no hash, digest, or signature:** the check would be performed by the same process that
writes the file, with the same user's permissions, on the same disk. Anyone who can corrupt `games.json`
can rewrite the digest next to it. The only real safeguard in this class is keeping the previous copy.

### 2.4 Directory rule

```csharp
public static class AppPaths
{
    public static string AppDataRoot { get; }                       // {BaseDirectory}/data, resolved once
    public static string EnsureAppDataPath(string filePath);        // returns filePath after ensuring its parent
}
```

- `EnsureAppDataPath` refuses a path outside `AppDataRoot` (segment-aware: `PathText.IsUnder`) by throwing
  `InvalidOperationException`. A path outside the root is a programming error, not a user condition.
- It is the **only** place `Directory.CreateDirectory` is called for JSON state.
- `AppDataPaths` getters stop creating `data/` implicitly; the root is created once at startup (or by the
  first `EnsureAppDataPath`).
- External paths (store folders, ProgramData, user folders) are checked with `Directory.Exists` only. A
  missing external directory is a refusal, never a creation. That rule is executed by the repair-write plan.
- Windows does not create parent directories for `File.WriteAllText`/`Copy`/`Move`. The `CreateDirectory`
  entry visible in ProcMon is an explicit call in this codebase, not OS behaviour.

### 2.5 Not part of this contract

- Detecting tampering, validating authorship, encrypting state, or protecting against a different user on
  the same machine. Not a boundary this application can hold.
- Repair writes into store folders — separate plan.
- Backups the user manages (`data/` copy is documented as the backup; unchanged).
- `startup.log` rotation.

---

## 3. Migration

Order chosen so each step is provably safe before the catalogue is touched:

| # | Store | Why here |
|---|---|---|
| 1 | `settings.json` | smallest shape; failure is visible at startup; no cross-references |
| 2 | `libraries.json` | read by settings and scanning; still small |
| 3 | `games.json` | the catalogue; migrate after the mechanics have run on two files |
| 4 | `blacklist.json` | direct read/write becomes `AppPaths` + one write; embedded fallback stays |
| 5 | `games_metadata.json` | a cache; losing it is recoverable by re-enrichment, so it can go last |
| 6 | `collections.json` | service is unwired; either migrate it or delete it with the collection work |

Each step: replace the service's `JsonFileHelper` calls with one `JsonDocumentStore<T>` instance, keep the
service's public methods byte-for-byte in behaviour, add the store's tests to that service's test file.

`JsonFileHelper` is deleted when the last caller migrates. `FAQ.md:91,101,173` is corrected in the same
change: "unreadable file is preserved as `.corrupt` and the previous copy is restored".

### Sequencing

- Required before per-launch sync. A one-shot inventory import may land first, but must write through the
  same store.
- The repair writers are independent, but their directory rule is the same one: app root only.

---

## 4. Tests

| Case | Assertion |
|---|---|
| first run | `Missing`, defaults returned, no files created by `Read` |
| normal round trip | `Write` then `Read` → `Ok`, value equal |
| `.bak` created | after a second write, `.bak` exists and holds the first value |
| one `.bak` only | directory file set is `{name}.json`, `{name}.json.bak` — no numbered copies |
| corrupt with `.bak` | `Corrupt`, value from `.bak`, `.corrupt` exists with the damaged bytes, original restored |
| corrupt without `.bak` | `Corrupt`, defaults returned, `.corrupt` exists, `Path` unmodified |
| zero-byte file | same as corrupt |
| newer version | `Unreadable`, defaults in memory, file bytes unchanged after a read |
| interrupted write | truncate the file mid-JSON, `Read` recovers `.bak` and reports |
| read-back failure | injected bad serializer → `Write` restores `.bak` and throws |
| outside root | `EnsureAppDataPath("/tmp/x/y.json")` on Windows-style root throws; no directory created |
| inside root, nested | returns the path and creates the parent under `data/` |

Tests use temp directories supplied by the test, so they run on the Linux host. Store tests must not depend
on `AppDomain.CurrentDomain.BaseDirectory`.

---

## 5. Open questions

- Whether `.corrupt` files accumulate across sessions. Current answer: one per store, overwritten; the
  user may delete it. Revisit only if it hides a recurring failure.
- Whether the shell shows the recovery message as a status line or a one-time banner. Decide in the UI
  change, not here.
