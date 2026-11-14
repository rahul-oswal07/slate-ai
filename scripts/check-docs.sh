#!/usr/bin/env bash
# Checks the docs: no task or decision number used twice, no link to a file that isn't there.
# Run from anywhere; called by scripts/verify.sh. Exits non-zero on any failure.
#
# Why: tasks and decisions are one file each (docs/tasks/, docs/decisions/), so git no longer
# stops two branches from both taking "T-111". This does, on the second PR.
set -euo pipefail
cd "$(dirname "$0")/.."

fail=0

# 1. One file per ID in docs/tasks/ and docs/decisions/ (T-111-slug.md, D-100-slug.md).
for dir in docs/tasks docs/decisions; do
  [ -d "$dir" ] || continue
  dups=$(find "$dir" -maxdepth 1 -name '[TD]-[0-9]*.md' -exec basename {} \; \
    | sed -E 's/^([TD]-[0-9]+).*/\1/' | sort | uniq -d)
  for id in $dups; do
    echo "check-docs: $id is used by more than one file in $dir:"
    find "$dir" -maxdepth 1 -name "$id-*.md" | sort | sed 's/^/  /'
    fail=1
  done
done

# 2. Relative markdown links point at something that exists.
#    Looks at [text](target) and ![alt](target); skips web links, mail links and same-page anchors.
while IFS= read -r file; do
  dir=$(dirname "$file")
  # grep -o prints one link per line; the sed keeps the target and drops any #anchor or "title".
  while IFS= read -r target; do
    [ -n "$target" ] || continue
    case "$target" in
      http://*|https://*|mailto:*|tel:*|'<'*) continue ;;
    esac
    path=${target//%20/ }
    if [ ! -e "$dir/$path" ]; then
      echo "check-docs: $file links to '$target', which doesn't exist"
      fail=1
    fi
  done < <(grep -oE '\]\([^)]+\)' "$file" 2>/dev/null \
            | sed -E 's/^\]\(//; s/\)$//; s/[[:space:]]+"[^"]*"$//; s/#.*$//' || true)
done < <(
  find docs -name '*.md' -not -path 'docs/design/prototype/*' -not -path 'docs/hackathon/*'
  for f in README.md CLAUDE.md REVIEW.md; do [ -f "$f" ] && echo "$f"; done
)

if [ "$fail" -ne 0 ]; then
  echo "check-docs: FAILED"
  exit 1
fi
echo "check-docs: OK"
