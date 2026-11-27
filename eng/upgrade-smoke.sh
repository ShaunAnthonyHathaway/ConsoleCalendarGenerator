#!/usr/bin/env bash
#
# Upgrades a program from the published ConsoleCalendarGenerator 1.0.0 to the package under test, the way a
# user does it, and checks what changes and what does not.
#
#   eng/upgrade-smoke.sh artifacts/package
#
# For a net8.0 program and for a net10.0 program, in a temporary folder outside the repository:
#   1. The program (eng/consumer-smoke/MigrationProbe) references ConsoleCalendarGenerator 1.0.0, restored from
#      nuget.org. It builds, runs and reports the behavior of 1.0.0 (expected-1.0.0.txt).
#   2. Code that uses the modern API does not compile against 1.0.0: that API is not in it.
#   3. The package reference is upgraded to the package under test with "dotnet add package", from a local feed
#      that is the only source allowed for this package. Nothing else about the program changes.
#   4. It builds again without a change to its source, and with the code that uses the modern API. It runs and
#      reports the behavior of 2.0: the five corrections, and everything else as before (expected-2.0.txt).
#   5. The assembly it then runs with is, byte for byte, the build in the package meant for its framework:
#      netstandard2.1 for net8.0, net10.0 for net10.0.
#
# NuGet's package cache is a folder inside the temporary one, so no package cached on the machine can stand in
# for either version. This is the only script besides "consumer-smoke.sh --published-sample" that takes
# ConsoleCalendarGenerator from nuget.org, and it takes 1.0.0 only.
#
# This is a package upgrade. Running the sample of 1.0.0 and putting the new assembly under a program that is
# not rebuilt are separate checks, in eng/consumer-smoke.sh.

set -euo pipefail
shopt -s nullglob
# shellcheck source=eng/common.sh
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"

package_directory="${1:-artifacts/package}"
packages=("$package_directory"/ConsoleCalendarGenerator.*.nupkg)
[ "${#packages[@]}" -eq 1 ] || fail "expected exactly one ConsoleCalendarGenerator .nupkg in $package_directory, found ${#packages[@]}"
package="${packages[0]}"
version="$(basename "$package" .nupkg)"
version="${version#ConsoleCalendarGenerator.}"
[ "$version" != "1.0.0" ] || fail "refusing to test a package numbered like the published 1.0.0"
template="$repository/eng/consumer-smoke/MigrationProbe"

new_work_folder
quiet_dotnet
unset NUGET_PACKAGES
mkdir "$work/feed"
cp "$package" "$work/feed/"

# nuget_config <folder> <source of ConsoleCalendarGenerator: nuget.org or package-under-test>
nuget_config() {
  local for_the_library='<package pattern="ConsoleCalendarGenerator" />'
  local from_nuget_org='' from_the_feed=''
  if [ "$2" = "nuget.org" ]; then from_nuget_org="$for_the_library"; else from_the_feed="$for_the_library"; fi
  cat > "$1/nuget.config" << CONFIG
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
    <add key="package-under-test" value="$work/feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="package-under-test">
      <package pattern="No.Such.Package.Placeholder" />
      $from_the_feed
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
      $from_nuget_org
    </packageSource>
  </packageSourceMapping>
</configuration>
CONFIG
}

# expect_output <what> <expected file> <library line> <actual output>
expect_output() {
  local expected
  expected="$(sed -e "s/{LIBRARY}/$3/" "$2" | tr -d '\r')"
  if [ "$4" != "$expected" ]; then
    echo "Expected:" >&2
    echo "$expected" >&2
    echo "Actual:" >&2
    echo "$4" >&2
    fail "$1 did not report what $(basename "$2") expects"
  fi
}

