#!/bin/bash
set -eu
set -o pipefail
cd `dirname $0`

COPYRIGHT='// Copyright (C) 2023 Karl Pickett / Vilark Project'

process() {
  while read filename; do
    if ! head -n1 $filename | fgrep -q "$COPYRIGHT"; then
       (echo "$COPYRIGHT"; cat $filename) > /tmp/file
      mv /tmp/file $filename
    fi
  done
}

find ../src/ -name '*.cs' | process
