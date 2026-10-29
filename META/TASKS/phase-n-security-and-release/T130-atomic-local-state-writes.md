# Task T130: Atomic Local-State Writes

**Tier:** 3 — Logic/Behavior
**Phase:** N — Security and Release Integrity
**Risk:** Medium
**Status:** pending
**Prerequisites:** Task T130 depends on Task T124

## Objective

Make local state durable and failures visible. An interrupted or corrupt write must leave a readable
backup and an explicit error, not a silently empty catalogue.

## What Needs to Change

### 1. One shared write path
- [ ] Implement the shared JSON store defined by `planning/130-duplicate-feature-consolidation.md`.
- [ ] Write to a temp file in the same directory, flush, then replace the target atomically.
- [ ] Keep the previous file as `.bak` before replacement.
- [ ] Delete the temp file on failure.

### 2. Read result
- [ ] Return a result that distinguishes `Ok`, `Missing`, and `Corrupt` instead of returning defaults.
- [ ] `Missing` may create defaults on first run; `Corrupt` must not overwrite automatically.
- [ ] Include the parse error and the file path in the corrupt result.

### 3. Consumers
- [ ] Migrate `JsonFileHelper` consumers to the shared store.
- [ ] Surface `Corrupt` in the UI with the `.bak` location and a "start empty" choice that is explicit.
- [ ] Do not delete or rewrite a corrupt file until the user chooses.

## Context

- `src/GamingCommander.App/Services/JsonFileHelper.cs` uses `File.WriteAllText` and returns defaults on
  any read failure.
- `docs/FAQ.md` currently describes the reset as intended behaviour; the documentation task updates it.
- Persisted schemas are disposable pre-ship, but user-curated tags, roots, and overrides are not.

## Requirements

- [ ] A crash between temp write and replace cannot truncate the live file.
- [ ] A corrupt file is preserved and reported.
- [ ] A `.bak` exists after every successful replace.
- [ ] First run behaves as before: missing files create defaults without an error.
- [ ] No component writes JSON directly outside the shared store.

## Verification

- [ ] Corruption test: truncated, zero-byte, and invalid-JSON files produce `Corrupt`, not defaults.
- [ ] Interruption test: a leftover temp file does not affect the next read.
- [ ] Replace test: `.bak` contains the previous bytes.
- [ ] UI/view-model test: corrupt state shows the message and does not silently reset.
- [ ] `dotnet build` / `dotnet test` green.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
