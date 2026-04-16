# Contributing

Thanks for contributing to GamingCommander.

## Before You Start

- Read [`AGENTS.md`](./AGENTS.md).
- Prefer small, reviewable pull requests.
- Open an issue first for large architectural or workflow changes.

## Development Expectations

- Keep launcher integrations isolated.
- Treat migration code as safety-critical.
- Add tests for parsers, registry readers, and migration flows when applicable.
- Avoid committing secrets, local machine paths, or generated launcher caches.

## Pull Requests

- Explain the user problem being solved.
- Describe risks, especially around filesystem mutations and launcher manifest updates.
- Include verification notes for build, tests, and any manual checks.

## Scope Guidance

Current priority order:

1. Base UI shell and configuration flow
2. Stand-alone game detection
3. Steam support
4. Additional launcher integrations
5. Metadata sync and polish
