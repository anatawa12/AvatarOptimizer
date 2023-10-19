#!/bin/sh

set -eu

mkdir -p static
jq --raw-output '.version' <../package.json  >static/latest.txt
hugo --minify --baseURL "$1"

