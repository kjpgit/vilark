#!/bin/sh
#
# This shows your process hierarchy, just so you
# are aware of what's going on behind the scenes.  Example:
#
# bash
#   vilark /usr    # Your main file explorer, supports IPC from vim
#     vim          # Vim child process
#       vilark     # Vilark (for buffer selection only)
#
# Usage:
#   echo $BASHPID > /tmp/pid
#   watch -n1 pstree.sh

pstree -U -p --hide-threads -c -a `cat /tmp/pid`
echo
find /tmp -maxdepth 1 -name '*.vilarktmp'
