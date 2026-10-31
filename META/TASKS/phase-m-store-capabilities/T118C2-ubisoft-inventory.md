# Task T118C2: Ubisoft Inventory Reader

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** ready (T136: registry inventory, NeedsVerification)
**Prerequisites:** Task T118, Task T130 (T136 done: registry inventory approved)

> **Do not start until T136 decides Ubisoft's readable source.** Existing registry/`uplay_install.state`
> signals classify folders; an inventory reader needs a confirmed source.

## Objective

Implement the Ubisoft registry inventory that T136 approved: `Ubisoft\Launcher\Installs\{gameId}\InstallDir`,
read-only. Do not parse `uplay_install.manifest` or `uplay_install.state` in this task.

- [ ] Enumerate per-game ids and `InstallDir`; persist the Ubisoft game ID.
- [ ] Return records for missing folders, state `Missing`.
- [ ] Mark the source `NeedsVerification` at the reader boundary; it is registry data, no decryption.
- [ ] No launcher locator, no manifest/state parser, no new dependency.
- [ ] Register through the shared import service.

## Tests

- [ ] Registry inventory with one/multiple ids; missing folder; unknown id shape.
- [ ] Unsupported source → no reader/records.
- [ ] No real Ubisoft install/registry access.

## Verification

- [ ] `dotnet build` 0 errors; `dotnet test` green.

## Stop rules

- Do not parse the manifest or `uplay_install.state`; registry only.
- Do not add a launcher locator (T136: not useful).
- Do not add Battle.net/Arc here.
- A real-install fixture is a follow-up verification, not a blocker; tests use mock registry data.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
