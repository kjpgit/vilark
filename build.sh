#!/bin/bash

# Simple build script for users that doesn't require make.
# It requires curl.

set -eu
set -o pipefail
cd `dirname $0`

DOTNET_URL=https://download.visualstudio.microsoft.com/download/pr/32f2c846-5581-4638-a428-5891dd76f630/ee8beef066f06c57998058c5af6df222/dotnet-sdk-8.0.100-preview.7.23376.3-linux-x64.tar.gz

DOTNET_DIR=dotnet_sdk
export DOTNET_CLI_TELEMETRY_OPTOUT=1

if [ ! -f $DOTNET_DIR/done ]; then
  echo "Downloading and extracting dotnet SDK (200 MB) to $DOTNET_DIR/ ..."
  mkdir -p $DOTNET_DIR
  curl $DOTNET_URL | tar -xz -C $DOTNET_DIR
  touch $DOTNET_DIR/done
  echo
fi

echo "Compiling ViLark.. "
./dotnet_sdk/dotnet publish src
cp artifacts/publish/program/release/vilark .

echo
echo "✅ Build Complete!"
echo

INSTALL_DIR=~/.local/bin
echo "Do you want to install the vilark executable to $INSTALL_DIR ?"
echo
read -p " [y/n]: " CONFIRM

if [ "$CONFIRM" = y -o "$CONFIRM" = Y ]; then
  echo "Installing to $INSTALL_DIR"
  install -v ./vilark $INSTALL_DIR
  echo
  echo "✅ Install Complete!"
  echo
fi

echo
echo "1) run ./vilark to make sure it runs / looks ok.  Press ESC to close it."
echo
echo
echo "2) Make sure vilark is in your PATH, or run: "
echo "     export VILARK=/path/to/vilark"
echo
echo "3) See the vim/ directory for bash and vim plugins"
echo
