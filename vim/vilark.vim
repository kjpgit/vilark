python3 << __python_vimcode_end__
###################################################################################################
#
# Vilark vim integration plugin.  (2023) Karl Pickett / Vilark project
#
#
# This plugin does not create any default key bindings.
# You must add them in your ~/.vimrc, according to your preferences.  Mine are:
#
#
#    nnoremap <space>e :python3 ViLark_BrowseCurrentDirectory()<CR>
#    nnoremap <space>E :python3 ViLark_BrowseCurrentDirectory(edit_in_new_tab=True)<CR>
#    nnoremap <space>b :python3 ViLark_BrowseCurrentBuffers()<CR>
#
#
#
#
#
###################################################################################################

import os
import subprocess
import tempfile
import urllib.request
import vim

# Either put vilark in your $PATH, or export VILARK=/path/to/vilark
g_vilark_cmd = os.environ.get("VILARK", "vilark")
# For any bug reports: :python3 print(g_vilark_plugin_version)
g_vilark_plugin_version = "1.5"

def ViLark_BrowseCurrentDirectory(edit_in_new_tab=False):
    VILARK_IPC_URL = os.environ.get("VILARK_IPC_URL", "")
    if VILARK_IPC_URL:
        # A parent Vilark process is already running, switch to its UX for a selection
        try:
            with urllib.request.urlopen(VILARK_IPC_URL + "/getfile") as response:
               body = response.read()
               chosen_file = body.decode("utf-8")
        except Exception as e:
            vim.command("redraw!")
            print("Vilark IPC Error: " + str(e))
        else:
            vim.command("redraw!")
            _vilark_internal_open(chosen_file, edit_in_new_tab)
        return

    # Run Vilark and wait for it to finish.
    # If the user chooses a file, it will be written to $VILARK_OUTPUT_FILE
    # as utf8 bytes, no newline at the end.
    # If the user doesn't choose a file, it will be empty.
    with tempfile.NamedTemporaryFile(mode="w", suffix=".vilarktmp") as tmp:
        vilark_env = os.environ.copy()
        vilark_env["VILARK_PRESERVE_TERMINAL"] = "1"
        vilark_env["VILARK_OUTPUT_FILE"] = tmp.name
        vilark_env["EDITOR"] = ""

        ret = subprocess.run([g_vilark_cmd, "."], env=vilark_env, check=True)
        chosen_file = open(tmp.name).read()
        _vilark_internal_open(chosen_file, edit_in_new_tab)


def _vilark_internal_open(chosen_file, edit_in_new_tab):
    # Be cautious about print(), because a long file name (2+ lines)
    # causes vim to show a 'press enter to continue' blurb.
    #print(f"Vilark: Selected file is {chosen_file}")
    vim.command("redraw!")
    if not chosen_file:
        print("Vilark: Select was canceled")
    else:
        if edit_in_new_tab:
            vim.command(f"ViLarkSafeTabEdit {chosen_file}")
        else:
            vim.command(f"ViLarkSafeEdit {chosen_file}")


def ViLark_BrowseCurrentBuffers():
    with tempfile.NamedTemporaryFile(mode="w", suffix=".vilarktmp") as tmp_input, \
            tempfile.NamedTemporaryFile(mode="w", suffix=".vilarktmp") as tmp_output:
        vilark_env = os.environ.copy()
        vilark_env["VILARK_PRESERVE_TERMINAL"] = "1"
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
            #print(f"Vilark: Selected buffer is ({chosen_buffer_num})")
            vim.command(f"buffer {chosen_buffer_num}")
        else:
            print("Vilark: Select was canceled")


__python_vimcode_end__

" Work around for fnameescape not being directly exposed to python
command! -nargs=1 ViLarkSafeEdit execute 'edit' fnameescape(<f-args>)
command! -nargs=1 ViLarkSafeTabEdit execute 'tabedit' fnameescape(<f-args>)

" Make vim not mess with the terminal at all, when it's started as a child
" process from vilark.  Vilark will restore to main screen when it exits.
" These options come from ":help restorescreen"
if !empty($VILARK_IPC_URL)
    set t_ti= t_te=
endif
