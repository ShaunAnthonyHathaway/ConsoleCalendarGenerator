#!/usr/bin/env bash
#
# Shows that eng/publication-preflight.sh refuses what it must, without GitHub, without nuget.org and without
# publishing: recorded answers stand for both, and one thing is made wrong at a time. Every wrong thing must be
# refused, for its own reason, and the right answers must pass.
#
#   eng/publication-preflight-selftest.sh [<folder with the four files of the release>]
#
# Without a folder the cases that need the real files are left out: every refusal that comes before the files
# are read is still shown, and so is the refusal of files that are not the approved ones. With the folder,
# which is the candidate or a download of the release, the right answers are also shown to pass, and the
# files are damaged in a copy.
#
# It uses no network when it refuses. It never asks nuget.org for a credential and has no way to.

set -euo pipefail
shopt -s nullglob
# shellcheck source=eng/common.sh
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"
# shellcheck source=eng/publication.sh
. "$repository/eng/publication.sh"

[ "$#" -le 1 ] || fail "usage: eng/publication-preflight-selftest.sh [<folder with the four files of the release>]"
real_files="${1:-}"
[ -z "$real_files" ] || [ -d "$real_files" ] || fail "there is no folder $real_files"
command -v jq > /dev/null || fail "jq is needed"

new_work_folder
preflight="$repository/eng/publication-preflight.sh"
good="$work/good"
count=0
refusals=0

# ---- the right answers ----

mkdir -p "$good/assets"
cat > "$good/context.env" << CONTEXT
ACTIONS=true
REPOSITORY=$approved_repository
EVENT_NAME=workflow_dispatch
REF=$approved_ref
WORKFLOW_REF=$approved_repository/$approved_workflow@$approved_ref
RUN_ATTEMPT=1
CONTEXT
assets="[]"
number=0
while read -r hash name; do
  number=$((number + 1))
  assets="$(jq -c --arg name "$name" --arg digest "sha256:$hash" --argjson id "$((800000000 + number))" \
    '. + [{id: $id, name: $name, state: "uploaded", digest: $digest}]' <<< "$assets")"
  if [ -n "$real_files" ]; then
    cp "$real_files/$name" "$good/assets/$name"
    # A kept candidate is read-only. The copies are damaged below, so they must be writable.
    chmod u+w "$good/assets/$name"
  else
    printf 'not the file of the release: %s\n' "$name" > "$good/assets/$name"
  fi
done <<< "$approved_checksums"
jq -n --argjson id "$approved_release_id" --arg tag "$approved_tag" --arg title "$approved_release_title" --argjson assets "$assets" \
  '{id: $id, tag_name: $tag, name: $title, draft: false, prerelease: false, immutable: true, published_at: "2026-11-28T02:37:46Z", assets: $assets}' > "$good/release.json"
jq -n --argjson id "$approved_release_id" '{id: $id}' > "$good/release-of-tag.json"
jq -n --arg ref "refs/tags/$approved_tag" --arg sha "$approved_tag_object" '{ref: $ref, object: {type: "tag", sha: $sha}}' > "$good/tag-ref.json"
jq -n --arg tag "$approved_tag" --arg sha "$approved_release_commit" '{tag: $tag, object: {type: "commit", sha: $sha}}' > "$good/tag-object.json"
jq -n '{versions: ["1.0.0"]}' > "$good/nuget-index.json"
echo 404 > "$good/nuget-package.status"

identity=(--version "$approved_version" --tag "$approved_tag" --release-id "$approved_release_id" --release-commit "$approved_release_commit")

# new_case -> a copy of the right answers, in $case, to make one thing wrong in
new_case() {
  count=$((count + 1))
  case="$work/case-$count"
  cp -R "$good" "$case"
}

# change <file of the case> <jq filter> -> the answer, changed
change() {
  jq "$2" "$case/$1" > "$case/$1.new"
  mv "$case/$1.new" "$case/$1"
}

