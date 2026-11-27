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

# The version this repository builds: a development version of 2.0, with the assembly and file version of the
# 2.x line. Both are changed here in the same change that changes them in the project file.
expected_version="2.0.0-dev"
expected_assembly_version="2.0.0.0"

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
[ "$version" = "$expected_version" ] || fail "the package is version $version, and this repository builds $expected_version"
[ "$(basename "$package")" = "ConsoleCalendarGenerator.$version.nupkg" ] || fail "the package file name does not match version $version"
[ "$(basename "$symbol_package")" = "ConsoleCalendarGenerator.$version.snupkg" ] || fail "the symbol package file name does not match version $version"
echo "Version:        $version"

commit="$(sed -n -E 's:.*<repository [^>]*commit="([0-9a-f]{40})".*:\1:p' <<< "$nuspec")"
[ -n "$commit" ] || fail "nuspec: no repository commit"

# ---- readme, documentation and symbols ----

readme="$(unzip -p "$package" README.md)"
[ -n "$readme" ] || fail "the packaged readme is empty"
# nuget.org shows only images with absolute URLs from a short list of hosts; any other image is a broken image
# there. The images of this readme are files of this repository, served by raw.githubusercontent.com.
other_images="$(grep -E '!\[[^]]*\]\(' <<< "$readme" | grep -v -E '!\[[^]]+\]\(https://raw\.githubusercontent\.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/master/[^) ]+\)' || true)"
[ -z "$other_images" ] || fail "the packaged readme has an image that is not served from this repository's master branch, or has no alternative text: $other_images"
# nuget.org does not resolve relative links either: every link is absolute or points inside the page.
relative_links="$(grep -o -E '\]\([^)]*\)' <<< "$readme" | grep -v -E '^\]\((https://|#)' || true)"
[ -z "$relative_links" ] || fail "the packaged readme has a link that is neither absolute nor inside the page: $relative_links"

# Every public type of the modern namespace. A new public type is added to this list in the change that adds it.
modern_types="CalendarTheme ConsoleMonthWriter DayState MonthLayout MonthLayoutCell MonthLayoutWeek MonthRenderOptions MonthRenderer MonthWriter RenderedLine RenderedLineKind RenderedMonth RenderedSegment RenderedSegmentKind TitleAlignment WeekdayLabelStyle"

