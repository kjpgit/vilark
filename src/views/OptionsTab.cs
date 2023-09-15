// Copyright (C) 2023 Karl Pickett / Vilark Project
namespace vilark;

class OptionsTab: IView
{
    public event EventHandler<bool>? SearchModeChanged;

    Config m_config;
    private int selectedIndex = 0;
    const int numControls = 9;

    public OptionsTab(IView parent, Config config) : base(parent) {
        m_config = config;
    }

    private ColorRGB selectionFGColor => m_config.SelectionFGColor;
    private ColorRGB selectionBGColor => m_config.SelectionBGColor;

    public override void Draw(Console console) {
        Log.Info($"drawing options {Size}");
        var ctx = new DrawContext(this, console);
        int tabIndex = 0;

        ctx.DrawRow("  When multiple search words are used:");
        string label = m_config.FuzzySearchMode switch {
            FuzzySearchMode.FUZZY_WORD_ORDERED => "Match exact order",
            FuzzySearchMode.FUZZY_WORD_UNORDERED => "Match any order",
            _ => throw new Exception("unknown FuzzySearchMode"),
        };
        drawListSelector(ctx, tabIndex++, "", label);
        ctx.DrawRow("");

        ctx.DrawRow("  How to start a Vim ($EDITOR) process:");
        label = m_config.EditorLaunchMode switch {
            EditorLaunchMode.EDITOR_LAUNCH_REPLACE => "Replace current process",
            EditorLaunchMode.EDITOR_LAUNCH_CHILD => "Start a child process",
            _ => throw new Exception("unknown EditorLaunchMode"),
        };
        drawListSelector(ctx, tabIndex++, "", label);
        ctx.DrawRow("");

        ctx.DrawRow("  When Zero-Lag UX switching, from the Vim plugin:");
        label = m_config.FastSwitchSearch switch {
            FastSwitchSearch.FAST_CLEAR_SEARCH => "Clear the search box text",
            FastSwitchSearch.FAST_PRESERVE_SEARCH => "Preserve the search box text",
            _ => throw new Exception("unknown FastSwitchSearch"),
        };
        drawListSelector(ctx, tabIndex++, "", label);
        ctx.DrawRow("");

        var fgTexts = new DisplayText[] {
            new DisplayText("  Selection Color (Foreground)   |"),
            new DisplayText("       ", bgColor:selectionFGColor),
            new DisplayText("| "),
            new DisplayText(" Example Selection ", fgColor:selectionFGColor, bgColor:selectionBGColor),
        };
        ctx.DrawRow(fgTexts);

        drawSlider(ctx, tabIndex++, "R", selectionFGColor.r);
        drawSlider(ctx, tabIndex++, "G", selectionFGColor.g);
        drawSlider(ctx, tabIndex++, "B", selectionFGColor.b);
        ctx.DrawRow("");

        var bgTexts = new DisplayText[] {
            new DisplayText("  Selection Color (Background)   |"),
            new DisplayText("       ", bgColor:selectionBGColor),
            new DisplayText("| "),
            new DisplayText(" Example Selection ", fgColor:selectionFGColor, bgColor:selectionBGColor),
        };
        ctx.DrawRow(bgTexts);

        drawSlider(ctx, tabIndex++, "R", selectionBGColor.r);
        drawSlider(ctx, tabIndex++, "G", selectionBGColor.g);
        drawSlider(ctx, tabIndex++, "B", selectionBGColor.b);
        ctx.DrawRow("");

        ctx.DrawRow("  ~~ Note: Use j/k/h/l to adjust ~~");

        while (ctx.usedRows < Size.height) {
            ctx.DrawRow("");
        }
    }

    private void drawSlider(DrawContext ctx, int tabIndex, string label, int current) {
        bool isSelected = (tabIndex == selectedIndex);
        int percent = MathUtil.GetPercent(current, 255);
        int sliderCols = 10 * percent / 100;
        string slider = new string('=', sliderCols);
        string selectedLeft  = isSelected ? BoxChars.SELECTED_LEFT : BoxChars.UNSELECTED_LEFT;
        string selectedRight = isSelected ? BoxChars.SELECTED_RIGHT : BoxChars.UNSELECTED_RIGHT;
        ColorRGB? fg = isSelected ? m_config.SelectionFGColor : null;
        ColorRGB? bg = isSelected ? m_config.SelectionBGColor : null;

        var texts = new DisplayText[] {
            new DisplayText($"      "),
            new DisplayText($"{selectedLeft} {label} {current,3} {selectedRight}", fgColor: fg, bgColor: bg),
            new DisplayText($"[{slider, -10}]"),
        };
        ctx.DrawRow(texts);
    }

    private void drawListSelector(DrawContext ctx, int tabIndex, string label, string current) {
        bool isSelected = (tabIndex == selectedIndex);
        string selectedLeft  = (isSelected) ? BoxChars.SELECTED_LEFT : BoxChars.UNSELECTED_LEFT;
        string selectedRight = (isSelected) ? BoxChars.SELECTED_RIGHT : BoxChars.UNSELECTED_RIGHT;
        ColorRGB? fg = isSelected ? m_config.SelectionFGColor : null;
        ColorRGB? bg = isSelected ? m_config.SelectionBGColor : null;

        var texts = new DisplayText[] {
            new DisplayText($"      "),
            new DisplayText($"{selectedLeft} {current} {selectedRight}", fgColor: fg, bgColor: bg),
            new DisplayText($""),
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
                GetAdjuster()(-1);
            } else if (kp.IsRune(']') || kp.IsRune('l')) {
                GetAdjuster()(1);
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
            1 => AdjustEditorLaunchMode,
            2 => AdjustFastSwitchSearch,
            3 => selectionFGColor.AdjustR,
            4 => selectionFGColor.AdjustG,
            5 => selectionFGColor.AdjustB,
            6 => selectionBGColor.AdjustR,
            7 => selectionBGColor.AdjustG,
            8 => selectionBGColor.AdjustB,
            _ => (int x) => { }
        };
    }

    private void AdjustSearchMode(int delta) {
        m_config.FuzzySearchMode = m_config.FuzzySearchMode.IncrementEnum(delta);
        SearchModeChanged?.Invoke(this, true);
    }

    private void AdjustEditorLaunchMode(int delta) {
        m_config.EditorLaunchMode = m_config.EditorLaunchMode.IncrementEnum(delta);
    }

    private void AdjustFastSwitchSearch(int delta) {
        m_config.FastSwitchSearch = m_config.FastSwitchSearch.IncrementEnum(delta);
    }

}
