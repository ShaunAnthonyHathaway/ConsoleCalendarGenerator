#!/usr/bin/env bash
#
# Prints the notes of a release: the text the GitHub release of the version is to carry. It changes nothing
# and creates no release.
#
#   eng/release-notes.sh 2.0.1
#
# The notes of a version are written for the people who read the release page and are kept in
# docs/release-notes/<version>.md, where they are reviewed with the change that prepares the release. The
# changelog has a section for the same version; the two are separate texts. The release has a title of its
# own, so the notes have no top-level heading.

set -euo pipefail
# shellcheck source=eng/common.sh
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"

[ "$#" -eq 1 ] || fail "usage: eng/release-notes.sh <version, such as 2.0.1>"
version="$1"
grep -q -E '^[0-9]+\.[0-9]+\.[0-9]+$' <<< "$version" || fail "'$version' is not a version of the form major.minor.patch"
file="$repository/docs/release-notes/$version.md"
[ -f "$file" ] || fail "there are no release notes for $version: docs/release-notes/$version.md does not exist"
tr -d '\r' < "$repository/CHANGELOG.md" | grep -q "^## \[$version\]" || fail "the changelog has no section for $version"

notes="$(tr -d '\r' < "$file")"
[ -n "$notes" ] || fail "docs/release-notes/$version.md is empty"
if grep -q '^# ' <<< "$notes"; then fail "the notes of $version have a top-level heading; the release has a title of its own"; fi
for asset in "$(package_name "$version")" "$(symbols_name "$version")" "$(binaries_name "$version")" "$checksums_name"; do
  grep -q -F "\`$asset\`" <<< "$notes" || fail "the notes of $version do not name the file $asset"
done
other_versions="$(grep -o -E 'ConsoleCalendarGenerator[.-][0-9]+\.[0-9]+\.[0-9]+' <<< "$notes" | grep -v -F "$version" || true)"
[ -z "$other_versions" ] || fail "the notes of $version name a file of another version: $other_versions"
if grep -o -E '/(blob|tree)/v[0-9][^/) ]*' <<< "$notes" | grep -q -v -x -E "/(blob|tree)/v$version"; then
  fail "the notes of $version link to a tag that is not v$version"
fi
# Trailing empty lines are not part of the notes.
printf '%s\n' "$notes" | awk '{ lines[NR] = $0 } /[^[:space:]]/ { last = NR } END { for (i = 1; i <= last; i++) print lines[i] }'
