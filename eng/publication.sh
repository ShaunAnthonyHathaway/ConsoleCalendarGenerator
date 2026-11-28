# The publication to nuget.org: which one is approved, and the checks its steps share. It is sourced, after
# common.sh, by publication-preflight.sh, publication-checksums.sh, publication-verify.sh and the self-test.
#
# Nothing here publishes, and no script of this folder does. The one command that pushes a package is in
# .github/workflows/publish-nuget.yml, which runs only when it is started by hand. These scripts read GitHub
# and nuget.org, download the files of a release, and compare. They request no credential of nuget.org.
#
# The variables below are used by the scripts that source this file.
# shellcheck disable=SC2034

# ---- The one publication that is approved ----
#
# The workflow publishes this and nothing else. Its inputs have to repeat these values, and every other value
# is refused: a later version is published by changing this block, the workflow's defaults and the tests that
# compare the two, in a reviewed pull request. The four files are the files of the immutable GitHub release;
# they are never built again, and a file with another checksum is not the release.

approved_repository="ShaunAnthonyHathaway/ConsoleCalendarGenerator"
approved_workflow=".github/workflows/publish-nuget.yml"
approved_ref="refs/heads/master"
approved_environment="nuget-production"
approved_nuget_user="ShaunHathaway"

approved_package_id="ConsoleCalendarGenerator"
approved_version="2.0.1"
approved_assembly_version="2.0.0.0"
approved_file_version="2.0.1.0"

approved_tag="v2.0.1"
approved_tag_object="248b5eb0c643e65e3565fd6a01d77d37b84e7868"
approved_release_commit="bff3ee1cb1e80b9cdc3bfe2abdfcbb13a60510cd"
approved_release_id="461246889"
approved_release_title="ConsoleCalendarGenerator 2.0.1"

# The SHA-256 of each of the four files, as sha256sum writes them, in the order of the names.
approved_checksums="76b7cf92a3cb92a1e4eb53d070c707b414d51a87420badd4dde5d6670f4f45b4  ConsoleCalendarGenerator-2.0.1-binaries.zip
e9a2a67f4d302a966ec7d7fe5c8d270c5458481f6d2bd831c5db21811d04ac76  ConsoleCalendarGenerator.2.0.1.nupkg
ee5e2789d1adba2a64b40f1728a5c5735c64bb05201f9e238c369b3716969502  ConsoleCalendarGenerator.2.0.1.snupkg
c95cdd7052a7c5779180cb6e31e44547aa54474ce505d3f335f551f49fce6a0e  SHA256SUMS.txt"

# What nuget.org lists before the publication: the version of 2023 and nothing else.
approved_versions_before='["1.0.0"]'

# ---- What a script is told, and where its facts come from ----

input_version=""
input_tag=""
input_release_id=""
input_release_commit=""

# A folder of recorded answers that stands for GitHub and nuget.org, for the self-test. Empty in every real use.
simulation=""

# Where the answers of GitHub and nuget.org are kept while a script runs. Set by start_facts.
facts=""

start_facts() {
  facts="$work/facts"
  mkdir -p "$facts"
  command -v jq > /dev/null || fail "jq is needed to read what GitHub and nuget.org answer"
  if [ -n "$simulation" ]; then
    # A simulation proves that the checks refuse what they must. It is never a step of a publication.
    [ "${GITHUB_ACTIONS:-}" != "true" ] || fail "a simulation is never run in a workflow"
    [ -d "$simulation" ] || fail "there is no folder $simulation"
    echo "SIMULATION: every answer of GitHub and nuget.org below is read from $simulation. Nothing here says anything about a release."
  else
    command -v gh > /dev/null || fail "the GitHub CLI (gh) is needed to read the release"
  fi
}

# passed <text> -> one line of the record
passed() {
  printf 'ok    %s\n' "$*"
}

