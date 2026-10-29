# Task T127: Security Contact and Download Verification

**Tier:** 1 — Documentation
**Phase:** N — Security and Release Integrity
**Risk:** Minimal
**Status:** pending
**Prerequisites:** Task T127 depends on Task T125

> Documentation-only. Do not run build/test.

## Objective

Give a user one obvious way to report a security problem, and one obvious way to check that the
downloaded archive is the one that was built. A contact, not a programme.

## What Needs to Change

- [ ] Add a minimal `SECURITY.md` (or a short Readme section if that is smaller): one contact address or
      private reporting route, the supported version line, and an explicit statement that there is no
      bounty, SLA, or CVE process.
- [ ] Add a "Verify your download" section to `GamingCommander.Readme.txt`:
      `sha256sum -c SHA256SUMS` on Linux/macOS, `Get-FileHash` on Windows, and what a mismatch means.
- [ ] State that the archive is not Authenticode-signed and that the checksum is the verification path.
- [ ] Link the release assets from `docs/FAQ.md` troubleshooting where a user is asked to reinstall.

## Context

- `SHA256SUMS` is published by Task T125.
- `README.md` is the developer entry point; `GamingCommander.Readme.txt` is shipped with the exe.
- The existing issue templates route reports to the public tracker; security reports must not be
  directed there.

## Requirements

- [ ] One private reporting route is stated and is not a public issue URL.
- [ ] No promise of response time, fix time, bounty, or CVE assignment.
- [ ] The verification instructions match the asset names produced by the release workflow.
- [ ] The unsigned-artifact statement is factual and does not imply a guarantee.

## Verification

- [ ] The named contact route exists.
- [ ] Every command in the verification section runs as written against a real release artifact.
- [ ] No document promises a disclosure programme that does not exist.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
