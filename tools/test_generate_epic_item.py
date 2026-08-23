#!/usr/bin/env python3
"""Fixture test for generate_epic_item (no real game libraries)."""

import json
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from generate_epic_item import generate_item  # noqa: E402


class GenerateEpicItemTests(unittest.TestCase):
    def test_mancpn_and_win64_exe(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp) / "OrphanGame"
            eg = root / ".egstore"
            eg.mkdir(parents=True)
            (eg / "AABBCCDDEEFF00112233445566778899.mancpn").write_text(
                json.dumps(
                    {
                        "FormatVersion": 0,
                        "CatalogNamespace": "ns-public-example",
                        "CatalogItemId": "item-public-example",
                        "AppName": "app-public-example",
                    }
                ),
                encoding="utf-8",
            )
            win64 = root / "Binaries" / "Win64"
            win64.mkdir(parents=True)
            (win64 / "OrphanGame.exe").write_bytes(b"MZ")
            item = generate_item(root)
            self.assertEqual(item["CatalogNamespace"], "ns-public-example")
            self.assertEqual(item["CatalogItemId"], "item-public-example")
            self.assertEqual(item["AppName"], "app-public-example")
            self.assertEqual(item["InstallationGuid"], "AABBCCDDEEFF00112233445566778899")
            self.assertEqual(item["LaunchExecutable"], "Binaries\\Win64\\OrphanGame.exe")
            self.assertTrue(item["bIsApplication"])
            self.assertNotIn("addons", item["AppCategories"])


if __name__ == "__main__":
    unittest.main()
