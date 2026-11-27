#!/usr/bin/env bash
#
# Checks the files of a release, as eng/release-bundle.sh makes them. It builds nothing of the library and
# changes nothing; it never publishes, uploads, tags or pushes.
#
#   eng/verify-release-bundle.sh artifacts/release-bundle
#
# What it requires:
#   - exactly four files, named for one version: the package, the symbol package, the binaries and SHA256SUMS.txt;
#   - SHA256SUMS.txt names exactly the other three, in order, and every checksum is right;
#   - the package and the symbol package pass eng/verify-package.sh;
#   - the archive holds exactly the expected files, and each is the same file as in the packages: the
#     assemblies and their documentation as in the package, the symbols as in the symbol package, the readme as
#     in the package, and the license of the commit the package was built from;
#   - the archive is reproducible: made again from its own contents, it is the same archive byte for byte;
#   - nothing in it names a folder of the machine it was built on;
#   - each symbol file is the one its assembly was built with, by the identifier and the checksum the assembly
#     records. That is the check nuget.org makes of a symbol package after the package itself is published,
#     when nothing can be taken back, so it is made here first.
#
# To check only the checksums of a bundle, anywhere, without this repository:
#   sha256sum -c SHA256SUMS.txt          (Linux, Git Bash)
#   shasum -a 256 -c SHA256SUMS.txt      (macOS)

set -euo pipefail
shopt -s nullglob
# shellcheck source=eng/common.sh
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"

[ "$#" -eq 1 ] || fail "usage: eng/verify-release-bundle.sh <folder with the bundle>"
bundle="$1"
[ -d "$bundle" ] || fail "there is no folder $bundle"

# ---- exactly four files, for one version ----

packages=("$bundle"/ConsoleCalendarGenerator.*.nupkg)
[ "${#packages[@]}" -eq 1 ] || fail "expected exactly one ConsoleCalendarGenerator .nupkg in $bundle, found ${#packages[@]}"
version="$(basename "${packages[0]}" .nupkg)"
version="${version#ConsoleCalendarGenerator.}"
[ "$version" != "1.0.0" ] || fail "version 1.0.0 is published; nothing built here may carry that number"

expected_files="$(printf '%s\n' "$(package_name "$version")" "$(symbols_name "$version")" "$(binaries_name "$version")" "$checksums_name" | LC_ALL=C sort)"
actual_files="$(cd "$bundle" && for file in * .[!.]*; do printf '%s\n' "$file"; done | LC_ALL=C sort)"
if [ "$actual_files" != "$expected_files" ]; then
  echo "Expected:" >&2
  echo "$expected_files" >&2
  echo "Found:" >&2
  echo "$actual_files" >&2
  fail "the bundle does not hold exactly the four files of version $version"
fi
package="$bundle/$(package_name "$version")"
symbol_package="$bundle/$(symbols_name "$version")"
binaries="$bundle/$(binaries_name "$version")"
checksums="$bundle/$checksums_name"
echo "Version:        $version"

# ---- the checksums ----

assets="$(printf '%s\n' "$(package_name "$version")" "$(symbols_name "$version")" "$(binaries_name "$version")" | LC_ALL=C sort)"
if LC_ALL=C grep -q $'\r' "$checksums"; then
  fail "$checksums_name has carriage returns; its lines end with a line feed"
fi
[ "$(tail -c 1 "$checksums" | od -An -c | tr -d ' ')" = '\n' ] || fail "$checksums_name does not end with a line feed"
bad_lines="$(grep -v -E '^[0-9a-f]{64}  [^ /\\]+$' "$checksums" || true)"
[ -z "$bad_lines" ] || fail "$checksums_name has a line that is not '<64 lowercase hexadecimal digits><two spaces><file name>': $bad_lines"
listed="$(cut -c67- "$checksums")"
if [ "$listed" != "$assets" ]; then
  echo "Expected, in this order:" >&2
  echo "$assets" >&2
  echo "Listed:" >&2
  echo "$listed" >&2
  fail "$checksums_name does not list exactly the three other files of the bundle, in the order of their names"
fi
while IFS= read -r line; do
  name="${line:66}"
  [ "$(sha256_of "$bundle/$name")" = "${line:0:64}" ] || fail "the checksum of $name is not the one in $checksums_name"
done < "$checksums"
echo "Checksums:      $checksums_name lists the three files, and every checksum is right"

new_work_folder
quiet_dotnet

# ---- the package and the symbol package ----

mkdir "$work/packages"
cp "$package" "$symbol_package" "$work/packages/"
bash "$repository/eng/verify-package.sh" "$work/packages" > "$work/verify-package.log" 2>&1 \
  || { cat "$work/verify-package.log" >&2; fail "the package or the symbol package of the bundle is not as expected"; }
echo "Packages:       the package and the symbol package are as eng/verify-package.sh expects"

nuspec="$(unzip -p "$package" ConsoleCalendarGenerator.nuspec | tr -d '\r')"
commit="$(sed -n -E 's:.*<repository [^>]*commit="([0-9a-f]{40})".*:\1:p' <<< "$nuspec")"
[ -n "$commit" ] || fail "the package does not name the commit it was built from"

