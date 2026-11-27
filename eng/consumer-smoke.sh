#!/usr/bin/env bash
#
# Installs the locally packed ConsoleCalendarGenerator package into small, separate applications, the way a
# consumer would, and checks that they build, run and get the library build meant for them.
#
#   dotnet pack ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj --configuration Release --output artifacts/package
#   eng/consumer-smoke.sh artifacts/package [--native-aot] [--published-sample]
#
# Everything happens in a temporary folder outside the repository, which is deleted afterwards:
#   - the package is copied to a local feed there, and NuGet is told to take ConsoleCalendarGenerator from that
#     feed only, never from nuget.org;
#   - NuGet's package cache is a folder there too, so the development package never enters the machine's cache.
#
# --native-aot also publishes an application with Native AOT against the package and runs it. That needs the
# platform's native build tools (for example clang on Linux).
#
# The sample program that accompanied 1.0.0 is compiled everywhere. On Linux with the util-linux "script" program
# it is also run, on a pseudo-terminal, and what it writes is compared with a file; see "The original sample"
# below for the exact scenario. --published-sample runs the same scenario with the published 1.0.0 package as
# well, compares the two by the differences 2.0 has on purpose, and runs the program that was compiled against
# 1.0.0 with the library under test put in its place. That is the only part of this script that takes ConsoleCalendarGenerator from nuget.org, and it does
# so in a folder of its own.

set -euo pipefail
shopt -s nullglob

package_directory=""
native_aot=false
published_sample=false
for argument in "$@"; do
  case "$argument" in
    --native-aot) native_aot=true ;;
    --published-sample) published_sample=true ;;
    *) package_directory="$argument" ;;
  esac
done
package_directory="${package_directory:-artifacts/package}"

fail() {
  echo "error: $*" >&2
  exit 1
}

repository="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
templates="$repository/eng/consumer-smoke"

packages=("$package_directory"/ConsoleCalendarGenerator.*.nupkg)
[ "${#packages[@]}" -eq 1 ] || fail "expected exactly one ConsoleCalendarGenerator .nupkg in $package_directory, found ${#packages[@]}"
package="${packages[0]}"
version="$(basename "$package" .nupkg)"
version="${version#ConsoleCalendarGenerator.}"
[ "$version" != "1.0.0" ] || fail "refusing to test a package numbered like the published 1.0.0"

work="$(mktemp -d)"
if command -v cygpath > /dev/null; then
  # Git Bash on Windows: use a path that both bash and dotnet understand.
  work="$(cygpath -m "$work")"
fi
cleanup() {
  rm -rf "$work" || true
}
trap cleanup EXIT

# Keep build processes from staying alive and holding the temporary folder open.
export MSBUILDDISABLENODEREUSE=1
export DOTNET_CLI_USE_MSBUILD_SERVER=0
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
unset NUGET_PACKAGES

mkdir "$work/feed"
cp "$package" "$work/feed/"
cp "$repository/global.json" "$work/"
# Stop the applications from picking up build settings from any folder above the temporary one.
echo '<Project />' > "$work/Directory.Build.props"
echo '<Project />' > "$work/Directory.Build.targets"
cat > "$work/nuget.config" << 'CONFIG'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <config>
    <add key="globalPackagesFolder" value="./packages" />
  </config>
  <fallbackPackageFolders>
    <clear />
  </fallbackPackageFolders>
  <packageSources>
    <clear />
    <add key="package-under-test" value="./feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="package-under-test">
      <package pattern="ConsoleCalendarGenerator" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
CONFIG

echo "Package under test: ConsoleCalendarGenerator $version"

# build_probe <application target framework> -> builds eng/consumer-smoke/AssetProbe for it, in its own folder
build_probe() {
  local framework="$1"
  local project="$work/probe-$framework"
  cp -R "$templates/AssetProbe" "$project"
  dotnet build "$project/AssetProbe.csproj" --configuration Release \
    -p:ConsumerTargetFramework="$framework" -p:PackageUnderTestVersion="$version" \
    -p:UseSharedCompilation=false -nodeReuse:false -warnaserror > "$project/build.log" 2>&1 \
    || { cat "$project/build.log"; fail "the $framework application did not build against the package"; }
}

