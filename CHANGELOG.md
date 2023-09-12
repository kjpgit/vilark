# ViLark Changelog

## 2023.9.12 (Version 1.6.1)
* Fix flickering on some terminals
* Make child vim and vilark proceses not mess with the terminal,
  only the parent process will restore to the "main screen" upon exit.

## 2023.9.11 (Version 1.6)
* Don't follow directory symlinks, because it often causes infinite loops
* Show count on main screen (can be disabled in config)
* Fix .gitignore rules wrongly flagging a parent directory component

## 2023.9.10 (Version 1.5)
* "Zero-Lag" Fast switch ability, using HTTP listener socket to respond to a file
  selection request from the vim plugin.  It means you only load a large
  directory tree (and process ignore rules) one time.

## 2023.9.10 (Version 1.4)
* Ability to launch $EDITOR as a child process (this is the new default)
* Sort files/dirs by name
* Big cleanup of tty and signal code.  No flicker, better handling of ctrl-z and
  alternate screen

## 2023.9.9 (Version 1.3)
* Async file scanning during startup, with progress spinner
* Can ctrl-c/ctrl-z during initial file scan
* Ignore Library/ by default, for Mac OS

## 2023.9.8 (Version 1.2)
* MacOS support.  Tested on Mac OS Ventura, ARM64 (EC2)
* Cursor wraps around to top/bottom in main page
* Env var VILARK_TTY_RESET (default is 'stty sane')
* Remove dependency on Libc.Tmds
* Use built-in Console.ReadKey() for keyboard processing
* SIGTSTP (Ctrl-Z) does graceful terminal cleanup, then SIGSTOP
* SIGINT (Ctrl-C) does graceful terminal cleanup, then SIGINT

## 2023.9.5
* Fix #1 (Opening a file with '|' in the name)

## 2023.9.4
* Initial public release 😊
* ~1800 LOC
* Already an indispensable part of my vim experience
