#!/usr/bin/env bash
set -euo pipefail

: "${GH_TOKEN:?GH_TOKEN must be set}"
: "${GITHUB_REPOSITORY:?GITHUB_REPOSITORY must be set}"
: "${GITHUB_SHA:?GITHUB_SHA must be set}"
: "${RELEASE_CHANNEL:?RELEASE_CHANNEL must be set}"
: "${GITHUB_OUTPUT:?GITHUB_OUTPUT must be set}"

releases=$(gh api --paginate "repos/${GITHUB_REPOSITORY}/releases?per_page=100")

if [[ "$RELEASE_CHANNEL" == "stable" ]]; then
  pattern='^v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$'
else
  pattern="^v(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)-${RELEASE_CHANNEL}\\.[0-9]+$"
fi

mapfile -t matching_tags < <(
  jq -r \
    --arg sha "$GITHUB_SHA" \
    --arg pattern "$pattern" \
    '.[] | select(.draft == true and .target_commitish == $sha) | .tag_name | select(test($pattern))' \
    <<< "$releases"
)

if (( ${#matching_tags[@]} > 1 )); then
  echo "Multiple matching draft releases target $GITHUB_SHA: ${matching_tags[*]}" >&2
  exit 1
fi

if (( ${#matching_tags[@]} == 1 )); then
  tag="${matching_tags[0]}"
  echo "Resuming draft release $tag."
  echo "resume=true" >> "$GITHUB_OUTPUT"
  echo "tag=$tag" >> "$GITHUB_OUTPUT"
  echo "version=${tag#v}" >> "$GITHUB_OUTPUT"
else
  echo "resume=false" >> "$GITHUB_OUTPUT"
fi
