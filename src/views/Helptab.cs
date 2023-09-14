// Copyright (C) 2023 Karl Pickett / Vilark Project
namespace vilark;

class HelpTab: IView
{
    Config m_config;

    public HelpTab(IView parent, Config config) : base(parent) {
        m_config = config;
    }

    public override void Draw(Console console) {
        var ctx = new DrawContext(this, console);

        ctx.DrawRow($"               Vilark : Version {VilarkMain.VERSION}");
        ctx.DrawRow("      (C) 2023 Karl Pickett / Vilark Project");
        ctx.DrawRow("");
        ctx.DrawRow("Key Bindings");
        ctx.DrawRow("  Escape                     cancel / quit");
        ctx.DrawRow("  Enter                      confirm selected file or buffer");
        ctx.DrawRow("");
        ctx.DrawRow("  Left/Right                 change tab");
        ctx.DrawRow("  Up/Down/Tab/ShiftTab       change selection");
        ctx.DrawRow("  PageUp/PageDown/Home/End   scroll (if scrollbar is indicated)");
        ctx.DrawRow("");
        ctx.DrawRow("  Ctrl-w                     delete previous word (in search box)");
        ctx.DrawRow("");
        ctx.DrawRow("Notes");
        ctx.DrawRow("  - .gitignore files are checked at every level");
        ctx.DrawRow("  - $VILARK_IGNORE_FILE holds additional ignore patterns ");
        ctx.DrawRow("    (Default: ~/.config/vilark/ignore_rules.txt)");

        while (ctx.usedRows < Size.height) {
            ctx.DrawRow("");
        }
    }

}
