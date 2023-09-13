// Copyright (C) 2023 Karl Pickett / Vilark Project
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Timers;
namespace vilark;

class Controller
{
    // Models / Data
    private Console console;
    private EventQueue<KeypressPayload> m_keyboard_events;
    private EventQueue<SignalPayload> m_signal_events;
    public EventQueue<Notification> m_notifications;
    public EventQueue<string> m_web_replies;
    private Config m_config;
    private InputModel m_input_model;
    private OutputModel m_output_model;
    private OptionsModel m_options_model;

    // Views
    private TitleBar m_titlebar;
    private MainTab m_main_tab;
    private OptionsTab m_options_tab;
    private HelpTab m_help_tab;
    private BottomBar m_bottombar;

    // User / UX State
    private EventWaitHandle? m_keyboard_paused = null;
    private bool        m_child_process_running = false;
    private bool        m_web_request_running = false;
    private System.Timers.Timer? m_redraw_timer = null;  // For the loading spinner only

    public Controller(Console console, OptionsModel optionsModel)
    {
        this.console = console;
        m_keyboard_events = new();
        m_signal_events = new();
        m_notifications = new();
        m_web_replies = new();
        m_options_model = optionsModel;

        m_config = new();
        m_input_model = new(optionsModel, m_config, m_notifications);
        m_output_model = new(m_config);

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

        m_redraw_timer = new System.Timers.Timer(LoadingView.RedrawMilliseconds);
        m_redraw_timer.Elapsed += OnRedrawTimer;
        m_redraw_timer.AutoReset = true;
        m_redraw_timer.Enabled = true;
    }

    public void Run(string[] args) {
        try {
            // This causes a clear screen / flicker, so avoid doing it often.
            // It also causes some settings to be saved, which is not always what we want.
            if (!IsPreserveTerminal()) {
                console.SetAlternateScreen(true);
            }
            PrepareViews();
            Redraw();
            m_input_model.StartLoadingAsync();
            RunEventLoop();
        } catch (Exception e) {
            Log.Info("Unclean exit");
            Log.Exception(e);
            CleanupTerminal();
            System.Console.WriteLine("Unexpected error");
            System.Console.WriteLine(e.ToString());
            Environment.Exit(1);
        }
    }

    private void CleanupTerminal() {
        Log.Info($"CleanupTerminal()");
        console.SetCursorVisible(true);
        console.SetUnderline(false);
        console.ResetTextAttrs();
        if (!IsPreserveTerminal()) {
            console.SetAlternateScreen(false);
        }
        console.Flush();
    }

    private void PrepareViews() {
        // Set initially visible views
        m_titlebar.SetVisible(true);
        m_main_tab.SetVisible(true);
        m_bottombar.SetVisible(true);
    }

