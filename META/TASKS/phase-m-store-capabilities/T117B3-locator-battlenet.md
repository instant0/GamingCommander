# Task T117B3: Locator — Battle.net (after data-source research)

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Low
**Status:** cancelled (T136: 2026-10-31) — detection-only
**Prerequisites:** Task T136 (documentation/planning for unknown data sources)

> **Do not start until T136 confirms the Agent install key and decides whether any inventory is readable.**
> The existing game-root signals already classify Battle.net folders; this task must not guess a data source.

## Objective

Implement the Battle.net locator only as far as T136 allows:

- [ ] Use the exact Agent install key recorded in `planning/137-unknown-store-data-sources.md`.
- [ ] Do **not** read `%ProgramData%\Battle.net\Agent\product.db` unless T136 explicitly approves a reader and
      a dependency; default is `Unsupported`.
- [ ] If only the unverified Agent DB exists, return `Found=false`, `LauncherReadOnlySafety.Unsupported`, with
      the install path when known and a reason.
- [ ] If T136 identifies a confirmed readable source, return `Found=true` with the correct safety.
- [ ] Register with `LauncherDiscoveryService`; no UI/policy wiring.
- [ ] Missing key → `NotFound`; never throw.

## Tests

- [ ] Documented key present → the T136-decided result (unsupported or found/needs verification).
- [ ] Missing key → `NotFound`.
- [ ] No SQLite, network, or package dependency.
- [ ] Mock registry + temp dirs.

## Verification

- [ ] `dotnet build` 0 errors; `dotnet test` green.
- [ ] `DiscoverAll` includes Battle.net.

## Stop rules

- No SQLite reader unless T136 approves it as its own dependency decision.
- Do not use per-game `.build.info` as a global inventory source.
- Do not add Arc here.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
