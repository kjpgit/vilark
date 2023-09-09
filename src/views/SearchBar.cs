// Copyright (C) 2023 Karl Pickett / ViLark Project
using static vilark.BoxChars;

namespace vilark;

class SearchBar: IView
{
    public String SearchText => searchText;  // sometimes we need to pull
    public event EventHandler<bool>? SearchChanged;

    private const int searchBoxLength = 25;
    private string searchText = "";
    private int cursorPos = 0;

    public SearchBar() { }

    public override void OnKeyPress(KeyPress kp) {
        if (kp.rune != null) {
            if (kp.IsRune(KeyPress.ASCII_CTRL_W)) {
                ClearWord();
            } else if (!kp.IsAsciiControlCode()) {
                searchText += kp.rune;
                SearchChanged?.Invoke(this, true);
            }
        }

        if (kp.keyCode == KeyCode.BACKSPACE) {
            if (searchText.Length > 0) {
                searchText = TextHelper.RemoveLastRune(searchText);
                SearchChanged?.Invoke(this, true);
            }
        }
    }

    private void ClearWord() {
        // Ctrl-w in bash removes all trailing spaces *and* any word
        searchText = searchText.TrimEnd();
        int sep = searchText.LastIndexOf(' ');
        if (sep >= 0) {
            // we aren't the last word, so go back to the last space
            searchText = searchText.Substring(0, sep+1);
        } else {
            // this is the last word, remove it
            searchText = "";
        }
        SearchChanged?.Invoke(this, true);
    }

    override public void UpdateCursor(Console console) {
        if (IsVisible) {
            console.SetCursorXY(cursorPos, 0, Size);
            console.SetCursorVisible(true);
        }
    }

    override public void Draw(Console console) {
        console.SetCursorXY(0, 0, Size);

        int usedCols = 0;
        console.Write(BOX_VERT);
        console.WriteRepeated(" "u8, 5);
        usedCols += 6;

        var label = new DisplayText("Search: ");
        console.Write(label);
        usedCols += label.Columns;

        var searchTextDisplayed = new DisplayText(searchText,
                maxColumns:searchBoxLength,
                autoFit: true);
        console.SetUnderline(true);
        console.Write(searchTextDisplayed);
        usedCols += searchTextDisplayed.Columns;

        // For cursor pos
        cursorPos = usedCols;

        // Finish search box
        int searchFinish = Math.Max(searchBoxLength - searchText.Length, 0);
        console.WriteRepeated(" "u8, searchFinish);
        console.SetUnderline(false);
        usedCols += searchFinish;

        int fillCols = Size.NumRemainingCols(usedCols) - 1;
        console.WriteRepeated(" "u8, fillCols);
        console.Write(BOX_VERT);
    }
}

