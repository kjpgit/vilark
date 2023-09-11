#!/bin/sh

# echo $BASHPID > /tmp/pid
  pstree -U --hide-threads -c -a `cat /tmp/pid`