# must_be <what> <the value found> <the value the approved publication has>
must_be() {
  [ "$2" = "$3" ] || fail "$1 is '$2'. The approved publication has '$3'."
  passed "$1: $2"
}

# identity_argument <option> <value> -> takes one of the four options every publication script has
identity_argument() {
  case "$1" in
    --version) input_version="$2" ;;
    --tag) input_tag="$2" ;;
    --release-id) input_release_id="$2" ;;
    --release-commit) input_release_commit="$2" ;;
    *) return 1 ;;
  esac
}

# The four values that are asked for are the approved ones. Nothing else is looked at before this passes.
check_inputs() {
  [ -n "$input_version" ] && [ -n "$input_tag" ] && [ -n "$input_release_id" ] && [ -n "$input_release_commit" ] \
    || fail "the version, the tag, the release id and the release commit must all be given: --version, --tag, --release-id, --release-commit"
  must_be "the version asked for" "$input_version" "$approved_version"
  must_be "the tag asked for" "$input_tag" "$approved_tag"
  must_be "the release id asked for" "$input_release_id" "$approved_release_id"
  must_be "the release commit asked for" "$input_release_commit" "$approved_release_commit"
}

# context <NAME> -> what GitHub Actions says about the run in GITHUB_<NAME>; empty outside a workflow
context() {
  if [ -n "$simulation" ]; then
    sed -n "s/^$1=//p" "$simulation/context.env" | sed -n 1p
  else
    local name="GITHUB_$1"
    printf '%s\n' "${!name:-}"
  fi
}

# check_context <first-attempt|any-attempt>
#
# In a workflow: the run is a manual run of the publication workflow, of this repository, on the default
# branch, and nothing else. A run that was started again is refused where a package could be pushed: starting
# it again is a decision of its own, made with a new run. Outside a workflow the script is a dry run on a
# maintainer's machine, which can read and compare and cannot publish.
check_context() {
  if [ "$(context ACTIONS)" = "true" ]; then
    must_be "the repository of the run" "$(context REPOSITORY)" "$approved_repository"
    must_be "the event that started the run" "$(context EVENT_NAME)" "workflow_dispatch"
    must_be "the ref the run is for" "$(context REF)" "$approved_ref"
    must_be "the workflow of the run" "$(context WORKFLOW_REF)" "$approved_repository/$approved_workflow@$approved_ref"
    if [ "$1" = "first-attempt" ]; then
      must_be "the attempt of the run" "$(context RUN_ATTEMPT)" "1"
    fi
  else
    local origin
    origin="$(git -C "$repository" remote get-url origin 2> /dev/null || true)"
    origin="${origin%.git}"
    case "$origin" in
      "$repository_url" | "git@github.com:$approved_repository" | "ssh://git@github.com/$approved_repository") ;;
      *) fail "this checkout's origin is '$origin', not $repository_url" ;;
    esac
    passed "not a workflow run: a dry run on a checkout of $approved_repository. It reads and compares, and cannot publish."
  fi
}

# github <name> <path below the repository in GitHub's API> -> keeps the answer as $facts/<name>.json
github() {
  if [ -n "$simulation" ]; then
    [ -f "$simulation/$1.json" ] || fail "the simulation has no $1.json"
    cp "$simulation/$1.json" "$facts/$1.json"
  else
    gh api "repos/$approved_repository/$2" > "$facts/$1.json" < /dev/null \
      || fail "GitHub did not answer for repos/$approved_repository/$2"
  fi
}

# fact <name> <jq filter> -> a value of an answer that was kept
fact() {
  jq -r "$2" "$facts/$1.json"
}

# asset <file name> <jq filter> -> a value of the release's asset of that name
asset() {
  jq -r --arg name "$1" ".assets[] | select(.name == \$name) | $2" "$facts/release.json"
}

