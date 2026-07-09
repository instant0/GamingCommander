# ADR-011: Cross-Platform-Safe Development

## Status
Accepted

## Date
2025-01-10

## Context
Development automation runs on Linux, but the target is Windows. The repository must support both environments without platform-specific assumptions leaking into code.

## Decision
- Use cross-platform-safe .NET APIs unless explicitly required.
- Treat Windows paths (C:\, D:\) as opaque strings outside testing.
- Do not introduce Linux-specific filesystem logic for Windows paths.
- Windows-specific validation tasks are explicitly documented.

## Consequences
- Development can proceed on Linux without a Windows environment.
- Platform-specific Windows features (registry, symlinks) require abstraction.
- Windows validation is a manual step before releases.
- Python research tools must handle both platforms carefully.
