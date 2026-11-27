#!/usr/bin/env bash
#
# Shows that eng/verify-release-bundle.sh refuses a bundle that is not exactly what eng/release-bundle.sh made.
# It takes a good bundle, damages a copy of it in one way at a time, and requires the check to fail each time,
# for the right reason. The bundle it is given is not touched.
#
#   eng/release-bundle-tamper-test.sh artifacts/release-bundle
#
# The damage: one byte changed in each of the three files; a file missing; a file too many; checksums in upper
# case, out of order, or with a line too many; and an archive that was put together again with one file more,
# or with a changed assembly, and given matching checksums, which only the comparison with the packages finds.

set -euo pipefail
shopt -s nullglob
# shellcheck source=eng/common.sh
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"

[ "$#" -eq 1 ] || fail "usage: eng/release-bundle-tamper-test.sh <folder with a good bundle>"
good="$1"
bash "$repository/eng/verify-release-bundle.sh" "$good" > /dev/null || fail "the bundle in $good is not good to begin with"
packages=("$good"/ConsoleCalendarGenerator.*.nupkg)
version="$(basename "${packages[0]}" .nupkg)"
version="${version#ConsoleCalendarGenerator.}"

new_work_folder
quiet_dotnet
tool="$(build_stored_zip_tool "$work/tool")"
count=0

# fresh -> a new copy of the good bundle, in $copy
fresh() {
  count=$((count + 1))
  copy="$work/bundle-$count"
  mkdir "$copy"
  cp "$good"/* "$copy/"
}

# flip <file> <offset> -> changes one byte of the file
flip() {
  local byte
  byte="$(od -An -tu1 -j "$2" -N 1 "$1" | tr -d ' ')"
  # shellcheck disable=SC2059
  printf "\\$(printf '%03o' $((byte ^ 255)))" | dd of="$1" bs=1 seek="$2" count=1 conv=notrunc 2> /dev/null
}

# checksums_again -> writes the checksums of the copy as the bundle script does
checksums_again() {
  (
    cd "$copy"
    for name in $(printf '%s\n' "$(package_name "$version")" "$(symbols_name "$version")" "$(binaries_name "$version")" | LC_ALL=C sort); do
      printf '%s  %s\n' "$(sha256_of "$name")" "$name"
    done > "$checksums_name"
  )
}

# refused <what was done> <part of the message the check must give>
refused() {
  if bash "$repository/eng/verify-release-bundle.sh" "$copy" > "$work/check.log" 2>&1; then
    cat "$work/check.log"
    fail "a bundle with $1 passed the check"
  fi
  grep -q -F "$2" "$work/check.log" || { cat "$work/check.log"; fail "a bundle with $1 was refused, but not because: $2"; }
  echo "ok: refused, $1"
}

fresh; flip "$copy/$(package_name "$version")" 100
refused "one byte changed in the package" "the checksum of $(package_name "$version") is not the one in"
fresh; flip "$copy/$(symbols_name "$version")" 100
refused "one byte changed in the symbol package" "the checksum of $(symbols_name "$version") is not the one in"
fresh; flip "$copy/$(binaries_name "$version")" 100
refused "one byte changed in the archive" "the checksum of $(binaries_name "$version") is not the one in"
fresh; printf 'x' >> "$copy/$(binaries_name "$version")"
refused "one byte added to the archive" "the checksum of $(binaries_name "$version") is not the one in"

fresh; rm "$copy/$(symbols_name "$version")"
refused "the symbol package missing" "does not hold exactly the four files"
fresh; rm "$copy/$checksums_name"
refused "the checksums missing" "does not hold exactly the four files"
fresh; echo "notes" > "$copy/NOTES.txt"
refused "a file too many" "does not hold exactly the four files"
fresh; cp "$copy/$(package_name "$version")" "$copy/ConsoleCalendarGenerator.9.9.9.nupkg"
refused "a second package" "expected exactly one ConsoleCalendarGenerator .nupkg"

fresh; tr 'a-f' 'A-F' < "$good/$checksums_name" > "$copy/$checksums_name"
refused "checksums in upper case" "has a line that is not"
fresh; { sed -n 2p "$good/$checksums_name"; sed -n 1p "$good/$checksums_name"; sed -n 3p "$good/$checksums_name"; } > "$copy/$checksums_name"
cmp -s "$good/$checksums_name" "$copy/$checksums_name" && fail "the checksums were not put out of order"
refused "checksums out of order" "does not list exactly the three other files"
fresh; sed -n '1p;2p' "$good/$checksums_name" > "$copy/$checksums_name"
refused "a checksum missing" "does not list exactly the three other files"
fresh; { cat "$good/$checksums_name"; printf '%s  %s\n' "$(sha256_of "$good/$checksums_name")" "$checksums_name"; } > "$copy/$checksums_name"
refused "a checksum too many" "does not list exactly the three other files"
fresh; sed -e 's/  / /' "$good/$checksums_name" > "$copy/$checksums_name"
refused "one space instead of two in the checksums" "has a line that is not"

# An archive put together again, with checksums that match it. Only its contents give it away.
fresh
mkdir "$work/more"
unzip -q "$copy/$(binaries_name "$version")" -d "$work/more"
echo "extra" > "$work/more/ConsoleCalendarGenerator-$version/EXTRA.txt"
rm "$copy/$(binaries_name "$version")"
dotnet "$tool" "$copy/$(binaries_name "$version")" "$work/more" > /dev/null
checksums_again
refused "a file added to the archive and the checksums made to match" "the archive does not hold exactly the expected files"

fresh
mkdir "$work/changed"
unzip -q "$copy/$(binaries_name "$version")" -d "$work/changed"
flip "$work/changed/ConsoleCalendarGenerator-$version/net10.0/ConsoleCalendarGenerator.dll" 200
rm "$copy/$(binaries_name "$version")"
dotnet "$tool" "$copy/$(binaries_name "$version")" "$work/changed" > /dev/null
checksums_again
refused "an assembly changed in the archive and the checksums made to match" "net10.0/ConsoleCalendarGenerator.dll in the archive is not the file in the package"

fresh
mkdir "$work/license"
unzip -q "$copy/$(binaries_name "$version")" -d "$work/license"
echo "Another license" > "$work/license/ConsoleCalendarGenerator-$version/LICENSE"
rm "$copy/$(binaries_name "$version")"
dotnet "$tool" "$copy/$(binaries_name "$version")" "$work/license" > /dev/null
checksums_again
refused "another license in the archive and the checksums made to match" "LICENSE in the archive is not the license of commit"

bash "$repository/eng/verify-release-bundle.sh" "$good" > /dev/null || fail "the good bundle no longer passes"
echo "A damaged bundle is refused in all $count ways, and the good one still passes."
