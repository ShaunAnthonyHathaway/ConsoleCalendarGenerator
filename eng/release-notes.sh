#!/usr/bin/env bash
#
# Prints the notes of a release: what the changelog says about the version, word for word. They are what the
# GitHub release of the version is to carry. It changes nothing and creates no release.
#
#   eng/release-notes.sh 2.0.0
#
# The notes are the changelog's section of the version from its first subsection ("### Added") to its end. The
# heading of the section, with its date, and the paragraph under it that says whether the version is published
# yet are left out: the release has a title and a date of its own.

set -euo pipefail
# shellcheck source=eng/common.sh
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"

[ "$#" -eq 1 ] || fail "usage: eng/release-notes.sh <version, such as 2.0.0>"
version="$1"
changelog="$repository/CHANGELOG.md"
tr -d '\r' < "$changelog" | grep -q "^## \[$version\]" || fail "the changelog has no section for $version"

notes="$(tr -d '\r' < "$changelog" | awk -v heading="## [$version]" '
  index($0, heading) == 1 { inside = 1; next }
  inside && /^## / { exit }
  inside && /^\[[^]]+\]: / { exit }
  inside && /^### / { started = 1 }
  inside && started { print }
')"
[ -n "$notes" ] || fail "the section of $version in the changelog has no subsections"
for subsection in "### Added" "### Changed" "### Breaking changes" "### Compatibility"; do
  grep -q -x -F "$subsection" <<< "$notes" || fail "the notes of $version have no '$subsection'"
done
[ "$(grep -c '^- \*\*' <<< "$(awk '/^### Breaking changes$/ { inside = 1; next } /^### / { inside = 0 } inside { print }' <<< "$notes")")" -eq 5 ] \
  || fail "the notes of $version do not list exactly five breaking changes"
# Trailing empty lines are not part of the notes.
printf '%s\n' "$notes" | awk '{ lines[NR] = $0 } /[^[:space:]]/ { last = NR } END { for (i = 1; i <= last; i++) print lines[i] }'
