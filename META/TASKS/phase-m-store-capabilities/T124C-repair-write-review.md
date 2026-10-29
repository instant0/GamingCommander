# Task T124C: Repair-Write Safety Review

**Tier:** 1 — Documentation / Architecture
**Phase:** M — Store Capabilities
**Risk:** Low (analysis only; the decision is high-impact)
**Status:** pending
**Prerequisites:** None

> Documentation-only. Do not run build/test. Do not implement the remedy here.

## Objective

Decide **guard or remove** for the two repair writers, and define a contract that never creates a
directory outside the application folder and never writes into it directly: generate the payload in
the application folder, prove it parses, then copy. Produce `planning/133-repair-write-safety.md`.

## What Needs to Change

### 1. Evidence
- [ ] `SteamAcfWriter.TryWrite`: callers, target path derivation, preflight, overwrite behaviour,
      `Directory.CreateDirectory`, failure modes.
- [ ] `EpicItemWriter.TryWrite`: callers, target path, hollow-item replacement, `Directory.CreateDirectory`,
      failure modes.
- [ ] Confirm the UI entry point in `MainWindow.PlatformRepair.cs` and the user-facing wording.
- [ ] Confirm what ADR-005 requires for this class of write.

### 2. Decision
- [ ] Present the three outcomes with their user-visible consequence:
      **guard both**, **guard Steam + remove Epic**, **remove both**.
- [ ] Recommend one. Do not decide silently.

### 3. Contract
- [ ] Preflight: `Directory.Exists(targetDir)` then `File.Exists(targetFile)`. Refuse with a message
      naming the expected location. Never call `Directory.CreateDirectory` on an external path.
- [ ] Target validation:
      Steam — `steamapps` exists and `steamapps\libraryfolders.vdf` exists.
      Epic — the target is the known ProgramData `EpicGamesLauncher\Data\Manifests` folder and exists.
- [ ] Generate in the application folder:
      1. Build the payload in memory (ACF/`.item` are a few KB).
      2. Serialize, re-read, and re-parse it; check required fields and byte length.
      3. Write it to `data/staging/<name>` and read that file back, so the exact bytes to be copied
         are proven to parse.
- [ ] Copy to the target:
      4. `File.Copy(staged, target)` — one call, from a validated source.
      5. Compare the target length with the staged length. On mismatch, delete the target and report.
      6. On `IOException`, delete a zero-length or oversized target if one exists, then report.
      7. Delete the staged file; journal the outcome.
- [ ] Nothing else is written to the target directory: no temp file, no rename step, no second pass.
- [ ] Replace path (hollow `.item`): back up to `data/backups/<timestamp>/`, copy, length-check, and
      restore the backup on mismatch.
- [ ] Do not record a hash. Length plus a successful re-parse is the validation; a digest protects
      nothing here and nothing would verify it.
- [ ] Journal: `data/repair-journal.jsonl`, recording target, backup location, staged length, final
      length, and outcome.
- [ ] Dry run: return target, backup location, payload length, and validation result without writing.
- [ ] Restore: copy the backup back, with the same preflight and no directory creation.
- [ ] Refuse relative paths, UNC paths, and paths the application did not discover itself.

### 4. Windows facts to record
- [ ] `File.WriteAllText`, `File.Copy`, and `File.Move` do **not** create missing parent directories;
      they fail with `DirectoryNotFoundException`.
- [ ] A `CreateDirectory` frame in ProcMon is an explicit API call from a process, not implicit
      filesystem behaviour. Removing our call removes the creation.
- [ ] `File.WriteAllText` opens with `FileMode.Create`, which truncates the target first; overwriting
      `File.Copy` truncates the destination too. A crash mid-copy can leave a short file, so the
      length check is the minimum guard.

### 5. Deliverable
- [ ] `planning/133-repair-write-safety.md` with the decision, the contract, the algorithm, and the
      consumer task area (no task IDs in the plan).
- [ ] Propose an ADR amendment to the human if the contract becomes durable architecture; do not edit
      `META/ADR/` directly.

## Context

- `docs/FAQ.md` and the Readme describe the repair actions; a removal outcome must update them.
- Plan 126 established that Epic file-based registration is a dead end; that weakens the case for
  keeping `EpicItemWriter`, but it does not automatically make the write safe to leave unguarded.
- The earlier security review proposed a generic "backup/journal/dry-run" contract; this task decides
  whether that contract is worth its cost for each writer.
- T129 implements this contract. T117 and T122 must not add a new repair path.

## Requirements

- [ ] Every current write path is listed with `file:line`.
- [ ] The three outcomes and their user-visible consequences are stated.
- [ ] The no-`CreateDirectory` rule and the preflight sequence are explicit.
- [ ] The algorithm names the staging location, the copy step, and the length check, and states that
      nothing else is written to the target directory.
- [ ] The plan states that no hash, signature, or hash chain is used, and why.
- [ ] The plan states what is backed up, where the journal lives, and what a dry run returns.
- [ ] No code, no behavior changes, no build/test.

## Verification

- [ ] `test -f planning/133-repair-write-safety.md`.
- [ ] The plan contains a test matrix: missing target dir, existing target file, hollow `.item`,
      read-only target, interrupted copy (short target), wrong library root.
- [ ] The plan states that a missing target directory is refused and not created.
- [ ] The plan names the target marker used for each store.
- [ ] The contract contains no digest, signature, or hash-chain step.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
