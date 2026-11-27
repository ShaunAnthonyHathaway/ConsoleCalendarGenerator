# Shared by the release scripts in this folder: release-bundle.sh, verify-release-bundle.sh,
# direct-reference-smoke.sh, upgrade-smoke.sh, release-links.sh and release-rehearsal.sh. It is sourced, not
# run. None of those scripts publishes, uploads, tags or pushes anything, and nothing here can.
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

# build_stored_zip_tool <folder to build it in> -> prints the path of the tool's assembly
build_stored_zip_tool() {
  local folder="$1"
  mkdir -p "$folder"
  isolate_from_the_repository "$folder"
  cp -R "$repository/eng/tools/StoredZip" "$folder/source"
  dotnet build "$folder/source/StoredZip.csproj" --configuration Release --output "$folder/out" \
    -p:UseSharedCompilation=false -nodeReuse:false -warnaserror > "$folder/build.log" 2>&1 \
    || { cat "$folder/build.log" >&2; fail "the archive tool did not build"; }
  printf '%s\n' "$folder/out/StoredZip.dll"
}
