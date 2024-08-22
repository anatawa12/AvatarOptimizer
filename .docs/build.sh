#!/bin/sh

set -eu

BASE_URL="$1"
LATEST_VERSION="$2"

mkdir -p static
./build-latest-txt.mjs "$BASE_URL" "$LATEST_VERSION"
hugo --minify --baseURL "$BASE_URL"

