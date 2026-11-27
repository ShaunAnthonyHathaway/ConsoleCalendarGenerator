#!/usr/bin/env bash
#
# After a version is published: checks what nuget.org serves against the candidate that was pushed. It reads
# nuget.org and needs no credential. It publishes nothing.
#
#   eng/verify-published-package.sh <folder with the candidate bundle>
#
# The candidate is the authority. Its four files must still pass eng/verify-release-bundle.sh, checksums
# included. Then:
#
#   1. The package is downloaded from nuget.org, and nuget.org's own record of it is read: the version is
#      listed, and the size and the SHA-512 it records are those of the file that was downloaded.
#   2. nuget.org signs every package it serves, by adding one entry, ".signature.p7s". So the served file is
#      NOT the file that was pushed, and its checksum is NOT the one in SHA256SUMS.txt; that is expected and is
#      said, not hidden. What is required: the served package has that entry, has every entry of the candidate
#      byte for byte, the manifest included, and has no other entry.
#   3. The served package passes the checks of a published package (eng/verify-package.sh --published):
#      identity, version, metadata, license, no dependency, both builds with their documentation, the readme,
#      the commit, the versions the assemblies carry.
#   4. The symbol package cannot be downloaded back from nuget.org, and nothing here pretends it can. What
#      nuget.org does serve is each symbol file, from its symbol server, to anyone who has the assembly. Each
#      is fetched with the identity its assembly records and must be the candidate's symbol file, byte for
#      byte. The symbol package itself stays protected by SHA256SUMS.txt and by the release it is attached to.
#   5. A program installs the version from nuget.org alone, builds, uses both APIs and reports the behavior of
#      the release; and the consumer smoke tests and the upgrade from 1.0.0 run against the served package.
#
# With --served <package file>, that file stands for the one nuget.org serves and nothing is downloaded: steps
# 1, 4 and 5 are left out. The self-test uses this.
#
#   eng/verify-published-package.sh --compare <package that was pushed> <package as served>
#
# makes only the comparison of step 2, for any two files.

set -euo pipefail
shopt -s nullglob
# shellcheck source=eng/common.sh
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"

if [ "${1:-}" = "--compare" ]; then
  [ "$#" -eq 3 ] || fail "usage: eng/verify-published-package.sh --compare <package that was pushed> <package as served>"
  compare_packages "$2" "$3"
  exit 0
fi

bundle="${1:-}"
served_file=""
if [ "$#" -eq 3 ] && [ "$2" = "--served" ]; then
  served_file="$3"
elif [ "$#" -ne 1 ]; then
  fail "usage: eng/verify-published-package.sh <folder with the candidate bundle> [--served <package file>]"
fi
[ -d "$bundle" ] || fail "there is no folder $bundle"

echo "== The candidate"
bash "$repository/eng/verify-release-bundle.sh" "$bundle" > /dev/null || fail "the bundle in $bundle does not pass its own check, so it is not the candidate"
packages=("$bundle"/ConsoleCalendarGenerator.*.nupkg)
candidate="${packages[0]}"
version="$(basename "$candidate" .nupkg)"
version="${version#ConsoleCalendarGenerator.}"
symbol_package="$bundle/$(symbols_name "$version")"
echo "Candidate:      ConsoleCalendarGenerator $version, SHA-256 $(sha256_of "$candidate")"

new_work_folder
quiet_dotnet
unset NUGET_PACKAGES
template="$repository/eng/consumer-smoke/MigrationProbe"

