# Detection Pattern Analysis Results

## Summary
Analyzed 165 games (1191 executables) from training data. Found 5 total issues:
- 4 false negatives in noise filtering
- 1 store detection gap

## Key Findings

### 1. Python/C# Pattern Parity Gaps

**Python missing patterns (C# has them):**
- `"crash"` - generic crash reporter filter (C# filters 100 exes, Python only 8)
- `"launcher"` - launcher executables (C# filters 40+ exes, Python doesn't)
- `"server"` - dedicated servers (C# filters servers, Python doesn't)
- `"steam"` - Steam-related executables (C# filters steam-related, Python doesn't)

**C# has problematic pattern:**
- `"eos"` in tier_3_store_bootstraps would incorrectly filter `FortniteClient-Win64-Shipping_EAC_EOS.exe` (actual game exe)

**Neither filters:**
- `"error"` - error reporters (BlizzardError, CrypticError, etc.)

### 2. False Negatives (Should Be Noise)
- `CrashReport` (RE8) - crash reporter
- `BlizzardError` (multiple Blizzard games) - error reporter  
- `UnityCrashHandler64` (multiple Unity games) - crash handler
- `UnityCrashHandler32` (Epic Games) - crash handler

### 3. Scoring Accuracy
- **100% accuracy** on 67 tested games
- Correctly selects primary executables in complex scenarios:
  - Backup copies (org_, copy of, etc.)
  - Multiple variants (x86/x64, DX9/DX11)
  - UE shipping builds
  - Launcher vs game exe selection

### 4. Backup Handling
- **100% correct** - all 23 games with backups properly penalized
- Scoring logic correctly handles:
  - `org_` prefix backups
  - `copy of` patterns
  - `-Game.exe` suffix backups
  - Numbered variants

### 5. Store Detection
- **96.6% coverage** (28/29 tested stores detected)
- 1 gap: Battlefield 1 not in training data (likely EA folder structure)

## Proposed Fixes

### Priority 1: Fix Noise Patterns
1. **Add generic `"crash"` pattern** to Python `_NOISE_EXE_PARTS`
2. **Add `"launcher"` pattern** to Python (but carefully - some games have legitimate launchers)
3. **Add `"server"` pattern** to Python for dedicated servers
4. **Add `"steam"` pattern** to Python for Steam-related executables
5. **Remove `"eos"` from C# blacklist** - breaks Fortnite detection
6. **Add `"error"` pattern** to both Python and C# for error reporters

### Priority 2: Documentation Updates
1. Update `docs/GAME-DETECTION-LOGIC.md` with pattern analysis findings
2. Update training test results in documentation
3. Add false positive/negative test cases to blacklist.json

### Priority 3: Training Harness Enhancement
1. Reconstruct virtual filesystem from parsed data
2. Test actual `detect.py` signal detection functions
3. Identify scoring failures in edge cases

## Files to Modify
1. `tools/detect.py` - Add missing noise patterns
2. `data/blacklist.json` - Remove "eos", add "error", update test cases
3. `docs/GAME-DETECTION-LOGIC.md` - Update with findings
4. `tools/train_detection.py` - Enhance with virtual filesystem testing

## Success Criteria
- Zero false negatives in noise filtering
- 100% scoring accuracy on training data
- Python/C# pattern parity achieved
- Documentation reflects actual detection behavior
