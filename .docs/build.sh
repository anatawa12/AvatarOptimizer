#!/bin/sh

set -eu

BASE_URL="$1"
LATEST_VERSION="$2"

mkdir -p static
echo "$LATEST_VERSION" > static/latest.txt
hugo --minify --baseURL "$BASE_URL"