# http_status <name> <address> -> the status nuget.org answers the address with
http_status() {
  if [ -n "$simulation" ]; then
    [ -f "$simulation/$1.status" ] || fail "the simulation has no $1.status"
    tr -d '\r\n ' < "$simulation/$1.status"
  else
    curl -s -L -o /dev/null -w '%{http_code}' "$2"
  fi
}

# approved_names -> the names of the four files, one a line, in order
approved_names() {
  cut -c67- <<< "$approved_checksums"
}

# The tag: annotated, the approved tag object, and it peels to the approved commit. GitHub is asked, because
# GitHub is where the release is; the checkout is asked as well where it has the tag.
check_tag() {
  github tag-ref "git/ref/tags/$approved_tag"
  must_be "the ref of the tag on GitHub" "$(fact tag-ref .ref)" "refs/tags/$approved_tag"
  must_be "the tag is annotated: its ref names an object of the type" "$(fact tag-ref .object.type)" "tag"
  must_be "the tag object" "$(fact tag-ref .object.sha)" "$approved_tag_object"
  github tag-object "git/tags/$approved_tag_object"
  must_be "the name the tag object carries" "$(fact tag-object .tag)" "$approved_tag"
  must_be "the tag object names an object of the type" "$(fact tag-object .object.type)" "commit"
  must_be "the commit the tag peels to" "$(fact tag-object .object.sha)" "$approved_release_commit"

  # A simulation is about what GitHub answers. The checkout it runs in may be a shallow one.
  [ -z "$simulation" ] || return 0
  git -C "$repository" cat-file -e "$approved_release_commit^{commit}" 2> /dev/null \
    || fail "this checkout does not have the release commit $approved_release_commit. Check out with the whole history."
  git -C "$repository" merge-base --is-ancestor "$approved_release_commit" HEAD \
    || fail "the release commit $approved_release_commit is not in the history of this checkout"
  passed "the release commit is in the history of this checkout"
  if git -C "$repository" rev-parse -q --verify "refs/tags/$approved_tag" > /dev/null; then
    must_be "the tag in this checkout is an object of the type" "$(git -C "$repository" cat-file -t "refs/tags/$approved_tag")" "tag"
    must_be "the tag object in this checkout" "$(git -C "$repository" rev-parse "refs/tags/$approved_tag")" "$approved_tag_object"
    must_be "the commit the tag peels to in this checkout" "$(git -C "$repository" rev-parse "refs/tags/$approved_tag^{commit}")" "$approved_release_commit"
  else
    echo "note  this checkout does not have the tag itself; what GitHub answered above is the authority"
  fi
}

# The release: the approved one, published, immutable, not a prerelease, the release GitHub has for the tag,
# with exactly the four files, each uploaded and each with the approved checksum as GitHub records it.
check_release() {
  github release "releases/$approved_release_id"
  must_be "the release id" "$(fact release .id)" "$approved_release_id"
  must_be "the tag of the release" "$(fact release .tag_name)" "$approved_tag"
  must_be "the title of the release" "$(fact release .name)" "$approved_release_title"
  must_be "the release is a draft" "$(fact release .draft)" "false"
  must_be "the release is a prerelease" "$(fact release .prerelease)" "false"
  must_be "the release is immutable" "$(fact release .immutable)" "true"
  local published
  published="$(fact release .published_at)"
  grep -q -E '^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z$' <<< "$published" \
    || fail "the release has no time of publication ('$published'): it is not published"
  passed "the release is published: $published"
  github release-of-tag "releases/tags/$approved_tag"
  must_be "the release GitHub has for the tag" "$(fact release-of-tag .id)" "$approved_release_id"

  must_be "the number of files attached to the release" "$(fact release '.assets | length')" "4"
  must_be "the names of the files attached" "$(fact release '[.assets[].name] | sort | join(" ")')" "$(approved_names | tr '\n' ' ' | sed 's/ $//')"
  local hash name
  while read -r hash name; do
    must_be "the state of $name" "$(asset "$name" .state)" "uploaded"
    must_be "the checksum GitHub records for $name" "$(asset "$name" .digest)" "sha256:$hash"
  done <<< "$approved_checksums"
}