# context_is <NAME> <value> -> what the run says about itself, changed
context_is() {
  grep -v "^$1=" "$case/context.env" > "$case/context.new"
  printf '%s=%s\n' "$1" "$2" >> "$case/context.new"
  mv "$case/context.new" "$case/context.env"
}

# refused <what is wrong> <part of the message it must be refused with> <arguments of the preflight...>
refused() {
  local what="$1" message="$2"
  shift 2
  if env -u GITHUB_ACTIONS bash "$preflight" "$@" > "$case/preflight.log" 2>&1; then
    cat "$case/preflight.log"
    fail "$what passed"
  fi
  grep -q -F -- "$message" "$case/preflight.log" || { cat "$case/preflight.log"; fail "$what was refused, but not because: $message"; }
  if grep -q -F "Every check passed" "$case/preflight.log" || grep -q -F "every check passed" "$case/preflight.log"; then
    cat "$case/preflight.log"
    fail "$what was refused and still reported as passed"
  fi
  refusals=$((refusals + 1))
  echo "ok: refused, $what"
}

echo "== What is asked for: only the approved publication"
new_case; refused "another version" "the version asked for is '2.0.2'" --version 2.0.2 --tag "$approved_tag" --release-id "$approved_release_id" --release-commit "$approved_release_commit" --simulate "$case" --into "$case/out"
[ ! -e "$case/out" ] || fail "something was downloaded for a version that is not approved"
new_case; refused "the version that is released on GitHub only" "the version asked for is '2.0.0'" --version 2.0.0 --tag v2.0.0 --release-id 461197895 --release-commit 117df06e5b6d684da6891cd3ac0310e5aa81ec98 --simulate "$case"
new_case; refused "another tag" "the tag asked for is 'v2.0.0'" --version "$approved_version" --tag v2.0.0 --release-id "$approved_release_id" --release-commit "$approved_release_commit" --simulate "$case"
new_case; refused "another release" "the release id asked for is '461197895'" --version "$approved_version" --tag "$approved_tag" --release-id 461197895 --release-commit "$approved_release_commit" --simulate "$case"
new_case; refused "the commit of the superseded candidate" "the release commit asked for is '097eace95f90d668badf9684188f32c3db164f4b'" --version "$approved_version" --tag "$approved_tag" --release-id "$approved_release_id" --release-commit 097eace95f90d668badf9684188f32c3db164f4b --simulate "$case"
new_case; refused "a value left out" "must all be given" --version "$approved_version" --tag "$approved_tag" --release-id "$approved_release_id" --simulate "$case"
new_case; refused "an option that does not exist" "unknown option --skip" "${identity[@]}" --simulate "$case" --skip checks

echo "== Where it runs: a manual run of the publication workflow, on the default branch, once"
new_case; context_is EVENT_NAME push; refused "a run started by a push" "the event that started the run is 'push'" "${identity[@]}" --simulate "$case"
new_case; context_is EVENT_NAME release; refused "a run started by a release" "the event that started the run is 'release'" "${identity[@]}" --simulate "$case"
new_case; context_is EVENT_NAME schedule; refused "a run started by a schedule" "the event that started the run is 'schedule'" "${identity[@]}" --simulate "$case"
new_case; context_is REF "refs/tags/$approved_tag"; refused "a run for a tag" "the ref the run is for is 'refs/tags/$approved_tag'" "${identity[@]}" --simulate "$case"
new_case; context_is REF refs/heads/other; refused "a run for another branch" "the ref the run is for is 'refs/heads/other'" "${identity[@]}" --simulate "$case"
new_case; context_is REPOSITORY someone/ConsoleCalendarGenerator; refused "a run in another repository" "the repository of the run is 'someone/ConsoleCalendarGenerator'" "${identity[@]}" --simulate "$case"
new_case; context_is WORKFLOW_REF "$approved_repository/.github/workflows/ci.yml@$approved_ref"; refused "a run of another workflow" "the workflow of the run is" "${identity[@]}" --simulate "$case"
new_case; context_is RUN_ATTEMPT 2; refused "a run that was started again" "the attempt of the run is '2'" "${identity[@]}" --simulate "$case"
new_case; refused "another user for nuget.org" "the user for nuget.org is 'someone'" "${identity[@]}" --simulate "$case" --nuget-user someone
new_case; refused "an e-mail address as the user for nuget.org" "the user for nuget.org is" "${identity[@]}" --simulate "$case" --nuget-user someone@example.com

