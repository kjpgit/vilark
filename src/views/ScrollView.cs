using static vilark.BoxChars;

namespace vilark;

interface IScrollItem {
    public string GetDisplayString();
    public string GetSelectionString();
}

// This also draws the left and right frame, because the scrollbar is on the right edge
class ScrollView: IView
{
    Config m_config;

    bool drawLeftFrame = true;
    bool drawRightFrame = true;

    private int lineCursor = 0;
    private int lineScroll = 0;
    private IEnumerable<IScrollItem> content_lines = null!;

    public ScrollView(Config config) {
        m_config = config;
    }

    // Called whenever the search results change, so we also adjust the scroll
    public void SetContentLines(IEnumerable<IScrollItem> lines) {
        content_lines = lines;
        ClampScroll();
    }

    // Called when the user moves the cursor (arrows / tab)
    public void MoveCursorUpDown(int yDelta) {
        // Try to scroll page instead, if we are already at the top/bottom.
        if (yDelta < 0 && lineCursor == 0) {
            Scroll(-1);
        } else if (yDelta > 0 && lineCursor == Size.height-1) {
            Scroll(1);
        } else {
            lineCursor += yDelta;
            ClampScroll();
        }
    }

    public void Scroll(int delta) {
        int lines = delta switch {
            2 => Size.height,
            -2 => -Size.height,
            3 => content_lines.Count(),
            -3 => -content_lines.Count(),
            _ => delta
        };
        Log.Info($"Scroll {delta} -> {lines}");
        lineScroll += lines;
        ClampScroll();
    }

    public IScrollItem? GetCurrentItem() {
        var cur = content_lines.Skip(lineScroll + lineCursor).Take(1).SingleOrDefault();
        return cur;
    }

    override public void Draw(Console console) {
        int all_count = content_lines.Count();
        var si = Size.GetScrollInfo(all_count, lineScroll);

        var visible_lines = content_lines.Skip(lineScroll).Take(Size.height).ToList();

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
                ColorRGB? fgColor = isSelected ? m_config.SelectionFGColor : null;
                ColorRGB? bgColor = isSelected ? m_config.SelectionBGColor : null;

                console.SetForegroundColor(fgColor);
                console.SetBackgroundColor(bgColor);
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

                console.SetForegroundColor(null);
                console.SetBackgroundColor(null);
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
        int maxScrollOffset = Math.Max(content_lines.Count() - Size.height, 0);
        lineScroll = Math.Clamp(lineScroll, 0, maxScrollOffset);

        // Update cursor too
        int maxCursorOffset = Math.Min(
                    Math.Max(content_lines.Count() - lineScroll - 1, 0), // Data remaining
                    Math.Max(Size.height - 1, 0) // Viewport size
                    );
        lineCursor = Math.Clamp(lineCursor, 0, maxCursorOffset);
    }

}

