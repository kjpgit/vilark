#!/bin/sh
###################################################################################################
#
# Simple example of bash -> Vilark -> vim
#
# Creates a smart 'vi' alias.  Examples:
#
#  'vi'      -> Browse current directory ('.') with Vilark, then run vim
#
#  'vi /etc' -> Browse selected directory ('/etc') with Vilark, then run vim
#
#  'vi /etc/hosts' -> Directly open '/etc/hosts' file in vim
#
#  'vi /etc/no_such_file' -> Directly open '/tmp/no_such_file' file in vim
#
# Notes:
#
# * If any other vim options are passed, like -R or -u, this script just launches
#   vim directly.  (It doesn't try to read your mind.)
#
# * If more than one file/dir/argument is passed, this script just launches
#   vim directly.  (Again, it doesn't try to read your mind.)
#
# (C) 2023 Karl Pickett / Vilark project
#
###################################################################################################


unalias vi 2>/dev/null || :  # Ensure an alias doesn't override us

function vi() {
  local vilark_cmd="vilark"
  local editor="vim"
  local -i arg_count="$#"

  # Allow override, if it's not in your $PATH
  [ -n "$VILARK" ] && vilark_cmd="$VILARK"

  if [ $arg_count = 0 ]; then
    # Browse current directory with Vilark
    EDITOR="$editor" $vilark_cmd .
  elif [ $arg_count = 1 ]; then
    # User selected one specific thing, see how to handle it.
    if [ -f "$1" ]; then
      # Directly open existing file
      $editor "$1"
    elif [ -d "$1" ]; then
      # Browse existing directory with Vilark.
      # You probably want vim & vilark to cd and stay in this directory,
      # but if you don't, comment this line out and use the next line instead.
      (cd "$1" && EDITOR="$editor" $vilark_cmd .)
      #EDITOR="$editor" $vilark_cmd "$1"
    else
      # User selected a non-existing file, or we can't categorize it,
      # open/create it directly
      $editor "$1"
    fi
  else
    # Options or multiple files/dirs are given. Don't play the parsing game,
    # just launch the editor.
    $editor "$@"
  fi
}

