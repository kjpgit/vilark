using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
namespace vilark;

class Controller
{
    // Models / Data
    private Console console;
    private InputEvent<KeyPress> keyboardEvent;
    private InputEvent<PosixSignal> signalEvent;
    private Config m_config;
    private InputModel m_input_model;
    private OutputModel m_output_model;
    private OptionsModel m_options_model;
    public EventWaitHandle SignalProcessingDone = new EventWaitHandle(initialState:false, EventResetMode.AutoReset);

    // Views
    private TitleBar m_titlebar;
    private MainTab m_main_tab;
    private OptionsTab m_options_tab;
    private HelpTab m_help_tab;
    private BottomBar m_bottombar;

    // User / UX State
    private IScrollItem? m_chosen_item = null;
    private bool m_quit_signaled = false;

    public Controller(Console console,
                            InputEvent<KeyPress> keyboardEvent,
                            InputEvent<PosixSignal> signalEvent,
                            OptionsModel optionsModel)
    {
        this.console = console;
        this.keyboardEvent = keyboardEvent;
        this.signalEvent = signalEvent;
        m_options_model = optionsModel;

        m_config = new();
        m_input_model = new(m_config);
        m_output_model = new();

        // Views
        m_titlebar = new();
        m_main_tab = new(m_config);
        m_options_tab = new(m_config);
        m_help_tab = new(m_config);
        m_bottombar = new();

        // Wire up events (late binding)
        m_main_tab.ItemChosen += OnItemChosen;
        m_main_tab.m_searchbar.SearchChanged += OnSearchChanged;
        m_options_tab.SearchModeChanged += OnSearchChanged;
    }

    public void Run(string[] args) {
        console.SetAlternateScreen(true);
        if (Environment.GetEnvironmentVariable("VILARK_NOCLEAR") == null) {
            console.ClearScreen();
        }
        console.Flush();

        try {
            m_input_model.LoadInput(m_options_model);
            UpdateSearchModel();

            PrepareViews();
            RunUntilExit();
            Log.Info("Clean exit");

            CleanupTerminal();
            m_config.SaveSettings();

            // This writes a tmp file, or execs a process
            m_output_model.WriteOutput(m_chosen_item);
            Environment.Exit(0);
        } catch (Exception e) {
            var edi = ExceptionDispatchInfo.Capture(e);
            Log.Info("Unclean exit");
            Log.Exception(e);
            CleanupTerminal();
            edi.Throw();
            //Environment.Exit(1);
        }
    }

    private void CleanupTerminal() {
        console.SetCursorVisible(true);
        console.SetUnderline(false);
        console.SetForegroundColor(null);
        console.SetBackgroundColor(null);
        console.SetAlternateScreen(false);
        console.Flush();
    }

    private void PrepareViews() {
        // Set initially visible views
        m_titlebar.SetVisible(true);
        m_main_tab.SetVisible(true);
        m_bottombar.SetVisible(true);
    }

    private void RunUntilExit() {
        Redraw();

        var waitHandles = new WaitHandle[] {keyboardEvent.ConsumerWaitHandle, signalEvent.ConsumerWaitHandle};
        while (true) {
            int whichReady = WaitHandle.WaitAny(waitHandles);
            if (whichReady == 0) {
                // Keyboard
                KeyPress kp = keyboardEvent.TakeEvent();
                onKeyPress(kp);
                if (m_quit_signaled) {
                    return;
                }
                // Allow next keypress read
                keyboardEvent.RestartProducer();
            } else if (whichReady == 1) {
                // Posix Signal
                PosixSignal sig = signalEvent.TakeEvent();
                onPosixSignal(sig);
                if (m_quit_signaled) {
                    return;
                }
                // Allow this signal to return
                SignalProcessingDone.Set();
                // Allow next signal to be processed
                signalEvent.RestartProducer();
            } else {
                throw new Exception($"invalid whichReady {whichReady}");
            }
        }
    }

    // NB: Don't change visiblity here, this needs to resize hidden windows too.
    private void UpdateViewDimensions() {
        var dim = console.GetDimensions();
        var winSize = new DrawRect(0, 0, dim.cols, dim.rows);
        int usedRows = 0;

        // Title
        m_titlebar.Resize(winSize.Subrect(0, 0, winSize.width, 1));
        usedRows += m_titlebar.Size.height;

        m_main_tab.Resize(winSize.Subrect(0, usedRows, winSize.width, winSize.height - 2));
        m_options_tab.Resize(winSize.Subrect(0, usedRows, winSize.width, winSize.height - 2));
        m_help_tab.Resize(winSize.Subrect(0, usedRows, winSize.width, winSize.height - 2));
        usedRows += m_main_tab.Size.height;

        // Bottom Status Bar/Frame
        m_bottombar.Resize(winSize.Subrect(0, usedRows, winSize.width, 1));
    }

    private void Redraw() {
        UpdateViewDimensions();

        // Hide cursor while redrawing
        console.SetCursorVisible(false);
        console.Flush();

        m_titlebar.DrawIfVisible(console);
        m_main_tab.DrawIfVisible(console);
        m_options_tab.DrawIfVisible(console);
        m_help_tab.DrawIfVisible(console);
        m_bottombar.DrawIfVisible(console);

        // Now re-enable and re-position cursor
        m_main_tab.UpdateCursor(console);
        m_options_tab.UpdateCursor(console);
        m_help_tab.UpdateCursor(console);

        console.Flush();
    }

    private void OnSearchChanged(object? sender, bool unused) {
        UpdateSearchModel();
    }

    private void OnItemChosen(object? sender, IScrollItem item) {
        m_chosen_item = item;
        m_quit_signaled = true;
    }

    private void UpdateSearchModel() {
        m_input_model.SetSearchFilter(m_main_tab.m_searchbar.SearchText);
        m_main_tab.m_scollview.SetContentLines(m_input_model.FilteredData);
    }

    private void ActiveTabChanged() {
        m_main_tab.SetVisible(false);
        m_options_tab.SetVisible(false);
        m_help_tab.SetVisible(false);
        Action<bool> setFunc = m_titlebar.CurrentTabIndex switch {
            0 => m_main_tab.SetVisible,
            1 => m_options_tab.SetVisible,
            2 => m_help_tab.SetVisible,
            _ => throw new Exception("unknown tab"),
        };
        setFunc(true);
    }

    private void onKeyPress(KeyPress kp) {
        Log.Info($"got keypress {kp}");

        if (kp.keyCode == KeyCode.ESCAPE) {
            m_quit_signaled = true; // Exit
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

        // We redraw after every keypress, no need to handle Ctrl-l specifically
        Redraw();
    }

    private void onPosixSignal(PosixSignal sig) {
        Log.Info($"onPosixSignal {sig}");

        if (sig == PosixSignal.SIGWINCH || sig == PosixSignal.SIGCONT) {
            console.SetAlternateScreen(true);
            Redraw();
        }
        if (sig == PosixSignal.SIGINT) {
            Log.Info("moving back to main terminal screen (SIGINT)");
            CleanupTerminal();
            Environment.Exit(3);
        }
        if (sig == PosixSignal.SIGTSTP) {
            Log.Info("moving back to main terminal screen (SIGTSTP)");
            CleanupTerminal();
            UnixProcess.SelfSigStop();
        }
        if (sig == PosixSignal.SIGTERM) {
            m_quit_signaled = true;
        }
    }

}