# upgrade <framework of the program> <build of the library it must get after the upgrade> <what that build says it is>
upgrade() {
  local framework="$1" asset="$2" asset_name="$3"
  local consumer="$work/upgrade-$framework"
  mkdir "$consumer"
  isolate_from_the_repository "$consumer"
  cp "$template/Program.cs" "$template/Modern.cs" "$template/PackageConsumer.csproj" "$consumer/"
  local project="$consumer/PackageConsumer.csproj"
  local program="$consumer/bin/Release/$framework/PackageConsumer.dll"
  local cached="$consumer/packages/consolecalendargenerator"
  # "dotnet add package" reads the project too, and takes the framework from the environment.
  export ConsumerTargetFramework="$framework"

  # ---- 1. as published ----
  nuget_config "$consumer" nuget.org
  grep -q '<PackageReference Include="ConsoleCalendarGenerator" Version="1.0.0" />' "$project" \
    || fail "the program does not start from a reference to 1.0.0"
  dotnet build "$project" --configuration Release -p:UseSharedCompilation=false -nodeReuse:false > "$consumer/build-1.log" 2>&1 \
    || { cat "$consumer/build-1.log"; fail "the $framework program did not build against the published 1.0.0"; }
  grep -q 'api.nuget.org' "$cached/1.0.0/.nupkg.metadata" || fail "1.0.0 was not restored from nuget.org: $(cat "$cached/1.0.0/.nupkg.metadata")"
  [ "$(ls "$cached")" = "1.0.0" ] || fail "more than the published 1.0.0 was restored: $(ls "$cached")"
  local output
  output="$(dotnet "$program" | tr -d '\r')" || fail "the $framework program did not run with 1.0.0"
  expect_output "the $framework program with 1.0.0" "$template/expected-1.0.0.txt" ".NETStandard,Version=v2.1, assembly version 1.0.0.0" "$output"
  echo "ok: a $framework program builds against the published 1.0.0 and reports the behavior of 1.0.0"

  # ---- 2. the modern API is not in 1.0.0 ----
  if dotnet build "$project" --configuration Release -p:UseModernApi=true -p:UseSharedCompilation=false -nodeReuse:false > "$consumer/build-modern-1.log" 2>&1; then
    fail "code that uses the modern API compiled against 1.0.0"
  fi
  grep -q -E "error CS0246.*'ConsoleCalendarGenerator'|error CS0234.*'ConsoleCalendarGenerator'" "$consumer/build-modern-1.log" \
    || { cat "$consumer/build-modern-1.log"; fail "the build against 1.0.0 failed, but not because the namespace ConsoleCalendarGenerator is missing"; }
  echo "ok: with 1.0.0, code that uses the modern API does not compile: the namespace is not there"

  # ---- 3. the upgrade ----
  nuget_config "$consumer" package-under-test
  dotnet add "$project" package ConsoleCalendarGenerator --version "$version" > "$consumer/upgrade.log" 2>&1 \
    || { cat "$consumer/upgrade.log"; fail "the package reference of the $framework program could not be upgraded to $version"; }
  grep -q "<PackageReference Include=\"ConsoleCalendarGenerator\" Version=\"$version\" />" "$project" \
    || { cat "$project"; fail "after the upgrade the program does not reference $version"; }
  if grep -q 'Version="1.0.0"' "$project"; then fail "after the upgrade the program still references 1.0.0"; fi
  cmp -s "$template/Program.cs" "$consumer/Program.cs" || fail "the source of the program changed"

  # ---- 4. the same source, and the modern API ----
  dotnet build "$project" --configuration Release -p:UseModernApi=true -p:UseSharedCompilation=false -nodeReuse:false -warnaserror > "$consumer/build-2.log" 2>&1 \
    || { cat "$consumer/build-2.log"; fail "the $framework program did not build against $version"; }
  grep -q -E '[/\\]feed"' "$cached/$version/.nupkg.metadata" || fail "$version was not restored from the local feed: $(cat "$cached/$version/.nupkg.metadata")"
  output="$(dotnet "$program" | tr -d '\r')" || fail "the $framework program did not run with $version"
  expect_output "the $framework program with $version" "$template/expected-2.0.txt" "$asset_name, assembly version 2.0.0.0" "$output"

  # ---- 5. the build meant for the framework ----
  unzip -p "$package" "lib/$asset/ConsoleCalendarGenerator.dll" | cmp -s - "$consumer/bin/Release/$framework/ConsoleCalendarGenerator.dll" \
    || fail "the $framework program does not run with the $asset build of the package"
  echo "ok: upgraded to $version from the local feed, the same source builds, gets the $asset build, and reports the five corrections and nothing else changed; the modern API is there"
}

# The two expected reports say what the upgrade is: everything under "[unchanged]" is the same in both, and
# each of the five numbered lines under "[corrected in 2.0]" is different. A change to either file that blurs
# that fails here, before anything is built.
section() {
  # section <file> <heading> -> the lines from the heading to the next empty line
  tr -d '\r' < "$1" | awk -v heading="$2" '$0 == heading { inside = 1 } inside && $0 == "" { exit } inside { print }'
}
[ -n "$(section "$template/expected-1.0.0.txt" "[unchanged]")" ] || fail "expected-1.0.0.txt has no [unchanged] section"
[ "$(section "$template/expected-1.0.0.txt" "[unchanged]")" = "$(section "$template/expected-2.0.txt" "[unchanged]")" ] \
  || fail "the [unchanged] sections of expected-1.0.0.txt and expected-2.0.txt differ"
for number in 1 2 3 4 5; do
  before="$(section "$template/expected-1.0.0.txt" "[corrected in 2.0]" | grep "^$number\. ")" || fail "expected-1.0.0.txt has no line for correction $number"
  after="$(section "$template/expected-2.0.txt" "[corrected in 2.0]" | grep "^$number\. ")" || fail "expected-2.0.txt has no line for correction $number"
  [ "$before" != "$after" ] || fail "correction $number reads the same in both expected reports: $before"
done
[ "$(section "$template/expected-2.0.txt" "[corrected in 2.0]" | grep -c '^[0-9]\. ')" -eq 5 ] || fail "expected-2.0.txt does not list exactly five corrections"
if grep -q '\[new in 2.0\]' "$template/expected-1.0.0.txt"; then fail "expected-1.0.0.txt has the section of the modern API"; fi
grep -q '\[new in 2.0\]' "$template/expected-2.0.txt" || fail "expected-2.0.txt has no section of the modern API"

echo "Package under test: ConsoleCalendarGenerator $version"
# An existing program: it used the netstandard2.1 build of 1.0.0 and keeps getting that build.
upgrade net8.0 netstandard2.1 ".NETStandard,Version=v2.1"
# A program on a current framework: 1.0.0 gave it the netstandard2.1 build, and 2.0 gives it the net10.0 build.
upgrade net10.0 net10.0 ".NETCoreApp,Version=v10.0"
echo "The upgrade from 1.0.0 is as expected."
