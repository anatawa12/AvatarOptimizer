#!/bin/sh

set -eu

cd "$(dirname "$0")"

rm -f AvatarOptimizer-1.x.x.zip

curl -sL "https://api.anatawa12.com/create-vpai/?repo=https%3A%2F%2Fvpm.anatawa12.com%2Fvpm.json&package=com.anatawa12.avatar-optimizer&version=1.x.x" \
  > AvatarOptimizer-1.x.x-installer.unitypackage

zip "AvatarOptimizer-1.x.x.zip" \
  README.ja.txt \
  README.en.txt \
  LICENSE.txt \
  AvatarOptimizer-1.x.x-installer.unitypackage \
  add-repo.url

rm AvatarOptimizer-1.x.x-installer.unitypackage
