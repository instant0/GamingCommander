# Task T34: Add VdfParser Unit Tests

**Tier:** 3 — Logic/Behavior
**Phase:** E — Stabilization
**Effort:** ~40 min
**Risk:** Minimal
**Status:** pending

---

## Objective

`VdfParser.cs` (138 lines) has zero test coverage. It parses VDF/ACF key-value files used by Steam (libraryfolders.vdf, appmanifest_*.acf). Add comprehensive tests covering normal parsing, edge cases, and error handling.

## What Needs to Change

### New file: `tests/GamingCommander.Core.Tests/VdfParserTests.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create test class `VdfParserTests` with `[Fact]` and `[Theory]` tests
- [ ] Add test cases:

**Basic parsing:**
- [ ] `Parse_SingleKeyValue_ParsesCorrectly` — `"key" "value"` → `dict["key"] = "value"`
- [ ] `Parse_MultipleKeyValues_ParsesAll` — 3 key-value pairs parsed correctly
- [ ] `Parse_NestedBlocks_ParsesHierarchy` — `"root" { "child" "value" }` → nested dict
- [ ] `Parse_MultipleNestedBlocks_ParsesAll` — Multiple blocks at same level

**Edge cases:**
- [ ] `Parse_EmptyInput_ReturnsEmptyDict` — Empty string → empty dictionary
- [ ] `Parse_WhitespaceOnly_ReturnsEmptyDict` — Just spaces/newlines → empty dictionary
- [ ] `Parse_QuotedValuesWithSpaces_PreservesSpaces` — `"key" "value with spaces"` → preserves spaces
- [ ] `Parse_EscapedQuotes_ParsesCorrectly` — `"key" "value with \"quotes\""` → handles escapes
- [ ] `Parse_TabsAndSpacesBoth_Work` — Mixed indentation → parsed correctly

**Error handling:**
- [ ] `Parse_MalformedInput_ReturnsPartialDict` — Incomplete file → parses what it can
- [ ] `Parse_UnclosedBlock_HandlesGracefully` — Missing closing `}` → no exception, partial result
- [ ] `Parse_QuoteInMiddleOfValue_ParsesValue` — `"key" "val\"ue"` → parses value

**Steam-specific:**
- [ ] `Parse_LibraryfoldersVdf_ParsesAllPaths` — Real libraryfolders.vdf content → correct path extraction
- [ ] `Parse_AppmanifestAcf_ParsesAllFields` — Real ACF content → all required fields extracted
- [ ] `Parse_AcfWithNestedBlocks_ParsesCorrectly` — ACF with nested `"installconfig"` block

## Context

- `VdfParser` is used by `SteamLibraryScanner` to parse `libraryfolders.vdf` and `appmanifest_*.acf`
- The parser handles quoted keys/values, nested blocks, and whitespace
- It's a minimal parser — not full VDF spec, just enough for Steam files
- Test data can be constructed from the mock data in `data/mock/steam/steamapps/`

## Requirements

- [ ] Test file created with 15+ test methods
- [ ] All tests pass: `dotnet test --filter "FullyQualifiedName~VdfParserTests"`
- [ ] Tests cover basic parsing, edge cases, error handling, and Steam-specific formats
- [ ] Test data is inline (no external file dependencies)
- [ ] No `[Collection]` or ordering dependencies between tests

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (now 32+ tests)
- [ ] `dotnet test --filter "FullyQualifiedName~VdfParserTests"` shows all tests passing
- [ ] Test coverage for VdfParser is >80%

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