echo "== nuget.org: the version must not be there"
new_case; change nuget-index.json '.versions += ["2.0.1"]'; refused "a version that nuget.org already lists" "nuget.org already lists $approved_package_id $approved_version" "${identity[@]}" --simulate "$case" --into "$case/out"
[ ! -e "$case/out" ] || fail "something was downloaded although nuget.org already lists the version"
grep -q -F "no login is made" "$case/preflight.log" || fail "the refusal does not say that no login is made"
new_case; change nuget-index.json '.versions += ["2.0.0"]'; refused "the GitHub-only version listed on nuget.org" "the versions nuget.org lists is" "${identity[@]}" --simulate "$case"
new_case; change nuget-index.json '.versions = []'; refused "nuget.org listing nothing" "the versions nuget.org lists is" "${identity[@]}" --simulate "$case"
new_case; echo 200 > "$case/nuget-package.status"; refused "a package file of the version on nuget.org" "nuget.org's answer for the package file of $approved_version" "${identity[@]}" --simulate "$case"

echo "== The tag: annotated, the approved object, the approved commit"
new_case; change tag-ref.json '.object.type = "commit"'; refused "a tag that is not annotated" "the tag is annotated: its ref names an object of the type is 'commit'" "${identity[@]}" --simulate "$case"
new_case; change tag-ref.json '.object.sha = "0000000000000000000000000000000000000000"'; refused "another tag object" "the tag object is '0000000000000000000000000000000000000000'" "${identity[@]}" --simulate "$case"
new_case; change tag-object.json '.object.sha = "097eace95f90d668badf9684188f32c3db164f4b"'; refused "a tag on another commit" "the commit the tag peels to is '097eace95f90d668badf9684188f32c3db164f4b'" "${identity[@]}" --simulate "$case"
new_case; change tag-object.json '.tag = "v2.0.2"'; refused "a tag object of another name" "the name the tag object carries is 'v2.0.2'" "${identity[@]}" --simulate "$case"

echo "== The release: published, immutable, not a prerelease, exactly the four files"
new_case; change release.json '.draft = true'; refused "a draft" "the release is a draft is 'true'" "${identity[@]}" --simulate "$case"
new_case; change release.json '.prerelease = true'; refused "a prerelease" "the release is a prerelease is 'true'" "${identity[@]}" --simulate "$case"
new_case; change release.json '.immutable = false'; refused "a release that can be changed" "the release is immutable is 'false'" "${identity[@]}" --simulate "$case"
new_case; change release.json 'del(.immutable)'; refused "a release that does not say it is immutable" "the release is immutable is 'null'" "${identity[@]}" --simulate "$case"
new_case; change release.json '.published_at = null'; refused "a release that is not published" "it is not published" "${identity[@]}" --simulate "$case"
new_case; change release.json '.tag_name = "v2.0.0"'; refused "a release of another tag" "the tag of the release is 'v2.0.0'" "${identity[@]}" --simulate "$case"
new_case; change release-of-tag.json '.id = 461197895'; refused "a tag that has another release" "the release GitHub has for the tag is '461197895'" "${identity[@]}" --simulate "$case"
new_case; change release.json '.assets += [{id: 1, name: "extra.zip", state: "uploaded", digest: "sha256:00"}]'; refused "a fifth file" "the number of files attached to the release is '5'" "${identity[@]}" --simulate "$case"
new_case; change release.json 'del(.assets[0])'; refused "a file missing from the release" "the number of files attached to the release is '3'" "${identity[@]}" --simulate "$case"
new_case; change release.json '.assets[1].name = "ConsoleCalendarGenerator.2.0.0.nupkg"'; refused "a file of another name" "the names of the files attached is" "${identity[@]}" --simulate "$case"
new_case; change release.json '.assets[1].state = "starter"'; refused "a file that is not uploaded" "the state of" "${identity[@]}" --simulate "$case"
new_case; change release.json '.assets[1].digest = "sha256:ee9c79d1"'; refused "a file GitHub records with another checksum" "the checksum GitHub records for" "${identity[@]}" --simulate "$case" --into "$case/out"
[ ! -e "$case/out" ] || fail "something was downloaded from a release whose files are not the approved ones"

