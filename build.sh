#!/bin/bash

# Simple build / install script for users
# Usage: ./build.sh

set -eu
set -o pipefail
cd `dirname $0`

DOTNET_URL_MAC_ARM="https://download.visualstudio.microsoft.com/download/pr/2a79b5ad-82a7-4615-a73b-91bf24028471/0e6a5c6d7f8b792a421e3796a93ef0a1/dotnet-sdk-8.0.100-osx-arm64.tar.gz"
DOTNET_URL_MAC_X64="https://download.visualstudio.microsoft.com/download/pr/e59acfc2-5987-43f9-bd03-0cbe446679e1/7db7313c1c99104279a69ccd47d160a1/dotnet-sdk-8.0.100-osx-x64.tar.gz"
DOTNET_URL_LINUX_X64="https://download.visualstudio.microsoft.com/download/pr/5226a5fa-8c0b-474f-b79a-8984ad7c5beb/3113ccbf789c9fd29972835f0f334b7a/dotnet-sdk-8.0.100-linux-x64.tar.gz"

# Build automatically on Linux/x64, Mac/x64, and Mac/ARM
echo "Machine Information: $OSTYPE - $HOSTTYPE"
if [[ "$OSTYPE" = darwin* ]] ; then
  if [[ $HOSTTYPE = arm64 ]] || [[ $HOSTTYPE = aarch64 ]]; then
    echo "Building for Mac OS + ARM"
    DOTNET_URL=$DOTNET_URL_MAC_ARM
  else
    echo "Building for Mac OS + x86_64"
    DOTNET_URL=$DOTNET_URL_MAC_X64
  fi
else
  echo "Building for Linux + x86_64.  Did not detect Mac OS."
  DOTNET_URL=$DOTNET_URL_LINUX_X64
fi

# CLI args can override autodetected settings
while [ "$#" -gt 0 ]; do
  if [ "$1" = "--docker" ]; then
    DOTNET_URL=USE_DOCKER
    shift
  else
    echo "Unknown argument: $1"
    exit 2
  fi
done


if [ $DOTNET_URL = USE_DOCKER ]; then
  # Build using docker
  docker build -t vilark .
  docker rm -f vilark_cont
  docker create --name vilark_cont vilark
  docker cp vilark_cont:/build/out/vilark .
else
  # Build using local toolchain
  DOTNET_DIR=dotnet_sdk
  export DOTNET_CLI_TELEMETRY_OPTOUT=1
  export DOTNET_NOLOGO=1
  if [ ! -f $DOTNET_DIR/done ]; then
    echo "Downloading and extracting dotnet SDK (200 MB) to $DOTNET_DIR/ ..."
    mkdir -p $DOTNET_DIR
    curl $DOTNET_URL | tar -xz -C $DOTNET_DIR
    touch $DOTNET_DIR/done
    echo
  fi

  echo "Compiling Vilark.. "
  ./dotnet_sdk/dotnet publish src
  cp artifacts/publish/program/release/vilark .
fi

echo
echo "✅ Build Complete!"
echo


INSTALL_DIR=~/.local/bin
VIM_INSTALL_DIR=~/.vim/plugin/vilark
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
  echo "⛔ Warning: 'vilark --version' failed to execute."
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
echo "nnoremap <space>E :python3 ViLark_BrowseCurrentDirectory(edit_in_new_tab=True)<CR>"
echo "nnoremap <space>b :python3 ViLark_BrowseCurrentBuffers()<CR>"
echo
echo "We hope you enjoy using Vilark!"
echo