# check_output <what> <expected library build> <actual output>
check_output() {
  local expected
  # The version the library must say it was built as is the version of the package that was installed, so an
  # application that was given any other package, such as a cached or a published one, fails here.
  expected="$(sed -e "s/{LIBRARY_BUILD}/$2/" -e "s/{PACKAGE_VERSION}/$version/" "$templates/AssetProbe/expected-output.txt" | tr -d '\r')"
  if [ "$3" != "$expected" ]; then
    echo "Expected:" >&2
    echo "$expected" >&2
    echo "Actual:" >&2
    echo "$3" >&2
    fail "$1 did not produce the expected output"
  fi
}

# run_probe <application target framework> <library build it must get>
run_probe() {
  local framework="$1"
  local expected_build="$2"
  build_probe "$framework"
  local output
  output="$(dotnet "$work/probe-$framework/bin/Release/$framework/AssetProbe.dll" | tr -d '\r')" \
    || fail "the $framework application did not run"
  check_output "the $framework application" "$expected_build" "$output"
  echo "ok: a $framework application gets the $expected_build build of $version and draws the expected calendar"
}

run_probe net10.0 ".NETCoreApp,Version=v10.0"
# net8.0 predates net10.0, so it must fall back to the netstandard2.1 build, as every existing consumer does.
run_probe net8.0 ".NETStandard,Version=v2.1"
# A newer application, such as one on net11.0, must be given the net10.0 build.
run_probe net11.0 ".NETCoreApp,Version=v10.0"

# The package must have come from the local feed, into the temporary cache.
metadata="$work/packages/consolecalendargenerator/$version/.nupkg.metadata"
[ -f "$metadata" ] || fail "the package was not restored into the temporary package cache"
grep -q -E '[/\\]feed"' "$metadata" || fail "the package was not restored from the local feed: $(cat "$metadata")"
echo "ok: the package was restored from the local feed only"

# The sample program that shipped with 1.0.0 must compile unchanged.
cp -R "$templates/OriginalSample" "$work/original-sample"
dotnet build "$work/original-sample/OriginalSample.csproj" --configuration Release \
  -p:PackageUnderTestVersion="$version" -p:UseSharedCompilation=false -nodeReuse:false -warnaserror \
  > "$work/original-sample/build.log" 2>&1 \
  || { cat "$work/original-sample/build.log"; fail "the 1.0.0 sample program no longer compiles against the package"; }
echo "ok: the 1.0.0 sample program compiles unchanged"

# ---- The original sample, run ----
#
# One exact scenario, so that two runs can be compared and a number means something:
#
#   program    eng/consumer-smoke/OriginalSample/Program.cs, the sample of 1.0.0 byte for byte, built for net10.0
#              in Release and started as "dotnet OriginalSample.dll" (RollForward LatestMajor: it runs on the
#              newest runtime installed)
#   terminal   a pseudo-terminal made by util-linux "script -qec <command> /dev/null": standard input, output
#              and error of the program are all that terminal, and what the terminal receives is captured
#   settings   TERM=xterm-256color, LC_ALL=C and LANG=C (so the current culture is the invariant culture and the
#              month names are English), NO_COLOR not set, terminal echo off ("stty -echo") before the program
#              starts
#   input      the four key presses the program waits for, each the letter x, written to the terminal after
#              echo is off; nothing else
#   captured   "raw": every byte the terminal received. That is the program's text with "\r\n" line ends (the
#              terminal turns "\n" into "\r\n"), the escape sequences the .NET runtime writes for colors and for
#              the keypad, the form feed and the two escape sequences the program writes itself to clear the
#              screen, and one x after each prompt, which the runtime echoes when it reads a key. The terminal
#              echoes nothing, and "script -q" adds nothing of its own.
#              "text": the raw bytes without escape sequences and without carriage returns.
#
# The text is what is compared with a file, and there is one file for each version, because 2.0 corrects what
# 1.0.0 wrote:
#
#   expected-text-1.0.0.txt   what the program writes with the published 1.0.0. It is historical and never changes.
#   expected-text-2.0.txt     what it writes with the package under test.
#
# The two differ in one way: 1.0.0 wrote two blank lines after a month whose last day is a Sunday, and 2.0 writes
# one, as after every month (D-60). This script checks that this is the whole difference between the two files.
# The other corrections do not show in the text: other colors for the calendars made with constructor arguments
# (D-57) are escape sequences; the last row of a month ending with the console's line terminator instead of a
# literal line feed (D-59) is the same byte on this terminal, whose terminator is a line feed; and the program
# passes no null list (D-58).
#
# The raw bytes depend on the runtime and on the terminal database, so they are compared between two runs on the
# same machine and never with a recorded value.

