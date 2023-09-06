# Vilark User Documentation

## UX Key Bindings

    Escape                     cancel / quit
    Enter                      confirm selected file or buffer

    Left/Right                 change tab
    Up/Down/Tab/ShiftTab       change selection
    PageUp/PageDown/Home/End   scroll (if scrollbar is indicated)

    Ctrl-w                     delete previous word (in search box)



## File Handling

* $EDITOR is used to open files

* .gitignore files are checked at every level

* $VILARK_IGNORE_FILE holds additional ignore patterns (Default: ~/.config/vilark/ignore_rules.txt)


## Options Tab

### "When multiple search words are used"

If you search for "mo sc", ViLark searches each path for "mo" and "sc".

If you use "Match exact order", the "sc" must match after "mo" in the path.

If you use "Match any order", both "sc" and "mo" can match anywhere in the path.

Example, if you search for "mo sc":

* "Models\\MyScrollView.cs" will always match, because "sc" in Scroll comes
  after "mo" in "Model".

If you have "Match any order" selected:

* "ScrollModel.cs" will also match, because "sc" in "Scroll" doesn't have to
  come after "Model".

