#!/usr/bin/env python3
"""Build an identification Epic .item from a game folder's .egstore.

Does not parse the binary .manifest (decode_manifest.py fails on some builds).
Catalog ids come from *.mancpn. Launch exe is the first non-redist *.exe under
Binaries/Win64, then Binaries/Win32, then folder root.

Usage:
  python3 tools/generate_epic_item.py /path/to/game
  python3 tools/generate_epic_item.py /path/to/game --out /tmp/game.item
"""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path


def find_egstore(game_dir: Path) -> Path:
    for name in (".egstore", ".egsstore"):
        p = game_dir / name
        if p.is_dir():
            return p
    raise FileNotFoundError(f"no .egstore in {game_dir}")


def read_mancpn(egstore: Path) -> dict:
    files = sorted(egstore.glob("*.mancpn"))
    if not files:
        raise FileNotFoundError(f"no .mancpn in {egstore}")
    data = json.loads(files[0].read_text(encoding="utf-8"))
    ns = (data.get("CatalogNamespace") or "").strip()
    item = (data.get("CatalogItemId") or "").strip()
    app = (data.get("AppName") or "").strip()
    if not ns or not item or not app:
        raise ValueError(f"incomplete .mancpn: {files[0]}")
    return {"namespace": ns, "catalog_id": item, "app_name": app, "source": files[0].name}


def installation_guid(egstore: Path) -> str:
    for pat in ("*.manifest", "*.mancpn"):
        files = sorted(egstore.glob(pat))
        if files:
            return files[0].stem
    return "0" * 32


def pick_launch_exe(game_dir: Path) -> str:
    skip = {"unins000", "unins001", "setup", "vcredist", "dxsetup", "oalinst", "ue3redist"}
    for rel in ("Binaries/Win64", "Binaries/Win32", "Binaries", "."):
        folder = game_dir / rel
        if not folder.is_dir():
            continue
        for exe in sorted(folder.glob("*.exe")):
            if exe.stem.lower() in skip or exe.stem.lower().startswith("unins"):
                continue
            return str(exe.relative_to(game_dir)).replace("\\", "/")
    return ""


def to_windows_install(path: Path) -> str:
    text = str(path.resolve())
    if text.startswith("/mnt/") and len(text) > 6 and text[5].isalpha() and text[6] == "/":
        text = text[5].upper() + ":" + text[6:].replace("/", "\\")
    else:
        text = text.replace("/", "\\")
    return text


def generate_item(game_dir: Path, display_name: str | None = None) -> dict:
    game_dir = game_dir.resolve()
    egstore = find_egstore(game_dir)
    ids = read_mancpn(egstore)
    guid = installation_guid(egstore)
    launch = pick_launch_exe(game_dir)
    install = to_windows_install(game_dir)
    folder = game_dir.name
    name = display_name or Path(launch).stem or folder
    ns, cid, app = ids["namespace"], ids["catalog_id"], ids["app_name"]
    return {
        "FormatVersion": 0,
        "bIsIncompleteInstall": False,
        "LaunchCommand": "",
        "LaunchExecutable": launch.replace("/", "\\") if launch else "",
        "ManifestLocation": install + "/.egstore",
        "ManifestHash": "",
        "bIsApplication": True,
        "bIsExecutable": bool(launch),
        "bIsManaged": False,
        "bNeedsValidation": False,
        "bRequiresAuth": True,
        "bAllowMultipleInstances": False,
        "bCanRunOffline": True,
        "bAllowUriCmdArgs": False,
        "bLaunchElevated": False,
        "BaseURLs": [],
        "BuildLabel": "Live",
        "AppCategories": ["public", "games", "applications"],
        "ChunkDbs": [],
        "CompatibleApps": [],
        "DisplayName": name,
        "InstallationGuid": guid,
        "InstallLocation": install,
        "InstallSessionId": "",
        "InstallTags": [],
        "InstallComponents": [],
        "HostInstallationGuid": "00000000000000000000000000000000",
        "PrereqIds": [],
        "PrereqSHA1Hash": "",
        "LastPrereqSucceededSHA1Hash": "",
        "StagingLocation": install + "\\.egstore\\bps",
        "TechnicalType": "public,games,applications",
        "VaultThumbnailUrl": "",
        "VaultTitleText": "",
        "InstallSize": 0,
        "MainWindowProcessName": "",
        "ProcessNames": [],
        "BackgroundProcessNames": [],
        "IgnoredProcessNames": [],
        "DlcProcessNames": [],
        "ExpectingDLCInstalled": {},
        "MandatoryAppFolderName": folder,
        "OwnershipToken": "true",
        "SidecarConfigRevision": 0,
        "PreloadState": 0,
        "CatalogNamespace": ns,
        "CatalogItemId": cid,
        "AppName": app,
        "AppVersionString": "",
        "MainGameCatalogNamespace": ns,
        "MainGameCatalogItemId": cid,
        "MainGameAppName": app,
        "AllowedUriEnvVars": [],
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("game_dir")
    parser.add_argument("--out", help="write JSON here (default: stdout)")
    parser.add_argument("--name", help="override DisplayName")
    args = parser.parse_args()
    item = generate_item(Path(args.game_dir), display_name=args.name)
    text = json.dumps(item, indent=2)
    if args.out:
        Path(args.out).write_text(text + "\n", encoding="utf-8")
    else:
        print(text)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
