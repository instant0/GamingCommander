# Task T125: Release Integrity Gate

**Tier:** 3 — Logic/Behavior
**Phase:** N — Security and Release Integrity
**Risk:** Medium
**Status:** pending
**Prerequisites:** None

## Objective

Make the release path fail closed: one workflow publishes the artifact, the SBOM, and a checksum
file; the vulnerability scan fails the job instead of reporting advice.

## What Needs to Change

### 1. One release path
- [ ] Compare `.github/workflows/release.yml` and `.github/workflows/build-release-enhanced.yml`.
- [ ] Keep one publishing workflow and retire the other. Do not leave two paths that can produce a release.
- [ ] Replace the deprecated `actions/create-release@v1` step with the current release action or `gh release`.
- [ ] Attach the built ZIP, the SBOM, and `SHA256SUMS` to the release.

### 2. Fail-closed scanning
- [ ] Remove `continue-on-error: true` from the vulnerability step.
- [ ] Fail on high/critical findings from `dotnet list package --vulnerable` and the image/filesystem scan.
- [ ] Keep the report artifact even when the job fails.

### 3. Action pinning and permissions
- [ ] Pin every action by full commit SHA with a version comment.
- [ ] Replace `aquasecurity/trivy-action@master` with a pinned SHA.
- [ ] Set `permissions:` to the minimum the release job needs; scope `contents: write` to the publish job only.

### 4. Checksums
- [ ] Generate `SHA256SUMS` over the release archive and SBOM.
- [ ] Publish it as a release asset.

### 5. SBOM
- [ ] Choose one authoritative SBOM: the CI-generated Syft output, or a regenerated checked-in copy.
- [ ] If the checked-in copy stays, regenerate it from the release build and record the exact command.
- [ ] Attach the authoritative SBOM to the release.

## Context

- Workflows: `.github/workflows/release.yml`, `.github/workflows/build-release-enhanced.yml`.
- The advisory scan currently uses `dotnet list package --vulnerable` and Trivy.
- The checked-in SBOM is version 0.4.0 and hand-generated; the CI SBOM is a different file.
- The contract and user instructions for verifying the checksum are handled by the documentation task.

## Requirements

- [ ] Exactly one workflow can publish a release.
- [ ] A scan finding fails the workflow.
- [ ] No action is referenced by a mutable branch or tag.
- [ ] The release contains the archive, the SBOM, and `SHA256SUMS`.
- [ ] The checked-in SBOM and the CI SBOM are reconciled, or one is removed.
- [ ] The workflow parses and the job graph is valid.

## Verification

- [ ] `actionlint` (or the GitHub workflow parser) reports no errors.
- [ ] A dry run on a branch produces the archive and `SHA256SUMS`.
- [ ] `sha256sum -c SHA256SUMS` succeeds against the downloaded archive.
- [ ] The deprecated release action is gone from the repository.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
