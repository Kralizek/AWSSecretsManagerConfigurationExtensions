#!/usr/bin/env bash
set -euo pipefail

: "${GH_TOKEN:?GH_TOKEN must be set}"
: "${GITHUB_REPOSITORY:?GITHUB_REPOSITORY must be set}"
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
    --arg pattern "$pattern" \
    '.[]
      | select(.draft == true)
      | select(.author.login == "github-actions[bot]")
      | select(.tag_name | test($pattern))
      | {tag: .tag_name, target: .target_commitish}' \
    <<< "$releases"
)

if (( ${#matching_releases[@]} > 1 )); then
  tags=$(printf '%s\n' "${matching_releases[@]}" | jq -r .tag | paste -sd ' ' -)
  echo "Multiple resumable draft releases match channel $RELEASE_CHANNEL: $tags" >&2
  exit 1
fi

if (( ${#matching_releases[@]} == 1 )); then
  release="${matching_releases[0]}"
  tag=$(jq -r .tag <<< "$release")
  target=$(jq -r .target <<< "$release")

  echo "Resuming draft release $tag at $target."
  echo "resume=true" >> "$GITHUB_OUTPUT"
  echo "tag=$tag" >> "$GITHUB_OUTPUT"
  echo "version=${tag#v}" >> "$GITHUB_OUTPUT"
  echo "target=$target" >> "$GITHUB_OUTPUT"
else
  echo "resume=false" >> "$GITHUB_OUTPUT"
fi
