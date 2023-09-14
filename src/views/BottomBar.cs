// Copyright (C) 2023 Karl Pickett / Vilark Project
using static vilark.BoxChars;

namespace vilark;

// A widget with a row of:
//
//   \-------------/
//
class BottomBar: IView
{
    public BottomBar(IView parent) : base(parent) { }

    override public void Draw(Console console) {
        console.SetCursorXY(0, 0, Size);
        console.Write(BOX_BOTTOM_LEFT);
        int currentUsed = 1;

        int fillWidth = Size.NumRemainingCols(currentUsed + 1);
        console.WriteRepeated(BOX_HORIZ, fillWidth);

        console.Write(BOX_BOTTOM_RIGHT);
    }
}

