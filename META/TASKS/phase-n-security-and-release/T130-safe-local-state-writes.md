# Task T130: Safe Local-State Writes

**Tier:** 3 — Logic/Behavior
**Phase:** N — Security and Release Integrity
**Risk:** Medium
**Status:** ready
**Prerequisites:** Task T130 depends on Task T124B (done)

## Shape (decided here; `planning/132` has the rationale)

```csharp
public enum StoreReadStatus { Ok, Missing, Corrupt, Unreadable }

public readonly record struct StoreReadResult<T>(
    StoreReadStatus Status, T? Value, string? Message, string? PreservedPath);

public sealed class JsonDocumentStore<T> where T : class
{
    public JsonDocumentStore(string path, Action<string>? warningSink = null);
    public string Path { get; }
    public string BackupPath { get; }
    public StoreReadResult<T> Read(Func<T> defaults, Func<T, bool>? isReadable = null);
    public void Write(T value);
}
```

- `isReadable` lets the caller apply `SchemaVersioning.IsReadable(dto.Version)`; a parsed-but-unreadable
  value returns `Unreadable` and must not touch the file.
- `warningSink` receives the same one-line message returned in the result. Do not build a logging
  framework; `App` passes a lambda that appends to the startup log and records the first load warning for
  the status bar.
- Store location: `src/GamingCommander.App/Services/JsonDocumentStore.cs`; guard:
  `src/GamingCommander.App/AppPaths.cs` (`AppDataRoot`, `EnsureAppDataPath`). Do not add it to Core.

## Consumers (migrate in this order)

1. `JsonConfigService` → `settings.json`
2. `LibrariesDatabaseService` → `libraries.json`
3. `GamesDatabaseService` → `games.json`
4. `BlacklistLoader` → guard only; it restores the file when missing and never overwrites
5. `MetadataStore` → `games_metadata.json`
6. `CollectionsDatabaseService` → test-only consumer; migrate or leave on a direct write, but it must not
   keep a second production write path.

`JsonFileHelper` is deleted when the last caller migrates. `AppDataPaths` stops creating `data/` in a
property getter; creation goes through `AppPaths` (once at startup or on first write).


## Objective

One write path for the app's JSON files, with one safeguard: back up, write, verify the file parses,
accept — or put the backup back. A damaged save must never read back as an empty catalogue.

## What Needs to Change

### 1. Write sequence
- [x] Serialize to a string **first**; a serializer failure must not touch the disk.
- [x] If the current file exists and is non-empty, copy it to `data/<name>.bak`, overwriting the previous backup.
- [x] Write the new JSON.
- [x] Read the file back and deserialize it.
- [x] Accept if it parses. If it does not, copy `.bak` back over the file and report the failure.
- [x] No temp file, no rename, no hash, no digest, no signature, no hash chain.

### 2. Read path
- [x] Missing file → defaults (first run), no error.
- [x] Parse failure → keep the damaged bytes as `<name>.corrupt` (one file, overwritten), restore
      `<name>.bak`, use the value from `.bak`, and surface a message naming both files.
- [x] Zero-byte file is treated as a parse failure, not as "missing".
- [x] Newer schema version (`SchemaVersioning.IsReadable` false) → `Unreadable`: return defaults in memory,
      never write over the file, and surface a message. A newer build's file is not our data to replace.
- [x] Never return an empty catalogue on a parse failure.

### 3. Consumers
- [x] Migrate `JsonFileHelper` consumers to the shared store.
- [x] No component writes JSON directly outside the shared store.
- [x] Keep the read result in one shape: `Ok`, `Missing`, `Corrupt`, `Unreadable`.

### 4. Directory rule
- [x] `Directory.CreateDirectory` is allowed only under the application data root
      (`data/`, `data/backups/`, `data/staging/`).
- [x] Add one guard helper, `AppPaths.EnsureAppDataPath(string filePath)`, that rejects a path outside that
      root by throwing; use it wherever a directory is created.
- [x] Call the guard **only when the parent directory is missing**. Existing parents (test temp dirs, the
      already-created `data/`) are not re-validated; the guard exists to stop creation, not to police writes.
- [x] Do not create external directories. If an external directory is missing, surface an error.

## Context

- `src/GamingCommander.App/Services/JsonFileHelper.cs` calls `File.WriteAllText` and returns defaults
  on any read failure. `File.WriteAllText` opens with `FileMode.Create`, so it truncates the target
  before the new bytes land; a killed save plus a defaults-returning read is how a catalogue can be
  wiped by the next save.
- `EnsureDirectoryExists` in the same file creates the parent of any path it is given. That is the
  code behind the `CreateDirectory` frame seen in ProcMon, and it is why the guard lives there.
- `docs/FAQ.md` currently describes the reset as intended behaviour; the documentation task updates it.
- Persisted schemas are disposable pre-ship, but user-curated tags, roots, and overrides are not.
- The contract is produced by the persistence review (`planning/132-persistence-write-integrity.md`).
- Sequencing: this must land before per-launch store sync, which writes on every launch. A one-shot
  import may land first, but it must write through this store; there is no `JsonFileHelper` fallback.

## Requirements

- [x] Every successful write leaves a `.bak` containing the previous bytes.
- [x] A failed verify restores the `.bak` and reports, leaving a parseable file.
- [x] A corrupt file encountered on read is preserved as `.corrupt`, not overwritten.
- [x] First run behaves as before: missing files create defaults without an error.
- [x] No component writes JSON directly outside the shared store.
- [x] No `Directory.CreateDirectory` call exists outside the application data root.
- [x] No hash, digest, signature, or hash-chain field is added to any persisted file.
- [x] The write path performs exactly one file write per save.