    private void RunEventLoop() {
        var waitHandles = new WaitHandle[] {
            m_keyboard_events.ConsumerWaitHandle,
            m_signal_events.ConsumerWaitHandle,
            m_notifications.ConsumerWaitHandle
        };
        while (true) {
            int whichReady = WaitHandle.WaitAny(waitHandles);
            if (whichReady == 0) {
                // Keyboard
                KeypressPayload payload = m_keyboard_events.TakeEvent();
                onKeyPress(payload.keypress);
                if (!m_child_process_running || m_web_request_running) {
                    // Allow keyboard thread to read again from tty
                    payload.doneProcessing.Set();
                } else {
                    // Don't reenable keyboard yet.  We still need to respond
                    // to signals like ctrl-z however.
                    m_keyboard_paused = payload.doneProcessing;
                }
            } else if (whichReady == 1) {
                // Posix Signal
                SignalPayload payload = m_signal_events.TakeEvent();
                onPosixSignal(payload.signal);
                // Allow the waiting signal handler thread to return
                payload.doneProcessing.Set();
            } else if (whichReady == 2) {
                Notification notification = m_notifications.TakeEvent();
                onNotification(notification);
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
        Log.Info("Redraw()");
        UpdateViewDimensions();

        // Hide cursor while redrawing
        console.SetCursorVisible(false);

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

    private void OnRedrawTimer(object? sender, ElapsedEventArgs args) {
        // NB: This is called on a threadpool thread.
        Log.Info("OnRedrawTimer Start");
        m_notifications.AddEvent(new Notification(ForceRedraw: true));
        Log.Info("OnRedrawTimer End");
    }

    private void OnSearchChanged(object? sender, bool unused) {
        UpdateSearchModel();
    }

    private void OnItemChosen(object? sender, ISelectableItem item) {
        DoChosenItem(item);
    }

    private void UpdateSearchModel() {
        m_input_model.SetSearchFilter(m_main_tab.m_searchbar.SearchText, m_config.FuzzySearchMode);
        var data = m_input_model.FilteredData;
        if (data != null) {
            m_main_tab.m_scrollview.SetContentLines(data);
            m_main_tab.m_searchbar.TotalCount = data.Count();
        }
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

    private void onKeyPress(KeyPress kp) {
        Log.Info($"got keypress {kp}");

        if (kp.keyCode == KeyCode.ESCAPE) {
            if (m_web_request_running) {
                m_web_replies.AddEvent("");
                m_web_request_running = false;
            } else {
                DoExit();
            }
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

        // We redraw after every keypress, no need to handle Ctrl-L specifically
        if (!m_child_process_running || m_web_request_running) {
            Redraw();
        }
    }

    private void onPosixSignal(PosixSignal sig) {
        Log.Info($"onPosixSignal {sig}, child_running={m_child_process_running}, web_running={m_web_request_running}");

        if (sig == PosixSignal.SIGWINCH || sig == PosixSignal.SIGCONT) {
            if (!m_child_process_running || m_web_request_running) {
                if (sig == PosixSignal.SIGCONT) {
                    // todo: would a non-clearing command be better here?
		    console.SetAlternateScreen(true);
                }
                Redraw();
            }
        }

        if (sig == PosixSignal.SIGINT) {
            // The default handler will terminate us when this func returns
            CleanupTerminal();
        }

        if (sig == PosixSignal.SIGTSTP) {
            CleanupTerminal();
            // The default action of SIGTSTP doesn't send a STOP when we capture it.
            // So send it ourself.
            UnixProcess.SelfSigStop();
        }

        if (sig == PosixSignal.SIGTERM) {
            DoExit();
        }
    }

    private void onNotification(Notification notification) {
        Log.Info($"Got notification {notification}");
        if (notification.Processed != null) {
            m_main_tab.m_loading_view.CurrentData = notification;
        }
        if (notification.CompletedData != null) {
            // Files all done loading.  Turn off the progress bar, and move to main UX
            m_main_tab.m_loading_view.SetVisible(false);
            m_main_tab.m_scrollview.SetVisible(true);
            m_input_model.SetCompletedData(notification.CompletedData);
            // Update in case the user already typed something
            UpdateSearchModel();

            // Turn off the redraw timer
            m_redraw_timer!.Enabled = false;

            Redraw();
        }
        if (notification.ErrorMessage != null) {
            CleanupTerminal();
            System.Console.WriteLine("Error when scanning files");
            System.Console.WriteLine(notification.ErrorMessage);
            Environment.Exit(1);
        }
        if (notification.ChildExited == true) {
            Log.Info("Child process exited, re-enabling keyboard / tty reads.");
            m_child_process_running = false;
            EnableKeyboard();  // No reason to be suspended
            Redraw();
        }
        if (notification.ForceRedraw) {
            // Sent by the loading progress timer
            Redraw();
        }
        if (notification.WebRequest != null) {
            Log.Info("Fast switching to our UX, for IPC request");
            m_web_request_running = true;
            if (m_config.FastSwitchSearch == FastSwitchSearch.FAST_CLEAR_SEARCH) {
                m_main_tab.m_searchbar.ClearSearch();
            }
            EnableKeyboard();
            Redraw();
        }
    }

    private void DoExit() {
        Log.Info($"Exiting due to escape or SIGTERM. Nothing chosen. child={m_child_process_running}");
        CleanupTerminal();
        m_config.SaveSettings();
        m_output_model.WriteOutput(null);
        Environment.Exit(0);
    }

    private void DoChosenItem(ISelectableItem item) {
        Log.Info("An item was chosen.");

        // This will either:
        // a. execve() and not return (it could throw an exception on failure)
        // b. Process.Start() and wait for child process to exit
        if (m_output_model.GetEditorCommand() != null) {
            if (m_web_request_running) {
                var fullPath = Path.GetFullPath(item.GetChoiceString());
                m_web_replies.AddEvent(fullPath);
                m_web_request_running = false;
            } else {
                m_output_model.LaunchEditor(item, m_notifications);
                m_child_process_running = true;
            }
        } else {
            CleanupTerminal();
            m_config.SaveSettings();
            m_output_model.WriteOutput(item);
            Environment.Exit(0);
        }
    }

    // This is called by other threads (thread pool ones)
    // Signal our main thread safely.
    // This does not return until our main thread ack's it.
    public void ExternalSignalInput(PosixSignal signal) {
        using (var doneProcessing = new EventWaitHandle(false, EventResetMode.AutoReset)) {
            var payload = new SignalPayload(signal, doneProcessing);
            m_signal_events.AddEvent(payload);
            payload.doneProcessing.WaitOne();
        }
    }

    // Called by a separate console read thread.
    // This does not return until our main thread ack's it.
    // This is so we can choose to launch a child process like vim and wait for it,
    // and not read from the tty while it is running.
    public void ExternalKeyboardInput(KeypressPayload payload) {
        m_keyboard_events.AddEvent(payload);
    }

    private void EnableKeyboard() {
        if (m_keyboard_paused != null) {
            Log.Info("Enabling keyboard");
            m_keyboard_paused.Set();
            m_keyboard_paused = null;
        }
    }

    private bool IsPreserveTerminal() {
        var e = Environment.GetEnvironmentVariable("VILARK_PRESERVE_TERMINAL");
        return (e != null && e != "0");
    }

}
