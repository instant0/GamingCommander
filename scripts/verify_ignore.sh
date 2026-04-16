#!/usr/bin/env bash
set -euo pipefail

echo "=== IGNORE VERIFICATION: start ==="

if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  echo "Not a git repository. Initialize with: git init && git add . && git commit -m 'Initial scaffold'";
  exit 1
fi

echo
echo "Ignored files reporting (git status --ignored -s):" 
git status --ignored -s

echo
echo "List of untracked files ignored by patterns (git ls-files -i --exclude-standard):" 
git ls-files --others -i --exclude-standard || true

echo
echo "Summary:"
git ls-files --others -i --exclude-standard | wc -l
echo "Completed." 