for framework in net10.0 netstandard2.1; do
  # grep -c reads its whole input, so unzip is never cut off mid-write (which "set -o pipefail" would report).
  unzip -p "$package" "lib/$framework/ConsoleCalendarGenerator.xml" | grep -c '<name>ConsoleCalendarGenerator</name>' > /dev/null \
    || fail "lib/$framework/ConsoleCalendarGenerator.xml is not the library's documentation file"
  # Every public type and member is documented in the source. The comments for every type must reach the
  # package, for both builds. (That no public member lacks a comment is enforced when compiling, and
  # XmlDocumentationTests checks every member of the built documentation file.)
  documentation="$(unzip -p "$package" "lib/$framework/ConsoleCalendarGenerator.xml" | tr -d '\r')"
  for type in $modern_types; do
    grep -q "<member name=\"T:ConsoleCalendarGenerator.$type\">" <<< "$documentation" \
      || fail "lib/$framework/ConsoleCalendarGenerator.xml does not document ConsoleCalendarGenerator.$type"
  done
  # The legacy class: the type, each constructor, and both forms of each method.
  for member in 'T:CalendarGenerator.ConsoleCalendar"' 'M:CalendarGenerator.ConsoleCalendar.#ctor"' \
      'M:CalendarGenerator.ConsoleCalendar.#ctor(System.ConsoleColor)"' \
      'M:CalendarGenerator.ConsoleCalendar.#ctor(System.ConsoleColor,System.ConsoleColor)"' \
      'M:CalendarGenerator.ConsoleCalendar.#ctor(System.ConsoleColor,System.ConsoleColor,System.ConsoleColor)"' \
      'M:CalendarGenerator.ConsoleCalendar.#ctor(System.ConsoleColor,System.ConsoleColor,System.ConsoleColor,System.ConsoleColor)"' \
      'M:CalendarGenerator.ConsoleCalendar.GenerateMonth(System.Int32,System.Int32)"' \
      'M:CalendarGenerator.ConsoleCalendar.GenerateMonth(System.Int32,System.Int32,System.Collections.Generic.List{System.DateTime})"' \
      'M:CalendarGenerator.ConsoleCalendar.GenerateYear(System.Int32)"' \
      'M:CalendarGenerator.ConsoleCalendar.GenerateYear(System.Int32,System.Collections.Generic.List{System.DateTime})"' \
      'M:CalendarGenerator.ConsoleCalendar.GetTitleColor"' 'M:CalendarGenerator.ConsoleCalendar.SetTitleColor(System.ConsoleColor)"' \
      'M:CalendarGenerator.ConsoleCalendar.GetDayOfWeekColor"' 'M:CalendarGenerator.ConsoleCalendar.SetDayOfWeekColor(System.ConsoleColor)"' \
      'M:CalendarGenerator.ConsoleCalendar.GetDateColor"' 'M:CalendarGenerator.ConsoleCalendar.SetDateColor(System.ConsoleColor)"' \
      'M:CalendarGenerator.ConsoleCalendar.GetSelectedDateColor"' 'M:CalendarGenerator.ConsoleCalendar.SetSelectedDateColor(System.ConsoleColor)"'; do
    grep -q -F "<member name=\"$member" <<< "$documentation" \
      || fail "lib/$framework/ConsoleCalendarGenerator.xml does not document $member"
  done
  for member in 'M:ConsoleCalendarGenerator.MonthLayout.Create(' 'M:ConsoleCalendarGenerator.MonthRenderer.Render(' \
      'P:ConsoleCalendarGenerator.MonthRenderOptions.Default"' 'F:ConsoleCalendarGenerator.RenderedSegmentKind.Day"' \
      'M:ConsoleCalendarGenerator.DayState.#ctor(' 'P:ConsoleCalendarGenerator.RenderedSegment.State"' \
      'M:ConsoleCalendarGenerator.CalendarTheme.GetColor(' 'M:ConsoleCalendarGenerator.MonthWriter.Write(' \
      'M:ConsoleCalendarGenerator.ConsoleMonthWriter.Write('; do
    grep -q -F "<member name=\"$member" <<< "$documentation" \
      || fail "lib/$framework/ConsoleCalendarGenerator.xml does not document $member"
  done
  # The versions the assembly itself carries. Its version resource is UTF-16, so without the zero bytes each
  # name is directly followed by its value: the assembly version and the file version of the 2.x line, and the
  # package version with the commit the package was built from.
  version_resource="$(unzip -p "$package" "lib/$framework/ConsoleCalendarGenerator.dll" | LC_ALL=C tr -d '\000' \
    | LC_ALL=C grep -a -o -E '(Assembly Version|FileVersion|ProductVersion)[0-9A-Za-z.+-]+' || true)"
  grep -q -F "Assembly Version$expected_assembly_version" <<< "$version_resource" \
    || fail "lib/$framework/ConsoleCalendarGenerator.dll does not have the assembly version $expected_assembly_version"
  grep -q -F "FileVersion$expected_assembly_version" <<< "$version_resource" \
    || fail "lib/$framework/ConsoleCalendarGenerator.dll does not have the file version $expected_assembly_version"
  grep -q -F "ProductVersion$version+$commit" <<< "$version_resource" \
    || fail "lib/$framework/ConsoleCalendarGenerator.dll does not have the informational version $version+$commit"
  # Nothing of the test project is in the package. That includes the frozen source of 1.0.0, which the tests
  # keep as a historical reference: neither its namespace nor the private methods that only it has.
  for file in dll xml; do
    for marker in 'ConsoleCalendarGenerator.Tests' 'Published100' 'ConvertToShortDay' 'GetNextShortDay'; do
      if unzip -p "$package" "lib/$framework/ConsoleCalendarGenerator.$file" | LC_ALL=C grep -a -c -F "$marker" > /dev/null; then
        fail "lib/$framework/ConsoleCalendarGenerator.$file contains $marker, which belongs to the tests"
      fi
    done
  done
  for marker in 'ConsoleCalendarGenerator.Tests' 'Published100'; do
    if unzip -p "$symbol_package" "lib/$framework/ConsoleCalendarGenerator.pdb" | LC_ALL=C grep -a -c -F "$marker" > /dev/null; then
      fail "lib/$framework/ConsoleCalendarGenerator.pdb names a source file of the tests ($marker)"
    fi
  done
  # Source Link: the symbols must say where on GitHub the exact sources are.
  unzip -p "$symbol_package" "lib/$framework/ConsoleCalendarGenerator.pdb" \
    | grep -a -c 'https://raw.githubusercontent.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator/' > /dev/null \
    || fail "lib/$framework/ConsoleCalendarGenerator.pdb has no Source Link to the repository"
done

echo "The package and the symbol package are as expected."
