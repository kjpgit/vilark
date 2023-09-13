// Copyright (C) 2023 Karl Pickett / Vilark Project
using static vilark.BoxChars;
namespace vilark;

record struct ScrollInfo(int IndStart, int IndHeight);

readonly record struct DrawRect(int x, int y, int width, int height)
{
    public int NumRemainingCols(int usedCols) {
        int ret = Math.Clamp(width - usedCols, 0, width);
        return ret;
    }

    public DrawRect Subrect(int x, int y, int width, int height) {
        return new DrawRect(this.x + x, this.y + y, width, height);
    }


    public ScrollInfo GetScrollInfo(int totalItems, int scrollOffset) {
        // What percentage of data could be visible in our viewport => scrollInd height
        int height_percent = MathUtil.GetPercent(height, totalItems);
        int scrollIndHeight = Math.Clamp(height_percent * height / 100, 1, height);

        // How many items we have scrolled -> item scroll %
        int max_item_scroll = Math.Max(totalItems - height, 0);
        int item_scroll_percent = MathUtil.GetPercent(scrollOffset, max_item_scroll);

        // How many total tty lines our scroll indicator can move.
        // (Some are so tall, they can only move a few lines when transitioning from 0% => 100% )
        int max_ind_movement = height - scrollIndHeight;

        // % of max_ind_movement  => tty vertical position of scroll indicator
        int scrollIndStartY = Math.Clamp(item_scroll_percent * max_ind_movement / 100, 0, height - 1);

        Log.Info($"max_ind_movement: {max_ind_movement}, totalItems: {totalItems}, scrollOffset: {scrollOffset}");
        Log.Info($"item_scroll_percent: {item_scroll_percent}, height_percent: {height_percent}");

        return new ScrollInfo(scrollIndStartY, scrollIndHeight);
    }

}

class DrawContext
{
    public int usedRows = 0;
    private IView view;
    private Console console;

    public DrawContext(IView view, Console console) {
        this.view = view;
        this.console = console;
    }

    private DrawRect Size => view.Size;

    public void DrawRow(DisplayText[] texts) {
        console.SetCursorXY(0, usedRows, Size);
        int usedCols = 0;
        console.Write(BOX_VERT);
        usedCols += 1;

        foreach (var text in texts) {
            console.Write(text);
            usedCols += text.Columns;
        }

        int fillCols = Size.NumRemainingCols(usedCols) - 1;
        console.WriteRepeated(" "u8, fillCols);
        console.Write(BOX_VERT);
        usedRows += 1;
    }

    public void DrawRow(string text) {
        DrawRow(new DisplayText[] { new DisplayText(text) });
    }
}
