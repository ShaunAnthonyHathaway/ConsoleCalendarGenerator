#!/usr/bin/env bash
#
# Checks that a folder holds exactly the four files of the approved publication, each with the approved
# SHA-256, and that SHA256SUMS.txt verifies the other three. It reads the folder and nothing else, changes
# nothing and publishes nothing.
#
#   eng/publication-checksums.sh <folder>
#
# The approved checksums are in eng/publication.sh. The workflow runs this as the last thing before the
# package is pushed, on the files that are pushed.

set -euo pipefail
shopt -s nullglob
# shellcheck source=eng/common.sh
. "$(dirname "${BASH_SOURCE[0]}")/common.sh"
# shellcheck source=eng/publication.sh
. "$repository/eng/publication.sh"

[ "$#" -eq 1 ] || fail "usage: eng/publication-checksums.sh <folder with the four files of the release>"
new_work_folder
check_checksums "$1"
echo "The four files are the approved files of $approved_package_id $approved_version."
