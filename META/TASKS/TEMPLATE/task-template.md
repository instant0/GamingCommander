# Task Template — GamingCommander Task Specification

**Purpose:** Standardized task format for all implementation work. Every task must follow this template.

---

## Header Format

```markdown
# Task TX-N: [Clear, Descriptive Title]

**Tier:** [1-3] — [Category]
**Phase:** [Phase Letter] — [Phase Name]
**Effort:** ~X min
**Risk:** [Minimal | Low | Medium | High]
**Status:** [pending | in_progress | completed]
**Prerequisites:** [None | Task TX-N depends on Task TX-M]
```

### Tier Definitions

| Tier | Category | Description |
|------|----------|-------------|
| 1 | Documentation | Docs, comments, README updates — no logic changes |
| 2 | Code Structure | File splits, extractions, renames — pure refactors, no logic changes |
| 3 | Logic/Behavior | Feature work, bug fixes, test creation — changes observable behavior |

### Risk Definitions

| Risk | When to Use |
|------|-------------|
| Minimal | Doc changes, file renames, pure extraction |
| Low | Code moves with no behavior change, test additions |
| Medium | Behavior changes in isolated code paths |
| High | Changes affecting multiple components, core architecture, or safety-critical flows |

---

## Required Sections

Every task MUST contain these sections in order:

### 1. Objective

One paragraph explaining WHY this task exists. What problem does it solve? What improvement does it deliver?

### 2. What Needs to Change

Specific files, methods, or line ranges to modify. For each change:

```markdown
### [File/Namespace/Component Name]
**File:** `path/to/file.cs`
**Current state:** [What exists now]
**Actions:**
- [ ] [Specific action 1]
- [ ] [Specific action 2]
```

For new files:
```markdown
**New file:** `path/to/newfile.cs`
**Content:**
[Exact code to create, or clear description of contents]
```

### 3. Context

- Why these specific changes (not alternatives)
- What was considered and rejected (if any)
- References to architecture decisions, research docs, or prior work

### 4. Requirements

Checklist of all conditions that must be true when the task is complete:

```markdown
- [ ] [Specific requirement 1]
- [ ] [Specific requirement 2]
```

### 5. Verification

Checklist of how to verify the task was completed correctly:

```markdown
- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (N tests)
- [ ] [Specific verification step]
```

### 6. Completion Notes

**Filled in AFTER the task is done:**

```markdown
## Completion Notes

- **Completed:** YYYY-MM-DD
- **What was done:** [Summary of changes]
- **Verification:** [Build/test results]
- **Issues encountered:** [None | Description of issues]
```

---

## Task Naming Convention

```
TX-[sequential-number]-[kebab-case-description].md
```

Examples:
- `T16-extract-filesystem-helpers.md`
- `T17-rename-ambiguous-variables.md`
- `T18-add-folder-scanner-xml-docs.md`
- `T19-extract-available-types-constant.md`

### Phase Grouping

Tasks are grouped by phase in subdirectories:

```
META/TASKS/
├── TEMPLATE/
│   └── task-template.md
├── phase-d-complexity-reduction/
│   ├── T16-extract-filesystem-helpers.md
│   ├── T17-rename-ambiguous-variables.md
│   └── ...
├── phase-e-stabilization/
│   ├── T25-noise-check-divergence-fix.md
│   └── ...
└── COMPLETED/
    └── (moved here when done)
```

---

## Task Generation Rules

### For Planners (Generating Tasks)

1. **30-60 minute target:** Each task should be completable by a junior developer (or AI agent) in 30-60 minutes
2. **One concern per task:** Don't mix documentation + code changes + tests
3. **Dependency ordering:** Tasks that depend on others must state it in Prerequisites
4. **Verify before starting:** Always run `dotnet build && dotnet test` before beginning work
5. **Edit-save-delete:** Never delete-then-edit. Always edit in place or create-then-delete
6. **Documentation first:** Update docs BEFORE making code changes, then finalize AFTER

