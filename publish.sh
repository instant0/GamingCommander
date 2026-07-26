#!/usr/bin/env bash
# publish.sh — Stamp version with timestamp, then publish.
# Usage: ./publish.sh [publish-directory]
set -euo pipefail

PUBDIR="${1:-publish}"
VERSION="0.4.$(date +%Y%m%d.%H%M)"
PROPS="Directory.Build.props"

# Stamp version into props
sed -i "s|<Version>.*</Version>|<Version>${VERSION}</Version>|" "$PROPS"
sed -i "s|<FileVersion>.*</FileVersion>|<FileVersion>${VERSION}.0</FileVersion>|" "$PROPS"

echo "Version: ${VERSION}"
dotnet publish src/GamingCommander.App/GamingCommander.App.csproj -c Release -r win-x64 --self-contained -o "$PUBDIR"
echo "Published to ${PUBDIR}/ (${VERSION})"
