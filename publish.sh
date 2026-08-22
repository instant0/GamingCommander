#!/usr/bin/env bash
# publish.sh — Stamp version with timestamp, then publish.
# Usage: ./publish.sh [publish-directory]
set -euo pipefail

PUBDIR="/mnt/r/gc/"
YYDDD=$(date +%y%j)  # e.g. 26207 (year + day-of-year, both fit in 16-bit)
HHMM=$(date +%H%M)
PROPS="Directory.Build.props"

# Version (3-part, max 65535 per component) and FileVersion (4-part with time)
sed -i "s|<Version>.*</Version>|<Version>0.4.${YYDDD}</Version>|" "$PROPS"
sed -i "s|<FileVersion>.*</FileVersion>|<FileVersion>0.4.${YYDDD}.${HHMM}</FileVersion>|" "$PROPS"

echo "Version: 0.4.${YYDDD}.${HHMM}"
dotnet publish src/GamingCommander.App/GamingCommander.App.csproj \
  -c Release -r win-x64 --self-contained \
  -p:DebugType=None \
  -o "$PUBDIR"

# Clean up files that don't belong in the distributable
rm -f "$PUBDIR"/*.pdb
rm -f "$PUBDIR"/createdump.exe
rm -f "$PUBDIR"/*Tests*.dll "$PUBDIR"/*Tests*.deps.json "$PUBDIR"/*Tests*.runtimeconfig.json
rm -rf "$PUBDIR"/CodeCoverage "$PUBDIR"/InstrumentationEngine
rm -f "$PUBDIR"/*.so "$PUBDIR"/*.dylib

echo "Published to ${PUBDIR}/ (0.4.${YYDDD}.${HHMM})"
