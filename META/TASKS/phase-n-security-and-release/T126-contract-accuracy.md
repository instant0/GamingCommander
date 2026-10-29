# Task T126: Reconcile the Data Contract

**Tier:** 1 — Documentation
**Phase:** N — Security and Release Integrity
**Risk:** Low
**Status:** pending
**Prerequisites:** None

> Documentation-only. Do not run build/test. Do not change code in this task.

## Objective

Make the live documents describe exactly what the shipped application reads, writes, and contacts —
no more and no less — before the launcher-discovery and inventory work adds new readers.

## What Needs to Change

### 1. `docs/ONLINE-AND-DATA.md`
- [ ] Add the Epic `.item` write to §2 with its exact target folder, trigger, and user-visible entry point.
- [ ] Reconcile the "never written" and "never contacted" lists with the code.
- [ ] Record the `data/games.json` trust boundary: it supplies `ExecutablePath` and arguments at launch,
      is local user data, and is not integrity-checked.
- [ ] State what deleting `data/` removes and what backups/journals retain.

### 2. `GamingCommander.Readme.txt`
- [ ] Bring ACCESS 1–12 into line with the writes the app can perform, including the Steam library and
      Epic ProgramData paths.
- [ ] Keep the existing plain-language register; do not turn it into a security document.

### 3. `docs/FAQ.md`
- [ ] Add or correct the entry that explains catalogue loss and the backup behaviour once the shared
      JSON write path exists.

### 4. `docs/detection/10-store-capabilities.md`
- [ ] Confirm the `.item` write description matches §1 of this task.
- [ ] Remove or qualify any statement that the store GraphQL endpoint is contacted.

## Context

- `docs/README.md` names `ONLINE-AND-DATA.md` as the live contract for "ACF/`.item` writes".
- `src/GamingCommander.App/Services/EpicItemWriter.cs` and `SteamAcfWriter.cs` are the only writers
  outside `data/`.
- `src/GamingCommander.App/Services/EpicStoreSearch.cs` is compiled but unreferenced; do not remove it
  here (it is classified by the consolidation review).
- ADR-005 defines the safety expectations for writes of this class.

## Requirements

- [ ] Every write path in the code appears in the contract with its target and trigger.
- [ ] Every network endpoint in the code appears in the contract, or is marked as not shipped.
- [ ] The Readme access inventory and the contract agree.
- [ ] No new "never" claim is introduced without a code reference to support it.
- [ ] No task IDs appear in the documents.

## Verification

- [ ] `grep` for `HttpClient`, `File.Write`, `Directory.CreateDirectory`, `Process.Start` and compare the
      results with §2 of the contract.
- [ ] `grep` for `.item`, `ACF`, `graphql`, and `never` across the live docs; no contradiction remains.
- [ ] `docs/README.md` live table still points at the correct files.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
