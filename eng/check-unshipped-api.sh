#!/usr/bin/env bash
# Fails when any PublicAPI.Unshipped.txt under src/ still lists API surface. A release ships everything: the entries move to the
# PublicAPI.Shipped.txt next to each file (docs/contributing.md, "Releasing"). The publish workflow runs this on every tag build.
set -euo pipefail
cd "$(dirname "$0")/.."

status=0
while IFS= read -r -d '' file; do
  # Ignore the byte-order mark, CR line endings, blank lines and the `#nullable enable` header the analyzer requires.
  pending=$(sed -e '1s/^\xEF\xBB\xBF//' -e 's/\r$//' "$file" | grep -v -e '^#nullable enable$' -e '^[[:space:]]*$' || true)
  if [[ -n "$pending" ]]; then
    count=$(printf '%s\n' "$pending" | wc -l | tr -d ' ')
    echo "::error file=$file::$count unshipped public API entries; move them to PublicAPI.Shipped.txt before tagging a release."
    printf '%s\n' "$pending" | head -n 20
    status=1
  fi
done < <(find src -name 'PublicAPI.Unshipped.txt' -print0 | sort -z)

if [[ $status -eq 0 ]]; then
  echo "All PublicAPI.Unshipped.txt files are empty."
fi
exit $status
