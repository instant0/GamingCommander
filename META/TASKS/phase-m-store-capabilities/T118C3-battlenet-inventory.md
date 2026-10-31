# Task T118C3: Battle.net Inventory Reader (after data-source research)

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** cancelled (T136: 2026-10-31) — no SQLite dependency
**Prerequisites:** Task T118, Task T130, Task T136

> **Do not start until T136 decides whether any Battle.net inventory is readable.** The Agent `product.db`
> is unverified SQLite; the default is detection-only.

## Objective

Implement only the source T136 approves. Default expectation: no reader; Battle.net remains detected by
game-root `.build.info`/`.product.db` markers and the folder scan.

## What Needs to Change

- [ ] Follow the T136 decision; if it says unsupported, add no reader and record detection-only.
- [ ] If a reader is approved, add its dependency, schema, and tests as a separate decision; do not add SQLite
      speculatively.
- [ ] Return records for missing folders when a reader exists.
- [ ] Register through the shared import service.
- [ ] Never read the Agent DB just to “see if it works”.

## Tests

- [ ] Only if a reader is approved: one fixture per shape, missing folder, corrupt store.
- [ ] If unsupported: no new reader, no dependency; detection tests unchanged.

## Verification

- [ ] `dotnet build` 0 errors; `dotnet test` green.

## Stop rules

- No SQLite/WinRT dependency without T136 approval.
- Do not use per-game `.build.info` as a global inventory.
- Do not add Arc here.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
