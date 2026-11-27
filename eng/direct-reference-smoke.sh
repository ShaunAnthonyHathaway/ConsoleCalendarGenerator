#!/usr/bin/env bash
#
# Uses the library without NuGet, from the binaries archive of a release bundle: a program references one
# assembly by its path, builds, runs, and reports what it observes.
#
#   eng/direct-reference-smoke.sh artifacts/release-bundle
#
# In a temporary folder outside the repository, the archive is unpacked and the program
# (eng/consumer-smoke/MigrationProbe) is built twice:
#   - a net10.0 program against net10.0/ConsoleCalendarGenerator.dll;
#   - a net8.0 program against netstandard2.1/ConsoleCalendarGenerator.dll.
# Each has no package reference to ConsoleCalendarGenerator, and NuGet is given no source that has it. Each must
# run with exactly the assembly from the archive, find the documentation and the symbols beside it, use the
# legacy class and the modern API, and report the behavior of 2.0 (expected-2.0.txt), the five corrections
# included.
#
# Native AOT is tested through the package, in eng/consumer-smoke.sh, not here.

set -euo pipefail
shopt -s nullglob
# shellcheck source=eng/common.sh
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"

bundle="${1:-artifacts/release-bundle}"
archives=("$bundle"/ConsoleCalendarGenerator-*-binaries.zip)
[ "${#archives[@]}" -eq 1 ] || fail "expected exactly one binaries archive in $bundle, found ${#archives[@]}"
archive="${archives[0]}"
version="$(basename "$archive" -binaries.zip)"
version="${version#ConsoleCalendarGenerator-}"
template="$repository/eng/consumer-smoke/MigrationProbe"

new_work_folder
quiet_dotnet
unset NUGET_PACKAGES
unzip -q "$archive" -d "$work/binaries"
unpacked="$work/binaries/ConsoleCalendarGenerator-$version"
[ -d "$unpacked" ] || fail "the archive does not unpack to a folder ConsoleCalendarGenerator-$version"

# direct <framework of the program> <folder of the archive> <what that build says it is>
direct() {
  local framework="$1" build="$2" build_name="$3"
  local consumer="$work/direct-$framework"
  mkdir "$consumer"
  isolate_from_the_repository "$consumer"
  cp "$template/Program.cs" "$template/Modern.cs" "$template/DirectConsumer.csproj" "$consumer/"
  # nuget.org is there for what the SDK itself may need to build for a framework; the library is not on offer.
  cat > "$consumer/nuget.config" << 'CONFIG'
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
    <packageSource key="nuget.org">
      <package pattern="Microsoft.*" />
      <package pattern="runtime.*" />
      <package pattern="NETStandard.*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
CONFIG
  for extension in dll xml pdb; do
    [ -s "$unpacked/$build/ConsoleCalendarGenerator.$extension" ] || fail "the archive has no $build/ConsoleCalendarGenerator.$extension"
  done
  if grep -q -i 'PackageReference' "$consumer/DirectConsumer.csproj"; then fail "the program has a package reference"; fi

  dotnet build "$consumer/DirectConsumer.csproj" --configuration Release \
    -p:ConsumerTargetFramework="$framework" -p:LibraryAssemblyPath="$unpacked/$build/ConsoleCalendarGenerator.dll" -p:UseModernApi=true \
    -p:UseSharedCompilation=false -nodeReuse:false -warnaserror > "$consumer/build.log" 2>&1 \
    || { cat "$consumer/build.log"; fail "the $framework program did not build against $build/ConsoleCalendarGenerator.dll"; }

  # No package of that name took part: not in what NuGet resolved, and not in its cache folder.
  if grep -q -i '"consolecalendargenerator/' "$consumer/obj/project.assets.json"; then fail "NuGet resolved a ConsoleCalendarGenerator package for the program"; fi
  [ ! -e "$consumer/packages/consolecalendargenerator" ] || fail "a ConsoleCalendarGenerator package was restored"

  local out="$consumer/bin/Release/$framework"
  cmp -s "$unpacked/$build/ConsoleCalendarGenerator.dll" "$out/ConsoleCalendarGenerator.dll" || fail "the program does not run with the assembly from the archive"
  # The build finds the symbols and the documentation because they lie beside the assembly, and copies both.
  cmp -s "$unpacked/$build/ConsoleCalendarGenerator.pdb" "$out/ConsoleCalendarGenerator.pdb" || fail "the symbols beside the assembly were not picked up"
  cmp -s "$unpacked/$build/ConsoleCalendarGenerator.xml" "$out/ConsoleCalendarGenerator.xml" || fail "the documentation beside the assembly was not picked up"

  local output expected
  output="$(dotnet "$out/DirectConsumer.dll" | tr -d '\r')" || fail "the $framework program did not run"
  expected="$(sed -e "s/{LIBRARY}/$build_name, assembly version 2.0.0.0/" "$template/expected-2.0.txt" | tr -d '\r')"
  if [ "$output" != "$expected" ]; then
    echo "Expected:" >&2
    echo "$expected" >&2
    echo "Actual:" >&2
    echo "$output" >&2
    fail "the $framework program did not report what expected-2.0.txt expects"
  fi
  echo "ok: a $framework program that references $build/ConsoleCalendarGenerator.dll directly, with no package, runs with that assembly, uses both APIs and reports the five corrections"
}

echo "Binaries under test: $(basename "$archive")"
direct net10.0 net10.0 ".NETCoreApp,Version=v10.0"
direct net8.0 netstandard2.1 ".NETStandard,Version=v2.1"
echo "Direct use of the assemblies is as expected."