if [ -z "$served_file" ]; then
  echo "== 1. What nuget.org serves, and its record of it"
  served="$work/served/$(package_name "$version")"
  mkdir "$work/served"
  status="$(curl -s -L -o "$served" -w '%{http_code}' "$nuget_packages/consolecalendargenerator/$version/consolecalendargenerator.$version.nupkg")"
  [ "$status" = "200" ] || fail "nuget.org does not serve ConsoleCalendarGenerator $version (HTTP $status). It is not published, or not indexed yet."
  leaf="$(curl -s -L --compressed "$nuget_registration/consolecalendargenerator/$version.json" | tr -d '\r\n')"
  grep -q '"listed": *true' <<< "$leaf" || fail "nuget.org does not list ConsoleCalendarGenerator $version"
  catalog="$(grep -o '"catalogEntry": *"[^"]*"' <<< "$leaf" | sed -E 's/.*"(https[^"]*)"/\1/')"
  [ -n "$catalog" ] || fail "nuget.org's record of the version names no catalog entry"
  record="$(curl -s -L --compressed "$catalog" | tr -d '\r\n')"
  recorded_size="$(grep -o '"packageSize": *[0-9]*' <<< "$record" | grep -o '[0-9]*$')"
  recorded_hash="$(grep -o '"packageHash": *"[^"]*"' <<< "$record" | sed -E 's/.*: *"([^"]*)"/\1/')"
  grep -q '"packageHashAlgorithm": *"SHA512"' <<< "$record" || fail "nuget.org records the package with another checksum than SHA-512"
  [ "$recorded_size" = "$(size_of "$served")" ] || fail "nuget.org records a package of $recorded_size bytes, and the download has $(size_of "$served")"
  served_hash="$(openssl dgst -sha512 -binary "$served" | openssl base64 -A)"
  [ "$recorded_hash" = "$served_hash" ] || fail "the download does not have the SHA-512 that nuget.org records for the package"
  echo "Served:         $(size_of "$served") bytes, with the size and the SHA-512 that nuget.org records; listed"
else
  [ -f "$served_file" ] || fail "there is no file $served_file"
  served="$served_file"
  echo "== 1. A file that stands for what nuget.org serves (nothing is downloaded)"
fi

echo "== 2. The served package against the candidate"
if cmp -s "$candidate" "$served"; then
  fail "the served file is the candidate file, byte for byte, which nuget.org never serves: it signs every package"
fi
echo "Not the same file, as expected: nuget.org adds its signature, so the served file ($(size_of "$served") bytes) is not the candidate file ($(size_of "$candidate") bytes) and does not have its checksum."
compare_packages "$candidate" "$served"

echo "== 3. The served package as a published package"
bash "$repository/eng/verify-package.sh" --published "$served" > "$work/published.log" 2>&1 \
  || { cat "$work/published.log" >&2; fail "the served package is not as a published package of this version must be"; }
tail -1 "$work/published.log"
candidate_commit="$(unzip -p "$candidate" ConsoleCalendarGenerator.nuspec | tr -d '\r' | sed -n -E 's:.*<repository [^>]*commit="([0-9a-f]{40})".*:\1:p')"
served_commit="$(unzip -p "$served" ConsoleCalendarGenerator.nuspec | tr -d '\r' | sed -n -E 's:.*<repository [^>]*commit="([0-9a-f]{40})".*:\1:p')"
[ -n "$candidate_commit" ] && [ "$served_commit" = "$candidate_commit" ] || fail "the served package names commit '$served_commit', and the candidate was built from $candidate_commit"
echo "Commit:         $served_commit, the commit the candidate was built from"

if [ -n "$served_file" ]; then
  echo
  echo "The file is the candidate package plus a signature. The checks that need nuget.org were not made."
  exit 0
fi

echo "== 4. The symbols"
tool="$(build_tool SymbolIdentity "$work/symbol-tool")"
mkdir "$work/served-entries" "$work/candidate-symbols"
unzip -q "$served" -d "$work/served-entries"
unzip -q "$symbol_package" -d "$work/candidate-symbols"
for framework in $library_builds; do
  assembly="$work/served-entries/lib/$framework/ConsoleCalendarGenerator.dll"
  identity="$(dotnet "$tool" "$assembly" "$work/candidate-symbols/lib/$framework/ConsoleCalendarGenerator.pdb")" \
    || fail "the $framework symbol file of the candidate is not the one the served $framework assembly was built with"
  server_path="$(sed -n 's/^server-path=//p' <<< "$identity" | tr -d '\r')"
  checksum="$(sed -n 's/^checksum=//p' <<< "$identity" | tr -d '\r')"
  status="$(curl -s -L -o "$work/served-$framework.pdb" -w '%{http_code}' -H "SymbolChecksum: $checksum" "$nuget_symbols/$server_path")"
  [ "$status" = "200" ] || fail "nuget.org's symbol server does not have the $framework symbol file (HTTP $status). The symbol package was not pushed, was refused, or is not validated yet; its page on nuget.org says which."
  cmp -s "$work/served-$framework.pdb" "$work/candidate-symbols/lib/$framework/ConsoleCalendarGenerator.pdb" \
    || fail "the $framework symbol file nuget.org serves is not the candidate's"
  echo "Symbols:        $framework: nuget.org's symbol server has the candidate's symbol file, byte for byte"
