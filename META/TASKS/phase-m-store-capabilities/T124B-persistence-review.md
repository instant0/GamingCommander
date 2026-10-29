# Task T124B: Persistence Write-Integrity Review

**Tier:** 1 — Documentation / Architecture
**Phase:** M — Store Capabilities
**Risk:** Low (analysis only)
**Status:** pending
**Prerequisites:** None

> Documentation-only. Do not run build/test. Do not implement the remedy here.

## Objective

Define the shared persistence contract and the directory-creation rule, then produce
`planning/132-persistence-write-integrity.md`. The plan is the input to T130 and to the store readers
that persist through the shared store.

## What Needs to Change

### 1. Inventory
- [ ] Every `JsonFileHelper` caller (25 files) and every persistent `File.WriteAllText`, `File.Copy`,
      `File.Move`, and `Directory.CreateDirectory`.
- [ ] Classify each directory creation as **app-owned** (`data/`, `data/backups/`, `data/staging/`) or
      **external** (store folders, ProgramData, user paths).
- [ ] Record the read behaviour of each caller: does it distinguish "missing" from "corrupt"?

### 2. Shared store contract
- [ ] Define `JsonDocumentStore<T>`: private DTO, shape check on load, one write path, one rolling
      `.bak`.
- [ ] Define the read result: `OK`, `Missing`, `Corrupt`; `Corrupt` carries the parse error and path.
- [ ] `Missing` creates defaults on first run. `Corrupt` keeps the damaged bytes as `.corrupt`,
      restores `.bak`, and reports — it never returns an empty catalogue quietly.
- [ ] Define the write sequence: copy the current file to `.bak` (overwrite) → write the new JSON →
      read it back and deserialize → accept, or copy `.bak` back and report.
- [ ] State that this is the one safeguard: no temp file, no rename, no hash, digest, signature, or
      hash chain. Record why a digest protects nothing (a check the same user can rewrite).
- [ ] Record the failure chain being fixed: `File.WriteAllText` truncates (`FileMode.Create`),
      `ReadFromFile` returns defaults on a parse failure, the next write persists the empty result.

### 3. Directory rule
- [ ] `Directory.CreateDirectory` is allowed only under the application data root.
- [ ] Define one guard helper (for example `AppPaths.EnsureAppDataDirectory`) that rejects a path
      outside that root.
- [ ] External paths are checked with `Directory.Exists` only; a missing external directory is a
      refusal, not a creation.
- [ ] Record that Windows does not create parent directories for `File.WriteAllText`/`Copy`/`Move`;
      the `CreateDirectory` frame in ProcMon is an explicit API call, not implicit OS behaviour.

### 4. Migration
- [ ] Order the consumers, name the shared signatures, list the tests, and state which upcoming work
      must use the store before adding a new persistence path.
- [ ] State the sequencing: required before per-launch sync; a one-shot import may precede it.

## Context

- `src/GamingCommander.App/Services/JsonFileHelper.cs` writes in place and returns defaults on any
  read failure.
- `docs/FAQ.md` currently describes the reset as intended behaviour.
- T130 implements this contract. T121 (per-launch sync) must not ship before it. T118 uses it if it
  exists, otherwise `JsonFileHelper`; neither may add a new direct write.
- Persisted schemas are disposable pre-ship, but user-curated tags, roots, and overrides are not.

## Requirements

- [ ] The plan names one write path and one read result shape.
- [ ] The plan states the app-folder-only `CreateDirectory` rule and the guard signature.
- [ ] The plan lists every caller that must migrate and the order.
- [ ] The plan distinguishes `Missing` from `Corrupt` and preserves a corrupt file.
- [ ] The plan contains no hashing, signing, or hash-chain step, and says why.
- [ ] No code, no behavior changes, no build/test.

## Verification

- [ ] `test -f planning/132-persistence-write-integrity.md`.
- [ ] The plan has a caller table with `file:line`.
- [ ] A test plan covers corrupt, zero-byte, interrupted write, and first run.
- [ ] No direct `File.WriteAllText` remains in the target design.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
