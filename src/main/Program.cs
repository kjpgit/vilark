// Copyright (C) 2023 Karl Pickett / ViLark Project
using System.Runtime.InteropServices;
using System.Collections;
using vilark;

record struct KeypressPayload(KeyPress keypress, EventWaitHandle doneProcessing);
record struct SignalPayload(PosixSignal signal, EventWaitHandle doneProcessing);

class ViLarkMain
{
    public const string VERSION = "1.6.1";

    static void Main(string[] args)
    {
        Thread.CurrentThread.Name = "mainThread";
        string? debugLogName = Environment.GetEnvironmentVariable("VILARK_DEBUG_LOG");
        string logLevel = Environment.GetEnvironmentVariable("VILARK_DEBUG_LOG_LEVEL") ?? "INFO";

        if (debugLogName != null) {
            Log.Open(debugLogName, logLevel);
        }

        // Log execution env
        foreach (var arg in args) {
            Log.Info($"Main arg: {arg}");
        }
        var envVars = Environment.GetEnvironmentVariables().Cast<DictionaryEntry>();
        var sortedEnvVars = envVars.OrderBy(x => (string)x.Key);
        foreach (var e in sortedEnvVars) {
            Log.Info($"Main env: {e.Key}={e.Value}");
        }

        // Parse CLI options.  This will exit if anything is invalid.
        var optionsModel = new OptionsModel();

        // Options look ok, continue startup.
        // Set up the UX and signal handlers.
        var console = new vilark.Console();
        var controller = new Controller(console, optionsModel);

        // Synchronization is important here.. one signal at a time is processed by main thread,
        // and we wait until it is done before returning to the runtime.
        var captureSignal = (PosixSignalContext context, bool cancel) => {
            Log.Info($"Signal start: {context.Signal}");
            controller.ExternalSignalInput(context.Signal);
            context.Cancel = cancel;
            Log.Info($"Signal end: {context.Signal}");
        };

        UnixProcess.RegisterSignalHandler(PosixSignal.SIGWINCH, (context) => captureSignal(context, false));
        UnixProcess.RegisterSignalHandler(PosixSignal.SIGTERM, (context) => captureSignal(context, false));

        // We get this if the user resumes after CTRL-Z SUSPend
        // We redraw and re-enable alternate screen when this happens
        // Also, the .NET runtime restores termios as long as we don't cancel it.
        UnixProcess.RegisterSignalHandler(PosixSignal.SIGCONT, (context) => captureSignal(context, false));

        // CTRL-\ sends this, we don't want it to kill us, so we cancel it
        UnixProcess.RegisterSignalHandler(PosixSignal.SIGQUIT, (context) => captureSignal(context, true));

        // CTRL-Z handler, if it comes as a signal.
        // Unfortunately, .NET doesn't support reading it as a keypress.
        //
        // The choices we have:
        // 1) Capture and ignore signal = user can't suspend with ctrl-Z
        // 2) Default signal = suspend app but keeps us on application screen (doesn't switch to shell)
        // 3) Capture and switch back to main screen, then send ourselves a SIGSTOP.
        // We go for option 3)
        //
        // Feature request filed: https://github.com/dotnet/runtime/issues/91709
        //
        UnixProcess.RegisterSignalHandler(PosixSignal.SIGTSTP, (context) => captureSignal(context, true));

        // CTRL-C
        // This is terrible if left at the default, it kills us and leaves us on the app screen.
        // So we catch it, do basic terminal cleanup, and exit.
        //
        // I don't know of many full screen apps that do exit on ctrl-c (tig does), but
        // we're just a simple dialog so it's probably fine.
        UnixProcess.RegisterSignalHandler(PosixSignal.SIGINT, (context) => captureSignal(context, false));

        // Read from keyboard/tty in a separate thread.
        var keyboard = new Keyboard();
        var consoleReadThread = new Thread(() => {
                var doneProcessing = new EventWaitHandle(false, EventResetMode.AutoReset);
                foreach (KeyPress kp in keyboard.GetKeyPress()) {
                    controller.ExternalKeyboardInput(new KeypressPayload(kp, doneProcessing));
                    doneProcessing.WaitOne();
                }
            });
        consoleReadThread.Name = "consoleReadThread";
        consoleReadThread.Start();

        // For super fast IPC, without launching another process :-)
        if (Environment.GetEnvironmentVariable("VILARK_IPC_URL") == null) {
            var web = new WebListener(controller);
            Environment.SetEnvironmentVariable("VILARK_IPC_URL", web.GetUrl());
            var webThread = new Thread(() => {
                    web.Run();
                });
            webThread.Name = "webThread";
            webThread.Start();
        } else {
            Log.Info("VILARK_IPC_URL already set, not starting another web listener");
        }

        // Run eventloop
        controller.Run(args);
    }

}
