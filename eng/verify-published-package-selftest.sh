#!/usr/bin/env bash
#
# Shows that the comparison of eng/verify-published-package.sh works, before anything is published, on the
# two things that exist: the package nuget.org serves for the published 1.0.0, and a bundle of this repository.
#
#   eng/verify-published-package-selftest.sh artifacts/release-bundle
#
# With the published 1.0.0, which nuget.org signed when it was pushed in 2023:
#   - the file nuget.org serves has the size and the SHA-512 that nuget.org itself records for it;
#   - it has the entry ".signature.p7s";
#   - taken apart and put together again without that entry, it gives a stand-in for the file that was pushed,
#     and the comparison of the two passes: every entry the same, the signature the only difference.
# With that pair, and with the bundle's package given a signature the same way, a difference is made in one
# place at a time, and the comparison must refuse each: an entry changed, an entry missing, an entry too many,
# no signature, a changed manifest, a changed readme.
#
# It downloads 1.0.0 from nuget.org and needs no credential. It publishes nothing.

set -euo pipefail
shopt -s nullglob
# shellcheck source=eng/common.sh
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"

[ "$#" -eq 1 ] || fail "usage: eng/verify-published-package-selftest.sh <folder with a good bundle>"
bundle="$1"
packages=("$bundle"/ConsoleCalendarGenerator.*.nupkg)
[ "${#packages[@]}" -eq 1 ] || fail "expected exactly one ConsoleCalendarGenerator .nupkg in $bundle"
candidate="${packages[0]}"

new_work_folder
quiet_dotnet
zip_tool="$(build_stored_zip_tool "$work/zip-tool")"
count=0

# repack <folder> <package file to write> -> a package made of the files of the folder
repack() {
  rm -f "$2"
  dotnet "$zip_tool" "$2" "$1" > /dev/null
}

# refused <what was done> <part of the message> <command...>
refused() {
  local what="$1" message="$2"
  shift 2
  count=$((count + 1))
  if "$@" > "$work/check.log" 2>&1; then
    cat "$work/check.log"
    fail "$what passed"
  fi
  grep -q -F "$message" "$work/check.log" || { cat "$work/check.log"; fail "$what was refused, but not because: $message"; }
  echo "ok: refused, $what"
}
compare() {
  bash "$repository/eng/verify-published-package.sh" --compare "$1" "$2"
}

echo "== The published 1.0.0, as nuget.org serves it"
served="$work/served-1.0.0.nupkg"
status="$(curl -s -L -o "$served" -w '%{http_code}' "$nuget_packages/consolecalendargenerator/1.0.0/consolecalendargenerator.1.0.0.nupkg")"
[ "$status" = "200" ] || fail "nuget.org did not serve ConsoleCalendarGenerator 1.0.0 (HTTP $status)"
leaf="$(curl -s -L --compressed "$nuget_registration/consolecalendargenerator/1.0.0.json" | tr -d '\r\n')"
catalog="$(grep -o '"catalogEntry": *"[^"]*"' <<< "$leaf" | sed -E 's/.*"(https[^"]*)"/\1/')"
record="$(curl -s -L --compressed "$catalog" | tr -d '\r\n')"
recorded_size="$(grep -o '"packageSize": *[0-9]*' <<< "$record" | grep -o '[0-9]*$')"
recorded_hash="$(grep -o '"packageHash": *"[^"]*"' <<< "$record" | sed -E 's/.*: *"([^"]*)"/\1/')"
[ "$recorded_size" = "$(size_of "$served")" ] || fail "nuget.org records $recorded_size bytes for 1.0.0, and the download has $(size_of "$served")"
[ "$recorded_hash" = "$(openssl dgst -sha512 -binary "$served" | openssl base64 -A)" ] || fail "the download of 1.0.0 does not have the SHA-512 nuget.org records"
echo "ok: the download has the size ($recorded_size bytes) and the SHA-512 that nuget.org records for it"
entries_of "$served" | grep -q -x -F '.signature.p7s' || fail "the served 1.0.0 has no .signature.p7s"
echo "ok: it has the entry .signature.p7s, which nuget.org added"

