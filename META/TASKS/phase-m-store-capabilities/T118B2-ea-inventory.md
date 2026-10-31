# Task T118B2: EA Inventory Reader (after data-source research)

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** cancelled (T136: 2026-10-31) — EA detection-only
**Prerequisites:** Task T118, Task T130, Task T136

> **Do not start until T136 decides EA's readable source.** The directory/registry signals already classify
> EA folders; an inventory reader is only justified by a confirmed source.

## Objective

Implement only the EA source T136 marks readable. Default expectations:

- Legacy Origin `%ProgramData%\Origin\LocalContent\*.mfst` and per-game registry keys are possible, but
  `NeedsVerification` until confirmed.
- Modern encrypted EA App `IS` is **Unsupported**; never decrypt or guess.
- If T136 finds no readable inventory, this task becomes “EA detection-only” and no reader is added.

## What Needs to Change

- [ ] Follow the T136 decision and exact path/format.
- [ ] Return records for missing folders, state `Missing`.
- [ ] Persist EA content ID only when the source provides it; otherwise omit.
- [ ] No encrypted `IS`, no decryption, no new dependency unless T136 approves one.
- [ ] Register the reader through the shared import service, not a new write path.

## Tests

- [ ] One fixture per readable source; missing folder; corrupt log.
- [ ] Unsupported source → no reader/no records, not guessed.
- [ ] No real EA install/registry access.

## Verification

- [ ] `dotnet build` 0 errors; `dotnet test` green.
- [ ] No writes outside the shared store.

## Stop rules

- If T136 leaves an open question, do not implement; leave EA detection-only and record it.
- Do not add Ubisoft/Battle.net/Arc here.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
