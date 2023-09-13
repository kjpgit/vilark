# Starting Vilark - Zero Lag UX Switching

Vilark's Zero-Lag UX switching feature is helpful if you have a large directory,
for example hundreds of thousands of files, which takes more than a few seconds
to load.

Vilark can load the directory once, and stay running in the background, until
it gets a request from Vim to select a new file.  When it gets the request,
Vilark will instantly draw its UX and respond to keyboard input - with
absolutely Zero Lag.


## Starting Vilark for Zero-Lag Switching

1. Run `vilark <directory>` from the shell.  Replace `<directory>`
   with a large directory you want to scan, like `/usr`.

2. Vilark will scan the directory you specified, and its subdirectories, for files.

3. Press right arrow to change to the Options tab.

4. Ensure that "Start a child process" is chosen, under the "How to start a
   Vim/EDITOR process" section.

5. Press left/right arrow to go back to the Files tab.  Select a file with up/down
   arrow or typing in the search box.

6. Press ENTER to confirm (open) the selected file.  By default, this will
   launch Vim (your `$EDITOR`) on the file, but Vilark will stay running in the
   background.

7. Ensure you created some Vim keybinding(s) for Vilark.  See the
   top of `~/.vim/plugin/vilark/vilark.vim` for an example.

8. Press your browse key / sequence, which the example maps as \<space\>e

9. Vim will signal the background Vilark process to wake up, redraw the screen,
   and choose another file.  There is no lag, because it was already running,
   and the directory was already scanned.


## Options for Zero Lag Switching

Each time you switch back to the background Vilark process, you can choose to
preserve the search text, or clear it and start fresh.

You can configure this in the Vilark Options tab.
