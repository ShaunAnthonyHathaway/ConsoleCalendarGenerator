#!/usr/bin/env bash
#
# Takes the one build that may become a release, checks it, and writes the record that names it. It downloads
# from GitHub and reads nuget.org. It publishes nothing: no tag, no release, no package, no setting.
#
#   eng/designate-candidate.sh <workflow run id> <folder to keep the candidate in>
#
# The candidate of a version is one thing and nothing else (D-79):
#   the artifact "release-bundle" of the Linux job, of the first run of the workflow that a push to master
#   started for the release commit, in its first attempt.
# It is not a build made here, not a build of a pull request, not the build of another operating system, and
# not the result of running the workflow again. If that run failed or its artifact is not good, there is no
# candidate for that commit: the problem is corrected in a new reviewed commit, and the first push run of the
# new commit is the next one to look at. This script therefore refuses instead of repairing.
#
# What it requires before it downloads anything:
#   - this checkout is at the release commit, which is the head of master on GitHub, has no uncommitted
#     changes, and holds a release (no version suffix);
#   - the run is a push run on master for exactly that commit, is the first such run, is in its first attempt,
#     and every one of its jobs succeeded.
# Then it downloads the artifact once, into the folder, and runs every check a release has to pass on those
# exact files: the bundle and its checksums, the package, the tamper test, direct use of the assemblies, the
# upgrade from the published 1.0.0, the consumer smoke tests with Native AOT and the sample of 1.0.0, and the
# comparison that will be made after publication, rehearsed with a signature added.
#
# Only when all of it passes does it write, beside the bundle and not inside it:
#   CANDIDATE-RECORD.txt   where the files come from, their checksums, and what was checked
#   RELEASE-NOTES.md       the notes of the release, taken from the changelog
# and it makes the kept files read-only. The record is not committed: the release commit is the commit the
# candidate was built from, and nothing is added after it for the candidate's sake.

set -euo pipefail
shopt -s nullglob
# shellcheck source=eng/common.sh
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"