echo "== The files: the approved bytes, and nothing else"
new_case; echo "one byte more" >> "$case/assets/$(package_name "$approved_version")"; refused "a package that is not the approved file" "The approved file has" "${identity[@]}" --simulate "$case"
new_case; echo "one byte more" >> "$case/assets/$(symbols_name "$approved_version")"; refused "a symbol package that is not the approved file" "The approved file has" "${identity[@]}" --simulate "$case"
new_case; echo "anything" > "$case/assets/extra.txt"; refused "a folder with a fifth file" "does not hold exactly the four files" "${identity[@]}" --simulate "$case" --bundle "$case/assets"
new_case; rm "$case/assets/$checksums_name"; refused "a folder without the checksums" "does not hold exactly the four files" "${identity[@]}" --simulate "$case" --bundle "$case/assets"
new_case; mkdir "$case/out"; echo "something" > "$case/out/file"; refused "a download into a folder that has files" "is not an empty folder" "${identity[@]}" --simulate "$case" --into "$case/out"
new_case; refused "a download and a folder at once" "give one of them" "${identity[@]}" --simulate "$case" --into "$case/out" --bundle "$case/assets"

echo "== A simulation is never a step of a publication"
new_case
if GITHUB_ACTIONS=true bash "$preflight" "${identity[@]}" --simulate "$case" > "$case/preflight.log" 2>&1; then
  fail "a simulation ran in a workflow"
fi
grep -q -F "a simulation is never run in a workflow" "$case/preflight.log" || { cat "$case/preflight.log"; fail "a simulation in a workflow was refused for another reason"; }
refusals=$((refusals + 1))
echo "ok: refused, a simulation in a workflow"

echo "== The right answers"
if [ -n "$real_files" ]; then
  new_case
  env -u GITHUB_ACTIONS bash "$preflight" "${identity[@]}" --simulate "$case" --nuget-user "$approved_nuget_user" --into "$case/out" > "$case/preflight.log" 2>&1 \
    || { cat "$case/preflight.log"; fail "the right answers and the files of the release were refused"; }
  grep -q -F "SIMULATION: every check passed" "$case/preflight.log" || fail "the right answers did not pass as a simulation"
  if grep -q -F "Every check passed for" "$case/preflight.log"; then fail "a simulation reported itself as a real preflight"; fi
  bash "$repository/eng/publication-checksums.sh" "$case/out" > /dev/null || fail "the files kept by the preflight are not the approved files"
  echo "ok: passed, the right answers with the files of the release ($(grep -c '^ok    ' "$case/preflight.log") checks)"
  new_case; rm "$case/assets/$(binaries_name "$approved_version")"; printf 'not an archive' > "$case/assets/$(binaries_name "$approved_version")"
  refused "an archive that is not the approved file" "The approved file has" "${identity[@]}" --simulate "$case"
  if bash "$repository/eng/publication-checksums.sh" "$case/assets" > /dev/null 2>&1; then fail "the checksum check accepted a folder with a damaged archive"; fi
  refusals=$((refusals + 1))
  echo "ok: refused, the checksum check on a folder with a damaged archive"
else
  new_case
  refused "files that are not the files of the release" "The approved file has" "${identity[@]}" --simulate "$case"
  echo "note: the right answers were not shown to pass, because no folder with the files of the release was given"
fi

echo
echo "The preflight refuses each of $refusals wrong cases for its own reason. Nothing was published, and no credential was requested."
