# Task T121C: Store Sync Integration Tests

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Low
**Status:** pending
**Prerequisites:** Task T121C depends on Task T121B

## Objective

Prove the full launch-check path with fixtures: first import, new game on the next launch, removed
game marked missing, enrichment only for new games, and no duplicates.

## What Needs to Change

- [ ] Fixture per store group (file-backed, catalog, registry-only).
- [ ] Scenario 1: initial import populates N games.
- [ ] Scenario 2: inventory gains one game → next launch adds exactly one.
- [ ] Scenario 3: inventory loses one game → next launch marks it `Missing`, does not delete.
- [ ] Scenario 4: unchanged inventory → empty diff, no writes.
- [ ] Scenario 5: `EnrichNewGames` scans only new paths.
- [ ] Assert the time budget and that a failing reader does not break other stores.

## Context

- Test the service, not the UI.
- Use the existing fixture/registry mocks.

## Requirements

- [ ] All five scenarios covered.
- [ ] No duplicates, no deletions, no store-file writes.

## Verification

- [ ] `dotnet build` / `dotnet test` green with the new tests.
- [ ] Test names describe the launch behavior.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
