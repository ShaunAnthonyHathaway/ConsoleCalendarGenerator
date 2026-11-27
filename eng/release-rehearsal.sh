#!/usr/bin/env bash
#
# A rehearsal of release preparation that changes nothing. It answers one question on every build: if the
# release were prepared from this commit, would the steps of the checklist work?
#
#   eng/release-rehearsal.sh
#
# It builds no package, creates no file in the repository, needs no tag and publishes nothing. In particular it
# never produces a package or an archive that carries the release version: the version identity is worked out
# by evaluating the project, not by building it. What it checks:
#
#   1. The repository still builds a development version, and nothing claims a release.
#   2. With the version suffix removed, the project evaluates to the release identity: package version
#      <major>.<minor>.<patch>, assembly and file version <major>.0.0.0 and the file version of the prefix, an
#      informational version made of the package version and the commit, and release notes that are the
#      changelog of this repository.
#   3. The links of the README that are part of the version can be moved to the tag of the release, by the one
#      substitution of eng/release-links.sh, and the result is a readme the package check accepts.
#   4. The changelog has one "Unreleased" section, which can be given the version and a date.
#   5. Every place that states the development version by number is found, and listed for the person who
#      prepares the release.
#
# The checklist itself is in docs/ai-context/packaging-and-releases.md.

set -euo pipefail
# shellcheck source=eng/common.sh
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"

project="$repository/ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj"
quiet_dotnet
new_work_folder

# property <name> [msbuild arguments] -> the value the project gives the property
property() {
  local name="$1"
  shift
  dotnet msbuild "$project" -p:TargetFramework=net10.0 "$@" -getProperty:"$name" | tr -d '\r'
}

echo "== 1. Today: a development version"
prefix="$(sed -n -E 's:.*<VersionPrefix>([^<]+)</VersionPrefix>.*:\1:p' "$project")"
suffix="$(sed -n -E 's:.*<VersionSuffix>([^<]+)</VersionSuffix>.*:\1:p' "$project")"
grep -q -E '^[0-9]+\.[0-9]+\.[0-9]+$' <<< "$prefix" || fail "the project file has no version prefix of the form major.minor.patch"
[ -n "$suffix" ] || fail "the project file has no version suffix: this is not a development version, and a rehearsal is not what is needed"
development="$prefix-$suffix"
[ "$(property PackageVersion)" = "$development" ] || fail "the project does not evaluate to $development"
tag="v$prefix"
[ -z "$(git -C "$repository" tag --list "$tag")" ] || fail "the tag $tag exists in this checkout, while the repository still builds $development"
for document in README.md CHANGELOG.md CONTRIBUTING.md SECURITY.md; do
  if tr -d '\r' < "$repository/$document" | grep -q -F "/$tag/"; then fail "$document links to the tag $tag, which does not exist"; fi
done
grep -q '^## \[Unreleased\]$' <(tr -d '\r' < "$repository/CHANGELOG.md") || fail "the changelog has no Unreleased section"
echo "Version today:  $development. No tag $tag, no link to it, and the changelog's section is Unreleased."

echo "== 2. The release identity, evaluated and not built"
release_package_version="$(property PackageVersion -p:VersionSuffix=)"
release_assembly_version="$(property AssemblyVersion -p:VersionSuffix=)"
release_file_version="$(property FileVersion -p:VersionSuffix=)"
release_informational_version="$(property InformationalVersion -p:VersionSuffix= -t:AddSourceRevisionToInformationalVersion)"
release_notes="$(property PackageReleaseNotes -p:VersionSuffix=)"
commit="$(git -C "$repository" rev-parse HEAD)"
major="${prefix%%.*}"
[ "$release_package_version" = "$prefix" ] || fail "without the suffix the package version is '$release_package_version', not $prefix"
[ "$release_assembly_version" = "$major.0.0.0" ] || fail "without the suffix the assembly version is '$release_assembly_version', not $major.0.0.0"
[ "$release_file_version" = "$prefix.0" ] || fail "without the suffix the file version is '$release_file_version', not $prefix.0"
[ "$release_informational_version" = "$prefix+$commit" ] || fail "without the suffix the informational version is '$release_informational_version', not $prefix+$commit"
[ "$release_notes" = "$repository_url/blob/master/CHANGELOG.md" ] || [ "$release_notes" = "$repository_url/blob/$tag/CHANGELOG.md" ] \
  || fail "the release notes are '$release_notes', not the changelog of this repository"
