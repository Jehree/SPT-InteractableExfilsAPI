#!/bin/bash

# cleanup
rm -rf dist

# build
dotnet build

# prepare files
mkdir -p dist/BepInEx/plugins
cp ./bin/Debug/net471/InteractableExfilsAPI.dll ./dist/BepInEx/plugins/InteractableExfilsAPI.dll

cp JEHREE-InteractableExfilsAPI-LICENSE.txt ./dist/JEHREE-InteractableExfilsAPI-LICENSE.txt
cp README.md ./dist/JEHREE-InteractableExfilsAPI-README.md

# grab the version
version=$(git describe --tags --abbrev=0 | sed 's/^v//')

# create the zip
cd dist && npx bestzip ../Jehree-InteractableExfilsAPI-$version.zip *
