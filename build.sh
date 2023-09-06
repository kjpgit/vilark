#!/bin/bash

# Simple build / install script for users that doesn't require make.
# It requires curl.

set -eu
set -o pipefail
cd `dirname $0`

DOTNET_URL=https://download.visualstudio.microsoft.com/download/pr/32f2c846-5581-4638-a428-5891dd76f630/ee8beef066f06c57998058c5af6df222/dotnet-sdk-8.0.100-preview.7.23376.3-linux-x64.tar.gz

INSTALL_DIR=~/.local/bin
VIM_INSTALL_DIR=~/.vim/plugin/vilark
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

echo "Do you want to install the vilark executable to $INSTALL_DIR ?"
echo
read -p " [y/n]: " CONFIRM
if [ "$CONFIRM" = y -o "$CONFIRM" = Y ]; then
  install -d $INSTALL_DIR
  install -v ./vilark $INSTALL_DIR
  echo
  echo "✅ Install Complete!"
  echo
fi

# Check if vilark is found in PATH
RC=0
VERSION=`vilark --version` || RC=$?
if [ $RC != 0 ]; then
  echo
  echo "⚠ Warning: 'vilark --version' failed to execute."
  echo "The vim plugin needs vilark in your PATH, or run this:"
  echo "     export VILARK=/path/to/vilark"
  echo
fi

echo "Do you want to install the vim plugin to $VIM_INSTALL_DIR ?"
echo
read -p " [y/n]: " CONFIRM
if [ "$CONFIRM" = y -o "$CONFIRM" = Y ]; then
  install -d $VIM_INSTALL_DIR
  install -m644 -v ./vim/vilark.vim $VIM_INSTALL_DIR
  echo
  echo "✅ Plugin Install Complete!"
  echo
fi

echo "---------------------------------------------------------"
echo
echo "Install steps completed."
echo "Run 'vilark' to make sure it runs / looks ok."
echo "Press Left/Right arrow to change tabs, and ESC to close it."
echo
echo "The vim plugin does not create any key bindings."
echo "You must add them in your ~/.vimrc, according to your preferences.  Mine are:"
echo
echo "nnoremap <space>e :python3 ViLark_BrowseCurrentDirectory()<CR>"
echo "nnoremap <space>b :python3 ViLark_BrowseCurrentBuffers()<CR>"
echo
echo "We hope you enjoy using ViLark!"
echo
