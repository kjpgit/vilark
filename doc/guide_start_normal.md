# Starting Vilark - Normal

Vilark is very flexible in how you start it.  You can start Vilark from the shell first, and have
it launch Vim, or you can start Vim first and have it launch Vilark as needed.

## Starting Vilark from the shell

1. Run `vilark` from the shell.  Note that `vilark` needs to be in your `$PATH`.
   By default, the install script copies it to `~/.local/bin`.

2. Vilark will scan the current directory, and its subdirectories, for files.
   Press CTRL-C to cancel, if the list is too big.

3. Press left/right arrow to change tabs.

4. Press up/down arrow or tab/shift-tab to select a file, or type in the search box.

5. Press ENTER to confirm (open) the selected file.  By default, this will
   launch your `$EDITOR` on the file, and Vilark will exit.

## Starting Vilark from Vim

1. Ensure you created some keybinding(s) for Vilark.  See the
   top of `~/.vim/plugin/vilark/vilark.vim` for an example.

2. Run `vim`, and then press your browse key / sequence, which the example maps as \<space\>e

3. Vim will launch a child `vilark` process.  The Vilark full-screen UX will
   be drawn, and respond to keyboard input.

4. Vilark will scan the current directory, and its subdirectories, for files.
   Press CTRL-C to cancel, if the list is too big.

5. Press left/right arrow to change tabs.

6. Press up/down arrow or tab/shift-tab to select a file, or type in the search box.

7. Press ENTER to confirm (open) the selected file.  Vilark will exit,
   and Vim will redraw its UX and respond to keyboard input.
