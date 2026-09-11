#!/usr/bin/env bash
# Prints the body of one version's section of CHANGELOG.md (Keep a Changelog layout), for the GitHub release notes.
# Usage: eng/changelog-section.sh 1.3.5
set -euo pipefail
version=${1:?usage: changelog-section.sh <version>}
cd "$(dirname "$0")/.."

section=$(awk -v heading="## [$version]" '
  /^## \[/ { printing = (index($0, heading) == 1); next }
  printing && !/^\[[^]]+\]: / { print }
' CHANGELOG.md | sed -e '/./,$!d' | sed -e ':a' -e '/^\n*$/{$d;N;ba' -e '}')

if [[ -z "$section" ]]; then
  echo "::error file=CHANGELOG.md::No section '## [$version]' in CHANGELOG.md" >&2
  exit 1
fi
printf '%s\n' "$section"
