#!/usr/bin/env bash
# Inspects every .nupkg in a directory (default: artifacts/) and fails unless each one carries the README and icon named in its nuspec,
# the LGPL-3.0-or-later license expression, the repository url with a commit, at least one lib/ assembly, and has a symbols package next to it.
# Usage: eng/verify-nupkg.sh [directory]
set -euo pipefail
dir=${1:-artifacts}
shopt -s nullglob
packages=("$dir"/*.nupkg)
if [[ ${#packages[@]} -eq 0 ]]; then
  echo "::error::No .nupkg files in $dir"
  exit 1
fi

status=0
fail() { echo "::error file=$1::$2"; status=1; }

for package in "${packages[@]}"; do
  echo "== $package"
  listing=$(unzip -Z1 "$package")
  nuspec=$(unzip -p "$package" '*.nuspec')

  readme=$(sed -n 's/.*<readme>\(.*\)<\/readme>.*/\1/p' <<<"$nuspec")
  icon=$(sed -n 's/.*<icon>\(.*\)<\/icon>.*/\1/p' <<<"$nuspec")
  [[ -n "$readme" ]] && grep -qx "$readme" <<<"$listing" || fail "$package" "readme '$readme' missing from the package"
  [[ -n "$icon" ]] && grep -qx "$icon" <<<"$listing" || fail "$package" "icon '$icon' missing from the package"
  grep -q '<license type="expression">LGPL-3.0-or-later</license>' <<<"$nuspec" || fail "$package" "license expression is not LGPL-3.0-or-later"
  grep -q '<repository type="git" url="[^"]*"[^>]*commit="[0-9a-f]\{40\}"' <<<"$nuspec" || fail "$package" "repository url or commit missing"
  grep -q '^lib/[^/]*/.*\.dll$' <<<"$listing" || fail "$package" "no lib/ assembly"
  [[ -f "${package%.nupkg}.snupkg" ]] || fail "$package" "symbols package missing"

  sed -n 's/.*<\(id\|version\|readme\|icon\|license[^>]*\)>\(.*\)<\/[a-z]*>.*/  \1: \2/p' <<<"$nuspec"
  grep -E '^(lib/|README|docs/|icon)' <<<"$listing" | sed 's/^/  /'
done

exit $status
