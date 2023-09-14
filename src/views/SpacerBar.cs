// Copyright (C) 2023 Karl Pickett / Vilark Project
using static vilark.BoxChars;

namespace vilark;

// A widget with rows of:
//
//   |              |
//   |              |
//   |              |
//
class SpacerBar: IView
{
    public SpacerBar(IView parent) : base(parent) { }

    override public void Draw(Console console) {
        for (int i = 0; i < Size.height; i++) {
            int currentUsed = 0;
            console.SetCursorXY(0, i, Size);
            console.Write(BOX_VERT);
            currentUsed += 1;

            int fillWidth = Size.NumRemainingCols(currentUsed + 1);
            console.WriteRepeated(BOX_BLANK, fillWidth);

            console.Write(BOX_VERT);
        }
    }
}

