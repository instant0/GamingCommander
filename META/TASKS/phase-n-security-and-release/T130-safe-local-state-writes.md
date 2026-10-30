# Task T130: Safe Local-State Writes

**Tier:** 3 — Logic/Behavior
**Phase:** N — Security and Release Integrity
**Risk:** Medium
**Status:** pending
**Prerequisites:** Task T130 depends on Task T124B

## Objective

One write path for the app's JSON files, with one safeguard: back up, write, verify the file parses,
accept — or put the backup back. A damaged save must never read back as an empty catalogue.

## What Needs to Change

### 1. Write sequence
- [ ] Serialize to a string **first**; a serializer failure must not touch the disk.
- [ ] If the current file exists and is non-empty, copy it to `data/<name>.bak`, overwriting the previous backup.
- [ ] Write the new JSON.
- [ ] Read the file back and deserialize it.
- [ ] Accept if it parses. If it does not, copy `.bak` back over the file and report the failure.
- [ ] No temp file, no rename, no hash, no digest, no signature, no hash chain.

### 2. Read path
- [ ] Missing file → defaults (first run), no error.
- [ ] Parse failure → keep the damaged bytes as `<name>.corrupt` (one file, overwritten), restore
      `<name>.bak`, use the value from `.bak`, and surface a message naming both files.
- [ ] Zero-byte file is treated as a parse failure, not as "missing".
- [ ] Newer schema version (`SchemaVersioning.IsReadable` false) → `Unreadable`: return defaults in memory,
      never write over the file, and surface a message. A newer build's file is not our data to replace.
- [ ] Never return an empty catalogue on a parse failure.

### 3. Consumers
- [ ] Migrate `JsonFileHelper` consumers to the shared store.
- [ ] No component writes JSON directly outside the shared store.
- [ ] Keep the read result in one shape: `Ok`, `Missing`, `Corrupt`, `Unreadable`.

### 4. Directory rule
- [ ] `Directory.CreateDirectory` is allowed only under the application data root
      (`data/`, `data/backups/`, `data/staging/`).
- [ ] Add one guard helper, `AppPaths.EnsureAppDataPath(string filePath)`, that rejects a path outside that
      root by throwing; use it wherever a directory is created.
- [ ] Do not create external directories. If an external directory is missing, surface an error.

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

- [ ] Every successful write leaves a `.bak` containing the previous bytes.
- [ ] A failed verify restores the `.bak` and reports, leaving a parseable file.
- [ ] A corrupt file encountered on read is preserved as `.corrupt`, not overwritten.
- [ ] First run behaves as before: missing files create defaults without an error.
- [ ] No component writes JSON directly outside the shared store.
- [ ] No `Directory.CreateDirectory` call exists outside the application data root.
- [ ] No hash, digest, signature, or hash-chain field is added to any persisted file.
- [ ] The write path performs exactly one file write per save.

## Verification

- [ ] First-run test: no file → defaults, no error, no `.bak`.
- [ ] Write test: `.bak` holds the previous bytes after a successful save.
- [ ] Verify-failure test: a write that cannot be re-parsed restores `.bak` and reports.
- [ ] Corruption test: truncated, zero-byte, and invalid-JSON files produce `Corrupt`, keep the damaged
      bytes as `.corrupt`, restore `.bak`, and do not silently reset.
- [ ] Newer-version test: a file with a higher `Version` produces `Unreadable`, returns defaults in memory,
      and the bytes on disk are unchanged after the read.
- [ ] Rollover test: a second save replaces the `.bak`, it does not accumulate files.
- [ ] Guard test: `AppPaths.EnsureAppDataPath` rejects a path outside the app root and creates nothing.
- [ ] `dotnet build` / `dotnet test` green.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