# ---- the archive: exactly these files, in this order ----

root="ConsoleCalendarGenerator-$version"
expected_entries="$root/LICENSE
$root/README.md
$root/net10.0/ConsoleCalendarGenerator.dll
$root/net10.0/ConsoleCalendarGenerator.pdb
$root/net10.0/ConsoleCalendarGenerator.xml
$root/netstandard2.1/ConsoleCalendarGenerator.dll
$root/netstandard2.1/ConsoleCalendarGenerator.pdb
$root/netstandard2.1/ConsoleCalendarGenerator.xml"
actual_entries="$(unzip -Z1 "$binaries" | tr -d '\r')"
if [ "$actual_entries" != "$expected_entries" ]; then
  echo "Expected, in this order:" >&2
  echo "$expected_entries" >&2
  echo "Found:" >&2
  echo "$actual_entries" >&2
  fail "the archive does not hold exactly the expected files"
fi
unzip -t -q "$binaries" > /dev/null || fail "the archive is damaged"
mkdir "$work/unpacked"
unzip -q "$binaries" -d "$work/unpacked"

# ---- every file of the archive is the same file as in the packages ----

for framework in $library_builds; do
  for extension in dll xml; do
    unzip -p "$package" "lib/$framework/ConsoleCalendarGenerator.$extension" | cmp -s - "$work/unpacked/$root/$framework/ConsoleCalendarGenerator.$extension" \
      || fail "$framework/ConsoleCalendarGenerator.$extension in the archive is not the file in the package"
  done
  unzip -p "$symbol_package" "lib/$framework/ConsoleCalendarGenerator.pdb" | cmp -s - "$work/unpacked/$root/$framework/ConsoleCalendarGenerator.pdb" \
    || fail "$framework/ConsoleCalendarGenerator.pdb in the archive is not the file in the symbol package"

  # Nothing names a folder of the machine that built it. A build made for release maps the checkout to "/_/":
  # the symbols say where each source file is on GitHub, at the commit, and the assembly names its symbols
  # under the same root.
  LC_ALL=C grep -a -c -F "\"/_/*\":\"$raw_url/$commit/*\"" "$work/unpacked/$root/$framework/ConsoleCalendarGenerator.pdb" > /dev/null \
    || fail "$framework/ConsoleCalendarGenerator.pdb does not map its sources from /_/ to commit $commit on GitHub. Build with -p:ContinuousIntegrationBuild=true."
  LC_ALL=C grep -a -c -F "/_/ConsoleCalendarGenerator/obj/Release/$framework/ConsoleCalendarGenerator.pdb" "$work/unpacked/$root/$framework/ConsoleCalendarGenerator.dll" > /dev/null \
    || fail "$framework/ConsoleCalendarGenerator.dll names its symbols by a path of the machine that built it. Build with -p:ContinuousIntegrationBuild=true."
done
unzip -p "$package" README.md | cmp -s - "$work/unpacked/$root/README.md" || fail "README.md in the archive is not the readme in the package"
git -C "$repository" cat-file -e "$commit^{commit}" 2> /dev/null \
  || fail "this checkout does not have commit $commit, which the package was built from, so its license cannot be compared"
git -C "$repository" show "$commit:LICENSE" | cmp -s - "$work/unpacked/$root/LICENSE" || fail "LICENSE in the archive is not the license of commit $commit"
echo "Archive:        8 files, each the same as in the packages; the license of commit ${commit:0:9}"

# ---- the archive is reproducible ----

tool="$(build_stored_zip_tool "$work/tool")"
dotnet "$tool" "$work/again.zip" "$work/unpacked" > /dev/null
cmp -s "$binaries" "$work/again.zip" || fail "the archive is not reproducible: made again from its own contents, it is another archive"
echo "Reproducible:   made again from its own contents, the archive is the same byte for byte"

# ---- the symbols are the ones the assemblies were built with ----

symbol_tool="$(build_tool SymbolIdentity "$work/symbol-tool")"
for framework in $library_builds; do
  dotnet "$symbol_tool" "$work/unpacked/$root/$framework/ConsoleCalendarGenerator.dll" "$work/unpacked/$root/$framework/ConsoleCalendarGenerator.pdb" > "$work/symbols-$framework.log" 2>&1 \
    || { cat "$work/symbols-$framework.log" >&2; fail "$framework/ConsoleCalendarGenerator.pdb is not the symbol file $framework/ConsoleCalendarGenerator.dll was built with; nuget.org would refuse the symbol package"; }
done
echo "Symbols:        each symbol file is the one its assembly records, by identifier and checksum"

echo
echo "The bundle of version $version, built from commit $commit:"
while IFS= read -r line; do
  name="${line:66}"
  printf '  %s  %8s bytes  %s\n' "${line:0:64}" "$(size_of "$bundle/$name")" "$name"
done < "$checksums"
echo "The release bundle is as expected."
