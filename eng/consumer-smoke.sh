#!/usr/bin/env bash
#
# Installs the locally packed ConsoleCalendarGenerator package into small, separate applications, the way a
# consumer would, and checks that they build, run and get the library build meant for them.
#
#   dotnet pack ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj --configuration Release --output artifacts/package
#   eng/consumer-smoke.sh artifacts/package [--native-aot]
#
# Everything happens in a temporary folder outside the repository, which is deleted afterwards:
#   - the package is copied to a local feed there, and NuGet is told to take ConsoleCalendarGenerator from that
#     feed only, never from nuget.org;
#   - NuGet's package cache is a folder there too, so the development package never enters the machine's cache.
#
# --native-aot also publishes an application with Native AOT against the package and runs it. That needs the
# platform's native build tools (for example clang on Linux).

set -euo pipefail
shopt -s nullglob

package_directory=""
native_aot=false
for argument in "$@"; do
  case "$argument" in
    --native-aot) native_aot=true ;;
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