[ "$(property PackageId -p:VersionSuffix=)" = "ConsoleCalendarGenerator" ] || fail "the package ID is not ConsoleCalendarGenerator"
echo "Package:        ConsoleCalendarGenerator $release_package_version"
echo "Assembly:       $release_assembly_version, file $release_file_version, informational $release_informational_version"
echo "Release notes:  $release_notes"
echo "The files of that release would be: $(package_name "$prefix"), $(symbols_name "$prefix"), $(binaries_name "$prefix"), $checksums_name"

echo "== 3. The README's links at the tag"
bash "$repository/eng/release-links.sh" "$tag" > "$work/links.log" 2>&1 || { cat "$work/links.log"; fail "the links of the README cannot be moved to $tag"; }
grep -E '^[0-9]+ lines change' "$work/links.log"

echo "== 4. The changelog"
changelog="$(tr -d '\r' < "$repository/CHANGELOG.md")"
[ "$(grep -c '^## \[Unreleased\]$' <<< "$changelog")" -eq 1 ] || fail "the changelog does not have exactly one Unreleased section"
grep -q '^\[Unreleased\]: ' <<< "$changelog" || fail "the changelog has no link definition for the Unreleased section"
if grep -q -E "^## \[$prefix\]" <<< "$changelog"; then fail "the changelog already has a section for $prefix"; fi
# The same file with the section named for the release, on a date that stands for the day of the release.
released="$(sed -e "s/^## \[Unreleased\]$/## [$prefix] - 2000-01-01/" <<< "$changelog")"
headings="$(grep '^## ' <<< "$released")"
[ "$(sed -n 1p <<< "$headings")" = "## [$prefix] - 2000-01-01" ] || fail "the section of $prefix would not be the first section of the changelog"
grep -q -E '^## \[1\.0\.0\] - [0-9]{4}-[0-9]{2}-[0-9]{2}$' <<< "$headings" || fail "the changelog has no dated section for 1.0.0 to follow"
for section in "### Added" "### Changed" "### Breaking changes" "### Compatibility"; do
  grep -q -x -F "$section" <<< "$released" || fail "the changelog has no '$section'"
done
echo "Changelog:      one Unreleased section with its link definition; it becomes '## [$prefix] - <date>', before the section of 1.0.0."

echo "== 5. Where the development version is stated"
# The places that must change in the change that prepares the release, because a check compares them with the
# project file. Each must be found, so that this list cannot fall behind the repository.
must_state() {
  # must_state <file> <fixed text that states the version there>
  local line
  line="$(grep -n -F "$2" "$repository/$1" | head -1 | cut -d: -f1)"
  [ -n "$line" ] || fail "$1 no longer states the development version as '$2'; update eng/release-rehearsal.sh and the checklist"
  printf '  %s:%s\n' "$1" "$line"
}
echo "Change together with the project file:"
must_state "ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj" "<VersionSuffix>$suffix</VersionSuffix>"
must_state "eng/verify-package.sh" "expected_version=\"$development\""
must_state "ConsoleCalendarGenerator.Tests/VersionIdentityTests.cs" "$development"
echo "Documents that name it, each to be read again:"
for document in CHANGELOG.md CONTRIBUTING.md CLAUDE.md docs/ai-context/packaging-and-releases.md docs/ai-context/compatibility.md docs/ai-context/roadmap.md; do
  count="$(grep -c -F "$development" "$repository/$document" || true)"
  [ "$count" -gt 0 ] || fail "$document no longer names $development; update eng/release-rehearsal.sh and the checklist"
  printf '  %s (%s)\n' "$document" "$count"
done

echo
echo "The rehearsal passed. Nothing was built, changed, tagged or published, and no file of version $prefix exists."
