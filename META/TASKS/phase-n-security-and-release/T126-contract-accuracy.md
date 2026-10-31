# Task T126: Reconcile the Data Contract

**Tier:** 1 — Documentation
**Phase:** N — Security and Release Integrity
**Risk:** Low
**Status:** pending
**Prerequisites:** Task T126 depends on Task T124D

> Documentation-only. Do not run build/test. Do not change code in this task.

## Objective

Make the live documents describe exactly what the shipped application reads, writes, and contacts —
no more and no less — before the launcher-discovery and inventory work adds new readers.

## What Needs to Change

### 1. `docs/ONLINE-AND-DATA.md`
- [ ] Add the Epic `.item` **modification** to §2 with its exact target folder, trigger, and user-visible
      entry point. State plainly that the app cannot create an `.item`; it updates one Epic already wrote,
      and refuses when it is absent or unreadable.
- [ ] Add the Steam ACF **creation** rule: only when the target is absent, only in an existing Steam library.
- [ ] Add `data/repair-journal.jsonl` and `data/backups/<utc-stamp>/` to the app-owned write list, and state
      what deleting `data/` removes.
- [ ] Reconcile the "never written" and "never contacted" lists with the code.
- [ ] State that neither repair path creates a store or ProgramData directory; a missing target is a
      refusal, not a creation.
- [ ] Keep one online section: local Epic manifests are the Epic metadata source; the Epic store lookup is
      **not called in v1** and, when wired, is identity-only. Do not list it as a metadata provider.

### 2. `GamingCommander.Readme.txt`
- [ ] Bring ACCESS 1–12 into line with the writes the app can perform, including the Steam library and
      Epic ProgramData paths.
- [ ] Correct ACCESS 10: it currently says `data/` is "the ONLY folder GamingCommander writes to". Name the
      two repair writes and the journal/backups, or the claim is false.
- [ ] Keep the existing plain-language register; do not turn it into a security document.

### 3. `docs/FAQ.md`
- [ ] Add or correct the entry that explains catalogue loss and the backup behaviour once the shared
      JSON write path exists.

### 4. `docs/detection/10-store-capabilities.md`
- [ ] Confirm the `.item` modification description matches §1 of this task.
- [ ] State that Epic store lookup is **identity only** (resolve title/namespace/catalog id for a game local
      files cannot identify; resolve the title before PCGW). Do not call it metadata enrichment and do not
      describe it as `.item` generation.
- [ ] State that local `.item`/`.mancpn` manifests are the Epic metadata source, with PCGW and the Steam
      lookup filling the remaining fields.
- [ ] State that the store GraphQL endpoint is **not called in v1** and is reserved for that identity
      lookup. Do not describe it as removed or as "never contacted".

## Context

- `docs/README.md` names `ONLINE-AND-DATA.md` as the live contract for "ACF/`.item` writes".
- `src/GamingCommander.App/Services/EpicItemWriter.cs` and `SteamAcfWriter.cs` are the only writers
  outside `data/`.
- `src/GamingCommander.App/Services/EpicStoreSearch.cs` is compiled but unreferenced. It is a deferred
  opt-in metadata client, not dead code; do not remove it here.
- ADR-005 defines the safety expectations for writes of this class.

## Requirements

- [ ] Every write path in the code appears in the contract with its target and trigger.
- [ ] Every network endpoint in the code appears in the contract, or is marked as deferred / not called
      in v1.
- [ ] The Readme access inventory and the contract agree.
- [ ] No new "never" claim is introduced without a code reference to support it.
- [ ] No task IDs appear in the documents.

## Verification

- [ ] `grep` for `HttpClient`, `File.Write`, `Directory.CreateDirectory`, `Process.Start` and compare the
      results with §2 of the contract.
- [ ] `grep` for `.item`, `ACF`, `graphql`, and `never` across the live docs; no contradiction remains.
- [ ] The GraphQL endpoint is described as deferred, not absent.
- [ ] `docs/README.md` live table still points at the correct files.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
