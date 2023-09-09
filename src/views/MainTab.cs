namespace vilark;

class MainTab: IView
{
    public event EventHandler<IScrollItem>? ItemChosen;

    public LoadingView m_loading_view = new();
    public SearchBar m_searchbar = new();
    public SpacerBar m_spacerbar = new();
    public ScrollView m_scollview;

    public MainTab(Config config) {
        m_scollview = new(config);
    }

    public override void UpdateCursor(Console console) {
        if (IsVisible) {
            m_searchbar.UpdateCursor(console);
        }
    }

    public override void OnResize() {
        int usedRows = 0;

        // Search bar and spacer/whitespace below
        m_searchbar.Resize(Size.Subrect(0, 0, Size.width, 1));
        m_searchbar.SetVisible(true);
        usedRows += 1;
        m_spacerbar.Resize(Size.Subrect(0, usedRows, Size.width, 1));
        m_spacerbar.SetVisible(true);
        usedRows += 1;

        // Main file browsing area, w/scrolling
        m_loading_view.Resize(Size.Subrect(0, usedRows, Size.width, Size.height-usedRows));
        m_scollview.Resize(Size.Subrect(0, usedRows, Size.width, Size.height-usedRows));
        m_loading_view.SetVisible(true);
    }

    public override void Draw(Console console) {
        m_searchbar.DrawIfVisible(console);
        m_spacerbar.DrawIfVisible(console);
        m_scollview.DrawIfVisible(console);
        m_loading_view.DrawIfVisible(console);
    }

    public override void OnKeyPress(KeyPress kp) {
        if (kp.IsRune('\r') || kp.IsRune('\n')) {
            var chosen_item = m_scollview.GetCurrentItem();
            if (chosen_item != null) {
                // The user has selected an item
                ItemChosen?.Invoke(this, chosen_item);
            }
        } else if (kp.keyCode == KeyCode.DOWN_ARROW || kp.IsRune('\t')) {
            m_scollview.MoveCursorUpDown(1);
        } else if (kp.keyCode == KeyCode.UP_ARROW || kp.keyCode == KeyCode.BACKTAB) {
            m_scollview.MoveCursorUpDown(-1);
        } else if (kp.keyCode == KeyCode.PAGE_DOWN) {
            m_scollview.Scroll(2);
        } else if (kp.keyCode == KeyCode.PAGE_UP) {
            m_scollview.Scroll(-2);
        } else if (kp.keyCode == KeyCode.END) {
            m_scollview.Scroll(3);
        } else if (kp.keyCode == KeyCode.HOME) {
            m_scollview.Scroll(-3);
        } else {
            // Let search box handle it
            m_searchbar.OnKeyPress(kp);
        }
    }

}