# download_release <an empty folder> -> the four files, each fetched by the id the release gives it
download_release() {
  local folder="$1" hash name id
  while read -r hash name; do
    id="$(asset "$name" .id)"
    grep -q -E '^[0-9]+$' <<< "$id" || fail "the release names no file $name"
    if [ -n "$simulation" ]; then
      cp "$simulation/assets/$name" "$folder/$name" 2> /dev/null || fail "the simulation has no file $name"
    else
      gh api -H "Accept: application/octet-stream" "repos/$approved_repository/releases/assets/$id" > "$folder/$name" < /dev/null \
        || fail "$name could not be downloaded from the release"
    fi
    passed "downloaded $name ($(size_of "$folder/$name") bytes), file $id of release $approved_release_id"
  done <<< "$approved_checksums"
}

# check_checksums <folder>
#
# The folder holds exactly the four files, and each has the approved SHA-256. Then the release's own file of
# checksums is used the way anyone would use it.
check_checksums() {
  local folder="$1" hash name actual found expected
  [ -d "$folder" ] || fail "there is no folder $folder"
  expected="$(approved_names | LC_ALL=C sort)"
  found="$(cd "$folder" && for name in * .[!.]*; do if [ -e "$name" ]; then printf '%s\n' "$name"; fi; done | LC_ALL=C sort)"
  if [ "$found" != "$expected" ]; then
    echo "Expected:" >&2
    echo "$expected" >&2
    echo "Found:" >&2
    echo "$found" >&2
    fail "$folder does not hold exactly the four files of the release"
  fi
  while read -r hash name; do
    actual="$(sha256_of "$folder/$name")"
    [ "$actual" = "$hash" ] || fail "the SHA-256 of $name is $actual. The approved file has $hash."
    passed "$hash  $name"
  done <<< "$approved_checksums"
  if command -v sha256sum > /dev/null; then
    (cd "$folder" && sha256sum -c "$checksums_name") > "$work/checksums.log" 2>&1 || { cat "$work/checksums.log" >&2; fail "$checksums_name does not verify the files beside it"; }
  else
    (cd "$folder" && shasum -a 256 -c "$checksums_name") > "$work/checksums.log" 2>&1 || { cat "$work/checksums.log" >&2; fail "$checksums_name does not verify the files beside it"; }
  fi
  passed "$checksums_name verifies the three files it lists: $(tr '\n' ' ' < "$work/checksums.log" | sed 's/ $//')"
}

