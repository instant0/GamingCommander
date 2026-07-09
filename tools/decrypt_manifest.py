#!/usr/bin/env python3
"""
Epic Games .manifest file decryptor
Parses encrypted .manifest files from .egstore folder
"""

import os
import sys
import struct
import hashlib
from pathlib import Path


# Hardcoded key that may work (from ViperSoftX sample)
KNOWN_KEY = bytes.fromhex("71C54C3BCFFCE591A70C0B5BA6448327BC975D89F3021053125F1CB9A7C0AF72")
KNOWN_IV = bytes.fromhex("C0BA0B56EAC742AFD4CB680EE0EB4FB0")


def try_aes_decrypt(data: bytes, key: bytes, iv: bytes) -> bytes | None:
    """Try AES-256-CBC decryption."""
    try:
        from Crypto.Cipher import AES
        cipher = AES.new(key, AES.MODE_CBC, iv)
        decrypted = cipher.decrypt(data)
        
        # Check padding
        pad_len = decrypted[-1]
        if pad_len >= 1 and pad_len <= 16:
            padding = decrypted[-pad_len:]
            if all(b == pad_len for b in padding):
                return decrypted[:-pad_len]
    except Exception:
        pass
    return None


def derive_key(hardware_id: str, user_id: str) -> bytes:
    """Derive key from hardware and user IDs."""
    combined = hardware_id + user_id + "IS"
    return hashlib.sha256(combined.encode()).digest()


def parse_manifest_header(data: bytes) -> dict:
    """Parse manifest header to find metadata."""
    result = {}
    
    # Try to find readable strings in binary
    strings = []
    current = []
    for b in data:
        if 32 <= b <= 126:
            current.append(chr(b))
        else:
            if len(current) > 3:
                strings.append(''.join(current))
            current = []
    if current:
        strings.append(''.join(current))
    
    # Look for common field names
    for s in strings:
        if 'AppName' in s or 'DisplayName' in s or 'Launch' in s:
            print(f"Found: {s}")
    
    return result


def decrypt_manifest_file(filepath: str) -> dict:
    """Try to decrypt an Epic .manifest file."""
    
    with open(filepath, 'rb') as f:
        data = f.read()
    
    print(f"File: {filepath}")
    print(f"Size: {len(data)} bytes")
    print(f"First 32 bytes: {data[:32].hex()}")
    
    # Try known key
    result = try_aes_decrypt(data, KNOWN_KEY, KNOWN_IV)
    if result:
        print("Known key worked!")
        return parse_manifest_header(result)
    
    # Try parsing header to find encryption method
    if len(data) >= 8:
        version = struct.unpack('<I', data[:4])[0]
        flags = struct.unpack('<I', data[4:8])[0]
        print(f"Version: {version}, Flags: {flags}")
        
        # Flags: 0x01 = compressed, 0x02 = encrypted
        if flags & 0x02:
            print("File is encrypted")
    
    # Look for strings
    print("\nSearching for strings...")
    parse_manifest_header(data)
    
    return {}


def main():
    if len(sys.argv) < 2:
        print("Usage: python decrypt_manifest.py <manifest_file>")
        sys.exit(1)
    
    filepath = sys.argv[1]
    if not os.path.exists(filepath):
        print(f"File not found: {filepath}")
        sys.exit(1)
    
    decrypt_manifest_file(filepath)


if __name__ == "__main__":
    main()