#!/usr/bin/env bash
#
# Puts together the files of a release from a package that has been built, packed and checked: the package, the
# symbol package, an archive of the library's assemblies for use without NuGet, and a file of checksums.
#
#   dotnet build ConsoleCalendarGenerator.sln --configuration Release -p:ContinuousIntegrationBuild=true
#   dotnet pack ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj --configuration Release --no-build --output artifacts/package
#   eng/release-bundle.sh artifacts/package artifacts/release-bundle
#
# The result, in the output folder and nowhere else:
#
#   ConsoleCalendarGenerator.<version>.nupkg
#   ConsoleCalendarGenerator.<version>.snupkg
#   ConsoleCalendarGenerator-<version>-binaries.zip
#   SHA256SUMS.txt
#
# It builds nothing of the library. The assemblies, their documentation and the readme in the archive are taken
# out of the package, and the symbols out of the symbol package, so the archive cannot hold another build than
# the packages do. They must also be the files of this checkout's Release build, byte for byte, which is the
# build the tests ran against. The license is the one of the commit the package was built from.
#
# It refuses a checkout with uncommitted changes, a package from another commit, a build that names folders of
# this machine (one made without ContinuousIntegrationBuild), and an output folder that is not empty. A bundle
# that does not pass its check is not written to the output folder at all.
#
# It never publishes, uploads, tags or pushes, and it changes no file of the repository. On GitHub Actions a
# bundle is made and checked on every run. A bundle is not a release: the candidate of a version is the one
# bundle that docs/ai-context/packaging-and-releases.md names, and what this script makes anywhere else never is.

set -euo pipefail
shopt -s nullglob
# shellcheck source=eng/common.sh
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"

[ "$#" -eq 2 ] || fail "usage: eng/release-bundle.sh <folder with the packed package> <folder to create the bundle in>"
package_directory="$1"
output="$2"

echo "== The package"
bash "$repository/eng/verify-package.sh" "$package_directory"

packages=("$package_directory"/ConsoleCalendarGenerator.*.nupkg)
package="${packages[0]}"
version="$(basename "$package" .nupkg)"
version="${version#ConsoleCalendarGenerator.}"
symbol_package="$package_directory/$(symbols_name "$version")"
[ -f "$symbol_package" ] || fail "there is no $(symbols_name "$version") beside the package"

if [ -e "$output" ]; then
  [ -d "$output" ] || fail "$output exists and is not a folder"
  existing=("$output"/* "$output"/.[!.]*)
  [ "${#existing[@]}" -eq 0 ] || fail "$output is not empty: ${existing[*]}. A bundle is made in a folder of its own."
fi

echo "== The checkout the package was built from"
nuspec="$(unzip -p "$package" ConsoleCalendarGenerator.nuspec | tr -d '\r')"
commit="$(sed -n -E 's:.*<repository [^>]*commit="([0-9a-f]{40})".*:\1:p' <<< "$nuspec")"
[ -n "$commit" ] || fail "the package does not name the commit it was built from"
head="$(git -C "$repository" rev-parse HEAD)"
[ "$commit" = "$head" ] || fail "the package was built from commit $commit, and this checkout is at $head"
changes="$(git -C "$repository" status --porcelain --untracked-files=no)"
[ -z "$changes" ] || fail "the checkout has uncommitted changes, so the package is not a build of commit $commit:
$changes"
echo "Commit:         $commit, with no uncommitted changes"

new_work_folder
quiet_dotnet

# The files of the build this checkout holds must be the files in the packages: the package was packed from
# this build, and this build is the one that was tested.
stage="$work/stage/ConsoleCalendarGenerator-$version"
for framework in $library_builds; do
  mkdir -p "$stage/$framework"
  for extension in dll xml pdb; do
    built="$repository/ConsoleCalendarGenerator/bin/Release/$framework/ConsoleCalendarGenerator.$extension"
    [ -f "$built" ] || fail "there is no Release build of the library here: $built is missing"
    if [ "$extension" = pdb ]; then
      unzip -p "$symbol_package" "lib/$framework/ConsoleCalendarGenerator.$extension" > "$stage/$framework/ConsoleCalendarGenerator.$extension"
    else
      unzip -p "$package" "lib/$framework/ConsoleCalendarGenerator.$extension" > "$stage/$framework/ConsoleCalendarGenerator.$extension"
    fi
    cmp -s "$built" "$stage/$framework/ConsoleCalendarGenerator.$extension" \
      || fail "$framework/ConsoleCalendarGenerator.$extension in the package is not the file of this checkout's Release build. Pack with --no-build from the build that was tested."
  done
done
unzip -p "$package" README.md > "$stage/README.md"
git -C "$repository" show "$commit:LICENSE" > "$stage/LICENSE"
echo "Build:          the packages hold this checkout's Release build, byte for byte"

# The bundle is put together and checked in the temporary folder, and reaches the output folder only when the
# check has passed, so that a bundle that failed its check is never left where a good one is expected.
echo "== The bundle"
made="$work/bundle"
mkdir "$made"
tool="$(build_stored_zip_tool "$work/tool")"
dotnet "$tool" "$made/$(binaries_name "$version")" "$work/stage"
cp "$package" "$made/$(package_name "$version")"
cp "$symbol_package" "$made/$(symbols_name "$version")"

# One line for each file, "<64 lowercase hexadecimal digits><two spaces><file name>", in the order of the
# names: the format sha256sum writes and checks.
(
  cd "$made"
  for name in $(printf '%s\n' "$(package_name "$version")" "$(symbols_name "$version")" "$(binaries_name "$version")" | LC_ALL=C sort); do
    printf '%s  %s\n' "$(sha256_of "$name")" "$name"
  done > "$checksums_name"
)

echo "== Checking it"
bash "$repository/eng/verify-release-bundle.sh" "$made"

mkdir -p "$output"
cp "$made"/* "$output/"
bash "$repository/eng/verify-release-bundle.sh" "$output" > /dev/null || fail "the bundle changed on its way to $output"
echo "The bundle is in $output."
