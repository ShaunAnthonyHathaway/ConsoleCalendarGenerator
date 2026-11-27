#!/usr/bin/env bash
#
# The one substitution release preparation makes in README.md: the links whose target is part of the version
# move from the default branch to the tag of the release. Which links those are is in eng/release-links.txt.
#
#   eng/release-links.sh v2.0.0            shows what would change, checks the result, and changes nothing
#   eng/release-links.sh v2.0.0 --apply    makes the change in README.md
#
# Without --apply this is a rehearsal. It works on a copy, needs no tag to exist and leaves the repository as
# it is, so it can run on every build. It checks that:
#   - every link of the README into this repository is listed in eng/release-links.txt;
#   - every listed link that moves is in the README, and after the substitution none of them is left on the
#     default branch, while every link that stays is unchanged;
#   - the lines that change differ in the branch name alone;
#   - the files and folders the links name exist in this checkout, so they exist at a tag put on it;
#   - the result is a readme that the package check accepts for the released version.
#
# With --apply it writes README.md, and that is all it does: it creates no tag, commits nothing and publishes
# nothing. The links it writes lead nowhere until the tag exists, which is why this is done in the change that
# prepares a release and never before.

set -euo pipefail
# shellcheck source=eng/common.sh
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"

usage="usage: eng/release-links.sh <tag, such as v2.0.0> [--apply]"
tag="${1:-}"
apply=false
case "$#" in
  1) ;;
  2) [ "$2" = "--apply" ] || fail "$usage"
     apply=true ;;
  *) fail "$usage" ;;
esac
if ! grep -q -E '^v[0-9]+\.[0-9]+\.[0-9]+$' <<< "$tag"; then
  fail "'$tag' is not the tag of a release. A release is tagged v<major>.<minor>.<patch>."
fi
version="${tag#v}"
readme="$repository/README.md"
list="$repository/eng/release-links.txt"

moving="$(tr -d '\r' < "$list" | sed -n -E 's/^tag +//p')"
staying="$(tr -d '\r' < "$list" | sed -n -E 's/^master +//p')"
[ -n "$moving" ] && [ -n "$staying" ] || fail "eng/release-links.txt lists no links"
unknown="$(tr -d '\r' < "$list" | grep -v -E '^(#.*|[[:space:]]*|(tag|master) +https://[^ ]+)$' || true)"
[ -z "$unknown" ] || fail "eng/release-links.txt has a line that is neither a comment nor 'tag <address>' nor 'master <address>': $unknown"

# Every link of the README into this repository, as it is written there.
in_readme="$(tr -d '\r' < "$readme" | grep -o -E "\]\(($repository_url|$raw_url)[^) ]*\)" | sed -E 's/^\]\(//; s/\)$//' | LC_ALL=C sort -u)"
listed="$(printf '%s\n%s\n' "$moving" "$staying" | LC_ALL=C sort -u)"
if [ "$in_readme" != "$listed" ]; then
  echo "In the README:" >&2
  echo "$in_readme" >&2
  echo "In eng/release-links.txt:" >&2
  echo "$listed" >&2
  if grep -q -F "/$tag/" <<< "$in_readme"; then
    fail "the README already has links at $tag; the substitution has been made"
  fi
  fail "the links of the README into this repository are not the ones eng/release-links.txt lists"
fi

new_work_folder
tr -d '\r' < "$readme" > "$work/README.before.md"
cp "$work/README.before.md" "$work/README.after.md"
while IFS= read -r link; do
  case "$link" in
    */master/*) ;;
    *) fail "the link $link moves to the tag, but does not name the default branch" ;;
  esac
  target="${link/\/master\///$tag/}"
  path="${link#*/master/}"
  [ -e "$repository/$path" ] || fail "$link names $path, which is not in this checkout"
  # The address is replaced where it is the whole target of a link, and nowhere else.
  awk -v from="]($link)" -v to="]($target)" '{
    line = $0; result = ""
    while ((at = index(line, from)) > 0) { result = result substr(line, 1, at - 1) to; line = substr(line, at + length(from)) }
    print result line
  }' "$work/README.after.md" > "$work/README.next.md"
  mv "$work/README.next.md" "$work/README.after.md"
  grep -q -F "]($target)" "$work/README.after.md" || fail "$link is not in the README"
  if grep -q -F "]($link)" "$work/README.after.md"; then fail "$link is still on the default branch after the substitution"; fi
done <<< "$moving"
while IFS= read -r link; do
  grep -q -F "]($link)" "$work/README.after.md" || fail "$link, which stays on the default branch, is not in the README unchanged"
done <<< "$staying"

# The lines that changed differ in the branch name alone.
diff "$work/README.before.md" "$work/README.after.md" > "$work/difference.txt" || true
changed="$(grep -c '^<' "$work/difference.txt" || true)"
[ "$changed" -gt 0 ] || fail "the substitution changed nothing"
grep '^>' "$work/difference.txt" | sed -e 's/^> //' -e "s#/$tag/#/master/#g" > "$work/changed-back.txt"
grep '^<' "$work/difference.txt" | sed -e 's/^< //' > "$work/changed-from.txt"
cmp -s "$work/changed-back.txt" "$work/changed-from.txt" || fail "a changed line differs in more than the branch name"
[ "$(grep -o -F "/$tag/" "$work/README.after.md" | wc -l | tr -d ' ')" -eq "$(printf '%s\n' "$moving" | wc -l | tr -d ' ')" ] \
  || fail "the number of links at $tag is not the number of links that move"

# The result is a readme the package check accepts for the released version.
bash "$repository/eng/verify-package.sh" --check-readme "$work/README.after.md" "$version" \
  || fail "the README with links at $tag is not one the package check accepts for version $version"

echo "README.md, with the links that are part of the version moved to $tag:"
(diff -U 0 "$work/README.before.md" "$work/README.after.md" || true) | sed -e '1,2d' -e '/^@@/d' | sed -e 's/^/    /'
echo "$changed lines change, in the branch name alone. The links to the changelog, CONTRIBUTING.md, SECURITY.md, the license and the issues stay on the default branch."

if [ "$apply" = true ]; then
  if LC_ALL=C grep -q $'\r' "$readme"; then
    fail "README.md has carriage returns in this checkout; apply the substitution in a checkout with line feeds"
  fi
  cp "$work/README.after.md" "$readme"
  echo "README.md is changed. The links lead nowhere until the tag $tag exists."
else
  echo "Rehearsal only: README.md is unchanged, and no tag is needed or created."
fi