### For Builders (Completing Tasks)

1. **Read the full task first** — understand all requirements before touching code
2. **Verify prerequisites** — run `dotnet build && dotnet test` to establish baseline
3. **Implement incrementally** — make one change at a time, build after each
4. **Check all requirements** — review each `[ ]` item in the Requirements section
5. **Verify all checks** — run every command in the Verification section
6. **Fill completion notes** — document what was done and any issues
7. **Update session state** — update `META/SESSION/CURRENT.md` and `META/SESSION/NEXT.md`

### Complexity Budget

| Metric | Target | Red Flag | Action |
|--------|--------|----------|--------|
| File length | < 250 lines | > 300 lines | Split into single-responsibility files |
| Method length | < 30 lines | > 50 lines | Extract helper methods |
| Parameters per method | < 5 | > 7 | Use parameter object or split method |
| Nesting depth | < 3 levels | > 4 levels | Extract inner logic to helper |
| XML doc coverage | 100% public members | Any undocumented public member | Add docs before merge |
| Duplicate code | 0 exact duplicates | Any copy-pasted block > 5 lines | Extract to shared utility |

### Growth-Awareness Rule

**Don't wait for files to hit limits — split when responsibilities are clear.**

Before creating or modifying a file, ask:
1. Does this file have MORE THAN ONE clear responsibility?
2. Will this file grow with upcoming features (metadata, categories, search)?
3. Can I extract a self-contained piece NOW while it's small and clean?

If YES to any: split proactively. It's cheaper to split a 200-line file than a 500-line file.

**Example:** `MainWindow.axaml.cs` is 541 lines. The help dialog (`ShowHelpAsync`) is 107 lines of pure UI construction. Even though the file isn't "over limit" yet, extracting HelpDialogBuilder NOW prevents it from growing to 700+ lines when help content expands.

### Naming Rules

| Element | Rule | Bad → Good |
|---------|------|------------|
| Methods | Verb + noun, describe WHAT not HOW | `_scan()` → `DetectStoreSignals()` |
| Variables | Descriptive, no abbreviations | `swPath` → `steamworksPath` |
| Parameters | Full words, clear purpose | `p` → `pattern`, `db` → `databaseService` |
| Boolean vars | Prefix with `is`, `has`, `should` | `ok` → `isValid` |
| Collections | Plural noun | `entries` → `gameEntries` |
| Constants | PascalCase, descriptive | `N` → `MaxRetryCount` |

### Documentation Rules

- Every public member MUST have `/// <summary>` XML doc
- Every internal/private method with non-obvious purpose MUST have `/// <summary>`
- Comments explain WHY, not WHAT
- Use full references: `GameEntry.ExecutablePath` not `entry[2]`
- Reference doc filenames when context matters: `see META/ARCHITECTURE.md`

---

## Task Lifecycle

```
1. PLANNER creates task in META/TASKS/phase-X-name/
   ↓
2. BUILDER reads task, runs baseline verification
   ↓
3. BUILDER implements changes (edit-in-place, incremental)
   ↓
4. BUILDER runs verification checklist
   ↓
5. BUILDER fills completion notes
   ↓
6. BUILDER updates META/SESSION/CURRENT.md
   ↓
7. PLANNER creates next task or updates NEXT.md
```

---

## Example: Minimal Task (Tier 1)

```markdown
# Task TX-N: Fix Broken Link in README.md

**Tier:** 1 — Documentation
**Phase:** A — Documentation Safety Net
**Effort:** ~5 min
**Risk:** Minimal
**Status:** pending

---

## Objective

The README.md links to `docs/architecture.md` which doesn't exist. The correct path is `META/ARCHITECTURE.md`. This is a documentation-only change with no logic impact.

## What Needs to Change

### `README.md`
**File:** `/home/malware/projects/gamingCommander/README.md`
**Current state:** Line 42 contains `[architecture](docs/architecture.md)` — broken link
**Actions:**
- [ ] Replace `docs/architecture.md` with `META/ARCHITECTURE.md` on line 42

## Context

- `META/ARCHITECTURE.md` exists and is the canonical architecture document
- No other files reference `docs/architecture.md`

## Requirements

- [ ] Link resolves to existing file
- [ ] No other broken links introduced

## Verification

- [ ] `dotnet build` passes
- [ ] `dotnet test` passes (N tests)
- [ ] Grep confirms no remaining references to `docs/architecture.md`

## Completion Notes

- **Completed:** YYYY-MM-DD
- **What was done:**
- **Verification:**
- **Issues encountered:**
```

