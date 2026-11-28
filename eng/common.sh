# Shared by the release scripts in this folder: release-bundle.sh, verify-release-bundle.sh,
# release-bundle-tamper-test.sh, direct-reference-smoke.sh, upgrade-smoke.sh, release-links.sh,
# release-state.sh, release-notes.sh, verify-published-package.sh with its self-test,
# designate-candidate.sh, and the scripts of the publication, which also source publication.sh. It is sourced,
# not run. None of those scripts publishes, uploads, tags or pushes anything, and nothing here can.
#
# The variables below are used by the scripts that source this file.
# shellcheck disable=SC2034

repository="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
repository_url="https://github.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator"
raw_url="https://raw.githubusercontent.com/ShaunAnthonyHathaway/ConsoleCalendarGenerator"

fail() {
  echo "error: $*" >&2
  exit 1
}

# portable <path> -> the path in a form both bash and dotnet understand (Git Bash on Windows needs that).
portable() {
  if command -v cygpath > /dev/null; then
    cygpath -m "$1"
  else
    printf '%s\n' "$1"
  fi
}

# Creates a temporary folder outside the repository, in $work, and removes it when the script ends.
new_work_folder() {
  work="$(portable "$(mktemp -d)")"
  trap 'rm -rf "$work" || true' EXIT
}

# Keeps build processes from staying alive and holding a temporary folder open, and keeps their output plain.
quiet_dotnet() {
  export MSBUILDDISABLENODEREUSE=1
  export DOTNET_CLI_USE_MSBUILD_SERVER=0
  export DOTNET_NOLOGO=1
  export DOTNET_CLI_TELEMETRY_OPTOUT=1
}

# sha256_of <file> -> the SHA-256 of the file as 64 lowercase hexadecimal digits
sha256_of() {
  if command -v sha256sum > /dev/null; then
    sha256sum "$1" | cut -c1-64
  else
    shasum -a 256 "$1" | cut -c1-64
  fi
}

# size_of <file> -> its size in bytes
size_of() {
  wc -c < "$1" | tr -d ' '
}

# isolate_from_the_repository <folder> -> stops projects built under the folder from picking up the SDK
# version or the build settings of any folder above it, and gives them the SDK this repository uses.
isolate_from_the_repository() {
  cp "$repository/global.json" "$1/"
  echo '<Project />' > "$1/Directory.Build.props"
  echo '<Project />' > "$1/Directory.Build.targets"
}

# The four files of a release, for a version: the package, the symbol package, the binaries and the checksums.
package_name() { printf 'ConsoleCalendarGenerator.%s.nupkg\n' "$1"; }
symbols_name() { printf 'ConsoleCalendarGenerator.%s.snupkg\n' "$1"; }
binaries_name() { printf 'ConsoleCalendarGenerator-%s-binaries.zip\n' "$1"; }
checksums_name="SHA256SUMS.txt"

library_builds="net10.0 netstandard2.1"

# build_tool <name of a tool in eng/tools> <folder to build it in> -> prints the path of the tool's assembly
build_tool() {
  local name="$1" folder="$2"
  mkdir -p "$folder"
  isolate_from_the_repository "$folder"
  cp -R "$repository/eng/tools/$name" "$folder/source"
  dotnet build "$folder/source/$name.csproj" --configuration Release --output "$folder/out" \
    -p:UseSharedCompilation=false -nodeReuse:false -warnaserror > "$folder/build.log" 2>&1 \
    || { cat "$folder/build.log" >&2; fail "the tool $name did not build"; }
  printf '%s\n' "$folder/out/$name.dll"
}

# build_stored_zip_tool <folder to build it in> -> the tool that writes a reproducible archive
build_stored_zip_tool() {
  build_tool StoredZip "$1"
}

# The addresses of nuget.org that the release scripts read. None of them needs a credential.
nuget_packages="https://api.nuget.org/v3-flatcontainer"
nuget_registration="https://api.nuget.org/v3/registration5-gz-semver2"
nuget_symbols="https://symbols.nuget.org/download/symbols"

# entries_of <package file> -> the names of its entries, one a line, in order of the names
entries_of() {
  unzip -Z1 "$1" | tr -d '\r' | LC_ALL=C sort
}

# compare_packages <the package that was pushed> <the package nuget.org serves>
#
# nuget.org signs every package it serves, by adding one entry, ".signature.p7s". The served file is therefore
# never the pushed file, and its checksum is never the pushed file's checksum. What must hold instead: the
# served package has that one entry, has every entry of the pushed package, byte for byte, and has nothing
# else. The pushed package is the authority; anything missing, changed or added is a failure.
compare_packages() {
  local original="$1" served="$2" scratch problems=0 entry
  scratch="$(portable "$(mktemp -d)")"
  entries_of "$original" > "$scratch/original.txt"
  entries_of "$served" > "$scratch/served.txt"
  if [ -n "$(uniq -d "$scratch/original.txt")$(uniq -d "$scratch/served.txt")" ]; then
    rm -rf "$scratch"
    fail "a package names an entry twice"
  fi
  if grep -q -x -F '.signature.p7s' "$scratch/original.txt"; then
    rm -rf "$scratch"
    fail "the package that was pushed has a signature of its own; this comparison is for an unsigned package"
  fi
  if ! grep -q -x -F '.signature.p7s' "$scratch/served.txt"; then
    rm -rf "$scratch"
    fail "the served package has no .signature.p7s: nuget.org signs every package it serves, so this file is not one it served"
  fi
  grep -v -x -F '.signature.p7s' "$scratch/served.txt" > "$scratch/served-without-signature.txt" || true
  while IFS= read -r entry; do
    [ -z "$entry" ] && continue
    echo "missing from the served package: $entry" >&2
    problems=$((problems + 1))
  done <<< "$(LC_ALL=C comm -23 "$scratch/original.txt" "$scratch/served-without-signature.txt")"
  while IFS= read -r entry; do
    [ -z "$entry" ] && continue
    echo "in the served package and not in the pushed one: $entry" >&2
    problems=$((problems + 1))
  done <<< "$(LC_ALL=C comm -13 "$scratch/original.txt" "$scratch/served-without-signature.txt")"
  mkdir "$scratch/a" "$scratch/b"
  unzip -q "$original" -d "$scratch/a"
  unzip -q "$served" -d "$scratch/b"
  while IFS= read -r entry; do
    [ -f "$scratch/b/$entry" ] || continue
    if ! cmp -s "$scratch/a/$entry" "$scratch/b/$entry"; then
      echo "changed in the served package: $entry" >&2
      problems=$((problems + 1))
    fi
  done < "$scratch/original.txt"
  [ -s "$scratch/b/.signature.p7s" ] || { echo "the signature of the served package is empty" >&2; problems=$((problems + 1)); }
  local count
  count="$(wc -l < "$scratch/original.txt" | tr -d ' ')"
  rm -rf "$scratch"
  [ "$problems" -eq 0 ] || fail "the served package is not the pushed package plus nuget.org's signature: $problems differences"
  echo "Entries:        all $count entries of the pushed package are in the served one, byte for byte; the only other entry is .signature.p7s"
}