## Verification

- [x] First-run test: no file → defaults, no error, no `.bak`.
- [x] Write test: `.bak` holds the previous bytes after a successful save.
- [x] Verify-failure test: a write that cannot be re-parsed restores `.bak` and reports.
- [x] Corruption test: truncated, zero-byte, and invalid-JSON files produce `Corrupt`, keep the damaged
      bytes as `.corrupt`, restore `.bak`, and do not silently reset.
- [x] Newer-version test: a file with a higher `Version` produces `Unreadable`, returns defaults in memory,
      and the bytes on disk are unchanged after the read.
- [x] Rollover test: a second save replaces the `.bak`, it does not accumulate files.
- [x] Guard test: `AppPaths.EnsureAppDataPath` rejects a path outside the app root and creates nothing.
- [x] `dotnet build` / `dotnet test` green.

## Stop rules

- **Do not change a DTO, a schema version, or a service's public load/save semantics** beyond the recovery
  behaviour above.
- **No temp file, no rename, no hash, no digest, no signature, no second `.bak` chain.**
- **No new dependency, no background writer, no file watcher.**
- **Do not touch the repair writers** (`SteamAcfWriter`, `EpicItemWriter`) — Task T129 owns them.
- **Do not invent a logging framework.** `warningSink` plus `startup.log` is enough.
- **Tests use temp directories** supplied by the test; no test may write to the real `data/`.

## Files expected to change

`AppPaths.cs` (new), `JsonDocumentStore.cs` (new), `AppDataPaths.cs`, `JsonFileHelper.cs` (deleted),
`GamesDatabaseService.cs`, `LibrariesDatabaseService.cs`, `JsonConfigService.cs`, `MetadataStore.cs`,
`BlacklistLoader.cs`, `CollectionsDatabaseService.cs`, `App.axaml.cs` (wire the sink), plus new tests
`JsonDocumentStoreTests.cs`, `AppPathsTests.cs` and updates to the service tests.

## Completion Notes

- **Completed:** 2026-10-31
- **What was done:**
  - Added `AppPaths` (`src/GamingCommander.App/AppPaths.cs`): `AppDataRoot` resolved once from
    `AppDomain.CurrentDomain.BaseDirectory`, and `EnsureAppDataPath` which throws for a path outside the
    root and otherwise creates only the missing parent.
  - Added `JsonDocumentStore<T>` with `StoreReadStatus` / `StoreReadResult<T>`. `Read` distinguishes
    `Ok`/`Missing`/`Corrupt`/`Unreadable`; corruption preserves the exact damaged bytes as `.corrupt`,
    restores and returns the `.bak` value when it parses, and never resets silently. `Write` serializes
    first, copies the non-empty current file to the single `.bak`, writes once, re-parses, and restores
    the `.bak` (then throws) on a verification failure. No temp file, rename, hash, or second backup.
  - Migrated `JsonConfigService` (`settings.json`), `LibrariesDatabaseService` (`libraries.json`),
    `GamesDatabaseService` (`games.json`), `MetadataStore` (`games_metadata.json`) and
    `CollectionsDatabaseService` (`collections.json`) to one store each, passing
    `SchemaVersioning.IsReadable` as `isReadable`. `BlacklistLoader` keeps its direct read and embedded
    fallback but creates the missing parent only through `AppPaths` (restore-only, never overwrite).
  - Deleted `JsonFileHelper`. `AppDataPaths` getters are now pure (no `Directory.CreateDirectory`).
  - `App` passes a warning sink to every production store: it appends to `startup.log` and the first
    warning wins `ShellViewModel.StatusText` once. `GetLogPath` ensures `data/` through `AppPaths`.
  - Each service gained an optional `Action<string>? warningSink = null` constructor parameter, so
    existing call sites and public load/save semantics are unchanged.
- **Verification:**
  - `dotnet build GamingCommander.sln -v q --nologo`: 0 errors; only the three pre-existing
    AVLN3001 designer notices.
  - `dotnet test GamingCommander.sln --no-build -v q --nologo`: 726 passing — Core 219, App 506,
    Migration 1 (was 708: +18 App tests).
  - New `JsonDocumentStoreTests` cover first run, round trip, `.bak` rollover with one backup file,
    corrupt-with/without-backup, zero-byte, truncated and invalid JSON, newer version, sink message,
    and a forced verify failure that restores the backup. New `AppPathsTests` cover the outside-root
    refusal (nothing created), nested creation inside the root, and the pure `AppDataPaths` getters.
    Updated `JsonConfigServiceTests`, `GamesDatabaseServiceTests` and `BlacklistLoaderTests` for the
    recovery path and the parent-directory guard.
- **Issues encountered:**
  - The verify-failure case needs an injectable serializer, so `JsonDocumentStore` takes an optional
    `JsonSerializerOptions` third constructor argument (defaults to the previous indented,
    case-insensitive options). Production never passes it; the test uses a converter that writes valid
    JSON but throws on read.
  - `BlacklistLoader` can no longer restore into a missing external directory (by design, the guard
    refuses it); `Load_WithMissingFile_ReturnsEmbeddedDefaultsAndRestoresFile` now creates the test-owned
    `data/` directory first, and the missing-directory test asserts no directory is created.
  - The two remaining `Directory.CreateDirectory` calls in `src/` are the repair writers
    (`SteamAcfWriter`, `EpicItemWriter`), which T129 owns and this task must not touch.
