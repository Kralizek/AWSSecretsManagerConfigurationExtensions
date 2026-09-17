#!/usr/bin/env bash
set -euo pipefail

: "${GH_TOKEN:?GH_TOKEN must be set}"
: "${GITHUB_REPOSITORY:?GITHUB_REPOSITORY must be set}"
: "${GITHUB_SHA:?GITHUB_SHA must be set}"
: "${TAG:?TAG must be set}"
: "${GITHUB_OUTPUT:?GITHUB_OUTPUT must be set}"

error_file=$(mktemp)
trap 'rm -f "$error_file"' EXIT

if release=$(gh api "repos/${GITHUB_REPOSITORY}/releases/tags/${TAG}" 2>"$error_file"); then
  :
elif grep -Eq 'HTTP 404|\(HTTP 404\)' "$error_file"; then
  echo "resume=false" >> "$GITHUB_OUTPUT"
  exit 0
else
  cat "$error_file" >&2
  exit 1
fi

draft=$(jq -r .draft <<< "$release")
author=$(jq -r .author.login <<< "$release")
target=$(jq -r .target_commitish <<< "$release")

if [[ "$draft" != "true" ]]; then
  echo "Release $TAG already exists and is not a draft." >&2
  exit 1
fi

if [[ "$author" != "github-actions[bot]" ]]; then
  echo "Draft release $TAG was not created by this workflow." >&2
  exit 1
fi

if [[ "$target" != "$GITHUB_SHA" ]]; then
  echo "Draft release $TAG targets $target instead of current commit $GITHUB_SHA." >&2
  exit 1
fi

echo "Resuming draft release $TAG."
echo "resume=true" >> "$GITHUB_OUTPUT"
