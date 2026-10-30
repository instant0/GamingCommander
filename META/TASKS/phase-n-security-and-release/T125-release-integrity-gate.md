# Task T125: Release Path Cleanup

**Tier:** 3 — Logic/Behavior
**Phase:** N — Security and Release Integrity
**Risk:** Low
**Status:** pending
**Prerequisites:** None

## Objective

Get one release workflow that builds the archive once, publishes it with a checksum, pins its actions,
and holds the minimum token permissions. The checksum is a **corruption check**, not an authenticity
guarantee.

## What this does not do

- It does not stop anyone replacing the release artifact. A checksum published next to the artifact
  shares the same trust root; whoever can replace one can replace both. Only Authenticode signing would
  change that, and it is out of scope by decision.
- It is not an anti-MITM control. Downloads go over HTTPS to GitHub; if that channel is broken, the
  checksum file is broken with it.
- It does not protect a user who does not check the value.

What it does fix: a truncated or wrong file arriving on a slow or proxied connection, a build produced
by a second, drifting workflow, a build step pulled from a moving branch, and a release token exposing
more than publishing.

## What Needs to Change

### 1. One release path
- [ ] Compare `.github/workflows/release.yml` and `.github/workflows/build-release-enhanced.yml`.
- [ ] Keep one publishing workflow and retire the other; two paths that can both publish is the actual
      defect here.
- [ ] Replace the deprecated `actions/create-release@v1` step with the current release action or `gh release`.
- [ ] Attach the built ZIP, the SBOM, the scan report, and `SHA256SUMS` to the release. All four come
      from the same job run.

### 2. Scanning
- [ ] Remove `continue-on-error: true` from the vulnerability step.
- [ ] Fail on high/critical findings; keep low/medium advisory so a base-image CVE does not block a
      release indefinitely.
- [ ] Record an explicit ignore list with a reason per entry.
- [ ] Keep the report artifact even when the job fails.

### 3. Action pinning and permissions
- [ ] Pin every action by full commit SHA with a version comment.
- [ ] Replace `aquasecurity/trivy-action@master` with a pinned SHA.
- [ ] Set `permissions:` to the minimum the release job needs; scope `contents: write` to the publish
      job only.

### 4. Checksum artifact
- [ ] Generate `SHA256SUMS` in the same release job, from the same build output that produced the
      archive, the SBOM, and the scan report — so all four describe the same bytes.
- [ ] Hash the archive and the SBOM; do not include `SHA256SUMS` in itself.
- [ ] Publish it as a release asset next to the other three.
- [ ] Label the user instruction as "verify the download completed correctly", not "verify the
      publisher".

### 5. SBOM
- [ ] One authoritative SBOM only: the CI-generated one. Remove the hand-maintained checked-in copy
      rather than reconciling two files that will drift again.
- [ ] Record the exact generation command in the workflow.

## Context

- Workflows: `.github/workflows/release.yml`, `.github/workflows/build-release-enhanced.yml`.
- The advisory scan currently uses `dotnet list package --vulnerable` and Trivy.
- The checked-in SBOM is version 0.4.0 and hand-generated; the CI SBOM is a different file. Two SBOMs
  is worse than one.
- The user-facing wording lives in the documentation task; it must not promise authenticity.

## Requirements

- [ ] Exactly one workflow can publish a release.
- [ ] A high/critical finding fails the workflow; low/medium does not.
- [ ] No action is referenced by a mutable branch or tag.
- [ ] The release contains the archive, the SBOM, the scan report, and `SHA256SUMS`.
- [ ] Only one SBOM exists.
- [ ] No document describes the checksum as proof of authenticity.
- [ ] The workflow parses and the job graph is valid.

## Verification

- [ ] `actionlint` (or the GitHub workflow parser) reports no errors.
- [ ] A dry run on a branch produces the archive and `SHA256SUMS`.
- [ ] `sha256sum -c SHA256SUMS` succeeds against the downloaded archive.
- [ ] The deprecated release action is gone from the repository.
- [ ] `grep` for `@master`/`@v` in `.github/workflows` returns only SHA pins.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
