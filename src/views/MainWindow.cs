// Copyright (C) 2023 Karl Pickett / Vilark Project
namespace vilark;

class MainWindow: IView
{
    public TitleBar m_titlebar;
    public MainTab m_main_tab;
    public OptionsTab m_options_tab;
    public HelpTab m_help_tab;
    public BottomBar m_bottombar;
    public event EventHandler<bool>? WindowClosedByEscape;

    public MainWindow(Config config) : base(null) {
        m_titlebar = new(this);
        m_main_tab = new(this, config);
        m_options_tab = new(this, config);
        m_help_tab = new(this, config);
        m_bottombar = new(this);

        // Set initially visible views
        m_titlebar.SetVisible(true);
        m_main_tab.SetVisible(true);
        m_bottombar.SetVisible(true);
    }

    public override void OnResize() {
        int usedRows = 0;

        // Title
        m_titlebar.Resize(Size.Subrect(0, 0, Size.width, 1));
        usedRows += m_titlebar.Size.height;

        m_main_tab.Resize(Size.Subrect(0, usedRows, Size.width, Size.height - 2));
        m_options_tab.Resize(Size.Subrect(0, usedRows, Size.width, Size.height - 2));
        m_help_tab.Resize(Size.Subrect(0, usedRows, Size.width, Size.height - 2));
        usedRows += m_main_tab.Size.height;

        // Bottom Status Bar/Frame
        m_bottombar.Resize(Size.Subrect(0, usedRows, Size.width, 1));
    }

    private void ActiveTabChanged() {
        m_main_tab.SetVisible(false);
        m_options_tab.SetVisible(false);
        m_help_tab.SetVisible(false);
        IView v = m_titlebar.CurrentTabIndex switch {
            0 => m_main_tab,
            1 => m_options_tab,
            2 => m_help_tab,
            _ => throw new Exception("unknown tab"),
        };
        v.SetVisible(true);
    }

    public void onKeyPress(KeyPress kp) {
        if (kp.keyCode == KeyCode.ESCAPE) {
            WindowClosedByEscape?.Invoke(this, true);
        } else if (kp.keyCode == KeyCode.RIGHT_ARROW) {
            m_titlebar.CycleTabs(1);
            ActiveTabChanged();
        } else if (kp.keyCode == KeyCode.LEFT_ARROW) {
            m_titlebar.CycleTabs(-1);
            ActiveTabChanged();
        } else if (m_main_tab.IsVisible) {
            m_main_tab.OnKeyPress(kp);
        } else if (m_options_tab.IsVisible) {
            m_options_tab.OnKeyPress(kp);
        }
    }
}


