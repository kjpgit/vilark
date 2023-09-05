python3 << __python_vimcode_end__
###################################################################################################
#
# ViLark vim integration plugin.  (2023) Karl Pickett / ViLark project
#
# [[ Key Bindings ]]
#
# This file creates (2) normal-mode key bindings by default.
#
#   <space>e    ->  Pick a file to open. ESC cancels.
#
#   <space>b    ->  Pick a buffer to switch to. ESC cancels.
#
# Search below for 'nnoremap' if you want to customize.
#
#
# [[ Installation ]]
#
# You can use this file like any other vimrc snippet.
#
# a) Copy and paste it entirely to your .vimrc
#
# b) (or) Add "source ~/.vim/vilark.vimrc" to your .vimrc
#
#
###################################################################################################
__python_vimcode_end__


" Vim keyboard bindings, feel free to comment these out and
" customize them in your personal vimrc instead.
" TODO: A better way to customize these and still get plugin updates
nnoremap <space>e :python3 ViLark_BrowseCurrentDirectory()<CR>
nnoremap <space>b :python3 ViLark_BrowseCurrentBuffers()<CR>


python3 << __python_vimcode_end__
###################################################################################################
#
# [[ Main Script ]]
#
###################################################################################################

import os
import subprocess
import tempfile
import vim

# Either put vilark in your $PATH, or export VILARK=/path/to/vilark
g_vilark_cmd = os.environ.get("VILARK", "vilark")
# For any bug reports: :python3 print(g_vilark_plugin_version)
g_vilark_plugin_version = "1.1"

def ViLark_BrowseCurrentDirectory():
    with tempfile.NamedTemporaryFile(mode="w", suffix=".vilarktmp") as tmp:
        vilark_env = os.environ.copy()
        vilark_env["VILARK_OUTPUT_FILE"] = tmp.name
        vilark_env["EDITOR"] = ""

        # Run ViLark and wait for it to finish.
        # If the user chooses a file, it will be written to $VILARK_OUTPUT_FILE
        # as utf8 bytes, no newline at the end.
        # If the user doesn't choose a file, it will be empty.
        ret = subprocess.run([g_vilark_cmd, "."], env=vilark_env, check=True)
        vim.command("redraw!")
        chosen_file = open(tmp.name).read()
        if chosen_file:
            print(f"ViLark: Selected file is {chosen_file}")
            vim.command(f"ViLarkSafeEdit {chosen_file}")
        else:
            print("ViLark: Select was canceled")


def ViLark_BrowseCurrentBuffers():
    with tempfile.NamedTemporaryFile(mode="w", suffix=".vilarktmp") as tmp_input, \
            tempfile.NamedTemporaryFile(mode="w", suffix=".vilarktmp") as tmp_output:
        vilark_env = os.environ.copy()
        vilark_env["VILARK_INPUT_LABEL"] = "Buffers"
        vilark_env["VILARK_INPUT_DISPLAY"] = "DIR_BAR_FILE"  # For readability
        vilark_env["VILARK_INPUT_FILE"] = tmp_input.name
        vilark_env["VILARK_OUTPUT_FILE"] = tmp_output.name
        vilark_env["EDITOR"] = ""

        for b in vim.buffers:
            line = f"{b.number:<8}  {b.name}"
            tmp_input.write(line + "\n")
        tmp_input.flush()

        ret = subprocess.run([g_vilark_cmd], env=vilark_env, check=True)
        vim.command("redraw!")
        chosen_buffer_line = open(tmp_output.name).read()
        if chosen_buffer_line:
            chosen_buffer_num = chosen_buffer_line.strip().split(' ')[0]
            print(f"ViLark: Selected buffer is ({chosen_buffer_num})")
            vim.command(f"buffer {chosen_buffer_num}")
        else:
            print("ViLark: Select was canceled")


__python_vimcode_end__

" Work around for fnameescape not being directly exposed to python
command -nargs=1 ViLarkSafeEdit execute 'edit' fnameescape(<f-args>)