# sample_run <application dll> <file for the raw bytes>
sample_run() {
  local ready="$work/sample-ready"
  rm -f "$ready"
  (
    # Wait until echo is off, so that the terminal itself never echoes a key, whatever the timing.
    for _ in $(seq 1 600); do
      [ -e "$ready" ] && break
      sleep 0.05
    done
    printf 'xxxx'
  ) | env -u NO_COLOR TERM=xterm-256color LC_ALL=C LANG=C \
      timeout 120 script -qec "stty -echo; : > '$ready'; exec dotnet '$1'" /dev/null > "$2" \
    || fail "the 1.0.0 sample program did not run to its end on a pseudo-terminal: $1"
}

# sample_text <raw file> -> the text: no escape sequences, no carriage returns
sample_text() {
  LC_ALL=C sed -E $'s#\x1b\\[[0-?]*[ -/]*[@-~]##g; s#\x1b[=>c]##g' "$1" | tr -d '\r'
}

# sample_without_colors <raw file> -> the raw bytes without the sequences that set or reset a color
sample_without_colors() {
  LC_ALL=C sed -E $'s#\x1b\\[[0-9;]*m##g' "$1"
}

sha256_of() {
  sha256sum "$1" | cut -d ' ' -f 1
}

expected_text_100="$templates/OriginalSample/expected-text-1.0.0.txt"
expected_text_20="$templates/OriginalSample/expected-text-2.0.txt"

# sample_check_text <what> <raw file> <file with the expected text>
sample_check_text() {
  if sample_text "$2" | LC_ALL=C grep -q $'\x1b'; then
    fail "$1 wrote an escape sequence this script does not know how to remove"
  fi
  if ! sample_text "$2" | cmp -s - "$3"; then
    sample_text "$2" | diff "$3" - | head -20 >&2 || true
    fail "$1 does not write the text in eng/consumer-smoke/OriginalSample/$(basename "$3")"
  fi
}

# with_one_blank_line_after_every_month < text of 1.0.0 > that text as 2.0 writes it (D-60)
#
# The last row of a month that ends on a Sunday is a full week: twenty characters of digits and spaces. In 1.0.0
# two blank lines follow such a row, in 2.0 one. A full week inside a month is followed by the next row, not by
# a blank line, so the first blank line directly after a full week is the one 2.0 does not write.
with_one_blank_line_after_every_month() {
  awk '
    after_a_full_week && $0 == "" { after_a_full_week = 0; next }
    { after_a_full_week = (length($0) == 20 && $0 ~ /^[ 0-9]*$/) }
    { print }'
}

# with_one_blank_line_after_every_month_raw <raw file of 1.0.0> -> those raw bytes as 2.0 writes them (D-60)
#
# In the raw bytes a month ends with its line ends followed by the sequence that resets the colors. Three line
# ends before it are the month that ends on a Sunday; every other month has two.
with_one_blank_line_after_every_month_raw() {
  LC_ALL=C perl -0pe 's/\r\n\r\n\r\n(\x1b\[39;49m)/\r\n\r\n$1/g' "$1"
}