[ "$#" -eq 2 ] || fail "usage: eng/designate-candidate.sh <workflow run id> <folder to keep the candidate in>"
run="$1"
keep="$2"
slug="ShaunAnthonyHathaway/ConsoleCalendarGenerator"
artifact_name="release-bundle"
grep -q -E '^[0-9]+$' <<< "$run" || fail "'$run' is not the id of a workflow run"
command -v gh > /dev/null || fail "the GitHub CLI (gh) is needed to read the run and download its artifact"
[ "$(uname -s)" = "Linux" ] || fail "the candidate is checked on Linux, where the sample of 1.0.0 can be run and Native AOT is tested"
if [ -e "$keep" ]; then
  existing=("$keep"/* "$keep"/.[!.]*)
  [ -d "$keep" ] && [ "${#existing[@]}" -eq 0 ] || fail "$keep exists and is not an empty folder. A candidate is kept in a folder of its own, and is downloaded once."
fi

api() {
  gh api "repos/$slug/$1" --jq "$2"
}

echo "== The release commit"
project="$repository/ConsoleCalendarGenerator/ConsoleCalendarGenerator.csproj"
version="$(sed -n -E 's:.*<VersionPrefix>([^<]+)</VersionPrefix>.*:\1:p' "$project")"
if grep -q '<VersionSuffix>' "$project"; then fail "this checkout builds a development version; a candidate is a build of a release"; fi
commit="$(git -C "$repository" rev-parse HEAD)"
[ -z "$(git -C "$repository" status --porcelain --untracked-files=no)" ] || fail "the checkout has uncommitted changes"
master="$(git -C "$repository" ls-remote origin refs/heads/master | cut -f1)"
[ "$commit" = "$master" ] || fail "this checkout is at $commit, and master on GitHub is at $master"
echo "Version:        $version"
echo "Commit:         $commit, the head of master on GitHub, with no uncommitted changes"

echo "== The run"
[ "$(api "actions/runs/$run" .event)" = "push" ] || fail "run $run was not started by a push"
[ "$(api "actions/runs/$run" .head_branch)" = "master" ] || fail "run $run is not a run on master"
[ "$(api "actions/runs/$run" .head_sha)" = "$commit" ] || fail "run $run is a run for commit $(api "actions/runs/$run" .head_sha), not for the release commit"
[ "$(api "actions/runs/$run" .path)" = ".github/workflows/ci.yml" ] || fail "run $run is not a run of the CI workflow"
attempt="$(api "actions/runs/$run" .run_attempt)"
[ "$attempt" = "1" ] || fail "run $run has been run again (attempt $attempt). A rerun builds again, so its artifact is not the candidate, and the artifact of the first attempt is no longer the only build of this run."
[ "$(api "actions/runs/$run" .status)" = "completed" ] || fail "run $run has not finished"
[ "$(api "actions/runs/$run" .conclusion)" = "success" ] || fail "run $run did not succeed, so there is no candidate for this commit"
first="$(api "actions/runs?head_sha=$commit&event=push&per_page=100" '[.workflow_runs[] | select(.path == ".github/workflows/ci.yml")] | sort_by(.run_number) | .[0].id')"
[ "$first" = "$run" ] || fail "run $run is not the first push run for the release commit; that is run $first"
run_number="$(api "actions/runs/$run" .run_number)"
run_started="$(api "actions/runs/$run" .run_started_at)"
run_url="$(api "actions/runs/$run" .html_url)"
jobs="$(api "actions/runs/$run/attempts/1/jobs" '.jobs[] | [.id, .name, .conclusion, .completed_at] | @tsv')"
[ "$(wc -l <<< "$jobs" | tr -d ' ')" -eq 3 ] || fail "run $run does not have exactly three jobs"
if cut -f3 <<< "$jobs" | grep -q -v -x success; then
  echo "$jobs" >&2
  fail "not every job of run $run succeeded"
fi
linux_job="$(grep 'ubuntu' <<< "$jobs")" || fail "run $run has no Linux job"
[ "$(wc -l <<< "$linux_job" | tr -d ' ')" -eq 1 ] || fail "run $run has more than one Linux job"
echo "Run:            $run (number $run_number), started by a push to master for the release commit, the first such run, attempt 1"
echo "Jobs:           all three succeeded; the Linux job is $(cut -f1 <<< "$linux_job"), $(cut -f2 <<< "$linux_job")"

echo "== The artifact"
artifacts="$(api "actions/runs/$run/artifacts" '.artifacts[] | select(.name == "'"$artifact_name"'") | [.id, .size_in_bytes, .created_at, .expires_at, .expired, (.digest // "none"), .workflow_run.head_sha] | @tsv')"
[ -n "$artifacts" ] && [ "$(wc -l <<< "$artifacts" | tr -d ' ')" -eq 1 ] || fail "run $run does not have exactly one artifact named $artifact_name"
artifact_id="$(cut -f1 <<< "$artifacts")"
[ "$(cut -f5 <<< "$artifacts")" = "false" ] || fail "the artifact has expired"
[ "$(cut -f7 <<< "$artifacts")" = "$commit" ] || fail "the artifact belongs to another commit"
mkdir -p "$keep"
gh run download "$run" --repo "$slug" --name "$artifact_name" --dir "$keep/$artifact_name"
downloaded="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
bundle="$keep/$artifact_name"
echo "Artifact:       $artifact_name, id $artifact_id, $(cut -f2 <<< "$artifacts") bytes as stored, created $(cut -f3 <<< "$artifacts"), kept by GitHub until $(cut -f4 <<< "$artifacts")"
echo "Downloaded:     $downloaded, to $bundle"

new_work_folder
quiet_dotnet
results="$work/results.txt"
: > "$results"
# check <name for the record> <command...> -> runs it, keeps its last line for the record, stops on failure
check() {
  local name="$1"
  shift
  if ! "$@" > "$work/check.log" 2>&1; then
    cat "$work/check.log" >&2
    fail "the candidate did not pass: $name. Nothing is designated."
  fi
  printf '%-34s passed: %s\n' "$name" "$(tail -1 "$work/check.log")" >> "$results"
  echo "ok: $name"
}

echo "== The checks, on these exact files"
check "bundle and checksums" bash "$repository/eng/verify-release-bundle.sh" "$bundle"
[ -f "$bundle/$(package_name "$version")" ] || fail "the bundle is not a bundle of version $version"
bundle_commit="$(unzip -p "$bundle/$(package_name "$version")" ConsoleCalendarGenerator.nuspec | tr -d '\r' | sed -n -E 's:.*<repository [^>]*commit="([0-9a-f]{40})".*:\1:p')"
[ "$bundle_commit" = "$commit" ] || fail "the package in the bundle was built from $bundle_commit, not from the release commit"
at_the_tag="$(unzip -p "$bundle/$(package_name "$version")" README.md | grep -o -F "/v$version/" | wc -l | tr -d ' ')"
[ "$at_the_tag" -eq 3 ] || fail "the packed readme has $at_the_tag links at v$version, not the three that are part of the version"
mkdir "$work/packages"
cp "$bundle/$(package_name "$version")" "$bundle/$(symbols_name "$version")" "$work/packages/"
check "package and symbol package" bash "$repository/eng/verify-package.sh" "$work/packages"
check "damaged bundle refused" bash "$repository/eng/release-bundle-tamper-test.sh" "$bundle"
check "direct use of the assemblies" bash "$repository/eng/direct-reference-smoke.sh" "$bundle"
check "upgrade from published 1.0.0" bash "$repository/eng/upgrade-smoke.sh" "$bundle"
check "consumers, AOT, 1.0.0 sample" bash "$repository/eng/consumer-smoke.sh" "$bundle" --native-aot --published-sample
check "post-publication comparison" bash "$repository/eng/verify-published-package-selftest.sh" "$bundle"
check "release state of the source" bash "$repository/eng/release-state.sh"
bash "$repository/eng/verify-release-bundle.sh" "$bundle" > /dev/null || fail "the bundle changed while it was being checked"

echo "== The record"
notes="$keep/RELEASE-NOTES.md"
bash "$repository/eng/release-notes.sh" "$version" > "$notes"
record="$keep/CANDIDATE-RECORD.txt"
{
  echo "ConsoleCalendarGenerator $version: the candidate"
  echo
  echo "These four files are the only files authorized to be attached to the GitHub release v$version, and the"
  echo "package and the symbol package among them the only files authorized to be pushed to nuget.org as version"
  echo "$version. They are never built again and never replaced. A file with another checksum is not the release."
  echo
  echo "Files"
  while IFS= read -r line; do
    printf '  %s  %8s bytes  %s\n' "${line:0:64}" "$(size_of "$bundle/${line:66}")" "${line:66}"
  done < "$bundle/$checksums_name"
  printf '  %s  %8s bytes  %s  (lists the three above; SHA-256 of the file itself)\n' "$(sha256_of "$bundle/$checksums_name")" "$(size_of "$bundle/$checksums_name")" "$checksums_name"
  echo
  echo "Where they come from"
  echo "  Repository:        https://github.com/$slug"
  echo "  Release version:   $version"
  echo "  Release commit:    $commit (the head of master when designated; the commit the package names)"
  echo "  Workflow:          CI, .github/workflows/ci.yml"
  echo "  Run:               $run (run number $run_number), $run_url"
  echo "  Event and branch:  push, master"
  echo "  Run started:       $run_started"
  echo "  Attempt:           1. The run was not run again; no rerun produced or replaced these files."
  echo "  First push run:    yes, the first run a push to master started for the release commit"
  echo "  Linux job:         $(cut -f1 <<< "$linux_job"), \"$(cut -f2 <<< "$linux_job")\", completed $(cut -f4 <<< "$linux_job")"
  echo "  Artifact:          $artifact_name, id $artifact_id"
  echo "  Artifact stored:   $(cut -f2 <<< "$artifacts") bytes, created $(cut -f3 <<< "$artifacts"), digest $(cut -f6 <<< "$artifacts")"
  echo "  Kept by GitHub:    until $(cut -f4 <<< "$artifacts")"
  echo "  Downloaded:        $downloaded, once, into the folder this record is in"
  echo
  echo "The jobs of the run (the same commit on all three systems; only the Linux job's bundle is the candidate)"
  while IFS= read -r job; do
    printf '  %s  %-24s %s\n' "$(cut -f1 <<< "$job")" "$(cut -f2 <<< "$job")" "$(cut -f3 <<< "$job")"
  done <<< "$jobs"
  echo
  echo "Checked on the downloaded files, $(date -u +%Y-%m-%d), on Linux"
  sed 's/^/  /' "$results"
  echo "  The package names the release commit, and its readme has the three links of the version at v$version."
  echo
  echo "Not in these files, and not done"
  echo "  No tag, no GitHub release and no publication to nuget.org existed when this record was written."
  echo "  The intended tag is v$version on the release commit; the intended title is \"ConsoleCalendarGenerator $version\";"
  echo "  the notes are in RELEASE-NOTES.md beside this record, taken from the changelog."
} > "$record"
chmod a-w "$bundle"/* "$record" "$notes"
cat "$record"
echo
echo "The candidate of ConsoleCalendarGenerator $version is designated. It is in $bundle, read-only."
