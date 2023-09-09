# ViLark Changelog

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
