#!/usr/bin/env bash
#
# After ConsoleCalendarGenerator was pushed to nuget.org: fetches the four files of the release again, the
# same way the preflight does, and checks what nuget.org serves against them. It reads GitHub and nuget.org
# and needs no credential of nuget.org. It publishes nothing, pushes nothing again, and takes nothing back.
#
#   eng/publication-verify.sh --version 2.0.1 --tag v2.0.1 --release-id 461246889 \
#     --release-commit bff3ee1cb1e80b9cdc3bfe2abdfcbb13a60510cd [--wait <minutes>] [--native-aot] [--into <folder>]
#
# The four values must be the ones of the approved publication, as for eng/publication-preflight.sh.
#
#   1. The four values asked for are the approved ones, and the run is a manual run of the publication
#      workflow on the default branch, or a checkout of this repository on a machine.
#   2. The tag and the release are still the approved ones, and the four files, downloaded from the release,
#      have the approved SHA-256. They are the authority for everything that follows.
#   3. eng/verify-published-package.sh on those files: nuget.org lists the version; the package it serves is
#      the release's package plus nuget.org's signature and nothing else, entry by entry; its metadata, its
#      commit, its two builds, its documentation, its readme, its public API and the versions in its
#      assemblies are the release's; the symbol files on nuget.org's symbol server are the release's; and
#      installed from nuget.org alone into new programs it behaves as the release must, on net10.0 and, for
#      the netstandard2.1 build, on net8.0, with both APIs and the five behaviors of version 2.
#
# With --wait, nuget.org is given that many minutes to index the package and to validate the symbols. A
# failure here changes nothing on nuget.org: the version stays as it was pushed. What to do then is a decision
# for the maintainer, and running the publication again is never it.

set -euo pipefail
shopt -s nullglob
# shellcheck source=eng/common.sh
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"
# shellcheck source=eng/publication.sh
. "$repository/eng/publication.sh"

into=""
options=()
while [ "$#" -gt 0 ]; do
  if [ "$1" = "--native-aot" ]; then
    options+=(--native-aot)
    shift
    continue
  fi
  [ "$#" -ge 2 ] || fail "the option $1 needs a value. See the head of eng/publication-verify.sh."
  if identity_argument "$1" "$2"; then
    shift 2
    continue
  fi
  case "$1" in
    --into) into="$2" ;;
    --wait)
      grep -q -E '^[0-9]+$' <<< "$2" || fail "--wait takes a number of minutes"
      options+=(--wait "$2")
      ;;
    *) fail "unknown option $1. See the head of eng/publication-verify.sh." ;;
  esac
  shift 2
done

new_work_folder
quiet_dotnet
start_facts

echo "== 1. What is asked for, and where this runs"
check_inputs
check_context any-attempt

echo "== 2. The release, and its four files"
check_tag
check_release
if [ -n "$into" ]; then
  if [ -e "$into" ]; then
    existing=("$into"/* "$into"/.[!.]*)
    [ -d "$into" ] && [ "${#existing[@]}" -eq 0 ] || fail "$into exists and is not an empty folder"
  fi
  mkdir -p "$into"
  files="$into"
else
  files="$work/release"
  mkdir "$files"
fi
download_release "$files"
check_checksums "$files"

echo "== 3. What nuget.org serves, against those files"
bash "$repository/eng/verify-published-package.sh" "$files" ${options[@]+"${options[@]}"}

check_checksums "$files" > /dev/null
echo
echo "The four files of release $approved_release_id are the approved ones, and $approved_package_id $approved_version on nuget.org is what they are."