# check_package <package file>
#
# What the package says it is: the approved ID and version, built from the release commit, with no
# dependency, with both builds of the library and their documentation, and with assemblies that carry the
# versions of the release.
check_package() {
  local package="$1" nuspec entries framework entry resource
  nuspec="$(unzip -p "$package" ConsoleCalendarGenerator.nuspec | tr -d '\r')"
  must_be "the package ID" "$(sed -n -E 's:.*<id>([^<]+)</id>.*:\1:p' <<< "$nuspec")" "$approved_package_id"
  must_be "the package version" "$(sed -n -E 's:.*<version>([^<]+)</version>.*:\1:p' <<< "$nuspec")" "$approved_version"
  must_be "the commit the package names" "$(sed -n -E 's:.*<repository [^>]*commit="([0-9a-f]{40})".*:\1:p' <<< "$nuspec")" "$approved_release_commit"
  if grep -q '<dependency ' <<< "$nuspec"; then fail "the package depends on another package"; fi
  grep -q '<group targetFramework="net10\.0" />' <<< "$nuspec" || fail "the package has no empty dependency group for net10.0"
  grep -q '<group targetFramework="\.NETStandard2\.1" />' <<< "$nuspec" || fail "the package has no empty dependency group for netstandard2.1"
  passed "the package has no dependencies, for net10.0 and for netstandard2.1"
  entries="$(entries_of "$package")"
  for framework in $library_builds; do
    for entry in "lib/$framework/ConsoleCalendarGenerator.dll" "lib/$framework/ConsoleCalendarGenerator.xml"; do
      grep -q -x -F "$entry" <<< "$entries" || fail "the package has no $entry"
    done
    # The version resource of an assembly is UTF-16: without its zero bytes each name is followed by its value.
    resource="$(unzip -p "$package" "lib/$framework/ConsoleCalendarGenerator.dll" | LC_ALL=C tr -d '\000' \
      | LC_ALL=C grep -a -o -E '(Assembly Version|FileVersion|ProductVersion)[0-9A-Za-z.+-]+' || true)"
    grep -q -F "Assembly Version$approved_assembly_version" <<< "$resource" || fail "the $framework assembly does not have the assembly version $approved_assembly_version"
    grep -q -F "FileVersion$approved_file_version" <<< "$resource" || fail "the $framework assembly does not have the file version $approved_file_version"
    grep -q -F "ProductVersion$approved_version+$approved_release_commit" <<< "$resource" \
      || fail "the $framework assembly does not have the informational version $approved_version+$approved_release_commit"
    passed "lib/$framework: the assembly and its documentation; assembly version $approved_assembly_version, file version $approved_file_version, informational version $approved_version+$approved_release_commit"
  done
  grep -q -x -F "README.md" <<< "$entries" || fail "the package has no README.md"
  passed "the package has its README.md"
}

# check_public_api <package file> <folder to work in>
#
# Both assemblies of the package have exactly the public API that eng/public-api.txt states, which is the API
# the tests require of the source: nothing missing, nothing added, the names of the parameters included.
check_public_api() {
  local package="$1" folder="$2" tool framework result
  tool="$(build_tool PublicApi "$folder/tool")"
  mkdir -p "$folder/assemblies"
  for framework in $library_builds; do
    mkdir -p "$folder/assemblies/$framework"
    unzip -p "$package" "lib/$framework/ConsoleCalendarGenerator.dll" > "$folder/assemblies/$framework/ConsoleCalendarGenerator.dll"
    result="$(dotnet "$tool" "$folder/assemblies/$framework/ConsoleCalendarGenerator.dll" "$repository/eng/public-api.txt" | tr -d '\r')" \
      || fail "the public API of the $framework assembly is not the one eng/public-api.txt states"
    passed "lib/$framework: the public API is exactly the ${result#entries=} entries of eng/public-api.txt"
  done
}

# nuget.org does not have the version: it lists what it listed before, and it has no package file of the
# version. If it does have the version, something published it, and nothing may be pushed.
check_not_published() {
  if [ -n "$simulation" ]; then
    [ -f "$simulation/nuget-index.json" ] || fail "the simulation has no nuget-index.json"
    cp "$simulation/nuget-index.json" "$facts/nuget-index.json"
  else
    curl -s -f -L -o "$facts/nuget-index.json" "$nuget_packages/consolecalendargenerator/index.json" \
      || fail "nuget.org did not answer for the versions of $approved_package_id"
  fi
  local versions status
  versions="$(jq -c '.versions' "$facts/nuget-index.json")"
  if jq -e --arg version "$approved_version" '.versions | index($version) != null' "$facts/nuget-index.json" > /dev/null; then
    fail "nuget.org already lists $approved_package_id $approved_version (it lists $versions). Nothing is pushed and no login is made. Find out what published it before anything else is done."
  fi
  must_be "the versions nuget.org lists" "$versions" "$approved_versions_before"
  status="$(http_status nuget-package "$nuget_packages/consolecalendargenerator/$approved_version/consolecalendargenerator.$approved_version.nupkg")"
  must_be "nuget.org's answer for the package file of $approved_version (404: it has none)" "$status" "404"
}
