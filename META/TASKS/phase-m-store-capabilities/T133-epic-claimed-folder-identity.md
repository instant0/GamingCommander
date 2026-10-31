# Task T133: Epic Claimed-Folder Identity — Path Only

**Tier:** 2 — Correctness
**Phase:** M — Store Capabilities
**Risk:** Low (one scanner, one regression test)
**Status:** pending
**Prerequisites:** None. Run before the Epic inventory work.

## Objective

Stop `EpicLibraryScanner` from treating a folder **name** as an identity. An Epic catalog item claims the
folder recorded in its `InstallLocation`; a different folder that happens to share the name is not claimed.

## The defect

`EpicLibraryScanner` builds `claimedFolders` from each catalog item and adds both forms:

```csharp
claimedFolders.Add(EpicInstallPath.Normalize(installLocation));
claimedFolders.Add(EpicInstallPath.FolderName(installLocation));
```

Later, a known Epic entry from another library is skipped when either form matches:

```csharp
claimedFolders.Contains(EpicInstallPath.Normalize(folder))
|| claimedFolders.Contains(EpicInstallPath.FolderName(folder))
```

So a catalog item for `D:\Games\Foo` also claims `E:\Games\Foo`. The known entry in `E:\Games\Foo` is not
marked Orphaned and is silently omitted from the Epic anchor, while the same-name item is shown as the
installed one.

This is the same class of collision removed from `EpicInstallPath.Same`; it survived in the scanner.

## What Needs to Change

- [ ] Key `claimedFolders` by the normalized **full path only** (`PathText.NormalizeForCompare`).
- [ ] Remove the `EpicInstallPath.FolderName(...)` entries and the folder-name lookup in the known-entry
      loop.
- [ ] Keep `EpicInstallPath.FolderName` for display/AppName only (writer and UI), not for identity.
- [ ] Record the item's install path even when it is absent on disk, so a later pass can report it; do not
      record its name.
- [ ] Do not change `EpicItemCatalog`'s dedup key (path + display name inside one catalog is a different,
      safe composite).

## Requirements

- [ ] No Epic identity decision compares folder names.
- [ ] Two same-named folders in different libraries remain distinct entries.
- [ ] A known Epic entry is skipped only when its full path matches a catalog install path, or when
      `EpicItemCatalog.MatchesInstall` says so (it uses `EpicInstallPath.Same`, which is path-exact).
- [ ] No behavior change for unique folder names.
- [ ] No writes, no new dependency.

## Verification

- [ ] New test `EpicLibraryScannerTests`: catalog item at `D:\Games\Foo`; known entry at `E:\Games\Foo`;
      assert the second is not skipped and is marked Orphaned (or left as the known entry), and the two ids
      differ.
- [ ] Existing Epic scanner/catalog tests green.
- [ ] `grep -n "FolderName" src/GamingCommander.App/Services/EpicLibraryScanner.cs` shows no identity use.
- [ ] `dotnet build` / `dotnet test` green.

## Context

- `src/GamingCommander.App/Services/EpicLibraryScanner.cs` — the claimed set and the known-entry loop.
- `src/GamingCommander.App/Services/EpicInstallPath.cs` — `Same` is path-exact; `FolderName` is display.
- `src/GamingCommander.Core/Services/PathText.cs` — the comparison rule.
- The connection map records this as a known gap; this task closes it.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
