# Task T128: Supply-Chain Hygiene

**Tier:** 3 — Logic/Behavior
**Phase:** N — Security and Release Integrity
**Risk:** Medium
**Status:** pending
**Prerequisites:** None

## Objective

Keep the advisory scan meaningful: update dependencies automatically, restore from a lock file, drop
end-of-life package pins, and make compiler warnings visible in the build.

## What Needs to Change

### 1. Dependency updates
- [ ] Add `.github/dependabot.yml` for `nuget` and `github-actions`, weekly, grouped by ecosystem.
- [ ] Confirm Dependabot PRs run the normal build and test workflow.

### 2. Locked restore
- [ ] Enable `RestorePackagesWithLockFile` in `Directory.Build.props`.
- [ ] Commit the generated `packages.lock.json` files.
- [ ] Use `RestoreLockedMode` in CI so an unlocked change fails instead of floating.

### 3. End-of-life pins
- [ ] Remove `System.Net.Http` 4.3.4 and `System.Text.RegularExpressions` 4.3.1 from
      `tests/Directory.Build.props` if no test target needs them.
- [ ] If one is still required, replace it with a supported version and record why.

### 4. Warnings
- [ ] Set `TreatWarningsAsErrors` to `true` for the app and core projects.
- [ ] Fix or explicitly suppress the warnings that surface; each suppression carries a reason.
- [ ] Leave the test projects out of the first pass if third-party analyzers make them noisy.

## Context

- `Directory.Build.props` currently sets `TreatWarningsAsErrors` to `false`.
- `tests/Directory.Build.props` carries the EOL pins.
- No `packages.lock.json` exists and no Dependabot configuration exists.
- This task lands at a phase boundary; do not start it mid-chain if a build is in flight.

## Requirements

- [ ] Restore is deterministic in CI.
- [ ] No end-of-life package is referenced.
- [ ] App and core build with warnings as errors.
- [ ] Every new suppression has a comment explaining it.
- [ ] Dependabot covers both NuGet and GitHub Actions.

## Verification

- [ ] `dotnet restore --locked-mode` succeeds from a clean checkout.
- [ ] `dotnet build` and `dotnet test` are green.
- [ ] `dotnet list package --vulnerable --include-transitive` reports no vulnerable package.
- [ ] Removing a lock file entry causes CI restore to fail (checked on a branch, then reverted).

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