if [ "$(uname -s)" = "Linux" ] && command -v script > /dev/null && command -v stty > /dev/null; then
  under_test="$work/original-sample/bin/Release/net10.0"
  sample_run "$under_test/OriginalSample.dll" "$work/sample-under-test.raw"
  sample_check_text "the 1.0.0 sample program, with the package under test," "$work/sample-under-test.raw" "$expected_text_20"
  echo "ok: the 1.0.0 sample program, run on a pseudo-terminal with the package under test, writes the text expected of 2.0"

  # The two expected texts differ by the one blank line after each month that ends on a Sunday, and by nothing else.
  with_one_blank_line_after_every_month < "$expected_text_100" > "$work/expected-text-1.0.0-corrected.txt"
  awk '{ print }' "$expected_text_20" > "$work/expected-text-2.0-lines.txt"
  cmp -s "$work/expected-text-1.0.0-corrected.txt" "$work/expected-text-2.0-lines.txt" \
    || fail "expected-text-2.0.txt is not expected-text-1.0.0.txt with one blank line after every month"
  blank_lines_removed=$(( $(awk 'END { print NR }' "$expected_text_100") - $(awk 'END { print NR }' "$expected_text_20") ))
  [ "$blank_lines_removed" -gt 0 ] || fail "the two expected texts do not differ, although the program draws months that end on a Sunday"
  echo "ok: the text expected of 2.0 is the text of 1.0.0 without the second blank line after the $blank_lines_removed months drawn that end on a Sunday"

  if [ "$published_sample" = true ]; then
    # The same program against the published 1.0.0, in a folder of its own with nuget.org as its only source.
    published="$work/published"
    mkdir "$published"
    cp "$repository/global.json" "$published/"
    echo '<Project />' > "$published/Directory.Build.props"
    echo '<Project />' > "$published/Directory.Build.targets"
    cat > "$published/nuget.config" << 'CONFIG'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <config>
    <add key="globalPackagesFolder" value="./packages" />
  </config>
  <fallbackPackageFolders>
    <clear />
  </fallbackPackageFolders>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <clear />
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
CONFIG
    cp -R "$templates/OriginalSample" "$published/original-sample"
    dotnet build "$published/original-sample/OriginalSample.csproj" --configuration Release \
      -p:PackageUnderTestVersion=1.0.0 -p:UseSharedCompilation=false -nodeReuse:false -warnaserror \
      > "$published/build.log" 2>&1 \
      || { cat "$published/build.log"; fail "the 1.0.0 sample program did not build against the published 1.0.0"; }
    compiled_for_100="$published/original-sample/bin/Release/net10.0"
    published_metadata="$published/packages/consolecalendargenerator/1.0.0/.nupkg.metadata"
    grep -q 'api.nuget.org' "$published_metadata" || fail "the published 1.0.0 was not restored from nuget.org: $(cat "$published_metadata")"

    sample_run "$compiled_for_100/OriginalSample.dll" "$work/sample-published.raw"
    sample_check_text "the 1.0.0 sample program, with the published 1.0.0," "$work/sample-published.raw" "$expected_text_100"
    echo "ok: with the published 1.0.0 it writes the text recorded for 1.0.0"

    # Raw bytes. The two runs differ in what 2.0 corrects and in nothing else: after the second blank line of
    # the months that end on a Sunday is taken out of what 1.0.0 wrote (D-60), the two are the same bytes except
    # inside the sequences that set a color (D-57).
    echo "    raw bytes, published 1.0.0:    $(wc -c < "$work/sample-published.raw" | tr -d ' ') bytes, SHA-256 $(sha256_of "$work/sample-published.raw")"
    echo "    raw bytes, package under test: $(wc -c < "$work/sample-under-test.raw" | tr -d ' ') bytes, SHA-256 $(sha256_of "$work/sample-under-test.raw")"
    echo "    text, published 1.0.0:         $(sample_text "$work/sample-published.raw" | wc -c | tr -d ' ') bytes, SHA-256 $(sample_text "$work/sample-published.raw" | sha256sum | cut -d ' ' -f 1)"
    echo "    text, package under test:      $(sample_text "$work/sample-under-test.raw" | wc -c | tr -d ' ') bytes, SHA-256 $(sample_text "$work/sample-under-test.raw" | sha256sum | cut -d ' ' -f 1)"
    with_one_blank_line_after_every_month_raw "$work/sample-published.raw" > "$work/sample-published.corrected"
    sample_without_colors "$work/sample-published.corrected" > "$work/sample-published.uncolored"
    sample_without_colors "$work/sample-under-test.raw" > "$work/sample-under-test.uncolored"
    cmp -s "$work/sample-published.uncolored" "$work/sample-under-test.uncolored" \
      || fail "the raw terminal bytes of the 1.0.0 sample program differ from the published 1.0.0 in more than the blank line after a Sunday and color sequences"
    [ "$(wc -c < "$work/sample-published.corrected")" -eq "$(wc -c < "$work/sample-under-test.raw")" ] \
      || fail "the raw terminal bytes of the 1.0.0 sample program have another length than the published 1.0.0 without its second blank lines"
    echo "ok: the raw terminal bytes differ from the published 1.0.0 in the blank line after a Sunday and in color sequences only"

    # Binary compatibility: the program as compiled against 1.0.0, not rebuilt, with the library under test in
    # place of the published assembly. It is the netstandard2.1 build, which is the one 1.0.0 has.
    substituted="$work/substituted"
    cp -R "$compiled_for_100" "$substituted"
    cmp -s "$substituted/ConsoleCalendarGenerator.dll" "$published/packages/consolecalendargenerator/1.0.0/lib/netstandard2.1/ConsoleCalendarGenerator.dll" \
      || fail "the program compiled against 1.0.0 does not carry the published assembly"
    unzip -p "$package" lib/netstandard2.1/ConsoleCalendarGenerator.dll > "$substituted/ConsoleCalendarGenerator.dll"
    cmp -s "$substituted/OriginalSample.dll" "$compiled_for_100/OriginalSample.dll" || fail "the substituted program was rebuilt"
    sample_run "$substituted/OriginalSample.dll" "$work/sample-substituted.raw"
    sample_check_text "the program compiled against 1.0.0, running the library under test," "$work/sample-substituted.raw" "$expected_text_20"
    cmp -s "$work/sample-substituted.raw" "$work/sample-under-test.raw" \
      || fail "the program compiled against 1.0.0 draws differently with the library under test than the program compiled against it"
    echo "ok: the program compiled against 1.0.0 runs unchanged with the library under test, byte for byte like the rebuilt one"
  fi
elif [ "$published_sample" = true ]; then
  fail "--published-sample needs Linux and the util-linux 'script' program"
else
  echo "skipped: running the 1.0.0 sample program needs Linux and the util-linux 'script' program"
fi

if [ "$native_aot" = true ]; then
  project="$work/native-aot"
  cp -R "$templates/AssetProbe" "$project"
  dotnet publish "$project/AssetProbe.csproj" --configuration Release --output "$project/published" \
    -p:ConsumerTargetFramework=net10.0 -p:PackageUnderTestVersion="$version" -p:PublishAot=true \
    -p:UseSharedCompilation=false -nodeReuse:false -warnaserror > "$project/publish.log" 2>&1 \
    || { cat "$project/publish.log"; fail "the Native AOT publish failed or reported trim or AOT warnings"; }
  executable="$project/published/AssetProbe"
  [ -f "$executable" ] || executable="$project/published/AssetProbe.exe"
  output="$("$executable" | tr -d '\r')" || fail "the Native AOT application did not run"
  check_output "the Native AOT application" ".NETCoreApp,Version=v10.0" "$output"
  echo "ok: a Native AOT application publishes without warnings and draws the expected calendar"
fi

echo "All consumer checks passed."
