# Task T31: Close Stale TECH_DEBT Entries

**Tier:** 1 — Documentation
**Phase:** E — Stabilization
**Effort:** ~15 min
**Risk:** Minimal
**Status:** ✅ completed

---

## Objective

Bugs 1-4 in `META/BACKLOG/TECH_DEBT.md` were fixed in code but the entries were never marked as closed. Bug 5 was also fixed but the entry says "Open" for Bug 6/7. Verify which bugs are actually fixed in code, mark them as closed, and clean up the document.

## What Needs to Change

### `META/BACKLOG/TECH_DEBT.md`

**Current state:** 
- Bug 1 (GOG detection): Status says "✅ Fixed" — verify in code
- Bug 2 (EA detection): Status says "✅ Fixed" — verify in code
- Bug 3 (Ubisoft detection): Status says "✅ Fixed" — verify in code
- Bug 4 (Recursive Directory.GetFiles): Status says "✅ Fixed" — verify in code
- Bug 5 (Static vs instance noise check): Status says "✅ Fixed" — verify in code
- Bug 6 (Blacklist tier flattening): Status says "Open" — confirmed still open
- Bug 7 (ScoreExecutable ignores JSON blacklist): Status says "Open" — confirmed still open

**Actions:**
- [x] Verify Bug 1 is fixed: `grep -n "goggame\*" src/GamingCommander.App/Services/FolderScanner.cs` — should show prefix match
- [x] Verify Bug 2 is fixed: `grep -n "__Installer" src/GamingCommander.App/Services/FolderScanner.cs` — should show directory check
- [x] Verify Bug 3 is fixed: `grep -n "uplay_install.manifest" src/GamingCommander.App/Services/FolderScanner.cs` — should show file check
- [x] Verify Bug 4 is fixed: `grep -n "TopDirectoryOnly" src/GamingCommander.App/Services/FolderScanner.cs` — should show non-recursive
- [x] Verify Bug 5 is fixed: `grep -n "IsNoiseExeName" src/GamingCommander.App/Services/FolderScanner.cs` — should show instance method
- [x] Mark Bugs 1-5 as "✅ Fixed" with verification date
- [x] Leave Bugs 6-7 as "Open" (to be fixed in T32, T33)
- [x] Add a "Last verified" timestamp to the header

## Context

- Bugs 1-4 were fixed during Phase 1.2 research
- Bug 5 was fixed during detection hardening (Plan 99)
- The entries were documented but never updated to reflect the fixes
- Bugs 6-7 are genuinely still open and will be fixed in Phase E

## Requirements

- [x] All 5 fixed bugs marked as "✅ Fixed" with verification date
- [x] Bugs 6-7 remain "Open"
- [x] No other changes to the document

## Verification

- [x] `dotnet build` passes
- [x] `dotnet test` passes (17 tests)
- [x] `grep -c "✅ Fixed" META/BACKLOG/TECH_DEBT.md` returns 5 (Bugs 1-5)
- [x] `grep -c "Open" META/BACKLOG/TECH_DEBT.md` returns 2 (Bugs 6-7) + 3 other open items

## Completion Notes

- **Completed:** 2026-07-19
- **What was done:** Verified all 5 bugs are fixed in code. Added verification dates and line references to each bug entry. Added "Last verified" timestamp to document header.
- **Verification:** Build clean, 17 tests passing. Bugs 1-5 confirmed fixed with code evidence.
- **Issues encountered:** None
