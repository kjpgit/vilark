// Copyright (C) 2023 Karl Pickett / Vilark Project
using System.Runtime.InteropServices;
using System.Collections;
using vilark;

record struct KeypressPayload(KeyPress keypress, EventWaitHandle doneProcessing);
record struct SignalPayload(PosixSignal signal, EventWaitHandle doneProcessing);

class VilarkMain
{
    public const string VERSION = "1.6.2";

    static void Main(string[] args)
    {
        Thread.CurrentThread.Name = "MainThread";
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
        var controller = new Controller(optionsModel);

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

        controller.StartKeyboard();
        controller.StartIPC();

        // Run eventloop
        controller.Run(args);
    }

}