---

## Example: Code Structure Task (Tier 2)

```markdown
# Task TX-N: Extract FileSystemHelper Utility

**Tier:** 2 — Code Structure
**Phase:** D — Complexity Reduction
**Effort:** ~25 min
**Risk:** Minimal
**Status:** pending
**Prerequisites:** None

---

## Objective

`FolderScanner.cs` and `SteamLibraryScanner.cs` contain identical private static methods: `GetDirectoriesSafe()` and `GetLastWriteTimeSafe()`. This violates the DRY principle and makes maintenance harder. Extract these to a shared utility class.

## What Needs to Change

### 1. New file: `src/GamingCommander.App/Services/FileSystemHelper.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create `FileSystemHelper.cs` with namespace `GamingCommander.App.Services`
- [ ] Add `/// <summary>` to class: "Safe filesystem operations that return defaults on failure."
- [ ] Move `GetDirectoriesSafe(string path)` from FolderScanner.cs (line 712)
- [ ] Move `GetLastWriteTimeSafe(DirectoryInfo dir)` from FolderScanner.cs (line 738)
- [ ] Both methods stay `internal static` (shared within App assembly)
- [ ] Add XML docs to both methods

### 2. `src/GamingCommander.App/Services/FolderScanner.cs`

**Current state:** Lines 712-748 contain private static `GetDirectoriesSafe` and `GetLastWriteTimeSafe`
**Actions:**
- [ ] Delete `GetDirectoriesSafe()` (lines 712-722)
- [ ] Delete `GetLastWriteTimeSafe()` (lines 738-748)
- [ ] Update all call sites to use `FileSystemHelper.GetDirectoriesSafe()` and `FileSystemHelper.GetLastWriteTimeSafe()`
- [ ] Add `using GamingCommander.App.Services;` if not already present

### 3. `src/GamingCommander.App/Services/SteamLibraryScanner.cs`

**Current state:** Lines 434-456 contain identical private static `GetDirectoriesSafe` and `GetLastWriteTimeSafe`
**Actions:**
- [ ] Delete `GetDirectoriesSafe()` (lines 434-444)
- [ ] Delete `GetLastWriteTimeSafe()` (lines 446-456)
- [ ] Update all call sites to use `FileSystemHelper.GetDirectoriesSafe()` and `FileSystemHelper.GetLastWriteTimeSafe()`

## Context

- Both methods are identical — exact copy-paste
- `GetDirectoriesSafe` is called ~8 times in FolderScanner, ~5 times in SteamLibraryScanner
- `GetLastWriteTimeSafe` is called ~6 times in FolderScanner, ~3 times in SteamLibraryScanner
- No external consumers — both scanners are in the same assembly

## Requirements

- [ ] `FileSystemHelper.cs` created with both methods
- [ ] Both methods have `/// <summary>` XML docs
- [ ] FolderScanner.cs no longer contains `GetDirectoriesSafe` or `GetLastWriteTimeSafe`
- [ ] SteamLibraryScanner.cs no longer contains `GetDirectoriesSafe` or `GetLastWriteTimeSafe`
- [ ] All call sites updated to use `FileSystemHelper.*`
- [ ] No behavior change — same exception handling

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -r "GetDirectoriesSafe" src/` shows only FileSystemHelper.cs and call sites
- [ ] `grep -r "GetLastWriteTimeSafe" src/` shows only FileSystemHelper.cs and call sites

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
```
