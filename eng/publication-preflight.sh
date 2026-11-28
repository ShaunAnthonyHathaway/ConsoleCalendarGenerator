#!/usr/bin/env bash
#
# Everything that has to hold before ConsoleCalendarGenerator is published to nuget.org, checked in one place.
# It reads GitHub and nuget.org, downloads the four files of the release and compares. It publishes nothing,
# requests no credential of nuget.org, and cannot: nothing in it pushes a package. Run on a maintainer's
# machine it is a dry run of the same checks.
#
#   eng/publication-preflight.sh --version 2.0.1 --tag v2.0.1 --release-id 461246889 \
#     --release-commit bff3ee1cb1e80b9cdc3bfe2abdfcbb13a60510cd
#
# The four values must be the ones of the approved publication, which eng/publication.sh states. They are
# asked for so that whoever starts the publication says what is to be published, and they select nothing: any
# other value is refused.
#
#   --into <folder>        keep the downloaded files there (an empty or a new folder). Without it they are
#                          downloaded to a temporary folder and removed.
#   --bundle <folder>      do not download: check the files of this folder as the files of the release.
#   --nuget-user <name>    the user the workflow logs in to nuget.org as, which must be the approved one.
#   --simulate <folder>    the self-test's: recorded answers stand for GitHub and nuget.org. Refused in a workflow.
#
# What it requires, in this order, each before the next:
#    1. The four values asked for are the approved version, tag, release id and release commit.
#    2. In a workflow: a manual run of the publication workflow of this repository, on the default branch, in
#       its first attempt. On a machine: a checkout of this repository.
#    3. nuget.org lists the version of 2023 and nothing else, and has no file of the version to publish.
#    4. The tag is annotated, is the approved tag object, and peels to the approved commit.
#    5. The release is the approved one: published, immutable, not a prerelease, the release of the tag, with
#       exactly the four files, each uploaded and recorded by GitHub with the approved checksum.
#    6. The four files, downloaded from the release, have the approved SHA-256, and SHA256SUMS.txt verifies.
#    7. The bundle passes eng/verify-release-bundle.sh: the package and the symbol package as
#       eng/verify-package.sh expects them, the archive, and each symbol file the one its assembly records.
#    8. The package is the approved ID and version, built from the release commit, with no dependency, both
#       builds and the versions of the release in its assemblies.
#    9. Both assemblies have exactly the public API of eng/public-api.txt.
#   10. nuget.org still does not have the version: the last thing looked at.
#
# In the workflow every one of these passes before the login to nuget.org is made, and the login is a later
# step of the same job. See docs/ai-context/packaging-and-releases.md.

set -euo pipefail
shopt -s nullglob
# shellcheck source=eng/common.sh
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"
# shellcheck source=eng/publication.sh
. "$repository/eng/publication.sh"

into=""
bundle=""
nuget_user=""
nuget_user_given=false
while [ "$#" -gt 0 ]; do
  [ "$#" -ge 2 ] || fail "the option $1 needs a value. See the head of eng/publication-preflight.sh."
  if identity_argument "$1" "$2"; then
    shift 2
    continue
  fi
  case "$1" in
    --into) into="$2" ;;
    --bundle) bundle="$2" ;;
    --nuget-user) nuget_user="$2"; nuget_user_given=true ;;
    --simulate) simulation="$2" ;;
    *) fail "unknown option $1. See the head of eng/publication-preflight.sh." ;;
  esac
  shift 2
done
[ -z "$into" ] || [ -z "$bundle" ] || fail "--into downloads the files and --bundle checks files that are there: give one of them"

new_work_folder
quiet_dotnet
start_facts

echo "== 1. What is asked for"
check_inputs

echo "== 2. Where this runs"
check_context first-attempt
if [ "$nuget_user_given" = true ]; then
  must_be "the user for nuget.org" "$nuget_user" "$approved_nuget_user"
fi

echo "== 3. nuget.org does not have the version"
check_not_published

echo "== 4. The tag"
check_tag

echo "== 5. The release"
check_release

echo "== 6. The four files"
if [ -n "$bundle" ]; then
  [ -d "$bundle" ] || fail "there is no folder $bundle"
  files="$bundle"
  passed "not downloaded: the files of $bundle are checked as the files of the release"
else
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
fi
check_checksums "$files"
package="$files/$(package_name "$approved_version")"

echo "== 7. The bundle"
bash "$repository/eng/verify-release-bundle.sh" "$files" > "$work/bundle.log" 2>&1 \
  || { cat "$work/bundle.log" >&2; fail "the files of the release do not pass eng/verify-release-bundle.sh"; }
grep -E '^(Version|Checksums|Packages|Archive|Reproducible|Symbols): ' "$work/bundle.log" | sed 's/^/ok    /'
[ "$(tail -1 "$work/bundle.log")" = "The release bundle is as expected." ] || fail "eng/verify-release-bundle.sh did not finish"

echo "== 8. The package"
check_package "$package"

echo "== 9. The public API"
check_public_api "$package" "$work/public-api"

echo "== 10. nuget.org still does not have the version"
check_not_published
check_checksums "$files" > /dev/null
passed "the four files still have the approved SHA-256 after the checks"

echo
if [ -n "$simulation" ]; then
  echo "SIMULATION: every check passed on recorded answers. This is a test of the checks and authorizes nothing."
else
  echo "Every check passed for $approved_package_id $approved_version: release $approved_release_id, tag $approved_tag, commit $approved_release_commit."
  echo "Nothing was published, and no credential of nuget.org was requested. Publishing is a separate step that needs its own approval."
fi
