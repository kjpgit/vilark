# Vilark - Choosing a Vim buffer

While Vim tabs are useful, I find myself still switching buffers a lot.  And
Vilark makes it very fast and easy to do so.

1. Ensure you created a keybinding for the Vilark browse buffers action.  See the
   top of `~/.vim/plugin/vilark/vilark.vim` for an example.

2. Run `vim`, and then press your browse buffers key / sequence, which the example maps as \<space\>b

3. Vim will launch a child `vilark` process.  The Vilark full-screen UX will
   be drawn, and respond to keyboard input.

4. Vilark will list all of Vim's open buffers, showing their number and name.

5. Press up/down arrow or tab/shift-tab to select a file, or type in the search box.
   Note that you can search by file name or by number.

6. Press ENTER to confirm (open) the selected buffer.  Vilark will exit,
   and Vim will redraw its UX and respond to keyboard input.
