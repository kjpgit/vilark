# ViLark

The file selector for humans. 😀

* Works great with Vim, but can also be used from the CLI to launch any $EDITOR.

* Uses C# AOT (100% native code compilation), so startup is lightning fast.


![Tutorial Screen Recording](./doc/videos/demo.gif)



## Installation Instructions

Installing on Linux or WSL2 is very easy.  The `build.sh` script will automatically
download the .Net 8 SDK to compile ViLark to a single-file executable with
native code (GC, but no JIT).

After building, you can completely remove the .Net SDK to free up space.

    git clone --depth 1 https://github.com/kjpgit/vilark
    cd vilark
    ./build.sh

* For a simple bash wrapper function, see [vilark.sh](vim/vilark.sh)

## Supported Terminals

A modern terminal emulator (24 bit color, xterm key sequences) is required.  ViLark is tested on:

* [Chrome Secure Shell (aka hterm)](https://chrome.google.com/webstore/detail/secure-shell/iodihamcpbpeioajjeobimgagajmlibd)

* [Windows Terminal (typically used with WSL2)](https://apps.microsoft.com/store/detail/windows-terminal/9N0DX20HK701?hl=en-us&gl=us&rtc=1)

* Linux console, xterm, lxterminal, gnome-terminal

NOTE: The Google Roboto Mono font is buggy
[(bug)](https://github.com/google/fonts/issues/360)
and is missing certain box drawing
characters and/or they have incorrect widths.  Please use a better font.

## Apple Support

There is preliminary Mac OS support in the `karl/mac-support` branch.
I still need to test it, unfortunately I don't have easy access to macs.
Any testing help is greatly appreciated.

## Additional Documentation

[User Manual](doc/README.md)

## About ViLark

Lark (noun) : A source of or quest for amusement or adventure

Skylark (noun) : The male skylark
[sings as it flies](https://www.youtube.com/watch?v=k71j3aW8DMw)

This project is the culmination of 25 years of experience.  I hope it brings you
as much joy as it does me.

© 2023 Karl Pickett / ViLark Project
