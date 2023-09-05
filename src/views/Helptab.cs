namespace vilark;

class HelpTab: IView
{
    Config m_config;

    public HelpTab(Config config) {
        m_config = config;
    }

    public override void Draw(Console console) {
        var ctx = new DrawContext(this, console);

        ctx.DrawRow($"               ViLark : Version {ViLarkMain.VERSION}");
        ctx.DrawRow("      (C) 2023 Karl Pickett / ViLark Project");
        ctx.DrawRow("");
        ctx.DrawRow("Key Bindings");
        ctx.DrawRow("  Escape                    close/exit (without selecting anything)");
        ctx.DrawRow("  Enter                     confirm selected file/item");
        ctx.DrawRow("");
        ctx.DrawRow("  Left/Right arrow          change tab");
        ctx.DrawRow("  Up/Down arrow,            change selection");
        ctx.DrawRow("    Tab/ShiftTab                            ");
        ctx.DrawRow("  PageUp/PageDown,          scroll (if scrollbar is indicated)");
        ctx.DrawRow("    Home/End                                                  ");
        ctx.DrawRow("");
        ctx.DrawRow("  Ctrl-w                    delete previous word (in search box)");
        ctx.DrawRow("");
        ctx.DrawRow("Notes");
        ctx.DrawRow("  - $EDITOR is used to open files");
        ctx.DrawRow("  - .gitignore files are checked at every level");
        ctx.DrawRow("  - $VILARK_IGNORE_FILE holds additional ignore patterns ");
        ctx.DrawRow("    (Default: ~/.config/vilark/ignore_rules.txt)");

        while (ctx.usedRows < Size.height) {
            ctx.DrawRow("");
        }
    }

}
