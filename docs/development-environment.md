# Development Environment

## Working Model

The current repository workflow assumes two execution environments:

1. **Linux development environment** for planning, repository scaffolding, portable .NET code, docs, tests that do not depend on Windows APIs, and static validation.
2. **Windows validation environment** for native UI behavior, registry access, launcher integration, manifest handling, and real migration testing.

## What Linux Can Validate

- repository structure and documentation,
- solution and project scaffolding,
- portable .NET library builds,
- unit tests that do not require Windows APIs,
- parser logic against fixtures,
- basic static analysis where supported.

## What Must Be Validated On Windows

- any Windows-native UI stack behavior,
- registry reads and launcher install discovery,
- Steam/Epic/GOG/EA/Ubisoft integration against real local installs,
- junction/symlink behavior as used by the app,
- installer/publishing workflow,
- shell integration and protocol launching.

## Recommended Development Split

- Keep core models, parsing, and migration planning logic portable where practical.
- Isolate Windows-only behavior behind explicit interfaces.
- Use fixture-driven tests for launcher metadata parsers.
- Maintain a short manual Windows verification checklist per milestone.

## Phase 0 Manual Windows Checklist

- Confirm selected UI framework renders correctly on Windows.
- Confirm proof-of-concept app launches successfully.
- Confirm registry abstraction can be implemented against Windows APIs cleanly.
- Confirm any chosen packaging strategy is compatible with the selected UI stack.