done
echo "The symbol package itself cannot be downloaded from nuget.org. Its exact bytes are the ones in SHA256SUMS.txt: $(sha256_of "$symbol_package")"

echo "== 5. Installed from nuget.org"
# from_nuget_org <framework of the program> <build of the library it must get> <what that build says it is>
from_nuget_org() {
  local framework="$1" asset="$2" asset_name="$3"
  local consumer="$work/nuget-org-$framework"
  mkdir "$consumer"
  isolate_from_the_repository "$consumer"
  cp "$template/Program.cs" "$template/Modern.cs" "$template/PackageConsumer.csproj" "$consumer/"
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
</configuration>
CONFIG
  export ConsumerTargetFramework="$framework"
  dotnet add "$consumer/PackageConsumer.csproj" package ConsoleCalendarGenerator --version "$version" > "$consumer/add.log" 2>&1 \
    || { cat "$consumer/add.log"; fail "ConsoleCalendarGenerator $version could not be installed from nuget.org into a $framework program"; }
  dotnet build "$consumer/PackageConsumer.csproj" --configuration Release -p:UseModernApi=true -p:UseSharedCompilation=false -nodeReuse:false -warnaserror > "$consumer/build.log" 2>&1 \
    || { cat "$consumer/build.log"; fail "the $framework program did not build against the version from nuget.org"; }
  grep -q 'api.nuget.org' "$consumer/packages/consolecalendargenerator/$version/.nupkg.metadata" || fail "the version was not restored from nuget.org"
  unzip -p "$candidate" "lib/$asset/ConsoleCalendarGenerator.dll" | cmp -s - "$consumer/bin/Release/$framework/ConsoleCalendarGenerator.dll" \
    || fail "the $framework program does not run with the candidate's $asset assembly"
  local output expected
  output="$(dotnet "$consumer/bin/Release/$framework/PackageConsumer.dll" | tr -d '\r')" || fail "the $framework program did not run"
  expected="$(sed -e "s/{LIBRARY}/$asset_name, assembly version ${version%%.*}.0.0.0/" "$template/expected-2.0.txt" | tr -d '\r')"
  [ "$output" = "$expected" ] || { echo "$output" >&2; fail "the $framework program did not report what expected-2.0.txt expects"; }
  echo "Installed:      a $framework program installs $version from nuget.org alone, runs with the candidate's $asset assembly, uses both APIs and reports the five corrections"
}
from_nuget_org net8.0 netstandard2.1 ".NETStandard,Version=v2.1"
from_nuget_org net10.0 net10.0 ".NETCoreApp,Version=v10.0"
bash "$repository/eng/consumer-smoke.sh" "$work/served" > "$work/consumer-smoke.log" 2>&1 || { cat "$work/consumer-smoke.log"; fail "the consumer smoke tests fail against the served package"; }
echo "Consumers:      $(tail -1 "$work/consumer-smoke.log")"
bash "$repository/eng/upgrade-smoke.sh" "$work/served" > "$work/upgrade-smoke.log" 2>&1 || { cat "$work/upgrade-smoke.log"; fail "the upgrade from 1.0.0 fails against the served package"; }
echo "Upgrade:        $(tail -1 "$work/upgrade-smoke.log")"

echo
echo "ConsoleCalendarGenerator $version on nuget.org is the candidate built from commit $candidate_commit:"
echo "  - the package nuget.org serves is the candidate's package plus nuget.org's signature, and nothing else;"
echo "  - the symbol files nuget.org serves are the candidate's;"
echo "  - installed from nuget.org, it behaves as the release must."
echo "Not verified, because nuget.org does not offer it: the bytes of the symbol package as nuget.org stores it."
