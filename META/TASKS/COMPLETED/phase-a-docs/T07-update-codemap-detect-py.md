# Task T07: Update CODE_MAP.md for detect.py

**Tier:** 1 — Documentation Cleanup
**Phase:** A — Documentation Safety Net
**Effort:** ~15 min
**Risk:** Minimal
**Status:** completed

---

## Objective

`META/CODE_MAP.md` lists the old Python tools (`detect_folder.py`, `list_standalone_games.py`) as active but does not mention `tools/detect.py` — the unified replacement. Update the Python tools table.

## What Needs to Change

- **File:** `/home/malware/projects/gamingCommander/META/CODE_MAP.md`
- **Section:** "Existing Python Tools (tools/)" — lines 170–185

### Specific Changes

1. **Add `detect.py`** to the table:
   ```
   | `detect.py` | Unified game detection tool (replaces detect_folder.py + list_standalone_games.py) | ✅ Primary |
   ```

2. **Mark `detect_folder.py` as deprecated:**
   - Change status from `✅ Exists` to `⚠️ Deprecated — use detect.py`

3. **Mark `list_standalone_games.py` as deprecated:**
   - Change status from `✅ Exists` to `⚠️ Deprecated — use detect.py`

4. **Optionally add a note** above the table:
   ```markdown
   > **Note:** `detect.py` is the unified replacement for `detect_folder.py` and `list_standalone_games.py`.
   > Deprecated tools are retained for reference only.
   ```

## Context

- `tools/detect.py` (1829 lines) merged `detect_folder.py` and `list_standalone_games.py` into a single tool (Plan 98)
- `META/SESSION/CURRENT.md` lines 46–55 document the merge
- `META/SESSION/NEXT.md` line 93 notes detect.py needs refactoring (separate concern)
- The deprecated tools still exist on disk but should not be referenced as primary

## Requirements

- [ ] Add `detect.py` to the Python tools table
- [ ] Mark `detect_folder.py` and `list_standalone_games.py` as deprecated
- [ ] Keep deprecated tools in the table (they still exist on disk)

## Verification

- [ ] `dotnet build` passes
- [ ] `dotnet test` passes (17 tests)
- [ ] `META/CODE_MAP.md` includes `detect.py` in the tools table
- [ ] Deprecated tools are clearly marked

## Completion Notes

- **Completed:** 2026-07-18
- **What was done:** Updated `META/CODE_MAP.md` Python tools table:
  1. Added note about detect.py being the unified replacement
  2. Added `detect.py` as "✅ Primary" at top of table
  3. Marked `detect_folder.py` as "⚠️ Deprecated — use detect.py"
  4. Marked `list_standalone_games.py` as "⚠️ Deprecated — use detect.py"
- **Verification:** Build clean, 17 tests passing
- **No issues encountered.**
