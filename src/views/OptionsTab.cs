namespace vilark;

class OptionsTab: IView
{
    public event EventHandler<bool>? SearchModeChanged;

    Config m_config;
    private int selectedIndex = 0;
    const int numControls = 7;

    public OptionsTab(Config config) {
        m_config = config;
    }

    private FuzzySearchMode FuzzySearchMode => m_config.FuzzySearchMode;
    private ColorRGB selectionFGColor => m_config.SelectionFGColor;
    private ColorRGB selectionBGColor => m_config.SelectionBGColor;

    public override void Draw(Console console) {
        Log.Info($"drawing options {Size}");
        var ctx = new DrawContext(this, console);

        ctx.DrawRow("  When multiple search words are used:");
        string searchLabel = FuzzySearchMode switch {
            FuzzySearchMode.FUZZY_WORD_ORDERED => "Match exact order",
            FuzzySearchMode.FUZZY_WORD_UNORDERED => "Match any order",
            _ => throw new Exception("unknown FuzzySearchMode"),
        };
        drawListSelector(ctx, 0, "", searchLabel);

        ctx.DrawRow("");
        var fgTexts = new DisplayText[] {
            new DisplayText("  Selection Color (Foreground)   |"),
            new DisplayText("       ", bgColor:selectionFGColor),
            new DisplayText("| "),
            new DisplayText(" Example Selection ", fgColor:selectionFGColor, bgColor:selectionBGColor),
        };
        ctx.DrawRow(fgTexts);

        drawSlider(ctx, 1, "R", selectionFGColor.r);
        drawSlider(ctx, 2, "G", selectionFGColor.g);
        drawSlider(ctx, 3, "B", selectionFGColor.b);

        ctx.DrawRow("");
        var bgTexts = new DisplayText[] {
            new DisplayText("  Selection Color (Background)   |"),
            new DisplayText("       ", bgColor:selectionBGColor),
            new DisplayText("| "),
            new DisplayText(" Example Selection ", fgColor:selectionFGColor, bgColor:selectionBGColor),
        };
        ctx.DrawRow(bgTexts);

        drawSlider(ctx, 4, "R", selectionBGColor.r);
        drawSlider(ctx, 5, "G", selectionBGColor.g);
        drawSlider(ctx, 6, "B", selectionBGColor.b);

        ctx.DrawRow("");
        ctx.DrawRow("  ~~ Note: Use j/k/h/l to adjust ~~");

        while (ctx.usedRows < Size.height) {
            ctx.DrawRow("");
        }
    }

    private void drawSlider(DrawContext ctx, int tabIndex, string label, int current) {
        int percent = MathUtil.GetPercent(current, 255);
        int sliderCols = 10 * percent / 100;
        string slider = new string('=', sliderCols);
        string selectedMarker = (tabIndex == selectedIndex) ? "X" : " ";

        var texts = new DisplayText[] {
            new DisplayText($"      {selectedMarker} {label} {current,3} |"),
            new DisplayText($"{slider, -10}"),
            new DisplayText("|"),
        };
        ctx.DrawRow(texts);
    }

    private void drawListSelector(DrawContext ctx, int tabIndex, string label, string current) {
        string selectedMarker = (tabIndex == selectedIndex) ? "X" : " ";

        var texts = new DisplayText[] {
            new DisplayText($"      {selectedMarker} |< "),
            new DisplayText($"{current, -17}"),
            new DisplayText(" >|"),
        };
        ctx.DrawRow(texts);
    }

    override public void OnKeyPress(KeyPress kp) {
        if (kp.keyCode == KeyCode.DOWN_ARROW || kp.IsRune('\t') || kp.IsRune('j')) {
            cycleTabIndex(1);
        }
        if (kp.keyCode == KeyCode.UP_ARROW || kp.keyCode == KeyCode.BACKTAB || kp.IsRune('k')) {
            cycleTabIndex(-1);
        }
        if (kp.rune != null) {
            if (kp.IsRune('[') || kp.IsRune('h')) {
                GetAdjuster()(-10);
            } else if (kp.IsRune(']') || kp.IsRune('l')) {
                GetAdjuster()(10);
            }
        }
    }

    private void cycleTabIndex(int delta) {
        selectedIndex += delta;
        if (selectedIndex >= numControls)
            selectedIndex = 0;
        if (selectedIndex < 0)
            selectedIndex = numControls - 1;
    }

    private Action<int> GetAdjuster() {
        return selectedIndex switch {
            0 => AdjustSearchMode,
            1 => selectionFGColor.AdjustR,
            2 => selectionFGColor.AdjustG,
            3 => selectionFGColor.AdjustB,
            4 => selectionBGColor.AdjustR,
            5 => selectionBGColor.AdjustG,
            6 => selectionBGColor.AdjustB,
            _ => (int x) => { }
        };
    }

    private void AdjustSearchMode(int delta) {
        if (FuzzySearchMode == FuzzySearchMode.FUZZY_WORD_ORDERED) {
            m_config.FuzzySearchMode = FuzzySearchMode.FUZZY_WORD_UNORDERED;
        } else {
            m_config.FuzzySearchMode = FuzzySearchMode.FUZZY_WORD_ORDERED;
        }
        SearchModeChanged?.Invoke(this, true);
    }

}
