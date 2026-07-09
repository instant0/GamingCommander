#!/usr/bin/env python3
"""
Epic Games .manifest file parser
Based on meszmate/manifest Go implementation
"""

import struct
import zlib
import json
from pathlib import Path


def read_fstring(data, offset):
    """Read FString: uint32 length + null-terminated string."""
    if offset + 4 > len(data):
        return "", offset
    
    size = struct.unpack('<I', data[offset:offset+4])[0]
    if size == 0:
        return "", offset + 4
    
    offset += 4
    if offset + size > len(data):
        return "", offset
    
    string_data = data[offset:offset+size-1]
    return string_data.decode('utf-8', errors='ignore'), offset + size


def read_fstring_array(data, offset):
    """Read FString array: uint32 count + strings."""
    if offset + 4 > len(data):
        return [], offset
    
    count = struct.unpack('<I', data[offset:offset+4])[0]
    offset += 4
    
    result = []
    for _ in range(count):
        s, offset = read_fstring(data, offset)
        result.append(s)
    
    return result, offset


def parse_manifest(data):
    """Parse Epic manifest file."""
    offset = 0
    
    if len(data) < 25:
        print("File too small")
        return None
    
    header_size = struct.unpack('<i', data[0:4])[0]
    data_size_uncompressed = struct.unpack('<i', data[4:8])[0]
    data_size_compressed = struct.unpack('<i', data[8:12])[0]
    sha_hash = data[12:32]
    stored_as = data[32]
    version = struct.unpack('<i', data[33:37])[0]
    
    print(f"Header Size: {header_size}")
    print(f"Uncompressed: {data_size_uncompressed}")
    print(f"Compressed: {data_size_compressed}")
    print(f"SHA: {sha_hash.hex()}")
    print(f"Stored As: {stored_as} (1=compressed, 2=encrypted)")
    print(f"Version: {version}")
    
    offset = 37
    
    is_compressed = (stored_as & 0x01) != 0
    is_encrypted = (stored_as & 0x02) != 0
    
    print(f"Compressed: {is_compressed}, Encrypted: {is_encrypted}")
    
    if is_encrypted:
        print("File is encrypted - need decryption key")
        return None
    
    if is_compressed:
        body = zlib.decompress(data[offset:])
    else:
        body = data[offset:]
    
    return parse_metadata(body)


def parse_metadata(data):
    """Parse metadata from decompressed data."""
    offset = 0
    
    meta = {}
    
    meta['data_size'], offset = struct.unpack('<I', data[offset:offset+4]), offset + 4
    meta['data_version'], offset = data[offset], offset + 1
    meta['feature_level'], offset = struct.unpack('<i', data[offset:offset+4])[0], offset + 4
    meta['is_file_data'], offset = data[offset] != 0, offset + 1
    meta['app_id'], offset = struct.unpack('<i', data[offset:offset+4])[0], offset + 4
    
    meta['app_name'], offset = read_fstring(data, offset)
    meta['build_version'], offset = read_fstring(data, offset)
    meta['launch_exe'], offset = read_fstring(data, offset)
    meta['launch_command'], offset = read_fstring(data, offset)
    
    prereq_ids, offset = read_fstring_array(data, offset)
    meta['prereq_ids'] = prereq_ids
    
    meta['prereq_name'], offset = read_fstring(data, offset)
    meta['prereq_path'], offset = read_fstring(data, offset)
    meta['prereq_args'], offset = read_fstring(data, offset)
    
    if meta['data_version'] >= 1:
        meta['build_id'], offset = read_fstring(data, offset)
    
    print("\n=== METADATA ===")
    for k, v in meta.items():
        print(f"{k}: {v}")
    
    return meta


def main():
    import sys
    
    if len(sys.argv) < 2:
        print("Usage: python parse_manifest.py <manifest_file>")
        sys.exit(1)
    
    filepath = sys.argv[1]
    
    with open(filepath, 'rb') as f:
        data = f.read()
    
    print(f"File: {filepath}")
    print(f"Size: {len(data)} bytes\n")
    
    result = parse_manifest(data)
    
    if result:
        print("\n=== SUMMARY ===")
        print(f"App Name: {result.get('app_name', 'N/A')}")
        print(f"Build Version: {result.get('build_version', 'N/A')}")
        print(f"Launch Exe: {result.get('launch_exe', 'N/A')}")
        print(f"Launch Command: {result.get('launch_command', 'N/A')}")


if __name__ == "__main__":
    main()