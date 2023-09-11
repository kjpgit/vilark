// Copyright (C) 2023 Karl Pickett / ViLark Project
using static vilark.BoxChars;

namespace vilark;


// This also draws the left and right frame, because the scrollbar is on the right edge
class ScrollView: IView
{
    Config m_config;

    bool drawLeftFrame = true;
    bool drawRightFrame = true;

    private int lineCursor = 0;
    private int lineScroll = 0;
    private IEnumerable<ISelectableItem>? content_lines = null;

    public ScrollView(Config config) {
        m_config = config;
    }

    public int TotalLineCount => content_lines != null ? content_lines.Count() : 0;

    // Called whenever the search results change, so we also adjust the scroll
    public void SetContentLines(IEnumerable<ISelectableItem> lines) {
        content_lines = lines;
        ClampScroll();
    }

    // Called when the user moves the cursor (arrows / tab)
    public void MoveCursorUpDown(int yDelta) {
        // If we are already at the top/bottom, we can scroll or wrap around.
        if (yDelta < 0 && lineCursor == 0) {
            if (lineScroll == 0) {
                // Can't scroll up anymore, wrap around to bottom
                lineCursor = Size.height + 999;  // this will be corrected by ClampScroll
                lineScroll = TotalLineCount + 999; // this will be corrected by ClampScroll
                ClampScroll();
            } else {
                Scroll(-1);
            }
        } else if (yDelta > 0 && lineCursor == MaxCursorOffset) {
            if (lineScroll == MaxLineScrollOffset) {
                // Can't scroll down any more, wrap around to top
                lineCursor = 0;
                lineScroll = 0;
            } else {
                Scroll(1);
            }
        } else {
            lineCursor += yDelta;
            ClampScroll();
        }
    }

    public void Scroll(int delta) {
        int lines = delta switch {
            2 => Size.height,
            -2 => -Size.height,
            3 => TotalLineCount,
            -3 => -TotalLineCount,
            _ => delta
        };
        Log.Info($"Scroll {delta} -> {lines}");
        lineScroll += lines;
        ClampScroll();
    }

    public ISelectableItem? GetCurrentItem() {
        if (content_lines != null) {
            var cur = content_lines.Skip(lineScroll + lineCursor).Take(1).SingleOrDefault();
            return cur;
        } else {
            return null;
        }
    }

    override public void Draw(Console console) {
        int all_count = TotalLineCount;
        var si = Size.GetScrollInfo(all_count, lineScroll);

        var visible_lines = content_lines!.Skip(lineScroll).Take(Size.height).ToList();

        //Log.Info($"lineScroll: {lineScroll}");
        //Log.Info($"all_count: {all_count}");
        //Log.Info($"IndStart: {si.IndStart}");
        //Log.Info($"IndHeight: {si.IndHeight}");
        //Log.Info($"Size.height: {Size.height}");

        // Draw content
        for (int i = 0; i < Size.height; i++) {
            console.SetCursorXY(0, i, Size);
            int line_len = 0;
            if (drawLeftFrame) {
                console.Write(BOX_VERT);
                line_len += 1;
            }
            console.Write(BOX_BLANK);
            line_len += 1;

            if (i < visible_lines.Count) {
                bool isSelected = (i == lineCursor);
                if (isSelected) {
                    console.SetForegroundColor(m_config.SelectionFGColor);
                    console.SetBackgroundColor(m_config.SelectionBGColor);
                }

                console.Write(BOX_BLANK);
                line_len += 1;

                int maxTextWidth = Size.NumRemainingCols(line_len + 2); // pad + scrollbar
                var displayText = new DisplayText(visible_lines[i].GetDisplayString(),
                        maxColumns:maxTextWidth,
                        autoFit: true);
                //Log.Info($"maxTextWidth {maxTextWidth}, {displayText.Columns}");
                console.Write(displayText);
                line_len += displayText.Columns;

                console.Write(BOX_BLANK);
                line_len += 1;

                if (isSelected) {
                    console.ResetTextAttrs();
                }

            }

            int fillWidth = Size.NumRemainingCols(line_len + 1);
            console.WriteRepeated(" "u8, fillWidth);

            if (drawRightFrame) {
                if (si.IndHeight == Size.height) {
                    console.Write(BOX_VERT);
                } else {
                    if (i >= si.IndStart && i < si.IndStart + si.IndHeight) {
                        console.Write(SCROLLBAR_INDICATOR);
                    } else {
                        console.Write(SCROLLBAR_BACKGROUND);
                    }
                }
            }
        }
    }

    // Ensure we aren't scrolled out of bounds (top or bottom)
    // Both scrolling and filtering / limiting results can need this
    private void ClampScroll() {
        lineScroll = Math.Clamp(lineScroll, 0, MaxLineScrollOffset);

        // Update cursor too
        lineCursor = Math.Clamp(lineCursor, 0, MaxCursorOffset);
    }

    // If there are 10 lines and the window height is 6, the max lines we can shift up is 4
    private int MaxLineScrollOffset => Math.Max(TotalLineCount - Size.height, 0);

    // (10 total lines - 2 shifted up) = 8
    // 8-1 is 7, meaning the cursor can move forward 7 from the first row.
    //
    // (10 total lines - 9 shifted up) = 1
    // 1-1 is 0, meaning the cursor can move forward 0 more (it's already on the last row)
    //
    // 1 total line (all the others filtered out) - 1 = 0, no more following rows to move to
    private int MaxCursorOffset => Math.Min(
                    Math.Max(TotalLineCount - lineScroll - 1, 0), // num lines following
                    Math.Max(Size.height - 1, 0) // Viewport size
                    );

}

