# META/SESSION/CURRENT.md — Current Project State

**Nature:** Scratch. **Overwritten** every session handoff.
**Audience:** All agents. Read every session.
**Updated:** 2026-09-10 — EOSH RE: keep/remove gate model CORRECTED in reconcile fn `0x141da6250`; doc `planning/126-facts-eosh-re.md` updated.

---

## This session (2026-09-10)

**EOSH launcher reverse-engineering (DISCOVERY, ongoing).** All findings in `planning/126-facts-eosh-re.md` (append-only facts doc).

### Key result — CORRECTED reconcile gate model

Previous sessions documented a `vtable[0xd0]`-as-gate + hash-match-as-keep model. **Both readings were wrong.** Verified in the disassembly:

- **`vtable[0xd0]` (0x141da9532) is NOT a gate** — empty and non-empty FString outcomes BOTH flow into the removal log (`record 0x142922398`) + removal call (`0x141db0ab0`). The string only fills the log's InstallId field.
- **The 7-field hash lookup (0x141da9493) is a duplicate detector, not a keep test** — hash-match path (0x141da9544) logs "removing stale duplicate … still installed elsewhere" (`record 0x142922278`) and REMOVES; hash-miss path logs "removing installation not reported by EOSH" and REMOVES.
- **Real KEEP gates in phase-2 loop** (over `[rbp-0x50]` FCountedString list, 16-byte `{ptr,count}` elements):
  1. `[obj+0xec] != 0` skip flag (via `vtable[0x8]` at 0x141da93ab) → KEEP
  2. **`vtable[0x48]` on global singleton `0x14316fed0`** at 0x141da942b → TRUE → KEEP (this is the EOSH-reported-set membership test; the singleton is a Meyers-static class instance written at 0x141404267, read at 0x141a04f52/0x141db72a6/0x141da941d/0x141dee704)
- **Reconcile fn `0x141da6250` has NO direct call sites** → invoked via delegate/event (EoshInstallationsChanged pattern → matches observed ~300 ms removal after `.item` load).
- **EOSH `get-installed-apps-response` JSON schema strings fully mapped** (installations/installationId/state/dir/metaDir/manifestPath/pendingManifestPath/platform/sandboxId/itemId/revision/aliases/entries/label/launchCommand/launchExe/prereqSHA1Hash/buildSize/dependencies/pendingTags/stagedVersion/manifestData/pendingManifestData/dependencyConfig — VTAs in doc).
- **Implication for the user question (TR/DS kept vs orphans removed):** decided by `vtable[0x48]` membership in the EOSH-reported set held by global `0x14316fed0`. If server-fed (expected — matches proven server call before deletion), local `.item`/manifest content cannot forge it.

### Open questions (see doc "Next steps")
1. Class/contents of global `0x14316fed0` — is its set populated from the server response or a local cache? (THE question for TR/DS vs orphans)
2. Phase-1 → hash-table relationship (what the 0x68-stride table at `[rbp+0x7a0]` holds).
3. `0x141dcab60` / `0x141dadff0` — how reconcile enumerates local `.item`s into 5-FString records.
4. Class of phase-2 local-install objects (vtable 0xc0/0xd0 getters).
5. Confirm `vtable[0x48]` = `Contains(record)` vs other semantics.

## Prior session context

Plan 125 (code refactor) COMPLETE 2026-09-09 — see git history / NEXT.md prior content. Next app-code plan: `123-detection-bugfixes.md` (PLANNED in planning/README) — not started; read plan before implementing. Standing rules unchanged: one file at a time; new files before removal; behavior-preserving; Windows paths opaque; full build + suite after each step.