mkdir "$work/served-entries"
unzip -q "$served" -d "$work/served-entries"
cp "$work/served-entries/.signature.p7s" "$work/signature.p7s"
cp -R "$work/served-entries" "$work/pushed-entries"
rm "$work/pushed-entries/.signature.p7s"
pushed="$work/pushed-1.0.0.nupkg"
repack "$work/pushed-entries" "$pushed"
compare "$pushed" "$served" | sed 's/^/    /'
echo "ok: without that entry it is the package that was pushed: the comparison passes on a pair made by nuget.org"

echo "== A difference at a time, with 1.0.0"
cp -R "$work/pushed-entries" "$work/changed"
printf 'x' >> "$work/changed/lib/netstandard2.1/ConsoleCalendarGenerator.dll"
repack "$work/changed" "$work/changed.nupkg"
refused "an assembly that differs from the served one" "changed in the served package: lib/netstandard2.1/ConsoleCalendarGenerator.dll" compare "$work/changed.nupkg" "$served"
cp -R "$work/pushed-entries" "$work/more"
echo "extra" > "$work/more/EXTRA.txt"
repack "$work/more" "$work/more.nupkg"
refused "an entry the served package lacks" "missing from the served package: EXTRA.txt" compare "$work/more.nupkg" "$served"
cp -R "$work/served-entries" "$work/served-more"
echo "extra" > "$work/served-more/EXTRA.txt"
repack "$work/served-more" "$work/served-more.nupkg"
refused "an entry too many in the served package" "in the served package and not in the pushed one: EXTRA.txt" compare "$pushed" "$work/served-more.nupkg"
refused "a served package with no signature" "has no .signature.p7s" compare "$pushed" "$pushed"
refused "a pushed package that is signed" "has a signature of its own" compare "$served" "$served"

echo "== The bundle's package, given a signature the way nuget.org gives one"
mkdir "$work/candidate-entries"
unzip -q "$candidate" -d "$work/candidate-entries"
cp -R "$work/candidate-entries" "$work/signed-entries"
cp "$work/signature.p7s" "$work/signed-entries/.signature.p7s"
signed="$work/signed.nupkg"
repack "$work/signed-entries" "$signed"
bash "$repository/eng/verify-published-package.sh" "$bundle" --served "$signed" | sed 's/^/    /'
echo "ok: the whole check passes on the bundle's package plus a signature"

# signed_with <name of the case> <command that changes the folder given as its argument> -> a signed package with that change
signed_with() {
  local folder="$work/case-$1"
  cp -R "$work/signed-entries" "$folder"
  "$2" "$folder"
  repack "$folder" "$work/case-$1.nupkg"
}
change_manifest() { printf ' ' >> "$1/ConsoleCalendarGenerator.nuspec"; }
change_readme() { printf 'x' >> "$1/README.md"; }
change_documentation() { printf ' ' >> "$1/lib/net10.0/ConsoleCalendarGenerator.xml"; }
drop_assembly() { rm "$1/lib/netstandard2.1/ConsoleCalendarGenerator.dll"; }
add_file() { echo "extra" > "$1/lib/net10.0/Extra.dll"; }
drop_signature() { rm "$1/.signature.p7s"; }
whole() {
  bash "$repository/eng/verify-published-package.sh" "$bundle" --served "$1"
}
signed_with manifest change_manifest
refused "a changed manifest" "changed in the served package: ConsoleCalendarGenerator.nuspec" whole "$work/case-manifest.nupkg"
signed_with readme change_readme
refused "a changed readme" "changed in the served package: README.md" whole "$work/case-readme.nupkg"
signed_with documentation change_documentation
refused "a changed documentation file" "changed in the served package: lib/net10.0/ConsoleCalendarGenerator.xml" whole "$work/case-documentation.nupkg"
signed_with assembly drop_assembly
refused "an assembly missing" "missing from the served package: lib/netstandard2.1/ConsoleCalendarGenerator.dll" whole "$work/case-assembly.nupkg"
signed_with extra add_file
refused "a file too many" "in the served package and not in the pushed one: lib/net10.0/Extra.dll" whole "$work/case-extra.nupkg"
signed_with unsigned drop_signature
refused "no signature" "has no .signature.p7s" whole "$work/case-unsigned.nupkg"
refused "the candidate file itself, which nuget.org never serves" "which nuget.org never serves" whole "$candidate"

echo "The comparison accepts a package plus nuget.org's signature, on a pair nuget.org made, and refuses a difference in all $count ways."
