# Phase G — Code Quality and Tests

**Plan:** none; these are direct quality tasks.
**Scope:** test-suite shape and removal of app-version compatibility code.

## Task Tracker

| # | Task | Tier | Priority | Status |
|---|------|------|----------|--------|
| T135 | [Remove app-version backward compatibility](T135-remove-app-version-backcompat.md) | 2 | P2 | Done |
| T134 | [Test suite reduction — one test per behavior](T134-test-suite-reduction.md) | 2 | P2 | Done |

## Execution Order

```
T135 (remove version/back-compat code and its tests)
  → T134 (audit the smaller suite, remove duplicates, record behavior map)
```

T135 runs first because it deletes a whole class of tests (schema-version/newer-version) and simplifies
`JsonDocumentStore`; auditing before that would count tests that are about to disappear.

Older task documents in this phase (T48–T57) are historical; they are not part of the current queue.
