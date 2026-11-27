#!/usr/bin/env bash
#
# Checks what "dotnet pack" produced: one package and one symbol package for ConsoleCalendarGenerator, with
# exactly the expected files and the expected metadata. It does not build, pack or publish anything.
#
#   dotnet pack ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj --configuration Release --output artifacts/package
#   eng/verify-package.sh artifacts/package
#
# (The pack command itself runs package validation against the published 1.0.0; see the project file.)

set -euo pipefail
shopt -s nullglob

directory="${1:-artifacts/package}"
repository_url="https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator"

fail() {
  echo "error: $*" >&2
  exit 1
}

# ---- exactly one package and one symbol package, and nothing else ----

packages=("$directory"/ConsoleCalendarGenerator.*.nupkg)
symbols=("$directory"/ConsoleCalendarGenerator.*.snupkg)
everything=("$directory"/*)
[ "${#packages[@]}" -eq 1 ] || fail "expected exactly one ConsoleCalendarGenerator .nupkg in $directory, found ${#packages[@]}"
[ "${#symbols[@]}" -eq 1 ] || fail "expected exactly one ConsoleCalendarGenerator .snupkg in $directory, found ${#symbols[@]}"
[ "${#everything[@]}" -eq 2 ] || fail "expected only the two packages in $directory, found: ${everything[*]}"
package="${packages[0]}"
symbol_package="${symbols[0]}"
echo "Package:        $package"
echo "Symbol package: $symbol_package"

# The files every package has besides its content.
packaging_parts='^(_rels/\.rels|\[Content_Types\]\.xml|package/services/metadata/core-properties/[0-9a-z]+\.psmdcp|ConsoleCalendarGenerator\.nuspec)$'

content_of() {
  unzip -Z1 "$1" | tr -d '\r' | grep -v -E "$packaging_parts" | LC_ALL=C sort
}

# ---- exact content: both library builds with their documentation, and the readme ----

expected_content="README.md
lib/net10.0/ConsoleCalendarGenerator.dll
lib/net10.0/ConsoleCalendarGenerator.xml
lib/netstandard2.1/ConsoleCalendarGenerator.dll
lib/netstandard2.1/ConsoleCalendarGenerator.xml"
actual_content="$(content_of "$package")"
if [ "$actual_content" != "$expected_content" ]; then
  echo "Expected content:" >&2
  echo "$expected_content" >&2
  echo "Actual content:" >&2
  echo "$actual_content" >&2
  fail "the package does not contain exactly the expected files"
fi

expected_symbols="lib/net10.0/ConsoleCalendarGenerator.pdb
lib/netstandard2.1/ConsoleCalendarGenerator.pdb"
actual_symbols="$(content_of "$symbol_package")"
if [ "$actual_symbols" != "$expected_symbols" ]; then
  echo "Actual content:" >&2
  echo "$actual_symbols" >&2
  fail "the symbol package does not contain exactly the two symbol files"
fi

# ---- metadata ----

nuspec="$(unzip -p "$package" ConsoleCalendarGenerator.nuspec | tr -d '\r')"

require() {
  # require <description> <extended regular expression that must match a line of the nuspec>
  grep -q -E "$2" <<< "$nuspec" || fail "nuspec: $1 is missing or not as expected"
}

require "the package id" '<id>ConsoleCalendarGenerator</id>'
require "the author" '<authors>Shaun Hathaway</authors>'
require "the MIT license expression" '<license type="expression">MIT</license>'
require "the readme" '<readme>README\.md</readme>'
require "the project URL" "<projectUrl>$repository_url</projectUrl>"
require "the repository URL and commit" "<repository type=\"git\" url=\"$repository_url\" commit=\"[0-9a-f]{40}\" />"
require "a description" '<description>.+</description>'
require "a copyright" '<copyright>.+</copyright>'
require "tags" '<tags>.+</tags>'
require "the net10.0 dependency group" '<group targetFramework="net10\.0" />'
require "the netstandard2.1 dependency group" '<group targetFramework="\.NETStandard2\.1" />'
if grep -q '<dependency ' <<< "$nuspec"; then
  fail "nuspec: the package must not depend on other packages"
fi

version="$(sed -n -E 's:.*<version>(.+)</version>.*:\1:p' <<< "$nuspec")"
[ -n "$version" ] || fail "nuspec: no version"
[ "$version" != "1.0.0" ] || fail "version 1.0.0 is already published; a build from this repository must not use it"
[ "$(basename "$package")" = "ConsoleCalendarGenerator.$version.nupkg" ] || fail "the package file name does not match version $version"
[ "$(basename "$symbol_package")" = "ConsoleCalendarGenerator.$version.snupkg" ] || fail "the symbol package file name does not match version $version"
echo "Version:        $version"

# ---- readme, documentation and symbols ----

readme="$(unzip -p "$package" README.md)"
[ -n "$readme" ] || fail "the packaged readme is empty"
# nuget.org shows only images with absolute URLs (from a short list of hosts); a relative path would be a broken image.
other_images="$(grep -E '!\[[^]]*\]\(' <<< "$readme" | grep -v -E '!\[[^]]*\]\(https://' || true)"
[ -z "$other_images" ] || fail "the packaged readme has an image that is not an absolute https URL: $other_images"

# Every public type of the modern namespace. A new public type is added to this list in the change that adds it.
modern_types="MonthLayout MonthLayoutCell MonthLayoutWeek MonthRenderOptions MonthRenderer RenderedLine RenderedLineKind RenderedMonth RenderedSegment RenderedSegmentKind TitleAlignment WeekdayLabelStyle"

for framework in net10.0 netstandard2.1; do
  # grep -c reads its whole input, so unzip is never cut off mid-write (which "set -o pipefail" would report).
  unzip -p "$package" "lib/$framework/ConsoleCalendarGenerator.xml" | grep -c '<name>ConsoleCalendarGenerator</name>' > /dev/null \
    || fail "lib/$framework/ConsoleCalendarGenerator.xml is not the library's documentation file"
  # The modern API is documented in the source. The comments for every one of its types must reach the
  # package, for both builds. (That no member of those types lacks a comment is enforced when compiling.)
  documentation="$(unzip -p "$package" "lib/$framework/ConsoleCalendarGenerator.xml" | tr -d '\r')"
  for type in $modern_types; do
    grep -q "<member name=\"T:ConsoleCalendarGenerator.$type\">" <<< "$documentation" \
      || fail "lib/$framework/ConsoleCalendarGenerator.xml does not document ConsoleCalendarGenerator.$type"
  done
  for member in 'M:ConsoleCalendarGenerator.MonthLayout.Create(' 'M:ConsoleCalendarGenerator.MonthRenderer.Render(' \
      'P:ConsoleCalendarGenerator.MonthRenderOptions.Default"' 'F:ConsoleCalendarGenerator.RenderedSegmentKind.Day"'; do
    grep -q -F "<member name=\"$member" <<< "$documentation" \
      || fail "lib/$framework/ConsoleCalendarGenerator.xml does not document $member"
  done
  # Source Link: the symbols must say where on GitHub the exact sources are.
  unzip -p "$symbol_package" "lib/$framework/ConsoleCalendarGenerator.pdb" \
    | grep -a -c 'https://raw.githubusercontent.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/' > /dev/null \
    || fail "lib/$framework/ConsoleCalendarGenerator.pdb has no Source Link to the repository"
done

echo "The package and the symbol package are as expected."
