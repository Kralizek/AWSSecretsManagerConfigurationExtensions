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

mapfile -t matching_releases < <(
  jq -rc \
    --arg sha "$GITHUB_SHA" \
    --arg pattern "$pattern" \
    '.[]
      | select(.target_commitish == $sha)
      | select(.tag_name | test($pattern))
      | {tag: .tag_name, draft: .draft}' \
    <<< "$releases"
)

if (( ${#matching_releases[@]} > 1 )); then
  tags=$(printf '%s\n' "${matching_releases[@]}" | jq -r .tag | paste -sd ' ' -)
  echo "Multiple matching releases target $GITHUB_SHA: $tags" >&2
  exit 1
fi

if (( ${#matching_releases[@]} == 1 )); then
  release="${matching_releases[0]}"
  tag=$(jq -r .tag <<< "$release")
  draft=$(jq -r .draft <<< "$release")

  if [[ "$draft" == "true" ]]; then
    echo "Resuming draft release $tag."
    published=false
  else
    echo "Resuming published release $tag."
    published=true
  fi

  echo "resume=true" >> "$GITHUB_OUTPUT"
  echo "published=$published" >> "$GITHUB_OUTPUT"
  echo "tag=$tag" >> "$GITHUB_OUTPUT"
  echo "version=${tag#v}" >> "$GITHUB_OUTPUT"
else
  echo "resume=false" >> "$GITHUB_OUTPUT"
  echo "published=false" >> "$GITHUB_OUTPUT"
fi
