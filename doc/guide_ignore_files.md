# Ignoring Files

## Gitignore

Vilark will check each directory (and subdirectory) it scans for a `.gitignore` file.

Any matching files will not be shown in the Vilark UX, and matching subdirectories
will not be scanned.


## Global ignores

The default global ignore file is `~/.config/vilark/ignore_rules.txt`

This file contains rules (in .gitignore format) which apply to all files/dirs
scanned by Vilark.

Note that each rule causes a performance slowdown, especially wildcard rules,
because they get converted into regexes.

So if Vilark is slow to start up, try removing some ignore rules.  On the other
hand, adding some ignore rules can improve search performance by limiting the
number of files loaded into memory.

You can set `$VILARK_IGNORE_FILE` to a different file, or set it to "" to
not ignore anything.

