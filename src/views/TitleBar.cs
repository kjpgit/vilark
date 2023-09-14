// Copyright (C) 2023 Karl Pickett / Vilark Project
using static vilark.BoxChars;

namespace vilark;

class TitleBar: IView
{
    private List<string> tabNames = new();
    private int currentTabIndex = 0;

    public string CurrentTabName => tabNames[CurrentTabIndex];
    public int CurrentTabIndex => currentTabIndex;

    public TitleBar(IView parent) : base(parent) {
        var contentName = "Files";
        string? contentNameOverride = Environment.GetEnvironmentVariable("VILARK_INPUT_LABEL");
        if (contentNameOverride != null) {
            contentName = contentNameOverride;
        }
        tabNames.Add(contentName);
        tabNames.Add("Options");
        tabNames.Add("Help");
    }

    public void CycleTabs(int delta) {
        int newIdx = currentTabIndex;
        newIdx += delta;
        if (newIdx < 0) {
            newIdx = tabNames.Count() - 1;
        }
        if (newIdx >= tabNames.Count()) {
            newIdx = 0;
        }
        currentTabIndex = newIdx;
        Log.Info($"Switching to tab {currentTabIndex}");
    }

    private int GetTabCols() {
        int ret = 0;
        foreach (var t in tabNames) {
            ret += t.Length + 2;
        }
        return ret;
    }

    override public void Draw(Console console) {
        console.SetCursorXY(0, 0, Size);

        console.Write(BOX_TOP_LEFT);
        int currentUsed = 1;

        int fillWidth = Size.NumRemainingCols(currentUsed + GetTabCols() + 3);
        console.WriteRepeated(BOX_HORIZ, fillWidth);
        currentUsed += fillWidth;

        console.Write(BOX_TEE_LEFT);

        foreach (var tabName in tabNames) {
            console.Write(BOX_BLANK);
            bool underlined = (tabName == CurrentTabName);

            console.SetUnderline(underlined);
            var tabNameText = new DisplayText(tabName);
            console.Write(tabNameText);
            console.SetUnderline(false);
            console.Write(BOX_BLANK);
        }
        console.Write(BOX_TEE_RIGHT);
        console.Write(BOX_TOP_RIGHT);
    }
}

