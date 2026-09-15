#!/usr/bin/env bash
set -euo pipefail

: "${GH_TOKEN:?GH_TOKEN must be set}"
: "${GITHUB_REPOSITORY:?GITHUB_REPOSITORY must be set}"
: "${GITHUB_SHA:?GITHUB_SHA must be set}"
: "${TAG:?TAG must be set}"
: "${RELEASE_CHANNEL:?RELEASE_CHANNEL must be set}"
: "${RESUME:?RESUME must be set}"

if [[ "$RESUME" == "true" ]]; then
  echo "Reusing draft release $TAG."
  exit 0
fi

args=(
  "$TAG"
  --repo "$GITHUB_REPOSITORY"
  --target "$GITHUB_SHA"
  --title "$TAG"
  --generate-notes
  --draft
)

if [[ -n "${PREVIOUS_TAG:-}" ]]; then
  args+=(--notes-start-tag "$PREVIOUS_TAG")
fi

if [[ "$RELEASE_CHANNEL" != "stable" ]]; then
  args+=(--prerelease)
fi

gh release create "${args[@]}"
