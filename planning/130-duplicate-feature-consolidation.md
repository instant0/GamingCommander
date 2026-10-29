# 130 — Shared Path Rule and Duplicate-Capability Decisions

**Status:** D1 implemented; D2–D7 open.
**Scope:** the contracts the next launchers, inventory readers, and sync work must reuse instead of
re-deriving. Decisions only where a consumer exists.

---

## D1 — Path text: decided and implemented

**Rule:** one comparison rule, `PathText` (`src/GamingCommander.Core/Services/PathText.cs`).

| Function | Use |
|---|---|
| `TrimTrailingSeparators(path)` | the only allowed touch on a **real** path: whitespace and trailing separators. Separators are left as they are, so the value still works with `Directory`/`File`. |
| `NormalizeForCompare(path)` | backslashes, no trailing separator, drive root kept (`C:` → `C:\`). Case preserved. **Never** passed to the filesystem, a shell, or storage. |
| `Equals(a, b)` | `OrdinalIgnoreCase` on the comparison form. |
| `IsUnder(child, parent)` | segment-aware containment, excludes the exact match. |
| `Leaf(path)` | last folder name. |

**Why case folding lives in the comparison, not the value:** NTFS is case-insensitive, but .NET string
comparison is not, and the same folder arrives from the registry (`\`), ACF and Epic JSON (`/`), and the
folder picker (mixed case). Folding the stored string destroys the casing Windows preserves for display
and for the real path. `ToUpper()` is also culture-sensitive (`install` → `İNSTALL` under a Turkish
locale). Use `StringComparison.OrdinalIgnoreCase` and `StringComparer.OrdinalIgnoreCase`; a separate
derived key is allowed only if it is never displayed, stored as the path, or passed to the shell.

**Migrations landed:**

| Old | Now |
|---|---|
| `SteamAcfParser.NormalizePath` | removed; real-path callers use `TrimTrailingSeparators`, comparisons use `PathText.Equals` |
| `RegistryFallbackDetector.NormalizePath` | `PathText.NormalizeForCompare` (comparison-only, never opened) |
| `EpicInstallPath.Normalize` / `FolderName` | `PathText.NormalizeForCompare` / `PathText.Leaf`; no lowercase |
| `EpicInstallPath.Same` folder-name fallback | removed; `PathText.Equals` |
| `SteamLibraryMembership.Normalize` | `PathText.NormalizeForCompare`, segments split on `\` |
| `LibraryManager.IsChildOf` | `PathText.IsUnder` |
| `GameStoreIndex.NormalizePath` | `PathText.NormalizeForCompare` (index key) |
| `WindowsExplorer.IsUnder` | `PathText.Equals || PathText.IsUnder` (inclusive containment) |

**Behaviour changes, each with a test:**

1. ACF paths written with `/` and registry paths written with `\` for the same folder now compare equal.
2. `C:/Games/X` is now recognised as under `C:\Games` (previously a raw prefix compare said no).
3. `EpicInstallPath.Same` no longer treats two different folders with the same last name as one game.
4. Stored and displayed paths keep their original casing and separators.

**Not covered, on purpose:** UNC paths, `..` resolution, `Path.GetFullPath`, symlinks/junctions, and
per-directory case sensitivity. Windows paths are opaque strings here. `LibraryManager.NormalizeLibraryRoot`
walks up to a Steam root and is a different job; it stays.

---

## D2–D7 — open

| # | Capability | State |
|---|---|---|
| D2 | Store identity | open — `GameStoreIndex` key vs `GameIdentity`; the folder-name heuristic is now removed from D1's scope |
| D3 | Launcher location | open — `SteamInstallPathLocator` vs planned `ILauncherLocator` |
| D4 | Inventory read | open — scanners vs planned `IStoreInventoryReader` |
| D5 | Scan/sync write | open — `LibraryManager` vs planned `StoreSyncService` |
| D6 | Policy gate | open — `IStorePolicyProvider` is the only gate |
| D7 | Stat/fingerprint | open — `FileSystemHelper.GetLastWriteTimeSafe` vs `StoreManifestFingerprint` |

Each is decided when its consumer is about to be built, in the plan for that work.

---

## Do not consolidate

- `LibraryManager.NormalizeLibraryRoot` — tree walk, not a string normaliser.
- `WindowsExplorer.TryNormalizeFolder` — validation (drive-only, rejects UNC, `..`, blocked extensions);
  it may call `PathText` but must not become it.
- `WindowsExplorer.IsUnder` — inclusive of the root; expressed as `Equals || IsUnder`.
- Display-string edition matching (`TitleText`, `EditionTokens`) versus filename-tail matching — different
  inputs, both needed.
- Registry fallback versus manifest inventory — different sources, not duplicates.
- Parent-bound child sets (`ParentBoundChildNames`).

## Reuse checklist for new store work

- [ ] Comparing paths? `PathText.Equals` / `IsUnder`. Never `==`, `ToUpper`, or a new normaliser.
- [ ] Using a path with `Directory`/`File`? `TrimTrailingSeparators` at most; do not change separators.
- [ ] Storing a path? Store what the source gave you.
- [ ] Dictionaries keyed by path? `StringComparer.OrdinalIgnoreCase`.
- [ ] New reader/locator? Implement the decided interface, do not add another entry point.
