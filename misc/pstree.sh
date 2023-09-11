#!/bin/sh

# echo $BASHPID > /tmp/pid
# watch -n1 pstree.sh
pstree -U --hide-threads -c -a `cat /tmp/pid`
echo
find /tmp -maxdepth 1 -name '*.vilarktmp'
