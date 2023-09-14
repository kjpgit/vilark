// Copyright (C) 2023 Karl Pickett / Vilark Project
namespace vilark;

class MainTab: IView
{
    public event EventHandler<ISelectableItem>? ItemChosen;

    public LoadingView m_loading_view;
    public SearchBar m_searchbar;
    public SpacerBar m_spacerbar;
    public ScrollView m_scrollview;

    public MainTab(IView parent, Config config) : base(parent) {
        m_scrollview = new(this, config);
        m_loading_view = new(this);
        m_searchbar = new(this);
        m_spacerbar = new(this);

        // Initially visible views
        m_searchbar.SetVisible(true);
        m_spacerbar.SetVisible(true);
        m_loading_view.SetVisible(true);
        // scrollview is not visible until loading is done
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
        usedRows += 1;
        m_spacerbar.Resize(Size.Subrect(0, usedRows, Size.width, 1));
        usedRows += 1;

        // Main file browsing area, w/scrolling
        m_loading_view.Resize(Size.Subrect(0, usedRows, Size.width, Size.height-usedRows));
        m_scrollview.Resize(Size.Subrect(0, usedRows, Size.width, Size.height-usedRows));
    }

    public override void OnKeyPress(KeyPress kp) {
        if (kp.IsRune('\r') || kp.IsRune('\n')) {
            var chosen_item = m_scrollview.GetCurrentItem();
            if (chosen_item != null) {
                // The user has selected an item
                ItemChosen?.Invoke(this, chosen_item);
            }
        } else if (kp.keyCode == KeyCode.DOWN_ARROW || kp.IsRune('\t')) {
            m_scrollview.MoveCursorUpDown(1);
        } else if (kp.keyCode == KeyCode.UP_ARROW || kp.keyCode == KeyCode.BACKTAB) {
            m_scrollview.MoveCursorUpDown(-1);
        } else if (kp.keyCode == KeyCode.PAGE_DOWN) {
            m_scrollview.Scroll(2);
        } else if (kp.keyCode == KeyCode.PAGE_UP) {
            m_scrollview.Scroll(-2);
        } else if (kp.keyCode == KeyCode.END) {
            m_scrollview.Scroll(3);
        } else if (kp.keyCode == KeyCode.HOME) {
            m_scrollview.Scroll(-3);
        } else {
            // Let search box handle it
            m_searchbar.OnKeyPress(kp);
        }
    }

}
